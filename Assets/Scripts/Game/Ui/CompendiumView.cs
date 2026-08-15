using System;
using System.Collections.Generic;
using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: pause Compendium reader — volume list left, entry body right.
    /// Parses headings, bold, and <c>[text](Other-Page.md)</c> links into TMP rich text.
    /// Authored on the pause canvas; serialized references only.</summary>
    public sealed class CompendiumView : MonoBehaviour, IPointerClickHandler
    {
        const string Tag = "Compendium";

        [SerializeField] GameObject _root;
        [SerializeField] RectTransform _volumeListRoot;
        [SerializeField] GameObject _volumeButtonTemplate;
        [SerializeField] RectTransform _entryListRoot;
        [SerializeField] GameObject _entryButtonTemplate;
        [SerializeField] TextMeshProUGUI _titleLabel;
        [SerializeField] TextMeshProUGUI _bodyLabel;
        [SerializeField] ScrollRect _bodyScroll;
        [SerializeField] Button _closeButton;

        readonly List<Button> _volumeButtons = new List<Button>(8);
        readonly List<Button> _entryButtons = new List<Button>(16);
        int _openVolume = -1;
        string _openStem;
        bool _built;

        public bool IsOpen => _root != null && _root.activeSelf;

        public event Action Closed;

        void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
            if (_volumeButtonTemplate != null)
                _volumeButtonTemplate.SetActive(false);
            if (_entryButtonTemplate != null)
                _entryButtonTemplate.SetActive(false);
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
            _root.SetActive(true);
            if (!string.IsNullOrEmpty(preferredStem) && TryOpenStem(preferredStem))
                return;
            if (_openVolume < 0)
                SelectVolume(0);
            else
                RefreshBody();
        }

        public void Close()
        {
            if (_root == null || !_root.activeSelf) return;
            _root.SetActive(false);
            if (Closed != null) Closed.Invoke();
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
            if (_volumeListRoot == null || _volumeButtonTemplate == null)
            {
                Log.Error(Tag, "Volume list template not authored.");
                return;
            }

            for (int i = 0; i < CompendiumCatalog.Volumes.Length; i++)
            {
                int volumeIndex = i;
                var go = Instantiate(_volumeButtonTemplate, _volumeListRoot);
                go.name = "Volume_" + CompendiumCatalog.Volumes[i].Title;
                go.SetActive(true);
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = CompendiumCatalog.Volumes[i].Title;
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => SelectVolume(volumeIndex));
                    _volumeButtons.Add(btn);
                }
            }
        }

        void SelectVolume(int index)
        {
            if (index < 0 || index >= CompendiumCatalog.Volumes.Length) return;
            _openVolume = index;
            RebuildEntryList();
            var stems = CompendiumCatalog.Volumes[index].EntryStems;
            if (stems.Length > 0)
                TryOpenStem(stems[0]);
        }

        void RebuildEntryList()
        {
            if (_entryListRoot == null || _entryButtonTemplate == null) return;

            for (int i = 0; i < _entryButtons.Count; i++)
            {
                if (_entryButtons[i] != null)
                    Destroy(_entryButtons[i].gameObject);
            }
            _entryButtons.Clear();

            if (_openVolume < 0) return;
            var stems = CompendiumCatalog.Volumes[_openVolume].EntryStems;
            for (int i = 0; i < stems.Length; i++)
            {
                string stem = stems[i];
                string title = stem.Replace('-', ' ');
                if (CompendiumCatalog.TryLoadEntry(stem, out var entry))
                    title = entry.Title;

                var go = Instantiate(_entryButtonTemplate, _entryListRoot);
                go.name = "Entry_" + stem;
                go.SetActive(true);
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = title;
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    string captured = stem;
                    btn.onClick.AddListener(() => TryOpenStem(captured));
                    _entryButtons.Add(btn);
                }
            }
        }

        bool TryOpenStem(string stem)
        {
            if (!CompendiumCatalog.TryLoadEntry(stem, out var entry))
            {
                Log.Warn(Tag, "Missing Compendium page: " + stem);
                return false;
            }

            int volume = FindVolumeForStem(stem);
            if (volume >= 0 && volume != _openVolume)
            {
                _openVolume = volume;
                RebuildEntryList();
            }

            _openStem = stem;
            if (_titleLabel != null) _titleLabel.text = entry.Title;
            if (_bodyLabel != null)
                _bodyLabel.text = CompendiumCatalog.ToTmpRichText(entry.BodyMarkdown);
            if (_bodyScroll != null)
                _bodyScroll.verticalNormalizedPosition = 1f;
            return true;
        }

        void RefreshBody()
        {
            if (!string.IsNullOrEmpty(_openStem))
                TryOpenStem(_openStem);
        }

        static int FindVolumeForStem(string stem)
        {
            for (int v = 0; v < CompendiumCatalog.Volumes.Length; v++)
            {
                var stems = CompendiumCatalog.Volumes[v].EntryStems;
                for (int i = 0; i < stems.Length; i++)
                {
                    if (string.Equals(stems[i], stem, StringComparison.OrdinalIgnoreCase))
                        return v;
                }
            }
            return -1;
        }
    }
}
