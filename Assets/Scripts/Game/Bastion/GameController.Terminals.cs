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

        void ClearPendingTerminal()
        {
            _pendingTerminalId = null;
            _pendingWaveIndexAtPull = 0;
            _pendingLayout = null;
            if (Sim != null)
                Sim.CancelSequentialDraws();
        }

        /// <summary>Field terminal Interact: start that terminal's remaining Waves as a
        /// sequence. Completes the terminal on the Nth clear.</summary>
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

            // Field Wave Spawns register only while active; enable before TriggerDraw.
            terminal.SetUnlockedSpawnsActive(true);

            int cleared = 0;
            int needed = terminal.WavesToComplete;
            if (layout != null)
                layout.TryGetFieldProgress(terminal.TerminalId, out cleared, out needed);
            int remaining = needed - cleared;
            if (remaining < 1) remaining = 1;
            int total = remaining;
            if (Sim.CurrentWaveIndex < WaveDirector.MaxWaves)
            {
                int runLeft = WaveDirector.MaxWaves - Sim.CurrentWaveIndex;
                if (runLeft < total) total = runLeft;
                if (total < 1) total = 1;
            }

            _pendingTerminalId = terminal.TerminalId;
            _pendingWaveIndexAtPull = Sim.CurrentWaveIndex;
            _pendingLayout = layout;
            Sim.ArmSequentialDraws(total);
            Sim.TriggerDraw();
            if (Sim.Assault != AssaultState.Assault)
            {
                ClearPendingTerminal();
                Sim.CancelSequentialDraws();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

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

            _pendingWaveIndexAtPull = Sim.CurrentWaveIndex;
            Log.Event("Terminal", id + " wave " + k + "/" + n + " cleared");
            if (layout.IsTerminalDone(id))
            {
                Log.Event("Terminal", id + " complete — wave cleared");
                ClearPendingTerminal();
            }
        }

        /// <summary>Prep T at the Hatch: start a Hatch-bank Wave.</summary>
        public bool StartHatchBankWave()
        {
            if (_net != null && _net.IsClientOnly)
            {
                Log.Event("Hatch", "hatch-bank refused — client cannot start a Wave");
                return false;
            }

            if (Sim == null || RunIsOver)
            {
                Log.Event("Hatch", "hatch-bank refused — run not live");
                return false;
            }
            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
            {
                Log.Event("Hatch", "hatch-bank refused — not in Prep");
                return false;
            }
            if (Sim.Unbanked <= 0L)
            {
                Log.Event("Hatch", "hatch-bank refused — unbanked=0");
                return false;
            }
            if (Sim.HatchBankSlotsLeft <= 0)
            {
                Log.Event("Hatch", "hatch-bank refused — no slots");
                return false;
            }
            if (Sim.AllWavesCleared)
            {
                Log.Event("Hatch", "hatch-bank refused — all waves cleared");
                return false;
            }

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

            Sim.TriggerDraw(WaveMode.HatchBank);
            if (Sim.Assault != AssaultState.Assault)
            {
                RestoreHatchBankSpawns();
                Log.Event("Hatch", "hatch-bank refused — draw did not start");
                return false;
            }

            Log.Event("Hatch", "hatch-bank started wave " + Sim.CurrentWaveIndex
                + " unbanked=" + Sim.Unbanked
                + " slots=" + Sim.HatchBankSlotsLeft);
            return true;
        }

        void RestoreHatchBankSpawns()
        {
            if (_hatchBankLayout == null) return;
            _hatchBankLayout.RestoreFieldSpawns();
            _hatchBankLayout = null;
        }
    }
}
