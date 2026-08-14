using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;

namespace GatekeeperBastion.Game
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
            return s_preview.NextWave(HatchBank.CarryWaveIndex(carry), sim.Siphon.Level).enemyCount;
        }

        int FieldPreviewBodies()
        {
            int idx = Sim.CurrentWaveIndex;
            if (idx >= WaveDirector.MaxWaves) idx = WaveDirector.MaxWaves - 1;
            if (idx < 0) idx = 0;
            return s_preview.NextWave(idx, Sim.Siphon.Level).enemyCount;
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
                if (Sim.Unbanked > 0L && Sim.HatchBankSlotsLeft > 0)
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
            return layout == null || !layout.IsTerminalDone(terminal.TerminalId);
        }

        void ToggleTerminalSelect(BastionTerminal terminal)
        {
            bool on = _armedTerminalId != terminal.TerminalId;
            _armedTerminalId = on ? terminal.TerminalId : null;
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
        /// Nth clear. Prep after the clear is untimed — Hatch-bank is legal there.</summary>
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

        bool HatchBankRefuse(string reason)
        {
            Log.Event("Hatch", "hatch-bank refused — " + reason);
            return false;
        }

        /// <summary>Prep T at the Hatch after E-select: start a Hatch-bank Wave.</summary>
        public bool StartHatchBankWave()
        {
            if (_net != null && _net.IsClientOnly)
                return HatchBankRefuse("client cannot start a Wave");
            if (Sim == null || RunIsOver)
                return HatchBankRefuse("run not live");
            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
                return HatchBankRefuse("not in Prep");
            if (!_armedHatch)
                return HatchBankRefuse("select intensity first");
            _armedHatch = false;
            if (Sim.Unbanked <= 0L)
                return HatchBankRefuse("unbanked=0");
            if (Sim.HatchBankSlotsLeft <= 0)
                return HatchBankRefuse("no slots");
            if (Sim.AllWavesCleared)
                return HatchBankRefuse("all waves cleared");
            if (Sim.MasterSequenceLive)
                return HatchBankRefuse("Master Control sequence live");

            // Hatch is a scene root, so parent walk misses BastionLayout; use the authored ref.
            ClearPendingTerminal();
            BastionLayout layout = _layout;
            if (layout == null && _repair != null)
                layout = _repair.GetComponentInParent<BastionLayout>();
            if (layout != null)
                layout.ActivateHatchBankSpawns();
            else
                Log.Warn(Tag, "hatch-bank — no BastionLayout; spawn registry unchanged");
            _hatchBankLayout = layout;

            int intensity = Sim.Siphon.Level;
            long juiced = Sim.JuiceSpentThisPull;
            Sim.TriggerDraw(WaveMode.HatchBank);
            if (Sim.Assault != AssaultState.Assault)
            {
                RestoreHatchBankSpawns();
                return HatchBankRefuse("draw did not start");
            }

            Log.Event("Hatch", "hatch-bank started wave " + Sim.CurrentWaveIndex
                + " carry=" + Sim.HatchCarry
                + " convert=" + Sim.HatchConvertMultiplier.ToString("0.00")
                + " unbanked=" + Sim.Unbanked
                + " slots=" + Sim.HatchBankSlotsLeft
                + " intensity=" + intensity
                + " juiced=" + juiced);
            return true;
        }

        /// <summary>Prep T at Master Control after E-select: start the two-phase win sequence.</summary>
        public bool StartMasterControl(BastionTerminal terminal)
        {
            if (terminal == null)
            {
                Log.Warn(Tag, "StartMasterControl refused — no terminal");
                return false;
            }

            if (_net != null && _net.IsClientOnly)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — client cannot start Master Control");
                return false;
            }

            if (terminal.Kind != BastionTerminalKind.Master)
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

            if (Sim.MasterSequenceLive)
            {
                Log.Event("Terminal", "master-control refused — sequence already live");
                return false;
            }

            if (!string.IsNullOrEmpty(_pendingTerminalId) || Sim.SequentialPending)
            {
                Log.Event("Terminal", "master-control refused — sequence already pending");
                return false;
            }

            if (_armedTerminalId != terminal.TerminalId)
            {
                Log.Event("Terminal", "master-control refused — select intensity first");
                return false;
            }

            ClearPendingTerminal();
            _armedTerminalId = null;
            _armedHatch = false;
            _masterTerminal = terminal;
            BastionLayout layout = terminal.Layout;
            if (layout == null)
                layout = _layout;
            if (layout != null)
                layout.SetControlRoomSealed(true);
            terminal.SetUnlockedSpawnsActive(true);

            int intensity = Sim.Siphon.Level;
            long juiced = Sim.JuiceSpentThisPull;
            if (!Sim.StartMasterControl())
            {
                if (layout != null)
                    layout.SetControlRoomSealed(false);
                terminal.SetUnlockedSpawnsActive(false);
                _masterTerminal = null;
                Log.Event("Terminal", "master-control refused — draw did not start");
                return false;
            }

            Log.Event("Terminal", "master-control started terminalWaves=" + MasterControl.TerminalWaves
                + " hatchWaves=" + MasterControl.HatchWaves
                + " intensity=" + intensity
                + " juiced=" + juiced);
            return true;
        }

        void OnMasterHatchHalfStarted()
        {
            BastionLayout layout = _layout;
            if (layout == null && _masterTerminal != null)
                layout = _masterTerminal.Layout;
            if (layout != null)
            {
                layout.SetControlRoomSealed(false);
                layout.ActivateHatchBankSpawns();
                _hatchBankLayout = layout;
            }
            else
                Log.Warn(Tag, "master-control hatch half — no BastionLayout");
        }

        void SnapshotHatchBankGraph()
        {
            if (Meta == null) return;
            BastionLayout layout = _layout;
            if (layout == null && _masterTerminal != null)
                layout = _masterTerminal.Layout;
            if (layout == null) return;
            Meta.SetCompletedTerminalIds(layout.CopyCompletedTerminalIds());
        }

        void RestoreHatchBankSpawns()
        {
            if (_hatchBankLayout == null) return;
            _hatchBankLayout.RestoreFieldSpawns();
            _hatchBankLayout = null;
        }
    }
}
