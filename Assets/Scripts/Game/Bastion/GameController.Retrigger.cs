using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): composite Wave chains over a zone —
    /// consecutive Waves with no Prep between them, launched via
    /// <see cref="RunSim.TriggerCompositeDraw"/> and completing the whole zone on the chain's
    /// single clear. Two callers reach it: a multi-Wave zone's FIRST terminal trigger
    /// (GameController.Terminals.cs) and the post-loss retrigger offered at the zone's deepest
    /// terminal (beat 4; NAV offers no wave choice) — E arms it, +/- elects composite vs
    /// partial start, T launches. Pure offer rules live in <see cref="DemoRun"/>.</summary>
    public sealed partial class GameController
    {
        bool _armedRetrigger;
        bool _retriggerPartial;
        // Composite bookkeeping, shared by the first trigger and the retrigger: they complete
        // identically, so there is one completion path, not two.
        int _pendingCompositeZone = -1;
        int _pendingCompositeEnd;

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
                Meta, terminal.TerminalId,
                ZoneWavesClearedThisRun(DemoZones.ZoneOf(terminal.TerminalId)), doorOpen);
        }

        /// <summary>Field Waves this Run has cleared in the zone: every completed terminal of
        /// the zone is that zone's Waves-per-trigger. A zone the table does not author has no
        /// terminals, so it answers 0.</summary>
        int ZoneWavesClearedThisRun(int zone)
        {
            if (_layout == null) return 0;
            int cleared = 0;
            int perTrigger = DemoZones.WavesPerTrigger(zone);
            int count = DemoZones.TerminalCount(zone);
            for (int i = 0; i < count; i++)
            {
                string id = DemoZones.TerminalIdAt(zone, i);
                if (!string.IsNullOrEmpty(id) && _layout.IsTerminalDone(id))
                    cleared += perTrigger;
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
                    DemoConversationIds.ZoneRetrigger, DemoVoiceMode.Subtitle);
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
        /// whole zone's spawns feed the chain — a composite crosses every room the zone owns.</summary>
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
            ActivateZoneSpawns(offer.Zone);

            if (!Sim.TriggerCompositeDraw(start, offer.EndWave))
            {
                if (_layout != null) _layout.RestoreFieldSpawns();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

            _pendingCompositeZone = offer.Zone;
            _pendingCompositeEnd = offer.EndWave;
            Log.Event("Demo", "zone retrigger zone=" + offer.Zone + " start=" + start
                + " waves=" + offer.WavesFrom(start) + " mode=" + mode);
            return true;
        }

        /// <summary>Enable every spawn the zone's terminals own: a composite crosses each of
        /// the zone's rooms, so one terminal's spawns cannot feed the whole chain.</summary>
        void ActivateZoneSpawns(int zone)
        {
            if (_layout == null) return;
            int count = DemoZones.TerminalCount(zone);
            for (int i = 0; i < count; i++)
            {
                string id = DemoZones.TerminalIdAt(zone, i);
                if (string.IsNullOrEmpty(id)) continue;
                BastionTerminal t = _layout.FindTerminal(id);
                if (t != null) t.SetUnlockedSpawnsActive(true);
            }
        }

        /// <summary>A composite chain cleared (AssaultEnded fires once, after the last elected
        /// Wave). It records how deep the zone was run and NOTHING ELSE.
        ///
        /// <para>THE ZONE IS NOT COMPLETE (Zo, 2026-08-26). Completing its terminals here is
        /// what made the walk optional: the 2026-08-26 playthrough held the Hatch, killed all
        /// 48 bodies of zone 1's composite, and watched "zone complete" fire from 16 m away —
        /// then walked to T2 and was refused three times at a prop that had nothing left to
        /// do. Surviving the Wave is not the objective. REACHING THE TERMINAL IS, and
        /// GameController.ShutOffTerminal is the only thing that completes a zone.</para></summary>
        void RecordCompositeClear()
        {
            if (_pendingCompositeZone < 0) return;
            int zone = _pendingCompositeZone;
            int end = _pendingCompositeEnd;
            _pendingCompositeZone = -1;
            if (_layout == null) return;

            if (Meta != null && Meta.DemoActive)
                Meta.SetZoneDeepest(zone, end);
            Log.Event("Demo", "zone waves cleared zone=" + zone
                + " — the terminal is still the objective");
        }

        /// <summary>A terminal phase drops any half-run composite — the next Run re-offers.</summary>
        void ClearPendingRetrigger()
        {
            _pendingCompositeZone = -1;
            _armedRetrigger = false;
            _retriggerPartial = false;
        }
    }
}
