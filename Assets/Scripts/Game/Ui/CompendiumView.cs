using System;
using System.Collections.Generic;
using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: pause Compendium reader. Left: one scroll of volume
    /// headers with every entry under them (organized + exhaustive). Right: Game body.</summary>
    public sealed class CompendiumView : MonoBehaviour, IPointerClickHandler
    {
        const string Tag = "Compendium";

        static readonly Color VolumeHeaderColor = new Color(0.55f, 0.62f, 0.72f, 1f);
        static readonly Color EntryNormalColor = new Color(0.88f, 0.90f, 0.94f, 1f);
        static readonly Color EntrySelectedColor = new Color(0.95f, 0.78f, 0.35f, 1f);
        static readonly Color EntrySelectedBg = new Color(0.28f, 0.32f, 0.40f, 1f);
        static readonly Color EntryNormalBg = new Color(0.15f, 0.18f, 0.22f, 0.0f);

        [SerializeField] GameObject _root;
        [SerializeField, Tooltip("Content root under the left nav ScrollRect.")]
        RectTransform _navContent;
        [SerializeField] GameObject _volumeHeaderTemplate;
        [SerializeField] GameObject _entryButtonTemplate;
        [SerializeField] ScrollRect _navScroll;
        [SerializeField] TextMeshProUGUI _titleLabel;
        [SerializeField] TextMeshProUGUI _bodyLabel;
        [SerializeField] ScrollRect _bodyScroll;
        [SerializeField] Button _closeButton;

        [SerializeField, OptionalBinding, Tooltip("HUD / pause chrome hidden while the reader is open.")]
        GameObject[] _hideWhileOpen;

        // Legacy wires from the first authoring pass — used if _navContent is unset.
        [SerializeField, OptionalBinding] RectTransform _volumeListRoot;
        [SerializeField, OptionalBinding] GameObject _volumeButtonTemplate;
        [SerializeField, OptionalBinding] RectTransform _entryListRoot;

        readonly List<NavEntry> _navEntries = new List<NavEntry>(32);
        readonly List<bool> _hideWasActive = new List<bool>(8);
        string _openStem;
        bool _built;
        bool _hidingHud;

        struct NavEntry
        {
            public string Stem;
            public Button Button;
            public TextMeshProUGUI Label;
            public Image Background;
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public event Action Closed;

        void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
            HideTemplate(_volumeHeaderTemplate);
            HideTemplate(_entryButtonTemplate);
            HideTemplate(_volumeButtonTemplate);
            if (_root != null)
                _root.SetActive(false);
        }

        public void Open(string preferredStem = null)
        {
            if (_root == null)
            {
                Log.Error(Tag, "Compendium root not authored.");
                return;
            }
            EnsureBuilt();
            SetHudHidden(true);
            _root.SetActive(true);
            if (!string.IsNullOrEmpty(preferredStem) && TryOpenStem(preferredStem))
                return;
            if (string.IsNullOrEmpty(_openStem) && _navEntries.Count > 0)
                TryOpenStem(_navEntries[0].Stem);
            else
                RefreshBody();
        }

        public void Close()
        {
            if (_root == null || !_root.activeSelf) return;
            _root.SetActive(false);
            SetHudHidden(false);
            if (Closed != null) Closed.Invoke();
        }

        void OnDisable()
        {
            // Domain reload / canvas disable must not leave HUD permanently off.
            SetHudHidden(false);
        }

        void SetHudHidden(bool hide)
        {
            if (_hideWhileOpen == null || _hideWhileOpen.Length == 0) return;

            if (hide)
            {
                if (_hidingHud) return;
                _hideWasActive.Clear();
                for (int i = 0; i < _hideWhileOpen.Length; i++)
                {
                    var go = _hideWhileOpen[i];
                    bool was = go != null && go.activeSelf;
                    _hideWasActive.Add(was);
                    if (go != null && was) go.SetActive(false);
                }
                _hidingHud = true;
                return;
            }

            if (!_hidingHud) return;
            for (int i = 0; i < _hideWhileOpen.Length && i < _hideWasActive.Count; i++)
            {
                var go = _hideWhileOpen[i];
                if (go != null && _hideWasActive[i]) go.SetActive(true);
            }
            _hideWasActive.Clear();
            _hidingHud = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_bodyLabel == null || eventData == null) return;
            int link = TMP_TextUtilities.FindIntersectingLink(
                _bodyLabel, eventData.position, eventData.pressEventCamera);
            if (link < 0) return;
            var info = _bodyLabel.textInfo.linkInfo[link];
            string stem = info.GetLinkID();
            if (!string.IsNullOrEmpty(stem))
                TryOpenStem(stem);
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            RectTransform content = ResolveNavContent();
            GameObject headerTemplate = ResolveHeaderTemplate();
            GameObject entryTemplate = ResolveEntryTemplate();
            if (content == null || headerTemplate == null || entryTemplate == null)
            {
                Log.Error(Tag, "Nav content or templates not authored.");
                return;
            }

            // Clear any leftover authored placeholders under the content root.
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (child == headerTemplate || child == entryTemplate) continue;
                if (child == _volumeButtonTemplate || child == _entryButtonTemplate) continue;
                Destroy(child);
            }

            _navEntries.Clear();
            for (int v = 0; v < CompendiumCatalog.Volumes.Length; v++)
            {
                var volume = CompendiumCatalog.Volumes[v];
                SpawnHeader(content, headerTemplate, volume.Title);

                for (int i = 0; i < volume.EntryStems.Length; i++)
                {
                    string stem = volume.EntryStems[i];
                    string title = CompendiumCatalog.DisplayTitle(stem);
                    SpawnEntry(content, entryTemplate, stem, title);
                }
            }

            if (_navScroll != null)
                _navScroll.verticalNormalizedPosition = 1f;
        }

        void SpawnHeader(RectTransform parent, GameObject template, string title)
        {
            var go = Instantiate(template, parent);
            go.name = "Volume_" + title.Replace(' ', '_');
            go.SetActive(true);

            var btn = go.GetComponent<Button>();
            if (btn != null) btn.interactable = false;

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = title.ToUpperInvariant();
                label.color = VolumeHeaderColor;
                label.fontStyle = FontStyles.Bold;
            }
        }

        void SpawnEntry(RectTransform parent, GameObject template, string stem, string title)
        {
            var go = Instantiate(template, parent);
            go.name = "Entry_" + stem;
            go.SetActive(true);

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "  " + title;
                label.color = EntryNormalColor;
            }

            var btn = go.GetComponent<Button>();
            var bg = go.GetComponent<Image>();
            if (btn != null)
            {
                string captured = stem;
                btn.onClick.AddListener(() => TryOpenStem(captured));
            }

            _navEntries.Add(new NavEntry
            {
                Stem = stem,
                Button = btn,
                Label = label,
                Background = bg
            });
        }

        bool TryOpenStem(string stem)
        {
            if (!CompendiumCatalog.TryLoadEntry(stem, out var entry))
            {
                Log.Warn(Tag, "Missing Compendium page: " + stem);
                return false;
            }

            _openStem = stem;
            if (_titleLabel != null) _titleLabel.text = entry.Title;
            if (_bodyLabel != null)
                _bodyLabel.text = CompendiumCatalog.ToTmpRichText(entry.BodyMarkdown);
            if (_bodyScroll != null)
                _bodyScroll.verticalNormalizedPosition = 1f;

            PaintSelection(stem);
            return true;
        }

        void PaintSelection(string stem)
        {
            for (int i = 0; i < _navEntries.Count; i++)
            {
                bool selected = string.Equals(_navEntries[i].Stem, stem, StringComparison.OrdinalIgnoreCase);
                if (_navEntries[i].Label != null)
                    _navEntries[i].Label.color = selected ? EntrySelectedColor : EntryNormalColor;
                if (_navEntries[i].Background != null)
                    _navEntries[i].Background.color = selected ? EntrySelectedBg : EntryNormalBg;
            }
        }

        void RefreshBody()
        {
            if (!string.IsNullOrEmpty(_openStem))
                TryOpenStem(_openStem);
        }

        RectTransform ResolveNavContent()
        {
            if (_navContent != null) return _navContent;
            if (_volumeListRoot != null) return _volumeListRoot;
            return null;
        }

        GameObject ResolveHeaderTemplate()
        {
            if (_volumeHeaderTemplate != null) return _volumeHeaderTemplate;
            return _volumeButtonTemplate;
        }

        GameObject ResolveEntryTemplate()
        {
            return _entryButtonTemplate;
        }

        static void HideTemplate(GameObject template)
        {
            if (template != null) template.SetActive(false);
        }
    }
}
