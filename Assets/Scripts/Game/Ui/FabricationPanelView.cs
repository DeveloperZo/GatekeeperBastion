using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: authored FABRICATION body: hatch, equipment and payload rows plus the bay
    /// line. The HATCH / EQUIPMENT / PAYLOAD headings are authored scene text. A dumb binder —
    /// code writes text, toggles rows and wires clicks; layout, colour and font are the
    /// scene's.</summary>
    public sealed class FabricationPanelView : MonoBehaviour
    {
        const string Tag = "FabricationPanelView";

        [SerializeField] PanelRowView _hatchRow;
        [SerializeField] PanelRowView _equipmentSlotRow;
        [SerializeField] PanelRowView _repairRigRow;
        [SerializeField] PanelRowView _decoyRow;
        [SerializeField] TextMeshProUGUI _bayLine;

        bool _bindingsChecked;

        public PanelRowView HatchRow => _hatchRow;
        public PanelRowView EquipmentSlotRow => _equipmentSlotRow;

        /// <summary>Deactivated (not absent) until the equipment slot is purchased.</summary>
        public PanelRowView RepairRigRow => _repairRigRow;

        public PanelRowView DecoyRow => _decoyRow;

        void Awake() => ValidateRequiredBindings();

        /// <summary>Logs one error per missing field.</summary>
        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_hatchRow), _hatchRow);
            Require(nameof(_equipmentSlotRow), _equipmentSlotRow);
            Require(nameof(_repairRigRow), _repairRigRow);
            Require(nameof(_decoyRow), _decoyRow);
            Require(nameof(_bayLine), _bayLine);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }

        public void SetBayLine(string text) => UiBind.SetText(_bayLine, text);
    }
}
