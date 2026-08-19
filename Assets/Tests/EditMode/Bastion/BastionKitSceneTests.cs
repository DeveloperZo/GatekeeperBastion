using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Authored Bastion kit is unique scene geometry. A YAML drop below
    /// 15 named rooms is the same wreck that orphan-cleanup and Rebuild have
    /// produced twice — this test is the gate that commit could not see.</summary>
    public class BastionKitSceneTests
    {
        const int AuthoredRooms = 15;
        const int MaxPrefabInstances = 40;

        [Test]
        public void BastionScene_HasFifteenNamedAuthoredRooms()
        {
            string yaml = Read("Assets/Scenes/Bastion.unity");
            string guid = SegmentGuid();

            int segments = Regex.Matches(yaml, @"guid: " + guid).Count;
            Assert.AreEqual(AuthoredRooms, segments,
                "BastionSegment count — orphan cleanup or Rebuild stripped rooms");

            for (int i = 1; i <= AuthoredRooms; i++)
            {
                int names = Regex.Matches(
                    yaml, @"^  m_Name: R" + i + @"\s*$", RegexOptions.Multiline).Count;
                Assert.AreEqual(1, names, "room name R" + i);
            }

            int packed = Regex.Matches(yaml, @"^PrefabInstance:", RegexOptions.Multiline).Count;
            Assert.LessOrEqual(packed, MaxPrefabInstances,
                "packed prefab instances — Rebuild re-stamped kit pieces");

            string[] closets =
            {
                "Spawn_S1", "Spawn_S2", "Spawn_S3", "Spawn_S4", "Spawn_S5", "Spawn_S6",
                "Spawn_ControlW", "Spawn_ControlE"
            };
            for (int i = 0; i < closets.Length; i++)
            {
                Assert.GreaterOrEqual(
                    Regex.Matches(
                        yaml,
                        @"(m_Name|value): " + closets[i] + @"\s*$",
                        RegexOptions.Multiline).Count,
                    1,
                    closets[i] + " missing — spawn closets were wiped");
            }
        }

        [Test]
        public void NetShipWait_SourcePinsBastionViewHidden()
        {
            string wait = Read("Assets/Scripts/Game/Net/NetShipWait.cs");
            Assert.IsTrue(wait.IndexOf("ship wait — Bastion view hidden",
                System.StringComparison.Ordinal) >= 0);
            string client = Read("Assets/Scripts/Game/Bastion/GameController.Client.cs");
            Assert.IsTrue(client.IndexOf("NetShipWait.IsActive",
                System.StringComparison.Ordinal) >= 0);
            string fpc = Read("Assets/Scripts/Game/Player/FirstPersonController.cs");
            Assert.IsTrue(fpc.IndexOf("!isActiveAndEnabled", System.StringComparison.Ordinal) >= 0);
            Assert.IsTrue(fpc.IndexOf("!_cc.enabled", System.StringComparison.Ordinal) >= 0);
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
