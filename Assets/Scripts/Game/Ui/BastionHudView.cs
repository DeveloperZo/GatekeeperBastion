using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Binds the authored Bastion fight HUD: four fixed corner cards, left/right edge
    /// drawers, and the MsgGeneral / MsgPlayer lanes. Controllers push strings and values. The
    /// one RectTransform write allowed here is the MsgPlayer lane's anchored Y, which has to
    /// clear the left drawer's top edge and so cannot be authored (M35 criteria 26, 30, 34).</summary>
    public sealed class BastionHudView : MonoBehaviour
    {
        const float IrrelevantAlpha = 0.4f;

        /// <summary>Canvas units between the left drawer's top edge and the toast's bottom.</summary>
        const float PlayerMessageGap = 24f;

        static readonly Vector3[] Corners = new Vector3[4];

        public event Action ReturnClicked;

        [SerializeField] RectTransform _root;

        [Header("Vignette")]
        [SerializeField] GameObject _vignetteRoot;
        [SerializeField, OptionalBinding] TMP_Text _downLabel;

        [Header("Corner cards")]
        [SerializeField] GameObject _profileRoot;
        [SerializeField] TMP_Text _profileLevel;
        [SerializeField] TMP_Text _profileExp;
        [SerializeField] GameObject _scoreRoot;
        [SerializeField] TMP_Text _scoreSalvage;
        [SerializeField] TMP_Text _scoreMeta;
        [SerializeField] GameObject _vitalsRoot;
        [SerializeField] TMP_Text _playerLabel;
        [SerializeField] Slider _playerBar;
        [SerializeField] GameObject _staminaBlock;
        [SerializeField, OptionalBinding] CanvasGroup _staminaGroup;
        [SerializeField] TMP_Text _staminaLabel;
        [SerializeField] Slider _staminaBar;
        [SerializeField] GameObject _weaponCardRoot;
        [SerializeField] TMP_Text _weaponName;
        [SerializeField] TMP_Text _weaponAmmo;

        // Clip bar. Always on while a gun is equipped: fill is clipRemaining/capacity, and
        // reloading swaps the fill for ReloadProgress under a RELOADING label. The row is
        // reserved space in a fixed-height card, so it dims rather than deactivating.
        [SerializeField] GameObject _reloadBlock;
        [SerializeField, OptionalBinding] CanvasGroup _reloadGroup;
        [SerializeField] TMP_Text _reloadLabel;
        [SerializeField] Slider _reloadBar;

        [Header("Ability (drawer detail host, optional)")]
        [SerializeField, OptionalBinding] GameObject _abilityBlock;
        [SerializeField, OptionalBinding] CanvasGroup _abilityGroup;
        [SerializeField, OptionalBinding] TMP_Text _abilityLabel;
        [SerializeField, OptionalBinding] Slider _abilityBar;

        [Header("Crosshair / Prompt")]
        [SerializeField] GameObject _crosshairRoot;
        [SerializeField] GameObject _crosshairCross;
        [SerializeField] GameObject _crosshairDot;
        [SerializeField] GameObject _crosshairBrackets;
        [SerializeField] GameObject _promptRoot;
        [SerializeField] TMP_Text _promptText;

        [Header("Drawers")]
        [SerializeField] GameObject _leftDrawerRoot;

        /// <summary>The visible card inside the left drawer's band. MsgPlayer clears its top
        /// edge; the band root stretches the whole screen height and cannot serve.</summary>
        [SerializeField, OptionalBinding] RectTransform _leftDrawerPanel;
        [SerializeField] GameObject _rightDrawerRoot;
        [SerializeField, OptionalBinding] GameObject _prepRoot;
        [SerializeField, OptionalBinding] TMP_Text _prepHeading;
        [SerializeField, OptionalBinding] TMP_Text[] _prepLines = new TMP_Text[4];
        [SerializeField, OptionalBinding] GameObject _assaultRoot;
        [SerializeField, OptionalBinding] TMP_Text _assaultHeading;
        [SerializeField, OptionalBinding] GameObject _shortcutRoot;
        [SerializeField, OptionalBinding] ShortcutPanelView _shortcuts;
        [SerializeField, OptionalBinding] TMP_Text _drawerHatchLabel;
        [SerializeField, OptionalBinding] Slider _drawerHatchBar;
        [SerializeField, OptionalBinding] TMP_Text _drawerWeaponDetail;

        [Header("Message lanes")]
        [SerializeField] GameObject _msgGeneralRoot;
        [SerializeField] TMP_Text _msgGeneralText;
        [SerializeField] GameObject _msgPlayerRoot;
        [SerializeField] TMP_Text _msgPlayerText;

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
        bool _leftDrawerKnown;
        bool _leftDrawerOpen;
        bool _rightDrawerKnown;
        bool _rightDrawerOpen;
        bool _prepMode;

        public void EnsureBuilt()
        {
            if (!_missingLogged)
            {
                _missingLogged = true;
                LogMissing(nameof(_root), _root);
                LogMissing(nameof(_endScreen), _endScreen);
                LogMissing(nameof(_vitalsRoot), _vitalsRoot);
                LogMissing(nameof(_weaponCardRoot), _weaponCardRoot);
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

        public void ShowArriving()
        {
            HideChrome();
            SetGeneralMessage("arriving at the bastion...");
        }

        /// <summary>Enters the fight look: the four cards and the crosshair. Drawer state is
        /// not decided here — HudController pushes Controls' state the same frame, and forcing
        /// a value would flip the drawer for one frame against the player's toggle.</summary>
        public void ShowFight(bool prep)
        {
            HideChrome();
            // Seed before the first PushFightValues so the card never flashes WEAPON/0/0.
            SetWeapon("—", "—");
            SetReloadLabel("CLIP");
            SetReloadBar(0f);
            SetReloadBlockVisible(true);
            SetDrawerWeaponDetail(null);
            SetPrepHeading("PREP");
            ShowCornerCards(true);
            UiBind.Show(_crosshairRoot, true);
            _prepMode = prep;
            _leftDrawerKnown = false;
            _rightDrawerKnown = false;
        }

        public void ShowEnd()
        {
            HideChrome();
            _endScreen?.SetVisible(true);
            SetLeftDrawerOpen(true);
        }

        public void ShowEmergency()
        {
            HideChrome();
            UiBind.Show(_emergencyRoot, true);
            SetLeftDrawerOpen(true);
        }

        public void HideChrome()
        {
            UiBind.Show(_vignetteRoot, false);
            ShowCornerCards(false);
            UiBind.Show(_crosshairRoot, false);
            UiBind.Show(_promptRoot, false);
            UiBind.Show(_prepRoot, false);
            UiBind.Show(_assaultRoot, false);
            UiBind.Show(_shortcutRoot, false);
            UiBind.Show(_leftDrawerRoot, false);
            UiBind.Show(_rightDrawerRoot, false);
            SetGeneralMessage(null);
            SetPlayerMessage(null);
            _endScreen?.SetVisible(false);
            UiBind.Show(_emergencyRoot, false);
            _leftDrawerKnown = false;
            _rightDrawerKnown = false;
        }

        void ShowCornerCards(bool visible)
        {
            UiBind.Show(_profileRoot, visible);
            UiBind.Show(_scoreRoot, visible);
            UiBind.Show(_vitalsRoot, visible);
            UiBind.Show(_weaponCardRoot, visible);
        }

        public void SetCrosshairVisible(bool visible) => UiBind.Show(_crosshairRoot, visible);
        public void SetEndVisible(bool visible) => _endScreen?.SetVisible(visible);
        public void SetEmergencyVisible(bool visible) => UiBind.Show(_emergencyRoot, visible);

        /// <summary>Closed is deactivated, which is what "fully off-screen" buys: no partial
        /// peek past the 24-unit inset, and no RectTransform write from a view.</summary>
        public void SetLeftDrawerOpen(bool open)
        {
            if (_leftDrawerKnown && _leftDrawerOpen == open) return;
            _leftDrawerKnown = true;
            _leftDrawerOpen = open;
            UiBind.Show(_leftDrawerRoot, open);
            UiBind.Show(_shortcutRoot, open);
            UpdatePlayerMessageAnchor();
        }

        public void SetRightDrawerOpen(bool open)
        {
            if (_rightDrawerKnown && _rightDrawerOpen == open)
            {
                SyncMissionPhaseHosts(open);
                return;
            }
            _rightDrawerKnown = true;
            _rightDrawerOpen = open;
            UiBind.Show(_rightDrawerRoot, open);
            SyncMissionPhaseHosts(open);
        }

        /// <summary>The mission drawer shows prep detail or the assault strip, never both, and
        /// nothing when it is closed.</summary>
        void SyncMissionPhaseHosts(bool drawerOpen)
        {
            UiBind.Show(_prepRoot, drawerOpen && _prepMode);
            UiBind.Show(_assaultRoot, drawerOpen && !_prepMode);
        }

        public void SetProfile(string levelLine, string expLine)
        {
            UiBind.SetText(_profileLevel, levelLine);
            UiBind.SetText(_profileExp, expLine ?? "");
        }

        public void SetScore(string salvageLine, string metaLine)
        {
            UiBind.SetText(_scoreSalvage, salvageLine);
            UiBind.SetText(_scoreMeta, metaLine);
        }

        public void SetWeapon(string name, string ammoLine)
        {
            UiBind.SetText(_weaponName, name ?? "");
            UiBind.SetText(_weaponAmmo, ammoLine ?? "");
        }

        public void SetDrawerHatch(string label, float hp, float maxHp)
        {
            UiBind.SetText(_drawerHatchLabel, label);
            UiBind.SetBar(_drawerHatchBar, hp, maxHp);
        }

        public void SetDrawerWeaponDetail(string text) =>
            UiBind.SetTextOrHide(_drawerWeaponDetail, text);

        public void SetGeneralMessage(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            UiBind.Show(_msgGeneralRoot, show);
            if (show) UiBind.SetText(_msgGeneralText, text);
        }

        public void SetPlayerMessage(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            UiBind.Show(_msgPlayerRoot, show);
            if (!show) return;
            UiBind.SetText(_msgPlayerText, text);
            UpdatePlayerMessageAnchor();
        }

        /// <summary>Mid-left lane: centred vertically while the left drawer is closed, and
        /// 24 units clear of the drawer's top edge while it is open. Derived from the live
        /// drawer rect rather than a constant, so a drawer height change cannot overlap it.</summary>
        void UpdatePlayerMessageAnchor()
        {
            if (_msgPlayerRoot == null) return;
            var lane = _msgPlayerRoot.transform as RectTransform;
            if (lane == null) return;

            float y = 0f;
            if (_leftDrawerOpen)
            {
                RectTransform drawer = _leftDrawerPanel != null
                    ? _leftDrawerPanel
                    : (_leftDrawerRoot != null ? _leftDrawerRoot.transform as RectTransform : null);
                var band = lane.parent as RectTransform;
                if (drawer != null && band != null)
                {
                    // Layout may still be pending the frame the drawer activates; ForceUpdate
                    // so GetWorldCorners is not the pre-layout 100×100 default.
                    Canvas.ForceUpdateCanvases();
                    drawer.GetWorldCorners(Corners);
                    float top = band.InverseTransformPoint(Corners[1]).y;
                    y = top + PlayerMessageGap + lane.rect.height * 0.5f;
                }
            }

            Vector2 pos = lane.anchoredPosition;
            if (!Mathf.Approximately(pos.y, y)) lane.anchoredPosition = new Vector2(pos.x, y);
        }

        void LateUpdate()
        {
            // Recompute while the toast is up: SetLeftDrawerOpen early-returns on unchanged
            // state, so a first open that raced layout would otherwise stick forever.
            if (_msgPlayerRoot != null && _msgPlayerRoot.activeSelf && _leftDrawerOpen)
                UpdatePlayerMessageAnchor();
        }

        public void SetPromptVisible(bool visible) => UiBind.Show(_promptRoot, visible);

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

        public void SetCrosshair(CrosshairShape shape)
        {
            if (_crosshair == shape) return;
            _crosshair = shape;
            UiBind.Show(_crosshairCross, shape == CrosshairShape.Cross);
            UiBind.Show(_crosshairDot, shape == CrosshairShape.Dot);
            UiBind.Show(_crosshairBrackets, shape == CrosshairShape.Brackets);
        }

        public void SetPrompt(string text) => UiBind.SetText(_promptText, text);

        public void SetPrepHeading(string text)
        {
            UiBind.SetText(_prepHeading, text ?? "");
            if (_prepHeading != null) UiBind.Show(_prepHeading.gameObject, !string.IsNullOrEmpty(text));
        }

        public void SetPrepLine(int index, string text)
        {
            int i = index - 1;
            if (_prepLines == null || i < 0 || i >= _prepLines.Length) return;
            UiBind.SetTextOrHide(_prepLines[i], text);
        }

        public void SetAssaultHeading(string text) => UiBind.SetText(_assaultHeading, text);

        public void SetShortcutRows(IReadOnlyList<ShortcutRow> rows, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed && rows != null) _shortcuts.SetRows(rows);
        }

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
