using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: MainMenu front-end flow — CONTINUE / START NEW (overwrite
    /// confirm) / HOST / JOIN / OPTIONS / QUIT. Continue, Start, Host and Join all load
    /// <see cref="SceneFlow.Ship"/>; Bastion is reached by EMBARK. Host EMBARKs first so
    /// <see cref="NetSession"/> is listening before the client EMBARKs.</summary>
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
            _view.SetCampaignStrip(banked, runs, deepest, Application.version);
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
            EnterBastionHost(relay: true);
        }

        void OnHostLan()
        {
            if (_loading || _busy) return;
            EnterBastionHost(relay: false);
        }

        void EnterBastionHost(bool relay)
        {
            _loading = true;
            LoadCampaignForNet();
            NetJoinIntent.LastError = null;
            if (relay)
                NetJoinIntent.RequestHostRelay(_pendingRelay, _pendingJoinCode);
            else
                NetJoinIntent.RequestHost();
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShip())
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
        /// allocates on the menu. Empty code falls through to typed IPv4. Invalid values
        /// refuse here without loading Bastion.</summary>
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
                _busy = true;
                _view.SetJoinRefusal("Joining…");
                RelayJoin.Result result = await RelayJoin.TryJoin(code);
                _busy = false;
                if (!result.Ok)
                {
                    _view.SetJoinRefusal(result.Error);
                    return;
                }
                EnterBastionJoinRelay(result.Payload, result.JoinCode);
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

            _loading = true;
            NetJoinIntent.LastError = null;
            LoadCampaignForNet();
            NetJoinIntent.RequestJoin(ip, port);
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShip())
                _loading = false;
        }

        void EnterBastionJoinRelay(RelayJoin.Payload payload, string joinCode)
        {
            _loading = true;
            NetJoinIntent.LastError = null;
            LoadCampaignForNet();
            NetJoinIntent.RequestJoinRelay(payload, joinCode);
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShip())
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
