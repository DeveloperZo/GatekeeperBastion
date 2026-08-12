using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): the per-frame fight readout — score and
    /// wave strip, hatch and player vitals, stamina, interact prompt and damage vignette. Every
    /// push is change-gated on cached values so LateUpdate stays allocation-free.</summary>
    public sealed partial class HudController
    {
        long _cacheElapsed = long.MinValue;
        long _cacheWave = long.MinValue;
        long _cacheSiphon = long.MinValue;
        long _cacheKill = long.MinValue;
        long _cacheRunSalvage = long.MinValue;
        long _cacheBanked = long.MinValue;
        long _cacheHatchHp = long.MinValue;
        long _cacheHatchMax = long.MinValue;
        long _cachePlayerHp = long.MinValue;
        bool _cacheAlarm;
        bool _cachePlayerDead;

        void PushFightValues(GameController gc, RunSim sim)
        {
            Controls.TickPanelToggle();
            Controls.TickMissionDrawerToggle();
            bool leftOpen = !Controls.Collapsed;
            _view.SetLeftDrawerOpen(leftOpen);
            _view.SetRightDrawerOpen(Controls.MissionDrawerOpen);

            long elapsed = (long)sim.RunElapsed;
            long kill = sim.CurrentKillAward;
            long runSal = (long)sim.Salvage.RunSalvage;
            long banked = gc.Meta != null ? gc.Meta.BankedSalvage : 0L;

            if (elapsed != _cacheElapsed || sim.CurrentWaveIndex != _cacheWave
                || sim.Siphon.Level != _cacheSiphon || kill != _cacheKill
                || runSal != _cacheRunSalvage || banked != _cacheBanked)
            {
                _cacheElapsed = elapsed;
                _cacheWave = sim.CurrentWaveIndex;
                _cacheSiphon = sim.Siphon.Level;
                _cacheKill = kill;
                _cacheRunSalvage = runSal;
                _cacheBanked = banked;

                _view.SetProfile("LV 1", "EXP —");

                _lineBuf.Clear();
                _lineBuf.Append("Banked ").Append(banked);
                string salvage = _lineBuf.ToString();
                _lineBuf.Clear();
                _lineBuf.Append("Run ").Append(runSal)
                    .Append("  ·  Siphon L").Append(sim.Siphon.Level)
                    .Append("  ·  Wave ").Append(sim.CurrentWaveIndex).Append('/')
                    .Append(WaveDirector.MaxWaves);
                _view.SetScore(salvage, _lineBuf.ToString());
            }

            var hatch = sim.Ship.Hatch;
            long hatchHp = (long)hatch.Hp;
            long hatchMax = (long)hatch.MaxHp;
            bool alarm = _hatchAlert > 0f;
            bool alarmChanged = alarm != _cacheAlarm;
            if (alarmChanged) _cacheAlarm = alarm;
            if (hatchHp != _cacheHatchHp || hatchMax != _cacheHatchMax || alarmChanged)
            {
                _cacheHatchHp = hatchHp;
                _cacheHatchMax = hatchMax;

                _lineBuf.Clear();
                if (alarm) _lineBuf.Append("<b>HATCH — UNDER ATTACK</b>");
                else _lineBuf.Append("HATCH");
                string hatchLabel = _lineBuf.ToString();

                _lineBuf.Clear();
                _lineBuf.Append(hatchHp).Append('/').Append(hatchMax);
                _view.SetHatch(hatchLabel, _lineBuf.ToString(), hatch.Hp, hatch.MaxHp);
                _view.SetHatchAlert(alarm);

                // Toast is text-only — no second bar. Clears when the alert window ends.
                if (alarm) _view.SetPlayerMessage("<b>HATCH UNDER ATTACK</b>");
                else _view.SetPlayerMessage(null);
            }

            var player = Player;
            if (player != null)
            {
                long php = (long)player.Hp;
                bool dead = player.IsDead;
                if (dead != _cachePlayerDead)
                {
                    _cachePlayerDead = dead;
                    _view.SetPlayerLabel(dead ? "<b>HEALTH (DOWN)</b>" : "HEALTH");
                }
                if (php != _cachePlayerHp)
                {
                    _cachePlayerHp = php;
                    _view.SetPlayerBar(player.Hp, player.MaxHp);
                }
                PushStamina();
            }

            PushWeaponReadout(gc);
            if (leftOpen) PushShortcutPanel(sim);

            if (sim.Assault == AssaultState.Prep)
                PushPrepPanel(sim);
            else
                PushAssaultPanel(sim);

            PushCrosshair();
            PushPrompt();
            PushVignette();
        }

        void PushStamina()
        {
            if (_fpc == null)
            {
                ResolveRefs();
                if (_fpc == null)
                {
                    _view.SetStaminaBlockVisible(false);
                    return;
                }
            }

            // Always shown — Zo's bottom vitals band; hiding a full bar made the corner look empty.
            _view.SetStaminaBlockVisible(true);
            _view.SetStaminaLabel(_fpc.IsSprinting ? "<b>SPRINT</b>" : "STAMINA");
            _view.SetStaminaBar(_fpc.Stamina, _fpc.StaminaMax);
        }

        void PushPrompt()
        {
            if (_interactor == null || string.IsNullOrEmpty(_interactor.CurrentPrompt))
            {
                _view.SetPromptVisible(false);
                return;
            }

            _view.SetPrompt(_interactor.CurrentPrompt);
            _view.SetPromptVisible(true);
        }

        void PushVignette()
        {
            var player = Player;
            if (player == null)
            {
                _view.SetVignette(0f, false, null);
                return;
            }

            if (player.IsDead)
                _view.SetVignette(1f, true, "YOU ARE DOWN");
            else if (_vignette > 0f)
                _view.SetVignette(Mathf.Clamp01(_vignette / 0.35f) * 0.35f, false, null);
            else
                _view.SetVignette(0f, false, null);
        }
    }
}
