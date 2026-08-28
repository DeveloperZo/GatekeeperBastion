using System;
using System.Collections;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the three-card protocol pick authored into the CREW QUARTERS
    /// panel. Dumb by design — it owns the entrance and nothing else;
    /// <see cref="CrewQuartersStation"/> decides which cards exist and what pressing one does.
    /// </summary>
    /// <remarks>The entrance is motion only — a short rise and a scale settle, staggered per
    /// card, on unscaled time. It runs about a third of a second, every widget in it is live
    /// the whole way through, and nothing waits on it. Alpha is deliberately untouched:
    /// StationTerminal forces every CanvasGroup under an open station panel to alpha 1 every
    /// frame, so a fade would be a fight rather than an animation.</remarks>
    public sealed class ProtocolCardDeckView : MonoBehaviour
    {
        const string Tag = "ProtocolCardDeckView";

        public const string BrowseLabel = "SEE THE FULL CATALOG";

        [SerializeField] TMP_Text _heading;
        [SerializeField] ProtocolCardView[] _cards;
        [SerializeField, OptionalBinding] Button _browse;

        [Header("Entrance")]
        [SerializeField] float _rise = 34f;
        [SerializeField] float _duration = 0.18f;
        [SerializeField] float _stagger = 0.06f;

        Coroutine _entrance;
        bool _bindingsChecked;

        public int CardCount => _cards != null ? _cards.Length : 0;

        void Awake() => ValidateRequiredBindings();

        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_heading), _heading);
            if (_cards == null || _cards.Length < ProtocolOffer.CardCount)
            {
                Log.Error(Tag, name + ": deck holds " + CardCount + " card slots and the offer"
                    + " deals " + ProtocolOffer.CardCount + " — re-run Build/Protocol Bar.");
                return;
            }
            for (int i = 0; i < _cards.Length; i++)
                Require("_cards[" + i + "]", _cards[i]);
        }

        public ProtocolCardView CardAt(int index)
        {
            if (_cards == null || index < 0 || index >= _cards.Length) return null;
            return _cards[index];
        }

        public void SetHeading(string text) => UiBind.SetText(_heading, text);

        /// <summary>The way out of the deck and into the catalog. Hidden when the station
        /// hands over no action: an escape that does nothing is worse than none.</summary>
        public void BindBrowse(string label, Action onBrowse)
        {
            if (_browse == null) return;
            UiBind.Show(_browse, onBrowse != null);
            if (onBrowse == null) return;
            UiBind.SetButtonLabel(_browse, string.IsNullOrEmpty(label) ? BrowseLabel : label);
            _browse.onClick.RemoveAllListeners();
            _browse.onClick.AddListener(() => onBrowse());
            _browse.interactable = true;
        }

        void OnEnable()
        {
            StopEntrance();
            _entrance = StartCoroutine(Rise());
        }

        void OnDisable()
        {
            StopEntrance();
            SnapAll();
        }

        void StopEntrance()
        {
            if (_entrance == null) return;
            StopCoroutine(_entrance);
            _entrance = null;
        }

        IEnumerator Rise()
        {
            int n = CardCount;
            for (int i = 0; i < n; i++)
            {
                var card = _cards[i];
                if (card != null) card.SetEntrance(_rise, 0f);
            }

            float start = Time.unscaledTime;
            float total = _duration + _stagger * (n > 0 ? n - 1 : 0);
            while (true)
            {
                float elapsed = Time.unscaledTime - start;
                for (int i = 0; i < n; i++)
                {
                    var card = _cards[i];
                    if (card == null) continue;
                    float t = _duration <= 0f
                        ? 1f
                        : Mathf.Clamp01((elapsed - _stagger * i) / _duration);
                    card.SetEntrance(_rise, EaseOutCubic(t));
                }
                if (elapsed >= total) break;
                yield return null;
            }

            SnapAll();
            _entrance = null;
        }

        void SnapAll()
        {
            for (int i = 0; i < CardCount; i++)
            {
                var card = _cards[i];
                if (card != null) card.SnapToRest();
            }
        }

        static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        void OnDestroy()
        {
            if (_browse != null) _browse.onClick.RemoveAllListeners();
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, name + ": missing binding '" + field + "'.");
        }
    }
}
