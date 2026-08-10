using System;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>Binds the authored ARMORY upgrade wheel — five spokes placed around a hub in the
    /// scene, not by this class. <see cref="Configure"/> writes labels, marks the centre spoke and
    /// arms the buy action; the ring geometry and every colour are the authored panel's
    /// (M35 criteria 18, 38).</summary>
    public sealed class ArmoryWheelView : MonoBehaviour
    {
        public const int SpokeCount = 5;

        [SerializeField] Button[] _spokes = new Button[SpokeCount];
        [SerializeField] TMP_Text[] _spokeLabels = new TMP_Text[SpokeCount];
        /// <summary>Optional per-spoke "this is the centre" decoration, authored from Shift parts.</summary>
        [SerializeField] GameObject[] _spokeSelected;
        [SerializeField] TMP_Text _hubTitle;
        [SerializeField] TMP_Text _hubSub;
        [SerializeField] TMP_Text _projFire;
        [SerializeField] TMP_Text _projReload;
        [SerializeField] Button _buyButton;
        [SerializeField] TMP_Text _buyLabel;
        [SerializeField] TMP_Text _reasonLabel;

        bool _hubLabeled;

        public void EnsureBuilt()
        {
            if (_hubLabeled) return;
            _hubLabeled = true;
            UiBind.SetText(_hubSub, "FOCUS");
        }

        public void Configure(ArmoryWheelDisplay d, Action<int> onSelectCentre, Action buy)
        {
            EnsureBuilt();
            gameObject.SetActive(true);

            for (int i = 0; i < SpokeCount; i++)
            {
                if (_spokes == null || i >= _spokes.Length || _spokes[i] == null) continue;

                // Distance 0 is the spoke currently in focus; 1 neighbours it; 2 is opposite.
                int dist = d.SpokeDistances != null && i < d.SpokeDistances.Length
                    ? d.SpokeDistances[i] : 2;
                bool centre = dist == 0;

                if (_spokeSelected != null && i < _spokeSelected.Length)
                    UiBind.Show(_spokeSelected[i], centre);

                if (_spokeLabels != null && i < _spokeLabels.Length)
                {
                    string text = d.SpokeLabels != null && i < d.SpokeLabels.Length
                        ? d.SpokeLabels[i] : "";
                    UiBind.SetText(_spokeLabels[i], centre ? $"<b>{text}</b>" : text);
                }

                int captured = d.SpokeRingIndices != null && i < d.SpokeRingIndices.Length
                    ? d.SpokeRingIndices[i] : i;
                _spokes[i].onClick.RemoveAllListeners();
                _spokes[i].onClick.AddListener(() => onSelectCentre?.Invoke(captured));
            }

            UiBind.SetText(_hubTitle, d.HubTitle);
            UiBind.SetText(_projFire, d.ProjFireLine);
            UiBind.SetText(_projReload, d.ProjReloadLine);

            bool showBuy = d.ShowBuyButton;
            if (_buyButton != null)
            {
                UiBind.Show(_buyButton, showBuy);
                _buyButton.onClick.RemoveAllListeners();
                if (showBuy) _buyButton.onClick.AddListener(() => buy?.Invoke());
            }
            if (showBuy) UiBind.SetText(_buyLabel, d.BuyButtonLabel);

            UiBind.SetTextOrHide(_reasonLabel, showBuy ? null : d.ReasonLine);
        }
    }
}
