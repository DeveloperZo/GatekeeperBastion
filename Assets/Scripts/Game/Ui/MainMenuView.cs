using System;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: binds the authored Shift menu in <c>MainMenu.unity</c>. All wiring is by
    /// serialized reference — a null reference logs one error naming the
    /// field at scene load instead of silently no-opping. Panels switch through
    /// <see cref="MainPanelManager.OpenPanel"/>; the confirm modals are authored duplicates of
    /// Shift's <c>Exit</c> modal whose title/description text lives in the scene.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        public const string HomePanel = "Home";

        [SerializeField] MainPanelManager _panels;
        [SerializeField, Tooltip("Shift 'Main Panels' root. Authored in MainMenu.unity.")]
        GameObject _mainPanelsRoot;
        [SerializeField] GameObject _continueButton;
        [SerializeField] TextMeshProUGUI _campaignLine;
        [SerializeField] Button _continueBtn;
        [SerializeField] Button _startNewBtn;
        [SerializeField] Button _optionsBtn;
        [SerializeField] Button _quitBtn;
        [SerializeField] ModalWindowManager _overwriteModal;
        [SerializeField] Button _overwriteConfirmBtn;
        [SerializeField] Button _overwriteCancelBtn;
        [SerializeField] ModalWindowManager _exitModal;
        [SerializeField] Button _exitConfirmBtn;
        [SerializeField] Button _exitCancelBtn;
        [SerializeField] BlurManager _modalBlur;

        public Action onContinue;
        public Action onStartNew;
        public Action onOptions;
        public Action onOverwriteConfirm;
        public Action onQuitConfirm;

        void Awake()
        {
            ValidateBindings();

            Wire(_continueBtn, () => { if (onContinue != null) onContinue.Invoke(); });
            Wire(_startNewBtn, () => { if (onStartNew != null) onStartNew.Invoke(); });
            Wire(_optionsBtn, () => { if (onOptions != null) onOptions.Invoke(); });
            Wire(_quitBtn, () => { if (onQuitConfirm != null) onQuitConfirm.Invoke(); });
            Wire(_overwriteConfirmBtn, () =>
            {
                HideModal(_overwriteModal);
                if (onOverwriteConfirm != null) onOverwriteConfirm.Invoke();
            });
            Wire(_overwriteCancelBtn, () =>
            {
                HideModal(_overwriteModal);
                FocusDefault();
            });
            Wire(_exitConfirmBtn, () => { if (onQuitConfirm != null) onQuitConfirm.Invoke(); });
            Wire(_exitCancelBtn, () =>
            {
                HideModal(_exitModal);
                FocusDefault();
            });
        }

        void ValidateBindings()
        {
            Require(_panels, nameof(_panels));
            Require(_mainPanelsRoot, nameof(_mainPanelsRoot));
            Require(_continueButton, nameof(_continueButton));
            Require(_campaignLine, nameof(_campaignLine));
            Require(_continueBtn, nameof(_continueBtn));
            Require(_startNewBtn, nameof(_startNewBtn));
            Require(_optionsBtn, nameof(_optionsBtn));
            Require(_quitBtn, nameof(_quitBtn));
            Require(_overwriteModal, nameof(_overwriteModal));
            Require(_overwriteConfirmBtn, nameof(_overwriteConfirmBtn));
            Require(_overwriteCancelBtn, nameof(_overwriteCancelBtn));
            Require(_exitModal, nameof(_exitModal));
            Require(_exitConfirmBtn, nameof(_exitConfirmBtn));
            Require(_exitCancelBtn, nameof(_exitCancelBtn));
            Require(_modalBlur, nameof(_modalBlur));
        }

        void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
                Debug.LogError(
                    $"[MainMenuView] '{name}' is missing serialized reference '{field}' " +
                    "(wire it in MainMenu.unity).", this);
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
            string deepest = deepestWave < 1 ? "1" : deepestWave.ToString();
            UiBind.SetText(_campaignLine,
                $"{banked} banked  —  {runs} run(s)  —  deepest Wave {deepest}");
        }

        public void SetContinueVisible(bool visible) => UiBind.Show(_continueButton, visible);

        /// <summary>Opens a panel by the name it is registered under in
        /// <see cref="MainPanelManager.panels"/>. The only way this class changes screens.</summary>
        public void OpenPanel(string panelName)
        {
            if (_panels != null && !string.IsNullOrEmpty(panelName))
                _panels.OpenPanel(panelName);
        }

        public void ShowMain()
        {
            HideModal(_overwriteModal);
            HideModal(_exitModal);
            RevealMainPanelsRoot();
            OpenPanel(HomePanel);
            FocusDefault();
        }

        /// <summary>Shift Main Panel Root defaults to Invisible (alpha 0). SplashScreenManager
        /// used to Play("Start"); without it the Home buttons never appear. Scene disables
        /// that Animator; this still Plays Start / forces the CanvasGroup if it is on.</summary>
        void RevealMainPanelsRoot()
        {
            var root = _mainPanelsRoot;
            if (root == null) return;

            var anim = root.GetComponent<Animator>();
            if (anim != null && anim.enabled)
            {
                anim.Play("Start");
                return;
            }

            var group = root.GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        /// <summary>Opens the authored <c>Confirm Overwrite</c> modal (criterion 11). Its
        /// title and description are scene text, not code strings.</summary>
        public void ShowOverwriteModal() => ShowModal(_overwriteModal, _overwriteCancelBtn);

        /// <summary>Opens the authored <c>Exit</c> modal (criterion 12).</summary>
        public void ShowExitModal() => ShowModal(_exitModal, _exitCancelBtn);

        void ShowModal(ModalWindowManager modal, Button focus)
        {
            if (modal == null) return;
            modal.ModalWindowIn();
            if (_modalBlur != null) _modalBlur.BlurInAnim();

            var es = EventSystem.current;
            if (es != null && focus != null) es.SetSelectedGameObject(focus.gameObject);
        }

        void HideModal(ModalWindowManager modal)
        {
            // ModalWindowOut starts a coroutine — only legal while the modal object is active.
            if (modal != null && modal.gameObject.activeInHierarchy)
            {
                modal.ModalWindowOut();
                if (_modalBlur != null) _modalBlur.BlurOutAnim();
            }
        }
    }
}
