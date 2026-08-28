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

        /// <summary>Steps the price climbs before it stops climbing. The escalation exists so
        /// the third death is not casual; past that it was only deciding how much of a lost run
        /// to erase, which is a different job and not one this rule should be doing.</summary>
        public const int MaxSteps = 3;

        /// <summary>Cost of the next respawn after <paramref name="taken"/> have been
        /// taken this run: 150 / 300 / 450 / 450 / … The counter is the run's, not the seat's,
        /// so a party that keeps dying prices itself out together.
        /// <para>THE CLIMB IS CAPPED AT <see cref="MaxSteps"/> (Zo, 2026-08-27: "i also kept
        /// dying and respawning which drained resources"). Uncapped it read 150/300/450/600/
        /// 750/900, and the 03:18 playthrough paid 3150 across six deaths against a gross of
        /// 3750 — 84% of everything the run earned, on a run that then ended lost. That is the
        /// standing ruling inverted: it should be impossible to die with no progress to show
        /// for it, and an unbounded curve guarantees the opposite for exactly the player who
        /// is already having the worst time. Capped, the same six deaths cost 2250.</para>
        /// <para>The cap does NOT make dying cheap — the third still costs 3x the first, which
        /// is the whole of what the escalation was for. It stops the fee outrunning the run.
        /// </para></summary>
        public static long CostFor(int taken)
        {
            if (taken < 0) taken = 0;
            int step = taken + 1;
            if (step > MaxSteps) step = MaxSteps;
            return BaseCost * step;
        }

        /// <summary>Null when the dead seat may buy its way back now. Named refuse
        /// otherwise, cheapest disqualifier first: outside a run nothing applies, and
        /// otherwise the purse either covers the price or the run is over.
        /// <paramref name="purse"/> is the REALM'S BANK (Zo, 2026-08-25) — kills bank as they
        /// land, so the carried pile the fee used to come out of no longer fills on its own.
        /// The refusal string is shown to the player, so it must name the purse it read.</summary>
        public static string Refuse(RunPhase phase, long cost, long purse)
        {
            if (phase != RunPhase.Tethered) return "no run";
            if (purse < cost) return "not enough banked";
            return null;
        }
    }
}
