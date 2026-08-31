using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The Hatch dock portal (way home) as the scene authors it. Terminal recall
    /// pads are <see cref="RecallPadSceneTests"/> — same graybox, different verb.</summary>
    public class HatchPortalSceneTests
    {
        const string ScenePath = "Assets/Scenes/Bastion_Coop.unity";
        const string PortalScript = "GatekeepersProtocol.Game.HatchPortal";

        /// <summary>One at the Hatch only. T2/T5 carry HatchRecallPad (return to Hatch), not
        /// the dock verb.</summary>
        const int PortalCount = 1;

        static string Scene()
        {
            Assert.IsTrue(File.Exists(ScenePath), ScenePath + " is missing");
            return File.ReadAllText(ScenePath);
        }

        static string Document(string scene, string anchor)
        {
            Match m = Regex.Match(scene, @"^--- !u!(\d+) &" + anchor + "$", RegexOptions.Multiline);
            if (!m.Success) return null;
            int next = scene.IndexOf("\n--- !u!", m.Index + 5, StringComparison.Ordinal);
            return next < 0 ? scene.Substring(m.Index) : scene.Substring(m.Index, next - m.Index);
        }

        static string[] LitParts(string scene)
        {
            var all = new System.Collections.Generic.List<string>();
            int found = 0;
            foreach (Match at in Regex.Matches(scene, Regex.Escape(PortalScript)))
            {
                found++;
                Match block = Regex.Match(scene.Substring(at.Index),
                    @"litParts:\n((?:  - \{fileID: \d+\}\n)+)");
                Assert.IsTrue(block.Success,
                    "a HatchPortal has no litParts entries, so nothing about it ever lights");
                foreach (Match id in Regex.Matches(block.Groups[1].Value, @"fileID: (\d+)"))
                    all.Add(id.Groups[1].Value);
            }
            Assert.Greater(found, 0, "the Bastion scene has no HatchPortal — the way home is gone");
            Assert.AreEqual(PortalCount, found,
                "the scene has " + found + " HatchPortals, not " + PortalCount
                + ". Dock is Hatch-only; field terminals use HatchRecallPad.");
            return all.ToArray();
        }

        [Test]
        public void LitParts_ReferenceGameObjects_NotTransforms()
        {
            string scene = Scene();
            string[] ids = LitParts(scene);
            Assert.Greater(ids.Length, 0, "litParts is empty");

            foreach (string id in ids)
            {
                string doc = Document(scene, id);
                Assert.IsNotNull(doc, "litParts points at " + id + ", which is not in the scene");
                StringAssert.StartsWith("--- !u!1 ", doc,
                    "litParts entry " + id + " is not a GameObject. Unity will read it as null, "
                    + "the portal will never hide itself, and its colliders will sit next to "
                    + "the Hatch stealing the interactable.");
            }
        }

        [Test]
        public void TheVisuals_AreDormantInTheScene_NotOnlyHiddenByAwake()
        {
            string scene = Scene();
            foreach (string id in LitParts(scene))
            {
                string doc = Document(scene, id);
                Match active = Regex.Match(doc, @"m_IsActive: (\d)");
                Assert.IsTrue(active.Success, "no m_IsActive on litPart " + id);
                Assert.AreEqual("0", active.Groups[1].Value,
                    "a portal visual is active in the saved scene. The portal must be dark "
                    + "until a zone closes, and it must be dark BY AUTHORING, not by script.");
            }
        }
    }
}
