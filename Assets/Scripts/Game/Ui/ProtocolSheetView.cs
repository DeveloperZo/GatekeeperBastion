using System;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: one authored protocol detail template. Bind injects
    /// <see cref="BoarderProtocol.Sheet"/>; layout stays in the scene.</summary>
    public sealed class ProtocolSheetView : MonoBehaviour
    {
        const string Tag = "ProtocolSheetView";

        public const string BackLabel = "BACK";
        public const string TitleHeading = "Protocol Title";
        public const string DescriptionHeading = "Protocol Description";
        public const string CrewHeading = "Crew Ability";
        public const string PersonalHeading = "Personal Ability";
        public const string PassiveHeading = "Passive Ability";

        [SerializeField] TMP_Text _titleLabel;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _descriptionLabel;
        [SerializeField] TMP_Text _doctrine;
        [SerializeField] TMP_Text _crewLabel;
        [SerializeField] TMP_Text _crew;
        [SerializeField] TMP_Text _personalLabel;
        [SerializeField] TMP_Text _personal;
        [SerializeField] TMP_Text _passiveLabel;
        [SerializeField] TMP_Text _passive;
        [SerializeField] Button _claim;
        [SerializeField] Button _back;

        bool _bindingsChecked;

        void Awake() => ValidateRequiredBindings();

        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_titleLabel), _titleLabel);
            Require(nameof(_title), _title);
            Require(nameof(_descriptionLabel), _descriptionLabel);
            Require(nameof(_doctrine), _doctrine);
            Require(nameof(_crewLabel), _crewLabel);
            Require(nameof(_crew), _crew);
            Require(nameof(_personalLabel), _personalLabel);
            Require(nameof(_personal), _personal);
            Require(nameof(_passiveLabel), _passiveLabel);
            Require(nameof(_passive), _passive);
            Require(nameof(_claim), _claim);
            Require(nameof(_back), _back);
        }

        public void Bind(BoarderProtocol.Sheet sheet, string claimLabel, Action onClaim,
            bool claimEnabled, Action onBack)
        {
            SetHeading(_titleLabel, TitleHeading);
            Wrap(_title, sheet.Name);
            SetHeading(_descriptionLabel, DescriptionHeading);
            Wrap(_doctrine, sheet.Doctrine);
            SetHeading(_crewLabel, CrewHeading);
            Wrap(_crew, sheet.Crew);
            SetHeading(_personalLabel, PersonalHeading);
            Wrap(_personal, sheet.Personal);
            SetHeading(_passiveLabel, PassiveHeading);
            Wrap(_passive, sheet.Passive);

            if (_claim != null)
            {
                UiBind.Show(_claim, !string.IsNullOrEmpty(claimLabel));
                if (!string.IsNullOrEmpty(claimLabel))
                {
                    UiBind.SetButtonLabel(_claim, claimLabel);
                    _claim.onClick.RemoveAllListeners();
                    if (onClaim != null && claimEnabled)
                        _claim.onClick.AddListener(() => onClaim());
                    _claim.interactable = claimEnabled;
                }
            }

            if (_back == null) return;
            UiBind.Show(_back, onBack != null);
            if (onBack == null) return;
            UiBind.SetButtonLabel(_back, BackLabel);
            _back.onClick.RemoveAllListeners();
            _back.onClick.AddListener(() => onBack());
            _back.interactable = true;
        }

        static void SetHeading(TMP_Text tmp, string text)
        {
            UiBind.SetText(tmp, text);
            if (tmp == null) return;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        static void Wrap(TMP_Text tmp, string text)
        {
            UiBind.SetText(tmp, text != null ? text : "");
            if (tmp == null) return;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }
    }
}
