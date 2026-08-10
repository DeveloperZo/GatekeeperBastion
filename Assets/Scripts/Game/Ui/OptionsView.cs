using System;
using System.Collections.Generic;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>One options row spec pushed by <see cref="OptionsScreen"/> — no Sim types.</summary>
    public struct OptionsRowSpec
    {
        public int RowIndex;
        public string Label;
        public int Category;
        public bool IsFloat;
        public bool IsInt;
        public float FloatValue;
        public float Min;
        public float Max;
        public float DefaultFloat;
        public bool BoolValue;
        public bool DefaultBool;
        public int IntValue;
        public int DefaultInt;
    }

    /// <summary>One generated Controls-tab row (M35 criterion 13): keycap text + action label.
    /// Built by <see cref="OptionsScreen"/> from the single-source binding table.</summary>
    public struct OptionsControlRow
    {
        public string Key;
        public string Label;
    }

    /// <summary>Options presentation over Shift's authored Settings panel. Registers itself in
    /// <see cref="Awake"/> so <see cref="OptionsScreen"/> reaches it without a scene search
    /// (M35 criterion 14). All wiring is serialized (criterion 13); Shift's nested
    /// MainPanelManager owns sub-tab visuals — this class only records the active tab index,
    /// whose order matches <c>SettingCategory</c> (criterion 13). The Controls tab's binding
    /// rows are generated at open by cloning <see cref="_controlsRowTemplate"/>.</summary>
    public sealed class OptionsView : MonoBehaviour
    {
        [Header("Panel routing (criterion 14)")]
        [SerializeField] MainPanelManager _panels;
        [SerializeField] string _settingsPanel = "Settings";
        [SerializeField] string _homePanel = "Home";
        [Tooltip("Canvas root to (de)activate around open/close; null on the menu, whose canvas is always live.")]
        [SerializeField, OptionalBinding] GameObject _root;

        [Header("Tabs — order must match SettingCategory")]
        [SerializeField] Button[] _tabButtons;

        [Header("Rows (criterion 13)")]
        [SerializeField] OptionsRowBinding[] _rowBindings;
        [SerializeField] Button _resetButton;
        [SerializeField] Button _backButton;

        [Header("Display-mode revert (criterion 13)")]
        [SerializeField] TextMeshProUGUI _displayRevertLabel;
        [SerializeField] Button _keepDisplayButton;

        [Header("Controls tab — generated at open (criterion 13)")]
        [SerializeField] RectTransform _controlsRowsRoot;
        [SerializeField] GameObject _controlsRowTemplate;

        /// <summary>The scene's registered instance, set in <see cref="Awake"/>. Canvas
        /// activation always precedes <see cref="OptionsScreen.Open"/>, so this exists by
        /// first use.</summary>
        public static OptionsView Registered { get; private set; }

        readonly List<ControlRowWidgets> _controlRowPool = new List<ControlRowWidgets>(24);
        int _activeTab;

        public Action<int, float> OnFloatChanged;
        public Action<int> OnFloatDragEnd;
        public Action<int, bool> OnBoolChanged;
        public Action<int, int> OnIntChanged;
        public Action<int> OnResetPage;
        public Action OnConfirmDisplay;
        public Action OnBack;
        public int ActiveTab => _activeTab;

        [Serializable]
        public sealed class OptionsRowBinding
        {
            public int RowIndex;
            public int Category;
            [OptionalBinding] public Slider Slider;
            [OptionalBinding] public TextMeshProUGUI ValueLabel;
            [OptionalBinding] public TextMeshProUGUI NameLabel;
            [OptionalBinding] public SwitchManager Switch;
            [OptionalBinding] public HorizontalSelector IntSelector;
        }

        struct ControlRowWidgets
        {
            public GameObject Root;
            public TextMeshProUGUI Key;
            public TextMeshProUGUI Label;
        }

        void Awake()
        {
            Registered = this;
            ValidateBindings();
            WireControls();
        }

        void OnDestroy()
        {
            if (Registered == this) Registered = null;
        }

        void ValidateBindings()
        {
            Require(_panels, nameof(_panels));
            Require(_resetButton, nameof(_resetButton));
            Require(_backButton, nameof(_backButton));
            Require(_displayRevertLabel, nameof(_displayRevertLabel));
            Require(_keepDisplayButton, nameof(_keepDisplayButton));
            Require(_controlsRowsRoot, nameof(_controlsRowsRoot));
            Require(_controlsRowTemplate, nameof(_controlsRowTemplate));
            if (_tabButtons == null || _tabButtons.Length == 0)
                Require(null, nameof(_tabButtons));
            if (_rowBindings == null || _rowBindings.Length == 0)
                Require(null, nameof(_rowBindings));
        }

        void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
                Debug.LogError(
                    $"[OptionsView] '{name}' is missing serialized reference '{field}' " +
                    "(M35 criterion 13 — wire it in the scene).", this);
        }

        void WireControls()
        {
            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.onClick.AddListener(() => OnResetPage?.Invoke(_activeTab));
            }
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(() => OnBack?.Invoke());
            }
            if (_keepDisplayButton != null)
            {
                _keepDisplayButton.onClick.RemoveAllListeners();
                _keepDisplayButton.onClick.AddListener(() => OnConfirmDisplay?.Invoke());
                _keepDisplayButton.gameObject.SetActive(false);
            }
            if (_displayRevertLabel != null)
                _displayRevertLabel.gameObject.SetActive(false);

            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    int tab = i;
                    if (_tabButtons[i] == null) continue;
                    // Runtime listener only records the index; the authored persistent call on
                    // the same button drives Shift's nested MainPanelManager page switch.
                    _tabButtons[i].onClick.AddListener(() => ShowTab(tab));
                }
            }

            if (_rowBindings == null) return;
            for (int i = 0; i < _rowBindings.Length; i++)
            {
                var b = _rowBindings[i];
                if (b == null) continue;
                int idx = b.RowIndex;
                if (b.Slider != null)
                {
                    b.Slider.onValueChanged.RemoveAllListeners();
                    b.Slider.onValueChanged.AddListener(v =>
                    {
                        OnFloatChanged?.Invoke(idx, v);
                        OnFloatDragEnd?.Invoke(idx);
                    });
                }
                if (b.Switch != null)
                {
                    b.Switch.saveValue = false;
                    b.Switch.invokeAtStart = false;
                    b.Switch.OnEvents.RemoveAllListeners();
                    b.Switch.OffEvents.RemoveAllListeners();
                    b.Switch.OnEvents.AddListener(() => OnBoolChanged?.Invoke(idx, true));
                    b.Switch.OffEvents.AddListener(() => OnBoolChanged?.Invoke(idx, false));
                }
                if (b.IntSelector != null)
                {
                    b.IntSelector.saveValue = false;
                    b.IntSelector.invokeAtStart = false;
                    if (b.IntSelector.itemList != null)
                    {
                        for (int k = 0; k < b.IntSelector.itemList.Count; k++)
                        {
                            int value = k;
                            b.IntSelector.itemList[k].onValueChanged.RemoveAllListeners();
                            b.IntSelector.itemList[k].onValueChanged
                                .AddListener(() => OnIntChanged?.Invoke(idx, value));
                        }
                    }
                }
            }
        }

        /// <summary>Route in (M35 criterion 14): activates the canvas root when one is
        /// authored, then opens Shift's Settings panel on this scene's MainPanelManager.</summary>
        public void Open()
        {
            if (_root != null) _root.SetActive(true);
            if (_panels != null) _panels.OpenPanel(_settingsPanel);
        }

        public void SetOpen(bool open)
        {
            if (open)
            {
                Open();
                return;
            }
            SetDisplayRevertSeconds(0f);
            if (_root != null) _root.SetActive(false);
            else if (_panels != null) _panels.OpenPanel(_homePanel);
        }

        /// <summary>Records the active sub-tab; index order matches <c>SettingCategory</c>
        /// (criterion 13). Page visuals belong to Shift's nested MainPanelManager.</summary>
        public void ShowTab(int tab) => _activeTab = tab;

        /// <summary>Scene-hotkey entry (Shift's authored PressKeyEvent) — routes the panel's
        /// Esc through the same close path as the BACK button.</summary>
        public void CloseFromHotkey() => OnBack?.Invoke();

        public void RefreshRowValues(IReadOnlyList<OptionsRowSpec> specs)
        {
            if (specs == null || _rowBindings == null) return;
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                for (int r = 0; r < _rowBindings.Length; r++)
                {
                    var row = _rowBindings[r];
                    if (row == null || row.RowIndex != spec.RowIndex) continue;
                    if (row.NameLabel != null) UiBind.SetText(row.NameLabel, spec.Label);
                    if (spec.IsFloat && row.Slider != null)
                    {
                        row.Slider.minValue = spec.Min;
                        row.Slider.maxValue = spec.Max;
                        row.Slider.SetValueWithoutNotify(spec.FloatValue);
                        if (row.ValueLabel != null)
                            UiBind.SetText(row.ValueLabel, spec.FloatValue.ToString("0.##"));
                    }
                    else if (spec.IsInt && row.IntSelector != null)
                    {
                        row.IntSelector.index = spec.IntValue;
                        row.IntSelector.defaultIndex = spec.IntValue;
                        if (row.IntSelector.gameObject.activeInHierarchy)
                            row.IntSelector.UpdateUI();
                    }
                    else if (!spec.IsFloat && !spec.IsInt && row.Switch != null)
                    {
                        row.Switch.isOn = spec.BoolValue;
                        var anim = row.Switch.switchAnimator;
                        if (anim != null && anim.isActiveAndEnabled)
                            anim.Play(spec.BoolValue ? "Switch On" : "Switch Off");
                    }
                    break;
                }
            }
        }

        /// <summary>Fills the Controls tab from the single-source binding table by cloning the
        /// serialized row template (criterion 13). Pooled — rows beyond the list deactivate.
        /// Template contract: the TMP label under a child named "Primary" is the keycap;
        /// any other TMP label is the action label.</summary>
        public void SetControlRows(IReadOnlyList<OptionsControlRow> rows)
        {
            if (_controlsRowTemplate == null || _controlsRowsRoot == null) return;
            if (_controlsRowTemplate.activeSelf) _controlsRowTemplate.SetActive(false);

            int used = 0;
            for (int i = 0; i < (rows?.Count ?? 0); i++)
            {
                ControlRowWidgets slot = ControlRowAt(used++);
                UiBind.SetText(slot.Key, rows[i].Key);
                UiBind.SetText(slot.Label, rows[i].Label);
                slot.Root.transform.SetAsLastSibling();
                UiBind.Show(slot.Root, true);
            }
            for (int i = used; i < _controlRowPool.Count; i++)
                UiBind.Show(_controlRowPool[i].Root, false);
        }

        ControlRowWidgets ControlRowAt(int index)
        {
            while (_controlRowPool.Count <= index)
            {
                GameObject clone = Instantiate(_controlsRowTemplate, _controlsRowsRoot);
                var widgets = new ControlRowWidgets { Root = clone };
                var labels = clone.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    if (HasAncestorNamed(labels[i].transform, clone.transform, "Primary"))
                        widgets.Key = labels[i];
                    else if (widgets.Label == null)
                        widgets.Label = labels[i];
                }
                _controlRowPool.Add(widgets);
            }
            return _controlRowPool[index];
        }

        static bool HasAncestorNamed(Transform t, Transform stopAt, string ancestorName)
        {
            for (Transform p = t; p != null && p != stopAt; p = p.parent)
            {
                if (p.name == ancestorName) return true;
            }
            return false;
        }

        public void SetDisplayRevertSeconds(float secondsRemaining)
        {
            bool armed = secondsRemaining > 0f;
            if (_displayRevertLabel != null)
            {
                UiBind.Show(_displayRevertLabel.gameObject, armed);
                if (armed)
                    UiBind.SetText(_displayRevertLabel,
                        $"Reverting display in {Mathf.CeilToInt(secondsRemaining)}s");
            }
            if (_keepDisplayButton != null)
                UiBind.Show(_keepDisplayButton.gameObject, armed);
        }
    }
}
