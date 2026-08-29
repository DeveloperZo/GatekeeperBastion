namespace GatekeepersProtocol.Sim
{
    /// <summary>The availability rule for the zone-terminal shutoff verb — the T that ENDS a
    /// field wave (a wave begins at the Hatch and ends at a terminal). One pure predicate,
    /// called by the prompt and by anything gating the press, so the offer and the gate
    /// cannot disagree.</summary>
    /// <remarks>THERE IS DELIBERATELY NO AssaultState PARAMETER. The shutoff is available in
    /// Assault AND in Prep: pressed mid-wave it is the off switch; pressed after the field
    /// was killed empty (which drops the sim to Prep) it still completes the zone, because
    /// the walk is the objective. The 2026-08-28 bug was not this rule but a tick gate one
    /// layer up — `if (Assault != Prep) return;` sitting ABOVE the handler whose own comment
    /// claimed both states — which dead-ended the verb silently during a live wave. Anything
    /// routing input to the shutoff must consult this predicate, never a phase guard of its
    /// own.</remarks>
    public static class FieldShutoff
    {
        /// <summary>Has the terminal's zone been run at? The shutoff ends a wave that began
        /// at the Hatch, so it may not be pressed on a zone nobody has started — that would
        /// let a player skip the lane entirely. An id off the zone table (Master Control,
        /// finale, null) has no zone rule to enforce and answers true.</summary>
        public static bool WavesHaveRun(string terminalId, int currentWaveIndex)
        {
            int zone = DemoZones.ZoneOf(terminalId);
            if (zone <= 0) return true;
            return currentWaveIndex >= DemoZones.FirstWave(zone);
        }

        /// <summary>Is the shutoff on offer at this terminal right now? Callers supply the
        /// one scene-owned fact (<paramref name="terminalDone"/>, from BastionLayout) as a
        /// plain value; everything else is Run state.</summary>
        public static bool MayRun(RunPhase phase, bool runIsOver, WaveMode mode,
            string terminalId, bool terminalDone, int currentWaveIndex)
        {
            if (runIsOver || phase != RunPhase.Tethered) return false;
            if (mode == WaveMode.HatchBank) return false;
            if (DemoZones.ZoneOf(terminalId) <= 0) return false;
            if (terminalDone) return false;
            return WavesHaveRun(terminalId, currentWaveIndex);
        }
    }
}
