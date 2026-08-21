using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
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

        /// <summary>Footprints are the authored spread (1 : 0.85 : 1.4) times SceneBuilder's
        /// BodyScale. The spread is the design and does not move; BodyScale is one number and
        /// moved once, 1.0 → 0.75 (Zo, 2026-08-20), to give the finale elite room to tower.</summary>
        [Test]
        public void EnemyPrefabs_KeepTheAuthoredAgentFootprint()
        {
            const float body = 0.75f;
            AssertAgent("Assets/Prefabs/EnemyGatekeeper.prefab", 0.4f * body, 2f * body);
            AssertAgent("Assets/Prefabs/HunterGatekeeper.prefab", 0.4f * 0.85f * body, 2f * 0.85f * body);
            AssertAgent("Assets/Prefabs/ThiefGatekeeper.prefab", 0.4f * 1.4f * body, 2f * 1.4f * body);
        }

        /// <summary>The rule those numbers serve, stated once so a future rescale cannot slip
        /// past by moving all three literals together: no kind is baked wider than the carve the
        /// Bastion navmesh was built with, because a gatekeeper wider than the carve wedges in
        /// the doorway it is walking at.</summary>
        [Test]
        public void EnemyPrefabs_StayInsideTheBakedAgentRadius()
        {
            const float bakedAgentRadius = 0.5f; // ProjectSettings/NavMeshAreas.asset
            AssertAgentRadiusUnder("Assets/Prefabs/EnemyGatekeeper.prefab", bakedAgentRadius);
            AssertAgentRadiusUnder("Assets/Prefabs/HunterGatekeeper.prefab", bakedAgentRadius);
            AssertAgentRadiusUnder("Assets/Prefabs/ThiefGatekeeper.prefab", bakedAgentRadius);
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

        // The rule gated here has not moved: emission on, per kind, distinct, and worn by the
        // wrapper. Only the spelling has, three times, which is why this matches a set rather
        // than one literal — Synty's graph exposes _Enable_Emission and _Emission_Color, Built-In
        // Standard uses the _EMISSION keyword with _EmissionColor, and the robot pack's graph
        // uses _EmissiveStrength with _EmissiveColor. Matching all three keeps the gate honest
        // through a shader swap instead of failing on the day the material changes family.
        static readonly Regex EmissionColorLine = new Regex(
            @"_Emissi(?:on|ve)_?Color:\s*\{r:\s*([-\d.]+),\s*g:\s*([-\d.]+),\s*b:\s*([-\d.]+)",
            RegexOptions.Compiled);
        static readonly Regex EmissionEnabledLine = new Regex(
            @"_Enable_Emission:\s*1\b|^\s*-\s*_EMISSION\s*$|_EmissiveStrength:\s*(?!0+(?:\.0+)?\s*$)[\d.]+",
            RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>Asserts the kind material exists with emission on and that the kind's wrapper
        /// prefab references it; returns the emission colour triple for the distinctness check.</summary>
        static string AssertEmissiveMatWorn(string kind)
        {
            string matPath = "Assets/Packs/Gatekeepers/" + kind + ".mat";
            Assert.IsTrue(File.Exists(Path.GetFullPath(matPath)), matPath + " missing");
            string yaml = File.ReadAllText(Path.GetFullPath(matPath));
            Assert.IsTrue(EmissionEnabledLine.IsMatch(yaml), matPath + " emission not enabled");
            var m = EmissionColorLine.Match(yaml);
            Assert.IsTrue(m.Success, matPath + " has no emission colour");

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

        static void AssertAgentRadiusUnder(string prefabPath, float bakedAgentRadius)
        {
            string yaml = File.ReadAllText(Path.GetFullPath(prefabPath));
            var m = AgentBlock.Match(yaml);
            Assert.IsTrue(m.Success, "NavMeshAgent block missing in " + prefabPath);
            float gotR = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.LessOrEqual(gotR, bakedAgentRadius,
                prefabPath + " is baked wider than the navmesh carve (" + bakedAgentRadius +
                "); it will wedge rather than path.");
        }
    }
}
