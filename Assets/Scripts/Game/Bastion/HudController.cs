using System;
using System.Collections.Generic;
using System.Text;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Bastion-scene HUD controller. Reads sim state and pushes values
    /// into <see cref="BastionHudView"/>. Owns the Bastion Esc dispatcher (the Escape contract)
    /// and pause open/close without <c>Time.timeScale</c>.</summary>
    public sealed partial class HudController : MonoBehaviour
    {
        const string Tag = "HudController";
        const float HatchAlertSeconds = 2f;
        const float VirtualHudHeight = 900f;

        /// <summary>True while the Shift pause overlay is up — fight input must not fire under it.</summary>
        public static bool IsPauseOpen { get; private set; }

        [SerializeField, Tooltip("Bastion GameController. Authored in the scene.")]
        GameController _game;

        [SerializeField, Tooltip("Bastion wave spawner. Authored in the scene.")]
        WaveSpawner _spawner;

        [SerializeField, Tooltip("'Canvas - Pause Menu' root object. Authored in the scene.")]
        GameObject _pauseCanvas;

        [SerializeField, Tooltip("Every pause/settings modal the Esc dispatcher may need to close. Authored in the scene.")]
        ModalWindowManager[] _modals;

        [SerializeField, OptionalBinding, Tooltip("Authored ShortcutPanelView on Canvas - Pause Menu.")]
        ShortcutPanelView _pauseShortcuts;

        [SerializeField, OptionalBinding, Tooltip("Authored CompendiumView on Canvas - Pause Menu.")]
        CompendiumView _compendium;

        Interactor _interactor;
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
        readonly List<ShortcutRow> _shortcutRows = new List<ShortcutRow>(16);
        readonly List<ShortcutRow> _pauseShortcutRows = new List<ShortcutRow>(16);
        readonly StringBuilder _lineBuf = new StringBuilder(128);

        long _cacheAlive = long.MinValue;
        long _cacheAssaultWave = long.MinValue;
        long _cacheHatchBankSec = long.MinValue;
        long _cacheMasterLeft = long.MinValue;
        bool _missionDrawerAutoOpened;

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
            Controls.SetCollapsed(true);
            Controls.SetMissionDrawerOpen(false);
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
            _missionDrawerAutoOpened = false;
            _shownPhase = (RunPhase)(-1);
            _shownAssault = (AssaultState)(-1);
            Controls.SetCollapsed(true);
            Controls.SetMissionDrawerOpen(false);
            SubscribeSim();
        }

        void OnDisable() => UnsubscribeSim();

        void Start()
        {
            ResolveRefs();
            ApplyPhase(_game != null ? _game.Sim : null);
        }

        void ResolveRefs()
        {
            var player = Player;
            if (player == null) return;
            if (_gun == null) _gun = player.GetComponentInChildren<PlayerGun>();
            if (_ability == null && _gun != null) _ability = _gun.GetComponent<PlayerAbility>();
            if (_interactor == null) _interactor = player.GetComponentInChildren<Interactor>();
            if (_fpc == null) _fpc = player.GetComponent<FirstPersonController>();
        }

        void ResolvePauseHosts()
        {
            if (_pauseCanvas == null)
            {
                Log.Error(Tag, "Canvas - Pause Menu not found.");
                return;
            }
            _pauseAnimator = _pauseCanvas.GetComponent<Animator>();
            _pauseGroup = _pauseCanvas.GetComponent<CanvasGroup>();
            _pauseBlur = _pauseCanvas.GetComponentInChildren<BlurManager>(true);
        }

        void SubscribeSim()
        {
            var gc = _game;
            if (gc == null || gc.Sim == null || _subscribed) return;
            gc.Sim.PhaseChanged += OnSimPhaseChanged;
            gc.Sim.WaveStarted += OnWaveStarted;
            gc.Sim.AssaultEnded += OnAssaultEnded;
            _subscribed = true;
        }

        void UnsubscribeSim()
        {
            var gc = _game;
            if (!_subscribed || gc == null || gc.Sim == null) return;
            gc.Sim.PhaseChanged -= OnSimPhaseChanged;
            gc.Sim.WaveStarted -= OnWaveStarted;
            gc.Sim.AssaultEnded -= OnAssaultEnded;
            _subscribed = false;
        }

        void OnSimPhaseChanged(RunPhase phase)
        {
            _endPresented = false;
            ApplyPhase(_game != null ? _game.Sim : null);
        }

        void OnWaveStarted(WaveSpec _)
        {
            var sim = _game != null ? _game.Sim : null;
            if (sim == null || sim.Phase != RunPhase.Tethered) return;
            if (_shownAssault != AssaultState.Assault)
            {
                _shownAssault = AssaultState.Assault;
                if (_view != null) _view.ShowFight(prep: false);
            }
        }

        void OnAssaultEnded()
        {
            var sim = _game != null ? _game.Sim : null;
            if (sim == null || sim.Phase != RunPhase.Tethered) return;
            if (_shownAssault != AssaultState.Prep)
            {
                _shownAssault = AssaultState.Prep;
                if (_view != null) _view.ShowFight(prep: true);
            }
        }

        void Update()
        {
            if (_view == null) return;
            if (!_subscribed) SubscribeSim();
            ResolveRefs();
            OptionsScreen.TickDisplayRevert();
            TickEscape();
            TickClientEndDismiss();

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

        /// <summary>Client-only fight/assault lines read the mirror; host and solo read the sim.
        /// Other HUD fields stay on local state and are allowed to be wrong on a client.</summary>
        AssaultState ViewAssault(RunSim sim)
        {
            var net = _game != null ? _game.Net : null;
            if (net != null && net.IsClientOnly && net.Mirror != null)
                return (AssaultState)net.Mirror.Phase.Value;
            return sim.Assault;
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

            var gc = _game;
            var sim = gc != null ? gc.Sim : null;
            if (sim == null)
            {
                _lastHatchHp = -1f;
                return;
            }

            float hp = sim.Ship.Hatch.Hp;
            var net = gc.Net;
            if (net != null && net.IsClientOnly && net.Mirror != null)
                hp = net.Mirror.HatchHp.Value;
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
                // Terminal phases still hide fight chrome. Do not advance _shownPhase for
                // Tethered here — that skipped the once-per-Run mission-drawer latch forever.
                if (sim.Phase == RunPhase.Escaped || sim.Phase == RunPhase.Lost)
                {
                    _shownPhase = sim.Phase;
                    _view.ShowEnd();
                }
                else if (sim.Phase != RunPhase.Docked && sim.Phase != RunPhase.Tethered)
                {
                    _shownPhase = sim.Phase;
                    _view.ShowEmergency();
                }
                return;
            }

            if (sim.Phase == _shownPhase
                && (sim.Phase != RunPhase.Tethered || ViewAssault(sim) == _shownAssault))
                return;

            _shownPhase = sim.Phase;
            var assault = ViewAssault(sim);
            _shownAssault = assault;

            switch (sim.Phase)
            {
                case RunPhase.Docked:
                    // Docked is the start of a Run: re-arm the once-per-Run mission drawer.
                    _missionDrawerAutoOpened = false;
                    _view.ShowArriving();
                    break;
                case RunPhase.Tethered:
                    if (assault == AssaultState.Prep && !_missionDrawerAutoOpened)
                    {
                        _missionDrawerAutoOpened = true;
                        Controls.SetMissionDrawerOpen(true);
                    }
                    _view.ShowFight(assault == AssaultState.Prep);
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
            var gc = _game;
            if (gc == null || gc.Sim == null || _view == null) return;
            var sim = gc.Sim;

            try
            {
                if (TryPushClientEnd())
                    return;

                if (_shownPhase != sim.Phase
                    || (sim.Phase == RunPhase.Tethered && _shownAssault != ViewAssault(sim)))
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

        void OnReturnClicked()
        {
            var gc = _game;
            if (gc != null) gc.ReturnToShip();
        }

        static void ForceClearPauseFlag() => IsPauseOpen = false;
    }
}
