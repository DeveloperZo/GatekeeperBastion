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

        /// <summary>The spine, end to end (Zo, 2026-08-25): R1 OpenArea carrying the three
        /// hatches, R2..R8 the lane, R9 the Control Room. Every room is named rather than
        /// sampled — a missing middle room is exactly what a spot check walks past.</summary>
        static readonly string[] CoopRoomNames =
        {
            "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9"
        };

        /// <summary>One segment per room. Pinned exact rather than banded: the spine is
        /// authored in Tools/bastion-sketch/bastion-coop-layout.txt and its room count is a
        /// design decision, so a scene that grew or lost one should fail here and be read.</summary>
        const int SpineSegments = 9;

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

            // THE OPT-OUT THAT ATE THIS TEST. It used to bail Inconclusive unless the scene
            // still held the two-storey emit's U-rooms — so the moment the Bastion became a
            // single-storey spine (Zo, 2026-08-25) the whole scene contract stopped running,
            // and reported itself "inconclusive" in every gate for a day while the spine, the
            // hatches and the room count went unchecked. A guard that turns a broken contract
            // into a quiet skip is worse than no guard: nothing is red, and nothing is covered.
            string yaml = File.ReadAllText(Path.GetFullPath(CoopScenePath));
            Assert.IsTrue(Regex.IsMatch(yaml, @"^  m_Name: R1\s*$", RegexOptions.Multiline),
                CoopScenePath + " has no R1 — the sketch has never been emitted into it.");
            int hatchDoors = 0;
            for (int id = 1; id <= 3; id++)
            {
                int names = Regex.Matches(
                    yaml, @"^  m_Name: Hatch_" + id + @"\s*$", RegexOptions.Multiline).Count;
                if (names >= 1)
                    hatchDoors++;
            }

            Assert.AreEqual(3, hatchDoors,
                CoopScenePath + " has lost one of Hatch_1..3 — every wave begins at the Hatch "
                + "(Docs/Design/0-Gameplay.md), so this is the start line itself.");

            string guid = SegmentGuid();
            int segments = Regex.Matches(yaml, @"guid: " + guid).Count;
            Assert.AreEqual(SpineSegments, segments,
                "the spine is " + SpineSegments + " rooms, R1 through R9. The 20-30 band this "
                + "once allowed belonged to the sprawl the spine replaced.");

            for (int i = 0; i < CoopRoomNames.Length; i++)
            {
                int names = Regex.Matches(
                    yaml,
                    @"^  m_Name: " + CoopRoomNames[i] + @"\s*$",
                    RegexOptions.Multiline).Count;
                Assert.GreaterOrEqual(names, 1, CoopRoomNames[i] + " missing");
            }

            // NO STAIRS. The Bastion is a single-storey spine and the walk up the LANE is the
            // pressure (Docs/Design/0-Gameplay.md), so a flight appearing here would be a
            // second storey arriving by accident. Checked against the YAML rather than the
            // live scene so it holds even for a stair authored inactive — the same invariant
            // BastionCoopStairWalkTests pins from the other side.
            foreach (Match sm in Regex.Matches(yaml, @"^  m_Name: (\S+)\s*$", RegexOptions.Multiline))
            {
                string nm = sm.Groups[1].Value;
                Assert.IsFalse(
                    nm.StartsWith("Stair_") || nm.StartsWith("GStairWalk_")
                        || nm.StartsWith("GStairLand_"),
                    nm + " is a stair, and the Bastion is single-storey (Zo, 2026-08-25).");
            }

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
