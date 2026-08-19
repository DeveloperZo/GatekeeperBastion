using System;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>One LoL-style ability square: name, keycap, radial fill, countdown.</summary>
    [Serializable]
    public sealed class AbilitySlotBinding
    {
        public GameObject root;
        public TMP_Text key;
        public TMP_Text name;
        public Image cooldownFill;
        public TMP_Text countdown;
    }

    /// <summary>Bottom-centre protocol bar: Passive, Personal, Crew (ult). Authored in
    /// Bastion.unity; HudController injects names, keys and fills.</summary>
    public sealed class ProtocolAbilityBarView : MonoBehaviour
    {
        const string Tag = "ProtocolAbilityBarView";

        [SerializeField] AbilitySlotBinding _passive;
        [SerializeField] AbilitySlotBinding _personal;
        [SerializeField] AbilitySlotBinding _crew;

        bool _bindingsChecked;

        void Awake() => ValidateRequiredBindings();

        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            RequireSlot("passive", _passive);
            RequireSlot("personal", _personal);
            RequireSlot("crew", _crew);
        }

        public void SetVisible(bool visible) => UiBind.Show(gameObject, visible);

        public void Bind(
            string passiveName, float passiveFill, string passiveCd,
            string personalKey, string personalName, float personalFill, string personalCd,
            string crewKey, string crewName, float crewFill, string crewCd)
        {
            Apply(_passive, "", passiveName, passiveFill, passiveCd);
            Apply(_personal, personalKey, personalName, personalFill, personalCd);
            Apply(_crew, crewKey, crewName, crewFill, crewCd);
        }

        static void Apply(AbilitySlotBinding slot, string key, string abilityName, float fill,
            string countdown)
        {
            if (slot == null) return;
            UiBind.SetText(slot.key, key != null ? key : "");
            UiBind.Show(slot.key, !string.IsNullOrEmpty(key));
            bool cooling = !string.IsNullOrEmpty(countdown);
            UiBind.SetText(slot.name, abilityName != null ? abilityName : "");
            UiBind.Show(slot.name, !cooling);
            if (slot.cooldownFill != null)
            {
                slot.cooldownFill.fillAmount = fill;
                UiBind.Show(slot.cooldownFill, cooling);
            }
            UiBind.SetTextOrHide(slot.countdown, countdown);
        }

        void RequireSlot(string which, AbilitySlotBinding slot)
        {
            if (slot == null || slot.root == null || slot.name == null || slot.cooldownFill == null
                || slot.countdown == null)
            {
                Log.Error(Tag, name + ": missing binding '" + which + "'.");
                return;
            }
            if (which != "passive" && slot.key == null)
                Log.Error(Tag, name + ": missing key on '" + which + "'.");
        }
    }
}
