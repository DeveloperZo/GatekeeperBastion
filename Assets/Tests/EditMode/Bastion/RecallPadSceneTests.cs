using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Field-terminal recall pads: Ship teleporter graybox (Pad/Emitter/Glass) with
    /// <c>HatchRecallPad</c>, not a runtime mesh and not the dock <c>HatchPortal</c>.</summary>
    public class RecallPadSceneTests
    {
        const string ScenePath = "Assets/Scenes/Bastion_Coop.unity";
        const string RecallScript = "GatekeepersProtocol.Game.HatchRecallPad";

        [Test]
        public void FieldTerminals_CarryRecallPads_NotDockPortals()
        {
            Assert.IsTrue(File.Exists(ScenePath), ScenePath + " is missing");
            string scene = File.ReadAllText(ScenePath);
            Assert.IsTrue(Regex.IsMatch(scene, @"m_Name: T2 Portal"), "T2 Portal missing");
            Assert.IsTrue(Regex.IsMatch(scene, @"m_Name: T5 Portal"), "T5 Portal missing");

            int recalls = Regex.Matches(scene, Regex.Escape(RecallScript)).Count;
            Assert.AreEqual(2, recalls,
                "T2 and T5 each need HatchRecallPad (Ship teleporter graybox → Hatch)");

            // No HatchPortal on those two roots — dock stays Hatch-only.
            Match t2 = Regex.Match(scene,
                @"m_Name: T2 Portal[\s\S]{0,800}?m_EditorClassIdentifier: ([^\r\n]+)");
            Match t5 = Regex.Match(scene,
                @"m_Name: T5 Portal[\s\S]{0,800}?m_EditorClassIdentifier: ([^\r\n]+)");
            Assert.IsTrue(t2.Success && t5.Success);
            StringAssert.Contains("HatchRecallPad", t2.Groups[1].Value);
            StringAssert.Contains("HatchRecallPad", t5.Groups[1].Value);
            StringAssert.DoesNotContain("HatchPortal", t2.Groups[1].Value);
            StringAssert.DoesNotContain("HatchPortal", t5.Groups[1].Value);
        }

        [Test]
        public void RecallLitParts_ReferenceGameObjects()
        {
            string scene = File.ReadAllText(ScenePath);
            foreach (Match at in Regex.Matches(scene, Regex.Escape(RecallScript)))
            {
                Match block = Regex.Match(scene.Substring(at.Index),
                    @"litParts:\n((?:  - \{fileID: \d+\}\n)+)");
                Assert.IsTrue(block.Success, "HatchRecallPad missing litParts");
                foreach (Match id in Regex.Matches(block.Groups[1].Value, @"fileID: (\d+)"))
                {
                    Match m = Regex.Match(scene, @"^--- !u!(\d+) &" + id.Groups[1].Value + "$",
                        RegexOptions.Multiline);
                    Assert.IsTrue(m.Success);
                    Assert.AreEqual("1", m.Groups[1].Value,
                        "recall litParts must be GameObject ids, not Transforms");
                }
            }
        }
    }
}
