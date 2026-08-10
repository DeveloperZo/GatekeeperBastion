using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Binds the Ship hub's HUD strip inside the authored Shift Canvas. Controllers push
    /// strings; nothing here sets a colour, a font size or a RectTransform (M35 criteria 6, 17).</summary>
    public sealed class ShipHudView : MonoBehaviour
    {
        const string HudRootName = "Ship HUD";

        [SerializeField] RectTransform _root;
        [SerializeField] GameObject _headerPanel;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _salvageLine;
        [SerializeField] TextMeshProUGUI _objective;
        [SerializeField] TextMeshProUGUI _directory;
        [SerializeField] GameObject _crosshair;
        [SerializeField] TextMeshProUGUI _prompt;
        [SerializeField] ShortcutPanelView _shortcuts;

        bool _resolved;

        public ShortcutPanelView Shortcuts => _shortcuts;

        void Awake() => UiCanvasGuard.RepairIfBroken(gameObject);

        void OnEnable() => UiCanvasGuard.RepairIfBroken(gameObject);

        /// <summary>Resolves whatever the scene author did not wire and shows the strip.</summary>
        public void EnsureBuilt()
        {
            if (!_resolved)
            {
                _resolved = true;
                if (_root == null)
                    _root = (UiBind.Find(transform, HudRootName) ?? transform) as RectTransform;
                if (_shortcuts == null) _shortcuts = GetComponentInChildren<ShortcutPanelView>(true);
            }
            UiBind.Show(_root, true);
            UiBind.Show(_headerPanel, true);
            _shortcuts?.EnsureBuilt();
        }

        public void SetHeader(string title, string salvageLoadoutLine, string objective, string directoryLine)
        {
            EnsureBuilt();
            UiBind.SetText(_title, title);
            UiBind.SetText(_salvageLine, salvageLoadoutLine);
            UiBind.SetText(_objective, objective);
            UiBind.SetText(_directory, directoryLine);
        }

        public void SetPrompt(string text)
        {
            EnsureBuilt();
            UiBind.SetTextOrHide(_prompt, text);
        }

        /// <summary>Cursor-free warning reuses the prompt slot; <c>&lt;b&gt;</c> is the only
        /// emphasis available to us, colour being Shift UI Manager's.</summary>
        public void SetCursorFreeWarn(bool show)
        {
            EnsureBuilt();
            if (!show) return;
            UiBind.SetTextOrHide(_prompt, "<b>CURSOR FREE</b> — left-click to take back mouse look");
        }

        public void SetCrosshairVisible(bool visible)
        {
            EnsureBuilt();
            UiBind.Show(_crosshair, visible);
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            UiBind.Show(_root, visible);
        }

        public void SetShortcutLines(IList<string> lines, bool collapsed)
        {
            EnsureBuilt();
            if (_shortcuts == null) return;
            _shortcuts.SetCollapsed(collapsed);
            if (!collapsed) _shortcuts.SetLines(lines);
        }
    }
}
