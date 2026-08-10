using System;
using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>Binds one of the station panels duplicated from Shift's <c>Home</c> panel.
    /// Station scripts push text and actions; rows are clones of templates authored under
    /// <c>Content</c>, so this class sets no colour, no font size and no rect (M35 criteria
    /// 5, 6, 17). Emphasis travels as <c>&lt;b&gt;</c> / <c>&lt;i&gt;</c> markup.</summary>
    public sealed class StationPanelView : MonoBehaviour
    {
        static StationPanelView _instance;

        [SerializeField] RectTransform _root;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _banked;
        [SerializeField] TextMeshProUGUI _lifetime;
        [SerializeField] RectTransform _bodyContent;
        [SerializeField] RectTransform _wheelSlot;
        [SerializeField] ArmoryWheelView _wheel;
        [SerializeField] Button _closeButton;

        [Header("Templates — authored under Content, cloned per row")]
        [SerializeField] StationRowView _rowPrefab;
        [SerializeField] TextMeshProUGUI _labelPrefab;
        [SerializeField] TextMeshProUGUI _headingPrefab;
        [SerializeField] Button _buttonPrefab;
        [SerializeField] RectTransform _spacerPrefab;
        [SerializeField] RectTransform _rowHostPrefab;
        [SerializeField] StaggerEntrance _bodyStagger;

        Action _onClose;
        bool _closeWired;
        RectTransform _rowParent;
        readonly List<GameObject> _spawned = new List<GameObject>(32);

        public RectTransform BodyContent
        {
            get
            {
                EnsureBuilt();
                return _bodyContent;
            }
        }

        /// <summary>ARMORY wheel host. Authored under <c>Content</c>; never destroyed by ClearBody.</summary>
        public RectTransform WheelSlot
        {
            get
            {
                EnsureBuilt();
                return _wheelSlot;
            }
        }

        public ArmoryWheelView Wheel
        {
            get
            {
                EnsureBuilt();
                if (_wheel == null && _wheelSlot != null)
                    _wheel = _wheelSlot.GetComponentInChildren<ArmoryWheelView>(true);
                return _wheel;
            }
        }

        public void SetWheelVisible(bool visible)
        {
            EnsureBuilt();
            var w = Wheel;
            if (w != null) UiBind.Show(w, visible);
            else UiBind.Show(_wheelSlot, visible);
        }

        void Awake()
        {
            _instance = this;
            UiCanvasGuard.RepairIfBroken(gameObject);
            // The panel's CanvasGroup starts at alpha 0 (Shift idiom); never force-open from Awake.
            UiBind.Show(_root, false);
        }

        void OnEnable() => UiCanvasGuard.RepairIfBroken(gameObject);

        public static StationPanelView EnsureInstance()
        {
            if (_instance != null) return _instance;
            _instance = UiBind.First<StationPanelView>();
            _instance?.EnsureBuilt();
            return _instance;
        }

        /// <summary>Resolves the authored panel and wires close; does not open it —
        /// <see cref="SetVisible"/> owns that.</summary>
        public void EnsureBuilt()
        {
            UiCanvasGuard.RepairIfBroken(gameObject);
            if (_bodyContent == null)
                _bodyContent = UiBind.Find(transform, "Content") as RectTransform;
            if (_closeWired || _closeButton == null) return;
            _closeWired = true;
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => _onClose?.Invoke());
        }

        public void SetOnClose(Action onClose) => _onClose = onClose;

        public void SetHeader(string stationName, long banked, string lifetimeLine)
        {
            EnsureBuilt();
            UiBind.SetText(_title, stationName);
            UiBind.SetText(_banked, $"Banked salvage: {banked}");
            UiBind.SetText(_lifetime, lifetimeLine);
        }

        public void ClearBody()
        {
            EnsureBuilt();
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
            EnsureBuilt();
            var rt = Clone(_spacerPrefab, _bodyContent, "Spacer");
            if (rt != null) Track(rt.gameObject);
        }

        /// <summary>A body line. <paramref name="centered"/> picks the heading template;
        /// <paramref name="warning"/> and <paramref name="reason"/> become markup.</summary>
        public TextMeshProUGUI AddLabel(string text, bool centered = false, bool warning = false,
            bool reason = false)
        {
            EnsureBuilt();
            var parent = _rowParent != null ? _rowParent : _bodyContent;
            var template = centered && _headingPrefab != null ? _headingPrefab : _labelPrefab;
            var tmp = Clone(template, parent, "Label");
            if (tmp != null) tmp.text = Emphasise(text, warning, reason);
            if (tmp != null) Track(tmp.gameObject);
            return tmp;
        }

        public Button AddButton(string label, Action onClick, bool enabled = true)
        {
            EnsureBuilt();
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
            EnsureBuilt();
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
            EnsureBuilt();
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
            EnsureBuilt();
            var row = Clone(_rowPrefab, _bodyContent, "PricedRow");
            if (row == null) return null;
            row.Bind(name, cost, level, effect, actionLabel, onAction, actionEnabled, reason);
            Track(row.gameObject);
            return row;
        }

        public void SetCloseLabel(string label)
        {
            EnsureBuilt();
            ApplyButtonLabel(_closeButton, label);
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            UiBind.Show(_root, visible);
        }

        static string Emphasise(string text, bool warning, bool reason)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (warning) return $"<b>{text}</b>";
            return reason ? $"<i>{text}</i>" : text;
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
            return Is(go, _rowPrefab) || Is(go, _labelPrefab) || Is(go, _headingPrefab)
                || Is(go, _buttonPrefab) || Is(go, _spacerPrefab) || Is(go, _rowHostPrefab);
        }

        static bool Is(GameObject go, Component template) =>
            template != null && go == template.gameObject;

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
            if (tmp != null) tmp.text = text;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
