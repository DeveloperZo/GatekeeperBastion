using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: hitscan weapon on the player camera with per-slot clip/reload,
    /// hold left-click to fire, R to reload. Slots switch via 1/2/3, mouse wheel, and Q —
    /// never which owned weapon fills a slot (that is ARMORY-only).</summary>
    /// <remarks>
    /// THE PLAYER CAN ALWAYS SHOOT SOMETHING. With no owned weapon the rig falls back to the
    /// IMPROVISED SALVAGE CUTTER (40% damage, 50% rate). Critical hits are rolled here and ONLY
    /// here from Luck. Firing modes (pellets / pierce / falloff) branch on WeaponDef fields in
    /// one method — never a subclass per weapon.
    /// </remarks>
    public sealed partial class PlayerGun : MonoBehaviour
    {
        const string Tag = "PlayerGun";
        const int SlotCount = 3;

        /// <summary>Improvised-cutter damage as a fraction of the configured weapon.</summary>
        public const float CutterDamageFactor = 0.4f;

        /// <summary>Improvised-cutter fire rate as a fraction of the configured weapon.</summary>
        public const float CutterRateFactor = 0.5f;

        /// <summary>What a critical hit multiplies damage by. Double, flat — Luck buys chance.</summary>
        public const float CritDamageMultiplier = 2f;

        // Inert editor defaults for direct-play; overwritten by Configure at run start.
        [SerializeField] float damage = 14f;
        [SerializeField] float shotsPerSecond = 3f;
        [SerializeField] float clipSize = 8f;
        [SerializeField] float reloadSeconds = 1.4f;
        [SerializeField] float range = 45f;
        [SerializeField] float tracerWidth = 0.030f;

        [SerializeField, OptionalBinding, Tooltip("Isometric rig (Ship probe). While wired and " +
            "enabled, aim is unit-level off the cursor ray and fire does not require cursor " +
            "lock; null or disabled means first-person behavior, unchanged.")]
        IsometricCameraRig _isoRig;

        struct SlotState
        {
            public bool configured;
            public WeaponId id;
            public WeaponDef def;
            public int[] wheelPoints;
            public int clipRemaining;
            public float reloadTimer;
            public bool reloadLogged;
        }

        readonly SlotState[] _slots = new SlotState[SlotCount];
        WeaponSlot _activeSlot = WeaponSlot.Secondary;
        WeaponSlot _previousSlot = WeaponSlot.Secondary;
        float _cooldown;
        Material _tracerMat;
        Material _critTracerMat;
        Material _sparkMat;
        Material _critSparkMat;
        PlayerHealth _owner;
        PlayerAbility _ability;
        WeaponViewModel _view;
        // Pushed by GameController.ApplyMetaEffects (the rig is runtime-resolved, so the
        // controller binds itself). Null outside a fight scene.
        GameController _game;
        string _lastRefusal;
        // 32: crowded pierce lines used to truncate at 16 when geometry filled the buffer
        // (Architecture/5-Audit-2026-08-09). Layer mask still pending — no Enemy/World layers yet.
        readonly RaycastHit[] _pierceHits = new RaycastHit[32];
        readonly List<Damageable> _pierceScratch = new List<Damageable>(8);
        float _secondaryRegenAcc;

        /// <summary>Bodies the next shot pierces at full damage while Railshot is armed. 0 = idle.</summary>
        int _railshotPierceBodies;
        // Armed-window countdown, seconds; ticked down by Update while armed. Negative = lapsed.
        // The arm frame itself never ticks (elapsed time starts at zero on arming), so the
        // window's length does not depend on whether the arming script ran before this one.
        float _railshotArmedRemaining;
        int _railshotArmedFrame = -1;

        public WeaponSlot ActiveSlot => _activeSlot;
        public WeaponDef ActiveDef => _slots[(int)_activeSlot].def;
        public WeaponId ActiveWeaponId => _slots[(int)_activeSlot].id;
        public int ActiveTier
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                return s.wheelPoints != null ? WeaponWheel.DerivedTier(s.wheelPoints) : 0;
            }
        }

        /// <summary>Ability dial points on the active weapon (0 when unconfigured).</summary>
        public int ActiveAbilityPoints
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                if (s.wheelPoints == null || s.wheelPoints.Length != WeaponWheel.AttrCount) return 0;
                return s.wheelPoints[(int)WheelAttr.Ability];
            }
        }

        /// <summary>True while the isometric rig owns aim: cursor-ray targeting, no cursor lock.</summary>
        bool IsoAimActive => _isoRig != null && _isoRig.isActiveAndEnabled;

        /// <summary>True while Railshot is armed and has not lapsed.</summary>
        public bool RailshotArmed =>
            _railshotPierceBodies > 0 && _railshotArmedRemaining >= 0f;

        /// <summary>Arm the next shot as Railshot: pierce <paramref name="pierceBodies"/> at full
        /// damage. Lapses after <paramref name="armedSeconds"/> if unfired; re-arming resets
        /// the window.</summary>
        public void ArmRailshot(int pierceBodies, float armedSeconds)
        {
            if (pierceBodies < 1) pierceBodies = 1;
            _railshotPierceBodies = pierceBodies;
            _railshotArmedRemaining = Mathf.Max(0.01f, armedSeconds);
            _railshotArmedFrame = Time.frameCount;
        }

        /// <summary>Clear an armed Railshot that has lapsed unused.</summary>
        public void ClearExpiredRailshot()
        {
            if (_railshotPierceBodies <= 0) return;
            if (_railshotArmedRemaining >= 0f) return;
            ClearRailshot();
        }

        /// <summary>Drop Railshot without firing (wave boundary, weapon swap refuse path).</summary>
        public void ClearRailshot()
        {
            _railshotPierceBodies = 0;
            _railshotArmedRemaining = 0f;
        }

        /// <summary>Adopt the scene's GameController (called from ApplyMetaEffects at run
        /// start). CanFire reads run state through this handle.</summary>
        public void Bind(GameController game)
        {
            _game = game;
        }

        /// <summary>Consume the armed Railshot for this shot. Returns false when idle or lapsed.</summary>
        public bool TryConsumeRailshot(out int pierceBodies)
        {
            pierceBodies = 0;
            if (_railshotPierceBodies <= 0) return false;
            if (_railshotArmedRemaining < 0f)
            {
                ClearRailshot();
                return false;
            }
            pierceBodies = _railshotPierceBodies;
            ClearRailshot();
            return true;
        }

        /// <summary>Rounds left in the active slot's clip.</summary>
        public int ClipRemaining => _slots[(int)_activeSlot].clipRemaining;

        public bool LastShotWasCrit { get; private set; }

        public float CritChance
        {
            get
            {
                var meta = GameSession.Meta;
                return 0f;
            }
        }

        public int MagazineCapacity
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                if (!s.configured) return Mathf.Max(1, Mathf.RoundToInt(clipSize));
                return WeaponWheel.EffectiveClip(s.def, Point(s, WheelAttr.Clip));
            }
        }

        /// <summary>True when no owned weapon is configured on any slot — improvised cutter.</summary>
        public bool UsingSalvageCutter
        {
            get
            {
                var meta = GameSession.Meta;
                if (meta == null || !meta.OwnsAnyWeapon) return true;
                return !_slots[(int)_activeSlot].configured;
            }
        }

        public float EffectiveDamage
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                float dmg = s.configured
                    ? WeaponWheel.EffectiveDamage(s.def, Point(s, WheelAttr.Damage))
                    : damage;
                return UsingSalvageCutter ? dmg * CutterDamageFactor : dmg;
            }
        }

        public string WeaponBlockedReason
        {
            get
            {
                if (UsingSalvageCutter)
                    return $"NO WEAPON — improvised cutter: {EffectiveDamage:0} dmg. " +
                           "Buy the MK-1 at the ship's ARMORY.";
                return _lastRefusal;
            }
        }

        /// <summary>Push catalog stats for one slot at run start. Refills that slot's clip.
        /// Never call mid-fight for equip changes — criterion 4.</summary>
        public void Configure(WeaponSlot slot, WeaponDef def, int[] wheelPoints, WeaponId id)
        {
            int i = (int)slot;
            _slots[i].configured = true;
            _slots[i].id = id;
            _slots[i].def = def;
            _slots[i].wheelPoints = wheelPoints != null && wheelPoints.Length == WeaponWheel.AttrCount
                ? (int[])wheelPoints.Clone()
                : new int[WeaponWheel.AttrCount];
            int clipPts = Point(_slots[i], WheelAttr.Clip);
            _slots[i].clipRemaining = WeaponWheel.EffectiveClip(def, clipPts);
            _slots[i].reloadTimer = 0f;
            _slots[i].reloadLogged = false;
            ApplyTracerFromDef(def);
            int tier = WeaponWheel.DerivedTier(_slots[i].wheelPoints);
            Log.Info(Tag, $"Configured {slot}: {def.displayName} T{tier} — " +
                          $"{WeaponWheel.EffectiveDamage(def, Point(_slots[i], WheelAttr.Damage)):0.#} dmg, " +
                          $"{WeaponWheel.EffectiveFireRate(def, Point(_slots[i], WheelAttr.FireRate)):0.#}/s, " +
                          $"clip {WeaponWheel.EffectiveClip(def, clipPts)}.");
        }

        /// <summary>Legacy overload — treats flat Tier as the pre-wheel migration shape.</summary>
        public void Configure(WeaponSlot slot, WeaponDef def, int flatTier, WeaponId id)
        {
            var pts = new int[WeaponWheel.AttrCount];
            WeaponWheel.DistributeFlatTier(pts, flatTier);
            Configure(slot, def, pts, id);
        }

        static int Point(SlotState s, WheelAttr attr)
        {
            if (s.wheelPoints == null || s.wheelPoints.Length != WeaponWheel.AttrCount) return 0;
            return s.wheelPoints[(int)attr];
        }

        /// <summary>Clear every slot (direct-play / cutter fallback). Keeps serialized defaults.</summary>
        public void ClearLoadout()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _slots[i] = default;
                _slots[i].clipRemaining = Mathf.Max(1, Mathf.RoundToInt(clipSize));
            }
            _activeSlot = WeaponSlot.Secondary;
            _previousSlot = WeaponSlot.Secondary;
        }

        void Awake()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                _tracerMat = new Material(shader) { color = FxColors.TracerDefault };
                _sparkMat = new Material(shader) { color = FxColors.SparkDefault };
                _critTracerMat = new Material(shader) { color = FxColors.CritTracer };
                _critSparkMat = new Material(shader) { color = FxColors.CritSpark };
            }
            else
            {
                Log.Warn(Tag, "No unlit shader found; tracers and sparks disabled this session.");
            }
            ClearLoadout();
            _owner = GetComponentInParent<PlayerHealth>();
            _ability = GetComponent<PlayerAbility>();
            if (_ability == null) _ability = gameObject.AddComponent<PlayerAbility>();
            _view = GetComponentInParent<WeaponViewModel>();
        }

        void OnDestroy()
        {
            if (_tracerMat != null) Destroy(_tracerMat);
            if (_sparkMat != null) Destroy(_sparkMat);
            if (_critTracerMat != null) Destroy(_critTracerMat);
            if (_critSparkMat != null) Destroy(_critSparkMat);
        }

        /// <summary>True once the Bastion's tick owner drives this component (set by Bind);
        /// its own Update then yields so the written system order is the only order.</summary>
        bool _hosted;
        public bool IsHosted => _hosted;

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return; // the Bastion tick owner calls Tick in the written order
            Tick(Time.deltaTime);
        }

        /// <summary>Combat actions: cooldowns, slot input, regen, reload, fire.</summary>
        public void Tick(float dt)
        {
            _cooldown -= dt;
            // The armed window only counts down while armed; idle stays parked at zero.
            if (_railshotPierceBodies > 0 && Time.frameCount != _railshotArmedFrame)
                _railshotArmedRemaining -= dt;
            TickSlotInput();
            TickSecondaryRegen(dt);
            // Tick before fire so a reload started this frame is never immediately advanced.
            TickReload(dt);

            if (!CanFire()) return;

            if (_activeSlot == WeaponSlot.Drone)
            {
                TickDroneFire();
                return;
            }

            if (Controls.Down(ControlId.Reload) && !IsReloading && ClipRemaining < MagazineCapacity)
            {
                StartReload();
                return;
            }

            if (Controls.Held(ControlId.Fire)) TryFire();
        }

        /// <summary>Secondary never stays permanently dry — it regenerates its own ammo.
        /// Baseline always ticks on the Secondary slot; the MK-1 surge multiplies the rate only
        /// while that slot is active (PlayerAbility).</summary>
        void TickSecondaryRegen(float dt)
        {
            int si = (int)WeaponSlot.Secondary;
            ref var s = ref _slots[si];
            if (!s.configured || s.id == WeaponId.None) return;
            if (s.reloadTimer > 0f) return; // a manual reload owns the clip until it finishes

            int cap = WeaponWheel.EffectiveClip(s.def, Point(s, WheelAttr.Clip));
            if (s.clipRemaining >= cap)
            {
                _secondaryRegenAcc = 0f;
                return;
            }

            float rate = WeaponAbility.SecondaryRegenPerSecond;
            float mult = 1f;
            if (_ability == null) _ability = GetComponent<PlayerAbility>();
            if (_ability != null && _activeSlot == WeaponSlot.Secondary && _ability.RegenSurgeActive)
                mult = _ability.RegenSurgeMultiplier;
            rate *= mult;

            int before = s.clipRemaining;
            _secondaryRegenAcc += rate * dt;
            while (_secondaryRegenAcc >= 1f && s.clipRemaining < cap)
            {
                _secondaryRegenAcc -= 1f;
                s.clipRemaining++;
            }
            if (s.clipRemaining >= cap) _secondaryRegenAcc = 0f;

            int gained = s.clipRemaining - before;
            if (gained > 0 && mult > 1.01f && _ability != null)
                _ability.CreditRegenRound(gained * (mult - 1f) / mult);
        }

        public bool TryFire()
        {
            // Resumed mid-reload with rounds left is interruptible — empty-clip reload is not.
            if (IsReloading)
            {
                if (ClipRemaining <= 0) return false;
                CancelActiveReload();
            }
            if (!CanFire()) return false;
            if (_cooldown > 0f) return false;
            ref var s = ref _slots[(int)_activeSlot];
            if (s.clipRemaining <= 0)
            {
                StartReload();
                return false;
            }
            _cooldown = 1f / EffectiveFireRate;
            s.clipRemaining--;
            Fire();
            return true;
        }

        /// <summary>REDEPLOY: a Boarder carrying a machine has no hand for the gun. Pushed by
        /// <see cref="PlayerHardwareHandling"/>, never serialized.</summary>
        public bool HandsFull { get; set; }

        bool CanFire()
        {
            var gc = _game;
            if (HandsFull)
                return Refuse("carrying hardware — both hands are on the machine", false);
            // The Ship is weapons-holstered except the demo's wake fight, and only once the
            // MK-1 is in hand — unarmed is unarmed, the improvised cutter included.
            if (!SceneFlow.InCombatScene && gc == null && !DemoShipFight.CanFireOnShip)
                return Refuse($"not a combat scene (active '{SceneFlow.ActiveSceneName}') and no GameController present", false);

            if (gc != null && gc.RunIsOver)
                return Refuse("the run is over (terminal phase)", false);

            if (NetShipWait.IsActive)
                return Refuse("waiting on the Ship", false);

            if (_owner == null) _owner = GetComponentInParent<PlayerHealth>();
            if (_owner != null && _owner.IsDead)
                return Refuse("the player is down", false);

            // Iso aim is cursor-driven, so a free cursor is the working state there.
            if (Cursor.lockState != CursorLockMode.Locked && !IsoAimActive)
                return Refuse("the cursor is not captured — left-click once to recapture it", true);

            _lastRefusal = null;
            return true;
        }

        bool Refuse(string reason, bool isProblem)
        {
            if (_lastRefusal != reason)
            {
                _lastRefusal = reason;
                if (isProblem) Log.Warn(Tag, $"Fire refused: {reason}.");
                else Log.Info(Tag, $"Fire refused: {reason}.");
            }
            return false;
        }
    }
}
