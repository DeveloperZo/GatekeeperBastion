using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: hold-E repair point at the Hatch — requests
    /// repairPerSecond * dt from GameController.RepairHatch each frame while the interact key
    /// is held, only when the repair rig is equipped. Healing and salvage debit live in the
    /// sim; this only drives the command and surfaces the rate / session cost / missing-step
    /// prompt.</summary>
    /// <remarks>Every access to the station is Unity-null-safe: the hatch object can be destroyed
    /// (scene teardown once the run ends) while the Interactor still holds this interactable from
    /// its last raycast. Prefab serializes repairPerSecond = 8; GameController always pushes
    /// equipped state at run start so a rigless embark cannot silently keep that default.</remarks>
    public sealed class RepairInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Hatch to repair. Wired by SceneBuilder; falls back to a sibling component.")]
        public HatchStation station;

        [SerializeField, Tooltip("Bastion GameController. Authored in the scene.")]
        GameController _game;

        [SerializeField] float repairPerSecond = 8f;

        bool _equipped;
        bool _holding;
        bool _loggedRefusal;
        long _sessionSpent;
        float _sessionHp;

        /// <summary>Apply the equipment repair rate and whether the rig is equipped.
        /// Always called from GameController — both args, every run (Architecture gotcha 1).</summary>
        public void Configure(float newRepairPerSecond, bool equipped)
        {
            _equipped = equipped;
            repairPerSecond = equipped ? Mathf.Max(0f, newRepairPerSecond) : 0f;
        }

        void Awake()
        {
            if (station == null) station = GetComponent<HatchStation>();
        }

        void LateUpdate()
        {
            // Interact runs in Update via Interactor; if we were not held this frame, the session ends.
            if (!_holding)
            {
                _sessionSpent = 0;
                _sessionHp = 0f;
                _loggedRefusal = false;
            }
            _holding = false;
        }

        public string Prompt
        {
            get
            {
                if (station == null) station = GetComponent<HatchStation>(); // re-resolve if fake-null
                if (station == null) return null;
                var gc = _game;
                if (gc == null || gc.RunIsOver) return null; // no repair prompt once the run is over
                if (station.IsDestroyed) return "Hatch: DESTROYED";
                if (station.Hp >= station.MaxHp) return "Hatch: sealed tight";

                var meta = gc.Meta;
                string missing = meta != null ? meta.RepairMissingStep() : "no session";
                if (missing != null)
                    return $"Hatch damaged ({station.Hp:0}/{station.MaxHp:0}) — {missing}";

                long bank = meta != null ? meta.BankedSalvage : 0L;
                float rate = ShipSystems.RepairSalvagePerHp;
                if (bank <= 0)
                    return $"Hatch damaged ({station.Hp:0}/{station.MaxHp:0}) — no salvage to repair";

                if (_sessionSpent > 0 || _sessionHp > 0.01f)
                {
                    return $"Repairing +{_sessionHp:0.#} HP — {_sessionSpent} salvage " +
                           $"({rate:0}/HP, {bank} banked)";
                }

                return $"Hatch damaged ({station.Hp:0}/{station.MaxHp:0}) — {rate:0} salvage/HP";
            }
        }

        /// <summary>Per-frame while E is held. Sim mutation goes through GameController.</summary>
        public void Interact(float dt)
        {
            if (dt <= 0f || station == null) return;
            var gc = _game;
            if (gc == null || gc.RunIsOver || gc.Sim == null) return;

            _holding = true;

            var meta = gc.Meta;
            string missing = meta != null ? meta.RepairMissingStep() : "no session";
            if (missing != null || !_equipped || repairPerSecond <= 0f)
            {
                if (!_loggedRefusal)
                {
                    Log.Event("Hatch", $"repair refused — {(missing != null ? missing : "repair rig not equipped")}");
                    _loggedRefusal = true;
                }
                return;
            }

            long before = gc.Sim.RepairSalvageSpent;
            float applied = gc.RepairHatch(repairPerSecond * dt);
            _sessionSpent += gc.Sim.RepairSalvageSpent - before;
            _sessionHp += applied;
        }
    }
}
