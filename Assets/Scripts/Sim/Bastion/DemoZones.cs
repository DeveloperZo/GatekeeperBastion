namespace GatekeepersProtocol.Sim
{
    /// <summary>The demo's authored zone table: which Field terminals belong to a zone, how
    /// many Waves one trigger runs there, which 1-based field Waves the zone spans, and the
    /// door its survived Hatch-bank opens (Docs/Demo - Single/5-repeat-to-control-room.md).
    /// This is the single source of truth — <see cref="FirstTerminal"/> forwards its zone-1
    /// constants here rather than restating them, so a zone raises its own Wave count without
    /// touching the prefabs or the sketch.
    /// The table stops at <see cref="ZoneCount"/> on purpose: the Master Control run and the
    /// Hatch finale are their own beats and are not zones with terminals, doors and field
    /// Waves (Docs/Demo - Single/6-master-control.md, Docs/Demo - Single/7-hatch-finale.md).</summary>
    public static class DemoZones
    {
        /// <summary>Authored demo zones, 1-based. The finale is the two beats after the last
        /// zone and is deliberately absent from this table.</summary>
        public const int ZoneCount = 2;

        // Zone tables, indexed zone-1. One allocation at type init, never per frame.
        // Invariant: the last zone's end Wave IS WaveDirector.MaxWaves — the authored zones
        // run out exactly at the director's absolute ceiling, so every Wave this table names
        // is drawable and no zone is left half-reachable. Raising a zone's span past that
        // ceiling is a WaveDirector change, not a table change.
        static readonly string[][] TerminalIdsByZone =
        {
            new[] { "T1", "T2" },
            new[] { "T3" },
        };

        static readonly int[] WavesPerTriggerByZone = { 1, 3 };
        static readonly int[] FirstWaveByZone = { 1, 3 };
        static readonly int[] EndWaveByZone = { 2, 5 };
        static readonly string[] DoorIdByZone = { "D1", "D2" };

        static bool Authored(int zone)
        {
            return zone >= 1 && zone <= ZoneCount;
        }

        /// <summary>Field terminals in a zone, or 0 for a zone this table does not author.
        /// Indexed rather than array-valued: the interact-prompt getters that read this run
        /// every frame and must not allocate (Docs/Architecture.md, GOTCHAS).</summary>
        public static int TerminalCount(int zone)
        {
            return Authored(zone) ? TerminalIdsByZone[zone - 1].Length : 0;
        }

        /// <summary>The zone's index-th terminal id in authored order, or null when the zone
        /// or the index is out of range.</summary>
        public static string TerminalIdAt(int zone, int index)
        {
            if (!Authored(zone))
                return null;
            string[] ids = TerminalIdsByZone[zone - 1];
            return index >= 0 && index < ids.Length ? ids[index] : null;
        }

        /// <summary>Zone a Field terminal belongs to, or 0 when the id is not an authored demo
        /// terminal — the Master Control and finale terminals answer 0, and so does null.</summary>
        public static int ZoneOf(string terminalId)
        {
            if (string.IsNullOrEmpty(terminalId))
                return 0;
            for (int zone = 1; zone <= ZoneCount; zone++)
            {
                string[] ids = TerminalIdsByZone[zone - 1];
                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] == terminalId)
                        return zone;
                }
            }
            return 0;
        }

        /// <summary>Zone a 1-based field Wave belongs to. A Wave past the last authored zone
        /// reports that last zone as a FLOOR rather than 0: this answer is read by per-zone
        /// logs and by DemoZoneDeepestWave, and a zone of 0 would have a log claim a zone the
        /// Wave is not in. Reporting the deepest authored zone is the honest read — the Wave
        /// is at least that deep.</summary>
        public static int ZoneOfWave(int wave)
        {
            for (int zone = 1; zone <= ZoneCount; zone++)
            {
                if (wave <= EndWaveByZone[zone - 1])
                    return zone;
            }
            return ZoneCount;
        }

        /// <summary>The zone's first field Wave (1-based), or 0 for an unauthored zone.</summary>
        public static int FirstWave(int zone)
        {
            return Authored(zone) ? FirstWaveByZone[zone - 1] : 0;
        }

        /// <summary>The zone's last field Wave (1-based, inclusive), or 0 for an unauthored
        /// zone. The composite retrigger re-runs FirstWave..this.</summary>
        public static int EndWave(int zone)
        {
            return Authored(zone) ? EndWaveByZone[zone - 1] : 0;
        }

        /// <summary>Waves one terminal trigger runs in this zone, or 0 for an unauthored zone.
        /// Above 1 the trigger is a composite: consecutive Waves with no Prep between them.</summary>
        public static int WavesPerTrigger(int zone)
        {
            return Authored(zone) ? WavesPerTriggerByZone[zone - 1] : 0;
        }

        /// <summary>The zone's last terminal — the one that carries the composite retrigger
        /// offer, because reaching it means the zone's earlier terminals are behind you.
        /// Null for an unauthored zone.</summary>
        public static string DeepestTerminalId(int zone)
        {
            if (!Authored(zone))
                return null;
            string[] ids = TerminalIdsByZone[zone - 1];
            return ids.Length > 0 ? ids[ids.Length - 1] : null;
        }

        /// <summary>The door a survived Hatch-bank opens at the end of this zone, or null for
        /// an unauthored zone.</summary>
        public static string DoorId(int zone)
        {
            return Authored(zone) ? DoorIdByZone[zone - 1] : null;
        }

        /// <summary>True when a 1-based field Wave falls inside the zone's authored span.
        /// Unlike <see cref="ZoneOfWave"/> this does NOT floor: a Wave past the last authored
        /// zone answers false for every zone rather than being attributed to the deepest one.
        /// Use this wherever a Wave must genuinely BELONG to a zone — the floor is right for
        /// naming a zone in a log, and wrong for a gate. The Master Control terminal half and
        /// the terminals past the last authored zone both draw past the table's end, and a
        /// floored read there would credit them to the last zone.</summary>
        public static bool WaveIsIn(int zone, int wave)
        {
            if (!Authored(zone)) return false;
            return wave >= FirstWaveByZone[zone - 1] && wave <= EndWaveByZone[zone - 1];
        }

        /// <summary>Hatch-bank is illegal until every terminal of the CURRENT zone that is
        /// actually in the layout has completed this Run.
        /// The caller counts: <paramref name="terminalsPresent"/> is how many of the zone's
        /// terminals the layout holds, <paramref name="terminalsDone"/> how many of THOSE are
        /// complete. Passing counts rather than a layout or a lookup delegate is what keeps
        /// this rule scene-free — a terminal absent from the layout is simply never counted,
        /// so it cannot block.
        /// Off entirely once the demo is off: a save that has Escaped gets a free Hatch. Past
        /// the last authored zone the bank is not blocked either — the finale is its own beat
        /// and has no terminal gate here.</summary>
        public static bool HatchBankBlocked(
            bool demoActive,
            int currentZone,
            int terminalsPresent,
            int terminalsDone)
        {
            if (!demoActive)
                return false;
            if (!Authored(currentZone))
                return false;
            if (terminalsPresent <= 0)
                return false;
            return terminalsDone < terminalsPresent;
        }
    }
}
