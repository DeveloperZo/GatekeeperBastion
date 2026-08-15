using System;
using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: authored ARMORY body: slot toggles, equipped block, wheel captions, catalog
    /// rows. A dumb binder — every element exists in Ship.unity; code writes text, toggles
    /// rows and wires clicks. Layout, colour and font are the scene's.</summary>
    public sealed class ArmoryPanelView : MonoBehaviour
    {
        const string Tag = "ArmoryPanelView";

        [SerializeField] Button _primaryButton;
        [SerializeField] Button _secondaryButton;
        [SerializeField] TextMeshProUGUI _equippedLine;
        [SerializeField] TextMeshProUGUI _statLine;
        [SerializeField] TextMeshProUGUI _rotateHint;
        [SerializeField] TextMeshProUGUI _ringNote;
        [SerializeField] PanelRowView[] _catalogRows;
        [SerializeField] TextMeshProUGUI _rowsMissing;
        [SerializeField, OptionalBinding] GameObject _catalogHeading;
        [SerializeField, OptionalBinding] Button _backButton;

        bool _bindingsChecked;

        public int CatalogRowCount => _catalogRows != null ? _catalogRows.Length : 0;

        public PanelRowView CatalogRow(int index) =>
            _catalogRows != null && index >= 0 && index < _catalogRows.Length
                ? _catalogRows[index] : null;

        void Awake() => ValidateRequiredBindings();

        /// <summary>Logs one error per missing field. Array elements are each checked.</summary>
        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_primaryButton), _primaryButton);
            Require(nameof(_secondaryButton), _secondaryButton);
            Require(nameof(_equippedLine), _equippedLine);
            Require(nameof(_statLine), _statLine);
            Require(nameof(_rotateHint), _rotateHint);
            Require(nameof(_ringNote), _ringNote);
            Require(nameof(_rowsMissing), _rowsMissing);
            if (_catalogRows == null || _catalogRows.Length == 0)
            {
                Log.Error(Tag, $"{name}: missing binding '{nameof(_catalogRows)}'.");
                return;
            }
            for (int i = 0; i < _catalogRows.Length; i++)
                Require(nameof(_catalogRows) + "[" + i + "]", _catalogRows[i]);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }

        public void BindSlotButtons(string primaryLabel, Action onPrimary,
            string secondaryLabel, Action onSecondary)
        {
            BindButton(_primaryButton, primaryLabel, onPrimary);
            BindButton(_secondaryButton, secondaryLabel, onSecondary);
        }

        /// <summary>Equipped block. A null stat line hides the stat label (empty slot).</summary>
        public void BindEquipped(string equippedLine, string statLine)
        {
            UiBind.SetText(_equippedLine, equippedLine);
            UiBind.SetTextOrHide(_statLine, statLine);
        }

        /// <summary>Wheel captions; null hides. The ring note doubles as the ring-missing /
        /// wheel-missing message, so those states need no extra authored label.</summary>
        public void BindWheelNotes(string rotateHint, string ringNote)
        {
            UiBind.SetTextOrHide(_rotateHint, rotateHint);
            UiBind.SetTextOrHide(_ringNote, ringNote);
        }

        /// <summary>Catalog list (heading + rows). Off while the upgrade wheel is open.</summary>
        public void SetCatalogVisible(bool visible)
        {
            UiBind.Show(_catalogHeading, visible);
            if (_catalogRows != null)
            {
                for (int i = 0; i < _catalogRows.Length; i++)
                {
                    if (!visible)
                        UiBind.Show(_catalogRows[i], false);
                }
            }
            if (!visible) ShowRowsMissing(false);
        }

        /// <summary>Back from wheel to catalog. Null hides the button.</summary>
        public void BindBack(Action onBack)
        {
            if (_backButton == null) return;
            bool show = onBack != null;
            UiBind.Show(_backButton, show);
            if (!show) return;
            UiBind.SetButtonLabel(_backButton, "BACK");
            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(() => onBack.Invoke());
            _backButton.interactable = true;
        }

        /// <summary>The overflow line's text is authored; code only toggles it.</summary>
        public void ShowRowsMissing(bool show) => UiBind.Show(_rowsMissing, show);

        static void BindButton(Button button, string label, Action onClick)
        {
            if (button == null) return;
            UiBind.SetButtonLabel(button, label);
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
            button.interactable = true;
        }
    }
}
