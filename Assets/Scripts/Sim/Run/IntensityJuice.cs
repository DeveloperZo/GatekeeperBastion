using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Paid intensity steps. Cost is Unbanked; lowering does not refund.</summary>
    public static class IntensityJuice
    {
        public const int CostPerStep = 100;
        public const int MaxLevel = TetherSiphon.MaxLevel;

        /// <summary>Prep-only. <paramref name="atTrigger"/> is the look-at gate from Game.</summary>
        public static bool Try(RunSim sim, int delta, bool atTrigger)
        {
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
            if (sim.Unbanked < cost)
            {
                Log.Info("RunSim", "juice refused — unbanked=" + sim.Unbanked + " cost=" + cost);
                return false;
            }
            sim.Unbanked -= cost;
            sim.JuiceSpentThisPull += cost;
            sim.Siphon.Level += steps;
            return true;
        }
    }
}
