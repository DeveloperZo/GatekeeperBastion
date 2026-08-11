using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Authored CREW QUARTERS body: one row per attribute track. The ATTRIBUTES
    /// heading is authored scene text. A dumb binder — code writes text, toggles rows and
    /// wires clicks; layout, colour and font are the scene's.</summary>
    public sealed class CrewQuartersPanelView : MonoBehaviour
    {
        const string Tag = "CrewQuartersPanelView";

        [SerializeField] PanelRowView _vitalityRow;
        [SerializeField] PanelRowView _staminaRow;
        [SerializeField] PanelRowView _scavengingRow;
        [SerializeField] PanelRowView _luckRow;

        bool _bindingsChecked;

        public PanelRowView VitalityRow => _vitalityRow;
        public PanelRowView StaminaRow => _staminaRow;
        public PanelRowView ScavengingRow => _scavengingRow;
        public PanelRowView LuckRow => _luckRow;

        void Awake() => ValidateRequiredBindings();

        /// <summary>Logs one error per missing field.</summary>
        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_vitalityRow), _vitalityRow);
            Require(nameof(_staminaRow), _staminaRow);
            Require(nameof(_scavengingRow), _scavengingRow);
            Require(nameof(_luckRow), _luckRow);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }
    }
}
