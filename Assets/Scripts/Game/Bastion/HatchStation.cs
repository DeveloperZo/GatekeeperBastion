using GatekeeperBastion.Util;
using UnityEngine;
// Alias avoids CS0104: UnityEngine also declares a 'Subsystem' type.
using Subsystem = GatekeeperBastion.Sim.Subsystem;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: scene-side proxy for the sim Hatch — the defended umbilical
    /// junction back to your ship at one end of the bastion. Routes enemy damage and player
    /// repairs into GameController.Sim.Ship.Hatch; static Instance is the enemy pathing target
    /// and the HUD reads Hp/MaxHp. Reads the sim fresh each access, so a run reset (fresh
    /// RunSim) restores hatch HP with no scene work.</summary>
    /// <remarks>Instance is cleared in OnDisable, OnDestroy AND at play-mode start: a stale
    /// static pointing at a destroyed hatch passes `Instance != null` as false but is still an
    /// NRE the moment anyone touches .transform on a reference cached before it died — which is
    /// exactly what the enemy attack path used to do.</remarks>
    public sealed class HatchStation : MonoBehaviour
    {
        const string Tag = "HatchStation";

        /// <summary>The single hatch in the scene (one per bastion), or null if disabled.</summary>
        public static HatchStation Instance { get; private set; }

        /// <summary>Clears the static handle at play-mode start (domain-reload-disabled safety).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        static Subsystem SimHatch
        {
            get
            {
                var gc = GameController.Instance;
                return gc != null && gc.Sim != null ? gc.Sim.Ship.Hatch : null;
            }
        }

        /// <summary>Current sim hit points (0 when the sim is unavailable).</summary>
        public float Hp
        {
            get
            {
                var hatch = SimHatch;
                return hatch != null ? hatch.Hp : 0f;
            }
        }

        /// <summary>Maximum sim hit points (1 when the sim is unavailable, to keep ratios safe).</summary>
        public float MaxHp
        {
            get
            {
                var hatch = SimHatch;
                return hatch != null ? hatch.MaxHp : 1f;
            }
        }

        /// <summary>True when the sim hatch is destroyed or unavailable.</summary>
        public bool IsDestroyed
        {
            get
            {
                var hatch = SimHatch;
                return hatch == null || hatch.IsDestroyed;
            }
        }

        Collider _collider;

        /// <summary>Nearest point on the hatch's own surface to <paramref name="from"/>. Melee range
        /// must be measured against the SURFACE: the hatch is a 4x3x1.5 box whose transform sits at
        /// its centre 1.5m off the floor, so a gatekeeper pressed against the side is ~2.8m from that
        /// centre — outside a 2.4m attack range — and stands there doing nothing.</summary>
        public Vector3 ClosestSurfacePoint(Vector3 from)
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            return _collider != null ? _collider.ClosestPoint(from) : transform.position;
        }

        void OnEnable()
        {
            if (Instance != null && Instance != this)
                Log.Warn(Tag, "Multiple hatches in scene; last one wins.");
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void OnDestroy()
        {
            // Belt and braces: OnDisable is skipped when the object was already inactive.
            if (Instance == this) Instance = null;
        }

        /// <summary>Routes enemy damage into the sim hatch. No-op once the run is over.</summary>
        /// <remarks>Also the whole hatch half of the transcript, because every point of damage in
        /// the game funnels through here. The fatal blow is ANNOUNCED BEFORE it lands: Damage()
        /// raises Destroyed synchronously, which ends the run and emits the summary, so logging
        /// after the call would print the hatch's obituary underneath its own post-mortem.</remarks>
        public void ApplyDamage(float amount)
        {
            if (amount <= 0f) return;
            var gc = GameController.Instance;
            if (gc == null || gc.RunIsOver) return; // guard: no damage into a terminal/absent sim
            var hatch = SimHatch;
            if (hatch == null) return;

            if (hatch.Hp - amount <= 0f)
                Log.Event("Hatch", "HATCH BREACHED — the run is over");
            float before = hatch.Hp;
            hatch.Damage(amount);
            // One line per quarter lost: enough to see from the log whether gatekeepers ever reached
            // home at all, without a line per swing.
            LogThreshold(before, hatch.Hp, hatch.MaxHp);
        }

        /// <summary>Logs the first time the hatch drops through 75/50/25% of its health.</summary>
        static void LogThreshold(float before, float after, float max)
        {
            if (max <= 0f) return;
            float[] marks = { 0.75f, 0.5f, 0.25f };
            for (int i = 0; i < marks.Length; i++)
            {
                float line = marks[i] * max;
                if (before > line && after <= line)
                    Log.Event("Hatch", $"hatch under attack — {after:0}/{max:0} ({marks[i] * 100f:0}%)");
            }
        }

        /// <summary>Routes player repairs into the sim hatch via GameController.RepairHatch
        /// (charges salvage). No-op once destroyed, the run is over, or the repair rig is not
        /// equipped. Prefer calling GameController.RepairHatch directly from interactables.</summary>
        public void ApplyRepair(float amount)
        {
            if (amount <= 0f || IsDestroyed) return;
            var gc = GameController.Instance;
            if (gc == null || gc.RunIsOver) return;
            if (gc.Meta == null || !gc.Meta.HasRepairRigEquipped) return;
            gc.RepairHatch(amount);
        }
    }
}
