using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: MainMenu front-end flow — CONTINUE / REALMS / JOIN / OPTIONS /
    /// QUIT. CONTINUE re-enters the realm played last; REALMS opens the chooser. Both land on
    /// the co-op hull.
    ///
    /// There is no HOST here any more. Hosting needs a realm, and this screen cannot name one —
    /// it could only ever have hosted whichever realm happened to be opened last, which is a
    /// choice the player never made. It belongs to
    /// <see cref="RealmSelectController"/>, the one screen that knows the realm and the Boarder.
    /// JOIN stays, because joining is the one door that leads to somebody else's world and
    /// needs no realm of your own.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        const string Tag = "MainMenu";

        [SerializeField] MainMenuView _view;

        [SerializeField, Tooltip("Scene-local 'Background Music' AudioSource — never the DDOL host. Authored in MainMenu.unity.")]
        AudioSource _backgroundMusic;

        bool _loading;
        bool _busy;

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
            GameSession.ReleaseCurrent();
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

        /// <summary>Straight back into the realm played last. It opens the co-op ship for
        /// the same reason the chooser does: a realm is one world however you reached it, and
        /// landing in a different hull depending on which button you pressed would read as a
        /// bug (Docs/Milestones — the realm and the Boarder).</summary>
        void OnContinue()
        {
            if (_loading || !GameSession.HasSave) return;
            _loading = true;
            GameSession.ContinueGame();
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShipCoop())
                _loading = false;
        }

        /// <summary>Opens the realm chooser rather than starting a campaign on the spot.
        /// Starting fresh used to mean overwriting the one save on disk, which is why this
        /// asked to confirm; a realm is its own document, so making one destroys nothing and
        /// there is nothing left to confirm (Docs/Milestones — the realm and the Boarder).</summary>
        void OnStartNew()
        {
            if (_loading) return;
            _loading = true;
            if (!SceneFlow.LoadRealmSelect())
                _loading = false;
        }

        /// <summary>Kept for the authored overwrite modal's confirm button, which is wired in
        /// the menu scene and cannot be unwired from here. Nothing opens that modal any more,
        /// so this is only reachable if someone presses confirm on a dialog that no longer
        /// appears; sending them to the chooser is the honest answer.</summary>
        void StartNewAndGoToShip()
        {
            OnStartNew();
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
