using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>One station priced row: name, cost, level, effect, action. Authored in the scene; view binds only.</summary>
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
            if (_name != null) _name.text = name ?? "";
            if (_cost != null)
            {
                _cost.text = cost ?? "";
                _cost.gameObject.SetActive(!string.IsNullOrEmpty(cost));
            }
            if (_level != null)
            {
                _level.text = level ?? "";
                _level.gameObject.SetActive(!string.IsNullOrEmpty(level));
            }
            if (_effect != null) _effect.text = effect ?? "";

            bool hasReason = !string.IsNullOrEmpty(reason);
            if (_reason != null)
            {
                _reason.text = reason ?? "";
                _reason.gameObject.SetActive(hasReason);
            }

            if (_action == null) return;
            bool showAction = !string.IsNullOrEmpty(actionLabel);
            _action.gameObject.SetActive(showAction);
            if (!showAction) return;

            Ui.UiBind.SetButtonLabel(_action, actionLabel);

            _action.onClick.RemoveAllListeners();
            if (onAction != null) _action.onClick.AddListener(() => onAction());
            _action.interactable = actionEnabled;
        }
    }
}
