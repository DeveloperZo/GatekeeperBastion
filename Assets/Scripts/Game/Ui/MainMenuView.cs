using System;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Binds the front end copied out of Shift's <c>Main Menu (Desktop).unity</c>.
    /// Panels are switched with <see cref="MainPanelManager.OpenPanel"/> and the overwrite
    /// prompt is Shift's <c>Exit</c> modal driven by <see cref="ModalWindowManager"/> — this
    /// class holds no layout, no colour and no rect maths (M35 criteria 6, 7, 17).</summary>
    /// <remarks>
    /// Names the scene must use, because they are how this binds when nothing is serialised:
    /// the panel that holds the four buttons is <c>Home</c>; the buttons are <c>Continue</c>,
    /// <c>Start New</c>, <c>Options</c> and <c>Quit</c>; the confirm modal is <c>Exit</c>,
    /// with <c>Confirm</c> and <c>Cancel</c> buttons.
    /// </remarks>
    public sealed class MainMenuView : MonoBehaviour
    {
        public const string HomePanel = "Home";

        const string CanvasRoot = "Canvas";
        const string ContinueName = "Continue";
        const string StartNewName = "Start New";
        const string OptionsName = "Options";
        const string QuitName = "Quit";
        const string ModalName = "Exit";
        const string ConfirmName = "Confirm";
        const string CancelName = "Cancel";

        [SerializeField] MainPanelManager _panels;
        [SerializeField] GameObject _continueButton;
        [SerializeField] TextMeshProUGUI _campaignLine;
        [SerializeField] Button _continueBtn;
        [SerializeField] Button _startNewBtn;
        [SerializeField] Button _optionsBtn;
        [SerializeField] Button _quitBtn;
        [SerializeField] ModalWindowManager _overwriteModal;
        [SerializeField] Button _overwriteConfirmBtn;
        [SerializeField] Button _overwriteCancelBtn;

        bool _resolved;
        bool _wired;

        public Action onContinue;
        public Action onStartNew;
        public Action onOptions;
        public Action onQuit;
        public Action onOverwriteConfirm;
        public Action onOverwriteBack;

        public bool OverwriteOpen { get; private set; }

        /// <summary>True once the Shift Canvas is present — the signal that the front end
        /// exists in this scene at all.</summary>
        public bool HasFrontEnd => _panels != null;

        public void EnsureBuilt()
        {
            Resolve();
            if (_wired) return;
            _wired = true;

            Wire(_continueBtn, () => onContinue?.Invoke());
            Wire(_startNewBtn, () => onStartNew?.Invoke());
            Wire(_optionsBtn, () => onOptions?.Invoke());
            Wire(_quitBtn, () => onQuit?.Invoke());
            Wire(_overwriteConfirmBtn, () =>
            {
                onOverwriteConfirm?.Invoke();
                ShowMain();
            });
            Wire(_overwriteCancelBtn, () =>
            {
                onOverwriteBack?.Invoke();
                ShowMain();
            });
        }

        void Resolve()
        {
            if (_resolved) return;
            if (_panels == null) _panels = UiBind.First<MainPanelManager>();
            if (_panels == null) return;
            _resolved = true;

            Transform canvas = UiBind.Root(CanvasRoot) ?? _panels.transform.root;
            Transform home = UiBind.Find(canvas, HomePanel) ?? canvas;

            if (_continueBtn == null) _continueBtn = UiBind.Get<Button>(home, ContinueName);
            if (_continueButton == null && _continueBtn != null)
                _continueButton = _continueBtn.gameObject;
            // Shift Home labels: Play → Start New, Settings → Options.
            if (_startNewBtn == null)
                _startNewBtn = UiBind.Get<Button>(home, StartNewName)
                    ?? UiBind.Get<Button>(home, "Play");
            if (_optionsBtn == null)
                _optionsBtn = UiBind.Get<Button>(home, OptionsName)
                    ?? UiBind.Get<Button>(home, "Settings");
            if (_quitBtn == null) _quitBtn = UiBind.Get<Button>(home, QuitName);

            if (_overwriteModal == null)
                _overwriteModal = UiBind.Get<ModalWindowManager>(canvas, ModalName);
            if (_overwriteModal != null)
            {
                var modal = _overwriteModal.transform;
                if (_overwriteConfirmBtn == null)
                    _overwriteConfirmBtn = UiBind.Get<Button>(modal, ConfirmName);
                if (_overwriteCancelBtn == null)
                    _overwriteCancelBtn = UiBind.Get<Button>(modal, CancelName);
            }
        }

        static void Wire(Button btn, Action handler)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => handler());
        }

        public void FocusDefault()
        {
            var es = EventSystem.current;
            if (es == null) return;
            Button target = _continueButton != null && _continueButton.activeInHierarchy
                ? _continueBtn
                : _startNewBtn;
            if (target != null) es.SetSelectedGameObject(target.gameObject);
        }

        public void SetCampaignStrip(int banked, int runs, int deepestWave)
        {
            EnsureBuilt();
            string deepest = deepestWave < 1 ? "1" : deepestWave.ToString();
            UiBind.SetText(_campaignLine,
                $"{banked} banked  —  {runs} run(s)  —  deepest Wave {deepest}");
        }

        public void SetContinueVisible(bool visible)
        {
            EnsureBuilt();
            UiBind.Show(_continueButton, visible);
        }

        /// <summary>Opens a panel by the name it is registered under in
        /// <see cref="MainPanelManager.panels"/>. The only way this class changes screens.</summary>
        public void OpenPanel(string panelName)
        {
            EnsureBuilt();
            if (_panels != null && !string.IsNullOrEmpty(panelName))
                _panels.OpenPanel(panelName);
        }

        public void ShowMain()
        {
            EnsureBuilt();
            OverwriteOpen = false;
            // ModalWindowOut starts a coroutine — only legal while the modal object is active.
            if (_overwriteModal != null && _overwriteModal.gameObject.activeInHierarchy)
                _overwriteModal.ModalWindowOut();
            OpenPanel(HomePanel);
            FocusDefault();
        }

        public void ShowOverwriteModal(string title, string body)
        {
            EnsureBuilt();
            OverwriteOpen = true;
            if (_overwriteModal != null)
            {
                _overwriteModal.useCustomTexts = true;
                UiBind.SetText(_overwriteModal.windowTitle, title);
                UiBind.SetText(_overwriteModal.windowDescription, body);
                _overwriteModal.ModalWindowIn();
            }

            var es = EventSystem.current;
            if (es != null && _overwriteCancelBtn != null)
                es.SetSelectedGameObject(_overwriteCancelBtn.gameObject);
        }

        /// <summary>Hides the whole front end so the walkable hub behind it becomes the game.</summary>
        public void SetVisible(bool visible)
        {
            Resolve();
            if (_panels == null) return;
            var canvas = UiBind.Root(CanvasRoot);
            UiBind.Show(canvas, visible);
        }
    }
}
