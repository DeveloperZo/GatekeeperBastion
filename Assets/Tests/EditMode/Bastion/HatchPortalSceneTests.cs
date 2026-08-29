using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The Hatch portal, as the SCENE authors it. Both assertions below exist because
    /// the portal broke the Hatch on 2026-08-28 and nothing in the suite noticed.
    ///
    /// <para>WHAT HAPPENED. The portal was injected beside the Hatch with its
    /// <c>litParts</c> array pointing at the visuals' TRANSFORM ids while the field is a
    /// <c>GameObject[]</c>. Unity resolved every entry to null, so the component's
    /// hide-on-Awake did nothing, and the pad, emitter and glass stood active — with colliders
    /// — four metres from the Hatch from the moment the scene loaded. Interactor resolves
    /// through <c>GetComponentInParent</c>, so looking at the Hatch could return the PORTAL,
    /// whose prompt is null while it is shut. The reported symptom was "I could not trigger a
    /// start at hatch": the start line was intact and simply unreachable.</para>
    ///
    /// <para>WHY IT IS PINNED HERE AND NOT IN PLAYMODE. A wrong-type fileID is a serialization
    /// fact, not a runtime one — it produces no error, no warning and no exception, only a
    /// silently empty array. Reading the YAML is the only place it is visible at all, and it
    /// costs milliseconds. The offline harness compiles this file, so the guard runs on every
    /// change rather than only when someone opens Unity.</para></summary>
    public class HatchPortalSceneTests
    {
        const string ScenePath = "Assets/Scenes/Bastion_Coop.unity";
        const string PortalScript = "GatekeepersProtocol.Game.HatchPortal";

        /// <summary>One at the Hatch, one beside each field terminal (T2, T5). The Master
        /// terminal has none: its own switch ends the Run, and docking is refused for the whole
        /// finale, so a portal there could only ever be dark.</summary>
        const int PortalCount = 3;

        static string Scene()
        {
            Assert.IsTrue(File.Exists(ScenePath), ScenePath + " is missing");
            return File.ReadAllText(ScenePath);
        }

        /// <summary>The document whose anchor is <paramref name="anchor"/>, or null.</summary>
        static string Document(string scene, string anchor)
        {
            Match m = Regex.Match(scene, @"^--- !u!(\d+) &" + anchor + "$", RegexOptions.Multiline);
            if (!m.Success) return null;
            int next = scene.IndexOf("\n--- !u!", m.Index + 5, StringComparison.Ordinal);
            return next < 0 ? scene.Substring(m.Index) : scene.Substring(m.Index, next - m.Index);
        }

        /// <summary>Every litParts id on EVERY portal in the scene. Written as "every" on
        /// purpose: this checked only the FIRST match until portals were added beside T2 and
        /// T5 on 2026-08-28, at which point a first-match test would have gone on passing while
        /// two thirds of them were mis-wired.</summary>
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
                "the scene has " + found + " portals, not " + PortalCount + ". The way home is "
                + "offered at the Hatch and beside each field terminal; if that set changes, "
                + "this number is the deliberate record of the change.");
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
                // !u!1 is GameObject; !u!4 is Transform. The field is GameObject[], and Unity
                // resolves a Transform id in it to NULL rather than complaining.
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
                // Serialized state outranks a code default (Docs/ development rules): relying
                // on Awake to hide these leaves them live for the frames before it runs, and
                // live forever if the component fails to resolve. The scene says dark.
                Assert.AreEqual("0", active.Groups[1].Value,
                    "a portal visual is active in the saved scene. The portal must be dark "
                    + "until a zone closes, and it must be dark BY AUTHORING, not by script.");
            }
        }
    }
}
