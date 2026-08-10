using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): Prep / Assault centre strips and
    /// shortcut-panel rows.</summary>
    public sealed partial class HudController
    {
        void PushShortcutPanel(RunSim sim) => PushShortcutPanel(sim, FightShortcutContext(sim));

        void PushEndScreenShortcut() =>
            PushShortcutPanel(GameController.Instance.Sim, ControlContext.EndScreen);

        static ControlContext FightShortcutContext(RunSim sim)
        {
            var ctx = Controls.FightAlways;
            ctx |= sim.Assault == AssaultState.Prep ? ControlContext.Prep : ControlContext.Assault;
            return ctx;
        }

        void PushShortcutPanel(RunSim sim, ControlContext ctx)
        {
            Controls.TickPanelToggle();

            if ((ctx & Controls.FightAlways) != 0
                && _interactor != null
                && !string.IsNullOrEmpty(_interactor.CurrentPrompt))
                ctx |= ControlContext.NearInteract;

            int dialLevel = sim.Assault == AssaultState.Prep ? sim.Siphon.Level : -1;
            if (Controls.Collapsed)
            {
                ShortcutPanel.EstimateCollapsedHeight();
                _view.SetShortcutRows(null, true);
                return;
            }

            ShortcutPanel.FillRows(ctx, _shortcutRows, dialLevel);
            _view.SetShortcutRows(_shortcutRows, false);
        }

        void PushPrepPanel(RunSim sim)
        {
            if (sim.AllWavesCleared || sim.CurrentWaveIndex >= WaveDirector.MaxWaves)
            {
                _view.SetPrepHeading(
                    "<b>BASTION STRIPPED</b> — all " + WaveDirector.MaxWaves + " waves cleared");
                _view.SetPrepLine(1, "there is nothing left to draw");
                _view.SetPrepLine(2, null);
                _view.SetPrepLine(3, null);
                _view.SetPrepLine(4, null);
                return;
            }

            _view.SetPrepHeading("<b>PREP</b> — Siphon Draw ready");

            var hatch = sim.Ship.Hatch;
            bool damaged = hatch.Hp < hatch.MaxHp - 0.01f;
            var meta = GameController.Instance != null ? GameController.Instance.Meta : null;
            long bank = meta != null ? meta.BankedSalvage : 0L;
            float rate = ShipSystems.RepairSalvagePerHp;
            string missing = meta != null ? meta.RepairMissingStep() : null;
            string state;
            if (!damaged) state = "sealed tight";
            else if (missing != null) state = missing;
            else if (bank <= 0) state = "no salvage to repair";
            else state = "repair at the hatch (" + rate.ToString("0") + " salvage/HP)";

            _lineBuf.Clear();
            _lineBuf.Append("HATCH ").Append(hatch.Hp.ToString("0")).Append('/')
                .Append(hatch.MaxHp.ToString("0"));
            string hatchStat = _lineBuf.ToString();
            _view.SetPrepLine(1, damaged
                ? "<b>" + hatchStat + "</b> — damaged; " + state
                : hatchStat + " — " + state);

            int lvl = sim.Siphon.Level;
            _lineBuf.Clear();
            _lineBuf.Append("SIPHON L").Append(lvl)
                .Append(" (x").Append(sim.Siphon.RewardMultiplier.ToString("0.##")).Append(") — each kill +")
                .Append(sim.CurrentKillValue.ToString("0")).Append(" salvage");
            _view.SetPrepLine(2, _lineBuf.ToString());

            int wave = sim.CurrentWaveIndex;
            int here = _waves.NextWave(wave, lvl).enemyCount;
            if (lvl >= TetherSiphon.MaxLevel)
            {
                _view.SetPrepLine(3,
                    "dial at max (L" + TetherSiphon.MaxLevel + ") — step down for a smaller wave");
            }
            else
            {
                float nextKill = sim.CurrentKillValue / TetherSiphon.RewardMultiplierFor(lvl)
                    * TetherSiphon.RewardMultiplierFor(lvl + 1);
                int nextCount = _waves.NextWave(wave, lvl + 1).enemyCount;
                _lineBuf.Clear();
                _lineBuf.Append("raise to L").Append(lvl + 1).Append(": each kill +")
                    .Append(nextKill.ToString("0")).Append(", and wave ").Append(wave + 1)
                    .Append(" brings ").Append(nextCount).Append(" gatekeepers instead of ")
                    .Append(here);
                _view.SetPrepLine(3, _lineBuf.ToString());
            }

            _lineBuf.Clear();
            _lineBuf.Append("the draw brings wave ").Append(wave + 1).Append('/')
                .Append(WaveDirector.MaxWaves).Append(" — ").Append(here).Append(" gatekeepers");
            _view.SetPrepLine(4, _lineBuf.ToString());
        }

        void PushAssaultPanel(RunSim sim)
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<WaveSpawner>();
            int remaining = _spawner != null ? _spawner.AliveCount : 0;
            if (remaining != _cacheAlive || sim.CurrentWaveIndex != _cacheWave)
            {
                _cacheAlive = remaining;
                _lineBuf.Clear();
                _lineBuf.Append("ASSAULT — wave ").Append(sim.CurrentWaveIndex).Append('/')
                    .Append(WaveDirector.MaxWaves).Append("   |   enemies remaining: ")
                    .Append(remaining);
                _view.SetAssaultHeading(_lineBuf.ToString());
            }

            if (_hatchAlert <= 0f)
            {
                _view.SetHatchAlarmVisible(false);
                return;
            }

            var hatch = sim.Ship.Hatch;
            _lineBuf.Clear();
            _lineBuf.Append("<b>HATCH UNDER ATTACK</b> — ")
                .Append(hatch.Hp.ToString("0")).Append('/').Append(hatch.MaxHp.ToString("0"));
            _view.SetHatchAlarmStrip(_lineBuf.ToString());
            _view.SetHatchAlarmVisible(true);
        }
    }
}
