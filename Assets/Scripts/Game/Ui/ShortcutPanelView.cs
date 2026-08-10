using System;
using System.Collections.Generic;
using System.Linq;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Binds Shift's <c>Shortcut Keys</c> strip to the lines <see cref="ShortcutPanel"/>
    /// builds. The strip sizes itself from its authored layout group and fitter — nothing here
    /// writes a RectTransform (M35 criterion 6).</summary>
    public sealed class ShortcutPanelView : MonoBehaviour
    {
        const string StripName = "Shortcut Keys";

        [SerializeField] GameObject _panel;
        [SerializeField] TextMeshProUGUI _collapsedTip;
        [SerializeField] RectTransform _linesRoot;
        [SerializeField] TextMeshProUGUI[] _linePool;

        bool _resolved;

        /// <summary>Resolves the authored strip; the pool is whatever labels it already holds.</summary>
        public void EnsureBuilt()
        {
            if (!_resolved)
            {
                _resolved = true;
                if (_panel == null)
                {
                    var strip = UiBind.Find(transform, StripName);
                    if (strip != null) _panel = strip.gameObject;
                }
                if (_linesRoot == null && _panel != null)
                    _linesRoot = _panel.transform as RectTransform;
                if ((_linePool == null || _linePool.Length == 0) && _linesRoot != null)
                    _linePool = _linesRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            }
            UiBind.Show(_panel, true);
        }

        public void SetCollapsed(bool collapsed)
        {
            EnsureBuilt();
            UiBind.Show(_collapsedTip, collapsed);
            UiBind.Show(_linesRoot, !collapsed);
        }

        public void SetLines(IEnumerable<string> lines)
        {
            EnsureBuilt();
            if (_linePool == null || _linePool.Length == 0) return;

            string[] arr = lines == null
                ? Array.Empty<string>()
                : lines as string[] ?? lines.ToArray();

            for (int i = 0; i < _linePool.Length; i++)
                UiBind.SetTextOrHide(_linePool[i], i < arr.Length ? arr[i] : null);
        }
    }
}
