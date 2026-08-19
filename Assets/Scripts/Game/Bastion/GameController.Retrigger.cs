using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): the zone composite retrigger at the
    /// zone's deepest terminal — post-loss refight-avoidance (beat 4; NAV offers no wave
    /// choice). E arms it, +/- elects composite vs partial start, T launches the elected
    /// Waves consecutively via <see cref="RunSim.TriggerCompositeDraw"/>; the chain's clear
    /// completes every zone terminal so the bank re-arms. Pure offer rules live in
    /// <see cref="DemoRun"/>.</summary>
    public sealed partial class GameController
    {
        bool _armedRetrigger;
        bool _retriggerPartial;
        int _pendingRetriggerZone = -1;
        int _pendingRetriggerEnd;

        // demo.zone_retrigger plays on the first offer opened per zone. No v9 flag carries
        // it, so the latch is per session (statics reset on subsystem registration).
        static bool[] s_retriggerLineByZone = new bool[MetaState.DemoZoneCount];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRetriggerStatics()
        {
            s_retriggerLineByZone = new bool[MetaState.DemoZoneCount];
        }

        /// <summary>True while the armed retrigger has the partial start elected.</summary>
        public bool RetriggerPartialElected => _armedRetrigger && _retriggerPartial;

        /// <summary>The retrigger offer at <paramref name="terminal"/>, or default(None).
        /// Host only — the election is host sim; a client seat reads no offer (the co-op
        /// any-seat election is beat 4's multiplayer row, not built here).</summary>
        public DemoRetriggerOffer RetriggerOfferFor(BastionTerminal terminal)
        {
            if (terminal == null || _layout == null || Sim == null || RunIsOver)
                return DemoRetriggerOffer.None;
            if (terminal.Kind != BastionTerminalKind.Field)
                return DemoRetriggerOffer.None;
            if (_net != null && _net.IsClientOnly)
                return DemoRetriggerOffer.None;
            if (Sim.MasterSequenceLive)
                return DemoRetriggerOffer.None;

            bool doorOpen = terminal.UnlocksDoor != null
                && _layout.IsDoorOpen(terminal.UnlocksDoor.DoorId);
            return DemoRun.RetriggerOffer(
                Meta, terminal.TerminalId, ZoneOneWavesClearedThisRun(), doorOpen);
        }

        /// <summary>Zone-1 field Waves cleared this Run: each of its terminals is one Wave.</summary>
        int ZoneOneWavesClearedThisRun()
        {
            int cleared = 0;
            if (_layout != null)
            {
                if (_layout.IsTerminalDone(FirstTerminal.Id)) cleared += FirstTerminal.Waves;
                if (_layout.IsTerminalDone(FirstTerminal.SecondId)) cleared += FirstTerminal.Waves;
            }
            return cleared;
        }

        /// <summary>E at a terminal holding the offer: arm / disarm. Arming defaults to the
        /// composite; the first arm per zone speaks the offer line.</summary>
        void ToggleRetriggerSelect(BastionTerminal terminal, DemoRetriggerOffer offer)
        {
            bool on = !(_armedRetrigger && _armedTerminalId == terminal.TerminalId);
            _armedTerminalId = on ? terminal.TerminalId : null;
            _armedRetrigger = on;
            _retriggerPartial = false;
            if (on && offer.Zone >= 0 && offer.Zone < s_retriggerLineByZone.Length
                && !s_retriggerLineByZone[offer.Zone])
            {
                s_retriggerLineByZone[offer.Zone] = true;
                DemoVoice.PlayLocalAndBroadcast(
                    DemoConversationIds.ZoneRetrigger, DemoVoiceMode.Lock);
            }
            Log.Event("Terminal", terminal.TerminalId + (on
                ? " retrigger prep zone=" + offer.Zone + " mode=composite — selected"
                : " retrigger unselected"));
        }

        /// <summary>+/- while the retrigger is armed: elect composite vs partial start.</summary>
        void ToggleRetriggerElection(BastionTerminal terminal, DemoRetriggerOffer offer)
        {
            if (!offer.PartialAvailable)
            {
                Log.Event("Terminal", terminal.TerminalId
                    + " retrigger election refused — no partial start below the composite");
                return;
            }
            _retriggerPartial = !_retriggerPartial;
            Log.Event("Terminal", terminal.TerminalId + " retrigger election mode="
                + (_retriggerPartial ? "partial start=" + offer.PartialStart : "composite"));
        }

        /// <summary>T on the armed retrigger: launch the elected Waves consecutively. The
        /// whole zone's spawns feed the chain — a composite crosses both terminals' rooms.</summary>
        bool StartZoneRetrigger(BastionTerminal terminal)
        {
            if (_net != null && _net.IsClientOnly)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — client cannot start a Wave");
                return false;
            }
            var offer = RetriggerOfferFor(terminal);
            if (!offer.Available)
            {
                _armedRetrigger = false;
                _armedTerminalId = null;
                Log.Event("Terminal", terminal.TerminalId + " refused — no retrigger offer");
                return false;
            }
            if (!string.IsNullOrEmpty(_pendingTerminalId) || Sim.SequentialPending)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — wave already pending");
                return false;
            }

            int start = _retriggerPartial && offer.PartialAvailable
                ? offer.PartialStart
                : offer.CompositeStart;
            string mode = start > offer.CompositeStart ? "partial" : "composite";

            _armedTerminalId = null;
            _armedRetrigger = false;
            ClearPendingTerminal();
            ActivateZoneOneSpawns();

            if (!Sim.TriggerCompositeDraw(start, offer.EndWave))
            {
                if (_layout != null) _layout.RestoreFieldSpawns();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

            _pendingRetriggerZone = offer.Zone;
            _pendingRetriggerEnd = offer.EndWave;
            Log.Event("Demo", "zone retrigger zone=" + offer.Zone + " start=" + start
                + " waves=" + offer.WavesFrom(start) + " mode=" + mode);
            return true;
        }

        void ActivateZoneOneSpawns()
        {
            if (_layout == null) return;
            BastionTerminal first = _layout.FindTerminal(FirstTerminal.Id);
            if (first != null) first.SetUnlockedSpawnsActive(true);
            BastionTerminal second = _layout.FindTerminal(FirstTerminal.SecondId);
            if (second != null) second.SetUnlockedSpawnsActive(true);
        }

        /// <summary>The retrigger chain cleared (AssaultEnded fires once, after the last
        /// elected Wave): every zone terminal completes — the skipped Waves were cleared
        /// before the loss, which is the refight-avoidance — and the bank re-arms.</summary>
        void RecordRetriggerClear()
        {
            if (_pendingRetriggerZone < 0) return;
            int zone = _pendingRetriggerZone;
            int end = _pendingRetriggerEnd;
            _pendingRetriggerZone = -1;
            if (_layout == null) return;

            if (Meta != null && Meta.DemoActive)
                Meta.SetZoneDeepest(zone, end);
            string refuse;
            if (!_layout.IsTerminalDone(FirstTerminal.Id))
                _layout.TryCompleteTerminal(FirstTerminal.Id, out refuse);
            if (!_layout.IsTerminalDone(FirstTerminal.SecondId))
                _layout.TryCompleteTerminal(FirstTerminal.SecondId, out refuse);
            Log.Event("Demo", "zone terminals complete zone=" + zone);
        }

        /// <summary>A terminal phase drops any half-run retrigger — the next Run re-offers.</summary>
        void ClearPendingRetrigger()
        {
            _pendingRetriggerZone = -1;
            _armedRetrigger = false;
            _retriggerPartial = false;
        }
    }
}
