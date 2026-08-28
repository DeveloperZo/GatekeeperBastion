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

        /// <summary>The three-card pick, authored by SceneBuilder's Build/Protocol Bar row.
        /// Optional so a scene that has not been through that row still binds cleanly and the
        /// station falls back to the catalog it has always shown — the cards are a better
        /// first choice, not a load-bearing one.</summary>
        [SerializeField, OptionalBinding] ProtocolCardDeckView _cardDeck;

        bool _bindingsChecked;

        /// <summary>How many catalog rows this panel can draw. The station fills them from
        /// the catalog and pads the remainder with Coming Soon copy.</summary>
        public const int RowCount = 4;

        public ProtocolSheetView Sheet => _sheet;

        /// <summary>Null until the deck has been authored into this scene.</summary>
        public ProtocolCardDeckView CardDeck => _cardDeck;

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

        /// <summary>Show or hide the pick deck. Change-gated by UiBind, which matters here:
        /// the deck replays its entrance on every activation, so an ungated SetActive on each
        /// repaint would restart the animation while the player is reading.</summary>
        public void ShowCardDeck(bool visible) => UiBind.Show(_cardDeck, visible);

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }
    }
}
