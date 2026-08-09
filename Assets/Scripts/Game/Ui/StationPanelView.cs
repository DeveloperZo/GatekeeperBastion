using System;
using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game
{
    /// <summary>Shared framed panel for ship station consoles. Body rows Instantiated from authored templates.</summary>
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
        [SerializeField] StationRowView _rowPrefab;
        [SerializeField] TextMeshProUGUI _labelPrefab;
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

        /// <summary>ARMORY wheel host (M35). Prefab-authored; never destroyed by ClearBody.</summary>
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
                if (_wheel != null) return _wheel;
                if (_wheelSlot != null)
                    _wheel = _wheelSlot.GetComponentInChildren<ArmoryWheelView>(true);
                return _wheel;
            }
        }

        public void SetWheelVisible(bool visible)
        {
            EnsureBuilt();
            var w = Wheel;
            if (w != null) w.gameObject.SetActive(visible);
            else if (_wheelSlot != null) _wheelSlot.gameObject.SetActive(visible);
        }

        public static StationPanelView EnsureInstance()
        {
            if (_instance != null) return _instance;
            _instance = UnityEngine.Object.FindFirstObjectByType<StationPanelView>();
            _instance?.EnsureBuilt();
            return _instance;
        }

        public void EnsureBuilt()
        {
            if (_root != null) _root.gameObject.SetActive(true);
            if (_closeWired || _closeButton == null) return;
            _closeWired = true;
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => _onClose?.Invoke());
        }

        public void SetOnClose(Action onClose) => _onClose = onClose;

        public void SetHeader(string stationName, long banked, string lifetimeLine)
        {
            EnsureBuilt();
            if (_title != null) _title.text = stationName;
            if (_banked != null) _banked.text = $"Banked salvage: {banked}";
            if (_lifetime != null) _lifetime.text = lifetimeLine;
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

            // Wheel is authored under _wheelSlot — leave it; ArmoryStation re-Configure each open.
            SetWheelVisible(false);
        }

        public void PlayEntrance()
        {
            if (_bodyStagger == null) return;
            _bodyStagger.enabled = false;
            _bodyStagger.enabled = true;
        }

        public void AddSpacer(float height = 25f)
        {
            EnsureBuilt();
            if (_bodyContent == null || _spacerPrefab == null) return;
            var rt = Instantiate(_spacerPrefab, _bodyContent);
            rt.gameObject.SetActive(true);
            rt.name = "Spacer";
            var le = rt.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minHeight = height;
                le.preferredHeight = height;
            }
            Track(rt.gameObject);
        }

        public TextMeshProUGUI AddLabel(string text, bool centered = false, bool warning = false,
            bool reason = false)
        {
            EnsureBuilt();
            var parent = _rowParent != null ? _rowParent : _bodyContent;
            if (parent == null || _labelPrefab == null) return null;

            var tmp = Instantiate(_labelPrefab, parent);
            tmp.gameObject.SetActive(true);
            tmp.name = "Label";
            tmp.text = text ?? "";
            var align = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            var color = warning ? Theme.Warn : reason ? Theme.InkFaint : Theme.InkDim;
            var style = warning ? FontStyles.Bold : reason ? FontStyles.Italic : FontStyles.Normal;
            float size = centered ? Theme.Head : Theme.Body;
            Theme.Apply(tmp, size, color, style, align);
            Track(tmp.gameObject);
            return tmp;
        }

        public Button AddButton(string label, Action onClick, float width = 150f, float height = 50f,
            bool enabled = true)
        {
            EnsureBuilt();
            var parent = _rowParent != null ? _rowParent : _bodyContent;
            if (parent == null || _buttonPrefab == null) return null;

            var btn = Instantiate(_buttonPrefab, parent);
            btn.gameObject.SetActive(true);
            btn.name = "Action";
            ApplyButtonLabel(btn, label);
            var le = btn.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = width;
                le.minWidth = width;
                le.preferredHeight = height;
                le.minHeight = height;
            }
            btn.onClick.RemoveAllListeners();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            btn.interactable = enabled;
            Track(btn.gameObject);
            return btn;
        }

        public void AddReason(string text, float width = 260f, float height = 25f)
        {
            EnsureBuilt();
            var parent = _rowParent != null ? _rowParent : _bodyContent;
            if (parent == null || _labelPrefab == null) return;
            var tmp = Instantiate(_labelPrefab, parent);
            tmp.gameObject.SetActive(true);
            tmp.name = "Reason";
            tmp.text = text ?? "";
            Theme.Apply(tmp, Theme.Body, Theme.InkFaint, FontStyles.Italic);
            var le = tmp.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = width;
                le.minHeight = height;
            }
            Track(tmp.gameObject);
        }

        public void BeginRow()
        {
            EnsureBuilt();
            if (_bodyContent == null || _rowHostPrefab == null)
            {
                _rowParent = _bodyContent;
                return;
            }
            var row = Instantiate(_rowHostPrefab, _bodyContent);
            row.gameObject.SetActive(true);
            row.name = "Row";
            _rowParent = row;
            Track(row.gameObject);
        }

        public void EndRow() => _rowParent = null;

        public void AddRowLabel(string text, float width)
        {
            EnsureBuilt();
            if (_rowParent == null) BeginRow();
            if (_rowParent == null || _labelPrefab == null) return;
            var tmp = Instantiate(_labelPrefab, _rowParent);
            tmp.gameObject.SetActive(true);
            tmp.name = "Cell";
            tmp.text = text ?? "";
            Theme.Apply(tmp, Theme.Body, Theme.InkDim);
            var le = tmp.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = width;
                le.minWidth = width;
                le.preferredHeight = 25f;
            }
            Track(tmp.gameObject);
        }

        public void AddRowReason(string text, float width = 260f)
        {
            AddReason(text, width);
        }

        public void AddRowButton(string label, Action onClick, float width = 150f, bool enabled = true)
        {
            AddButton(label, onClick, width, 50f, enabled);
        }

        /// <summary>Criterion 19 priced row — unaffordable action stays visible and disabled.</summary>
        public StationRowView AddPricedRow(string name, string cost, string level, string effect,
            string actionLabel, Action onAction, bool actionEnabled, string reason = null)
        {
            EnsureBuilt();
            if (_bodyContent == null || _rowPrefab == null) return null;
            var row = Instantiate(_rowPrefab, _bodyContent);
            row.gameObject.SetActive(true);
            row.name = "PricedRow";
            row.Bind(name, cost, level, effect, actionLabel, onAction, actionEnabled, reason);
            Track(row.gameObject);
            return row;
        }

        public void SetCloseLabel(string label)
        {
            EnsureBuilt();
            if (_closeButton == null) return;
            ApplyButtonLabel(_closeButton, label);
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        void Track(GameObject go)
        {
            if (go != null) _spawned.Add(go);
        }

        bool IsTemplate(GameObject go)
        {
            if (go == null) return false;
            if (_rowPrefab != null && go == _rowPrefab.gameObject) return true;
            if (_labelPrefab != null && go == _labelPrefab.gameObject) return true;
            if (_buttonPrefab != null && go == _buttonPrefab.gameObject) return true;
            if (_spacerPrefab != null && go == _spacerPrefab.gameObject) return true;
            if (_rowHostPrefab != null && go == _rowHostPrefab.gameObject) return true;
            return false;
        }

        static void ApplyButtonLabel(Button button, string label)
        {
            if (button == null) return;
            var main = button.GetComponent<Michsky.UI.Shift.MainButton>();
            if (main != null)
            {
                main.buttonText = label ?? "";
                main.useCustomText = false;
            }
            var panel = button.GetComponent<Michsky.UI.Shift.MainPanelButton>();
            if (panel != null)
            {
                panel.buttonText = label ?? "";
                panel.useCustomText = false;
            }
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = label ?? "";
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
