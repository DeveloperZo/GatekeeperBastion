using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: MainMenu front-end flow — CONTINUE / START NEW (overwrite
    /// confirm) / OPTIONS / QUIT (exit confirm, M35 criterion 12). Continue and Start load
    /// <see cref="SceneFlow.Ship"/>; Embark to Bastion is a Ship-station action, not a menu
    /// action. Esc is not polled here — Shift's modal hotkeys own it on the menu.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        const string Tag = "MainMenu";

        [SerializeField] MainMenuView _view;

        bool _loading;

        void Awake()
        {
            SceneFlow.EnsureEventSystem();
            AudioBuses.ApplyFromSettings();

            // Prefer the DDOL host from Splash. Only adopt this scene's source when cold-starting MainMenu.
            FrontEndMusic.CaptureOrJoin(FindSceneBackgroundMusic());

            if (_view == null)
            {
                Log.Error(Tag,
                    "Missing serialized reference '_view' (M35 criterion 17 — wire it in MainMenu.unity).");
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

        /// <summary>Exit-modal confirm (M35 criterion 12): quits the build, stops Play mode
        /// in the editor.</summary>
        void Quit()
        {
            Log.Event(Tag, "Quit confirmed from the menu.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Scene-local Background Music only — never the DDOL host (Find is
        /// ambiguous when both exist and would hand CaptureOrJoin the wrong source).</summary>
        static AudioSource FindSceneBackgroundMusic()
        {
            var all = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var src = all[i];
                if (src == null || src.gameObject.name != "Background Music") continue;
                string sceneName = src.gameObject.scene.name;
                if (string.IsNullOrEmpty(sceneName) || sceneName == "DontDestroyOnLoad")
                    continue;
                return src;
            }
            return null;
        }
    }
}
