using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Sim;
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
        // The ROOT Transform, and only it: anchored on m_Father being fileID 0 and fenced off
        // from crossing a `---` document separator, so the visual child — deliberately left at
        // localScale 1 so the elite can scale it — cannot answer for the prefab's body size.
        static readonly Regex RootScaleBlock = new Regex(
            @"^Transform:(?:(?!^---)[\s\S])*?m_LocalScale:\s*\{x:\s*([-\d.eE+]+)" +
            @"(?:(?!^---)[\s\S])*?m_Father:\s*\{fileID:\s*0\}",
            RegexOptions.Multiline | RegexOptions.Compiled);

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

        /// <summary>Footprints are the authored spread (Default 1 : Hunter 0.85 : Thief 1.2 :
        /// Cloaker 1) times SceneBuilder's BodyScale. BodyScale is one number and it has moved
        /// twice: 1.0 → 0.75 (Zo, 2026-08-20) so the finale elite would tower, which made four
        /// kinds pay for one, and back to 1.0 now that the elite carries its own tell on
        /// MasterControl.FinaleEliteScale. The spread came back with it except for the Thief,
        /// which stops at 1.2 rather than its authored 1.4: at BodyScale 1.0 a 1.4 Thief bakes
        /// 0.4 × 1.4 = 0.56 of agent radius onto a navmesh carved at 0.5, and wedges. Cloaker and
        /// Default share a footprint on purpose — the Cloaker's tell is the cloak, not the size.</summary>
        [Test]
        public void EnemyPrefabs_KeepTheAuthoredAgentFootprint()
        {
            const float body = 1f;
            AssertAgent("Assets/Prefabs/EnemyGatekeeper.prefab", 0.4f * body, 2f * body);
            AssertAgent("Assets/Prefabs/HunterGatekeeper.prefab", 0.4f * 0.85f * body, 2f * 0.85f * body);
            AssertAgent("Assets/Prefabs/ThiefGatekeeper.prefab", 0.4f * 1.2f * body, 2f * 1.2f * body);
            AssertAgent("Assets/Prefabs/CloakerGatekeeper.prefab", 0.4f * 1f * body, 2f * 1f * body);
        }

        /// <summary>The rule those numbers serve, stated once so a future rescale cannot slip
        /// past by moving all four literals together: no kind is baked wider than the carve the
        /// Bastion navmesh was built with, because a gatekeeper wider than the carve wedges in
        /// the doorway it is walking at.
        /// <para>The Thief is the one with nothing left to give: 0.48 against the 0.5 carve is
        /// 0.02 of margin, so the next spread change runs into this gate before it reaches
        /// anything else.</para></summary>
        [Test]
        public void EnemyPrefabs_StayInsideTheBakedAgentRadius()
        {
            const float bakedAgentRadius = 0.5f; // ProjectSettings/NavMeshAreas.asset
            AssertAgentRadiusUnder("Assets/Prefabs/EnemyGatekeeper.prefab", bakedAgentRadius);
            AssertAgentRadiusUnder("Assets/Prefabs/HunterGatekeeper.prefab", bakedAgentRadius);
            AssertAgentRadiusUnder("Assets/Prefabs/ThiefGatekeeper.prefab", bakedAgentRadius);
            AssertAgentRadiusUnder("Assets/Prefabs/CloakerGatekeeper.prefab", bakedAgentRadius);
        }

        /// <summary>Size is half the enemy tutorial, and the elite is its top end: it must read
        /// as a boss beside a Default without being told which one it is. Computed from the two
        /// dials that actually produce it — the Thief's committed root scale and
        /// MasterControl.FinaleEliteScale, which WaveSpawner multiplies onto the visual child —
        /// rather than from a baked literal.
        /// <para>BodyScale cancels here, and that is the design rather than an oversight: the
        /// ratio reduces to the Thief's authored spread times FinaleEliteScale, because both
        /// roots carry BodyScale. The relative read is what the size channel actually delivers
        /// to a player, and scaling every kind together cannot change it — which is exactly
        /// what the 2026-08-20 shrink tried to do. That change took the elite from 1.0x to 2.1x
        /// a Default by REMOVING DressVisual's 1/scale counter-scale; the BodyScale 0.75 it
        /// shipped alongside moved this number not at all. So the two dials this gate can be
        /// failed by are the only two that were ever able to move it.</para></summary>
        [Test]
        public void FinaleElite_ReadsAsABossBesideADefault()
        {
            const float bossRatio = 1.8f;
            float defaultRoot = RootScaleOf("Assets/Prefabs/EnemyGatekeeper.prefab");
            float thiefRoot = RootScaleOf("Assets/Prefabs/ThiefGatekeeper.prefab");
            Assert.Greater(defaultRoot, 0f, "Default gatekeeper root scale is not positive");

            float eliteBody = thiefRoot * MasterControl.FinaleEliteScale;
            float ratio = eliteBody / defaultRoot;
            Assert.GreaterOrEqual(ratio, bossRatio,
                "Finale elite reads " + F(ratio) + "x a Default (Thief root " + F(thiefRoot) +
                " x FinaleEliteScale " + F(MasterControl.FinaleEliteScale) + " = " + F(eliteBody) +
                ", against Default root " + F(defaultRoot) + "). The size tell needs at least " +
                F(bossRatio) + "x. Shrinking the other kinds cannot satisfy this — BodyScale is " +
                "on both sides of the ratio and cancels, which is why the 2026-08-20 shrink " +
                "moved this number not at all. Grow FinaleEliteScale, or the Thief's spread if " +
                "the navmesh carve has room for it.");
        }

        /// <summary>The other end of the same dial: an elite that reads as a boss and puts its
        /// head through the ceiling reads as a bug. Height is derived, not measured — elite body
        /// scale times the Thief's skinned bind-pose AABB height — and bounded by the room it
        /// actually fights in, floor slab to ceiling.
        /// <para>The tolerance is deliberate and is NOT slack. Today the arithmetic lands ~1%
        /// OVER the opening, and that 1% cannot be adjudicated here: a bind-pose AABB extent
        /// includes the arms at their widest and does not promise the head is the tallest point
        /// at rest. M109 pre-registered the adjudication as one look at the elite in the control
        /// room, not as a number. So this test's job is not to settle the 1% — it is to catch a
        /// future change that sends the elite FAR through the ceiling, and to say the metres out
        /// loud every time it runs so the question stays visible instead of silently passing.</para></summary>
        [Test]
        public void FinaleElite_StaysWithinTheControlRoomOpening()
        {
            // Assets/Packs/Gatekeepers/Thief.prefab, skinned mesh m_AABB m_Extent.y 1.0101144,
            // doubled. A literal because the pack prefab carries three AABBs (body and LODs) and
            // picking one by regex would be an arbitrary rule dressed up as a reading.
            const float thiefBindPoseHeight = 2.02f;
            // How far past the opening the derived number may sit before this is a real defect
            // rather than AABB error. See the summary: it is a blow-through detector, not a fit.
            const float allowedOvershoot = 1.05f;

            float thiefRoot = RootScaleOf("Assets/Prefabs/ThiefGatekeeper.prefab");
            float eliteBody = thiefRoot * MasterControl.FinaleEliteScale;
            float metres = eliteBody * thiefBindPoseHeight;
            float opening = BastionKit.StoreyHeight - BastionKit.FloorSlabThickness;

            string note =
                "Finale elite derived body height is " + F(metres) + " m (Thief root " +
                F(thiefRoot) + " x FinaleEliteScale " + F(MasterControl.FinaleEliteScale) +
                " x " + F(thiefBindPoseHeight) + " m bind-pose AABB) against a " + F(opening) +
                " m opening (BastionKit.StoreyHeight - FloorSlabThickness), i.e. " +
                F((metres / opening - 1f) * 100f) + "% over. Arithmetic does not settle this: " +
                "the adjudication is eyes-on at the elite in the control room. What this gate " +
                "does settle is that the elite has not been grown far past the room it fights " +
                "in — and it has been, by more than " + F((allowedOvershoot - 1f) * 100f) + "%.";
            Assert.LessOrEqual(metres, opening * allowedOvershoot, note);
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

        /// <summary>Uniform root scale of a committed enemy prefab, read from YAML like every
        /// other number in this file so the gate is against the shipped asset and not against a
        /// copy of it. x only: the builder writes the three axes together.</summary>
        static float RootScaleOf(string prefabPath)
        {
            Assert.IsTrue(File.Exists(Path.GetFullPath(prefabPath)), prefabPath + " missing");
            string yaml = File.ReadAllText(Path.GetFullPath(prefabPath));
            var m = RootScaleBlock.Match(yaml);
            Assert.IsTrue(m.Success, "root Transform m_LocalScale missing in " + prefabPath);
            return float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        /// <summary>Short invariant float for assertion messages — the numbers in them are the
        /// point, so they must not come out in the runner's locale.</summary>
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

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
