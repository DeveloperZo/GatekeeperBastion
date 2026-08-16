using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: authored CREW QUARTERS body. Serialized names stay so Ship.unity
    /// bindings survive: vitality→protocol, stamina→Ghost, scavenging→Foresight, luck hidden.</summary>
    public sealed class CrewQuartersPanelView : MonoBehaviour
    {
        const string Tag = "CrewQuartersPanelView";

        [SerializeField] PanelRowView _vitalityRow;
        [SerializeField] PanelRowView _staminaRow;
        [SerializeField] PanelRowView _scavengingRow;
        [SerializeField] PanelRowView _luckRow;

        bool _bindingsChecked;

        public PanelRowView ProtocolRow => _vitalityRow;
        public PanelRowView GhostRow => _staminaRow;
        public PanelRowView ForesightRow => _scavengingRow;
        public PanelRowView UnusedRow => _luckRow;

        void Awake() => ValidateRequiredBindings();

        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_vitalityRow), _vitalityRow);
            Require(nameof(_staminaRow), _staminaRow);
            Require(nameof(_scavengingRow), _scavengingRow);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }
    }
}
