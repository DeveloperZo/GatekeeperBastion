using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The practice rule: a Run with nothing at stake. The range console enters
    /// it once when the lane opens; nothing saves it and nothing leaves it. Under it a kill
    /// pays nothing, the Hatch cannot be hurt and so cannot lose the Run, and a wave is drawn
    /// one at a time from the range's own terminal — through the same director, cadence and
    /// shapes a Boarding uses, so what lands on the lane is the wave the Bastion would send.
    /// Split from RunSim.cs like Composite and Master: same class, one concern per file.</summary>
    public sealed partial class RunSim
    {
        /// <summary>True for the life of a range session. Read by <see cref="CurrentKillAward"/>,
        /// <see cref="DamageHatch"/> and the Game layer's shutoff owe.</summary>
        public bool PracticeMode { get; private set; }

        /// <summary>The shape the next launch writes over the wave's own, or null for the wave's
        /// own. Set for the duration of one <see cref="DrawPracticeWave"/> and consumed by
        /// LaunchWave the way the Master terminal's shaping is.</summary>
        public WaveShape? ShapeOverride { get; private set; }

        /// <summary>Enter the practice rule. Once: there is no way back out, because a range
        /// that could turn into a Boarding mid-session would have a stake nobody chose.</summary>
        public void EnterPracticeMode()
        {
            if (PracticeMode) return;
            PracticeMode = true;
            Log.Event(Tag, "practice rule on — kills pay nothing, the Hatch cannot be lost, "
                + "waves draw at the range terminal");
        }

        /// <summary>The range terminal's draw: 1-based <paramref name="wave"/> as a single-wave
        /// assault, in <paramref name="shape"/> or the wave's own when null. Legal only under
        /// the practice rule, Tethered, in Prep. Seeks the wave and draws it through
        /// <see cref="TriggerCompositeDraw"/> so CurrentWaveIndex reads as the wave drawn and
        /// nothing chains after it; the drip and the armed switch are dropped first, because a
        /// fresh draw owns the lane.</summary>
        public bool DrawPracticeWave(int wave, WaveShape? shape)
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
            if (wave < 1 || wave > WaveDirector.MaxWaves)
            {
                Log.Info(Tag, "practice draw refused — wave " + wave + " is outside 1.."
                    + WaveDirector.MaxWaves);
                return false;
            }

            ResetFieldHold();
            ShapeOverride = shape;
            bool drawn = TriggerCompositeDraw(wave, wave);
            ShapeOverride = null;
            if (!drawn)
            {
                Log.Info(Tag, "practice draw refused — the draw did not start");
                return false;
            }
            Log.Event(Tag, "practice wave " + wave + " drawn — "
                + (shape.HasValue
                    ? WaveShapes.Name(shape.Value)
                    : "own shape, " + WaveShapes.Name(WaveShapes.For(wave))));
            return true;
        }

        /// <summary>LaunchWave's arm, after the Master shaping: the override rewrites the four
        /// counts from the same roster index, so the size and the health multiplier are the
        /// wave's own. No-op when nothing is overriding.</summary>
        WaveSpec ShapePracticeWave(WaveSpec spec, int waveIndex)
        {
            if (!ShapeOverride.HasValue) return spec;
            return WaveShapes.Apply(spec, ShapeOverride.Value, waveIndex);
        }
    }
}
