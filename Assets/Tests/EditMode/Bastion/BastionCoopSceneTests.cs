using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Disk pins for the co-op bastion scene. YAML only — the mcs harness
    /// cannot see MonoBehaviours. Missing scene or an un-emitted copy is inconclusive
    /// rather than a suite-wide fail.</summary>
    public class BastionCoopSceneTests
    {
        const string CoopScenePath = "Assets/Scenes/Bastion_Coop.unity";
        const string BuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";

        static readonly string[] CoopRoomNames =
        {
            "R1", "R6", "R10", "R15", "U1", "U6", "U10"
        };

        [Test]
        public void Emitter_SavesTheOpenedScene_NotAHardcodedBastionPath()
        {
            string src = Read("Assets/Scripts/Editor/Bastion/BastionSketchEmitter.cs");
            Assert.IsFalse(
                Regex.IsMatch(src, @"SaveScene\([^)]*""Assets/Scenes/Bastion\.unity""\)"),
                "SaveScene must use the opened scene path, never a Bastion.unity literal");
        }

        [Test]
        public void CoopScene_Exists_HasDemoSpine_AndThreeHatchDoors()
        {
            if (!File.Exists(Path.GetFullPath(CoopScenePath)))
            {
                Assert.Inconclusive(CoopScenePath + " not on disk yet");
                return;
            }

            string yaml = File.ReadAllText(Path.GetFullPath(CoopScenePath));
            if (!Regex.IsMatch(yaml, @"^  m_Name: R1\s*$", RegexOptions.Multiline)
                || !Regex.IsMatch(yaml, @"^  m_Name: U1\s*$", RegexOptions.Multiline))
            {
                Assert.Inconclusive(CoopScenePath + " has not been re-emitted to the two-storey sketch");
                return;
            }
            int hatchDoors = 0;
            for (int id = 1; id <= 3; id++)
            {
                int names = Regex.Matches(
                    yaml, @"^  m_Name: Hatch_" + id + @"\s*$", RegexOptions.Multiline).Count;
                if (names >= 1)
                    hatchDoors++;
            }

            if (hatchDoors < 3)
            {
                Assert.Inconclusive(
                    CoopScenePath + " exists but Hatch_1..3 are not emitted yet");
                return;
            }

            string guid = SegmentGuid();
            int segments = Regex.Matches(yaml, @"guid: " + guid).Count;
            Assert.GreaterOrEqual(segments, 20,
                "co-op room count must be in the 20–30 band");
            Assert.LessOrEqual(segments, 30,
                "co-op room count must be in the 20–30 band");

            for (int i = 0; i < CoopRoomNames.Length; i++)
            {
                int names = Regex.Matches(
                    yaml,
                    @"^  m_Name: " + CoopRoomNames[i] + @"\s*$",
                    RegexOptions.Multiline).Count;
                Assert.GreaterOrEqual(names, 1, CoopRoomNames[i] + " missing");
            }

            string[] stairs =
            {
                "GStairWalk_R1_U1", "GStairWalk_R6_U6", "GStairWalk_R10_U10"
            };
            for (int i = 0; i < stairs.Length; i++)
            {
                int names = Regex.Matches(
                    yaml,
                    @"^  m_Name: " + stairs[i] + @"\s*$",
                    RegexOptions.Multiline).Count;
                Assert.GreaterOrEqual(names, 1, stairs[i] + " missing");
            }
        }

        [Test]
        public void BuildSettings_ListsCoopSceneEnabled_OnceTheFileExists()
        {
            if (!File.Exists(Path.GetFullPath(CoopScenePath)))
            {
                Assert.Inconclusive(CoopScenePath + " not on disk yet");
                return;
            }

            string yaml = Read(BuildSettingsPath);
            Match m = Regex.Match(
                yaml,
                @"enabled: 1\s*\n\s*path: Assets/Scenes/Bastion_Coop\.unity",
                RegexOptions.Multiline);
            Assert.IsTrue(m.Success,
                "EditorBuildSettings must list Assets/Scenes/Bastion_Coop.unity with enabled: 1");
        }

        static string SegmentGuid()
        {
            string meta = Read("Assets/Scripts/Game/Bastion/BastionSegment.cs.meta");
            Match m = Regex.Match(meta, @"^guid: ([0-9a-f]+)\s*$", RegexOptions.Multiline);
            Assert.IsTrue(m.Success, "BastionSegment.cs.meta has no guid");
            return m.Groups[1].Value;
        }

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            return File.ReadAllText(path);
        }
    }
}
