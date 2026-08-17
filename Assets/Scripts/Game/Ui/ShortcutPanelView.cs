using System;
using System.Collections.Generic;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>One pooled keycap row of the shortcut strip: keycap label + description.
    /// The row object is an authored clone of a serialized template (Shift's
    /// <c>Keyboard Shortcut Key</c> keycap + Rajdhani labels through UIManagerText) —
    /// WP-3/WP-4 author the template in their scenes.</summary>
    [Serializable]
    public sealed class ShortcutRowBinding
    {
        public GameObject root;
        public TextMeshProUGUI key;
        public TextMeshProUGUI description;
        /// <summary>Present when the template embeds Shift's keycap button. Its
        /// <see cref="ShortcutButton.keyText"/> / <see cref="ShortcutButton.buttonText"/>
        /// must be updated or <c>OnEnable</c> restores the prefab defaults (ESC/EXIT).</summary>
        public ShortcutButton shortcut;
    }

    /// <summary>LOCAL-OWNER: binds Shift's <c>Shortcut Keys</c> strip. New path: pooled keycap rows fed by
    /// <see cref="ShortcutPanel.FillRows"/>'s structured <see cref="ShortcutRow"/> API via
    /// <see cref="SetRows"/>. Legacy path: <see cref="SetLines"/> writes raw string lines into
    /// whatever labels the authored strip already holds — it dies when WP-3/WP-4 port
    /// ShipHud / HudController to rows. The strip sizes itself from its authored layout group
    /// and fitter — nothing here writes a RectTransform.</summary>
    public sealed class ShortcutPanelView : MonoBehaviour
    {
        const string StripName = "Shortcut Keys";
        const string DescriptionChild = "Description";

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

        /// <summary>Structured row path: pools clones of the serialized row
        /// template, one per <see cref="ShortcutRow"/>. Section headings pool from the section
        /// template when one is authored. Rows beyond the list are deactivated, never destroyed.
        /// Logs one error and no-ops when the template is not wired.</summary>
        public void SetRows(IEnumerable<ShortcutRow> rows)
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
            if (rows == null)
            {
                for (int i = 0; i < _rowPool.Count; i++)
                    UiBind.Show(_rowPool[i].root, false);
                for (int i = 0; i < _sectionPool.Count; i++)
                    UiBind.Show(_sectionPool[i], false);
                return;
            }
            foreach (ShortcutRow row in rows)
            {
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
                slot.root.transform.SetAsLastSibling();
                // Seed ShortcutButton fields before enable so OnEnable cannot restore ESC/EXIT.
                if (slot.shortcut != null)
                {
                    slot.shortcut.keyText = row.Key != null ? row.Key : "";
                    slot.shortcut.buttonText = "";
                }
                UiBind.Show(slot.root, true);
                ApplyRow(slot, row);
            }

            for (int i = used; i < _rowPool.Count; i++)
                UiBind.Show(_rowPool[i].root, false);
            for (int i = sectionsUsed; i < _sectionPool.Count; i++)
                UiBind.Show(_sectionPool[i], false);
        }

        ShortcutRowBinding RowAt(int index)
        {
            while (_rowPool.Count <= index)
                _rowPool.Add(BindRow(Instantiate(_rowTemplate, _rowsRoot)));
            return _rowPool[index];
        }

        /// <summary>Template contract: optional child named Description is the action label;
        /// optional <see cref="ShortcutButton"/> under Keycap owns the key glyph (and must be
        /// driven via its fields). Fallback for unit tests: first TMP is key, second is
        /// description.</summary>
        static ShortcutRowBinding BindRow(GameObject clone)
        {
            var binding = new ShortcutRowBinding { root = clone };
            binding.shortcut = clone.GetComponentInChildren<ShortcutButton>(true);

            Transform desc = clone.transform.Find(DescriptionChild);
            if (desc != null)
                binding.description = desc.GetComponent<TextMeshProUGUI>();

            if (binding.shortcut != null)
            {
                binding.key = binding.shortcut.normalKeyText;
                // Keycap also carries an EXIT-style action label; description lives on the
                // sibling, so keep that keycap action empty and hide its labels.
                HideKeycapActionLabels(binding.shortcut);
            }

            if (binding.key == null || binding.description == null)
            {
                var labels = clone.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (binding.key == null && labels.Length > 0) binding.key = labels[0];
                if (binding.description == null)
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (labels[i] == binding.key) continue;
                        if (binding.shortcut != null && IsUnderKeycapAction(binding.shortcut, labels[i]))
                            continue;
                        binding.description = labels[i];
                        break;
                    }
                }
            }

            return binding;
        }

        static void ApplyRow(ShortcutRowBinding slot, ShortcutRow row)
        {
            string key = row.Key != null ? row.Key : "";
            string description = row.Description != null ? row.Description : "";

            if (slot.shortcut != null)
            {
                slot.shortcut.keyText = key;
                slot.shortcut.buttonText = "";
                if (slot.shortcut.normalKeyText != null)
                    UiBind.SetText(slot.shortcut.normalKeyText, key);
                if (slot.shortcut.highlightedKeyText != null)
                    UiBind.SetText(slot.shortcut.highlightedKeyText, key);
                HideKeycapActionLabels(slot.shortcut);
            }
            else
            {
                UiBind.SetText(slot.key, key);
            }

            UiBind.SetText(slot.description, description);
        }

        static void HideKeycapActionLabels(ShortcutButton shortcut)
        {
            if (shortcut.normalText != null)
            {
                shortcut.normalText.text = "";
                UiBind.Show(shortcut.normalText, false);
            }
            if (shortcut.highlightedText != null)
            {
                shortcut.highlightedText.text = "";
                UiBind.Show(shortcut.highlightedText, false);
            }
        }

        static bool IsUnderKeycapAction(ShortcutButton shortcut, TextMeshProUGUI label)
        {
            return label == shortcut.normalText
                || label == shortcut.highlightedText
                || label == shortcut.normalKeyText
                || label == shortcut.highlightedKeyText;
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
