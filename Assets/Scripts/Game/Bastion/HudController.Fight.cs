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
        long _cacheUnbanked = long.MinValue;
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

            ReadFightReadout(sim, out AssaultState assault, out int wave, out int alive,
                out long runSal, out int elapsed, out float hatchHp, out float hatchMax);
            long kill = sim.CurrentKillAward;
            long banked = gc.Meta != null ? gc.Meta.BankedSalvage : 0L;
            long unbanked = sim.Unbanked;
            int siphon = sim.Siphon.Level;
            var net = gc.Net;
            if (net != null && net.IsClientOnly)
            {
                var mirror = net.Mirror;
                if (mirror != null)
                {
                    banked = mirror.Banked.Value;
                    siphon = mirror.Intensity.Value;
                }
                var avatar = NetPlayerAvatar.LocalOwner;
                if (avatar != null)
                    unbanked = avatar.Unbanked.Value;
                else if (mirror != null)
                    unbanked = mirror.Unbanked.Value;
            }

            if (elapsed != _cacheElapsed || wave != _cacheWave
                || siphon != _cacheSiphon || kill != _cacheKill
                || runSal != _cacheRunSalvage || banked != _cacheBanked
                || unbanked != _cacheUnbanked)
            {
                _cacheElapsed = elapsed;
                _cacheWave = wave;
                _cacheSiphon = siphon;
                _cacheKill = kill;
                _cacheRunSalvage = runSal;
                _cacheBanked = banked;
                _cacheUnbanked = unbanked;

                _view.SetProfile(BastionHudView.ProfileRank, null);

                _lineBuf.Clear();
                _lineBuf.Append("Unbanked ").Append(unbanked)
                    .Append("  ·  Banked ").Append(banked);
                string salvage = _lineBuf.ToString();
                _lineBuf.Clear();
                int shown = wave;
                if (shown > WaveDirector.MaxWaves) shown = WaveDirector.MaxWaves;
                _lineBuf.Append("Run ").Append(runSal)
                    .Append("  ·  Intensity ").Append(siphon)
                    .Append("  ·  Wave ").Append(shown).Append('/')
                    .Append(WaveDirector.MaxWaves)
                    .Append("  ·  ");
                _lineBuf.Append(elapsed / 60).Append(':');
                int sec = elapsed % 60;
                if (sec < 10) _lineBuf.Append('0');
                _lineBuf.Append(sec);
                _view.SetScore(salvage, _lineBuf.ToString());
            }

            long hatchHpI = (long)hatchHp;
            long hatchMaxI = (long)hatchMax;
            bool alarm = _hatchAlert > 0f;
            bool alarmChanged = alarm != _cacheAlarm;
            if (alarmChanged) _cacheAlarm = alarm;
            if (hatchHpI != _cacheHatchHp || hatchMaxI != _cacheHatchMax || alarmChanged)
            {
                _cacheHatchHp = hatchHpI;
                _cacheHatchMax = hatchMaxI;

                _lineBuf.Clear();
                if (alarm) _lineBuf.Append("<b>HATCH — UNDER ATTACK</b>");
                else _lineBuf.Append("HATCH");
                string hatchLabel = _lineBuf.ToString();

                _lineBuf.Clear();
                _lineBuf.Append(hatchHpI).Append('/').Append(hatchMaxI);
                _view.SetHatch(hatchLabel, _lineBuf.ToString(), hatchHp, hatchMax);
                _view.SetHatchAlert(alarm);

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

            if (assault == AssaultState.Prep)
                PushPrepPanel(sim, wave, hatchHp, hatchMax, sim.TotalUnbanked);
            else
                PushAssaultPanel(sim, wave, alive);

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

        void ReadFightReadout(RunSim sim, out AssaultState assault, out int wave, out int alive,
            out long runSal, out int elapsed, out float hatchHp, out float hatchMax)
        {
            assault = ViewAssault(sim);
            wave = sim.CurrentWaveIndex;
            alive = _spawner != null ? _spawner.AliveCount : 0;
            runSal = sim.Salvage != null ? sim.Salvage.RunSalvage : 0L;
            elapsed = (int)sim.RunElapsed;
            hatchHp = 0f;
            hatchMax = 0f;
            if (sim.Ship != null)
            {
                hatchHp = sim.Ship.Hatch.Hp;
                hatchMax = sim.Ship.Hatch.MaxHp;
            }

            var net = _game != null ? _game.Net : null;
            if (net == null || !net.IsClientOnly) return;
            var mirror = net.Mirror;
            if (mirror == null) return;
            assault = (AssaultState)mirror.Phase.Value;
            wave = mirror.Wave.Value;
            alive = mirror.Alive.Value;
            runSal = mirror.Salvage.Value;
            elapsed = mirror.ElapsedSeconds.Value;
            hatchHp = mirror.HatchHp.Value;
            hatchMax = mirror.HatchMax.Value;
        }
    }
}
