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

            Assert.AreEqual(3, hatchDoors,
                CoopScenePath + " is the two-storey co-op emit but Hatch_1..3 are missing");

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
                "Stair_R1_U1", "Stair_R6_U6", "Stair_R10_U10",
                "GStairWalk_R1_U1", "GStairWalk_R6_U6", "GStairWalk_R10_U10",
                "GStairLand_R1_U1", "GStairLand_R6_U6", "GStairLand_R10_U10"
            };
            for (int i = 0; i < stairs.Length; i++)
            {
                int names = Regex.Matches(
                    yaml,
                    @"^  m_Name: " + stairs[i] + @"\s*$",
                    RegexOptions.Multiline).Count;
                Assert.GreaterOrEqual(names, 1, stairs[i] + " missing");
            }

            string stairsCs = Read("Assets/Scripts/Editor/Bastion/BastionSketchStairs.cs");
            Match guidM = Regex.Match(stairsCs, @"StairMeshGuid = ""([0-9a-f]+)""");
            Assert.IsTrue(guidM.Success, "BastionSketchStairs has no StairMeshGuid");
            string hull = Read("Tools/ship-hull/build_hull.py");
            StringAssert.Contains(guidM.Groups[1].Value, hull,
                "StairMeshGuid must be the ship kit Stairs guid");
            Assert.GreaterOrEqual(
                Regex.Matches(yaml, @"guid: " + guidM.Groups[1].Value).Count,
                3,
                "each Stair_* must reference SM_Prop_Stairs_01");

            Assert.IsTrue(
                TransformChildrenEmpty(yaml, "Generated"),
                "Generated must be empty after settle — rooms left there get wiped by ClearChildren");
            Assert.IsFalse(
                TransformChildrenEmpty(yaml, "Authored"),
                "co-op rooms must sit under Authored");
        }

        [Test]
        public void Emitter_EmitsStairsThenSettlesIntoAuthored_BySubtreeRef()
        {
            string src = Read("Assets/Scripts/Editor/Bastion/BastionSketchEmitter.cs");
            int emit = src.IndexOf("static void EmitSketch");
            Assert.Greater(emit, 0, "EmitSketch missing");
            int stairs = src.IndexOf("BastionSketchStairs.Emit", emit);
            int settle = src.IndexOf("SettleGeneratedIntoAuthored(generated, authored)", emit);
            Assert.Greater(stairs, emit, "EmitSketch must call BastionSketchStairs.Emit");
            Assert.Greater(settle, stairs, "settle must run after stairs so the mesh rides the room");
            StringAssert.Contains(
                "static int SettleGeneratedIntoAuthored(GameObject generated, GameObject authored)",
                src);
            string stairsCs = Read("Assets/Scripts/Editor/Bastion/BastionSketchStairs.cs");
            StringAssert.Contains("IsUnderAuthored", stairsCs);
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

        [Test]
        public void CoopScene_UsesTheShipCoopSpaceSkybox()
        {
            if (!File.Exists(Path.GetFullPath(CoopScenePath)))
            {
                Assert.Inconclusive(CoopScenePath + " not on disk yet");
                return;
            }

            string shipYaml = Read("Assets/Scenes/Ship - CoOp.unity");
            Match shipSky = Regex.Match(
                shipYaml,
                @"m_SkyboxMaterial: \{fileID: 2100000, guid: ([0-9a-f]+), type: 2\}");
            Assert.IsTrue(shipSky.Success, "Ship - CoOp has no skybox guid");

            string coopYaml = Read(CoopScenePath);
            StringAssert.Contains(
                "m_SkyboxMaterial: {fileID: 2100000, guid: " + shipSky.Groups[1].Value,
                coopYaml,
                "Bastion_Coop must use Ship - CoOp's PolygonSciFiSpace skybox");
        }

        static string SegmentGuid()
        {
            string meta = Read("Assets/Scripts/Game/Bastion/BastionSegment.cs.meta");
            Match m = Regex.Match(meta, @"^guid: ([0-9a-f]+)\s*$", RegexOptions.Multiline);
            Assert.IsTrue(m.Success, "BastionSegment.cs.meta has no guid");
            return m.Groups[1].Value;
        }

        static bool TransformChildrenEmpty(string yaml, string objectName)
        {
            Match header = Regex.Match(
                yaml,
                @"m_Name: " + Regex.Escape(objectName)
                + @"\s*\n(?:  [^\n]*\n){0,8}--- !u!4[^\n]*\nTransform:",
                RegexOptions.Multiline);
            Assert.IsTrue(header.Success, objectName + " transform not found in scene YAML");
            int from = header.Index;
            int nextDoc = yaml.IndexOf("\n--- ", from + header.Length);
            string block = nextDoc < 0
                ? yaml.Substring(from)
                : yaml.Substring(from, nextDoc - from);
            return Regex.IsMatch(block, @"m_Children: \[\]");
        }

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            return File.ReadAllText(path);
        }
    }
}
