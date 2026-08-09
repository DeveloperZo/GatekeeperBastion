using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Sequenced end-of-run panel (criterion 21): verdict → headline → staggered stats →
    /// salvage count-up → actions. Any input fast-forwards; a second input proceeds.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        const float VerdictHold = 1f;
        const float StatStagger = 0.25f;
        const int MaxVisibleStats = 6;

        public event Action ProceedClicked;

        [SerializeField] GameObject _root;
        [SerializeField] CanvasGroup _scrim;
        [SerializeField] GameObject _panel;
        [SerializeField] TMP_Text _verdict;
        [SerializeField] TMP_Text _headline;
        [SerializeField] GameObject[] _statRows;
        [SerializeField] TMP_Text[] _statLabels;
        [SerializeField] ValueAnimator _salvageCount;
        [SerializeField] TMP_Text _salvageCaption;
        [SerializeField] GameObject _actionsRoot;
        [SerializeField] Button _returnButton;
        [SerializeField] Button _detailsButton;
        [SerializeField] GameObject _detailsRoot;
        [SerializeField] TMP_Text[] _detailLabels;
        [SerializeField] StaggerEntrance _statsStagger;

        bool _wired;
        bool _sequenceComplete;
        bool _awaitingProceed;
        bool _detailsOpen;
        string _lastKey;
        Coroutine _sequence;
        float _salvageTarget;

        public void EnsureBuilt()
        {
            if (_wired || _returnButton == null) return;
            _wired = true;
            _returnButton.onClick.RemoveAllListeners();
            _returnButton.onClick.AddListener(OnReturn);
            if (_detailsButton != null)
            {
                _detailsButton.onClick.RemoveAllListeners();
                _detailsButton.onClick.AddListener(ToggleDetails);
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            if (_root != null) _root.SetActive(visible);
            if (!visible)
            {
                StopSequence();
                _sequenceComplete = false;
                _awaitingProceed = false;
                // Keep _lastKey so HudController's per-frame HideAll+Present does not restart.
            }
        }

        /// <summary>Push end-screen copy. Re-starts the sequence only when the identity changes.</summary>
        public void Present(
            string verdict,
            string headline,
            string[] primaryStats,
            float salvageValue,
            string salvageCaption,
            string[] detailStats)
        {
            EnsureBuilt();
            string key = verdict + "|" + headline + "|" + salvageValue;
            if (key == _lastKey)
            {
                // HudController re-pushes every frame after HideAll — re-show, do not restart.
                if (_root != null && !_root.activeSelf) SetVisible(true);
                return;
            }

            _lastKey = key;
            _salvageTarget = salvageValue;
            _sequenceComplete = false;
            _awaitingProceed = false;
            _detailsOpen = false;

            if (_verdict != null) _verdict.text = verdict ?? "";
            if (_headline != null) _headline.text = headline ?? "";
            if (_salvageCaption != null) _salvageCaption.text = salvageCaption ?? "";

            ApplyStatTexts(_statLabels, primaryStats);
            ApplyStatTexts(_detailLabels, detailStats);

            bool hasDetails = detailStats != null && detailStats.Length > 0;
            if (_detailsButton != null) _detailsButton.gameObject.SetActive(hasDetails);
            if (_detailsRoot != null) _detailsRoot.SetActive(false);

            SetVisible(true);
            StopSequence();
            _sequence = StartCoroutine(PlaySequence());
        }

        void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (!AnyInputDown()) return;

            if (!_sequenceComplete)
            {
                FastForward();
                return;
            }

            if (_awaitingProceed)
                OnReturn();
        }

        IEnumerator PlaySequence()
        {
            HideAllPhases();
            SetActive(_verdict, true);
            float holdUntil = Time.unscaledTime + VerdictHold;
            while (Time.unscaledTime < holdUntil && !_sequenceComplete)
                yield return null;
            if (_sequenceComplete) yield break;

            SetActive(_headline, true);
            yield return Wait(0.2f);
            if (_sequenceComplete) yield break;

            ShowStatsStaggered();
            int shown = Mathf.Min(MaxVisibleStats, _statRows != null ? _statRows.Length : 0);
            yield return Wait(shown * StatStagger + 0.2f);
            if (_sequenceComplete) yield break;

            ShowSalvage();
            yield return Wait(0.6f);
            if (_sequenceComplete) yield break;

            ShowActions();
            MarkComplete();
            _sequence = null;
        }

        void FastForward()
        {
            StopSequence();
            SetActive(_verdict, true);
            SetActive(_headline, true);
            ShowAllStatsImmediate();
            ShowSalvage();
            if (_salvageCount != null) _salvageCount.SetImmediate(_salvageTarget);
            ShowActions();
            MarkComplete();
        }

        void MarkComplete()
        {
            _sequenceComplete = true;
            // One-frame gap so the fast-forward key does not also proceed.
            _awaitingProceed = false;
            StartCoroutine(ArmProceedNextFrame());
        }

        IEnumerator ArmProceedNextFrame()
        {
            yield return null;
            _awaitingProceed = true;
        }

        void HideAllPhases()
        {
            SetActive(_verdict, false);
            SetActive(_headline, false);
            if (_statRows != null)
                for (int i = 0; i < _statRows.Length; i++)
                    SetActive(_statRows[i], false);
            if (_salvageCount != null) _salvageCount.gameObject.SetActive(false);
            if (_salvageCaption != null) _salvageCaption.gameObject.SetActive(false);
            if (_actionsRoot != null) _actionsRoot.SetActive(false);
        }

        void ShowStatsStaggered()
        {
            if (_statRows == null) return;
            int n = Mathf.Min(MaxVisibleStats, _statRows.Length);
            for (int i = 0; i < n; i++)
                SetActive(_statRows[i], true);
            if (_statsStagger != null)
            {
                _statsStagger.enabled = false;
                _statsStagger.enabled = true;
            }
        }

        void ShowAllStatsImmediate()
        {
            if (_statRows == null) return;
            int n = Mathf.Min(MaxVisibleStats, _statRows.Length);
            for (int i = 0; i < n; i++)
                SetActive(_statRows[i], true);
        }

        void ShowSalvage()
        {
            if (_salvageCaption != null) _salvageCaption.gameObject.SetActive(true);
            if (_salvageCount != null)
            {
                _salvageCount.gameObject.SetActive(true);
                _salvageCount.SetImmediate(0f);
                _salvageCount.SetValue(_salvageTarget);
            }
        }

        void ShowActions()
        {
            if (_actionsRoot != null) _actionsRoot.SetActive(true);
        }

        void ToggleDetails()
        {
            _detailsOpen = !_detailsOpen;
            if (_detailsRoot != null) _detailsRoot.SetActive(_detailsOpen);
        }

        void OnReturn() => ProceedClicked?.Invoke();

        void StopSequence()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }
        }

        IEnumerator Wait(float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until && !_sequenceComplete)
                yield return null;
        }

        static void ApplyStatTexts(TMP_Text[] labels, string[] values)
        {
            if (labels == null) return;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                bool show = values != null && i < values.Length && !string.IsNullOrEmpty(values[i]);
                labels[i].gameObject.SetActive(show);
                if (show) labels[i].text = values[i];
            }
        }

        static void SetActive(Component c, bool on)
        {
            if (c != null) c.gameObject.SetActive(on);
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go != null) go.SetActive(on);
        }

        static bool AnyInputDown()
        {
            if (Input.anyKeyDown) return true;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
            return false;
        }
    }
}
