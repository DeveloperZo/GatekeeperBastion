using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeeperBastion.Tests
{
    /// <summary>In-build Gatekeeper wrappers are three distinct pack refs; enemy prefab
    /// NavMeshAgent radius/height stay the pre-mesh-slice values.</summary>
    public class GatekeeperPresentationTests
    {
        static readonly Regex GuidLine = new Regex(@"^guid:\s*([0-9a-f]+)\s*$",
            RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex AgentBlock = new Regex(
            @"--- !u!195 &\d+\s+NavMeshAgent:[\s\S]*?m_Radius:\s*([-\d.]+)[\s\S]*?m_Height:\s*([-\d.]+)",
            RegexOptions.Compiled);

        [Test]
        public void Wrappers_ExistAsThreeDistinctAssets()
        {
            string d = GuidOf("Assets/Packs/Gatekeepers/Default.prefab");
            string h = GuidOf("Assets/Packs/Gatekeepers/Hunter.prefab");
            string t = GuidOf("Assets/Packs/Gatekeepers/Thief.prefab");
            Assert.AreNotEqual(d, h, "Default and Hunter wrappers share a GUID");
            Assert.AreNotEqual(d, t, "Default and Thief wrappers share a GUID");
            Assert.AreNotEqual(h, t, "Hunter and Thief wrappers share a GUID");
        }

        [Test]
        public void EnemyPrefabs_KeepPreSliceAgentFootprint()
        {
            AssertAgent("Assets/Prefabs/EnemyGatekeeper.prefab", 0.4f, 2f);
            AssertAgent("Assets/Prefabs/HunterGatekeeper.prefab", 0.4f * 0.85f, 2f * 0.85f);
            AssertAgent("Assets/Prefabs/RamGatekeeper.prefab", 0.4f * 1.4f, 2f * 1.4f);
        }

        // Colour is the tutorial: each kind glows its own palette tint. Exact colours live in
        // SceneBuilder's palette; this gates that emission is on, per-kind, and actually worn.
        [Test]
        public void Wrappers_WearDistinctEmissionTints()
        {
            string d = AssertEmissiveMatWorn("Default");
            string h = AssertEmissiveMatWorn("Hunter");
            string t = AssertEmissiveMatWorn("Thief");
            Assert.AreNotEqual(d, h, "Default and Hunter share an emission tint");
            Assert.AreNotEqual(d, t, "Default and Thief share an emission tint");
            Assert.AreNotEqual(h, t, "Hunter and Thief share an emission tint");
        }

        static readonly Regex EmissionColorLine = new Regex(
            @"_Emission_Color:\s*\{r:\s*([-\d.]+),\s*g:\s*([-\d.]+),\s*b:\s*([-\d.]+)",
            RegexOptions.Compiled);
        static readonly Regex EmissionEnabledLine = new Regex(
            @"_Enable_Emission:\s*1\b", RegexOptions.Compiled);

        /// <summary>Asserts the kind material exists with emission on and that the kind's wrapper
        /// prefab references it; returns the emission colour triple for the distinctness check.</summary>
        static string AssertEmissiveMatWorn(string kind)
        {
            string matPath = "Assets/Packs/Gatekeepers/" + kind + ".mat";
            Assert.IsTrue(File.Exists(Path.GetFullPath(matPath)), matPath + " missing");
            string yaml = File.ReadAllText(Path.GetFullPath(matPath));
            Assert.IsTrue(EmissionEnabledLine.IsMatch(yaml), matPath + " emission not enabled");
            var m = EmissionColorLine.Match(yaml);
            Assert.IsTrue(m.Success, matPath + " has no _Emission_Color");

            string guid = GuidOf(matPath);
            string prefab = File.ReadAllText(
                Path.GetFullPath("Assets/Packs/Gatekeepers/" + kind + ".prefab"));
            Assert.IsTrue(prefab.Contains(guid), kind + ".prefab does not wear " + kind + ".mat");
            return m.Groups[1].Value + "," + m.Groups[2].Value + "," + m.Groups[3].Value;
        }

        static string GuidOf(string prefabPath)
        {
            string meta = Path.GetFullPath(prefabPath + ".meta");
            Assert.IsTrue(File.Exists(Path.GetFullPath(prefabPath)), prefabPath + " missing");
            Assert.IsTrue(File.Exists(meta), meta + " missing");
            var m = GuidLine.Match(File.ReadAllText(meta));
            Assert.IsTrue(m.Success, "guid missing in " + meta);
            return m.Groups[1].Value;
        }

        static void AssertAgent(string prefabPath, float radius, float height)
        {
            string yaml = File.ReadAllText(Path.GetFullPath(prefabPath));
            var m = AgentBlock.Match(yaml);
            Assert.IsTrue(m.Success, "NavMeshAgent block missing in " + prefabPath);
            float gotR = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            float gotH = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            Assert.AreEqual(radius, gotR, 1e-4f, prefabPath + " radius");
            Assert.AreEqual(height, gotH, 1e-4f, prefabPath + " height");
        }
    }
}
