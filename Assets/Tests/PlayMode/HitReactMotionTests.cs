using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>The flinch must MOVE THE BODY. The EditMode HitReactTests pin the controller,
    /// clip and call sites on disk, and stayed green while the additive layer produced no
    /// visible motion in play (Docs/Milestones/3. completed/M84-gatekeepers-flinch-when-shot.md,
    /// 2026-08-24 correction). This is the gate a no-op layer cannot pass: fire the trigger on
    /// the shipped prefab and measure an upper-body bone actually rotating, over the noise the
    /// idle pose contributes on its own.</summary>
    public class HitReactMotionTests
    {
        const string PrefabPath = "Assets/Prefabs/EnemyGatekeeper.prefab";

        /// <summary>Seconds of pre-trigger sampling that establish how much the bone moves with
        /// nobody flinching (idle sway, damping) — the honest zero for the assertion.</summary>
        const float NoiseWindowSeconds = 0.2f;

        /// <summary>Seconds after the trigger in which the jolt must land. The clip peaks at
        /// 0.06s behind a 0.05s transition, so this is generous without letting the 0.25s
        /// clip's own recovery hide a missed peak.</summary>
        const float FlinchWindowSeconds = 0.25f;

        /// <summary>Degrees over the idle noise floor that count as a flinch. The authored peak
        /// stacks spine, chest and head muscles, so a working layer clears this by an order of
        /// magnitude; a no-op layer reads ~0 over noise.</summary>
        const float MinFlinchDegrees = 2f;

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
        public IEnumerator HitReactTrigger_RotatesAnUpperBodyBone()
        {
            GameObject prefab = LoadPrefab(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath + " missing");

            GameObject go = UnityEngine.Object.Instantiate(prefab, Vector3.up, Quaternion.identity);
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false; // no navmesh in a blank scene; hold still

            var anim = go.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(anim, "prefab has no Animator");
            Assert.IsNotNull(anim.runtimeAnimatorController, "Animator has no controller");
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; // blank scene has no camera

            // Let the animator bind and settle into its default states.
            yield return null;
            yield return null;

            Transform bone = anim.GetBoneTransform(HumanBodyBones.Head);
            if (bone == null) bone = anim.GetBoneTransform(HumanBodyBones.Chest);
            Assert.IsNotNull(bone, "avatar maps neither Head nor Chest — not the humanoid rig this test expects");

            // Noise floor: how far the bone strays from its own start with no trigger at all.
            Quaternion idleStart = bone.rotation;
            float idleNoise = 0f;
            float t = 0f;
            while (t < NoiseWindowSeconds)
            {
                t += Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
                yield return null;
                float drift = Quaternion.Angle(idleStart, bone.rotation);
                if (drift > idleNoise) idleNoise = drift;
            }

            Quaternion preTrigger = bone.rotation;
            anim.SetTrigger("HitReact");

            float peak = 0f;
            t = 0f;
            while (t < FlinchWindowSeconds)
            {
                t += Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
                yield return null;
                float swing = Quaternion.Angle(preTrigger, bone.rotation);
                if (swing > peak) peak = swing;
            }

            UnityEngine.Object.Destroy(go);
            Assert.Greater(peak, idleNoise + MinFlinchDegrees,
                "HitReact fired but the bone barely moved — the additive flinch layer is a no-op. " +
                $"peak={peak:0.00}deg idleNoise={idleNoise:0.00}deg bone={bone.name}");
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
