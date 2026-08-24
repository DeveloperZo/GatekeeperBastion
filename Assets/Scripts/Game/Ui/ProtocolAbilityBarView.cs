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

        /// <summary>Running and cooling are opposite states and must not read the same. The
        /// active flags are what separates them: without one, a square counting down 4s while
        /// the ability is DOING ITS JOB looks exactly like a square counting down 4s until the
        /// player may use it again.</summary>
        public void Bind(
            string passiveName, float passiveFill, string passiveCd,
            string personalKey, string personalName, float personalFill, string personalCd,
            bool personalActive,
            string crewKey, string crewName, float crewFill, string crewCd, bool crewActive)
        {
            Apply(_passive, "", passiveName, passiveFill, passiveCd, false);
            Apply(_personal, personalKey, personalName, personalFill, personalCd, personalActive);
            Apply(_crew, crewKey, crewName, crewFill, crewCd, crewActive);
        }

        /// <summary>Live ability squares keep their name and burn the fill hot; cooling ones
        /// drop the name and burn it dim. Same geometry, opposite reading.</summary>
        static readonly Color ActiveFill = new Color(1f, 0.62f, 0.18f, 1f);
        static readonly Color CoolingFill = new Color(0.32f, 0.36f, 0.42f, 1f);

        static void Apply(AbilitySlotBinding slot, string key, string abilityName, float fill,
            string countdown, bool active)
        {
            if (slot == null) return;
            UiBind.SetText(slot.key, key != null ? key : "");
            UiBind.Show(slot.key, !string.IsNullOrEmpty(key));
            bool hasCountdown = !string.IsNullOrEmpty(countdown);
            UiBind.SetText(slot.name, abilityName != null ? abilityName : "");
            // The name stays up while the ability runs — that is the tell that it is running.
            UiBind.Show(slot.name, active || !hasCountdown);
            if (slot.cooldownFill != null)
            {
                slot.cooldownFill.fillAmount = fill;
                slot.cooldownFill.color = active ? ActiveFill : CoolingFill;
                UiBind.Show(slot.cooldownFill, hasCountdown);
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
