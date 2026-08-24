namespace GatekeepersProtocol.Sim
{
    /// <summary>Respawn rules. One rule prices every dead seat's way back into a live run —
    /// during a wave while the shared pool has charges, at any wave break without limit —
    /// replacing the old Hatch-bank-window pair (free Prep embark, flat buy-back). RunSim
    /// owns the live stock and counter and calls these. Numbers are #12 constants — retune
    /// here and the 4-Data respawns block together (Docs/Milestones — drop in between
    /// waves).</summary>
    public static class Respawns
    {
        /// <summary>In-wave respawn charges per seat, seeded as the run becomes Tethered
        /// and grown per seat admitted mid-run. The pool limits WHEN a respawn may happen
        /// (mid-Assault), never how many break respawns a run may buy.</summary>
        public const int PoolPerSeat = 1;

        /// <summary>First respawn of a run. Every later one climbs — see
        /// <see cref="CostFor"/>. One Secondary MK-1 purchase, so the first death is
        /// affordable off one banked wave and the third is not casual.</summary>
        public const long BaseCost = 150L;

        /// <summary>Cost of the next respawn after <paramref name="taken"/> have been
        /// taken this run: 150 / 300 / 450 / … The counter is the run's, not the seat's —
        /// the pot is shared, so a repeat death raises the price for everyone.</summary>
        public static long CostFor(int taken)
        {
            if (taken < 0) taken = 0;
            return BaseCost * (taken + 1L);
        }

        /// <summary>Null when a respawn may be taken now. Named refuse otherwise, cheapest
        /// disqualifier first: outside a run nothing applies, mid-Assault the pool gates,
        /// and the realm's pot pays or nobody does.</summary>
        public static string Refuse(RunPhase phase, AssaultState assault, int stock,
            long cost, long pot)
        {
            if (phase != RunPhase.Tethered) return "no run";
            if (assault == AssaultState.Assault && stock <= 0)
                return "no charges until the break";
            if (pot < cost) return "cannot afford";
            return null;
        }
    }
}
