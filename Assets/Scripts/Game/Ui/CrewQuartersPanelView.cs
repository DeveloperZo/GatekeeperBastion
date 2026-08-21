using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: authored CREW QUARTERS body. Serialized row names stay so
    /// Ship.unity bindings survive: vitality→Default, stamina/scavenging/luck→stubs.
    /// Catalog and <see cref="ProtocolSheetView"/> swap like ARMORY catalog/wheel.</summary>
    public sealed class CrewQuartersPanelView : MonoBehaviour
    {
        const string Tag = "CrewQuartersPanelView";

        [SerializeField] PanelRowView _vitalityRow;
        [SerializeField] PanelRowView _staminaRow;
        [SerializeField] PanelRowView _scavengingRow;
        [SerializeField] PanelRowView _luckRow;
        [SerializeField] GameObject _catalogHeading;
        [SerializeField] ProtocolSheetView _sheet;

        bool _bindingsChecked;

        /// <summary>How many catalog rows this panel can draw. The station fills them from
        /// the catalog and pads the remainder with Coming Soon copy.</summary>
        public const int RowCount = 4;

        public ProtocolSheetView Sheet => _sheet;

        /// <summary>Catalog row by position. The serialized field names are the old upgrade
        /// tracks and stay that way so the Ship.unity bindings survive; position is what the
        /// station addresses them by.</summary>
        public PanelRowView RowAt(int index)
        {
            switch (index)
            {
                case 0: return _vitalityRow;
                case 1: return _staminaRow;
                case 2: return _scavengingRow;
                case 3: return _luckRow;
                default: return null;
            }
        }

        void Awake() => ValidateRequiredBindings();

        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_vitalityRow), _vitalityRow);
            Require(nameof(_staminaRow), _staminaRow);
            Require(nameof(_scavengingRow), _scavengingRow);
            Require(nameof(_luckRow), _luckRow);
            Require(nameof(_catalogHeading), _catalogHeading);
            Require(nameof(_sheet), _sheet);
        }

        public void SetCatalogVisible(bool visible)
        {
            UiBind.Show(_catalogHeading, visible);
            if (!visible)
            {
                UiBind.Show(_vitalityRow, false);
                UiBind.Show(_staminaRow, false);
                UiBind.Show(_scavengingRow, false);
                UiBind.Show(_luckRow, false);
            }
        }

        public void ShowSheet(bool visible) => UiBind.Show(_sheet, visible);

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }
    }
}
