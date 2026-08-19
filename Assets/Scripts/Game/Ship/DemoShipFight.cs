using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The beat-0 ship fight and the demo's Ship-scene surface, as a static owner
    /// (the NetShipWait pattern): DemoShipDirector binds the scene wires and ticks it; the
    /// gun, HUD and PlayerHealth read the properties. Two weak intruders release on the
    /// intruders line's Continue, the MK-1 is the only damage source, and the player
    /// cannot die on the Ship while the demo is active (HP floors at 1).</summary>
    public static class DemoShipFight
    {
        const string Tag = "Demo";

        // Beat 0 starting values (Docs/Demo/0-wake-on-a-dead-ship.md, Numbers — stamped at
        // this cut): weakest fodder body, Hatch-fodder swing against the player.
        public const int IntruderCount = 2;
        public const float IntruderHp = 30f;
        public const float IntruderDamage = 4f;
        public const float IntruderSwingSeconds = 0.7f;
        public const float IntruderMoveSpeed = 3.2f;
        public const float IntruderAttackRange = 1.8f;

        static GameObject _intruderPrefab;
        static Transform _spawn1;
        static Transform _spawn2;
        static bool _armed;
        static int _spawned;
        static int _down;
        static string _standing;

        /// <summary>Standing objective / idle prompt line for the Ship HUD. Null when none.</summary>
        public static string StandingObjective => _standing;

        /// <summary>Beat 0 sealed wake: crosshair off until the lid finishes opening.</summary>
        public static bool WakeHidesCrosshair { get; set; }

        public static void SetStanding(string line) => _standing = line;

        /// <summary>True while intruders are aboard (spawned and not all down).</summary>
        public static bool IsActive => _spawned > 0 && _down < _spawned;

        /// <summary>PlayerGun's Ship exception: fire only during the live fight, and only
        /// once the MK-1 is in hand — unarmed means unarmed, cutter included.</summary>
        public static bool CanFireOnShip
        {
            get
            {
                if (!IsActive) return false;
                var m = GameSession.Meta;
                return m != null && m.DemoWeaponGranted;
            }
        }

        /// <summary>PlayerHealth clamp: on the Ship while the demo is active, damage stops
        /// at 1 HP — getting hit is intended, dying in the tutorial is not.</summary>
        public static bool FloorsPlayerHp
        {
            get
            {
                var m = GameSession.Meta;
                return m != null && m.DemoActive
                    && SceneFlow.ActiveSceneName == SceneFlow.Ship;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _intruderPrefab = null;
            _spawn1 = null;
            _spawn2 = null;
            _armed = false;
            _spawned = 0;
            _down = 0;
            _standing = null;
            WakeHidesCrosshair = false;
        }

        /// <summary>Scene wires from DemoShipDirector.OnEnable.</summary>
        public static void BindScene(GameObject intruderPrefab, Transform spawn1, Transform spawn2)
        {
            _intruderPrefab = intruderPrefab;
            _spawn1 = spawn1;
            _spawn2 = spawn2;
        }

        /// <summary>Scene teardown — intruder instances die with the scene; state must not
        /// leak into the Bastion.</summary>
        public static void ClearScene()
        {
            _armed = false;
            _spawned = 0;
            _down = 0;
            _standing = null;
            WakeHidesCrosshair = false;
        }

        /// <summary>The ship terminal's T arms the release; the intruders line's Continue
        /// executes it (gated execution, never a timeScale write). With no line live the
        /// next Tick releases immediately — the gate cannot stick.</summary>
        public static void ArmRelease() => _armed = true;

        /// <summary>Reload matrix: terminal started, fight not done — immediate release
        /// (the grant rode the terminal's save, so a resumed seat is always armed). The
        /// replay flag survives for a pre-revision save that never heard the line.</summary>
        public static void ResumeArmed(bool replayLine)
        {
            if (replayLine)
                DemoVoice.Play(DemoConversationIds.Intruders, DemoVoiceMode.Lock);
            _armed = true;
        }

        /// <summary>Per-frame drive from DemoShipDirector.</summary>
        public static void Tick()
        {
            if (_armed && !DemoVoice.IsLive)
                Release();
        }

        /// <summary>Beat 0's MK-1 grant — the ship terminal's start hands it over
        /// (2026-08-18 revision: no ARMORY rack walk). Reads-back order: granted MK-1,
        /// then BuyWeapon's [Ship] bought line. Already-owned returns false and is
        /// still a successful grant (beat 0 trap). No line here — the intruders Lock
        /// that follows carries the copy.</summary>
        public static void GrantWeapon(MetaState m)
        {
            if (m == null) return;
            Log.Event(Tag, "granted MK-1");
            ShipCommands.BuyWeapon(WeaponId.Mk1Sidearm);
            m.DemoWeaponGranted = true;
            GameSession.Save();
            NotifyWeaponGranted();
        }

        /// <summary>Grant landed: put the MK-1 in the Ship gun's hands.</summary>
        public static void NotifyWeaponGranted()
        {
            var m = GameSession.Meta;
            var player = PlayerRegistry.Local;
            var gun = player != null ? player.GetComponentInChildren<PlayerGun>(true) : null;
            if (gun == null)
            {
                Log.Error(Tag, "MK-1 granted but no PlayerGun on the Ship rig — author it "
                    + "(scratch: Author Demo Wake Ship).");
            }
            else if (m != null)
            {
                var id = WeaponId.Mk1Sidearm;
                gun.Configure(WeaponSlot.Secondary, WeaponCatalog.Def(id), m.PointsOf(id), id);
            }
            if (IsActive) _standing = "Kill the Gatekeepers";
        }

        static void Release()
        {
            _armed = false;
            if (_spawned > 0) return;
            if (_intruderPrefab == null || _spawn1 == null || _spawn2 == null)
            {
                Log.Error(Tag, "intruder release refused — director wires missing "
                    + "(prefab/spawns). Author the Ship scene pieces.");
                return;
            }

            SpawnIntruder(_spawn1);
            SpawnIntruder(_spawn2);
            Log.Event(Tag, "intruders spawned n=" + _spawned);
            // The terminal's start granted the MK-1 with the same save — always armed here.
            _standing = "Kill the Gatekeepers";
        }

        static void SpawnIntruder(Transform at)
        {
            var go = Object.Instantiate(_intruderPrefab, at.position, at.rotation);
            go.name = "Demo Intruder " + (_spawned + 1);

            // The marker contributes XZ + yaw; the body's own capsule decides Y. The
            // prefab's pivot is the capsule centre (center 0, height 2 — feet at
            // pivot − 1) and on the Bastion the NavMeshAgent's baseOffset does that
            // lift; the Ship has no mesh and the agent is off, so spawning at the
            // marker's deck Y buries the body to the waist (2026-08-18 defect, same
            // class as the bed pose). ShipIntruder's walk is planar — this Y holds.
            var capsule = go.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Vector3 lifted = go.transform.position;
                lifted.y = at.position.y + capsule.height * 0.5f - capsule.center.y;
                go.transform.position = lifted;
            }

            // The prefab is the Bastion body. Its brain needs a GameController and its net
            // view needs a session — both absent on the solo Ship — so those components are
            // disabled and ShipIntruder drives the same agent and animator instead.
            var brain = go.GetComponent<EnemyGatekeeper>();
            if (brain != null) brain.enabled = false;
            var netView = go.GetComponent<NetEnemy>();
            if (netView != null) netView.enabled = false;

            var damageable = go.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.SetMaxHp(IntruderHp);
                damageable.onDeath.AddListener(() => OnIntruderDown(go));
            }

            var intruder = go.AddComponent<ShipIntruder>();
            intruder.Configure(IntruderDamage, IntruderSwingSeconds, IntruderMoveSpeed,
                IntruderAttackRange);
            _spawned++;
        }

        static void OnIntruderDown(GameObject body)
        {
            _down++;
            Log.Event(Tag, "intruder down " + _down + "/" + IntruderCount);
            if (body != null) Object.Destroy(body, 0.2f);
            if (_down < IntruderCount) return;

            var m = GameSession.Meta;
            if (m != null)
            {
                m.DemoShipFightDone = true;
                GameSession.Save();
            }
            Log.Event(Tag, "ship clear");
            DemoVoice.Play(DemoConversationIds.ShipClear, DemoVoiceMode.Lock);

            var player = PlayerRegistry.Local;
            bool hurt = player != null && player.Hp < player.MaxHp - 0.5f;
            _standing = hurt
                ? "Restore health at CREW QUARTERS"
                : "EMBARK from the NAV MAP";
        }
    }
}
