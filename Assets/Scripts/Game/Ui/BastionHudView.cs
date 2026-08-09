using System;
using System.Collections.Generic;
using GatekeeperBastion.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>View-only Bastion combat HUD. HudController pushes strings and fill values;
    /// this class never reads sim or session state. Hierarchy is authored (Shift variant).</summary>
    public sealed class BastionHudView : MonoBehaviour
    {
        public const float ShortcutBandWidth = 420f;
        public const float ShortcutMargin = 12f;
        const float IrrelevantAlpha = 0.4f;

        public event Action ReturnClicked;

        /// <summary>Bottom band height from <see cref="ShortcutPanel.LastDrawnHeight"/>.</summary>
        public float ShortcutBandHeight => ShortcutPanel.LastDrawnHeight;

        [SerializeField] RectTransform _root;
        [SerializeField] RectTransform _safeArea;

        [Header("Vignette")]
        [SerializeField] GameObject _vignetteRoot;
        [SerializeField] Image _vignetteImage;
        [SerializeField] TMP_Text _downLabel;

        [Header("Fight")]
        [SerializeField] GameObject _fightRoot;
        [SerializeField] CanvasGroup _fightGroup;
        [SerializeField] TMP_Text _timeWave;
        [SerializeField] TMP_Text _siphonLine;
        [SerializeField] TMP_Text _salvageLine;
        [SerializeField] ValueAnimator _salvagePunch;
        [SerializeField] TMP_Text _hatchLabel;
        [SerializeField] ValueAnimator _hatchBar;
        [SerializeField] TMP_Text _playerLabel;
        [SerializeField] ValueAnimator _playerBar;
        [SerializeField] GameObject _staminaBlock;
        [SerializeField] CanvasGroup _staminaGroup;
        [SerializeField] TMP_Text _staminaLabel;
        [SerializeField] ValueAnimator _staminaBar;
        [SerializeField] GameObject _weaponStack;
        [SerializeField] CanvasGroup _weaponGroup;
        [SerializeField] TMP_Text _weaponBlock;
        [SerializeField] GameObject _reloadBlock;
        [SerializeField] CanvasGroup _reloadGroup;
        [SerializeField] TMP_Text _reloadLabel;
        [SerializeField] ValueAnimator _reloadBar;
        [SerializeField] GameObject _abilityBlock;
        [SerializeField] CanvasGroup _abilityGroup;
        [SerializeField] TMP_Text _abilityLabel;
        [SerializeField] ValueAnimator _abilityBar;

        [Header("Crosshair / Prompt")]
        [SerializeField] GameObject _crosshairRoot;
        [SerializeField] TMP_Text _crosshairCenter;
        [SerializeField] TMP_Text _crosshairLeft;
        [SerializeField] TMP_Text _crosshairRight;
        [SerializeField] GameObject _promptRoot;
        [SerializeField] TMP_Text _promptText;

        [Header("Prep / Assault")]
        [SerializeField] GameObject _prepRoot;
        [SerializeField] TMP_Text _prepHeading;
        [SerializeField] TMP_Text _prepLine1;
        [SerializeField] TMP_Text _prepLine2;
        [SerializeField] TMP_Text _prepLine3;
        [SerializeField] TMP_Text _prepLine4;
        [SerializeField] GameObject _assaultRoot;
        [SerializeField] TMP_Text _assaultHeading;
        [SerializeField] GameObject _hatchAlarmRoot;
        [SerializeField] TMP_Text _hatchAlarmText;

        [Header("Shortcut / Arriving")]
        [SerializeField] GameObject _shortcutRoot;
        [SerializeField] ShortcutPanelView _shortcuts;
        [SerializeField] GameObject _arrivingRoot;
        [SerializeField] TMP_Text _arrivingText;

        [Header("End / Emergency")]
        [SerializeField] EndScreenView _endScreen;
        [SerializeField] GameObject _emergencyRoot;
        [SerializeField] TMP_Text _emergencyText;
        [SerializeField] Button _emergencyButton;

        bool _returnWired;
        string _lastSalvage;

        /// <summary>authored prefab required — wires buttons and activates the safe area.</summary>
        public void EnsureBuilt()
        {
            if (_safeArea != null) _safeArea.gameObject.SetActive(true);
            if (_root != null) _root.gameObject.SetActive(true);
            _shortcuts?.EnsureBuilt();
            _endScreen?.EnsureBuilt();

            if (_returnWired) return;
            _returnWired = true;
            if (_endScreen != null) _endScreen.ProceedClicked += OnReturn;
            if (_emergencyButton != null)
            {
                _emergencyButton.onClick.RemoveAllListeners();
                _emergencyButton.onClick.AddListener(OnReturn);
            }
        }

        void OnDestroy()
        {
            if (_endScreen != null) _endScreen.ProceedClicked -= OnReturn;
        }

        void OnReturn() => ReturnClicked?.Invoke();

        public void HideAll()
        {
            SetVisible(_vignetteRoot, false);
            SetVisible(_fightRoot, false);
            SetVisible(_crosshairRoot, false);
            SetVisible(_promptRoot, false);
            SetVisible(_prepRoot, false);
            SetVisible(_assaultRoot, false);
            SetVisible(_hatchAlarmRoot, false);
            SetVisible(_shortcutRoot, false);
            SetVisible(_arrivingRoot, false);
            _endScreen?.SetVisible(false);
            SetVisible(_emergencyRoot, false);
        }

        public void SetFightVisible(bool visible) => SetVisible(_fightRoot, visible);

        public void SetCrosshairVisible(bool visible) => SetVisible(_crosshairRoot, visible);

        public void SetPromptVisible(bool visible) => SetVisible(_promptRoot, visible);

        public void SetPrepVisible(bool visible) => SetVisible(_prepRoot, visible);

        public void SetAssaultVisible(bool visible) => SetVisible(_assaultRoot, visible);

        public void SetHatchAlarmVisible(bool visible) => SetVisible(_hatchAlarmRoot, visible);

        public void SetShortcutVisible(bool visible) => SetVisible(_shortcutRoot, visible);

        public void SetArrivingVisible(bool visible) => SetVisible(_arrivingRoot, visible);

        public void SetEndVisible(bool visible)
        {
            if (_endScreen != null) _endScreen.SetVisible(visible);
        }

        public void SetEmergencyVisible(bool visible) => SetVisible(_emergencyRoot, visible);

        public void SetTimeWave(string text) => SetText(_timeWave, text);

        public void SetSiphonLine(string text) => SetText(_siphonLine, text);

        public void SetSalvageLine(string text)
        {
            SetText(_salvageLine, text);
            if (!string.IsNullOrEmpty(text) && text != _lastSalvage)
            {
                _salvagePunch?.Punch();
                _lastSalvage = text;
            }
        }

        public void SetHatchLabel(string text, bool alarmStyle)
        {
            SetText(_hatchLabel, text);
            if (_hatchLabel != null)
                _hatchLabel.color = alarmStyle ? Theme.AlarmText : Theme.InkDim;
        }

        public void SetHatchBar(float value, float max, Color fill)
        {
            if (_hatchBar == null) return;
            _hatchBar.SetFillColor(fill);
            _hatchBar.SetBar(value, max);
        }

        public void SetPlayerLabel(string text)
        {
            SetText(_playerLabel, text);
            if (_playerLabel != null)
                _playerLabel.color = text != null && text.Contains("(DOWN)") ? Theme.AlarmText : Theme.InkDim;
        }

        public void SetPlayerBar(float value, float max)
        {
            if (_playerBar == null) return;
            _playerBar.SetFillColor(Theme.PlayerHp);
            _playerBar.SetBar(value, max);
        }

        public void SetStaminaBlockVisible(bool visible) => SetRelevance(_staminaBlock, _staminaGroup, visible);

        public void SetStaminaLabel(string text) => SetText(_staminaLabel, text);

        public void SetStaminaBar(float value, float max, bool sprinting)
        {
            if (_staminaBar == null) return;
            _staminaBar.SetFillColor(sprinting ? Theme.StaminaActive : Theme.Stamina);
            _staminaBar.SetBar(value, max);
        }

        public void SetReloadBlockVisible(bool visible) => SetRelevance(_reloadBlock, _reloadGroup, visible);

        public void SetReloadLabel(string text) => SetText(_reloadLabel, text);

        public void SetReloadBar(float progress)
        {
            if (_reloadBar == null) return;
            _reloadBar.SetFillColor(Theme.Warn);
            _reloadBar.SetBar(progress, 1f);
        }

        public void SetAbilityBlockVisible(bool visible) => SetRelevance(_abilityBlock, _abilityGroup, visible);

        public void SetAbilityLabel(string text) => SetText(_abilityLabel, text);

        public void SetAbilityBar(float progress, Color fill)
        {
            if (_abilityBar == null) return;
            _abilityBar.SetFillColor(fill);
            _abilityBar.SetBar(progress, 1f);
        }

        public void SetWeaponBlock(string text)
        {
            SetText(_weaponBlock, text);
            bool has = !string.IsNullOrEmpty(text);
            SetRelevance(_weaponStack, _weaponGroup, has);
        }

        public void SetCrosshair(CrosshairShape shape)
        {
            SetVisible(_crosshairCenter, shape == CrosshairShape.Cross || shape == CrosshairShape.Dot);
            SetVisible(_crosshairLeft, shape == CrosshairShape.Brackets);
            SetVisible(_crosshairRight, shape == CrosshairShape.Brackets);
            if (_crosshairCenter != null)
                _crosshairCenter.text = shape == CrosshairShape.Dot ? "•" : "+";
        }

        public void SetPrompt(string text) => SetText(_promptText, text);

        public void SetPrepHeading(string text, bool alarmStyle)
        {
            SetText(_prepHeading, text);
            if (_prepHeading != null)
                _prepHeading.color = alarmStyle ? Theme.AlarmText : Theme.Ink;
        }

        public void SetPrepLine(int index, string text, bool alarmStyle)
        {
            var label = index switch
            {
                1 => _prepLine1,
                2 => _prepLine2,
                3 => _prepLine3,
                4 => _prepLine4,
                _ => null
            };
            SetText(label, text);
            if (label != null)
            {
                label.color = alarmStyle ? Theme.AlarmText : Theme.InkDim;
                SetVisible(label.gameObject, !string.IsNullOrEmpty(text));
            }
        }

        public void SetAssaultHeading(string text) => SetText(_assaultHeading, text);

        public void SetHatchAlarmStrip(string text) => SetText(_hatchAlarmText, text);

        public void SetShortcutLines(IReadOnlyList<string> lines, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed && lines != null && lines.Count > 0)
                _shortcuts.SetLines(lines);
            SetVisible(_shortcutRoot, collapsed || (lines != null && lines.Count > 0));
        }

        public void SetArriving(string text) => SetText(_arrivingText, text);

        public void SetEndScreen(
            string cause, string framing, string earned, string repair, string net,
            string banked, string deepest)
        {
            EnsureBuilt();
            if (_endScreen == null) return;

            // Primary stats (≤6). Banked drives the salvage count-up separately.
            var primary = new[] { earned, repair, net, deepest };
            float salvage = ParseTrailingNumber(banked);
            _endScreen.Present(cause, framing, primary, salvage, banked, null);
        }

        public void SetEmergencyText(string text) => SetText(_emergencyText, text);

        public void SetVignette(float alpha, bool downOverlay, string downText)
        {
            bool show = alpha > 0.001f || downOverlay;
            SetVisible(_vignetteRoot, show);
            if (!show || _vignetteImage == null) return;

            if (downOverlay)
            {
                _vignetteImage.color = Theme.VignetteDown;
                SetVisible(_downLabel, true);
                SetText(_downLabel, downText);
            }
            else
            {
                var c = Theme.VignetteDamage;
                c.a = alpha;
                _vignetteImage.color = c;
                SetVisible(_downLabel, false);
            }
        }

        public void LayoutPrompt(float shortcutHeight)
        {
            if (_promptRoot == null) return;
            var rt = _promptRoot.GetComponent<RectTransform>();
            float bottom = Mathf.Max(shortcutHeight, 40f) + 36f;
            rt.offsetMin = new Vector2(ShortcutBandWidth + ShortcutMargin, bottom);
            rt.offsetMax = new Vector2(-12f, bottom + 30f);
        }

        public void LayoutPrepStrip(float shortcutHeight)
        {
            if (_prepRoot == null) return;
            float bottomClear = Mathf.Max(shortcutHeight, 40f) + 8f;
            var rt = _prepRoot.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(ShortcutBandWidth + ShortcutMargin + 8f, bottomClear);
            rt.offsetMax = new Vector2(-12f, bottomClear + 170f);
        }

        public void LayoutAssaultStrip(float shortcutHeight)
        {
            if (_assaultRoot == null) return;
            float bottomClear = Mathf.Max(shortcutHeight, 40f) + 8f;
            var rt = _assaultRoot.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(ShortcutBandWidth + ShortcutMargin + 8f, bottomClear);
            rt.offsetMax = new Vector2(-12f, bottomClear + 46f);

            if (_hatchAlarmRoot != null)
            {
                var alarmRt = _hatchAlarmRoot.GetComponent<RectTransform>();
                alarmRt.offsetMin = new Vector2(ShortcutBandWidth + ShortcutMargin + 8f, bottomClear + 46f + 8f);
                alarmRt.offsetMax = new Vector2(-12f, bottomClear + 46f + 8f + 30f);
            }
        }

        /// <summary>Irrelevant blocks stay on-screen at ~40% alpha (criterion 20).</summary>
        static void SetRelevance(GameObject block, CanvasGroup group, bool relevant)
        {
            if (block != null && !block.activeSelf) block.SetActive(true);
            if (group != null)
            {
                group.alpha = relevant ? 1f : IrrelevantAlpha;
                group.interactable = relevant;
                group.blocksRaycasts = relevant;
                return;
            }
            SetVisible(block, relevant);
        }

        static float ParseTrailingNumber(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0f;
            int i = line.Length - 1;
            while (i >= 0 && (char.IsDigit(line[i]) || line[i] == ',' || line[i] == '.')) i--;
            string num = line.Substring(i + 1).Replace(",", "");
            return float.TryParse(num, out float v) ? v : 0f;
        }

        static void SetText(TMP_Text label, string text)
        {
            if (label == null) return;
            label.text = text ?? string.Empty;
        }

        static void SetVisible(GameObject go, bool visible)
        {
            if (go != null) go.SetActive(visible);
        }

        static void SetVisible(TMP_Text label, bool visible)
        {
            if (label != null) label.gameObject.SetActive(visible);
        }
    }

    public enum CrosshairShape
    {
        Cross,
        Dot,
        Brackets
    }
}
