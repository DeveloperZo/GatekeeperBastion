using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: MainMenu front-end flow — CONTINUE / START NEW (overwrite
    /// confirm) / HOST / JOIN / OPTIONS / QUIT. Solo Continue / Start New load Ship
    /// offline. Host / Join start a DDOL <see cref="NetSession"/> then party-load Ship.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        const string Tag = "MainMenu";

        [SerializeField] MainMenuView _view;

        [SerializeField, Tooltip("Scene-local 'Background Music' AudioSource — never the DDOL host. Authored in MainMenu.unity.")]
        AudioSource _backgroundMusic;

        bool _loading;
        bool _busy;
        RelayJoin.Payload _pendingRelay;
        string _pendingJoinCode;

        void Awake()
        {
            SceneFlow.EnsureEventSystem();
            AudioBuses.ApplyFromSettings();

            // Prefer the DDOL host from Splash. Only adopt this scene's source when cold-starting MainMenu.
            FrontEndMusic.CaptureOrJoin(_backgroundMusic);

            if (_view == null)
            {
                Log.Error(Tag,
                    "Missing serialized reference '_view' (wire it in MainMenu.unity).");
                return;
            }

            _view.onContinue = OnContinue;
            _view.onStartNew = OnStartNew;
            _view.onHost = OnHost;
            _view.onJoin = OnJoin;
            _view.onJoinConnect = OnJoinConnect;
            _view.onJoinBack = OnJoinBack;
            _view.onHostCopy = OnHostCopy;
            _view.onHostEnter = OnHostEnter;
            _view.onHostLan = OnHostLan;
            _view.onHostBack = OnJoinBack;
            _view.onOptions = () => OptionsScreen.Open(null);
            _view.onOverwriteConfirm = StartNewAndGoToShip;
            _view.onQuitConfirm = Quit;
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshMenu();
        }

        void Update()
        {
            OptionsScreen.TickDisplayRevert();
        }

        void RefreshMenu()
        {
            if (_view == null) return;
            _view.SetContinueVisible(GameSession.HasSave);
            MetaState meta = GameSession.Meta;
            int banked = meta != null ? (int)meta.BankedSalvage : 0;
            int runs = meta != null ? meta.TotalRuns : 0;
            int deepest = meta != null ? meta.DeepestWaveReached : 0;
            _view.SetCampaignStrip(banked, runs, deepest, ProductVersion.Display);
            if (!string.IsNullOrEmpty(NetJoinIntent.LastError))
            {
                _view.ShowJoin();
                _view.SetJoinRefusal(NetJoinIntent.LastError);
                return;
            }
            _view.ShowMain();
        }

        void OnContinue()
        {
            if (_loading || !GameSession.HasSave) return;
            _loading = true;
            GameSession.ContinueGame();
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShip())
                _loading = false;
        }

        void OnStartNew()
        {
            if (GameSession.HasSave)
            {
                _view.ShowOverwriteModal();
                return;
            }
            StartNewAndGoToShip();
        }

        void StartNewAndGoToShip()
        {
            if (_loading) return;
            _loading = true;
            GameSession.StartNew();
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShip())
                _loading = false;
        }

        /// <summary>Home HOST — allocate a Relay join code on the menu, then confirm.
        /// LAN fallback is on the Host panel when Relay is down.</summary>
        async void OnHost()
        {
            if (_loading || _busy || _view == null) return;
            _busy = true;
            _pendingRelay = default;
            _pendingJoinCode = null;
            _view.SetHostCode("");
            _view.SetHostLanVisible(false);
            _view.SetHostStatus("Requesting join code…");
            _view.ShowHost();
            RelayJoin.Result result = await RelayJoin.TryHost();
            _busy = false;
            if (!result.Ok)
            {
                NetJoinIntent.LastError = result.Error;
                _view.SetHostStatus(result.Error);
                _view.SetHostLanVisible(true);
                return;
            }
            _pendingRelay = result.Payload;
            _pendingJoinCode = result.JoinCode;
            _view.SetHostCode(result.JoinCode);
            GUIUtility.systemCopyBuffer = result.JoinCode;
            _view.SetHostStatus("Copied to clipboard.");
            _view.SetHostLanVisible(false);
        }

        void OnHostCopy()
        {
            if (string.IsNullOrEmpty(_pendingJoinCode)) return;
            GUIUtility.systemCopyBuffer = _pendingJoinCode;
            if (_view != null) _view.SetHostStatus("Copied to clipboard.");
        }

        void OnHostEnter()
        {
            if (_loading || _busy) return;
            if (!RelayJoin.HasPayload(_pendingRelay) || string.IsNullOrEmpty(_pendingJoinCode))
            {
                if (_view != null) _view.SetHostStatus("No join code yet — wait or Host on LAN.");
                return;
            }
            EnterSessionHost(relay: true);
        }

        void OnHostLan()
        {
            if (_loading || _busy) return;
            EnterSessionHost(relay: false);
        }

        void EnterSessionHost(bool relay)
        {
            var existing = NetSession.FindLive();
            if (existing != null && existing.Active)
            {
                if (_view != null)
                    _view.SetHostStatus("Already hosting — leave the session first.");
                Log.Warn(Tag, "Host refused — session already active.");
                return;
            }

            _loading = true;
            LoadCampaignForNet();
            if (!DemoProgress.MultiplayerUnlocked(GameSession.Meta))
            {
                _loading = false;
                Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                if (_view != null)
                    _view.SetHostStatus("Multiplayer unlocks after your first upgrade — play the opening solo.");
                return;
            }
            NetJoinIntent.LastError = null;
            NetJoinIntent.Clear();
            FrontEndMusic.Stop();

            var session = NetSession.CreateBootstrap();
            if (session == null)
            {
                _loading = false;
                if (_view != null) _view.SetHostStatus("Net session bootstrap missing.");
                return;
            }
            if (relay)
                session.StartHostRelay(_pendingRelay, _pendingJoinCode);
            else
                session.StartHost();
            if (!session.Active)
            {
                _loading = false;
                if (_view != null) _view.SetHostStatus("Host start failed.");
                return;
            }
            if (!session.LoadLobbyShip())
                _loading = false;
        }

        void OnJoin()
        {
            if (_loading || _busy) return;
            _view.ShowJoin();
        }

        void OnJoinBack()
        {
            if (_loading || _busy) return;
            _view.ShowMain();
        }

        /// <summary>Join panel Connect. A six-character code is the friends path and
        /// allocates on the menu. Empty code falls through to typed IPv4.</summary>
        async void OnJoinConnect()
        {
            if (_loading || _busy || _view == null) return;
            string codeText = _view.JoinCodeText;
            if (!string.IsNullOrEmpty(codeText) && codeText.Trim().Length > 0)
            {
                if (!NetJoinIntent.TryParseJoinCode(codeText, out string code))
                {
                    _view.SetJoinRefusal("Enter a six-character join code.");
                    return;
                }
                if (string.Equals(code, _pendingJoinCode, System.StringComparison.OrdinalIgnoreCase)
                    && RelayJoin.HasPayload(_pendingRelay))
                {
                    Log.Event(Tag, "join is this host's code — starting host");
                    EnterSessionHost(relay: true);
                    return;
                }
                _busy = true;
                _view.SetJoinRefusal("Joining…");
                RelayJoin.Result result = await RelayJoin.TryJoin(code);
                _busy = false;
                if (!result.Ok)
                {
                    NetJoinIntent.LastError = result.Error;
                    _view.SetJoinRefusal(result.Error);
                    return;
                }
                EnterSessionJoinRelay(result.Payload, result.JoinCode);
                return;
            }

            if (!NetJoinIntent.TryParseIpv4(_view.JoinAddressText, out string ip))
            {
                _view.SetJoinRefusal("Enter a join code, or a dotted IPv4 address.");
                return;
            }
            if (!NetJoinIntent.IsJoinableAddress(ip))
            {
                _view.SetJoinRefusal("0.0.0.0 is a listen address — use 127.0.0.1 on this machine or the host LAN IPv4.");
                return;
            }
            if (!NetJoinIntent.TryParsePort(_view.JoinPortText, out ushort port))
            {
                _view.SetJoinRefusal("Port must be 1–65535.");
                return;
            }

            EnterSessionJoinLan(ip, port);
        }

        void EnterSessionJoinRelay(RelayJoin.Payload payload, string joinCode)
        {
            var existing = NetSession.FindLive();
            if (existing != null && existing.Active)
            {
                if (_view != null)
                    _view.SetJoinRefusal("Already in a session — leave first.");
                Log.Warn(Tag, "Join refused — session already active.");
                return;
            }

            _loading = true;
            NetJoinIntent.LastError = null;
            LoadCampaignForNet();
            if (!DemoProgress.MultiplayerUnlocked(GameSession.Meta))
            {
                _loading = false;
                Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                if (_view != null)
                    _view.SetJoinRefusal("Multiplayer unlocks after your first upgrade — play the opening solo.");
                return;
            }
            NetJoinIntent.Clear();
            FrontEndMusic.Stop();

            var session = NetSession.CreateBootstrap();
            if (session == null)
            {
                _loading = false;
                if (_view != null) _view.SetJoinRefusal("Net session bootstrap missing.");
                return;
            }
            session.StartClientRelay(payload, joinCode);
            if (!session.Active)
                _loading = false;
            // Guest lands in the host's networked Ship via NetworkSceneManager.
        }

        void EnterSessionJoinLan(string ip, ushort port)
        {
            var existing = NetSession.FindLive();
            if (existing != null && existing.Active)
            {
                if (_view != null)
                    _view.SetJoinRefusal("Already in a session — leave first.");
                Log.Warn(Tag, "Join refused — session already active.");
                return;
            }

            _loading = true;
            NetJoinIntent.LastError = null;
            LoadCampaignForNet();
            if (!DemoProgress.MultiplayerUnlocked(GameSession.Meta))
            {
                _loading = false;
                Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                if (_view != null)
                    _view.SetJoinRefusal("Multiplayer unlocks after your first upgrade — play the opening solo.");
                return;
            }
            NetJoinIntent.Clear();
            FrontEndMusic.Stop();

            var session = NetSession.CreateBootstrap();
            if (session == null)
            {
                _loading = false;
                if (_view != null) _view.SetJoinRefusal("Net session bootstrap missing.");
                return;
            }
            session.StartClient(ip, port);
            if (!session.Active)
                _loading = false;
        }

        static void LoadCampaignForNet()
        {
            if (GameSession.HasSave) GameSession.ContinueGame();
            else GameSession.StartNew();
        }

        /// <summary>Home QUIT — one click. Overwrite still confirms; leaving the
        /// process does not.</summary>
        void Quit() => SceneFlow.QuitGame();
    }
}
