using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Dev checkpoint seeding: land a live Run at a bank window or the Master
    /// Hatch half without playing the Waves that built that pile. GameController is the
    /// only caller; tests pin the arithmetic.</summary>
    public sealed partial class RunSim
    {
        /// <summary>Credit Unbanked and set carry as if <paramref name="wavesSinceBank"/>
        /// field Waves had cleared since the last Hatch-bank. <paramref name="fieldWavesDrawn"/>
        /// is the 1-based field Wave just finished (CurrentWaveIndex); −1 copies
        /// wavesSinceBank. Prep only.</summary>
        public void SeedBankPile(long unbanked, int wavesSinceBank, int fieldWavesDrawn = -1)
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "SeedBankPile ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            if (unbanked < 0L) unbanked = 0L;
            _wallets.Set(0, unbanked);
            if (wavesSinceBank < 0) wavesSinceBank = 0;
            WavesSinceBank = wavesSinceBank;
            int drawn = fieldWavesDrawn < 0 ? wavesSinceBank : fieldWavesDrawn;
            if (drawn < 0) drawn = 0;
            if (drawn > WaveDirector.MaxWaves) drawn = WaveDirector.MaxWaves;
            CurrentWaveIndex = drawn;
        }

        /// <summary>Jump to Master Control's Hatch half in Prep: convert mul from five
        /// terminal Waves, Unbanked = <see cref="HatchBank.MasterTerminalUnbanked"/>, then
        /// draw the elite window immediately. Fires <see cref="MasterHatchHalfStarted"/>
        /// before the draw so hatch spawns are live. Refuses outside Tethered+Prep.</summary>
        public bool SkipToMasterHatchHalf()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "SkipToMasterHatchHalf ignored (phase " + Phase + ", " + Assault + ")");
                return false;
            }
            if (SequentialPending)
            {
                Log.Info(Tag, "SkipToMasterHatchHalf ignored: sequence already pending");
                return false;
            }

            _startWave = 1;
            ClearChainState();
            CancelSequentialDraws();
            WavesSinceBank = MasterControl.TerminalWaves;
            _masterPhase = MasterControlPhase.Hatch;
            _masterLeft = MasterControl.HatchWaves;
            SnapshotHatchCarry();
            _masterHatchMulLocked = true;
            _wallets.Set(0, HatchBank.MasterTerminalUnbanked());
            Mode = WaveMode.HatchBank;
            var hatchHalf = MasterHatchHalfStarted;
            if (hatchHalf != null) hatchHalf();
            Log.Info(Tag, "master-control hatch half — hatchWaves=" + MasterControl.HatchWaves);
            TriggerDraw(WaveMode.HatchBank);
            return Assault == AssaultState.Assault;
        }
    }
}
