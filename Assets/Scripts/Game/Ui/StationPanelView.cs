using System;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: one authored station panel's chrome: header, close button and the ARMORY
    /// wheel host. The body is authored per station and bound by that station's panel view
    /// (<see cref="ArmoryPanelView"/> et al.); this class sets no colour, font size or rect.</summary>
    public sealed class StationPanelView : MonoBehaviour
    {
        const string Tag = "StationPanelView";

        [SerializeField] RectTransform _root;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField, OptionalBinding] TextMeshProUGUI _purpose;
        [SerializeField] TextMeshProUGUI _banked;
        [SerializeField, OptionalBinding] TextMeshProUGUI _lifetime;
        [SerializeField, OptionalBinding] TextMeshProUGUI _deal;
        [SerializeField, OptionalBinding] TextMeshProUGUI _footer;
        [SerializeField] RectTransform _bodyContent;
        [SerializeField, OptionalBinding] RectTransform _wheelSlot;
        [SerializeField, OptionalBinding] ArmoryWheelView _wheel;
        [SerializeField] Button _closeButton;

        public const string NoSellBack = "No sell-back.";
        public const string MovementLock = "Opening a console locks movement.";

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

        public void SetHeader(string stationName, string purpose, long banked, string lifetimeLine)
        {
            if (string.IsNullOrWhiteSpace(purpose))
                Log.Error(Tag, name + ": empty purpose.");
            UiBind.SetText(_title, stationName);
            UiBind.SetText(_purpose, purpose != null ? purpose : "");
            UiBind.SetText(_banked, "Banked salvage: " + banked);
            UiBind.SetText(_lifetime, lifetimeLine);
            SetDeal(null);
            UiBind.SetText(_footer, NoSellBack + "\n" + MovementLock);
        }

        public void SetDeal(string dealLine) => UiBind.SetTextOrHide(_deal, dealLine);

        public void SetCloseLabel(string label) => UiBind.SetButtonLabel(_closeButton, label);
    }
}
