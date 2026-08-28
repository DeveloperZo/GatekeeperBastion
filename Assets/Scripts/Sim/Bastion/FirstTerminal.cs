namespace GatekeepersProtocol.Sim
{
    /// <summary>Zone 1's terminal sequencing rules while the demo is active: T1 is hard-first,
    /// the other Field id waits on its clear, and the Hatch-bank waits on both.
    /// The per-zone table itself lives in <see cref="DemoZones"/>; the constants below forward
    /// there so there is one source of truth and a later zone raises its own Wave count without
    /// this file growing to hold it.
    /// <see cref="WaveDirector.MaxWaves"/> is an absolute end-Wave ceiling for the Run — it is
    /// NOT re-keyed per terminal trigger. Zone 2's composite runs Waves 3..5 and fits under it.
    /// Every rule here is gated on the caller's DemoActive: a save that has Escaped gets
    /// live terminals and a free Hatch (Docs/Demo - Single/1-first-terminal.md).</summary>
    public static class FirstTerminal
    {
        /// <summary>Zone 1's number, for per-zone logs and DemoZoneDeepestWave. Authored here
        /// because it is the key every other member on this class looks the table up by.</summary>
        public const int ZoneOneNumber = 1;

        /// <summary>Zone 1's first Field terminal. Hard-first on purpose: one id is fewer
        /// moving parts, and the second terminal's refuse prompt already names the rule.</summary>
        public static string Id => DemoZones.TerminalIdAt(ZoneOneNumber, 0);

        /// <summary>Zone 1's second Field terminal. Unblocked by <see cref="Id"/>'s clear.</summary>
        public static string SecondId => DemoZones.TerminalIdAt(ZoneOneNumber, 1);

        /// <summary>Waves per trigger in zone 1.</summary>
        public static int Waves => DemoZones.WavesPerTrigger(ZoneOneNumber);

        /// <summary>Zone 1's terminal count, for the (k/2) prompt.</summary>
        public static int ZoneTerminalCount => DemoZones.TerminalCount(ZoneOneNumber);

        /// <summary>Zone 1's first field Wave (1-based).</summary>
        public static int ZoneOneFirstWave => DemoZones.FirstWave(ZoneOneNumber);

        /// <summary>Zone 1's last field Wave (1-based): two terminals, one Wave each.
        /// The composite retrigger re-runs ZoneOneFirstWave..this.</summary>
        public static int ZoneOneEndWave => DemoZones.EndWave(ZoneOneNumber);

        /// <summary>Forwarder to <see cref="DemoZones.ZoneOfWave"/>; callers are being migrated.</summary>
        public static int ZoneOfWave(int wave)
        {
            return DemoZones.ZoneOfWave(wave);
        }

        /// <summary>True for a zone-1 Field id. Other ids belong to later zones, whose
        /// consecutive-Wave triggers are those zones' rows in <see cref="DemoZones"/>.</summary>
        public static bool IsZoneOne(string terminalId)
        {
            return DemoZones.ZoneOf(terminalId) == ZoneOneNumber;
        }

        /// <summary>Forwarder to <see cref="DemoZones.WavesPerTrigger"/>; callers are being
        /// migrated. An id outside the authored table still answers one Wave, which is the
        /// default a terminal is configured with.</summary>
        public static int WavesFor(string terminalId)
        {
            int zone = DemoZones.ZoneOf(terminalId);
            return zone == 0 ? 1 : DemoZones.WavesPerTrigger(zone);
        }

        /// <summary>Forwarder to <see cref="DemoZones.HatchBankBlocked"/> for zone 1, keeping
        /// the per-terminal argument shape; callers are being migrated to the counted form.
        /// A terminal that is not in the layout is not counted, so it cannot block.</summary>
        public static bool HatchBankBlocked(
            bool demoActive,
            bool firstExists,
            bool firstDone,
            bool secondExists,
            bool secondDone)
        {
            int present = (firstExists ? 1 : 0) + (secondExists ? 1 : 0);
            int done = (firstExists && firstDone ? 1 : 0) + (secondExists && secondDone ? 1 : 0);
            return DemoZones.HatchBankBlocked(demoActive, ZoneOneNumber, present, done);
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
            // The spine (Zo, 2026-08-25) gives every zone ONE terminal, so zone 1 has no
            // second id to sequence and this rule has nothing to say. It must also never
            // answer for a LATER zone's terminal — that terminal is behind a locked door,
            // which is the real gate; blocking it here would be a second, invisible one.
            if (ZoneTerminalCount < 2)
                return false;
            if (string.IsNullOrEmpty(terminalId) || terminalId == Id)
                return false;
            if (!IsZoneOne(terminalId))
                return false;
            if (!firstExists)
                return false;
            return !firstDone;
        }
    }
}
