using System;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>One authored station panel's chrome: header, close button and the ARMORY
    /// wheel host. The body is authored per station and bound by that station's panel view
    /// (<see cref="ArmoryPanelView"/> et al.); this class sets no colour, font size or rect.</summary>
    public sealed class StationPanelView : MonoBehaviour
    {
        const string Tag = "StationPanelView";

        [SerializeField] RectTransform _root;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _banked;
        [SerializeField, OptionalBinding] TextMeshProUGUI _lifetime;
        [SerializeField] RectTransform _bodyContent;
        [SerializeField, OptionalBinding] RectTransform _wheelSlot;
        [SerializeField, OptionalBinding] ArmoryWheelView _wheel;
        [SerializeField] Button _closeButton;

        Action _onClose;
        bool _closeWired;
        bool _bindingsChecked;

        public RectTransform BodyContent => _bodyContent;

        /// <summary>ARMORY wheel host. Authored under <c>List Content</c>.</summary>
        public RectTransform WheelSlot => _wheelSlot;

        public ArmoryWheelView Wheel
        {
            get
            {
                if (_wheel == null && _wheelSlot != null)
                    _wheel = _wheelSlot.GetComponentInChildren<ArmoryWheelView>(true);
                return _wheel;
            }
        }

        public void SetWheelVisible(bool visible)
        {
            var w = Wheel;
            if (w != null) UiBind.Show(w, visible);
            else UiBind.Show(_wheelSlot, visible);
        }

        void Awake()
        {
            UiCanvasGuard.RepairIfBroken(gameObject);
            WireClose();
            ValidateRequiredBindings();
        }

        void OnEnable() => UiCanvasGuard.RepairIfBroken(gameObject);

        /// <summary>Logs one error per missing required field. OptionalBinding fields are skipped.</summary>
        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_root), _root);
            Require(nameof(_title), _title);
            Require(nameof(_banked), _banked);
            Require(nameof(_bodyContent), _bodyContent);
            Require(nameof(_closeButton), _closeButton);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }

        void WireClose()
        {
            if (_closeWired || _closeButton == null) return;
            _closeWired = true;
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => { if (_onClose != null) _onClose.Invoke(); });
        }

        public void SetOnClose(Action onClose)
        {
            _onClose = onClose;
            WireClose();
        }

        public void SetHeader(string stationName, long banked, string lifetimeLine)
        {
            UiBind.SetText(_title, stationName);
            UiBind.SetText(_banked, "Banked salvage: " + banked);
            UiBind.SetText(_lifetime, lifetimeLine);
        }

        public void SetCloseLabel(string label) => UiBind.SetButtonLabel(_closeButton, label);
    }
}
