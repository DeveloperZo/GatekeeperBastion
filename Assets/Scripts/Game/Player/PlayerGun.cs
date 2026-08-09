using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
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
        string _lastRefusal;
        // 32: crowded pierce lines used to truncate at 16 when geometry filled the buffer
        // (Architecture/5-Audit-2026-08-09). Layer mask still pending — no Enemy/World layers yet.
        readonly RaycastHit[] _pierceHits = new RaycastHit[32];
        readonly List<Damageable> _pierceScratch = new List<Damageable>(8);
        float _secondaryRegenAcc;

        /// <summary>Bodies the next shot pierces at full damage while Railshot is armed. 0 = idle.</summary>
        int _railshotPierceBodies;
        float _railshotArmedUntil;

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

        /// <summary>True while Railshot is armed and has not lapsed.</summary>
        public bool RailshotArmed =>
            _railshotPierceBodies > 0 && Time.time <= _railshotArmedUntil;

        /// <summary>Arm the next shot as Railshot: pierce <paramref name="pierceBodies"/> at full
        /// damage. Lapses after <paramref name="armedSeconds"/> if unfired.</summary>
        public void ArmRailshot(int pierceBodies, float armedSeconds)
        {
            if (pierceBodies < 1) pierceBodies = 1;
            _railshotPierceBodies = pierceBodies;
            _railshotArmedUntil = Time.time + Mathf.Max(0.01f, armedSeconds);
        }

        /// <summary>Clear an armed Railshot that has lapsed unused.</summary>
        public void ClearExpiredRailshot()
        {
            if (_railshotPierceBodies <= 0) return;
            if (Time.time <= _railshotArmedUntil) return;
            ClearRailshot();
        }

        /// <summary>Drop Railshot without firing (wave boundary, weapon swap refuse path).</summary>
        public void ClearRailshot()
        {
            _railshotPierceBodies = 0;
            _railshotArmedUntil = 0f;
        }

        /// <summary>Consume the armed Railshot for this shot. Returns false when idle or lapsed.</summary>
        public bool TryConsumeRailshot(out int pierceBodies)
        {
            pierceBodies = 0;
            if (_railshotPierceBodies <= 0) return false;
            if (Time.time > _railshotArmedUntil)
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
                return meta == null ? 0f : Mathf.Clamp01(MetaUpgrades.CritChance(meta.LuckLevel));
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
                           "Buy the MK-1 at the ship's Armory.";
                return _lastRefusal;
            }
        }

        public float EffectiveFireRate
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                float rate = s.configured
                    ? WeaponWheel.EffectiveFireRate(s.def, Point(s, WheelAttr.FireRate))
                    : shotsPerSecond;
                rate = UsingSalvageCutter ? rate * CutterRateFactor : rate;
                return Mathf.Max(0.01f, rate);
            }
        }

        public string DisplayName
        {
            get
            {
                if (UsingSalvageCutter) return "Improvised Cutter";
                ref var s = ref _slots[(int)_activeSlot];
                return s.configured ? s.def.displayName : "Weapon";
            }
        }

        public WeaponCrosshair ActiveCrosshair
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                return s.configured ? s.def.crosshair : WeaponCrosshair.Cross;
            }
        }

        /// <summary>True when the given slot has a configured weapon.</summary>
        public bool SlotConfigured(WeaponSlot slot) => _slots[(int)slot].configured;

        public WeaponId WeaponInSlot(WeaponSlot slot) =>
            _slots[(int)slot].configured ? _slots[(int)slot].id : WeaponId.None;

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
                _tracerMat = new Material(shader) { color = Theme.TracerDefault };
                _sparkMat = new Material(shader) { color = Theme.SparkDefault };
                _critTracerMat = new Material(shader) { color = Theme.CritTracer };
                _critSparkMat = new Material(shader) { color = Theme.CritSpark };
            }
            else
            {
                Log.Warn(Tag, "No unlit shader found; tracers and sparks disabled this session.");
            }
            ClearLoadout();
            _owner = GetComponentInParent<PlayerHealth>();
            _ability = GetComponent<PlayerAbility>();
            if (_ability == null) _ability = gameObject.AddComponent<PlayerAbility>();
        }

        void OnDestroy()
        {
            if (_tracerMat != null) Destroy(_tracerMat);
            if (_sparkMat != null) Destroy(_sparkMat);
            if (_critTracerMat != null) Destroy(_critTracerMat);
            if (_critSparkMat != null) Destroy(_critSparkMat);
        }

        void Update()
        {
            _cooldown -= Time.deltaTime;
            TickSlotInput();
            TickReload(Time.deltaTime);
            TickSecondaryRegen();

            if (!CanFire()) return;

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
        void TickSecondaryRegen()
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
            _secondaryRegenAcc += rate * Time.deltaTime;
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

        void TickSlotInput()
        {
            if (Controls.Down(ControlId.SlotPrimary))
                TrySelectSlot(WeaponSlot.Primary);
            if (Controls.Down(ControlId.SlotSecondary))
                TrySelectSlot(WeaponSlot.Secondary);

            if (Controls.Down(ControlId.SlotLast))
                TrySelectSlot(_previousSlot);

            float scroll = Controls.ScrollDelta();
            if (Mathf.Abs(scroll) > 0.01f)
                CycleSlot(scroll > 0f ? 1 : -1);
        }

        void TrySelectSlot(WeaponSlot slot) => TrySelectSlotPublic(slot);

        /// <summary>Public for GameController.ApplyMetaEffects to pick the starting slot.</summary>
        public void TrySelectSlotPublic(WeaponSlot slot)
        {
            // Empty slots are no-ops (no Primary owned → 1 does nothing).
            if (!_slots[(int)slot].configured) return;
            if (slot == _activeSlot) return;

            ref var outgoing = ref _slots[(int)_activeSlot];
            ApplyReloadOnSwap(ref outgoing);

            _previousSlot = _activeSlot;
            _activeSlot = slot;
            _cooldown = 0f;
            ref var incoming = ref _slots[(int)_activeSlot];
            if (incoming.configured) ApplyTracerFromDef(incoming.def);
            Log.Event("Combat", $"switched to {SlotLabel(_activeSlot)}");
        }

        void CycleSlot(int direction)
        {
            // Cycle non-empty slots only.
            int start = (int)_activeSlot;
            for (int step = 1; step <= SlotCount; step++)
            {
                int idx = (start + direction * step) % SlotCount;
                if (idx < 0) idx += SlotCount;
                var candidate = (WeaponSlot)idx;
                if (!_slots[idx].configured) continue;
                TrySelectSlot(candidate);
                return;
            }
        }

        static string SlotLabel(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary: return "Primary (1)";
                case WeaponSlot.Secondary: return "Secondary (2)";
                default: return slot.ToString();
            }
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

        bool CanFire()
        {
            var gc = GameController.Instance;
            if (!SceneFlow.InCombatScene && gc == null)
                return Refuse($"not a combat scene (active '{SceneFlow.ActiveSceneName}') and no GameController present", true);

            if (gc != null && gc.RunIsOver)
                return Refuse("the run is over (terminal phase)", false);

            if (_owner == null) _owner = GetComponentInParent<PlayerHealth>();
            if (_owner != null && _owner.IsDead)
                return Refuse("the player is down", false);

            if (Cursor.lockState != CursorLockMode.Locked)
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
