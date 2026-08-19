namespace GatekeepersProtocol.Sim
{
    /// <summary>Zone 1's terminal rules while the demo is active: two Field terminals, one
    /// Wave per trigger, and the Hatch-bank waits on both.
    /// <see cref="WaveDirector.MaxWaves"/> caps consecutive Waves per terminal trigger, not
    /// per Run; the consecutive unit debuts in zone 2 and is not this file's to hold.
    /// Every rule here is gated on the caller's DemoActive: a save that has Escaped gets
    /// live terminals and a free Hatch (Docs/Demo/1-first-terminal.md).</summary>
    public static class FirstTerminal
    {
        /// <summary>Zone 1's first Field terminal. Hard-first on purpose: one id is fewer
        /// moving parts, and the second terminal's refuse prompt already names the rule.</summary>
        public const string Id = "T1";

        /// <summary>Zone 1's second Field terminal. Unblocked by <see cref="Id"/>'s clear.</summary>
        public const string SecondId = "T2";

        /// <summary>Waves per trigger in zone 1.</summary>
        public const int Waves = 1;

        /// <summary>Zone 1's terminal count, for the (k/2) prompt.</summary>
        public const int ZoneTerminalCount = 2;

        /// <summary>Zone 1's number, for per-zone logs and DemoZoneDeepestWave.</summary>
        public const int ZoneOneNumber = 1;

        /// <summary>Zone 1's first field Wave (1-based).</summary>
        public const int ZoneOneFirstWave = 1;

        /// <summary>Zone 1's last field Wave (1-based): two terminals, one Wave each.
        /// The composite retrigger re-runs ZoneOneFirstWave..this.</summary>
        public const int ZoneOneEndWave = ZoneTerminalCount * Waves;

        /// <summary>Zone a 1-based field Wave belongs to. Only zone 1 is authored while the
        /// demo's later zones are other beats'; deeper Waves report zone 2 as a floor so a
        /// log never claims zone 1 for a Wave past its end.</summary>
        public static int ZoneOfWave(int wave)
        {
            return wave <= ZoneOneEndWave ? ZoneOneNumber : ZoneOneNumber + 1;
        }

        /// <summary>True for a zone-1 Field id. Other ids belong to later zones, whose
        /// consecutive-Wave triggers are other beats' spec.</summary>
        public static bool IsZoneOne(string terminalId)
        {
            return terminalId == Id || terminalId == SecondId;
        }

        /// <summary>Waves a terminal trigger runs. One everywhere today; the per-zone table
        /// lives here so a later zone raises its own without touching the prefabs.</summary>
        public static int WavesFor(string terminalId)
        {
            return IsZoneOne(terminalId) ? Waves : 1;
        }

        /// <summary>Hatch-bank is illegal until BOTH zone-1 terminals have completed this
        /// Run. A terminal that is not in the layout cannot block.</summary>
        public static bool HatchBankBlocked(
            bool demoActive,
            bool firstExists,
            bool firstDone,
            bool secondExists,
            bool secondDone)
        {
            if (!demoActive)
                return false;
            if (firstExists && !firstDone)
                return true;
            return secondExists && !secondDone;
        }

        /// <summary>Other Field ids wait on the first terminal's Wave clearing. They do NOT
        /// wait on a Hatch-bank start: the zone's bank comes after both terminals, so the
        /// old bank-started clause would have deadlocked the second one.</summary>
        public static bool OtherFieldBlocked(
            string terminalId,
            bool demoActive,
            bool firstExists,
            bool firstDone)
        {
            if (!demoActive)
                return false;
            if (string.IsNullOrEmpty(terminalId) || terminalId == Id)
                return false;
            if (!firstExists)
                return false;
            return !firstDone;
        }
    }
}
