using System;
using System.Collections;
using System.Reflection;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>Live combined Renderer.bounds on the shipped Default prefab after death.
    /// Spawn height matches WaveSpawner (navmesh + baseOffset). A 0.4 m trough allowance
    /// stayed green while the corpse sat inside the 0.5 m floor slab.</summary>
    public class GatekeeperDeathAnimTests
    {
        const string PrefabPath = "Assets/Prefabs/EnemyGatekeeper.prefab";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeathPresentation_LiesOnTheWalkableFloor()
        {
            GameObject prefab = LoadPrefab(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath + " missing");

            var prefabAgent = prefab.GetComponent<NavMeshAgent>();
            float lift = prefabAgent != null ? prefabAgent.baseOffset : 0f;
            float floorY = 0f;
            GameObject go = UnityEngine.Object.Instantiate(
                prefab, Vector3.up * lift, Quaternion.identity);
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            var anim = go.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(anim, "prefab has no Animator");
            Assert.IsNotNull(anim.runtimeAnimatorController, "Animator has no controller");
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            yield return null;
            yield return null;

            EncapsulateY(go, out float startMinY, out float startMaxY, out float startH, out _);
            float startBoneRange = BoneYRange(anim);
            var gk = go.GetComponent<EnemyGatekeeper>();
            Assert.IsNotNull(gk);
            gk.BeginDeathPresentation();

            // Trough tracks the FALL only. The standing rig's skinned bounds already
            // reach ~0.18 below the walkable plane (bounds padding, not visible mesh),
            // so seeding from startMinY fails the floor assertion on the standing pose
            // before the collapse has moved anything.
            float peakMaxY = startMaxY;
            float troughMinY = float.PositiveInfinity;
            float t = 0f;
            // Past the 3.63 s authored falling-back clip (the live path now — the procedural
            // board-tip only covers a controller-less body), so the assertions below measure
            // the held last frame, not a mid-fall pose.
            while (t < 3.9f)
            {
                t += Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
                yield return null;
                EncapsulateY(go, out float midMin, out float midMax, out _, out _);
                if (midMax > peakMaxY) peakMaxY = midMax;
                if (midMin < troughMinY) troughMinY = midMin;
            }

            EncapsulateY(go, out float laterMinY, out float laterMaxY, out float laterH, out float laterPlanar);
            // Shape is judged on BONES, not renderer bounds: SkinnedMeshRenderer bounds are
            // conservative AABBs that stay standing-shaped through an in-pose fall (measured
            // laterH 1.50 on a body whose skeleton Y-range had dropped 1.35 -> 0.73), so a
            // bounds height assertion fails the accepted clip presentation.
            float endBoneRange = BoneYRange(anim);
            string numbers =
                $"startH={startH:0.00} laterH={laterH:0.00} laterPlanar={laterPlanar:0.00} " +
                $"startBoneRange={startBoneRange:0.00} endBoneRange={endBoneRange:0.00} " +
                $"floorY={floorY:0.00} lift={lift:0.00} " +
                $"startMinY={startMinY:0.00} laterMinY={laterMinY:0.00} troughMinY={troughMinY:0.00} " +
                $"startMaxY={startMaxY:0.00} laterMaxY={laterMaxY:0.00} peakMaxY={peakMaxY:0.00}";

            Assert.Less(endBoneRange, startBoneRange * 0.75f,
                "death must lay the skeleton down, not hold standing height. " + numbers);
            Assert.GreaterOrEqual(troughMinY, floorY - 0.05f,
                "mesh must rest on the walkable floor, not inside the slab. " + numbers);
            Assert.GreaterOrEqual(laterMinY, floorY - 0.05f,
                "corpse min.y must stay on the floor they died on. " + numbers);
            Assert.LessOrEqual(peakMaxY, startMaxY + 0.6f,
                "mesh must not kick into a floor above them. " + numbers);
        }

        /// <summary>World-Y spread across the mapped humanoid bones — the honest shape
        /// measure for a fall that plays inside the pose (the root transform never rotates).</summary>
        static float BoneYRange(Animator anim)
        {
            float lo = float.PositiveInfinity, hi = float.NegativeInfinity;
            for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; b++)
            {
                var tr = anim.GetBoneTransform(b);
                if (tr == null) continue;
                float y = tr.position.y;
                if (y < lo) lo = y;
                if (y > hi) hi = y;
            }
            return hi > lo ? hi - lo : 0f;
        }

        /// <summary>Clone shape: component disabled at spawn (NetEnemy does this), local
        /// Damageable never damaged, then BeginDeathPresentation from the death Rpc. IsDead
        /// is false here forever, so only the presentation-death latch keeps Update inert
        /// once the collapse ends — without it, Update resumed AI on the corpse and (with no
        /// GameController bound) ran Stop, observably flipping enabled off.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeathPresentation_StaysLatchedOnAnUndamagedClone()
        {
            GameObject prefab = LoadPrefab(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath + " missing");

            GameObject go = UnityEngine.Object.Instantiate(prefab, Vector3.up, Quaternion.identity);
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            var gk = go.GetComponent<EnemyGatekeeper>();
            Assert.IsNotNull(gk);
            gk.enabled = false; // NetEnemy.OnNetworkSpawn disables clone AI

            gk.BeginDeathPresentation();
            Assert.IsTrue(gk.enabled, "presentation must re-enable the component so LateUpdate ticks");

            float t = 0f;
            while (t < 1.2f) // past CollapseSeconds — the frame AI used to resurrect
            {
                t += Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
                yield return null;
            }

            Assert.IsTrue(gk.enabled,
                "presentation-dead AI must stay latched inert; Update ran Stop() on the corpse");
            if (agent != null)
                Assert.IsFalse(agent.enabled, "corpse agent must stay disabled after the collapse");
            UnityEngine.Object.Destroy(go);
        }

        static void EncapsulateY(GameObject go, out float minY, out float maxY, out float height, out float planar)
        {
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            var rends = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                Renderer r = rends[i];
                if (r == null || !r.enabled) continue;
                Bounds b = r.bounds;
                if (b.min.y < minY) minY = b.min.y;
                if (b.max.y > maxY) maxY = b.max.y;
                if (b.min.x < minX) minX = b.min.x;
                if (b.max.x > maxX) maxX = b.max.x;
                if (b.min.z < minZ) minZ = b.min.z;
                if (b.max.z > maxZ) maxZ = b.max.z;
            }
            height = maxY - minY;
            planar = Mathf.Max(maxX - minX, maxZ - minZ);
        }

        static GameObject LoadPrefab(string path)
        {
            var adb = Type.GetType("UnityEditor.AssetDatabase,UnityEditor");
            if (adb == null) return null;
            var load = adb.GetMethod(
                "LoadAssetAtPath",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Type) },
                null);
            if (load == null) return null;
            return load.Invoke(null, new object[] { path, typeof(GameObject) }) as GameObject;
        }
    }
}
