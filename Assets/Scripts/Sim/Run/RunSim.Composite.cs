using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The zone composite retrigger: post-loss, a zone's deepest terminal re-runs
    /// the zone's Waves consecutively — all of them by default, or from an elected partial
    /// start at the zone's deepest cleared Wave. Rides the deep-start chain machinery
    /// (delivery wait + InterWaveGap, no Prep between Waves), capped by
    /// <see cref="WaveDirector.MaxWaves"/> per trigger — the cap is per terminal trigger,
    /// never per Run (Docs/Decisions.md, the demo loop).</summary>
    public sealed partial class RunSim
    {
        /// <summary>
        /// Launch Waves <paramref name="startWave"/>..<paramref name="endWave"/> (1-based,
        /// inclusive) as one consecutive assault. Only legal in Tethered+Prep with no
        /// Master Control sequence and no pending sequential draw. Waves beyond MaxWaves
        /// per trigger are refused rather than clamped — a silently shortened composite
        /// would read as the zone re-run when it was not.
        /// </summary>
        public bool TriggerCompositeDraw(int startWave, int endWave)
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "TriggerCompositeDraw ignored (phase " + Phase + ", " + Assault + ")");
                return false;
            }
            if (_masterPhase != MasterControlPhase.None || SequentialPending)
            {
                Log.Info(Tag, "TriggerCompositeDraw ignored: sequence already live");
                return false;
            }
            if (startWave < 1 || endWave < startWave || endWave > WaveDirector.MaxWaves)
            {
                Log.Info(Tag, "TriggerCompositeDraw refused — bad range " + startWave + ".." + endWave);
                return false;
            }
            if (endWave - startWave + 1 > WaveDirector.MaxWaves)
            {
                Log.Info(Tag, "TriggerCompositeDraw refused — over the per-trigger cap");
                return false;
            }

            // The elected start replaces the wave cursor; this is not a NAV deep start, so
            // _startWave stays 1 and the StartWave-gated restore paths keep their meaning.
            CurrentWaveIndex = startWave - 1;
            AllWavesCleared = false;
            _startWave = 1;
            CancelSequentialDraws();
            Mode = WaveMode.Terminal;
            _chainActive = endWave > startWave;
            _chainUntil = endWave - 1;
            _awaitingDelivery = false;
            _interWaveTimer = 0f;
            Assault = AssaultState.Assault;
            LaunchWave();
            if (_chainActive && CurrentWaveIndex <= _chainUntil)
                _awaitingDelivery = true;
            return true;
        }
    }
}
