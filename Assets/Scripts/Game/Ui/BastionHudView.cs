using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Binds the authored <c>Canvas - In Game</c> copied out of Shift's
    /// <c>Simple HUD (Desktop).unity</c>. HudController pushes strings and values; nothing here
    /// reads sim state, sets a colour, or moves a RectTransform — the look is Shift UI Manager's
    /// and the layout is the scene's (M35 criteria 6, 11, 17).</summary>
    public sealed class BastionHudView : MonoBehaviour
    {
        /// <summary>Shift's HUD root, and the two bar blocks inside it.</summary>
        const string InGameCanvas = "Canvas - In Game";
        const string BarsBlock = "Armor & Health";
        const string HatchBlockName = "Armor";
        const string PlayerBlockName = "Health";
        const string MissionBlock = "Active Mission";

        const float IrrelevantAlpha = 0.4f;

        public event Action ReturnClicked;

        [SerializeField] RectTransform _root;

        [Header("Vignette")]
        [SerializeField] GameObject _vignetteRoot;
        [SerializeField] TMP_Text _downLabel;

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
        [SerializeField] CanvasGroup _staminaGroup;
        [SerializeField] TMP_Text _staminaLabel;
        [SerializeField] Slider _staminaBar;
        [SerializeField] GameObject _weaponStack;
        [SerializeField] CanvasGroup _weaponGroup;
        [SerializeField] TMP_Text _weaponBlock;
        [SerializeField] GameObject _reloadBlock;
        [SerializeField] CanvasGroup _reloadGroup;
        [SerializeField] TMP_Text _reloadLabel;
        [SerializeField] Slider _reloadBar;
        [SerializeField] GameObject _abilityBlock;
        [SerializeField] CanvasGroup _abilityGroup;
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

        bool _resolved;
        bool _returnWired;

        /// <summary>Resolves whatever the scene author did not wire, then arms the buttons.
        /// Safe to call every frame; the work happens once.</summary>
        public void EnsureBuilt()
        {
            Resolve();

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

        /// <summary>Name lookup into the Shift copy. Re-runs until the Canvas exists, so the
        /// HUD latches on whether the copy lands before or after this component.</summary>
        void Resolve()
        {
            if (_resolved) return;

            Transform canvas = _root != null ? _root : UiBind.Root(InGameCanvas);
            if (canvas == null) return;
            _resolved = true;
            if (_root == null) _root = canvas as RectTransform;

            Transform bars = UiBind.Find(canvas, BarsBlock);
            BindBar(bars, HatchBlockName, ref _hatchBar, ref _hatchLabel);
            BindBar(bars, PlayerBlockName, ref _playerBar, ref _playerLabel);

            if (_prepRoot == null)
            {
                var mission = UiBind.Find(canvas, MissionBlock);
                if (mission != null) _prepRoot = mission.gameObject;
            }

            if (_fightRoot == null && bars != null) _fightRoot = bars.gameObject;
            if (_shortcuts == null) _shortcuts = UiBind.Get<ShortcutPanelView>(canvas);
            if (_endScreen == null) _endScreen = UiBind.First<EndScreenView>();
        }

        /// <summary>Criterion 11: one binding, one source — the slider and its numeric label
        /// come out of the same block so they cannot drift apart.</summary>
        static void BindBar(Transform bars, string blockName, ref Slider bar, ref TMP_Text label)
        {
            if (bars == null) return;
            var block = UiBind.Find(bars, blockName);
            if (block == null) return;
            if (bar == null) bar = UiBind.Get<Slider>(block);
            if (label == null) label = UiBind.Get<TMP_Text>(block, "Text") ?? UiBind.Get<TMP_Text>(block);
        }

        void OnDestroy()
        {
            if (_endScreen != null) _endScreen.ProceedClicked -= OnReturn;
        }

        void OnReturn() => ReturnClicked?.Invoke();

        public void HideAll()
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

        public void SetFightVisible(bool visible) => UiBind.Show(_fightRoot, visible);

        public void SetCrosshairVisible(bool visible) => UiBind.Show(_crosshairRoot, visible);

        public void SetPromptVisible(bool visible) => UiBind.Show(_promptRoot, visible);

        public void SetPrepVisible(bool visible) => UiBind.Show(_prepRoot, visible);

        public void SetAssaultVisible(bool visible) => UiBind.Show(_assaultRoot, visible);

        public void SetHatchAlarmVisible(bool visible) => UiBind.Show(_hatchAlarmRoot, visible);

        public void SetShortcutVisible(bool visible) => UiBind.Show(_shortcutRoot, visible);

        public void SetArrivingVisible(bool visible) => UiBind.Show(_arrivingRoot, visible);

        public void SetEndVisible(bool visible) => _endScreen?.SetVisible(visible);

        public void SetEmergencyVisible(bool visible) => UiBind.Show(_emergencyRoot, visible);

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

        /// <summary>Criterion 14: the three crosshairs are authored from Shift parts and
        /// swapped by activation, not drawn as glyphs.</summary>
        public void SetCrosshair(CrosshairShape shape)
        {
            UiBind.Show(_crosshairCross, shape == CrosshairShape.Cross);
            UiBind.Show(_crosshairDot, shape == CrosshairShape.Dot);
            UiBind.Show(_crosshairBrackets, shape == CrosshairShape.Brackets);
        }

        public void SetPrompt(string text) => UiBind.SetText(_promptText, text);

        public void SetPrepHeading(string text) => UiBind.SetText(_prepHeading, text);

        /// <summary>Prep body line 1..4. Criterion 12 keeps <c>&lt;b&gt;</c> markup, so emphasis
        /// arrives inside <paramref name="text"/> rather than as a style set here.</summary>
        public void SetPrepLine(int index, string text)
        {
            int i = index - 1;
            if (_prepLines == null || i < 0 || i >= _prepLines.Length) return;
            UiBind.SetTextOrHide(_prepLines[i], text);
        }

        public void SetAssaultHeading(string text) => UiBind.SetText(_assaultHeading, text);

        public void SetHatchAlarmStrip(string text) => UiBind.SetText(_hatchAlarmText, text);

        public void SetShortcutLines(IReadOnlyList<string> lines, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed && lines != null && lines.Count > 0)
                _shortcuts.SetLines(lines);
            UiBind.Show(_shortcutRoot, collapsed || (lines != null && lines.Count > 0));
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

        /// <summary>Damage and death overlay. Intensity is the authored CanvasGroup's alpha —
        /// the tint itself belongs to the scene.</summary>
        public void SetVignette(float alpha, bool downOverlay, string downText)
        {
            bool show = alpha > 0.001f || downOverlay;
            UiBind.Show(_vignetteRoot, show);
            if (!show || _vignetteRoot == null) return;

            var group = _vignetteRoot.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = downOverlay ? 1f : Mathf.Clamp01(alpha);
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
