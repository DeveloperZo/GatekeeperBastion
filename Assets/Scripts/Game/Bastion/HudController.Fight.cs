using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): the per-frame fight readout — score and
    /// wave strip, hatch and player vitals, stamina, interact prompt and damage vignette. Every
    /// push is change-gated on cached values so LateUpdate stays allocation-free.</summary>
    public sealed partial class HudController
    {
        // Stamina is a continuous float that moves EVERY frame while sprinting and through the
        // regen tail, so the ungated push below wrote the Slider every one of those frames and
        // dirtied the HUD canvas for the whole sprint — the one per-frame HUD cost gated on
        // running (2026-08-18 defect). Quantised to whole points: 100 steps is finer than the
        // bar can show.
        long _cacheStamina = long.MinValue;
        int _cacheSprinting = -1;

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
        float _cacheCrewFill = -1f;
        float _cachePersonalFill = -1f;
        string _cacheCrewCdLabel;
        string _cachePersonalCdLabel;
        int _cacheHasProtocol = -1;
        // Protocol IDENTITY, not just its cooldown state. Without these the cache key was
        // (has, both fills, both countdown labels) — every one of which is identical between
        // two protocols that are simply both ready, so granting a new protocol from the range
        // console repainted nothing and the bar kept the old names until an ability was fired
        // and moved a fill. Enums, so the comparison is free.
        BoarderAbilityId _cacheCrewAbility = BoarderAbilityId.None;
        BoarderAbilityId _cachePersonalAbility = BoarderAbilityId.None;
        BoarderAbilityId _cachePassive = BoarderAbilityId.None;
        bool _cacheAlarm;
        bool _cachePlayerDead;

        void ShowFightHud(bool prep)
        {
            ResolveRefs();
            _cacheHasProtocol = -1;
            if (_view == null) return;
            _view.ShowFight(prep);
            PushProtocolChrome(_protocol != null && _protocol.HasProtocol);
        }

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

            // The carry is also what buys this seat back after a death, so the price rides
            // the same readout and the gate has to move when the price escalates.
            long respawnCost = sim.NextRespawnCost;
            if (elapsed != _cacheElapsed || wave != _cacheWave
                || siphon != _cacheSiphon || kill != _cacheKill
                || runSal != _cacheRunSalvage || banked != _cacheBanked
                || unbanked != _cacheUnbanked || respawnCost != _cacheRespawnCost)
            {
                _cacheElapsed = elapsed;
                _cacheWave = wave;
                _cacheSiphon = siphon;
                _cacheKill = kill;
                _cacheRunSalvage = runSal;
                _cacheBanked = banked;
                _cacheUnbanked = unbanked;
                _cacheRespawnCost = respawnCost;

                _view.SetProfile(BastionHudView.ProfileRank, null);

                _lineBuf.Clear();
                _lineBuf.Append("Unbanked ").Append(unbanked);
                // Silent while the seat can afford its own return; loud the moment it
                // cannot. Naming the shortfall only when it bites is what teaches the rule
                // without spending a line of the readout on it every frame.
                if (unbanked < respawnCost)
                    _lineBuf.Append(" (respawn needs ").Append(respawnCost).Append(')');
                _lineBuf.Append("  ·  Banked ").Append(banked);
                string salvage = _lineBuf.ToString();
                _lineBuf.Clear();
                int shown = wave;
                if (shown > WaveDirector.MaxWaves) shown = WaveDirector.MaxWaves;
                _lineBuf.Append("Boarding ").Append(runSal)
                    .Append("  ·  Ante ").Append(siphon)
                    .Append("  -  Wave ").Append(shown).Append('/')
                    .Append(WaveDirector.MaxWaves)
                    .Append("  -  ");
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
                // Label stays one word: the label TMP no longer wraps, and the full
                // "HATCH UNDER ATTACK" already runs on the player-message lane below.
                if (alarm) _lineBuf.Append("<b>HATCH!</b>");
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
                    _view.SetPlayerLabel(dead ? "<b>DOWN</b>" : "HEALTH");
                }
                if (php != _cachePlayerHp)
                {
                    _cachePlayerHp = php;
                    _view.SetPlayerBar(player.Hp, player.MaxHp);
                }
                PushStamina();
                PushProtocolReadout();
            }

            PushWeaponReadout(gc);
            if (leftOpen || IsPauseOpen) PushShortcutPanel(sim);

            if (assault == AssaultState.Prep)
                PushPrepPanel(sim, wave, hatchHp, hatchMax,
                    sim.TotalUnbanked > 0L ? sim.TotalUnbanked : sim.BankedSalvage);
            else
                PushAssaultPanel(sim, wave, alive);

            PushCrosshair();
            PushPrompt();
            PushFaucetLine(gc, sim);
            PushVignette();
        }

        // Faucet-loop status (the dev-gated graybox) on the unused general-message lane.
        // Change-gated on the whole faucet state tuple; with the toggle off the state is a
        // constant 0 and the lane is never touched after its one clearing push.
        long _cacheFaucetState = long.MinValue;

        void PushFaucetLine(GameController gc, RunSim sim)
        {
            long state = 0;
            if (sim.FaucetCycleLive || sim.FaucetAwaitingDraft)
            {
                state = 1
                    + (sim.FaucetAwaitingDraft ? 2L : 0L)
                    + (sim.FaucetMarked ? 4L : 0L)
                    + (sim.FaucetWavesEndured * 8L)
                    + (sim.FaucetDraftSize * 256L)
                    + (gc.FaucetDraftCursor * 4096L);
            }
            if (state == _cacheFaucetState) return;
            _cacheFaucetState = state;

            if (state == 0)
            {
                _view.SetGeneralMessage(null);
                return;
            }

            _lineBuf.Clear();
            if (sim.FaucetAwaitingDraft)
            {
                _lineBuf.Append("CHOOSE — [E] cycle  [T] confirm:  ");
                for (int i = 0; i < sim.FaucetDraftSize; i++)
                {
                    if (i > 0) _lineBuf.Append("  -  ");
                    bool on = i == gc.FaucetDraftCursor;
                    if (on) _lineBuf.Append("<b>> ");
                    _lineBuf.Append(FaucetLoop.AxisName((FaucetAxis)i));
                    if (on) _lineBuf.Append("</b>");
                }
            }
            else if (sim.FaucetMarked)
            {
                _lineBuf.Append("<b>FAUCET MARKED</b> — Terminal ")
                    .Append(gc.FaucetTerminalId)
                    .Append("  [T] shut off (draft ")
                    .Append(FaucetLoop.DraftSize(sim.FaucetWavesEndured)).Append(')');
            }
            else
            {
                _lineBuf.Append("FAUCET -- wave ").Append(sim.FaucetWavesEndured + 1)
                    .Append("  (off at ").Append(FaucetLoop.MarkAfterWaves).Append(')');
            }
            _view.SetGeneralMessage(_lineBuf.ToString());
        }

        void PushStamina()
        {
            if (_fpc == null)
            {
                ResolveRefs();
                if (_fpc == null)
                {
                    _view.SetStaminaBlockVisible(false);
                    _cacheStamina = long.MinValue; // re-arm: the next live frame must repaint
                    _cacheSprinting = -1;
                    return;
                }
            }

            // Always shown — Zo's bottom vitals band; hiding a full bar made the corner look empty.
            _view.SetStaminaBlockVisible(true);

            long points = (long)_fpc.Stamina;
            int sprinting = _fpc.IsSprinting ? 1 : 0;
            if (points == _cacheStamina && sprinting == _cacheSprinting) return;
            _cacheStamina = points;
            _cacheSprinting = sprinting;

            _view.SetStaminaLabel(sprinting == 1 ? "<b>SPRINT</b>" : "STAMINA");
            _view.SetStaminaBar(_fpc.Stamina, _fpc.StaminaMax);
        }

        void PushProtocolReadout()
        {
            if (_protocol == null)
            {
                ResolveRefs();
                if (_protocol == null)
                {
                    if (_cacheHasProtocol != 0)
                    {
                        _cacheHasProtocol = 0;
                        PushProtocolChrome(false);
                    }
                    return;
                }
            }

            int has = _protocol.HasProtocol ? 1 : 0;
            BoarderAbilityId crewAbility = _protocol.CrewAbility;
            BoarderAbilityId personalAbility = _protocol.PersonalAbility;
            BoarderAbilityId passive = _protocol.Passive;
            float crewFill = BoarderProtocol.OverlayFill(_protocol.CrewWindow, _protocol.CrewCooldown,
                BoarderProtocol.Window(_protocol.CrewAbility),
                BoarderProtocol.Cooldown(_protocol.CrewAbility));
            float persFill = BoarderProtocol.OverlayFill(_protocol.PersonalWindow, _protocol.PersonalCooldown,
                BoarderProtocol.Window(_protocol.PersonalAbility),
                BoarderProtocol.Cooldown(_protocol.PersonalAbility));
            string crewCd = CdLabel(_protocol.CrewWindow, _protocol.CrewCooldown);
            string persCd = CdLabel(_protocol.PersonalWindow, _protocol.PersonalCooldown);
            if (has == _cacheHasProtocol && crewFill == _cacheCrewFill && persFill == _cachePersonalFill
                && crewCd == _cacheCrewCdLabel && persCd == _cachePersonalCdLabel
                && crewAbility == _cacheCrewAbility && personalAbility == _cachePersonalAbility
                && passive == _cachePassive)
                return;
            _cacheHasProtocol = has;
            _cacheCrewAbility = crewAbility;
            _cachePersonalAbility = personalAbility;
            _cachePassive = passive;
            _cacheCrewFill = crewFill;
            _cachePersonalFill = persFill;
            _cacheCrewCdLabel = crewCd;
            _cachePersonalCdLabel = persCd;
            PushProtocolChrome(has != 0);
        }

        void PushProtocolChrome(bool equipped)
        {
            if (!equipped)
            {
                _view.SetProtocolLine("NO PROTOCOL");
                _view.SetProtocolAbilities(
                    "--", 0f, "",
                    Controls.DisplayOf(ControlId.PersonalAbility), "--", 0f, "", false,
                    Controls.DisplayOf(ControlId.CrewAbility), "--", 0f, "", false);
                return;
            }

            string proto = _protocol.ProtocolName;
            _view.SetProtocolLine(string.IsNullOrEmpty(proto) ? "NO PROTOCOL" : proto);
            string passive = _protocol.PassiveName;
            _view.SetProtocolAbilities(
                string.IsNullOrEmpty(passive) ? "--" : passive, 0f, "",
                Controls.DisplayOf(ControlId.PersonalAbility),
                _protocol.PersonalName,
                BoarderProtocol.OverlayFill(_protocol.PersonalWindow, _protocol.PersonalCooldown,
                    BoarderProtocol.Window(_protocol.PersonalAbility),
                    BoarderProtocol.Cooldown(_protocol.PersonalAbility)),
                CdLabel(_protocol.PersonalWindow, _protocol.PersonalCooldown),
                _protocol.PersonalWindow > 0f,
                Controls.DisplayOf(ControlId.CrewAbility),
                _protocol.CrewName,
                BoarderProtocol.OverlayFill(_protocol.CrewWindow, _protocol.CrewCooldown,
                    BoarderProtocol.Window(_protocol.CrewAbility),
                    BoarderProtocol.Cooldown(_protocol.CrewAbility)),
                CdLabel(_protocol.CrewWindow, _protocol.CrewCooldown),
                _protocol.CrewWindow > 0f);
        }

        static readonly string[] SecLabel = BuildSecLabel();

        static string[] BuildSecLabel()
        {
            var a = new string[61];
            for (int i = 0; i < a.Length; i++) a[i] = i.ToString();
            return a;
        }

        static string CdLabel(float window, float cooldown)
        {
            const float eps = 0.05f;
            if (window > eps) return SecAt(window);
            if (cooldown > eps) return SecAt(cooldown);
            return "";
        }

        static string SecAt(float seconds)
        {
            int n = (int)Mathf.Ceil(seconds);
            if (n < 0) n = 0;
            if (n < SecLabel.Length) return SecLabel[n];
            return n.ToString();
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
                _view.SetVignette(1f, true, DownText());
            else if (_vignette > 0f)
                _view.SetVignette(Mathf.Clamp01(_vignette / 0.35f) * 0.35f, false, null);
            else
                _view.SetVignette(0f, false, null);
        }

        // Cache key for the DOWN overlay line — rebuilt only when the offer changes, so the
        // per-frame vignette push stays allocation-free like every other push here.
        long _cacheRespawnCost = long.MinValue;
        long _cacheDownCost = long.MinValue;
        long _cacheDownCarry = long.MinValue;
        int _cacheDownSecondsLeft = int.MinValue;
        string _cacheDownReason;
        string _cacheDownText;

        /// <summary>The bleed-out countdown, or empty when nobody is on the clock. Trailing
        /// space is deliberate — it lets the caller concatenate without a second branch.</summary>
        static string BleedClock(GameController gc)
        {
            var sim = gc != null ? gc.Sim : null;
            if (sim == null || !sim.BleedOutLive) return string.Empty;
            return ((int)System.Math.Ceiling(sim.BleedOutRemaining)) + "s -- ";
        }

        /// <summary>DOWN overlay line. Host and solo carry the respawn offer or its refuse
        /// reason (the sim adjudicates); a guest keeps the plain line — its surface is the
        /// Ship wait.</summary>
        string DownText()
        {
            var gc = _game;
            var sim = gc != null ? gc.Sim : null;
            var net = gc != null ? gc.Net : null;
            if (sim == null || sim.Phase != RunPhase.Tethered
                || (net != null && net.IsClientOnly))
                return "YOU ARE DOWN";

            string reason = sim.RespawnRefuse(0);
            long cost = sim.NextRespawnCost;
            long carry = sim.BankedSalvage;
            // The bleed clock is part of the key: without it the line would be built once and
            // the countdown would sit frozen on the second it happened to start at. Keyed on
            // WHOLE seconds, so this still rebuilds ten times a death rather than every frame
            // and the push stays allocation-free in between.
            int secondsLeft = (int)System.Math.Ceiling(sim.BleedOutRemaining);
            if (cost != _cacheDownCost || reason != _cacheDownReason || carry != _cacheDownCarry
                || secondsLeft != _cacheDownSecondsLeft)
            {
                _cacheDownCost = cost;
                _cacheDownReason = reason;
                _cacheDownCarry = carry;
                _cacheDownSecondsLeft = secondsLeft;
                // Name the purse as well as the price: the fee comes out of the BANK and the
                // rest of it survives, which is the whole rule and is not guessable from a
                // bare number. Kills bank as they land, so this is also the running total of
                // what the Run has already secured — the answer to "is dying here worth it".
                _cacheDownText = reason == null
                    ? "YOU ARE DOWN -- " + BleedClock(gc) + "ENTER: RESPAWN ("
                      + cost + " of your " + carry + " banked)"
                    : "YOU ARE DOWN -- " + BleedClock(gc) + reason
                      + " (" + cost + " needed, banked " + carry + ")";
            }
            return _cacheDownText;
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
