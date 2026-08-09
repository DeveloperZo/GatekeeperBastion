using System;
using System.Collections.Generic;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    /// <summary>Options presentation — five tabs, authored Shift rows, confirm for Reset.</summary>
    public sealed class OptionsView : MonoBehaviour
    {
        [SerializeField] GameObject _root;
        [SerializeField] Button[] _tabButtons;
        [SerializeField] GameObject[] _pages;
        [SerializeField] Button _resetButton;
        [SerializeField] Button _backButton;
        [SerializeField] OptionsRowBinding[] _rowBindings;
        [SerializeField] ModalWindowManager _confirmModal;
        [SerializeField] Button _confirmYesBtn;
        [SerializeField] Button _confirmNoBtn;
        [SerializeField] TextMeshProUGUI _confirmYesLabel;
        [SerializeField] TextMeshProUGUI _confirmNoLabel;
        [SerializeField] TextMeshProUGUI _displayRevertLabel;
        [SerializeField] Button _keepDisplayButton;

        readonly List<RowWidgets> _rows = new List<RowWidgets>();
        bool _wired;
        int _activeTab;
        Action _pendingConfirm;

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
            public Slider Slider;
            public TextMeshProUGUI ValueLabel;
            public TextMeshProUGUI NameLabel;
            public SwitchManager Switch;
            public HorizontalSelector IntSelector;
        }

        struct RowWidgets
        {
            public int RowIndex;
            public int Category;
            public Slider Slider;
            public TextMeshProUGUI ValueLabel;
            public TextMeshProUGUI NameLabel;
            public SwitchManager Switch;
            public HorizontalSelector IntSelector;
        }

        /// <summary>authored prefab required — activates root and wires serialized controls.</summary>
        public void EnsureBuilt()
        {
            if (_root != null) _root.SetActive(true);
            if (_wired) return;
            _wired = true;

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.onClick.AddListener(() =>
                    ShowConfirm(
                        "Reset this page?",
                        "Restore this tab's settings to defaults.",
                        "Reset to defaults",
                        "Keep current",
                        () => OnResetPage?.Invoke(_activeTab)));
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
            if (_confirmYesBtn != null)
            {
                _confirmYesBtn.onClick.RemoveAllListeners();
                _confirmYesBtn.onClick.AddListener(() =>
                {
                    var act = _pendingConfirm;
                    _pendingConfirm = null;
                    HideConfirm();
                    act?.Invoke();
                });
            }
            if (_confirmNoBtn != null)
            {
                _confirmNoBtn.onClick.RemoveAllListeners();
                _confirmNoBtn.onClick.AddListener(HideConfirm);
            }

            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    int tab = i;
                    if (_tabButtons[i] == null) continue;
                    _tabButtons[i].onClick.RemoveAllListeners();
                    _tabButtons[i].onClick.AddListener(() => ShowTab(tab));
                }
            }

            _rows.Clear();
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
                _rows.Add(new RowWidgets
                {
                    RowIndex = idx,
                    Category = b.Category,
                    Slider = b.Slider,
                    ValueLabel = b.ValueLabel,
                    NameLabel = b.NameLabel,
                    Switch = b.Switch,
                    IntSelector = b.IntSelector
                });
            }

            ShowTab(0);
        }

        public void SetOpen(bool open)
        {
            EnsureBuilt();
            if (_root != null) _root.SetActive(open);
            if (open) ShowTab(_activeTab);
            else HideConfirm();
        }

        public void ShowTab(int tab)
        {
            _activeTab = tab;
            if (_pages != null)
            {
                for (int i = 0; i < _pages.Length; i++)
                    if (_pages[i] != null) _pages[i].SetActive(i == tab);
            }
        }

        public void RebuildRows(IReadOnlyList<OptionsRowSpec> specs)
        {
            EnsureBuilt();
            RefreshRowValues(specs);
        }

        public void RefreshRowValues(IReadOnlyList<OptionsRowSpec> specs)
        {
            if (specs == null) return;
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                for (int r = 0; r < _rows.Count; r++)
                {
                    if (_rows[r].RowIndex != spec.RowIndex) continue;
                    if (_rows[r].NameLabel != null) _rows[r].NameLabel.text = spec.Label;
                    if (spec.IsFloat && _rows[r].Slider != null)
                    {
                        _rows[r].Slider.minValue = spec.Min;
                        _rows[r].Slider.maxValue = spec.Max;
                        _rows[r].Slider.SetValueWithoutNotify(spec.FloatValue);
                        if (_rows[r].ValueLabel != null)
                            _rows[r].ValueLabel.text = spec.FloatValue.ToString("0.##");
                    }
                    else if (spec.IsInt && _rows[r].IntSelector != null)
                    {
                        _rows[r].IntSelector.index = spec.IntValue;
                        _rows[r].IntSelector.defaultIndex = spec.IntValue;
                        _rows[r].IntSelector.UpdateUI();
                    }
                    else if (!spec.IsFloat && !spec.IsInt && _rows[r].Switch != null)
                    {
                        _rows[r].Switch.isOn = spec.BoolValue;
                        var anim = _rows[r].Switch.switchAnimator;
                        if (anim != null)
                            anim.Play(spec.BoolValue ? "Switch On" : "Switch Off");
                    }
                    break;
                }
            }
        }

        public void SetDisplayRevertSeconds(float secondsRemaining)
        {
            bool armed = secondsRemaining > 0f;
            if (_displayRevertLabel != null)
            {
                if (!armed)
                    _displayRevertLabel.gameObject.SetActive(false);
                else
                {
                    _displayRevertLabel.gameObject.SetActive(true);
                    _displayRevertLabel.text =
                        $"Reverting display in {Mathf.CeilToInt(secondsRemaining)}s";
                }
            }
            if (_keepDisplayButton != null)
                _keepDisplayButton.gameObject.SetActive(armed);
        }

        public bool ConfirmOpen => _confirmModal != null && _confirmModal.gameObject.activeSelf;

        public void CancelConfirm() => HideConfirm();

        void ShowConfirm(string title, string body, string yes, string no, Action onYes)
        {
            _pendingConfirm = onYes;
            if (_confirmYesLabel != null) _confirmYesLabel.text = yes;
            if (_confirmNoLabel != null) _confirmNoLabel.text = no;
            SetMainButtonText(_confirmYesBtn, yes);
            SetMainButtonText(_confirmNoBtn, no);
            if (_confirmModal != null)
            {
                _confirmModal.useCustomTexts = true;
                if (_confirmModal.windowTitle != null) _confirmModal.windowTitle.text = title;
                if (_confirmModal.windowDescription != null)
                    _confirmModal.windowDescription.text = body;
                _confirmModal.ModalWindowIn();
            }
            var es = EventSystem.current;
            if (es != null && _confirmNoBtn != null)
                es.SetSelectedGameObject(_confirmNoBtn.gameObject);
        }

        void HideConfirm()
        {
            _pendingConfirm = null;
            if (_confirmModal != null) _confirmModal.ModalWindowOut();
        }

        static void SetMainButtonText(Button btn, string text)
        {
            if (btn == null) return;
            var mb = btn.GetComponent<MainButton>();
            if (mb == null) return;
            mb.buttonText = text;
            if (mb.normalText != null) mb.normalText.text = text;
            if (mb.highlightedText != null) mb.highlightedText.text = text;
            if (mb.pressedText != null) mb.pressedText.text = text;
        }
    }
}
