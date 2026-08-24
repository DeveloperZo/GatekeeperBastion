using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Value describing one gatekeeper wave: how many enemies OF EACH KIND, how tough, how big a
    /// spawn group is, how fast groups arrive, and the delay until the wave after this one.
    /// </summary>
    [Serializable]
    public struct WaveSpec
    {
        /// <summary>Total gatekeepers in this wave. Always equals
        /// <see cref="defaultCount"/> + <see cref="hunterCount"/> + <see cref="thiefCount"/> +
        /// <see cref="cloakerCount"/>.</summary>
        public int enemyCount;

        /// <summary>How many of <see cref="enemyCount"/> are <see cref="GatekeeperKind.Default"/>.
        /// The siphon dial's surplus lands here and nowhere else.</summary>
        public int defaultCount;

        /// <summary>How many of <see cref="enemyCount"/> are <see cref="GatekeeperKind.Hunter"/>.
        /// Fixed per wave; the dial never multiplies it.</summary>
        public int hunterCount;

        /// <summary>How many of <see cref="enemyCount"/> are <see cref="GatekeeperKind.Thief"/>.
        /// Fixed per wave; the dial never multiplies it.</summary>
        public int thiefCount;

        /// <summary>How many of <see cref="enemyCount"/> are <see cref="GatekeeperKind.Cloaker"/>.
        /// Fixed per wave; the dial never multiplies it.</summary>
        public int cloakerCount;

        /// <summary>Master Control's final Hatch-bank window: one Thief at
        /// <see cref="MasterControl.FinaleEliteHp"/>, no escorts. The counts still sum to
        /// <see cref="enemyCount"/>; the flag tells the spawner which Thief wears the HP.</summary>
        public bool finaleElite;

        /// <summary>Multiplier applied to each gatekeeper's kind-specific
        /// <see cref="GatekeeperStats.maxHp"/> (1 = baseline). One multiplier for all three kinds.</summary>
        public float enemyHealthMultiplier;

        /// <summary>How many gatekeepers spawn together in one group. 1 = the old one-at-a-time
        /// behaviour.</summary>
        public int groupSize;

        /// <summary>
        /// SECONDS BETWEEN GROUPS — NOT BETWEEN INDIVIDUALS. The name predates the meaning.
        ///
        /// A wave arrives as ceil(enemyCount / groupSize) groups, the members of a group
        /// spawning together and the next group arriving spawnInterval seconds later, so
        /// delivery takes (ceil(enemyCount / groupSize) - 1) * spawnInterval seconds.
        ///
        /// A spawn loop that treats this as a per-individual gap delivers the wave three to five
        /// times too slowly. This is the field to check first if a wave feels empty.
        /// </summary>
        public float spawnInterval;

        /// <summary>OBSOLETE under the draw model: waves start only via RunSim.TriggerDraw, so
        /// there is no wave timer. Kept for struct compatibility; NextWave always writes 0.
        /// (SpoolAssault, dormant, still fills it for the future escape-window design.)</summary>
        public float timeUntilNextWave;
    }
}
