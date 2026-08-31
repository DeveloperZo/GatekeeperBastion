using System;
using System.Collections.Generic;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: pause Compendium reader. Left: one scroll of volume
    /// headers with every entry under them (organized + exhaustive). Right: Game body;
    /// First Time Out also carries its walkthrough.</summary>
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
        readonly List<NavHeader> _navHeaders = new List<NavHeader>(8);
        readonly List<bool> _hideWasActive = new List<bool>(8);
        bool[] _volumeExpanded;
        string _openStem;
        bool _built;
        bool _builtShowComingSoon;
        bool _hidingHud;
        bool _initialized;
        bool _opening;

        struct NavEntry
        {
            public string Stem;
            public int Volume;
            public Button Button;
            public TextMeshProUGUI Label;
            public Image Background;
        }

        struct NavHeader
        {
            public int Volume;
            public string Title;
            public TextMeshProUGUI Label;
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public event Action Closed;

        void Awake()
        {
            // The view sits INSIDE the authored panel, so a first Open() on a closed panel
            // activates this object and runs Awake mid-Open — the start-closed default must
            // not undo the very Open() that woke it.
            bool first = !_initialized;
            Initialize();
            if (first && !_opening && _root != null)
                _root.SetActive(false);
        }

        void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
            HideTemplate(_volumeHeaderTemplate);
            HideTemplate(_entryButtonTemplate);
            HideTemplate(_volumeButtonTemplate);
        }

        public void Open(string preferredStem = null)
        {
            if (_root == null)
            {
                Log.Error(Tag, "Compendium root not authored.");
                return;
            }
            Initialize();
            RebuildIfFilterChanged();
            EnsureBuilt();
            SetHudHidden(true);
            _opening = true;
            _root.SetActive(true);
            _opening = false;
            if (!string.IsNullOrEmpty(preferredStem) && TryOpenStem(preferredStem))
                return;
            if (string.IsNullOrEmpty(_openStem))
            {
                if (TryOpenStem(CompendiumCatalog.DefaultOpenStem))
                    return;
                if (_navEntries.Count > 0)
                    TryOpenStem(_navEntries[0].Stem);
                return;
            }
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

        /// <summary>The Coming Soon filter is an options toggle; a nav built under the old
        /// value is torn down so the next build reads the current one.</summary>
        void RebuildIfFilterChanged()
        {
            if (!_built) return;
            if (_builtShowComingSoon == Settings.GetBool(SettingId.ShowComingSoonEntries))
                return;

            RectTransform content = ResolveNavContent();
            GameObject headerTemplate = ResolveHeaderTemplate();
            GameObject entryTemplate = ResolveEntryTemplate();
            if (content != null)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    var child = content.GetChild(i).gameObject;
                    if (child == headerTemplate || child == entryTemplate) continue;
                    if (child == _volumeButtonTemplate) continue;
                    Destroy(child);
                }
            }
            _navEntries.Clear();
            _navHeaders.Clear();
            _built = false;
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            _builtShowComingSoon = Settings.GetBool(SettingId.ShowComingSoonEntries);

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
            _navHeaders.Clear();
            _volumeExpanded = new bool[CompendiumCatalog.Volumes.Length];
            for (int v = 0; v < CompendiumCatalog.Volumes.Length; v++)
            {
                var volume = CompendiumCatalog.Volumes[v];
                SpawnHeader(content, headerTemplate, volume.Title, v);

                for (int i = 0; i < volume.EntryStems.Length; i++)
                {
                    string stem = volume.EntryStems[i];
                    // Volumes mirror Docs/Play/README.md and list every page; whether a page
                    // SHIPS is its own in_build flag, read here so a pulled page vanishes from
                    // the nav instead of opening blank.
                    if (!CompendiumCatalog.IsInBuild(stem)) continue;
                    if (!_builtShowComingSoon && CompendiumCatalog.IsComingSoon(stem)) continue;
                    string title = CompendiumCatalog.DisplayTitle(stem);
                    SpawnEntry(content, entryTemplate, stem, title, v);
                }
            }

            if (_navScroll != null)
                _navScroll.verticalNormalizedPosition = 1f;
        }

        void SpawnHeader(RectTransform parent, GameObject template, string title, int volume)
        {
            var go = Instantiate(template, parent);
            go.name = "Volume_" + title.Replace(' ', '_');
            go.SetActive(true);

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = VolumeHeaderColor;
                label.fontStyle = FontStyles.Bold;
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
                int captured = volume;
                btn.onClick.AddListener(() => ToggleVolume(captured));
            }

            _navHeaders.Add(new NavHeader { Volume = volume, Title = title, Label = label });
            PaintHeader(_navHeaders.Count - 1);
        }

        void SpawnEntry(RectTransform parent, GameObject template, string stem, string title,
            int volume)
        {
            var go = Instantiate(template, parent);
            go.name = "Entry_" + stem;
            go.SetActive(_volumeExpanded[volume]);

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
                Volume = volume,
                Button = btn,
                Label = label,
                Background = bg
            });
        }

        void ToggleVolume(int volume) =>
            SetVolumeExpanded(volume, !_volumeExpanded[volume]);

        /// <summary>Collapse/expand one volume's entry rows; headers always stay visible so
        /// the reader opens on a concise list of groupings.</summary>
        void SetVolumeExpanded(int volume, bool expanded)
        {
            if (_volumeExpanded == null || volume < 0 || volume >= _volumeExpanded.Length)
                return;
            if (_volumeExpanded[volume] == expanded) return;
            _volumeExpanded[volume] = expanded;

            for (int i = 0; i < _navEntries.Count; i++)
            {
                if (_navEntries[i].Volume != volume) continue;
                if (_navEntries[i].Button != null)
                    _navEntries[i].Button.gameObject.SetActive(expanded);
            }
            for (int h = 0; h < _navHeaders.Count; h++)
            {
                if (_navHeaders[h].Volume == volume)
                    PaintHeader(h);
            }
        }

        void PaintHeader(int index)
        {
            var header = _navHeaders[index];
            if (header.Label == null) return;
            string mark = _volumeExpanded != null && _volumeExpanded[header.Volume] ? "- " : "+ ";
            header.Label.text = mark + header.Title.ToUpperInvariant();
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

            // A page opened from a body link may live in a collapsed volume — unfold it so
            // the selection is visible in the nav.
            for (int i = 0; i < _navEntries.Count; i++)
            {
                if (string.Equals(_navEntries[i].Stem, stem, StringComparison.OrdinalIgnoreCase))
                {
                    SetVolumeExpanded(_navEntries[i].Volume, true);
                    break;
                }
            }

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
