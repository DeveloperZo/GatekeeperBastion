using System;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Main menu presentation — Shift buttons, campaign strip, overwrite modal, accent bar.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] GameObject _mainRoot;
        [SerializeField] GameObject _continueButton;
        [SerializeField] TextMeshProUGUI _campaignBanked;
        [SerializeField] TextMeshProUGUI _campaignRuns;
        [SerializeField] TextMeshProUGUI _campaignDeepest;
        [SerializeField] TextMeshProUGUI _campaignLine;
        [SerializeField] RectTransform _accentBar;
        [SerializeField] Button _continueBtn;
        [SerializeField] Button _startNewBtn;
        [SerializeField] Button _optionsBtn;
        [SerializeField] Button _quitBtn;
        [SerializeField] ModalWindowManager _overwriteModal;
        [SerializeField] Button _overwriteConfirmBtn;
        [SerializeField] Button _overwriteCancelBtn;
        [SerializeField] TextMeshProUGUI _overwriteConfirmLabel;
        [SerializeField] TextMeshProUGUI _overwriteCancelLabel;

        bool _wired;
        RectTransform _accentTarget;
        Vector2 _accentRest;

        public Action onContinue;
        public Action onStartNew;
        public Action onOptions;
        public Action onQuit;
        public Action onOverwriteConfirm;
        public Action onOverwriteBack;

        public bool OverwriteOpen { get; private set; }

        /// <summary>authored prefab required — wires serialized buttons; no construction.</summary>
        public void EnsureBuilt()
        {
            if (_mainRoot != null) _mainRoot.SetActive(true);
            if (_accentBar != null) _accentRest = _accentBar.anchoredPosition;
            ApplyModalVerbs();
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

        void Update()
        {
            TrackAccentFromSelection();
            if (_accentBar == null || _accentTarget == null) return;
            Vector2 want = new Vector2(_accentRest.x, _accentTarget.anchoredPosition.y);
            _accentBar.anchoredPosition = Vector2.Lerp(
                _accentBar.anchoredPosition, want, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        }

        void TrackAccentFromSelection()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return;
            var rt = es.currentSelectedGameObject.transform as RectTransform;
            if (rt == null) return;
            if (IsMenuButton(rt)) _accentTarget = rt;
        }

        bool IsMenuButton(RectTransform rt)
        {
            return rt == (_continueBtn != null ? _continueBtn.transform : null)
                || rt == (_startNewBtn != null ? _startNewBtn.transform : null)
                || rt == (_optionsBtn != null ? _optionsBtn.transform : null)
                || rt == (_quitBtn != null ? _quitBtn.transform : null);
        }

        void ApplyModalVerbs()
        {
            if (_overwriteConfirmLabel != null)
                _overwriteConfirmLabel.text = "Overwrite and start new";
            if (_overwriteCancelLabel != null)
                _overwriteCancelLabel.text = "Keep my save";
            SetMainButtonText(_overwriteConfirmBtn, "Overwrite and start new");
            SetMainButtonText(_overwriteCancelBtn, "Keep my save");
        }

        static void SetMainButtonText(Button btn, string text)
        {
            if (btn == null) return;
            var mb = btn.GetComponent<MainButton>();
            if (mb != null)
            {
                mb.buttonText = text;
                if (mb.normalText != null) mb.normalText.text = text;
                if (mb.highlightedText != null) mb.highlightedText.text = text;
                if (mb.pressedText != null) mb.pressedText.text = text;
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
            GameObject target = null;
            if (_continueButton != null && _continueButton.activeInHierarchy && _continueBtn != null)
                target = _continueBtn.gameObject;
            else if (_startNewBtn != null)
                target = _startNewBtn.gameObject;
            if (target != null)
            {
                es.SetSelectedGameObject(target);
                _accentTarget = target.transform as RectTransform;
            }
        }

        public void SetCampaignStrip(int banked, int runs, int deepestWave)
        {
            EnsureBuilt();
            if (_campaignBanked != null) _campaignBanked.text = $"{banked} banked";
            if (_campaignRuns != null) _campaignRuns.text = $"{runs} run(s)";
            string deepest = deepestWave < 1 ? "1" : deepestWave.ToString();
            if (_campaignDeepest != null) _campaignDeepest.text = $"deepest Wave {deepest}";
            if (_campaignLine != null)
                _campaignLine.text = $"{banked} banked  —  {runs} run(s)  —  deepest Wave {deepest}";
        }

        public void SetContinueVisible(bool visible)
        {
            EnsureBuilt();
            if (_continueButton != null) _continueButton.SetActive(visible);
        }

        public void ShowMain()
        {
            EnsureBuilt();
            OverwriteOpen = false;
            if (_mainRoot != null) _mainRoot.SetActive(true);
            if (_overwriteModal != null) _overwriteModal.ModalWindowOut();
            FocusDefault();
        }

        public void ShowOverwriteModal(string title, string body)
        {
            EnsureBuilt();
            ApplyModalVerbs();
            OverwriteOpen = true;
            if (_overwriteModal != null)
            {
                _overwriteModal.useCustomTexts = true;
                if (_overwriteModal.windowTitle != null) _overwriteModal.windowTitle.text = title;
                if (_overwriteModal.windowDescription != null)
                    _overwriteModal.windowDescription.text = body;
                _overwriteModal.ModalWindowIn();
            }

            var es = EventSystem.current;
            if (es != null && _overwriteCancelBtn != null)
                es.SetSelectedGameObject(_overwriteCancelBtn.gameObject);
        }
    }
}
