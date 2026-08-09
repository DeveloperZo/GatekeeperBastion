using System;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>M21 ARMORY upgrade wheel — five spokes, hub, projection lines, buy row.
    /// Hierarchy is authored (Shift Panel Button spokes around a hub). Configure only binds data.</summary>
    public sealed class ArmoryWheelView : MonoBehaviour
    {
        public const int SpokeCount = 5;
        const float RingRadius = 88f;

        [SerializeField] RectTransform _wheelRoot;
        [SerializeField] RectTransform[] _spokeRoots = new RectTransform[SpokeCount];
        [SerializeField] Button[] _spokes = new Button[SpokeCount];
        [SerializeField] Image[] _spokeFills = new Image[SpokeCount];
        [SerializeField] TMP_Text[] _spokeLabels = new TMP_Text[SpokeCount];
        [SerializeField] Image[] _spokeRays = new Image[SpokeCount];
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
            if (_hubLabeled || _hubSub == null) return;
            _hubLabeled = true;
            _hubSub.text = "FOCUS";
        }

        public void Configure(ArmoryWheelDisplay d, Action<int> onSelectCentre, Action buy)
        {
            EnsureBuilt();
            gameObject.SetActive(true);

            for (int i = 0; i < SpokeCount; i++)
            {
                if (_spokes[i] == null) continue;

                int dist = d.SpokeDistances != null && i < d.SpokeDistances.Length
                    ? d.SpokeDistances[i] : 2;
                float angleDeg = -90f + i * (360f / WeaponWheel.AttrCount);
                float rad = angleDeg * Mathf.Deg2Rad;
                float sx = Mathf.Cos(rad) * RingRadius;
                float sy = Mathf.Sin(rad) * RingRadius;

                if (_spokeRoots[i] != null)
                {
                    _spokeRoots[i].anchorMin = _spokeRoots[i].anchorMax = new Vector2(0.5f, 0.5f);
                    _spokeRoots[i].anchoredPosition = new Vector2(sx, sy);
                }

                if (_spokeRays[i] != null)
                {
                    var rayRt = _spokeRays[i].rectTransform;
                    rayRt.anchorMin = rayRt.anchorMax = new Vector2(0.5f, 0.5f);
                    rayRt.pivot = new Vector2(0.5f, 0f);
                    rayRt.sizeDelta = new Vector2(dist == 0 ? 3f : 1.5f, RingRadius);
                    rayRt.anchoredPosition = Vector2.zero;
                    rayRt.localRotation = Quaternion.Euler(0f, 0f, angleDeg + 90f);
                    _spokeRays[i].color = dist == 2 ? Theme.WheelSpokeDark : Theme.WheelSpokeLit;
                }

                Color fill = dist == 0 ? Theme.WheelCentre
                    : dist == 1 ? Theme.WheelNeighbour : Theme.WheelOpposite;
                if (_spokeFills[i] != null) _spokeFills[i].color = fill;

                if (_spokeLabels[i] != null)
                {
                    _spokeLabels[i].text = d.SpokeLabels != null && i < d.SpokeLabels.Length
                        ? d.SpokeLabels[i] : "";
                    _spokeLabels[i].color = dist == 0 ? Theme.WheelTextOnCentre
                        : dist == 1 ? Theme.Ink : Theme.InkFaint;
                }

                int captured = d.SpokeRingIndices != null && i < d.SpokeRingIndices.Length
                    ? d.SpokeRingIndices[i] : i;
                _spokes[i].onClick.RemoveAllListeners();
                _spokes[i].onClick.AddListener(() => onSelectCentre?.Invoke(captured));
            }

            if (_hubTitle != null) _hubTitle.text = d.HubTitle ?? "";
            if (_projFire != null) _projFire.text = d.ProjFireLine ?? "";
            if (_projReload != null) _projReload.text = d.ProjReloadLine ?? "";

            bool showBuy = d.ShowBuyButton;
            if (_buyButton != null)
            {
                _buyButton.gameObject.SetActive(showBuy);
                _buyButton.onClick.RemoveAllListeners();
                if (showBuy) _buyButton.onClick.AddListener(() => buy?.Invoke());
            }
            if (_buyLabel != null && showBuy) _buyLabel.text = d.BuyButtonLabel ?? "";

            bool showReason = !showBuy && !string.IsNullOrEmpty(d.ReasonLine);
            if (_reasonLabel != null)
            {
                _reasonLabel.gameObject.SetActive(showReason);
                if (showReason) _reasonLabel.text = d.ReasonLine;
            }
        }
    }
}
