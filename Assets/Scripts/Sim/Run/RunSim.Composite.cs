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
        // ------------------------------------------------------------------
        //  THE LAST WAVE BELONGS TO THE TERMINAL (Zo, 2026-09-02; M115). The Hatch draws a
        //  zone's opening span; the terminal press draws the zone's last wave into the live
        //  field; when that wave is down the terminal ARMS, and the shutoff closes the zone.
        //  The finale's grammar (kill the elite arms the switch) applied to every zone.
        // ------------------------------------------------------------------

        /// <summary>Roster of the zone's LAST wave spent and its field empty, so the terminal
        /// is armed for the shutoff. Raised by the spawner's ArmShutoff verdict; cleared at
        /// every Assault exit like the trickle flag.</summary>
        public bool FieldShutoffArmed { get; private set; }

        /// <summary>Is the current field wave its zone's last? False off the zone table (the
        /// finale, an unauthored Wave), in a Convert window, and before any wave was drawn.</summary>
        public bool ZoneFinalWaveDrawn
        {
            get
            {
                if (Mode != WaveMode.Terminal || _masterPhase != MasterControlPhase.None) return false;
                if (CurrentWaveIndex < 1) return false;
                int zone = DemoZones.ZoneOfWave(CurrentWaveIndex);
                if (zone <= 0 || !DemoZones.WaveIsIn(zone, CurrentWaveIndex)) return false;
                return CurrentWaveIndex >= DemoZones.EndWave(zone);
            }
        }

        /// <summary>The terminal's shutoff may run: the last wave was drawn and is down. In
        /// Assault "down" is the armed flag; in Prep the field is empty by definition (a
        /// retrigger that elected only the last wave clears with nothing owed).</summary>
        public bool ZoneShutoffReady =>
            ZoneFinalWaveDrawn && (Assault == AssaultState.Prep || FieldShutoffArmed);

        /// <summary>May the terminal call its zone's last wave? The opening span has been
        /// drawn and the next wave is the zone's last. Never mid-chain, never inside a Convert
        /// window or the finale.</summary>
        public bool CanCallZoneFinalWave
        {
            get
            {
                if (Phase != RunPhase.Tethered) return false;
                if (Mode == WaveMode.HatchBank && Assault == AssaultState.Assault) return false;
                if (_masterPhase != MasterControlPhase.None || SequentialPending) return false;
                if (ChainPending) return false;
                int next = CurrentWaveIndex + 1;
                int zone = DemoZones.ZoneOfWave(next);
                if (zone <= 0 || !DemoZones.WaveIsIn(zone, next)) return false;
                if (DemoZones.WavesPerTrigger(zone) <= 1) return false;
                return next == DemoZones.EndWave(zone)
                    && CurrentWaveIndex >= DemoZones.FirstWave(zone);
            }
        }

        /// <summary>The terminal press: draw the zone's last wave into whatever is standing.
        /// Legal in Assault (the opening span still alive, or dripping) and in Prep. Clears
        /// the drip — a fresh wave always does — chains nothing, and leaves the Assault live
        /// so the Hatch cannot offer a start while the zone is unfinished.</summary>
        public bool CallZoneFinalWave()
        {
            if (!CanCallZoneFinalWave)
            {
                Log.Info(Tag, "CallZoneFinalWave refused (wave " + CurrentWaveIndex + ", "
                    + Phase + ", " + Assault + ")");
                return false;
            }
            ClearChainState();
            CancelSequentialDraws();
            ResetFieldHold();
            Mode = WaveMode.Terminal;
            Assault = AssaultState.Assault;
            LaunchWave();
            _chainActive = false;
            _chainUntil = CurrentWaveIndex;
            Log.Event(Tag, "zone final wave called at the terminal — wave " + CurrentWaveIndex);
            return true;
        }

        /// <summary>The spawner's ArmShutoff verdict: the last wave's roster is spent and its
        /// field is empty. Idempotent; refused unless the last wave really is on the field.</summary>
        public void ArmFieldShutoff()
        {
            if (FieldShutoffArmed) return;
            if (Assault != AssaultState.Assault || !ZoneFinalWaveDrawn) return;
            FieldShutoffArmed = true;
            Log.Event(Tag, "field shutoff armed — the last wave is down, the terminal ends the zone");
        }

        /// <summary>Every Assault exit drops both holds: the drip and the armed switch.</summary>
        private void ResetFieldHold()
        {
            SetFieldTrickle(false);
            FieldShutoffArmed = false;
        }

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
