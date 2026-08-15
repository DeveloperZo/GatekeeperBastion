using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: MainMenu front-end flow — CONTINUE / START NEW (overwrite
    /// confirm) / HOST / JOIN / OPTIONS / QUIT. Continue and Start load
    /// <see cref="SceneFlow.Ship"/>; Host and Join load <see cref="SceneFlow.Bastion"/>.
    /// Esc is not polled here — Shift's modal hotkeys own it on the menu.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        const string Tag = "MainMenu";

        [SerializeField] MainMenuView _view;

        [SerializeField, Tooltip("Scene-local 'Background Music' AudioSource — never the DDOL host. Authored in MainMenu.unity.")]
        AudioSource _backgroundMusic;

        bool _loading;

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
            _view.SetCampaignStrip(banked, runs, deepest);
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

        /// <summary>Home HOST — continue the save if one exists, else a new campaign.
        /// No overwrite modal. Does not taint. Loads Bastion with a host intent.</summary>
        void OnHost()
        {
            if (_loading) return;
            _loading = true;
            LoadCampaignForNet();
            NetJoinIntent.LastError = null;
            NetJoinIntent.RequestHost();
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadBastion())
                _loading = false;
        }

        void OnJoin()
        {
            if (_loading) return;
            _view.ShowJoin();
        }

        void OnJoinBack()
        {
            if (_loading) return;
            _view.ShowMain();
        }

        /// <summary>Join panel Connect. Invalid IPv4 or port refuses here without loading
        /// Bastion. Valid: same campaign load as Host, then Bastion with a join intent.</summary>
        void OnJoinConnect()
        {
            if (_loading || _view == null) return;
            if (!NetJoinIntent.TryParseIpv4(_view.JoinAddressText, out string ip))
            {
                _view.SetJoinRefusal("Enter a dotted IPv4 address.");
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
            if (!SceneFlow.LoadBastion())
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
