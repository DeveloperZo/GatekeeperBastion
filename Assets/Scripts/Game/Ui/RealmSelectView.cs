using System;
using System.Collections.Generic;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// LOCAL-OWNER: binds the realm chooser authored in <c>RealmSelect.unity</c>. Dumb by
    /// design — it owns no rules, only widgets, and hands every decision to
    /// <see cref="RealmSelectController"/>.
    /// </summary>
    /// <remarks>
    /// Rows are cloned from a template kept inactive in the scene, the idiom
    /// <c>OptionsView.SetControlRows</c> and <c>CompendiumView</c> already use here. The pool
    /// is reused rather than destroyed, so reopening the screen allocates nothing after the
    /// first paint.
    ///
    /// Every serialized reference is required, and a missing one logs one error naming the
    /// field rather than silently no-opping — a chooser that draws nothing looks identical to
    /// a player with no realms.
    /// </remarks>
    public sealed class RealmSelectView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] RectTransform _realmRowsRoot;
        [SerializeField] GameObject _realmRowTemplate;
        [SerializeField] RectTransform _boarderRowsRoot;
        [SerializeField] GameObject _boarderRowTemplate;
        [SerializeField] TMP_InputField _nameField;
        [SerializeField] Button _createBtn;
        [SerializeField] Button _enterBtn;
        [SerializeField] Button _backBtn;
        [SerializeField] TextMeshProUGUI _status;

        public Action onCreate;
        public Action onEnter;
        public Action onBack;
        public Action<int> onPickRealm;
        public Action<int> onPickBoarder;

        readonly List<GameObject> _realmRows = new List<GameObject>();
        readonly List<GameObject> _boarderRows = new List<GameObject>();

        static readonly Color Picked = new Color(0.85f, 0.92f, 1f, 1f);
        static readonly Color Normal = new Color(0.62f, 0.66f, 0.72f, 1f);
        static readonly Color Refused = new Color(0.75f, 0.42f, 0.42f, 1f);

        /// <summary>What the player has typed for a new realm's name. Read on demand; there is
        /// no per-keystroke listener because nothing reacts until Create is pressed.</summary>
        public string NameText
        {
            get { return _nameField != null ? _nameField.text : ""; }
        }

        void Awake()
        {
            ValidateBindings();
            if (_realmRowTemplate != null) _realmRowTemplate.SetActive(false);
            if (_boarderRowTemplate != null) _boarderRowTemplate.SetActive(false);
            Wire(_createBtn, () => { if (onCreate != null) onCreate.Invoke(); });
            Wire(_enterBtn, () => { if (onEnter != null) onEnter.Invoke(); });
            Wire(_backBtn, () => { if (onBack != null) onBack.Invoke(); });
            if (_title != null) _title.text = "REALMS";
        }

        static void Wire(Button btn, Action handler)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => handler());
        }

        void ValidateBindings()
        {
            Require(_realmRowsRoot, "_realmRowsRoot");
            Require(_realmRowTemplate, "_realmRowTemplate");
            Require(_boarderRowsRoot, "_boarderRowsRoot");
            Require(_boarderRowTemplate, "_boarderRowTemplate");
            Require(_nameField, "_nameField");
            Require(_createBtn, "_createBtn");
            Require(_enterBtn, "_enterBtn");
            Require(_backBtn, "_backBtn");
        }

        void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
                Debug.LogError("[RealmSelectView] '" + name + "' is missing serialized reference '"
                    + field + "' (wire it in RealmSelect.unity).", this);
        }

        // ---- Painting ----

        public void SetRealms(RealmRegistry.SaveRow[] rows, int picked)
        {
            Paint(_realmRows, _realmRowsRoot, _realmRowTemplate, rows, picked, onPickRealm);
        }

        public void SetBoarders(RealmRegistry.SaveRow[] rows, int picked)
        {
            Paint(_boarderRows, _boarderRowsRoot, _boarderRowTemplate, rows, picked, onPickBoarder);
        }

        void Paint(List<GameObject> pool, RectTransform root, GameObject template,
            RealmRegistry.SaveRow[] rows, int picked, Action<int> onPick)
        {
            if (root == null || template == null) return;
            int n = rows != null ? rows.Length : 0;
            while (pool.Count < n)
                pool.Add(Instantiate(template, root));

            for (int i = 0; i < pool.Count; i++)
            {
                var go = pool[i];
                if (go == null) continue;
                if (i >= n)
                {
                    go.SetActive(false);
                    continue;
                }
                go.SetActive(true);
                go.transform.SetSiblingIndex(i);

                var row = rows[i];
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = row.Loadable ? row.Name : row.Name + "  (cannot be opened)";
                    label.color = !row.Loadable ? Refused : (i == picked ? Picked : Normal);
                }

                var button = go.GetComponent<Button>();
                if (button == null) continue;
                button.interactable = row.Loadable;
                int index = i; // captured per row, not per loop
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => { if (onPick != null) onPick.Invoke(index); });
            }
        }

        /// <summary>A line the player can act on: why Enter is unavailable, or what just
        /// happened. Never left holding a stale message from a previous action.</summary>
        public void SetStatus(string message)
        {
            if (_status == null) return;
            _status.text = message == null ? "" : message;
        }

        public void SetEnterEnabled(bool enabled)
        {
            if (_enterBtn != null) _enterBtn.interactable = enabled;
        }

        /// <summary>Put text in the name field. The chooser only ever clears it today, but a
        /// field you can read and not write is half a binding — and a caller that sets a name
        /// and reads it back is the only way to prove the field is wired rather than merely
        /// present in the scene.</summary>
        public void SetName(string name)
        {
            if (_nameField != null) _nameField.text = name == null ? "" : name;
        }

        public void ClearName() { SetName(""); }

        void OnDestroy()
        {
            // The rows outlive nothing, but the listeners captured this view. Dropping them
            // keeps a destroyed view from being called by a click that is already in flight.
            for (int i = 0; i < _realmRows.Count; i++)
                ClearRow(_realmRows[i]);
            for (int i = 0; i < _boarderRows.Count; i++)
                ClearRow(_boarderRows[i]);
            Log.Info("RealmSelectView", "chooser closed");
        }

        static void ClearRow(GameObject go)
        {
            if (go == null) return;
            var button = go.GetComponent<Button>();
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
