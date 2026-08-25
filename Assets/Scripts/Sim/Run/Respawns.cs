namespace GatekeepersProtocol.Sim
{
    /// <summary>Respawn rules. One rule prices every dead seat's way back into a live run:
    /// the seat pays out of the Unbanked it is carrying, and whatever is left over survives
    /// the death with it. Nothing else can pay — not the realm's bank, not another seat.
    /// <para>The carry IS the ration, which is why there is no charge pool: a seat that has
    /// been killing can stand up, a seat that just banked everything cannot, and the choice
    /// of when to bank is therefore also a choice about how mortal to be. RunSim owns the
    /// counter and calls these. Numbers are #12 constants — retune here and the 4-Data
    /// respawns block together (Docs/Milestones — drop in between waves).</para></summary>
    public static class Respawns
    {
        /// <summary>First respawn of a run. Every later one climbs — see
        /// <see cref="CostFor"/>. One Secondary MK-1 purchase, so the first death is
        /// affordable off one good push and the third is not casual.</summary>
        public const long BaseCost = 150L;

        /// <summary>Cost of the next respawn after <paramref name="taken"/> have been
        /// taken this run: 150 / 300 / 450 / … The counter is the run's, not the seat's, so
        /// a party that keeps dying prices itself out together.</summary>
        public static long CostFor(int taken)
        {
            if (taken < 0) taken = 0;
            return BaseCost * (taken + 1L);
        }

        /// <summary>Null when the dead seat may buy its way back now. Named refuse
        /// otherwise, cheapest disqualifier first: outside a run nothing applies, and
        /// otherwise the seat's own carry either covers the price or the run is over.
        /// <paramref name="carry"/> is that seat's Unbanked, never the realm's bank.</summary>
        public static string Refuse(RunPhase phase, long cost, long carry)
        {
            if (phase != RunPhase.Tethered) return "no run";
            if (carry < cost) return "not enough unbanked";
            return null;
        }
    }
}
