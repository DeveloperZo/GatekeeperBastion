using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): Prep / Assault centre strips and
    /// shortcut-panel rows.</summary>
    public sealed partial class HudController
    {
        void PushShortcutPanel(RunSim sim) => PushShortcutPanel(sim, FightShortcutContext(sim));

        void PushEndScreenShortcut()
        {
            Controls.TickPanelToggle();
            var sim = _game != null ? _game.Sim : null;
            if (sim == null) return; // guard: no controller/sim wired means no panel to fill
            PushShortcutPanel(sim, ControlContext.EndScreen);
        }

        ControlContext FightShortcutContext(RunSim sim)
        {
            var ctx = Controls.FightAlways;
            ctx |= ViewAssault(sim) == AssaultState.Prep ? ControlContext.Prep : ControlContext.Assault;
            return ctx;
        }

        void PushShortcutPanel(RunSim sim, ControlContext ctx)
        {
            if ((ctx & Controls.FightAlways) != 0
                && _interactor != null
                && !string.IsNullOrEmpty(_interactor.CurrentPrompt))
                ctx |= ControlContext.NearInteract;

            int dialLevel = ViewAssault(sim) == AssaultState.Prep ? sim.Siphon.Level : -1;
            if (IsPauseOpen) FillPauseShortcuts(sim, ctx, dialLevel);

            // Left drawer open ⇒ Collapsed false; fill full keycaps (no tip-only mode).
            if (Controls.Collapsed)
            {
                ShortcutPanel.EstimateCollapsedHeight();
                _view.SetShortcutRows(null, true);
                return;
            }

            ShortcutPanel.FillRows(ctx, _shortcutRows, dialLevel);
            _view.SetShortcutRows(_shortcutRows, false);
        }

        void FillPauseShortcuts(RunSim sim, ControlContext ctx, int dialLevel)
        {
            if (_pauseShortcuts == null || sim == null) return;
            ShortcutPanel.FillRows(ctx, _pauseShortcutRows, dialLevel, DevSurface.None);
            var net = _game != null ? _game.Net : null;
            string code = net != null ? net.JoinCode : null;
            if (!string.IsNullOrEmpty(code))
                _pauseShortcutRows.Add(new ShortcutRow("CODE", code, "SESSION"));
            _pauseShortcuts.SetCollapsed(false);
            _pauseShortcuts.SetRows(_pauseShortcutRows);
        }

        void PushPrepPanel(RunSim sim, int wave, float hatchHp, float hatchMax, long unbanked)
        {
            if (sim.AllWavesCleared)
            {
                _view.SetPrepHeading(
                    "<b>BASTION STRIPPED</b> — Master Control survived");
                _view.SetPrepLine(1, "there is nothing left to draw");
                _view.SetPrepLine(2, null);
                _view.SetPrepLine(3, null);
                _view.SetPrepLine(4, null);
                return;
            }

            int lvl = sim.Siphon.Level;
            bool stripped = wave >= WaveDirector.MaxWaves;
            int specIndex = stripped ? WaveDirector.MaxWaves - 1 : wave;
            int here = _waves.NextWave(specIndex, lvl, sim.PlayerCount).enemyCount;
            _lineBuf.Clear();
            _lineBuf.Append("<b>PREP</b>  ·  ");
            if (sim.MasterSequenceLive)
            {
                bool hatchHalf = sim.MasterPhase == MasterControlPhase.Hatch;
                int n = hatchHalf ? MasterControl.HatchWaves : MasterControl.TerminalWaves;
                int shown = n - sim.MasterWavesLeft + 1;
                if (shown < 1) shown = 1;
                if (shown > n) shown = n;
                _lineBuf.Append("Master Control  ");
                _lineBuf.Append(hatchHalf ? "Hatch-bank " : "terminal ");
                _lineBuf.Append(shown).Append('/').Append(n);
            }
            else
            {
                _lineBuf.Append("Wave ");
                if (stripped)
                    _lineBuf.Append(WaveDirector.MaxWaves);
                else
                    _lineBuf.Append(wave + 1);
                _lineBuf.Append('/').Append(WaveDirector.MaxWaves);
                if (stripped)
                    _lineBuf.Append(" — field stripped");
            }
            if (sim.SequentialFollowUpRemaining > 0f)
            {
                int sec = Mathf.CeilToInt(sim.SequentialFollowUpRemaining);
                _lineBuf.Append(" — next Wave in ").Append(sec).Append("s");
            }
            else if (!sim.MasterSequenceLive)
                _lineBuf.Append(" — use a field terminal");
            _view.SetPrepHeading(_lineBuf.ToString());

            bool damaged = hatchHp < hatchMax - 0.01f;
            _lineBuf.Clear();
            _lineBuf.Append("HATCH ").Append(hatchHp.ToString("0")).Append('/')
                .Append(hatchMax.ToString("0"));
            if (damaged) _lineBuf.Append("  ·  damaged — repair at hatch");
            else _lineBuf.Append("  ·  sealed");
            _view.SetPrepLine(1, _lineBuf.ToString());

            _lineBuf.Clear();
            if (sim.MasterPhase == MasterControlPhase.Hatch || (stripped && unbanked > 0L))
                _lineBuf.Append(here).Append(" gatekeepers on a Hatch-bank");
            else
                _lineBuf.Append(here).Append(" gatekeepers on the next terminal Wave");
            if (lvl < TetherSiphon.MaxLevel)
            {
                int nextCount = _waves.NextWave(specIndex, lvl + 1, sim.PlayerCount).enemyCount;
                _lineBuf.Append("  ·  +/- raise Intensity ").Append(lvl + 1)
                    .Append(" → ").Append(nextCount);
            }
            _view.SetPrepLine(2, _lineBuf.ToString());

            if (unbanked > 0L && sim.HatchBankSlotsLeft > 0
                && (_game == null || !_game.HatchBankStartBlocked))
            {
                _lineBuf.Clear();
                _lineBuf.Append("<b>Hatch-bank — T at the Hatch</b>  (")
                    .Append(unbanked).Append(" unbanked, ")
                    .Append(sim.HatchBankSlotsLeft).Append(" slots)");
                _view.SetPrepLine(3, _lineBuf.ToString());
            }
            else
                _view.SetPrepLine(3, null);
            _view.SetPrepLine(4, null);
        }

        void PushAssaultPanel(RunSim sim, int wave, int remaining)
        {
            bool hatchBank = sim.Mode == WaveMode.HatchBank;
            int bankSec = hatchBank ? Mathf.CeilToInt(sim.HatchBankRemaining) : 0;
            int masterLeft = sim.MasterWavesLeft;
            if (remaining == _cacheAlive && wave == _cacheAssaultWave
                && bankSec == _cacheHatchBankSec && masterLeft == _cacheMasterLeft)
                return;
            _cacheAlive = remaining;
            _cacheAssaultWave = wave;
            _cacheHatchBankSec = bankSec;
            _cacheMasterLeft = masterLeft;
            _lineBuf.Clear();
            if (sim.MasterSequenceLive)
            {
                bool hatchHalf = sim.MasterPhase == MasterControlPhase.Hatch;
                int n = hatchHalf ? MasterControl.HatchWaves : MasterControl.TerminalWaves;
                int shown = n - sim.MasterWavesLeft + 1;
                if (shown < 1) shown = 1;
                if (shown > n) shown = n;
                if (hatchHalf)
                    _lineBuf.Append("HATCH-BANK — Master Control ").Append(shown)
                        .Append('/').Append(n).Append(" — ").Append(bankSec).Append("s");
                else
                    _lineBuf.Append("ASSAULT — Master Control ").Append(shown)
                        .Append('/').Append(n);
                _lineBuf.Append("   |   enemies remaining: ").Append(remaining);
            }
            else if (hatchBank)
            {
                _lineBuf.Append("HATCH-BANK — ").Append(bankSec).Append("s")
                    .Append("   |   enemies remaining: ").Append(remaining);
            }
            else
            {
                int shown = wave;
                if (shown > WaveDirector.MaxWaves) shown = WaveDirector.MaxWaves;
                _lineBuf.Append("ASSAULT — wave ").Append(shown).Append('/')
                    .Append(WaveDirector.MaxWaves).Append("   |   enemies remaining: ")
                    .Append(remaining);
            }
            _view.SetAssaultHeading(_lineBuf.ToString());
        }
    }
}
