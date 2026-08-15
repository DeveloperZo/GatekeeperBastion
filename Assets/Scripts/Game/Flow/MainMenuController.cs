using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: MainMenu front-end flow — CONTINUE / START NEW (overwrite
    /// confirm) / OPTIONS / QUIT. Continue and Start load
    /// <see cref="SceneFlow.Ship"/>; Embark to Bastion is a Ship-station action, not a menu
    /// action. Esc is not polled here — Shift's modal hotkeys own it on the menu.</summary>
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

        /// <summary>Home QUIT — one click. Overwrite still confirms; leaving the
        /// process does not.</summary>
        void Quit() => SceneFlow.QuitGame();

    }
}
