using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>uGUI shell for the Ship hub HUD. Controllers push strings; no sim reads here.</summary>
    public sealed class ShipHudView : MonoBehaviour
    {
        [SerializeField] RectTransform _root;
        [SerializeField] GameObject _headerPanel;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _salvageLine;
        [SerializeField] TextMeshProUGUI _objective;
        [SerializeField] TextMeshProUGUI _directory;
        [SerializeField] TextMeshProUGUI _crosshair;
        [SerializeField] TextMeshProUGUI _prompt;
        [SerializeField] ShortcutPanelView _shortcuts;

        public ShortcutPanelView Shortcuts => _shortcuts;

        /// <summary>authored prefab required — activates serialized children only.</summary>
        public void EnsureBuilt()
        {
            if (_root != null) _root.gameObject.SetActive(true);
            if (_headerPanel != null) _headerPanel.SetActive(true);
            _shortcuts?.EnsureBuilt();
        }

        public void SetHeader(string title, string salvageLoadoutLine, string objective, string directoryLine)
        {
            EnsureBuilt();
            if (_title != null) _title.text = title;
            if (_salvageLine != null) _salvageLine.text = salvageLoadoutLine;
            if (_objective != null) _objective.text = objective;
            if (_directory != null) _directory.text = directoryLine;
        }

        public void SetPrompt(string text)
        {
            EnsureBuilt();
            if (_prompt == null) return;
            _prompt.text = text ?? "";
            Theme.Apply(_prompt, Theme.Head, Theme.Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            _prompt.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void SetCursorFreeWarn(bool show)
        {
            EnsureBuilt();
            if (!show || _prompt == null) return;
            _prompt.text = "CURSOR FREE — left-click to take back mouse look";
            Theme.Apply(_prompt, Theme.Head, Theme.Warn, FontStyles.Bold, TextAlignmentOptions.Center);
            _prompt.gameObject.SetActive(true);
        }

        public void SetCrosshairVisible(bool visible)
        {
            EnsureBuilt();
            if (_crosshair != null) _crosshair.gameObject.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        public void SetShortcutLines(IList<string> lines, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed) _shortcuts.SetLines(lines);
        }

        public void LayoutPrompt(float shortcutHeight)
        {
            EnsureBuilt();
            if (_prompt == null) return;
            var promptRt = _prompt.rectTransform;
            // Shift 25-based: 35 above the shortcut band floor.
            promptRt.anchoredPosition = new Vector2(0f, Mathf.Max(shortcutHeight, 50f) + 35f);
        }
    }
}
