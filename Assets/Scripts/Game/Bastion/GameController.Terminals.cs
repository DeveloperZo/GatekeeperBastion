using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): field-terminal Wave start and clear.
    /// Split when the command surface landed and GameController.cs passed the 400-line cap —
    /// same class, same state, one concern per file (EndScreen, DevCommands).</summary>
    public sealed partial class GameController
    {
        /// <summary>Field terminal id waiting on the live Wave clear. Null when none pending.</summary>
        string _pendingTerminalId;
        int _pendingWaveIndexAtPull;
        BastionLayout _pendingLayout;
        BastionLayout _hatchBankLayout;
        BastionTerminal _masterTerminal;
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
            return HatchBank.ApplyHatchComposition(raw, carry).enemyCount;
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
                _armedTerminalId = null;
            if (lookingHatch == null)
                _armedHatch = false;

            bool hatchStartable = lookingHatch != null && lookingHatch.OffersHatchBank;
            bool terminalStartable = lookingTerminal != null && CanSelectTerminal(lookingTerminal);

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
                if (Sim.TotalUnbanked > 0L && Sim.HatchBankSlotsLeft > 0)
                    Log.Event("Hatch", "hatch-bank refused — look at the Hatch");
                else
                    Log.Event("Run", "draw refused — Waves start at a terminal");
                return;
            }

            if (Controls.Down(ControlId.Interact))
            {
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

        /// <summary>Field terminal T after E-select: start one Wave. Completes the terminal on the
        /// Nth clear. Prep after the clear is untimed.</summary>
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
            if (layout.IsTerminalDone(id))
                Log.Event("Terminal", id + " complete — wave cleared");
            ClearPendingTerminal();
        }
    }
}
