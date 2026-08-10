using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Splash front-end — CONTINUE / START NEW (overwrite confirm) /
    /// OPTIONS / QUIT. Continue and Start load <see cref="SceneFlow.Ship"/>; Embark to Bastion
    /// is a Ship-station action, not a menu action.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        bool _loading;
        MainMenuView _view;

        void Awake()
        {
            SceneFlow.EnsureEventSystem();
            SceneFlow.RepairCanvas(gameObject);
            AudioBuses.ApplyFromSettings();
            _view = GetComponentInChildren<MainMenuView>(true);
            if (_view == null) _view = GetComponent<MainMenuView>();
            if (_view == null) return;
            _view.EnsureBuilt();

            _view.onContinue = OnContinue;
            _view.onStartNew = OnStartNew;
            _view.onOptions = () => OptionsScreen.Open(null);
            _view.onQuit = () => Application.Quit();
            _view.onOverwriteConfirm = StartNewAndGoToShip;
            _view.onOverwriteBack = () => _view.ShowMain();
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
            if (_view != null && _view.OverwriteOpen && Input.GetKeyDown(KeyCode.Escape))
                _view.ShowMain();
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
            if (!SceneFlow.LoadShip())
                _loading = false;
        }

        void OnStartNew()
        {
            if (GameSession.HasSave)
            {
                _view.ShowOverwriteModal(
                    "Overwrite save?",
                    "Starting a new campaign will overwrite your existing save.");
                return;
            }
            StartNewAndGoToShip();
        }

        void StartNewAndGoToShip()
        {
            if (_loading) return;
            _loading = true;
            GameSession.StartNew();
            if (!SceneFlow.LoadShip())
                _loading = false;
        }
    }
}
