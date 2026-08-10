using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Binds the authored Bastion HUD. HudController pushes strings and values; nothing
    /// here reads sim state or moves a RectTransform. All hosts are serialized — missing wires
    /// log once at EnsureBuilt (M35 criteria 26, 30, 34).</summary>
    public sealed class BastionHudView : MonoBehaviour
    {
        const float IrrelevantAlpha = 0.4f;

        public event Action ReturnClicked;

        [SerializeField] RectTransform _root;

        [Header("Vignette")]
        [SerializeField] GameObject _vignetteRoot;
        [SerializeField, OptionalBinding] TMP_Text _downLabel;

        [Header("Fight")]
        [SerializeField] GameObject _fightRoot;
        [SerializeField] TMP_Text _timeWave;
        [SerializeField] TMP_Text _siphonLine;
        [SerializeField] TMP_Text _salvageLine;
        [SerializeField] TMP_Text _hatchLabel;
        [SerializeField] Slider _hatchBar;
        [SerializeField] TMP_Text _playerLabel;
        [SerializeField] Slider _playerBar;
        [SerializeField] GameObject _staminaBlock;
        [SerializeField, OptionalBinding] CanvasGroup _staminaGroup;
        [SerializeField] TMP_Text _staminaLabel;
        [SerializeField] Slider _staminaBar;
        [SerializeField] GameObject _weaponStack;
        [SerializeField, OptionalBinding] CanvasGroup _weaponGroup;
        [SerializeField] TMP_Text _weaponBlock;
        [SerializeField] GameObject _reloadBlock;
        [SerializeField, OptionalBinding] CanvasGroup _reloadGroup;
        [SerializeField] TMP_Text _reloadLabel;
        [SerializeField] Slider _reloadBar;
        [SerializeField] GameObject _abilityBlock;
        [SerializeField, OptionalBinding] CanvasGroup _abilityGroup;
        [SerializeField] TMP_Text _abilityLabel;
        [SerializeField] Slider _abilityBar;

        [Header("Crosshair / Prompt")]
        [SerializeField] GameObject _crosshairRoot;
        [SerializeField] GameObject _crosshairCross;
        [SerializeField] GameObject _crosshairDot;
        [SerializeField] GameObject _crosshairBrackets;
        [SerializeField] GameObject _promptRoot;
        [SerializeField] TMP_Text _promptText;

        [Header("Prep / Assault")]
        [SerializeField] GameObject _prepRoot;
        [SerializeField] TMP_Text _prepHeading;
        [SerializeField] TMP_Text[] _prepLines = new TMP_Text[4];
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
        bool _missingLogged;
        float _vignetteAlpha = -1f;
        bool _vignetteDown;
        CrosshairShape _crosshair = (CrosshairShape)(-1);

        /// <summary>Arms button listeners once. Logs one error per null required field.</summary>
        public void EnsureBuilt()
        {
            if (!_missingLogged)
            {
                _missingLogged = true;
                LogMissing(nameof(_root), _root);
                LogMissing(nameof(_fightRoot), _fightRoot);
                LogMissing(nameof(_endScreen), _endScreen);
                LogMissing(nameof(_shortcuts), _shortcuts);
            }

            if (_returnWired) return;
            _returnWired = true;
            if (_endScreen != null) _endScreen.ProceedClicked += OnReturn;
            if (_emergencyButton != null)
            {
                _emergencyButton.onClick.RemoveAllListeners();
                _emergencyButton.onClick.AddListener(OnReturn);
            }
            _shortcuts?.EnsureBuilt();
            _endScreen?.EnsureBuilt();
        }

        void LogMissing(string field, UnityEngine.Object value)
        {
            if (value == null)
                Debug.LogError($"[BastionHudView] '{name}' missing serialized {field}.", this);
        }

        void OnDestroy()
        {
            if (_endScreen != null) _endScreen.ProceedClicked -= OnReturn;
        }

        void OnReturn() => ReturnClicked?.Invoke();

        /// <summary>One-shot visibility for a phase transition — not called every frame.</summary>
        public void ShowArriving()
        {
            HideChrome();
            UiBind.Show(_arrivingRoot, true);
        }

        /// <summary>Fight chrome on; prep or assault strip chosen by <paramref name="prep"/>.</summary>
        public void ShowFight(bool prep)
        {
            HideChrome();
            UiBind.Show(_fightRoot, true);
            UiBind.Show(_crosshairRoot, true);
            UiBind.Show(_shortcutRoot, true);
            UiBind.Show(_prepRoot, prep);
            UiBind.Show(_assaultRoot, !prep);
        }

        public void ShowEnd()
        {
            HideChrome();
            _endScreen?.SetVisible(true);
            UiBind.Show(_shortcutRoot, true);
        }

        public void ShowEmergency()
        {
            HideChrome();
            UiBind.Show(_emergencyRoot, true);
            UiBind.Show(_shortcutRoot, true);
        }

        public void HideChrome()
        {
            UiBind.Show(_vignetteRoot, false);
            UiBind.Show(_fightRoot, false);
            UiBind.Show(_crosshairRoot, false);
            UiBind.Show(_promptRoot, false);
            UiBind.Show(_prepRoot, false);
            UiBind.Show(_assaultRoot, false);
            UiBind.Show(_hatchAlarmRoot, false);
            UiBind.Show(_shortcutRoot, false);
            UiBind.Show(_arrivingRoot, false);
            _endScreen?.SetVisible(false);
            UiBind.Show(_emergencyRoot, false);
        }

        // Transition shims — keep both event-driven and legacy callers compiling.
        public void HideAll() => HideChrome();
        public void SetFightVisible(bool visible) => UiBind.Show(_fightRoot, visible);
        public void SetCrosshairVisible(bool visible) => UiBind.Show(_crosshairRoot, visible);
        public void SetPrepVisible(bool visible) => UiBind.Show(_prepRoot, visible);
        public void SetAssaultVisible(bool visible) => UiBind.Show(_assaultRoot, visible);
        public void SetShortcutVisible(bool visible) => UiBind.Show(_shortcutRoot, visible);
        public void SetArrivingVisible(bool visible) => UiBind.Show(_arrivingRoot, visible);
        public void SetEndVisible(bool visible) => _endScreen?.SetVisible(visible);
        public void SetEmergencyVisible(bool visible) => UiBind.Show(_emergencyRoot, visible);

        public void SetShortcutLines(IReadOnlyList<string> lines, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed && lines != null) _shortcuts.SetLines(lines);
            UiBind.Show(_shortcutRoot, collapsed || (lines != null && lines.Count > 0));
        }

        public void SetHatchAlarmVisible(bool visible) => UiBind.Show(_hatchAlarmRoot, visible);

        public void SetPromptVisible(bool visible) => UiBind.Show(_promptRoot, visible);

        public void SetTimeWave(string text) => UiBind.SetText(_timeWave, text);

        public void SetSiphonLine(string text) => UiBind.SetText(_siphonLine, text);

        public void SetSalvageLine(string text) => UiBind.SetText(_salvageLine, text);

        public void SetHatchLabel(string text) => UiBind.SetText(_hatchLabel, text);

        public void SetHatchBar(float value, float max) => UiBind.SetBar(_hatchBar, value, max);

        public void SetPlayerLabel(string text) => UiBind.SetText(_playerLabel, text);

        public void SetPlayerBar(float value, float max) => UiBind.SetBar(_playerBar, value, max);

        public void SetStaminaBlockVisible(bool visible) =>
            UiBind.SetRelevant(_staminaBlock, _staminaGroup, visible, IrrelevantAlpha);

        public void SetStaminaLabel(string text) => UiBind.SetText(_staminaLabel, text);

        public void SetStaminaBar(float value, float max) => UiBind.SetBar(_staminaBar, value, max);

        public void SetReloadBlockVisible(bool visible) =>
            UiBind.SetRelevant(_reloadBlock, _reloadGroup, visible, IrrelevantAlpha);

        public void SetReloadLabel(string text) => UiBind.SetText(_reloadLabel, text);

        public void SetReloadBar(float progress) => UiBind.SetBar(_reloadBar, progress, 1f);

        public void SetAbilityBlockVisible(bool visible) =>
            UiBind.SetRelevant(_abilityBlock, _abilityGroup, visible, IrrelevantAlpha);

        public void SetAbilityLabel(string text) => UiBind.SetText(_abilityLabel, text);

        public void SetAbilityBar(float progress) => UiBind.SetBar(_abilityBar, progress, 1f);

        public void SetWeaponBlock(string text)
        {
            UiBind.SetText(_weaponBlock, text);
            UiBind.SetRelevant(_weaponStack, _weaponGroup, !string.IsNullOrEmpty(text), IrrelevantAlpha);
        }

        public void SetCrosshair(CrosshairShape shape)
        {
            if (_crosshair == shape) return;
            _crosshair = shape;
            UiBind.Show(_crosshairCross, shape == CrosshairShape.Cross);
            UiBind.Show(_crosshairDot, shape == CrosshairShape.Dot);
            UiBind.Show(_crosshairBrackets, shape == CrosshairShape.Brackets);
        }

        public void SetPrompt(string text) => UiBind.SetText(_promptText, text);

        public void SetPrepHeading(string text) => UiBind.SetText(_prepHeading, text);

        public void SetPrepLine(int index, string text)
        {
            int i = index - 1;
            if (_prepLines == null || i < 0 || i >= _prepLines.Length) return;
            UiBind.SetTextOrHide(_prepLines[i], text);
        }

        public void SetAssaultHeading(string text) => UiBind.SetText(_assaultHeading, text);

        public void SetHatchAlarmStrip(string text) => UiBind.SetText(_hatchAlarmText, text);

        public void SetShortcutRows(IReadOnlyList<ShortcutRow> rows, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed && rows != null) _shortcuts.SetRows(rows);
            UiBind.Show(_shortcutRoot, collapsed || (rows != null && rows.Count > 0));
        }

        public void SetArriving(string text) => UiBind.SetText(_arrivingText, text);

        public void SetEndScreen(
            string cause, string framing, string earned, string repair, string net,
            string banked, string deepest)
        {
            EnsureBuilt();
            if (_endScreen == null) return;
            var primary = new[] { earned, repair, net, deepest };
            _endScreen.Present(cause, framing, primary, ParseTrailingNumber(banked), banked, null);
        }

        public void SetEmergencyText(string text) => UiBind.SetText(_emergencyText, text);

        public void SetVignette(float alpha, bool downOverlay, string downText)
        {
            bool show = alpha > 0.001f || downOverlay;
            UiBind.Show(_vignetteRoot, show);
            if (!show || _vignetteRoot == null) return;

            float target = downOverlay ? 1f : Mathf.Clamp01(alpha);
            if (!Mathf.Approximately(_vignetteAlpha, target) || _vignetteDown != downOverlay)
            {
                _vignetteAlpha = target;
                _vignetteDown = downOverlay;
                var group = _vignetteRoot.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = target;
            }
            UiBind.SetTextOrHide(_downLabel, downOverlay ? downText : null);
        }

        static float ParseTrailingNumber(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0f;
            int i = line.Length - 1;
            while (i >= 0 && (char.IsDigit(line[i]) || line[i] == ',' || line[i] == '.')) i--;
            string num = line.Substring(i + 1).Replace(",", "");
            return float.TryParse(num, out float v) ? v : 0f;
        }
    }

    public enum CrosshairShape
    {
        Cross,
        Dot,
        Brackets
    }
}
