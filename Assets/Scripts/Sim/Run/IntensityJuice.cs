using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Per-Wave wager dial (intensity / juice). Parked 2026-08-30 (Zo): the player
    /// no longer antes up, selects intensity, or sees Ante on prompts — Waves run at dial 0.
    /// <see cref="Enabled"/> is the production default copied onto each <see cref="RunSim"/>.
    /// Spend-path tests set <see cref="RunSim.AnteEnabled"/>. Not session state.</summary>
    public static class Ante
    {
        public static readonly bool Enabled = false;
    }

    /// <summary>Paid intensity steps. Cost is BANKED salvage — kills bank as they land
    /// (Zo, 2026-08-25), so the carried pile is empty unless the player deliberately
    /// Withdrew. Lowering does not refund.</summary>
    public static class IntensityJuice
    {
        public const int CostPerStep = 100;
        public const int MaxLevel = TetherSiphon.MaxLevel;

        /// <summary>Prep-only. <paramref name="atTrigger"/> is the look-at gate from Game.</summary>
        public static bool Try(RunSim sim, int delta, bool atTrigger)
        {
            if (!sim.AnteEnabled)
            {
                Log.Info("RunSim", "juice refused — Ante is parked");
                return false;
            }
            if (sim.Phase != RunPhase.Tethered || sim.Assault != AssaultState.Prep)
            {
                Log.Info("RunSim", "TryJuice ignored (phase " + sim.Phase + ", " + sim.Assault + ")");
                return false;
            }
            if (!atTrigger)
            {
                Log.Info("RunSim", "juice refused — look at a terminal");
                return false;
            }
            if (delta == 0) return false;
            if (delta < 0)
            {
                int before = sim.Siphon.Level;
                sim.Siphon.Level = before + delta;
                return sim.Siphon.Level != before;
            }
            int steps = System.Math.Min(delta, MaxLevel - sim.Siphon.Level);
            if (steps < 1) return false;
            int cost = steps * CostPerStep;
            if (!sim.SpendBanked(cost))
            {
                Log.Info("RunSim", "juice refused — banked=" + sim.BankedSalvage
                    + " cost=" + cost);
                return false;
            }
            sim.JuiceSpentThisPull += cost;
            sim.Siphon.Level += steps;
            return true;
        }
    }
}
