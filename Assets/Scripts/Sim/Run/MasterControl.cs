namespace GatekeepersProtocol.Sim
{
    /// <summary>Master Control win sequence: <see cref="TerminalWaves"/> control-room waves,
    /// then the finale elite as one more Terminal wave. Killing the elite ARMS the Master
    /// terminal; throwing that switch is what ends the Run (RunSim.ShutOffMasterControl).
    /// Live phase and remaining counts sit on <see cref="RunSim"/>.
    /// <para>2026-08-28: the sequence used to end in a Hatch-bank window back at the door.
    /// It ends at a terminal now, like every other wave in the game.
    /// <see cref="HatchWaves"/> is no longer read by the finale and survives only for the
    /// Hatch-bank fixtures that pin multi-window Convert machinery.</para></summary>
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
            spec.cloakerCount = 0;
            spec.thiefCount = FinaleEliteCount;
            spec.enemyCount = FinaleEliteCount;
            spec.groupSize = 1;
            spec.finaleElite = true;
            return spec;
        }
    }

    /// <summary>Which half of the Master Control sequence is live.
    /// <para>2026-08-28: <c>Hatch</c> became <c>Elite</c>. The finale used to end with a
    /// Hatch-bank window back at the door; it now ends where every other wave in the game
    /// ends — at a terminal — with the elite standing between the player and the switch.
    /// The member kept its number so an in-flight save reads the same slot.</para></summary>
    public enum MasterControlPhase : byte
    {
        None = 0,
        Terminal = 1,

        /// <summary>The elite is on the floor. THE SEQUENCE DOES NOT END ON ITS OWN from here:
        /// killing it only arms the Master terminal, and the run ends when that is shut off.
        /// </summary>
        Elite = 2,
    }
}
