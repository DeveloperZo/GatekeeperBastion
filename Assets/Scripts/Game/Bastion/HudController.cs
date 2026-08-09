using System;
using System.Collections.Generic;
using System.Text;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Bastion-scene HUD controller. Reads sim state and pushes strings
    /// into <see cref="BastionHudView"/> — no drawing here. Fight readouts, Prep/Assault strips,
    /// end screens and the emergency exit live across this class and its partials.</summary>
    /// <remarks>
    /// All mutation goes through GameController commands — this class only reads sim state.
    /// A push fault logs once and falls back to the emergency exit panel.
    /// DevTaintBanner stays IMGUI-only (DevConsole path); not drawn in uGUI.
    /// </remarks>
    public sealed partial class HudController : MonoBehaviour
    {
        const string Tag = "HudController";

        /// <summary>Seconds the hatch alarm stays lit after the last point of damage.</summary>
        const float HatchAlertSeconds = 2f;

        Interactor _interactor;
        WaveSpawner _spawner;
        PlayerGun _gun;
        FirstPersonController _fpc;
        BastionHudView _view;
        float _lastPlayerHp = -1f;
        float _vignette;
        float _lastHatchHp = -1f;
        float _hatchAlert;
        bool _pushFaulted;

        readonly WaveDirector _waves = new WaveDirector();
        readonly List<string> _weaponLines = new List<string>(8);
        readonly StringBuilder _weaponBlock = new StringBuilder(256);

        PlayerHealth Player => PlayerRegistry.Local;

        void Awake()
        {
            _view = GetComponent<BastionHudView>();
            if (_view == null) _view = gameObject.AddComponent<BastionHudView>();
            _view.EnsureBuilt();
            _view.ReturnClicked += OnReturnClicked;
        }

        void OnDestroy()
        {
            if (_view != null) _view.ReturnClicked -= OnReturnClicked;
        }

        void OnEnable()
        {
            _pushFaulted = false;
            _hatchAlert = 0f;
            _lastHatchHp = -1f;
        }

        void Start() => ResolveRefs();

        void ResolveRefs()
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<WaveSpawner>();
            var player = Player;
            if (player == null) return;
            if (_gun == null) _gun = player.GetComponentInChildren<PlayerGun>();
            if (_interactor == null) _interactor = player.GetComponentInChildren<Interactor>();
            if (_fpc == null) _fpc = player.GetComponent<FirstPersonController>();
        }

        void Update()
        {
            ResolveRefs();
            OptionsScreen.Tick(_fpc);
            var player = Player;
            if (player != null)
            {
                if (_lastPlayerHp >= 0f && player.Hp < _lastPlayerHp) _vignette = 0.35f;
                _lastPlayerHp = player.Hp;
            }
            if (_vignette > 0f) _vignette -= Time.deltaTime;
            TickHatchAlert();
        }

        void LateUpdate() => PushHud();

        void TickHatchAlert()
        {
            if (_hatchAlert > 0f) _hatchAlert -= Time.deltaTime;

            var gc = GameController.Instance;
            var sim = gc != null ? gc.Sim : null;
            if (sim == null)
            {
                _lastHatchHp = -1f;
                return;
            }

            float hp = sim.Ship.Hatch.Hp;
            if (_lastHatchHp >= 0f && hp < _lastHatchHp - 0.01f) _hatchAlert = HatchAlertSeconds;
            _lastHatchHp = hp;
        }

        void PushHud()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.Sim == null)
            {
                _view.HideAll();
                return;
            }

            var sim = gc.Sim;

            try
            {
                // DevTaintBanner.Draw is IMGUI-only — DevConsole still shows dev chrome.

                if (OptionsScreen.IsOpen)
                {
                    if (sim.Phase == RunPhase.Tethered) PushFight(gc, sim);
                    else _view.HideAll();
                    return;
                }

                switch (sim.Phase)
                {
                    case RunPhase.Docked:
                        _view.HideAll();
                        _view.SetArriving("arriving at the bastion...");
                        _view.SetArrivingVisible(true);
                        break;
                    case RunPhase.Tethered:
                        PushFight(gc, sim);
                        break;
                    case RunPhase.Escaped:
                        PushSuccess(gc, sim);
                        break;
                    default:
                        PushLose(gc, sim);
                        break;
                }
            }
            catch (Exception e)
            {
                if (!_pushFaulted)
                {
                    _pushFaulted = true;
                    Log.Error(Tag, $"HUD push threw {e.GetType().Name}: {e.Message}");
                }
                if (sim.Phase != RunPhase.Tethered) PushEmergencyExit();
            }
        }

        void PushFight(GameController gc, RunSim sim)
        {
            _view.HideAll();
            _view.SetFightVisible(true);
            _view.SetCrosshairVisible(true);
            _view.SetShortcutVisible(true);

            _view.SetTimeWave(
                $"T+{sim.RunElapsed:0}s    Wave {sim.CurrentWaveIndex}/{WaveDirector.MaxWaves}");
            _view.SetSiphonLine(
                $"Siphon L{sim.Siphon.Level} (x{sim.Siphon.RewardMultiplier:0.##})    " +
                $"each kill: +{sim.CurrentKillValue:0}");
            _view.SetSalvageLine(
                $"Run: {sim.Salvage.RunSalvage:0}    Banked: {gc.Meta?.BankedSalvage ?? 0}");

            var hatch = sim.Ship.Hatch;
            bool alarm = _hatchAlert > 0f;
            _view.SetHatchLabel(alarm ? "HATCH — UNDER ATTACK" : "HATCH", alarm);
            bool blink = alarm && Mathf.Repeat(_hatchAlert, 0.5f) > 0.25f;
            _view.SetHatchBar(hatch.Hp, hatch.MaxHp, blink ? Theme.HatchBlink : Theme.Accent);

            var player = Player;
            if (player != null)
            {
                _view.SetPlayerLabel(player.IsDead ? "YOU  (DOWN)" : "YOU");
                _view.SetPlayerBar(player.Hp, player.MaxHp);
                PushStamina();
            }

            PushWeaponReadout(gc);
            PushShortcutPanel(sim);
            float band = ShortcutPanel.LastDrawnHeight;

            if (sim.Assault == AssaultState.Prep)
            {
                PushPrepPanel(sim);
                _view.SetPrepVisible(true);
                _view.LayoutPrepStrip(band);
            }
            else
            {
                PushAssaultPanel(sim);
                _view.SetAssaultVisible(true);
                _view.LayoutAssaultStrip(band);
            }

            PushCrosshair();
            PushPrompt(band);
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

            bool spent = _fpc.Stamina < _fpc.StaminaMax - 0.01f;
            if (!spent && !_fpc.IsSprinting)
            {
                _view.SetStaminaBlockVisible(false);
                return;
            }

            _view.SetStaminaBlockVisible(true);
            _view.SetStaminaLabel(_fpc.IsSprinting ? "SPRINT" : "STAMINA");
            _view.SetStaminaBar(_fpc.Stamina, _fpc.StaminaMax, _fpc.IsSprinting);
        }

        void PushWeaponReadout(GameController gc)
        {
            _view.SetReloadBlockVisible(false);
            _view.SetAbilityBlockVisible(false);
            _weaponLines.Clear();

            if (_gun == null)
            {
                ResolveRefs();
                if (_gun == null)
                {
                    _view.SetWeaponBlock("NO WEAPON RIG FOUND");
                    return;
                }
            }

            if (_gun.ActiveSlot == WeaponSlot.Drone)
            {
                PushDecoyReadout(_weaponLines);
                _weaponLines.Add(SlotStrip(_gun));
                FlushWeaponBlock();
                return;
            }

            if (_gun.IsReloading)
            {
                _view.SetReloadBlockVisible(true);
                _view.SetReloadLabel("RELOADING");
                _view.SetReloadBar(_gun.ReloadProgress);
                if (Settings.GetBool(SettingId.DiscardReloadOnSwap) && _gun.HasAlternateWeapon)
                    _weaponLines.Add("SWAP DISCARDS THIS RELOAD");
            }
            else
            {
                _weaponLines.Add($"CLIP {_gun.ClipRemaining}/{_gun.MagazineCapacity}");
            }

            if (_gun.UsingSalvageCutter)
            {
                _weaponLines.Add($"NO WEAPON — improvised cutter: {_gun.EffectiveDamage:0} dmg.");
                _weaponLines.Add("Buy the MK-1 at the ship's ARMORY.");
                FlushWeaponBlock();
                return;
            }

            string blocked = _gun.WeaponBlockedReason;
            if (!string.IsNullOrEmpty(blocked))
                _weaponLines.Add($"HOLSTERED: {blocked}");

            _weaponLines.Add(SlotStrip(_gun));

            string crit = _gun.CritChance > 0f ? $" | {_gun.CritChance * 100f:0.#}% crit" : "";
            _weaponLines.Add(
                $"{_gun.DisplayName}: {_gun.EffectiveDamage:0} dmg | {_gun.EffectiveFireRate:0.#}/s | " +
                $"clip {_gun.MagazineCapacity}{crit}");

            PushAbilityReadout(_weaponLines);
            PushDecoyReadout(_weaponLines);
            FlushWeaponBlock();
        }

        void FlushWeaponBlock()
        {
            _weaponBlock.Clear();
            for (int i = 0; i < _weaponLines.Count; i++)
            {
                if (i > 0) _weaponBlock.Append('\n');
                _weaponBlock.Append(_weaponLines[i]);
            }
            _view.SetWeaponBlock(_weaponBlock.Length > 0 ? _weaponBlock.ToString() : null);
        }

        void PushDecoyReadout(List<string> lines)
        {
            var bay = DecoyBay.Instance;
            if (bay == null || !bay.HasPayload) return;
            string live = bay.State.IsLive
                ? $"LIVE {bay.State.Hp:0}/{bay.State.MaxHp:0}"
                : "ready";
            lines.Add($"DECOY  {bay.State.ChargesRemaining} left  ·  {live}");
        }

        void PushAbilityReadout(List<string> lines)
        {
            var ability = _gun != null ? _gun.GetComponent<PlayerAbility>() : null;
            if (ability == null || !ability.HasAbility)
            {
                _view.SetAbilityBlockVisible(false);
                return;
            }

            string name = ability.AbilityName;
            if (ability.RegenSurgeActive)
            {
                _view.SetAbilityBlockVisible(true);
                _view.SetAbilityLabel($"ABILITY {name} — SURGING");
                _view.SetAbilityBar(ability.CooldownProgress, Theme.AbilitySurge);
                return;
            }

            if (ability.CooldownRemaining > 0.05f)
            {
                _view.SetAbilityBlockVisible(true);
                _view.SetAbilityLabel($"ABILITY {name}  {ability.CooldownRemaining:0.0}s");
                _view.SetAbilityBar(ability.CooldownProgress, Theme.AbilityCooldown);
                return;
            }

            _view.SetAbilityBlockVisible(false);
            string pin = ability.PinBuildHits > 0
                ? $"  [{ability.PinBuildHits}/{WeaponAbility.PinHitThreshold}]"
                : "";
            lines.Add($"ABILITY {name}  [{Controls.DisplayOf(ControlId.WeaponAbility)}]{pin}");
        }

        static string SlotStrip(PlayerGun gun)
        {
            return $"{Mark(gun, WeaponSlot.Primary, 1)}   " +
                   $"{Mark(gun, WeaponSlot.Secondary, 2)}   " +
                   $"{Mark(gun, WeaponSlot.Drone, 3)}";
        }

        static string Mark(PlayerGun gun, WeaponSlot slot, int key)
        {
            bool live = gun.ActiveSlot == slot;
            string mark = live ? "▸" : "·";
            if (slot == WeaponSlot.Drone)
            {
                var bay = DecoyBay.Instance;
                if (bay == null || !bay.HasPayload)
                    return $"{key} {(live ? "▸" : "—")} —";
                string liveMark = bay.State.IsLive ? " live" : "";
                return $"{key} {mark} Decoy{liveMark}";
            }
            if (!gun.SlotConfigured(slot))
                return $"{key} {(live ? "▸" : "—")} —";
            string name = WeaponCatalog.Def(gun.WeaponInSlot(slot)).displayName;
            int sp = name.IndexOf(' ');
            string shortName = sp > 0 && sp < 6 ? name.Substring(0, sp) : (name.Length > 8 ? name.Substring(0, 8) : name);
            string paused = !live
                && !Settings.GetBool(SettingId.DiscardReloadOnSwap)
                && gun.SlotIsReloading(slot)
                ? " reload paused"
                : "";
            return $"{key} {mark} {shortName}{paused}";
        }

        void PushCrosshair()
        {
            WeaponCrosshair shape = _gun != null ? _gun.ActiveCrosshair : WeaponCrosshair.Cross;
            _view.SetCrosshair(shape switch
            {
                WeaponCrosshair.Dot => CrosshairShape.Dot,
                WeaponCrosshair.Brackets => CrosshairShape.Brackets,
                _ => CrosshairShape.Cross
            });
        }

        void PushPrompt(float shortcutHeight)
        {
            if (_interactor == null || string.IsNullOrEmpty(_interactor.CurrentPrompt))
            {
                _view.SetPromptVisible(false);
                return;
            }

            _view.SetPrompt(_interactor.CurrentPrompt);
            _view.LayoutPrompt(shortcutHeight);
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

        void OnReturnClicked()
        {
            GameController.Instance?.ReturnToShip();
        }
    }
}
