using System.Collections.Generic;
using System.Linq;
using GatekeeperBastion.Game;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GatekeeperBastion.Tests
{
    /// <summary>M35 criterion 35: the structured shortcut row API. Data side —
    /// <see cref="ShortcutPanel.FillRows"/> emits one row per binding label with grouped
    /// keycaps and dev rows appended last. View side — <see cref="ShortcutPanelView.SetRows"/>
    /// pools template clones, reusing instances across fills and deactivating (never
    /// destroying) surplus rows.</summary>
    public class ShortcutRowApiTests
    {
        readonly List<GameObject> _spawned = new List<GameObject>();
        readonly List<ShortcutRow> _rows = new List<ShortcutRow>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        // ---------- FillRows: data contract ----------

        [Test]
        public void FillRows_ClearsTheList_AndGroupsWeaponKeycapsIntoOneRow()
        {
            _rows.Clear();
            _rows.Add(new ShortcutRow("stale", "stale", "stale"));

            ShortcutPanel.FillRows(ControlContext.Fight, _rows);

            Assert.IsFalse(_rows.Any(r => r.Description == "stale"), "FillRows must clear first.");
            var weapons = _rows.Where(r => r.Description == "weapons").ToList();
            Assert.AreEqual(1, weapons.Count,
                "The three weapon-slot bindings must merge into one grouped row.");
            Assert.AreEqual("1 2 3", weapons[0].Key);
            Assert.AreEqual("FIGHT", weapons[0].Section);
        }

        [Test]
        public void FillRows_ReturnRow_MergesAllThreeConfirmKeys()
        {
            ShortcutPanel.FillRows(ControlContext.EndScreen, _rows);

            var ret = _rows.Where(r => r.Description == "return").ToList();
            Assert.AreEqual(1, ret.Count, "ENTER/SPACE/ESC confirm bindings merge into one row.");
            Assert.AreEqual("ENTER SPACE ESC", ret[0].Key);
        }

        [Test]
        public void FillRows_JuiceRow_CarriesIntensity()
        {
            ShortcutPanel.FillRows(ControlContext.Prep, _rows, dialLevel: 2);

            var juice = _rows.Where(r => r.Key == "+ -").ToList();
            Assert.AreEqual(1, juice.Count, "The two juice bindings must merge into '+ -'.");
            Assert.AreEqual("Intensity 2", juice[0].Description);
        }

        [Test]
        public void FillRows_DevRows_AreFlaggedAndAppendedAfterEveryGameplayRow()
        {
            // UNITY_EDITOR is defined in EditMode, so the dev section must be present.
            ShortcutPanel.FillRows(ControlContext.Fight, _rows);

            int firstDev = _rows.FindIndex(r => r.Dev);
            Assert.GreaterOrEqual(firstDev, 1, "Dev rows exist in the editor and never lead.");
            for (int i = firstDev; i < _rows.Count; i++)
            {
                Assert.IsTrue(_rows[i].Dev, "Dev rows are appended last, never interleaved.");
                Assert.AreEqual("DEV", _rows[i].Section);
                Assert.IsNotEmpty(_rows[i].Key, "Every dev row carries its keycap.");
            }
        }

        // ---------- SetRows: pooled view ----------

        ShortcutPanelView MakeView(out RectTransform rowsRoot, out GameObject template)
        {
            var panelGo = new GameObject("panel", typeof(RectTransform));
            _spawned.Add(panelGo);
            var view = panelGo.AddComponent<ShortcutPanelView>();

            rowsRoot = new GameObject("rows", typeof(RectTransform)).GetComponent<RectTransform>();
            rowsRoot.SetParent(panelGo.transform, false);

            template = new GameObject("row template", typeof(RectTransform));
            template.transform.SetParent(rowsRoot, false);
            var key = new GameObject("key", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            key.transform.SetParent(template.transform, false);
            var desc = new GameObject("desc", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            desc.transform.SetParent(template.transform, false);

            var so = new SerializedObject(view);
            so.FindProperty("_panel").objectReferenceValue = panelGo;
            so.FindProperty("_linesRoot").objectReferenceValue = rowsRoot;
            so.FindProperty("_rowsRoot").objectReferenceValue = rowsRoot;
            so.FindProperty("_rowTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static List<GameObject> ActiveClones(RectTransform rowsRoot, GameObject template)
        {
            var clones = new List<GameObject>();
            foreach (Transform child in rowsRoot)
                if (child.gameObject != template && child.gameObject.activeSelf)
                    clones.Add(child.gameObject);
            return clones;
        }

        [Test]
        public void SetRows_ClonesTemplatePerRow_AndWritesKeyAndDescription()
        {
            ShortcutPanelView view = MakeView(out RectTransform rowsRoot, out GameObject template);

            view.SetRows(new[]
            {
                new ShortcutRow("E", "interact", "MOVE"),
                new ShortcutRow("1 2 3", "weapons", "FIGHT"),
            });

            Assert.IsFalse(template.activeSelf, "The template itself stays inactive.");
            List<GameObject> clones = ActiveClones(rowsRoot, template);
            Assert.AreEqual(2, clones.Count);

            var labels0 = clones[0].GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.AreEqual("E", labels0[0].text);
            Assert.AreEqual("interact", labels0[1].text);
            var labels1 = clones[1].GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.AreEqual("1 2 3", labels1[0].text);
            Assert.AreEqual("weapons", labels1[1].text);
        }

        [Test]
        public void SetRows_ShortcutButtonTemplate_OverridesPrefabEscExitDefaults()
        {
            // Mirrors Ship's Row Template: Keycap(ShortcutButton ESC/EXIT) + Description sibling.
            // Regression: first-two-TMP binding wrote into the keycap, then OnEnable restored EXIT.
            var panelGo = new GameObject("panel", typeof(RectTransform));
            _spawned.Add(panelGo);
            var view = panelGo.AddComponent<ShortcutPanelView>();

            var rowsRoot = new GameObject("rows", typeof(RectTransform)).GetComponent<RectTransform>();
            rowsRoot.SetParent(panelGo.transform, false);

            var template = new GameObject("row template", typeof(RectTransform));
            template.transform.SetParent(rowsRoot, false);

            var keycap = new GameObject("Keycap", typeof(RectTransform));
            keycap.transform.SetParent(template.transform, false);
            var shortcut = keycap.AddComponent<Michsky.UI.Shift.ShortcutButton>();
            shortcut.keyText = "ESC";
            shortcut.buttonText = "EXIT";
            shortcut.normalKeyText = new GameObject("key", typeof(RectTransform))
                .AddComponent<TextMeshProUGUI>();
            shortcut.normalKeyText.transform.SetParent(keycap.transform, false);
            shortcut.normalKeyText.text = "ESC";
            shortcut.normalText = new GameObject("action", typeof(RectTransform))
                .AddComponent<TextMeshProUGUI>();
            shortcut.normalText.transform.SetParent(keycap.transform, false);
            shortcut.normalText.text = "EXIT";

            var descGo = new GameObject("Description", typeof(RectTransform));
            descGo.transform.SetParent(template.transform, false);
            var desc = descGo.AddComponent<TextMeshProUGUI>();

            var so = new SerializedObject(view);
            so.FindProperty("_panel").objectReferenceValue = panelGo;
            so.FindProperty("_linesRoot").objectReferenceValue = rowsRoot;
            so.FindProperty("_rowsRoot").objectReferenceValue = rowsRoot;
            so.FindProperty("_rowTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.SetRows(new[] { new ShortcutRow("WASD", "move", "MOVE") });

            List<GameObject> clones = ActiveClones(rowsRoot, template);
            Assert.AreEqual(1, clones.Count);
            var cloneBtn = clones[0].GetComponentInChildren<Michsky.UI.Shift.ShortcutButton>(true);
            Assert.IsNotNull(cloneBtn);
            Assert.AreEqual("WASD", cloneBtn.keyText);
            Assert.AreEqual("WASD", cloneBtn.normalKeyText.text);
            Assert.AreEqual("", cloneBtn.buttonText);
            Assert.IsFalse(cloneBtn.normalText.gameObject.activeSelf,
                "Keycap action label stays hidden; description is the sibling.");
            var cloneDesc = clones[0].transform.Find("Description")
                .GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("move", cloneDesc.text);
        }

        [Test]
        public void SetRows_ReusesPooledClones_AndDeactivatesSurplus()
        {
            ShortcutPanelView view = MakeView(out RectTransform rowsRoot, out GameObject template);

            view.SetRows(new[]
            {
                new ShortcutRow("A", "a", "S"),
                new ShortcutRow("B", "b", "S"),
                new ShortcutRow("C", "c", "S"),
            });
            List<GameObject> first = ActiveClones(rowsRoot, template);
            Assert.AreEqual(3, first.Count);
            var firstIds = first.Select(go => go.GetInstanceID()).ToHashSet();

            view.SetRows(new[] { new ShortcutRow("D", "d", "S") });
            List<GameObject> second = ActiveClones(rowsRoot, template);
            Assert.AreEqual(1, second.Count, "Surplus rows deactivate; they are not destroyed.");
            Assert.IsTrue(firstIds.Contains(second[0].GetInstanceID()),
                "The surviving row is a reused pool instance, not a fresh Instantiate.");
            Assert.AreEqual(4, rowsRoot.childCount,
                "Pool keeps template + 3 clones alive across fills (3 + 1 template = 4).");
        }
    }
}
