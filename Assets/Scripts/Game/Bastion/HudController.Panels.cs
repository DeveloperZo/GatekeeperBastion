using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): Prep / Assault centre strips, shortcut
    /// panel lines and end-screen pushes.</summary>
    public sealed partial class HudController
    {
        static readonly List<string> ShortcutLineBuf = new List<string>(16);

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
                _view.SetShortcutLines(null, true);
                return;
            }

            ShortcutPanel.FillLines(ctx, ShortcutLineBuf, dialLevel);
            _view.SetShortcutLines(ShortcutLineBuf, false);
        }

        void PushPrepPanel(RunSim sim)
        {
            if (sim.AllWavesCleared || sim.CurrentWaveIndex >= WaveDirector.MaxWaves)
            {
                _view.SetPrepHeading(
                    $"<b>BASTION STRIPPED</b> — all {WaveDirector.MaxWaves} waves cleared");
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
            else state = $"repair at the hatch ({rate:0} salvage/HP)";
            string hatchStat = $"HATCH {hatch.Hp:0}/{hatch.MaxHp:0}";
            _view.SetPrepLine(1, damaged
                ? $"<b>{hatchStat}</b> — damaged; {state}"
                : $"{hatchStat} — {state}");

            int lvl = sim.Siphon.Level;
            _view.SetPrepLine(2,
                $"SIPHON L{lvl} (x{sim.Siphon.RewardMultiplier:0.##}) — each kill +{sim.CurrentKillValue:0} salvage");

            int wave = sim.CurrentWaveIndex;
            int here = _waves.NextWave(wave, lvl).enemyCount;
            _view.SetPrepLine(3,
                lvl >= TetherSiphon.MaxLevel
                    ? $"dial at max (L{TetherSiphon.MaxLevel}) — step down for a smaller wave"
                    : $"raise to L{lvl + 1}: each kill +{sim.CurrentKillValue / TetherSiphon.RewardMultiplierFor(lvl) * TetherSiphon.RewardMultiplierFor(lvl + 1):0}, " +
                      $"and wave {wave + 1} brings {_waves.NextWave(wave, lvl + 1).enemyCount} gatekeepers instead of {here}");

            _view.SetPrepLine(4,
                $"the draw brings wave {wave + 1}/{WaveDirector.MaxWaves} — {here} gatekeepers");
        }

        void PushAssaultPanel(RunSim sim)
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<WaveSpawner>();
            int remaining = _spawner != null ? _spawner.AliveCount : 0;
            _view.SetAssaultHeading(
                $"ASSAULT — wave {sim.CurrentWaveIndex}/{WaveDirector.MaxWaves}   |   " +
                $"enemies remaining: {remaining}");

            if (_hatchAlert <= 0f)
            {
                _view.SetHatchAlarmVisible(false);
                return;
            }

            var hatch = sim.Ship.Hatch;
            _view.SetHatchAlarmStrip($"<b>HATCH UNDER ATTACK</b> — {hatch.Hp:0}/{hatch.MaxHp:0}");
            _view.SetHatchAlarmVisible(true);
        }
    }
}
