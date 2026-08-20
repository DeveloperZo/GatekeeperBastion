using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Game
{
    /// <summary>One zone's composite-retrigger offer, resolved for a terminal the player is
    /// looking at. Default is the whole zone again; the partial election starts at the
    /// zone's deepest cleared field Wave, so at least one Wave refights before the bank
    /// re-arms (Docs/Demo - Single/4-embark-at-wave-2.md, Gaps — the whole-zone-cleared floor).</summary>
    public readonly struct DemoRetriggerOffer
    {
        public readonly bool Available;
        public readonly int Zone;
        /// <summary>Composite start: the zone's first field Wave.</summary>
        public readonly int CompositeStart;
        /// <summary>Last field Wave of the zone — both elections run through it.</summary>
        public readonly int EndWave;
        /// <summary>Elected partial start (the zone's deepest cleared Wave).</summary>
        public readonly int PartialStart;
        /// <summary>False when the partial election would equal the composite.</summary>
        public readonly bool PartialAvailable;

        public DemoRetriggerOffer(int zone, int compositeStart, int endWave, int partialStart)
        {
            Available = true;
            Zone = zone;
            CompositeStart = compositeStart;
            EndWave = endWave;
            PartialStart = partialStart;
            PartialAvailable = partialStart > compositeStart;
        }

        public static DemoRetriggerOffer None => default;

        public int WavesFrom(int start) => EndWave - start + 1;
    }

    /// <summary>The demo Run's pure retrigger rules over MetaState — engine-free on purpose
    /// so the offline harness pins the matrix. Unity routing (arming, election input, the
    /// launch) lives in GameController; this class only answers what is offered.</summary>
    public static class DemoRun
    {
        /// <summary>The offer at the zone's deepest terminal, for whichever zone the id
        /// belongs to. Offered only when the zone has cleared Waves from a PREVIOUS Run (saved
        /// deepest ahead of this Run's zone progress), the zone is incomplete this Run, and no
        /// surviving bank holds its door open — a survived bank made the retrigger moot. An id
        /// the zone table does not author is never offered, and neither is anything outside
        /// the demo.
        /// <paramref name="zoneWavesClearedThisRun"/> is a COUNT of the zone's field Waves
        /// cleared this Run; the saved deepest is an ABSOLUTE Wave number, so the count is
        /// lifted onto the zone's Wave span before the two are compared. In zone 1 the two
        /// scales coincide (its span starts at Wave 1); in a later zone they do not.</summary>
        public static DemoRetriggerOffer RetriggerOffer(
            MetaState m,
            string terminalId,
            int zoneWavesClearedThisRun,
            bool doorOpen)
        {
            if (m == null || !m.DemoActive) return DemoRetriggerOffer.None;
            int zone = DemoZones.ZoneOf(terminalId);
            if (zone == 0) return DemoRetriggerOffer.None;
            if (terminalId != DemoZones.DeepestTerminalId(zone)) return DemoRetriggerOffer.None;
            if (doorOpen) return DemoRetriggerOffer.None;

            int deepest = m.ZoneDeepest(zone);
            if (deepest <= 0) return DemoRetriggerOffer.None;
            int first = DemoZones.FirstWave(zone);
            int end = DemoZones.EndWave(zone);
            if (zoneWavesClearedThisRun >= end - first + 1)
                return DemoRetriggerOffer.None; // zone complete this Run — the bank is next
            int deepestThisRun = zoneWavesClearedThisRun <= 0
                ? 0
                : first - 1 + zoneWavesClearedThisRun;
            if (deepest <= deepestThisRun)
                return DemoRetriggerOffer.None; // first attempt in progress, nothing to resume

            int partial = deepest < end ? deepest : end;
            return new DemoRetriggerOffer(zone, first, end, partial);
        }
    }
}
