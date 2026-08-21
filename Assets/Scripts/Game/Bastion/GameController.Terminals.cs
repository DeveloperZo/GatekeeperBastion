using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): field-terminal Wave start and clear.
    /// Split when the command surface landed and GameController.cs passed the 400-line cap —
    /// same class, same state, one concern per file (EndScreen, DevCommands, Retrigger).</summary>
    public sealed partial class GameController
    {
        /// <summary>Field terminal id waiting on the live Wave clear. Null when none pending.</summary>
        string _pendingTerminalId;
        int _pendingWaveIndexAtPull;
        BastionLayout _pendingLayout;
        BastionLayout _hatchBankLayout;
        BastionTerminal _masterTerminal;
        bool _masterLookPlayed;
        string _armedTerminalId;
        bool _armedHatch;

        /// <summary>True when this trigger has been E-selected this Prep (juice locked; T starts).</summary>
        public bool IsWaveStartArmed(string terminalId) =>
            !string.IsNullOrEmpty(terminalId) && terminalId == _armedTerminalId;

        /// <summary>True when Hatch-bank has been E-selected this Prep (juice locked; T starts).</summary>
        public bool IsHatchBankArmed => _armedHatch;

        static readonly WaveDirector s_preview = new WaveDirector();

        int PreviewBodies()
        {
            var sim = Sim;
            int carry = sim.Mode == WaveMode.HatchBank && sim.Assault == AssaultState.Assault
                ? sim.HatchCarry
                : sim.WavesSinceBank;
            WaveSpec raw = s_preview.NextWave(HatchBank.CarryWaveIndex(carry), sim.Siphon.Level, sim.PlayerCount);
            WaveSpec spec = HatchBank.ApplyHatchComposition(raw, carry);
            if (sim.MasterPhase == MasterControlPhase.Hatch)
                spec = MasterControl.ApplyFinaleElite(spec);
            return spec.enemyCount;
        }

        int FieldPreviewBodies()
        {
            int idx = Sim.CurrentWaveIndex;
            if (idx >= WaveDirector.MaxWaves) idx = WaveDirector.MaxWaves - 1;
            if (idx < 0) idx = 0;
            return s_preview.NextWave(idx, Sim.Siphon.Level, Sim.PlayerCount).enemyCount;
        }

        void TickPrepCommands()
        {
            var looking = _interactor != null ? _interactor.Current : null;
            var lookingTerminal = looking as BastionTerminal;
            var lookingHatch = looking as RepairInteractable;
            if (lookingTerminal == null || lookingTerminal.TerminalId != _armedTerminalId)
            {
                _armedTerminalId = null;
                _armedRetrigger = false;
                _retriggerPartial = false;
            }
            if (lookingHatch == null)
                _armedHatch = false;
            if (lookingTerminal != null && lookingTerminal.IsMaster)
                NoticeMasterLook();

            bool hatchStartable = lookingHatch != null && lookingHatch.OffersHatchBank;
            var retriggerOffer = RetriggerOfferFor(lookingTerminal);
            bool terminalStartable = !retriggerOffer.Available
                && lookingTerminal != null && CanSelectTerminal(lookingTerminal);

            if (Controls.Down(ControlId.PullDraw))
            {
                if (hatchStartable)
                {
                    if (!_armedHatch)
                    {
                        Log.Event("Hatch", "hatch-bank refused — select intensity first");
                        return;
                    }
                    StartHatchBankWave();
                    return;
                }
                if (retriggerOffer.Available)
                {
                    if (!_armedRetrigger || _armedTerminalId != lookingTerminal.TerminalId)
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — select the retrigger first");
                        return;
                    }
                    StartZoneRetrigger(lookingTerminal);
                    return;
                }
                if (terminalStartable)
                {
                    if (_armedTerminalId != lookingTerminal.TerminalId)
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — select intensity first");
                        return;
                    }
                    if (lookingTerminal.Kind == BastionTerminalKind.Master)
                        StartMasterControl(lookingTerminal);
                    else
                        StartTerminalWave(lookingTerminal);
                    return;
                }
                if (Sim.TotalUnbanked > 0L)
                    Log.Event("Hatch", "hatch-bank refused — look at the Hatch");
                else
                    Log.Event("Run", "draw refused — Waves start at a terminal");
                return;
            }

            if (Controls.Down(ControlId.Interact))
            {
                if (retriggerOffer.Available)
                {
                    ToggleRetriggerSelect(lookingTerminal, retriggerOffer);
                    return;
                }
                if (terminalStartable)
                {
                    ToggleTerminalSelect(lookingTerminal);
                    return;
                }
                if (hatchStartable)
                {
                    ToggleHatchSelect();
                    return;
                }
            }

            int delta = 0;
            if (Controls.Down(ControlId.DialUp)) delta = 1;
            else if (Controls.Down(ControlId.DialDown)) delta = -1;
            if (delta == 0) return;

            if (_armedRetrigger && retriggerOffer.Available
                && lookingTerminal != null && lookingTerminal.TerminalId == _armedTerminalId)
            {
                // While the retrigger is armed, +/- elects the start — juice has no seat
                // in this Prep (intensity is selected with the arm, level as it stands).
                ToggleRetriggerElection(lookingTerminal, retriggerOffer);
                return;
            }

            if (_armedHatch || !string.IsNullOrEmpty(_armedTerminalId))
            {
                Log.Event("Run", "juice refused — intensity selected");
                return;
            }

            bool atTrigger = lookingHatch != null
                || (lookingTerminal != null
                    && (lookingTerminal.Kind == BastionTerminalKind.Field
                        || lookingTerminal.Kind == BastionTerminalKind.Master));
            Sim.TryJuice(delta, atTrigger);
        }

        bool CanSelectTerminal(BastionTerminal terminal)
        {
            if (terminal == null || Sim == null || Sim.MasterSequenceLive) return false;
            if (terminal.Kind == BastionTerminalKind.Master) return true;
            if (terminal.Kind != BastionTerminalKind.Field) return false;
            BastionLayout layout = terminal.Layout;
            if (layout != null && layout.IsTerminalDone(terminal.TerminalId))
                return false;
            return FieldStartBlockReason(terminal.TerminalId) == null;
        }

        /// <summary>Null when this Field id may start. Else the refuse suffix after
        /// <c>start refused — </c>.</summary>
        public string FieldStartBlockReason(string terminalId)
        {
            if (string.IsNullOrEmpty(terminalId) || _layout == null)
                return null;
            MetaState meta = Meta;
            bool t1Exists = _layout.FindTerminal(FirstTerminal.Id) != null;
            bool t1Done = _layout.IsTerminalDone(FirstTerminal.Id);
            if (!FirstTerminal.OtherFieldBlocked(
                    terminalId, meta != null && meta.DemoActive, t1Exists, t1Done))
                return null;
            // Only one reason survives the zone re-spec: the old "Hatch-bank first" case
            // needed the bank-started clause, and zone 1's bank comes after both terminals.
            return "finish this terminal";
        }

        void ToggleTerminalSelect(BastionTerminal terminal)
        {
            bool on = _armedTerminalId != terminal.TerminalId;
            _armedTerminalId = on ? terminal.TerminalId : null;
            if (on && terminal.TerminalId == FirstTerminal.Id)
                Log.Event("Demo", "first field id=" + FirstTerminal.Id + " waves=" + FirstTerminal.Waves);
            Log.Event("Terminal", terminal.TerminalId + (on
                ? " prep intensity=" + Sim.Siphon.Level + " juice=" + Sim.JuiceSpentThisPull
                    + " bodies=" + FieldPreviewBodies() + " — selected"
                : " intensity unlocked"));
        }

        void ToggleHatchSelect()
        {
            bool on = !_armedHatch;
            _armedHatch = on;
            Log.Event("Hatch", on
                ? "hatch-bank prep intensity=" + Sim.Siphon.Level + " juice=" + Sim.JuiceSpentThisPull
                    + " bodies=" + PreviewBodies() + " — selected"
                : "intensity unlocked");
        }

        void ClearPendingTerminal()
        {
            _pendingTerminalId = null;
            _pendingWaveIndexAtPull = 0;
            _pendingLayout = null;
            if (Sim != null)
                Sim.CancelSequentialDraws();
        }

        /// <summary>Field terminal T after E-select: start the zone's trigger. A zone authored
        /// at one Wave per trigger draws one Wave and completes the terminal on its Nth clear;
        /// a zone authored above that launches its whole Wave span consecutively, with no Prep
        /// between them (Docs/Demo - Single/5-repeat-to-control-room.md). Prep after the clear is
        /// untimed either way.</summary>
        public bool StartTerminalWave(BastionTerminal terminal)
        {
            if (terminal == null)
            {
                Log.Warn(Tag, "StartTerminalWave refused — no terminal");
                return false;
            }

            if (_net != null && _net.IsClientOnly)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — client cannot start a Wave");
                return false;
            }

            if (terminal.Kind != BastionTerminalKind.Field)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — kind=" + terminal.Kind);
                return false;
            }

            if (Sim == null || RunIsOver)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — run not live");
                return false;
            }

            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — not in Prep");
                return false;
            }

            if (Sim.AllWavesCleared)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — run already ended");
                return false;
            }

            if (Sim.MasterSequenceLive)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — Master Control sequence live");
                return false;
            }

            BastionLayout layout = terminal.Layout;
            if (layout != null && layout.IsTerminalDone(terminal.TerminalId))
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — already complete");
                return false;
            }

            if (!string.IsNullOrEmpty(_pendingTerminalId) || Sim.SequentialPending)
            {
                string pending = _pendingTerminalId;
                if (string.IsNullOrEmpty(pending))
                    pending = "sequence";
                Log.Event("Terminal", terminal.TerminalId + " refused — wave already pending "
                    + pending);
                return false;
            }

            if (_armedTerminalId != terminal.TerminalId)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — select intensity first");
                return false;
            }

            string block = FieldStartBlockReason(terminal.TerminalId);
            if (block != null)
            {
                Log.Event("Demo", "start refused — " + block);
                return false;
            }

            _armedTerminalId = null;

            // A multi-Wave zone's trigger is one composite, never N separate draws with Prep
            // between them. Off the demo the table has no say — a campaign save keeps the
            // single draw at whatever Wave the Run has reached.
            MetaState demoMeta = Meta;
            int zone = demoMeta != null && demoMeta.DemoActive
                ? DemoZones.ZoneOf(terminal.TerminalId)
                : 0;
            int wavesPerTrigger = DemoZones.WavesPerTrigger(zone);
            if (wavesPerTrigger > 1)
                return StartZoneComposite(terminal, zone, wavesPerTrigger);

            int intensity = Sim.Siphon.Level;
            long juiced = Sim.JuiceSpentThisPull;

            // Field Wave Spawns register only while active; enable before TriggerDraw.
            terminal.SetUnlockedSpawnsActive(true);

            _pendingTerminalId = terminal.TerminalId;
            _pendingWaveIndexAtPull = Sim.CurrentWaveIndex;
            _pendingLayout = layout;
            Sim.TriggerDraw();
            if (Sim.Assault != AssaultState.Assault)
            {
                ClearPendingTerminal();
                Sim.CancelSequentialDraws();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

            Log.Event("Terminal", terminal.TerminalId + " started wave " + Sim.CurrentWaveIndex
                + " intensity=" + intensity + " juiced=" + juiced);
            return true;
        }

        /// <summary>The multi-Wave arm of <see cref="StartTerminalWave"/>: the zone's whole
        /// Wave span on one trigger, consecutively. Completion is the shared composite
        /// bookkeeping in GameController.Retrigger.cs — a first trigger and a post-loss
        /// retrigger end the same way, so there is one clear path, not two.</summary>
        bool StartZoneComposite(BastionTerminal terminal, int zone, int wavesPerTrigger)
        {
            int first = DemoZones.FirstWave(zone);
            int end = DemoZones.EndWave(zone);
            // The chain crosses every room the zone owns, so the zone's spawns go live, not
            // just this terminal's. Spawns register only while active; enable before the draw.
            ActivateZoneSpawns(zone);
            if (!Sim.TriggerCompositeDraw(first, end))
            {
                if (_layout != null) _layout.RestoreFieldSpawns();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

            _pendingCompositeZone = zone;
            _pendingCompositeEnd = end;
            Log.Event("Demo", "zone start zone=" + zone + " waves=" + wavesPerTrigger
                + " mode=composite");
            return true;
        }

        void RecordPendingTerminalClear()
        {
            if (string.IsNullOrEmpty(_pendingTerminalId))
                return;

            string id = _pendingTerminalId;
            int indexAtPull = _pendingWaveIndexAtPull;
            BastionLayout layout = _pendingLayout;

            if (layout == null)
            {
                Log.Warn(Tag, "AssaultEnded: pending terminal " + id + " but no BastionLayout");
                ClearPendingTerminal();
                return;
            }

            int waves = Sim.CurrentWaveIndex - indexAtPull;
            if (!layout.RecordWaveCleared(id, waves, out int k, out int n, out string refuseReason))
            {
                Log.Event("Terminal", id + " complete refused — " + refuseReason);
                ClearPendingTerminal();
                Sim.CancelSequentialDraws();
                return;
            }

            Log.Event("Terminal", id + " wave " + k + "/" + n + " cleared");
            // DemoZoneDeepestWave is NOT written here any more: RunSim.FieldWaveCleared
            // carries every field-Wave clear, including the ones inside a composite this
            // method never sees, and OnFieldWaveCleared below writes them all.
            MetaState meta = Meta;
            if (layout.IsTerminalDone(id))
            {
                Log.Event("Terminal", id + " complete — wave cleared");
                int zone = DemoZones.ZoneOf(id);
                if (meta != null && meta.DemoActive && zone > 0
                    && ZoneTerminalsAllDone(layout, zone))
                    Log.Event("Demo", "zone terminals complete zone=" + zone);
            }
            ClearPendingTerminal();
        }

        /// <summary>True when every terminal the zone table authors is complete in this
        /// layout. Absent ids answer false — a zone is not complete because a room is
        /// missing.</summary>
        static bool ZoneTerminalsAllDone(BastionLayout layout, int zone)
        {
            int count = DemoZones.TerminalCount(zone);
            if (count <= 0) return false;
            for (int i = 0; i < count; i++)
            {
                string id = DemoZones.TerminalIdAt(zone, i);
                if (string.IsNullOrEmpty(id) || !layout.IsTerminalDone(id))
                    return false;
            }
            return true;
        }

        /// <summary>A field Wave cleared (RunSim.FieldWaveCleared; 1-based, never a bank
        /// Wave). Per-zone deepest writes on field-Wave CLEAR, never on bank start (ruled at
        /// cut, Zo 2026-08-17), and it writes per Wave rather than per trigger so a composite
        /// that dies mid-chain still records the Waves it did clear — that is what the partial
        /// election and the guest BankCap read (Docs/Demo - Single/5-repeat-to-control-room.md).
        /// SetZoneDeepest is raise-only, so re-clearing an earlier Wave cannot lower it.</summary>
        void OnFieldWaveCleared(int wave)
        {
            MetaState meta = Meta;
            if (meta == null || !meta.DemoActive)
                return;
            meta.SetZoneDeepest(DemoZones.ZoneOfWave(wave), wave);
        }
    }
}
