using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>One pooled keycap row of the shortcut strip: keycap label + description.
    /// The row object is an authored clone of a serialized template (Shift's
    /// <c>Keyboard Shortcut Key</c> keycap + Rajdhani labels through UIManagerText) —
    /// M35 criterion 35. WP-3/WP-4 author the template in their scenes.</summary>
    [Serializable]
    public sealed class ShortcutRowBinding
    {
        public GameObject root;
        public TextMeshProUGUI key;
        public TextMeshProUGUI description;
    }

    /// <summary>Binds Shift's <c>Shortcut Keys</c> strip. New path: pooled keycap rows fed by
    /// <see cref="ShortcutPanel.FillRows"/>'s structured <see cref="ShortcutRow"/> API via
    /// <see cref="SetRows"/>. Legacy path: <see cref="SetLines"/> writes raw string lines into
    /// whatever labels the authored strip already holds — it dies when WP-3/WP-4 port
    /// ShipHud / HudController to rows. The strip sizes itself from its authored layout group
    /// and fitter — nothing here writes a RectTransform.</summary>
    public sealed class ShortcutPanelView : MonoBehaviour
    {
        const string StripName = "Shortcut Keys";

        [SerializeField] GameObject _panel;
        [SerializeField, OptionalBinding] TextMeshProUGUI _collapsedTip;
        [SerializeField] RectTransform _linesRoot;
        [SerializeField, OptionalBinding] TextMeshProUGUI[] _linePool;

        [Header("Keycap rows (criterion 35 — template authored by WP-3/WP-4)")]
        [SerializeField, OptionalBinding] RectTransform _rowsRoot;
        [SerializeField, OptionalBinding] GameObject _rowTemplate;
        [SerializeField, OptionalBinding] GameObject _sectionTemplate;

        readonly List<ShortcutRowBinding> _rowPool = new List<ShortcutRowBinding>(16);
        readonly List<TextMeshProUGUI> _sectionPool = new List<TextMeshProUGUI>(8);
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
            UiBind.Show(_rowsRoot, !collapsed);
        }

        /// <summary>Structured row path (M35 criterion 35): pools clones of the serialized row
        /// template, one per <see cref="ShortcutRow"/>. Section headings pool from the section
        /// template when one is authored. Rows beyond the list are deactivated, never destroyed.
        /// Logs one error and no-ops when the template is not wired.</summary>
        public void SetRows(IReadOnlyList<ShortcutRow> rows)
        {
            EnsureBuilt();
            if (_rowTemplate == null || _rowsRoot == null)
            {
                Debug.LogError(
                    $"[ShortcutPanelView] '{name}' has no serialized _rowTemplate/_rowsRoot — " +
                    "author the keycap row template (criterion 35) before calling SetRows.", this);
                return;
            }
            if (_rowTemplate.activeSelf) _rowTemplate.SetActive(false);
            if (_sectionTemplate != null && _sectionTemplate.activeSelf) _sectionTemplate.SetActive(false);

            int used = 0;
            int sectionsUsed = 0;
            string section = null;
            for (int i = 0; i < (rows?.Count ?? 0); i++)
            {
                ShortcutRow row = rows[i];
                if (_sectionTemplate != null && row.Section != section)
                {
                    section = row.Section;
                    TextMeshProUGUI heading = SectionAt(sectionsUsed++);
                    if (heading != null)
                    {
                        UiBind.SetText(heading, section);
                        // Keep pooled headings in visual order as sections change between fills.
                        heading.transform.SetAsLastSibling();
                        UiBind.Show(heading, true);
                    }
                }

                ShortcutRowBinding slot = RowAt(used++);
                UiBind.SetText(slot.key, row.Key);
                UiBind.SetText(slot.description, row.Description);
                slot.root.transform.SetAsLastSibling();
                UiBind.Show(slot.root, true);
            }

            for (int i = used; i < _rowPool.Count; i++)
                UiBind.Show(_rowPool[i].root, false);
            for (int i = sectionsUsed; i < _sectionPool.Count; i++)
                UiBind.Show(_sectionPool[i], false);
        }

        ShortcutRowBinding RowAt(int index)
        {
            while (_rowPool.Count <= index)
            {
                GameObject clone = Instantiate(_rowTemplate, _rowsRoot);
                var binding = new ShortcutRowBinding { root = clone };
                // Template contract: first TMP label is the keycap, second the description.
                var labels = clone.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (labels.Length > 0) binding.key = labels[0];
                if (labels.Length > 1) binding.description = labels[1];
                _rowPool.Add(binding);
            }
            return _rowPool[index];
        }

        TextMeshProUGUI SectionAt(int index)
        {
            while (_sectionPool.Count <= index)
            {
                GameObject clone = Instantiate(_sectionTemplate, _rowsRoot);
                _sectionPool.Add(clone.GetComponentInChildren<TextMeshProUGUI>(true));
            }
            return _sectionPool[index];
        }

        /// <summary>Legacy raw-line path — consumed by ShipHud / HudController until WP-3/WP-4
        /// port them to <see cref="SetRows"/>.</summary>
        public void SetLines(IEnumerable<string> lines)
        {
            EnsureBuilt();
            if (_linePool == null || _linePool.Length == 0) return;

            int count = 0;
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (count >= _linePool.Length) break;
                    UiBind.SetTextOrHide(_linePool[count], line);
                    count++;
                }
            }
            for (int i = count; i < _linePool.Length; i++)
                UiBind.SetTextOrHide(_linePool[i], null);
        }
    }
}
