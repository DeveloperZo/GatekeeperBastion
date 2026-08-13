using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
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

        static ControlContext FightShortcutContext(RunSim sim)
        {
            var ctx = Controls.FightAlways;
            ctx |= sim.Assault == AssaultState.Prep ? ControlContext.Prep : ControlContext.Assault;
            return ctx;
        }

        void PushShortcutPanel(RunSim sim, ControlContext ctx)
        {
            if ((ctx & Controls.FightAlways) != 0
                && _interactor != null
                && !string.IsNullOrEmpty(_interactor.CurrentPrompt))
                ctx |= ControlContext.NearInteract;

            int dialLevel = sim.Assault == AssaultState.Prep ? sim.Siphon.Level : -1;
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

        void PushPrepPanel(RunSim sim, int wave)
        {
            if (sim.AllWavesCleared || wave >= WaveDirector.MaxWaves)
            {
                _view.SetPrepHeading(
                    "<b>BASTION STRIPPED</b> — all " + WaveDirector.MaxWaves + " waves cleared");
                _view.SetPrepLine(1, "there is nothing left to draw");
                _view.SetPrepLine(2, null);
                _view.SetPrepLine(3, null);
                _view.SetPrepLine(4, null);
                return;
            }

            // Short card — siphon/currency live TR; wave lives TL. Prep points at field terminals.
            int lvl = sim.Siphon.Level;
            int here = _waves.NextWave(wave, lvl).enemyCount;
            _lineBuf.Clear();
            _lineBuf.Append("<b>PREP</b>  ·  Wave ").Append(wave + 1).Append('/')
                .Append(WaveDirector.MaxWaves).Append(" — use a field terminal");
            _view.SetPrepHeading(_lineBuf.ToString());

            var hatch = sim.Ship.Hatch;
            bool damaged = hatch.Hp < hatch.MaxHp - 0.01f;
            _lineBuf.Clear();
            _lineBuf.Append("HATCH ").Append(hatch.Hp.ToString("0")).Append('/')
                .Append(hatch.MaxHp.ToString("0"));
            if (damaged) _lineBuf.Append("  ·  damaged — repair at hatch");
            else _lineBuf.Append("  ·  sealed");
            _view.SetPrepLine(1, _lineBuf.ToString());

            _lineBuf.Clear();
            _lineBuf.Append(here).Append(" gatekeepers on the next terminal Wave");
            if (lvl < TetherSiphon.MaxLevel)
            {
                int nextCount = _waves.NextWave(wave, lvl + 1).enemyCount;
                _lineBuf.Append("  ·  [ ] raise L").Append(lvl + 1)
                    .Append(" → ").Append(nextCount);
            }
            _view.SetPrepLine(2, _lineBuf.ToString());
            _view.SetPrepLine(3, null);
            _view.SetPrepLine(4, null);
        }

        void PushAssaultPanel(int wave, int remaining)
        {
            if (remaining == _cacheAlive && wave == _cacheAssaultWave)
                return;
            _cacheAlive = remaining;
            _cacheAssaultWave = wave;
            _lineBuf.Clear();
            _lineBuf.Append("ASSAULT — wave ").Append(wave).Append('/')
                .Append(WaveDirector.MaxWaves).Append("   |   enemies remaining: ")
                .Append(remaining);
            _view.SetAssaultHeading(_lineBuf.ToString());
        }
    }
}
