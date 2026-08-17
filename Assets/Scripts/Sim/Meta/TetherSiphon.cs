namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The risk/reward siphon dial. Level 0..5; the reward multiplier is a HAND-AUTHORED
    /// five-step curve topping out at +50% (see <see cref="Curve"/>), NOT a formula.
    /// </summary>
    public sealed class TetherSiphon
    {
        /// <summary>Lowest dial setting (siphon idle, x1 reward).</summary>
        public const int MinLevel = 0;

        /// <summary>Highest dial setting (x1.5 reward).</summary>
        public const int MaxLevel = 5;

        /// <summary>
        /// THE DIAL — A HAND-AUTHORED CURVE, NOT A FORMULA. DO NOT "SIMPLIFY" IT INTO ONE: the
        /// steps are deliberately uneven and no closed form reproduces them. Array index is the
        /// dial level. WaveDirector.NextWave sizes waves off this same table, so payout and body
        /// count cannot drift apart. Authored values and their rationale: Docs/Design/4-Data.md.
        /// </summary>
        private static readonly float[] Curve = { 1.00f, 1.10f, 1.15f, 1.25f, 1.35f, 1.50f };

        private int _level;

        /// <summary>Current dial level. Assignment clamps into [MinLevel, MaxLevel].</summary>
        public int Level
        {
            get { return _level; }
            set { _level = value < MinLevel ? MinLevel : (value > MaxLevel ? MaxLevel : value); }
        }

        /// <summary>
        /// The dial's multiplier at an arbitrary level (clamped to [MinLevel, MaxLevel]).
        /// Static so WaveDirector can size a wave off the SAME table the reward comes from —
        /// this is the single source both sides read.
        /// </summary>
        public static float RewardMultiplierFor(int level)
        {
            if (level < MinLevel) level = MinLevel;
            if (level > MaxLevel) level = MaxLevel;
            return Curve[level];
        }

        /// <summary>Reward multiplier at the current level: 1, 1.1, 1.15, 1.25, 1.35, 1.5.</summary>
        public float RewardMultiplier
        {
            get { return RewardMultiplierFor(_level); }
        }

    }
}
