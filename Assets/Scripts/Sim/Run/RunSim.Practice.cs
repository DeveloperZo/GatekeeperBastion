using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The practice rule: a Run with nothing at stake. The range console enters
    /// it once when the lane opens; nothing saves it and nothing leaves it. Under it a kill
    /// pays nothing, the Hatch cannot be hurt and so cannot lose the Run, and a wave is drawn
    /// one at a time from the range board — one kind at one size through the same director,
    /// cadence and spawner a Boarding uses, so the beat that lands on the lane is a Boarding's.
    /// Split from RunSim.cs like Composite and Master: same class, one concern per file.</summary>
    public sealed partial class RunSim
    {
        /// <summary>True for the life of a range session. Read by <see cref="CurrentKillAward"/>,
        /// <see cref="DamageHatch"/> and the Game layer's shutoff owe.</summary>
        public bool PracticeMode { get; private set; }

        /// <summary>The roster the next launch writes over wave 1's own: a count above zero for
        /// the duration of one <see cref="DrawPracticeWave"/>, consumed by LaunchWave the way
        /// the Master terminal's shaping is, then zero.</summary>
        public int PracticeCount { get; private set; }

        public GatekeeperKind PracticeKind { get; private set; }

        /// <summary>Enter the practice rule. Once: there is no way back out, because a range
        /// that could turn into a Boarding mid-session would have a stake nobody chose.</summary>
        public void EnterPracticeMode()
        {
            if (PracticeMode) return;
            PracticeMode = true;
            Log.Event(Tag, "practice rule on — kills pay nothing, the Hatch cannot be lost, "
                + "waves draw from the range board");
        }

        /// <summary>The range board's draw: <paramref name="size"/> bodies of
        /// <paramref name="kind"/> as a single-wave assault. Legal only under the practice
        /// rule, Tethered, in Prep. Draws wave 1 through <see cref="TriggerCompositeDraw"/> —
        /// wave 1's group width, mean gap and health multiplier, the kind's own numbers — with
        /// the roster rewritten to one kind at the size, so nothing chains after it; the drip
        /// and the armed switch are dropped first, because a fresh draw owns the lane.</summary>
        public bool DrawPracticeWave(GatekeeperKind kind, PracticeSize size)
        {
            if (!PracticeMode)
            {
                Log.Info(Tag, "practice draw refused — not a practice run");
                return false;
            }
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "practice draw refused (phase " + Phase + ", " + Assault + ")");
                return false;
            }

            int count = PracticeWave.Count(size);
            ResetFieldHold();
            PracticeCount = count;
            PracticeKind = kind;
            bool drawn = TriggerCompositeDraw(1, 1);
            PracticeCount = 0;
            if (!drawn)
            {
                Log.Info(Tag, "practice draw refused — the draw did not start");
                return false;
            }
            Log.Event(Tag, "practice wave drawn — " + count + " " + kind + " ("
                + PracticeWave.Name(size) + ")");
            return true;
        }

        /// <summary>LaunchWave's arm, after the Master shaping: the roster becomes one kind at
        /// the practice count; the group width, the mean gap and the health multiplier stay
        /// wave 1's. No-op when nothing is pending.</summary>
        WaveSpec ShapePracticeWave(WaveSpec spec, int waveIndex)
        {
            if (PracticeCount <= 0) return spec;
            spec.enemyCount = PracticeCount;
            spec.defaultCount = 0;
            spec.hunterCount = 0;
            spec.thiefCount = 0;
            spec.cloakerCount = 0;
            switch (PracticeKind)
            {
                case GatekeeperKind.Hunter: spec.hunterCount = PracticeCount; break;
                case GatekeeperKind.Thief: spec.thiefCount = PracticeCount; break;
                case GatekeeperKind.Cloaker: spec.cloakerCount = PracticeCount; break;
                default: spec.defaultCount = PracticeCount; break;
            }
            spec.finaleElite = false;
            return spec;
        }
    }
}
