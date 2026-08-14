using System.Text;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: hold-E at the Hatch — repair when damaged (rig required),
    /// withdraw when sealed in Prep with Unbanked 0 (no rig). Hatch-bank is Prep E then T.
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
        bool _withdrawHold;
        bool _loggedRefusal;
        bool _repairRpcOpen;
        long _sessionSpent;
        long _sessionWithdrawn;
        float _sessionHp;

        static readonly WaveDirector s_preview = new WaveDirector();
        static readonly StringBuilder s_prompt = new StringBuilder(160);

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
                if (_repairRpcOpen)
                {
                    var verbs = LocalVerbs();
                    if (verbs != null) verbs.SetRepairing(false);
                    _repairRpcOpen = false;
                }
                _sessionSpent = 0;
                _sessionWithdrawn = 0;
                _sessionHp = 0f;
                _loggedRefusal = false;
                _withdrawHold = false;
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

                ReadRepairView(gc, out float hp, out float maxHp, out long bank, out bool destroyed,
                    out long unbanked, out int slots, out bool prep);
                if (destroyed) return "Hatch: DESTROYED";
                if (hp >= maxHp)
                {
                    if (prep && unbanked > 0L && (slots < 0 || slots > 0))
                    {
                        string e = Controls.DisplayOf(ControlId.Interact);
                        bool armed = gc != null && gc.IsHatchBankArmed;
                        s_prompt.Clear();
                        if (armed)
                            s_prompt.Append('[').Append(Controls.DisplayOf(ControlId.PullDraw))
                                .Append("] Hatch-bank");
                        else
                            s_prompt.Append('[').Append(Controls.DisplayOf(ControlId.DialUp))
                                .Append('/').Append(Controls.DisplayOf(ControlId.DialDown))
                                .Append("] Hatch-bank");
                        AppendStake(s_prompt, gc, unbanked);
                        s_prompt.Append("  [").Append(e).Append(armed ? "] edit" : "] select");
                        return s_prompt.ToString();
                    }
                    if (prep && unbanked == 0L && bank > 0L)
                    {
                        if (_withdrawHold || _sessionWithdrawn > 0L)
                            return $"Withdrawing {unbanked} unbanked — {bank} banked";
                        return $"Withdraw — hold E  ({bank} banked → unbanked)";
                    }
                    return "Hatch: sealed tight";
                }

                var meta = gc.Meta;
                string missing = meta != null ? meta.RepairMissingStep() : "no session";
                if (missing != null)
                    return $"Hatch damaged ({hp:0}/{maxHp:0}) — {missing}";

                float rate = ShipSystems.RepairSalvagePerHp;
                if (bank <= 0)
                    return $"Hatch damaged ({hp:0}/{maxHp:0}) — no salvage to repair";

                if (_sessionSpent > 0 || _sessionHp > 0.01f || _holding || _repairRpcOpen)
                {
                    if (_sessionSpent > 0 || _sessionHp > 0.01f)
                    {
                        return $"Repairing +{_sessionHp:0.#} HP — {_sessionSpent} salvage " +
                               $"({rate:0}/HP, {bank} banked)";
                    }
                    return $"Repairing... ({hp:0}/{maxHp:0}) — {bank} banked";
                }

                return $"Hatch damaged ({hp:0}/{maxHp:0}) — {rate:0} salvage/HP";
            }
        }

        /// <summary>Prep, sealed, Unbanked waiting, a slot left — E can select, T can start.</summary>
        public bool OffersHatchBank
        {
            get
            {
                var gc = _game;
                if (gc == null || gc.RunIsOver) return false;
                ReadRepairView(gc, out float hp, out float maxHp, out _, out bool destroyed,
                    out long unbanked, out int slots, out bool prep);
                return !destroyed && hp >= maxHp - 0.01f && prep && unbanked > 0L
                    && (slots < 0 || slots > 0);
            }
        }

        /// <summary>Per-frame while E is held. Sim mutation goes through GameController on the
        /// host; a client opens the repair channel instead.</summary>
        public void Interact(float dt)
        {
            if (dt <= 0f || station == null) return;
            var gc = _game;
            if (gc == null || gc.RunIsOver) return;

            _holding = true;

            ReadRepairView(gc, out float hp, out float maxHp, out long bank, out bool destroyed,
                out long unbanked, out _, out bool prep);
            if (destroyed) return;
            if (hp >= maxHp - 0.01f)
            {
                bool canStart = prep && unbanked == 0L && bank > 0L;
                if (!_withdrawHold && !canStart) return;
                _withdrawHold = true;
                HoldWithdraw(gc, dt);
                return;
            }

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

            if (IsClientOnly(gc))
            {
                if (!_repairRpcOpen)
                {
                    var verbs = LocalVerbs();
                    if (verbs != null)
                    {
                        verbs.SetRepairing(true);
                        _repairRpcOpen = true;
                    }
                }
                return;
            }

            if (gc.Sim == null) return;
            long before = gc.Sim.RepairSalvageSpent;
            float applied = gc.RepairHatch(repairPerSecond * dt);
            _sessionSpent += gc.Sim.RepairSalvageSpent - before;
            _sessionHp += applied;
        }

        void HoldWithdraw(GameController gc, float dt)
        {
            float rate = EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond;
            if (IsClientOnly(gc))
            {
                if (!_repairRpcOpen)
                {
                    var verbs = LocalVerbs();
                    if (verbs != null)
                    {
                        verbs.SetRepairing(true);
                        _repairRpcOpen = true;
                    }
                }
                return;
            }

            if (gc.Sim == null) return;
            long before = gc.Sim.Unbanked;
            gc.RepairHatch(rate * dt);
            _sessionWithdrawn += gc.Sim.Unbanked - before;
        }

        static bool IsClientOnly(GameController gc)
        {
            var net = gc.Net;
            return net != null && net.IsClientOnly;
        }

        NetVerbs LocalVerbs()
        {
            var gc = _game;
            if (gc == null || gc.Net == null) return null;
            return gc.Net.LocalVerbs;
        }

        void ReadRepairView(GameController gc, out float hp, out float maxHp, out long bank,
            out bool destroyed, out long unbanked, out int slots, out bool prep)
        {
            var net = gc.Net;
            var mirror = net != null && net.IsClientOnly ? net.Mirror : null;
            if (mirror != null)
            {
                hp = mirror.HatchHp.Value;
                maxHp = mirror.HatchMax.Value;
                bank = mirror.Banked.Value;
                destroyed = hp <= 0f;
                unbanked = mirror.Unbanked.Value;
                slots = -1;
                prep = (AssaultState)mirror.Phase.Value == AssaultState.Prep;
                return;
            }

            hp = station.Hp;
            maxHp = station.MaxHp;
            bank = gc.Meta != null ? gc.Meta.BankedSalvage : 0L;
            destroyed = station.IsDestroyed;
            unbanked = gc.Sim != null ? gc.Sim.Unbanked : 0L;
            slots = gc.Sim != null ? gc.Sim.HatchBankSlotsLeft : 0;
            prep = gc.Sim != null && gc.Sim.Assault == AssaultState.Prep;
        }

        static void AppendStake(StringBuilder sb, GameController gc, long unbanked)
        {
            int level = 0;
            int bodies = 0;
            if (gc != null && gc.Sim != null)
            {
                var sim = gc.Sim;
                level = sim.Siphon.Level;
                int carry = sim.Mode == WaveMode.HatchBank && sim.Assault == AssaultState.Assault
                    ? sim.HatchCarry
                    : HatchBank.ClampCarry(sim.WavesSinceBank);
                int idx = HatchBank.CarryWaveIndex(carry);
                bodies = s_preview.NextWave(idx, level).enemyCount;
                sb.Append("  Intensity ").Append(level)
                    .Append("  juice ").Append(IntensityJuice.CostPerStep)
                    .Append("  unbanked ").Append(unbanked)
                    .Append("  carry ").Append(carry)
                    .Append("  ×").Append(HatchBank.CarryConvert(carry).ToString("0.00"))
                    .Append("  ").Append(bodies);
                return;
            }
            sb.Append("  Intensity ").Append(level)
                .Append("  juice ").Append(IntensityJuice.CostPerStep)
                .Append("  unbanked ").Append(unbanked)
                .Append("  ").Append(bodies);
        }
    }
}
