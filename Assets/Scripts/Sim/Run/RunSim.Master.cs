using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The Master Control sequence: five Terminal Waves then the Hatch-bank half,
    /// driven off the sequential follow-up timer. Split from RunSim.cs at the line cap —
    /// same class, one concern per file (the Composite/Dock split).</summary>
    public sealed partial class RunSim
    {
        /// <summary>One pull: <see cref="MasterControl.TerminalWaves"/> Terminal Waves then
        /// <see cref="MasterControl.HatchWaves"/> Hatch-bank windows. Refuses outside
        /// Tethered+Prep, or when a sequence is already live. Disables deep-start chaining.</summary>
        public bool StartMasterControl()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "StartMasterControl ignored (phase " + Phase + ", " + Assault + ")");
                return false;
            }
            if (_masterPhase != MasterControlPhase.None)
            {
                Log.Info(Tag, "StartMasterControl ignored: sequence already live");
                return false;
            }
            if (SequentialPending)
            {
                Log.Info(Tag, "StartMasterControl ignored: sequence already pending");
                return false;
            }

            _startWave = 1;
            ClearChainState();
            CancelSequentialDraws();
            ResetFaucetCycle("master control");
            _masterPhase = MasterControlPhase.Terminal;
            _masterLeft = MasterControl.TerminalWaves;
            _masterHatchMulLocked = false;
            TriggerDraw(WaveMode.Terminal);
            if (Assault != AssaultState.Assault)
            {
                CancelMasterSequence();
                Log.Info(Tag, "StartMasterControl ignored: draw did not start");
                return false;
            }
            return true;
        }

        void CancelMasterSequence()
        {
            _masterPhase = MasterControlPhase.None;
            _masterLeft = 0;
            _masterHatchMulLocked = false;
            ResetFaucetCycle("sequence cancel"); // every caller is a cycle-ending moment
        }

        void AdvanceMasterControl(bool hatchBank)
        {
            _masterLeft--;
            if (_masterLeft > 0)
            {
                if (_masterPhase == MasterControlPhase.Hatch)
                    Mode = WaveMode.HatchBank;
                _followUpTimer = WaveDirector.InterWaveGap;
                return;
            }

            if (_masterPhase == MasterControlPhase.Terminal)
            {
                _masterPhase = MasterControlPhase.Hatch;
                _masterLeft = MasterControl.HatchWaves;
                SnapshotHatchCarry();
                _masterHatchMulLocked = true;
                Mode = WaveMode.HatchBank;
                _followUpTimer = WaveDirector.InterWaveGap;
                var hatchHalf = MasterHatchHalfStarted;
                if (hatchHalf != null) hatchHalf();
                Log.Info(Tag, "master-control hatch half — hatchWaves=" + MasterControl.HatchWaves);
                return;
            }

            if (hatchBank)
                DumpUnbanked();
            CancelMasterSequence();
            CompleteRun();
        }
    }
}
