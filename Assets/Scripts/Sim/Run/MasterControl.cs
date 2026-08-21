namespace GatekeepersProtocol.Sim
{
    /// <summary>Master Control win sequence. #12 — 5 terminal then <see cref="HatchWaves"/>
    /// Hatch-bank. Live phase and remaining counts sit on <see cref="RunSim"/>.
    /// <see cref="HatchWaves"/> is a static int so tests can still pin the multi-window
    /// machinery; production never writes it.</summary>
    public static class MasterControl
    {
        public const int TerminalWaves = 5;

        /// <summary>Hatch-bank windows in the second half. Production is 1 (one final window).
        /// Tests that pin intermediate-window no-dump set this to 5 and restore it.</summary>
        public static int HatchWaves = 1;

        /// <summary>Bodies in the finale window. One lever: a single Thief, no escorts.
        /// The 45 s drip/dump window stays Hatch-bank; the Wave-5 horde does not ride along.
        /// </summary>
        public const int FinaleEliteCount = 1;

        /// <summary>Pre-multiplier HP of that elite. Wave-5 Thief is
        /// <c>StatsFor(Thief).maxHp</c> (120) × 2.60 = 312 spawned; this is 2× that Thief
        /// line (240 × 2.60 = 624). Damage and speed stay the Thief's — HP is the one lever.
        /// </summary>
        public const float FinaleEliteHp = 240f;

        /// <summary>How much bigger the elite's BODY is than a Thief's. Presentation only — it
        /// does not touch the NavMeshAgent, so the elite paths, avoids and shoves exactly like
        /// the Thief it is, and HP stays the one mechanical lever named above.
        /// Deliberately off the agent: the Bastion navmesh is baked at agentRadius 0.5 and a
        /// Thief already carries 0.42, so scaling the footprint as well would put 0.63 m of
        /// radius on a mesh carved for 0.5 and let the boss wedge in the doorway it exists to
        /// reach.</summary>
        public const float FinaleEliteScale = 1.5f;

        /// <summary>Replace a Hatch-bank spec with the finale elite: one Thief, counts sum to
        /// 1, <see cref="WaveSpec.finaleElite"/> set so the spawner applies
        /// <see cref="FinaleEliteHp"/>. Keeps the incoming health multiplier so spawned HP
        /// stays comparable to the Wave-5 Thief it sits above.</summary>
        public static WaveSpec ApplyFinaleElite(WaveSpec spec)
        {
            spec.defaultCount = 0;
            spec.hunterCount = 0;
            spec.thiefCount = FinaleEliteCount;
            spec.enemyCount = FinaleEliteCount;
            spec.groupSize = 1;
            spec.finaleElite = true;
            return spec;
        }
    }

    /// <summary>Which half of the Master Control sequence is live.</summary>
    public enum MasterControlPhase : byte
    {
        None = 0,
        Terminal = 1,
        Hatch = 2,
    }
}
