namespace GatekeepersProtocol.Sim
{
    /// <summary>How big a practice wave is. Three settings on one key (Zo, 2026-09-03:
    /// "Wave Size - X, small, normal, large").</summary>
    public enum PracticeSize
    {
        Small = 0,
        Normal = 1,
        Large = 2,
    }

    /// <summary>The practice wave's numbers. A range wave is one kind, one size, breaching at
    /// one distance — the dimensions Zo named on 2026-09-03 — through the same director,
    /// cadence and spawner a Boarding uses, so the beat is a Boarding's and only the roster
    /// is the range's.</summary>
    public static class PracticeWave
    {
        /// <summary>One body: the time-to-kill read on a single Gatekeeper, which is what the
        /// range was built to answer (M99).</summary>
        public const int SmallCount = 1;

        /// <summary>The Bastion's wave 1 — <c>11 + 7i</c> at i = 0 — so "normal" on the range
        /// is the normal a Boarding opens with.</summary>
        public const int NormalCount = 11;

        /// <summary>Wave 3's roster, <c>11 + 7i</c> at i = 2 — the wave the shape table first
        /// stands a Thief on, and the largest a lane 20 m deep can answer without becoming a
        /// wall.</summary>
        public const int LargeCount = 25;

        /// <summary>Metres from the firing line a practice wave breaches at when nothing has
        /// been chosen. Zo, 2026-09-03: "Distance - default to 20 m" — a shooting range, not a
        /// walk; the first cut's closets 90 m down the lane put the fight a minute away from
        /// the press.</summary>
        public const int DefaultMetres = 20;

        /// <summary>The marks a wave can breach at, nearest first: the range figures of the
        /// three Primaries (30, 60, 90), two short marks (5, 14) and the default. Each mark
        /// is a post pair on the lane and a spawn point the distance terminal cycles through
        /// (Zo, 2026-09-03: "We need a dimension for distance. This should be triggered at a
        /// terminal").</summary>
        public static readonly int[] Distances = { 5, 14, 20, 30, 60, 90 };

        public static int Count(PracticeSize size)
        {
            switch (size)
            {
                case PracticeSize.Small: return SmallCount;
                case PracticeSize.Large: return LargeCount;
                default: return NormalCount;
            }
        }

        public static string Name(PracticeSize size)
        {
            switch (size)
            {
                case PracticeSize.Small: return "small";
                case PracticeSize.Large: return "large";
                default: return "normal";
            }
        }

        public static PracticeSize Next(PracticeSize size)
        {
            return (PracticeSize)(((int)size + 1) % 3);
        }
    }
}
