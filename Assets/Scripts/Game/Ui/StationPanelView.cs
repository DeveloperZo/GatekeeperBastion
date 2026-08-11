using System;
using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>One authored station panel. Templates live under <c>Content</c> and are cloned
    /// per row; this class sets no colour, font size or rect.</summary>
    public sealed class StationPanelView : MonoBehaviour
    {
        const string Tag = "StationPanelView";

        [SerializeField] RectTransform _root;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _banked;
        [SerializeField, OptionalBinding] TextMeshProUGUI _lifetime;
        [SerializeField] RectTransform _bodyContent;
        [SerializeField, OptionalBinding] RectTransform _wheelSlot;
        [SerializeField, OptionalBinding] ArmoryWheelView _wheel;
        [SerializeField] Button _closeButton;

        [Header("Templates — authored under Content, cloned per row")]
        [SerializeField] StationRowView _rowPrefab;
        [SerializeField] TextMeshProUGUI _labelPrefab;
        [SerializeField] TextMeshProUGUI _headingPrefab;
        [SerializeField] Button _buttonPrefab;
        [SerializeField] RectTransform _spacerPrefab;
        [SerializeField] RectTransform _rowHostPrefab;
        [SerializeField, OptionalBinding] StaggerEntrance _bodyStagger;

        Action _onClose;
        bool _closeWired;
        bool _bindingsChecked;
        RectTransform _rowParent;
        readonly List<GameObject> _spawned = new List<GameObject>(32);

        public RectTransform BodyContent => _bodyContent;

        /// <summary>ARMORY wheel host. Authored under <c>Content</c>; never destroyed by ClearBody.</summary>
        public RectTransform WheelSlot => _wheelSlot;

        public ArmoryWheelView Wheel
        {
            get
            {
                if (_wheel == null && _wheelSlot != null)
                    _wheel = _wheelSlot.GetComponentInChildren<ArmoryWheelView>(true);
                return _wheel;
            }
        }

        public void SetWheelVisible(bool visible)
        {
            var w = Wheel;
            if (w != null) UiBind.Show(w, visible);
            else UiBind.Show(_wheelSlot, visible);
        }

        void Awake()
        {
            UiCanvasGuard.RepairIfBroken(gameObject);
            WireClose();
            ValidateRequiredBindings();
        }

        void OnEnable() => UiCanvasGuard.RepairIfBroken(gameObject);

        /// <summary>Logs one error per missing required field. OptionalBinding fields are skipped.</summary>
        public void ValidateRequiredBindings()
        {
            if (_bindingsChecked) return;
            _bindingsChecked = true;
            Require(nameof(_root), _root);
            Require(nameof(_title), _title);
            Require(nameof(_banked), _banked);
            Require(nameof(_bodyContent), _bodyContent);
            Require(nameof(_closeButton), _closeButton);
            Require(nameof(_rowPrefab), _rowPrefab);
            Require(nameof(_labelPrefab), _labelPrefab);
            Require(nameof(_headingPrefab), _headingPrefab);
            Require(nameof(_buttonPrefab), _buttonPrefab);
            Require(nameof(_spacerPrefab), _spacerPrefab);
            Require(nameof(_rowHostPrefab), _rowHostPrefab);
        }

        void Require(string field, UnityEngine.Object value)
        {
            if (value != null) return;
            Log.Error(Tag, $"{name}: missing binding '{field}'.");
        }

        void WireClose()
        {
            if (_closeWired || _closeButton == null) return;
            _closeWired = true;
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => _onClose?.Invoke());
        }

        public void SetOnClose(Action onClose)
        {
            _onClose = onClose;
            WireClose();
        }

        public void SetHeader(string stationName, long banked, string lifetimeLine)
        {
            UiBind.SetText(_title, stationName);
            UiBind.SetText(_banked, "Banked salvage: " + banked);
            UiBind.SetText(_lifetime, lifetimeLine);
        }

        public void ClearBody()
        {
            _rowParent = null;
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();

            if (_bodyContent != null)
            {
                for (int i = _bodyContent.childCount - 1; i >= 0; i--)
                {
                    var child = _bodyContent.GetChild(i).gameObject;
                    if (IsTemplate(child)) continue;
                    if (_wheelSlot != null && child == _wheelSlot.gameObject) continue;
                    if (_wheel != null && child == _wheel.gameObject) continue;
                    Destroy(child);
                }
            }

            SetWheelVisible(false);
        }

        public void PlayEntrance()
        {
            if (_bodyStagger == null) return;
            _bodyStagger.enabled = false;
            _bodyStagger.enabled = true;
        }

        public void AddSpacer()
        {
            var rt = Clone(_spacerPrefab, _bodyContent, "Spacer");
            if (rt != null) Track(rt.gameObject);
        }

        /// <summary>A body line. <paramref name="centered"/> picks the heading template;
        /// <paramref name="warning"/> and <paramref name="reason"/> become markup.</summary>
        public TextMeshProUGUI AddLabel(string text, bool centered = false, bool warning = false,
            bool reason = false)
        {
            var parent = _rowParent != null ? _rowParent : _bodyContent;
            var template = centered && _headingPrefab != null ? _headingPrefab : _labelPrefab;
            var tmp = Clone(template, parent, "Label");
            if (tmp != null) tmp.text = Emphasise(text, warning, reason);
            if (tmp != null) Track(tmp.gameObject);
            return tmp;
        }

        public Button AddButton(string label, Action onClick, bool enabled = true)
        {
            var parent = _rowParent != null ? _rowParent : _bodyContent;
            var btn = Clone(_buttonPrefab, parent, "Action");
            if (btn == null) return null;
            ApplyButtonLabel(btn, label);
            btn.onClick.RemoveAllListeners();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            btn.interactable = enabled;
            Track(btn.gameObject);
            return btn;
        }

        public void AddReason(string text) => AddLabel(text, reason: true);

        public void BeginRow()
        {
            var row = Clone(_rowHostPrefab, _bodyContent, "Row");
            if (row == null)
            {
                _rowParent = _bodyContent;
                return;
            }
            _rowParent = row;
            Track(row.gameObject);
        }

        public void EndRow() => _rowParent = null;

        public void AddRowLabel(string text)
        {
            if (_rowParent == null) BeginRow();
            var tmp = Clone(_labelPrefab, _rowParent, "Cell");
            if (tmp == null) return;
            tmp.text = text ?? "";
            Track(tmp.gameObject);
        }

        public void AddRowReason(string text) => AddReason(text);

        public void AddRowButton(string label, Action onClick, bool enabled = true) =>
            AddButton(label, onClick, enabled);

        /// <summary>A priced row: an unaffordable action stays visible and disabled with its
        /// reason, rather than disappearing.</summary>
        public StationRowView AddPricedRow(string name, string cost, string level, string effect,
            string actionLabel, Action onAction, bool actionEnabled, string reason = null)
        {
            var row = Clone(_rowPrefab, _bodyContent, "PricedRow");
            if (row == null) return null;
            row.Bind(name, cost, level, effect, actionLabel, onAction, actionEnabled, reason);
            Track(row.gameObject);
            return row;
        }

        public void SetCloseLabel(string label) => ApplyButtonLabel(_closeButton, label);

        static string Emphasise(string text, bool warning, bool reason)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (warning) return "<b>" + text + "</b>";
            return reason ? "<i>" + text + "</i>" : text;
        }

        static T Clone<T>(T template, Transform parent, string name) where T : Component
        {
            if (template == null || parent == null) return null;
            var copy = Instantiate(template, parent);
            copy.gameObject.SetActive(true);
            copy.name = name;
            return copy;
        }

        void Track(GameObject go)
        {
            if (go != null) _spawned.Add(go);
        }

        bool IsTemplate(GameObject go)
        {
            if (go == null) return false;
            if (Is(go, _rowPrefab) || Is(go, _labelPrefab) || Is(go, _headingPrefab)
                || Is(go, _buttonPrefab) || Is(go, _spacerPrefab) || Is(go, _rowHostPrefab))
                return true;
            // Inactive Templates folder under Content hosts the serialized prefabs. Without
            // this, ClearBody destroys the host on the first refresh and every later Clone
            // returns null (Primary/Secondary toggle, buy, equip).
            return !go.activeSelf && HostsTemplate(go.transform);
        }

        bool HostsTemplate(Transform host)
        {
            return ChildOf(host, _rowPrefab) || ChildOf(host, _labelPrefab)
                || ChildOf(host, _headingPrefab) || ChildOf(host, _buttonPrefab)
                || ChildOf(host, _spacerPrefab) || ChildOf(host, _rowHostPrefab);
        }

        static bool Is(GameObject go, Component template) =>
            template != null && go == template.gameObject;

        static bool ChildOf(Transform host, Component template) =>
            template != null && template.transform.IsChildOf(host);

        static void ApplyButtonLabel(Button button, string label)
        {
            if (button == null) return;
            string text = label ?? "";
            var main = button.GetComponent<Michsky.UI.Shift.MainButton>();
            if (main != null)
            {
                main.buttonText = text;
                main.useCustomText = false;
            }
            var panel = button.GetComponent<Michsky.UI.Shift.MainPanelButton>();
            if (panel != null)
            {
                panel.buttonText = text;
                panel.useCustomText = false;
            }
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) UiBind.SetText(tmp, text);
        }
    }
}

// wp3-refresh
