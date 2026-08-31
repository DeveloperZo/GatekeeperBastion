using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The Master Control sequence: five Terminal Waves then the Hatch-bank half,
    /// driven off the sequential follow-up timer. Split from RunSim.cs at the line cap —
    /// same class, one concern per file (the Composite/Dock split).</summary>
    public sealed partial class RunSim
    {
        /// <summary>One pull: <see cref="MasterControl.TerminalWaves"/> Terminal Waves then
        /// one elite wave. Refuses outside Tethered+Prep, or when a sequence is already live.
        /// Disables deep-start chaining.</summary>
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

        /// <summary>True once the elite is down and the Master terminal is the only thing
        /// left between the player and the way out. Read by the Game layer's shutoff gate, so
        /// the prompt and the verb cannot disagree about when the switch is live.</summary>
        public bool MasterAwaitsShutoff { get; private set; }

        /// <summary>The player threw the switch. This is the ONLY thing that completes a Run
        /// through Master Control — the sequence deliberately stalls until it is called, so a
        /// finale cannot end by attrition any more than a zone can.</summary>
        public bool ShutOffMasterControl()
        {
            if (!MasterAwaitsShutoff)
            {
                Log.Info(Tag, "master shutoff refused — the elite is still standing");
                return false;
            }
            MasterAwaitsShutoff = false;

            // THE SWITCH PAYS, HERE AS EVERYWHERE ELSE. ShutOffFieldWave banks the carry at a
            // zone terminal on the stated rule that "reaching the switch IS the payout", and
            // CompleteRun ends by wiping whatever is left — so without this the Master switch
            // was the one switch in the game that CONFISCATED a stake, and it did it to the
            // player who had just won. That was invisible while the finale ended in a
            // Hatch-bank window, because the window dumped the pile on its way out; removing
            // the window removed the payout with it.
            long carried = TotalUnbanked;
            if (carried > 0L)
            {
                DumpUnbanked();
                Log.Event(Tag, "master shutoff banked " + carried);
            }

            CancelMasterSequence();
            CompleteRun();
            return true;
        }

        void CancelMasterSequence()
        {
            _masterPhase = MasterControlPhase.None;
            _masterLeft = 0;
            MasterAwaitsShutoff = false;
            _masterHatchMulLocked = false;
            ResetFaucetCycle("sequence cancel"); // every caller is a cycle-ending moment
        }

        void AdvanceMasterControl(bool hatchBank)
        {
            _masterLeft--;
            if (_masterLeft > 0)
            {
                // No mode switch on the way through any more: every wave of the sequence,
                // the elite included, is a Terminal wave fought in the control room.
                _followUpTimer = WaveDirector.InterWaveGap;
                return;
            }

            if (_masterPhase == MasterControlPhase.Terminal)
            {
                // THE ELITE IS A CONTROL-ROOM WAVE NOW, not a Hatch-bank window (Zo,
                // 2026-08-28: "Terminal ends it but elite death is the gate to terminal end").
                // Mode stays Terminal, so the finale is fought where it is fought and the run
                // does not send the player back down the lane for its last beat.
                _masterPhase = MasterControlPhase.Elite;
                _masterLeft = 1;
                _followUpTimer = WaveDirector.InterWaveGap;
                var eliteHalf = MasterHatchHalfStarted;
                if (eliteHalf != null) eliteHalf();
                Log.Event(Tag, "master-control elite — kill it to arm the terminal");
                return;
            }

            // THE ELITE IS DEAD AND THE RUN IS NOT OVER. Every other wave in the game ends at
            // a terminal and this one does too: killing the elite ARMS the switch, it does not
            // throw it. CompleteRun moved to ShutOffMasterControl, which is the player's hand
            // on the terminal rather than the sim deciding the run is finished for them.
            _masterLeft = 0;
            MasterAwaitsShutoff = true;
            Log.Event(Tag, "master-control elite down — the terminal will end the Boarding");
        }
    }
}
