using System.Collections.Generic;
using GatekeeperBastion.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Tests
{
    /// <summary>M35 criterion 27 at the UiBind choke point: equal values touch nothing —
    /// no text write, no slider write, no CanvasGroup write — so a steady HUD value cannot
    /// dirty its canvas. WP-1 checkpoint coverage for the change-gates.</summary>
    public class UiBindGatingTests
    {
        readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go.AddComponent<T>();
        }

        // ---------- SetText ----------

        [Test]
        public void SetText_WritesOnce_ThenGatesEqualValues()
        {
            TMP_Text label = Make<TextMeshProUGUI>("label");

            Assert.IsTrue(UiBind.SetText(label, "SALVAGE 120"), "First write must go through.");
            Assert.AreEqual("SALVAGE 120", label.text);

            Assert.IsFalse(UiBind.SetText(label, "SALVAGE 120"),
                "An equal value must not touch TMP_Text.text (criterion 27).");

            Assert.IsTrue(UiBind.SetText(label, "SALVAGE 130"), "A changed value must write.");
            Assert.AreEqual("SALVAGE 130", label.text);
        }

        [Test]
        public void SetText_TreatsNullAsEmpty_AndGatesIt()
        {
            TMP_Text label = Make<TextMeshProUGUI>("label");
            label.text = string.Empty;

            Assert.IsFalse(UiBind.SetText(label, null),
                "null → empty must gate against an already-empty label.");
            Assert.IsFalse(UiBind.SetText(null, "anything"), "A null label is a no-op.");
        }

        // ---------- SetNumber (cached per-frame formatting) ----------

        [Test]
        public void SetNumber_FormatsOnlyWhenTheValueChanges()
        {
            TMP_Text label = Make<TextMeshProUGUI>("label");
            long cache = long.MinValue;

            Assert.IsTrue(UiBind.SetNumber(label, ref cache, 120, "SALVAGE "), "First write goes through.");
            Assert.AreEqual("SALVAGE 120", label.text);
            Assert.AreEqual(120, cache);

            label.text = "tampered"; // prove the gate reads the cache, not the label
            Assert.IsFalse(UiBind.SetNumber(label, ref cache, 120, "SALVAGE "),
                "An equal value must not format or write (steady state allocates nothing).");
            Assert.AreEqual("tampered", label.text);

            Assert.IsTrue(UiBind.SetNumber(label, ref cache, 121, "SALVAGE "), "A changed value writes.");
            Assert.AreEqual("SALVAGE 121", label.text);
        }

        // ---------- SetBar ----------

        [Test]
        public void SetBar_WritesOnce_ThenGatesEqualValues()
        {
            Slider slider = Make<Slider>("bar");
            slider.interactable = true;

            Assert.IsTrue(UiBind.SetBar(slider, 40f, 100f), "First write must configure the bar.");
            Assert.IsFalse(slider.interactable, "Criterion 10: display-only bars are not interactable.");
            Assert.AreEqual(100f, slider.maxValue);
            Assert.AreEqual(40f, slider.value);

            Assert.IsFalse(UiBind.SetBar(slider, 40f, 100f),
                "Equal value and max must not touch the slider (criterion 27).");

            Assert.IsTrue(UiBind.SetBar(slider, 41f, 100f), "A changed value must write.");
            Assert.AreEqual(41f, slider.value);
        }

        // ---------- SetRelevant ----------

        [Test]
        public void SetRelevant_SetsBothStates_AndCorrectsDriftedAlpha()
        {
            CanvasGroup group = Make<CanvasGroup>("block");
            GameObject block = group.gameObject;

            UiBind.SetRelevant(block, group, relevant: false, dimAlpha: 0.35f);
            Assert.AreEqual(0.35f, group.alpha);
            Assert.IsFalse(group.interactable);
            Assert.IsFalse(group.blocksRaycasts);

            // The gate compares against live component state, so a drifted alpha under the
            // same relevance must be written back (equal state, by the same comparison,
            // is never written — see the SetText/SetBar gates for the observable proof).
            group.alpha = 0.9f;
            UiBind.SetRelevant(block, group, relevant: false, dimAlpha: 0.35f);
            Assert.AreEqual(0.35f, group.alpha, "Drifted alpha must be corrected.");

            UiBind.SetRelevant(block, group, relevant: true, dimAlpha: 0.35f);
            Assert.AreEqual(1f, group.alpha);
            Assert.IsTrue(group.interactable);
            Assert.IsTrue(group.blocksRaycasts);
        }
    }
}
