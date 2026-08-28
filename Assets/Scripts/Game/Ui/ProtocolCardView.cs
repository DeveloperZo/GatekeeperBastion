using System;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: one authored protocol card. Binds the same
    /// <see cref="BoarderProtocol.Sheet"/> the detail panel binds, so a copy change lands on
    /// the card and the sheet together or on neither. Layout stays in the scene
    /// (SceneBuilder authors the deck); this writes text and one button.</summary>
    /// <remarks>The entrance is driven from <see cref="ProtocolCardDeckView"/> and moves this
    /// card's RectTransform only. It deliberately does not touch alpha: StationTerminal forces
    /// every CanvasGroup under an open station panel to alpha 1 every frame, so a fade here
    /// would be a fight rather than an animation.</remarks>
    public sealed class ProtocolCardView : MonoBehaviour
    {
        const string Tag = "ProtocolCardView";

        public const string PickLabel = "CHOOSE";
        public const string CrewPrefix = "CREW  ";
        public const string PersonalPrefix = "PERSONAL  ";
        public const string PassivePrefix = "PASSIVE  ";

        /// <summary>Scale the card starts the entrance at. Small enough to read as arrival,
        /// large enough that the hit target barely moves under a cursor already on it.</summary>
        public const float EntranceScale = 0.94f;

        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _doctrine;
        [SerializeField] TMP_Text _crew;
        [SerializeField] TMP_Text _personal;
        [SerializeField] TMP_Text _passive;
        [SerializeField] Button _pick;
        [SerializeField, OptionalBinding] TMP_Text _refusal;

        RectTransform _rect;
        Vector2 _rest;
        bool _restCaptured;
        bool _bindingsChecked;

        void Awake()
        {
            EnsureRest();
            ValidateRequiredBindings();
        }

        /// <summary>Record where the scene author put this card. Idempotent and callable
        /// before Awake: the deck can be shown in the same frame the station panel first
        /// activates, and a rest position captured after the entrance had already offset the
        /// card would leave it permanently low.</summary>
        public void EnsureRest()
        {
            if (_restCaptured) return;
            _restCaptured = true;
            _rect = transform as RectTransform;
            if (_rect != null) _rest = _rect.anchoredPosition;
        }

        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_name), _name);
            Require(nameof(_doctrine), _doctrine);
            Require(nameof(_crew), _crew);
            Require(nameof(_personal), _personal);
            Require(nameof(_passive), _passive);
            Require(nameof(_pick), _pick);
        }

        /// <summary>Paint one protocol. <paramref name="refusal"/> is null when the card can
        /// be taken; otherwise it is the reason, shown rather than implied — a dark button
        /// with no line under it is the same defect as a dead one.</summary>
        public void Bind(BoarderProtocol.Sheet sheet, string pickLabel, Action onPick,
            bool pickEnabled, string refusal)
        {
            UiBind.SetText(_name, sheet.Name);
            UiBind.SetText(_doctrine, sheet.Doctrine);
            UiBind.SetText(_crew, CrewPrefix + sheet.Crew);
            UiBind.SetText(_personal, PersonalPrefix + sheet.Personal);
            UiBind.SetText(_passive, PassivePrefix + sheet.Passive);
            UiBind.SetTextOrHide(_refusal, refusal);

            if (_pick == null) return;
            UiBind.SetButtonLabel(_pick, string.IsNullOrEmpty(pickLabel) ? PickLabel : pickLabel);
            _pick.onClick.RemoveAllListeners();
            if (onPick != null && pickEnabled)
                _pick.onClick.AddListener(() => onPick());
            _pick.interactable = pickEnabled;
        }

        /// <summary>One entrance frame. <paramref name="t"/> is 0 fully out and 1 at rest;
        /// <paramref name="rise"/> is how far below rest "out" sits, in canvas units.</summary>
        public void SetEntrance(float rise, float t)
        {
            EnsureRest();
            if (_rect == null) return;
            float clamped = Mathf.Clamp01(t);
            _rect.anchoredPosition = new Vector2(_rest.x, _rest.y - rise * (1f - clamped));
            float s = Mathf.Lerp(EntranceScale, 1f, clamped);
            _rect.localScale = new Vector3(s, s, 1f);
        }

        public void SnapToRest()
        {
            EnsureRest();
            if (_rect == null) return;
            _rect.anchoredPosition = _rest;
            _rect.localScale = Vector3.one;
        }

        void OnDestroy()
        {
            // The listener captured the station's closure. Dropping it keeps a click already
            // in flight from reaching a station whose panel is gone.
            if (_pick != null) _pick.onClick.RemoveAllListeners();
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, name + ": missing binding '" + field + "'.");
        }
    }
}
