using System;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>Authored NAV MAP body: nav panel (node grid), map panel (destination, premise,
    /// start-wave picker, loadout warnings and the embark action). Exactly one panel shows at a
    /// time. The warning and load-failed texts are authored; code only toggles them. A dumb
    /// binder — layout, colour and font are the scene's.</summary>
    public sealed class NavMapPanelView : MonoBehaviour
    {
        const string Tag = "NavMapPanelView";

        [SerializeField] Button _bastionNode;   // the one live node on the nav panel
        [SerializeField] GameObject _navPanel;  // default view: node grid of bastions
        [SerializeField] GameObject _mapPanel;  // embark view: wave select + embark (List Content)
        [SerializeField] TextMeshProUGUI _destination;
        [SerializeField] TextMeshProUGUI _premise;
        [SerializeField] TextMeshProUGUI _startWaveLine;
        [SerializeField] Button _wavePrev;
        [SerializeField] TextMeshProUGUI _waveValue;
        [SerializeField] Button _waveNext;
        [SerializeField] TextMeshProUGUI _noWeaponWarning;
        [SerializeField] TextMeshProUGUI _noRigWarning;
        [SerializeField] Button _embarkButton;
        [SerializeField] TextMeshProUGUI _loadFailed;

        bool _bindingsChecked;

        void Awake() => ValidateRequiredBindings();

        /// <summary>Logs one error per missing field.</summary>
        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_bastionNode), _bastionNode);
            Require(nameof(_navPanel), _navPanel);
            Require(nameof(_mapPanel), _mapPanel);
            Require(nameof(_destination), _destination);
            Require(nameof(_premise), _premise);
            Require(nameof(_startWaveLine), _startWaveLine);
            Require(nameof(_wavePrev), _wavePrev);
            Require(nameof(_waveValue), _waveValue);
            Require(nameof(_waveNext), _waveNext);
            Require(nameof(_noWeaponWarning), _noWeaponWarning);
            Require(nameof(_noRigWarning), _noRigWarning);
            Require(nameof(_embarkButton), _embarkButton);
            Require(nameof(_loadFailed), _loadFailed);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }

        /// <summary>The nav panel is the default view; picking the live node swaps to the map
        /// panel. Exactly one of the two shows at a time — no back button. Locked nodes are
        /// authored, disabled, and never bound.</summary>
        public void BindMap(Action onSelectBastion, bool mapOpen)
        {
            WireButton(_bastionNode, onSelectBastion, true);
            UiBind.Show(_navPanel, !mapOpen);
            UiBind.Show(_mapPanel, mapOpen);
        }

        public void SetDestination(string text) => UiBind.SetText(_destination, text);

        public void SetPremise(string text) => UiBind.SetText(_premise, text);

        public void SetStartWaveLine(string text) => UiBind.SetText(_startWaveLine, text);

        /// <summary>Code writes the stepper labels — ASCII &lt; / &gt;, because LiberationSans
        /// lacks the triangle glyphs — then wires clicks and enablement.</summary>
        public void BindWaveControls(string waveValue, Action onPrev, bool prevEnabled,
            Action onNext, bool nextEnabled)
        {
            UiBind.SetButtonLabel(_wavePrev, "<");
            UiBind.SetButtonLabel(_waveNext, ">");
            UiBind.SetText(_waveValue, waveValue);
            WireButton(_wavePrev, onPrev, prevEnabled);
            WireButton(_waveNext, onNext, nextEnabled);
        }

        public void ShowWarnings(bool noWeapon, bool noRig)
        {
            UiBind.Show(_noWeaponWarning, noWeapon);
            UiBind.Show(_noRigWarning, noRig);
        }

        public void BindEmbark(string label, Action onEmbark)
        {
            if (_embarkButton == null) return;
            UiBind.SetButtonLabel(_embarkButton, label);
            WireButton(_embarkButton, onEmbark, true);
        }

        public void ShowLoadFailed(bool failed) => UiBind.Show(_loadFailed, failed);

        static void WireButton(Button button, Action onClick, bool enabled)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
            button.interactable = enabled;
        }
    }
}
