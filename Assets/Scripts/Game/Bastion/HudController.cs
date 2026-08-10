using System;
using System.Collections.Generic;
using System.Text;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Bastion-scene HUD controller. Reads sim state and pushes values
    /// into <see cref="BastionHudView"/>. Owns the Bastion Esc dispatcher (M35 Escape contract)
    /// and pause open/close without <c>Time.timeScale</c>.</summary>
    public sealed partial class HudController : MonoBehaviour
    {
        const string Tag = "HudController";
        const float HatchAlertSeconds = 2f;
        const float VirtualHudHeight = 900f;

        /// <summary>True while the Shift pause overlay is up — fight input must not fire under it.</summary>
        public static bool IsPauseOpen { get; private set; }

        Interactor _interactor;
        WaveSpawner _spawner;
        PlayerGun _gun;
        PlayerAbility _ability;
        FirstPersonController _fpc;
        BastionHudView _view;

        Animator _pauseAnimator;
        CanvasGroup _pauseGroup;
        BlurManager _pauseBlur;
        bool _pauseCapturedLock;
        CursorLockMode _pauseCapturedCursor;
        bool _pauseCapturedVisible;

        float _lastPlayerHp = -1f;
        float _vignette;
        float _lastHatchHp = -1f;
        float _hatchAlert;
        bool _pushFaulted;
        bool _subscribed;
        bool _endPresented;
        RunPhase _shownPhase = (RunPhase)(-1);
        AssaultState _shownAssault = (AssaultState)(-1);

        readonly WaveDirector _waves = new WaveDirector();
        readonly List<string> _weaponLines = new List<string>(8);
        readonly List<ShortcutRow> _shortcutRows = new List<ShortcutRow>(16);
        readonly StringBuilder _weaponBlock = new StringBuilder(256);
        readonly StringBuilder _lineBuf = new StringBuilder(128);

        long _cacheElapsed = long.MinValue;
        long _cacheWave = long.MinValue;
        long _cacheSiphon = long.MinValue;
        long _cacheKill = long.MinValue;
        long _cacheRunSalvage = long.MinValue;
        long _cacheBanked = long.MinValue;
        long _cacheHatchHp = long.MinValue;
        long _cacheHatchMax = long.MinValue;
        long _cachePlayerHp = long.MinValue;
        long _cacheAlive = long.MinValue;
        bool _cacheAlarm;
        bool _cachePlayerDead;

        PlayerHealth Player => PlayerRegistry.Local;

        void Awake()
        {
            _view = GetComponent<BastionHudView>();
            if (_view == null)
            {
                Log.Error(Tag, "BastionHudView missing on BastionHud — author and serialize it.");
                return;
            }
            _view.EnsureBuilt();
            _view.ReturnClicked += OnReturnClicked;
            ResolvePauseHosts();
        }

        void OnDestroy()
        {
            if (_view != null) _view.ReturnClicked -= OnReturnClicked;
            UnsubscribeSim();
            if (IsPauseOpen) ForceClearPauseFlag();
        }

        void OnEnable()
        {
            _pushFaulted = false;
            _hatchAlert = 0f;
            _lastHatchHp = -1f;
            _endPresented = false;
            _shownPhase = (RunPhase)(-1);
            _shownAssault = (AssaultState)(-1);
            SubscribeSim();
        }

        void OnDisable() => UnsubscribeSim();

        void Start()
        {
            ResolveRefs();
            ApplyPhase(GameController.Instance != null ? GameController.Instance.Sim : null);
        }

        void ResolveRefs()
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<WaveSpawner>();
            var player = Player;
            if (player == null) return;
            if (_gun == null) _gun = player.GetComponentInChildren<PlayerGun>();
            if (_ability == null && _gun != null) _ability = _gun.GetComponent<PlayerAbility>();
            if (_interactor == null) _interactor = player.GetComponentInChildren<Interactor>();
            if (_fpc == null) _fpc = player.GetComponent<FirstPersonController>();
        }

        void ResolvePauseHosts()
        {
            var pauseCanvas = GameObject.Find("Canvas - Pause Menu");
            if (pauseCanvas == null)
            {
                Log.Error(Tag, "Canvas - Pause Menu not found.");
                return;
            }
            _pauseAnimator = pauseCanvas.GetComponent<Animator>();
            _pauseGroup = pauseCanvas.GetComponent<CanvasGroup>();
            _pauseBlur = pauseCanvas.GetComponentInChildren<BlurManager>(true);
        }

        void SubscribeSim()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.Sim == null || _subscribed) return;
            gc.Sim.PhaseChanged += OnSimPhaseChanged;
            gc.Sim.WaveStarted += OnWaveStarted;
            gc.Sim.AssaultEnded += OnAssaultEnded;
            _subscribed = true;
        }

        void UnsubscribeSim()
        {
            var gc = GameController.Instance;
            if (!_subscribed || gc == null || gc.Sim == null) return;
            gc.Sim.PhaseChanged -= OnSimPhaseChanged;
            gc.Sim.WaveStarted -= OnWaveStarted;
            gc.Sim.AssaultEnded -= OnAssaultEnded;
            _subscribed = false;
        }

        void OnSimPhaseChanged(RunPhase phase)
        {
            _endPresented = false;
            ApplyPhase(GameController.Instance != null ? GameController.Instance.Sim : null);
        }

        void OnWaveStarted(WaveSpec _)
        {
            var sim = GameController.Instance != null ? GameController.Instance.Sim : null;
            if (sim == null || sim.Phase != RunPhase.Tethered) return;
            if (_shownAssault != AssaultState.Assault)
            {
                _shownAssault = AssaultState.Assault;
                _view?.ShowFight(prep: false);
            }
        }

        void OnAssaultEnded()
        {
            var sim = GameController.Instance != null ? GameController.Instance.Sim : null;
            if (sim == null || sim.Phase != RunPhase.Tethered) return;
            if (_shownAssault != AssaultState.Prep)
            {
                _shownAssault = AssaultState.Prep;
                _view?.ShowFight(prep: true);
            }
        }

        void Update()
        {
            if (_view == null) return;
            if (!_subscribed) SubscribeSim();
            ResolveRefs();
            OptionsScreen.TickDisplayRevert();
            TickEscape();

            var player = Player;
            if (player != null)
            {
                if (_lastPlayerHp >= 0f && player.Hp < _lastPlayerHp) _vignette = 0.35f;
                _lastPlayerHp = player.Hp;
            }
            if (_vignette > 0f) _vignette -= Time.deltaTime;
            TickHatchAlert();
        }

        void LateUpdate()
        {
            if (_view == null) return;
            PushValues();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!DevTaintBanner.IsVisible) return;
            float scale = Screen.height / VirtualHudHeight;
            Matrix4x4 prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DevTaintBanner.Draw(Screen.width / scale);
            GUI.matrix = prev;
        }
#endif

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

        void ApplyPhase(RunSim sim)
        {
            if (_view == null) return;
            if (sim == null)
            {
                _view.HideChrome();
                _shownPhase = (RunPhase)(-1);
                return;
            }

            if (OptionsScreen.IsOpen || IsPauseOpen)
            {
                if (sim.Phase == RunPhase.Tethered && _shownPhase != RunPhase.Tethered)
                {
                    _shownPhase = RunPhase.Tethered;
                    _shownAssault = sim.Assault;
                    _view.ShowFight(sim.Assault == AssaultState.Prep);
                }
                return;
            }

            if (sim.Phase == _shownPhase
                && (sim.Phase != RunPhase.Tethered || sim.Assault == _shownAssault))
                return;

            _shownPhase = sim.Phase;
            _shownAssault = sim.Assault;

            switch (sim.Phase)
            {
                case RunPhase.Docked:
                    _view.SetArriving("arriving at the bastion...");
                    _view.ShowArriving();
                    break;
                case RunPhase.Tethered:
                    _view.ShowFight(sim.Assault == AssaultState.Prep);
                    break;
                case RunPhase.Escaped:
                case RunPhase.Lost:
                    _view.ShowEnd();
                    break;
                default:
                    _view.ShowEmergency();
                    break;
            }
        }

        void PushValues()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.Sim == null || _view == null) return;
            var sim = gc.Sim;

            try
            {
                if (_shownPhase != sim.Phase
                    || (sim.Phase == RunPhase.Tethered && _shownAssault != sim.Assault))
                    ApplyPhase(sim);

                switch (sim.Phase)
                {
                    case RunPhase.Tethered:
                        PushFightValues(gc, sim);
                        break;
                    case RunPhase.Escaped:
                        if (!_endPresented)
                        {
                            _endPresented = true;
                            PushSuccess(gc, sim);
                        }
                        PushEndScreenShortcut();
                        break;
                    case RunPhase.Lost:
                        if (!_endPresented)
                        {
                            _endPresented = true;
                            PushLose(gc, sim);
                        }
                        PushEndScreenShortcut();
                        break;
                    case RunPhase.Docked:
                        break;
                    default:
                        if (!_endPresented)
                        {
                            _endPresented = true;
                            PushEmergencyExit();
                        }
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
                if (sim.Phase != RunPhase.Tethered && !_endPresented)
                {
                    _endPresented = true;
                    PushEmergencyExit();
                }
            }
        }

        void PushFightValues(GameController gc, RunSim sim)
        {
            long elapsed = (long)sim.RunElapsed;
            if (elapsed != _cacheElapsed || sim.CurrentWaveIndex != _cacheWave)
            {
                _cacheElapsed = elapsed;
                _cacheWave = sim.CurrentWaveIndex;
                _lineBuf.Clear();
                _lineBuf.Append("T+").Append(elapsed).Append("s    Wave ")
                    .Append(sim.CurrentWaveIndex).Append('/').Append(WaveDirector.MaxWaves);
                _view.SetTimeWave(_lineBuf.ToString());
            }

            long kill = (long)sim.CurrentKillValue;
            if (sim.Siphon.Level != _cacheSiphon || kill != _cacheKill)
            {
                _cacheSiphon = sim.Siphon.Level;
                _cacheKill = kill;
                _lineBuf.Clear();
                _lineBuf.Append("Siphon L").Append(sim.Siphon.Level)
                    .Append(" (x").Append(sim.Siphon.RewardMultiplier.ToString("0.##")).Append(")    ")
                    .Append("each kill: +").Append(kill);
                _view.SetSiphonLine(_lineBuf.ToString());
            }

            long runSal = (long)sim.Salvage.RunSalvage;
            long banked = gc.Meta != null ? gc.Meta.BankedSalvage : 0L;
            if (runSal != _cacheRunSalvage || banked != _cacheBanked)
            {
                _cacheRunSalvage = runSal;
                _cacheBanked = banked;
                _lineBuf.Clear();
                _lineBuf.Append("Run: ").Append(runSal).Append("    Banked: ").Append(banked);
                _view.SetSalvageLine(_lineBuf.ToString());
            }

            var hatch = sim.Ship.Hatch;
            long hatchHp = (long)hatch.Hp;
            long hatchMax = (long)hatch.MaxHp;
            bool alarm = _hatchAlert > 0f;
            if (alarm != _cacheAlarm)
            {
                _cacheAlarm = alarm;
                _view.SetHatchLabel(alarm ? "<b>HATCH — UNDER ATTACK</b>" : "HATCH");
            }
            if (hatchHp != _cacheHatchHp || hatchMax != _cacheHatchMax)
            {
                _cacheHatchHp = hatchHp;
                _cacheHatchMax = hatchMax;
                _view.SetHatchBar(hatch.Hp, hatch.MaxHp);
            }

            var player = Player;
            if (player != null)
            {
                long php = (long)player.Hp;
                bool dead = player.IsDead;
                if (dead != _cachePlayerDead)
                {
                    _cachePlayerDead = dead;
                    _view.SetPlayerLabel(dead ? "<b>YOU  (DOWN)</b>" : "YOU");
                }
                if (php != _cachePlayerHp)
                {
                    _cachePlayerHp = php;
                    _view.SetPlayerBar(player.Hp, player.MaxHp);
                }
                PushStamina();
            }

            PushWeaponReadout(gc);
            PushShortcutPanel(sim);

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

            bool spent = _fpc.Stamina < _fpc.StaminaMax - 0.01f;
            if (!spent && !_fpc.IsSprinting)
            {
                _view.SetStaminaBlockVisible(false);
                return;
            }

            _view.SetStaminaBlockVisible(true);
            _view.SetStaminaLabel(_fpc.IsSprinting ? "<b>SPRINT</b>" : "STAMINA");
            _view.SetStaminaBar(_fpc.Stamina, _fpc.StaminaMax);
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
                _lineBuf.Clear();
                _lineBuf.Append("CLIP ").Append(_gun.ClipRemaining).Append('/').Append(_gun.MagazineCapacity);
                _weaponLines.Add(_lineBuf.ToString());
            }

            if (_gun.UsingSalvageCutter)
            {
                _lineBuf.Clear();
                _lineBuf.Append("NO WEAPON — improvised cutter: ")
                    .Append(_gun.EffectiveDamage.ToString("0")).Append(" dmg.");
                _weaponLines.Add(_lineBuf.ToString());
                _weaponLines.Add("Buy the MK-1 at the ship's ARMORY.");
                FlushWeaponBlock();
                return;
            }

            string blocked = _gun.WeaponBlockedReason;
            if (!string.IsNullOrEmpty(blocked))
            {
                _lineBuf.Clear();
                _lineBuf.Append("HOLSTERED: ").Append(blocked);
                _weaponLines.Add(_lineBuf.ToString());
            }

            _weaponLines.Add(SlotStrip(_gun));

            _lineBuf.Clear();
            _lineBuf.Append(_gun.DisplayName).Append(": ")
                .Append(_gun.EffectiveDamage.ToString("0")).Append(" dmg | ")
                .Append(_gun.EffectiveFireRate.ToString("0.#")).Append("/s | clip ")
                .Append(_gun.MagazineCapacity);
            if (_gun.CritChance > 0f)
                _lineBuf.Append(" | ").Append((_gun.CritChance * 100f).ToString("0.#")).Append("% crit");
            _weaponLines.Add(_lineBuf.ToString());

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
            _lineBuf.Clear();
            _lineBuf.Append("DECOY  ").Append(bay.State.ChargesRemaining).Append(" left  ·  ");
            if (bay.State.IsLive)
                _lineBuf.Append("LIVE ").Append(bay.State.Hp.ToString("0")).Append('/')
                    .Append(bay.State.MaxHp.ToString("0"));
            else
                _lineBuf.Append("ready");
            lines.Add(_lineBuf.ToString());
        }

        void PushAbilityReadout(List<string> lines)
        {
            if (_ability == null && _gun != null) _ability = _gun.GetComponent<PlayerAbility>();
            if (_ability == null || !_ability.HasAbility)
            {
                _view.SetAbilityBlockVisible(false);
                return;
            }

            string name = _ability.AbilityName;
            if (_ability.RegenSurgeActive)
            {
                _view.SetAbilityBlockVisible(true);
                _lineBuf.Clear();
                _lineBuf.Append("<b>ABILITY ").Append(name).Append(" — SURGING</b>");
                _view.SetAbilityLabel(_lineBuf.ToString());
                _view.SetAbilityBar(_ability.CooldownProgress);
                return;
            }

            if (_ability.CooldownRemaining > 0.05f)
            {
                _view.SetAbilityBlockVisible(true);
                _lineBuf.Clear();
                _lineBuf.Append("ABILITY ").Append(name).Append("  ")
                    .Append(_ability.CooldownRemaining.ToString("0.0")).Append('s');
                _view.SetAbilityLabel(_lineBuf.ToString());
                _view.SetAbilityBar(_ability.CooldownProgress);
                return;
            }

            _view.SetAbilityBlockVisible(false);
            _lineBuf.Clear();
            _lineBuf.Append("ABILITY ").Append(name).Append("  [")
                .Append(Controls.DisplayOf(ControlId.WeaponAbility)).Append(']');
            if (_ability.PinBuildHits > 0)
                _lineBuf.Append("  [").Append(_ability.PinBuildHits).Append('/')
                    .Append(WeaponAbility.PinHitThreshold).Append(']');
            lines.Add(_lineBuf.ToString());
        }

        static string SlotStrip(PlayerGun gun)
        {
            return Mark(gun, WeaponSlot.Primary, 1) + "   " +
                   Mark(gun, WeaponSlot.Secondary, 2) + "   " +
                   Mark(gun, WeaponSlot.Drone, 3);
        }

        static string Mark(PlayerGun gun, WeaponSlot slot, int key)
        {
            bool live = gun.ActiveSlot == slot;
            string mark = live ? "▸" : "·";
            if (slot == WeaponSlot.Drone)
            {
                var bay = DecoyBay.Instance;
                if (bay == null || !bay.HasPayload)
                    return key + (live ? " ▸ —" : " — —");
                return key + " " + mark + " Decoy" + (bay.State.IsLive ? " live" : "");
            }
            if (!gun.SlotConfigured(slot))
                return key + (live ? " ▸ —" : " — —");
            string name = WeaponCatalog.Def(gun.WeaponInSlot(slot)).displayName;
            int sp = name.IndexOf(' ');
            string shortName = sp > 0 && sp < 6 ? name.Substring(0, sp)
                : (name.Length > 8 ? name.Substring(0, 8) : name);
            string paused = !live
                && !Settings.GetBool(SettingId.DiscardReloadOnSwap)
                && gun.SlotIsReloading(slot)
                ? " reload paused"
                : "";
            return key + " " + mark + " " + shortName + paused;
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

        void OnReturnClicked() => GameController.Instance?.ReturnToShip();

        static void ForceClearPauseFlag() => IsPauseOpen = false;
    }
}
