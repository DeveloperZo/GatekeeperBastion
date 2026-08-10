using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Binds the Ship hub's HUD strip on its own small canvas. Controllers push
    /// strings; nothing here sets a colour, a font size or a RectTransform (M35 criteria 25, 38).</summary>
    public sealed class ShipHudView : MonoBehaviour
    {
        [SerializeField] RectTransform _root;
        [SerializeField, OptionalBinding] GameObject _headerPanel;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _salvageLine;
        [SerializeField] TextMeshProUGUI _objective;
        [SerializeField, OptionalBinding] TextMeshProUGUI _directory;
        [SerializeField] GameObject _crosshair;
        [SerializeField] TextMeshProUGUI _prompt;
        [SerializeField, OptionalBinding] ShortcutPanelView _shortcuts;

        bool _promptIsWarn;

        public ShortcutPanelView Shortcuts => _shortcuts;

        void Awake() => UiCanvasGuard.RepairIfBroken(gameObject);

        void OnEnable() => UiCanvasGuard.RepairIfBroken(gameObject);

        public void SetHeader(string title, string salvageLoadoutLine, string objective, string directoryLine)
        {
            UiBind.SetText(_title, title);
            UiBind.SetText(_salvageLine, salvageLoadoutLine);
            UiBind.SetText(_objective, objective);
            UiBind.SetText(_directory, directoryLine);
        }

        public void SetPrompt(string text)
        {
            _promptIsWarn = false;
            UiBind.SetTextOrHide(_prompt, text);
        }

        public void SetCursorFreeWarn(bool show)
        {
            if (!show)
            {
                if (_promptIsWarn) UiBind.SetTextOrHide(_prompt, null);
                _promptIsWarn = false;
                return;
            }
            _promptIsWarn = true;
            UiBind.SetTextOrHide(_prompt, "<b>CURSOR FREE</b> - left-click to take back mouse look");
        }

        public void SetCrosshairVisible(bool visible) => UiBind.Show(_crosshair, visible);

        /// <summary>State-change only — callers must not toggle every frame (M35 criterion 26).</summary>
        public void SetHudRelevant(bool relevant)
        {
            UiBind.Show(_root, relevant);
            UiBind.Show(_headerPanel, relevant);
        }

        public void SetVisible(bool visible) => SetHudRelevant(visible);

        public void SetShortcutRows(IReadOnlyList<ShortcutRow> rows, bool collapsed)
        {
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed) _shortcuts.SetRows(rows);
        }

        public void SetShortcutLines(IList<string> lines, bool collapsed)
        {
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed) _shortcuts.SetLines(lines);
        }
    }
}
