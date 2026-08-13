using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: sequenced end-of-run panel: verdict → headline →
    /// staggered stats → salvage count-up → actions. Any input fast-forwards; a second input
    /// proceeds. Visibility is event-driven — Present once per end identity, not every frame.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        const float VerdictHold = 1f;
        const float StatStagger = 0.25f;
        const int MaxVisibleStats = 6;

        public event Action ProceedClicked;

        [SerializeField] GameObject _root;
        [SerializeField, OptionalBinding] CanvasGroup _scrim;
        [SerializeField, OptionalBinding] GameObject _panel;
        [SerializeField] TMP_Text _verdict;
        [SerializeField] TMP_Text _headline;
        [SerializeField] GameObject[] _statRows;
        [SerializeField] TMP_Text[] _statLabels;
        [SerializeField, OptionalBinding] ValueAnimator _salvageCount;
        [SerializeField, OptionalBinding] TMP_Text _salvageCaption;
        [SerializeField] GameObject _actionsRoot;
        [SerializeField] Button _returnButton;
        [SerializeField, OptionalBinding] Button _detailsButton;
        [SerializeField, OptionalBinding] GameObject _detailsRoot;
        [SerializeField, OptionalBinding] TMP_Text[] _detailLabels;
        [SerializeField, OptionalBinding] StaggerEntrance _statsStagger;

        bool _wired;
        bool _sequenceComplete;
        bool _awaitingProceed;
        bool _detailsOpen;
        string _lastKey;
        Coroutine _sequence;
        float _salvageTarget;

        public void EnsureBuilt()
        {
            if (_wired) return;
            if (_returnButton == null)
            {
                Debug.LogError($"[EndScreenView] '{name}' missing serialized _returnButton.", this);
                return;
            }
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
            UiBind.Show(_root, visible);
            if (!visible)
            {
                StopSequence();
                _sequenceComplete = false;
                _awaitingProceed = false;
                _lastKey = null;
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
            if (key == _lastKey) return;

            _lastKey = key;
            _salvageTarget = salvageValue;
            _sequenceComplete = false;
            _awaitingProceed = false;
            _detailsOpen = false;

            UiBind.SetText(_verdict, verdict != null ? verdict : "");
            UiBind.SetText(_headline, headline != null ? headline : "");
            UiBind.SetText(_salvageCaption, salvageCaption != null ? salvageCaption : "");

            ApplyStatTexts(_statLabels, primaryStats);
            ApplyStatTexts(_detailLabels, detailStats);

            bool hasDetails = detailStats != null && detailStats.Length > 0;
            UiBind.Show(_detailsButton, hasDetails);
            UiBind.Show(_detailsRoot, false);

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
            UiBind.Show(_detailsRoot, _detailsOpen);
        }

        void OnReturn()
        {
            if (ProceedClicked != null) ProceedClicked.Invoke();
        }

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
                UiBind.Show(labels[i], show);
                if (show) UiBind.SetText(labels[i], values[i]);
            }
        }

        static void SetActive(Component c, bool on)
        {
            if (c != null) UiBind.Show(c.gameObject, on);
        }

        static void SetActive(GameObject go, bool on) => UiBind.Show(go, on);

        static bool AnyInputDown()
        {
            // Defensive sample: the end screen can outlive the fight's tick owner.
            return Controls.Sample().anyPressDown;
        }
    }
}
