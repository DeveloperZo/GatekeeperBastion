using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: one station priced row: name, cost, level, effect, action. Authored in the scene; view binds only.</summary>
    public sealed class PanelRowView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _name;
        [SerializeField] TextMeshProUGUI _cost;
        [SerializeField] TextMeshProUGUI _level;
        [SerializeField] TextMeshProUGUI _effect;
        [SerializeField] TextMeshProUGUI _reason;
        [SerializeField] Button _action;

        public void Bind(string name, string cost, string level, string effect,
            string actionLabel, Action onAction, bool actionEnabled, string reason = null)
        {
            if (_name != null) _name.text = name != null ? name : "";
            if (_cost != null)
            {
                _cost.text = cost != null ? cost : "";
                _cost.gameObject.SetActive(!string.IsNullOrEmpty(cost));
            }
            if (_level != null)
            {
                _level.text = level != null ? level : "";
                _level.gameObject.SetActive(!string.IsNullOrEmpty(level));
            }
            if (_effect != null) _effect.text = effect != null ? effect : "";

            bool hasReason = !string.IsNullOrEmpty(reason);
            bool showAction = _action != null && !string.IsNullOrEmpty(actionLabel);
            // Disabled BUY + OWNED/EQUIPPED as a second label sat on the button.
            // Status occupies the action slot; the aside Reason row stays off.
            bool statusOnAction = showAction && !actionEnabled && hasReason;
            if (_reason != null)
            {
                bool showAside = hasReason && !statusOnAction;
                _reason.text = showAside ? reason : "";
                _reason.gameObject.SetActive(showAside);
            }

            if (_action == null) return;
            _action.gameObject.SetActive(showAction);
            if (!showAction) return;

            UiBind.SetButtonLabel(_action, statusOnAction ? reason : actionLabel);

            _action.onClick.RemoveAllListeners();
            if (onAction != null && actionEnabled)
                _action.onClick.AddListener(() => onAction());
            _action.interactable = actionEnabled;
        }
    }
}
