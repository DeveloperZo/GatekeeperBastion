using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Bottom-left shortcut list for Ship (and shared layout constants with Bastion).</summary>
    public sealed class ShortcutPanelView : MonoBehaviour
    {
        const float LineHeight = 25f;

        [SerializeField] GameObject _panel;
        [SerializeField] TextMeshProUGUI _collapsedTip;
        [SerializeField] RectTransform _linesRoot;
        [SerializeField] TextMeshProUGUI[] _linePool;

        /// <summary>authored prefab required — activates serialized children only.</summary>
        public void EnsureBuilt()
        {
            if (_panel != null) _panel.SetActive(true);
        }

        public void SetCollapsed(bool collapsed)
        {
            EnsureBuilt();
            if (_collapsedTip != null) _collapsedTip.gameObject.SetActive(collapsed);
            if (_linesRoot != null) _linesRoot.gameObject.SetActive(!collapsed);
        }

        public void SetLines(IEnumerable<string> lines)
        {
            EnsureBuilt();
            if (_linePool == null || _linePool.Length == 0) return;

            string[] arr = lines == null
                ? Array.Empty<string>()
                : lines as string[] ?? lines.ToArray();
            int count = arr.Length;

            for (int i = 0; i < _linePool.Length; i++)
            {
                if (_linePool[i] == null) continue;
                bool show = i < count;
                _linePool[i].gameObject.SetActive(show);
                if (show) _linePool[i].text = arr[i];
            }

            float height = 25f + Mathf.Min(count, _linePool.Length) * LineHeight + 25f;
            var rt = GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(height, 50f));
        }
    }
}
