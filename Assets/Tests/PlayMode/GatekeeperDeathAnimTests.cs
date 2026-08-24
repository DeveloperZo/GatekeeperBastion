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
    /// <summary>Live Animator + Renderer.bounds on the shipped Default prefab.
    /// A YAML pin on the controller cannot see a Mixamo fall that has been baked
    /// into an in-place vertical sink — that is what Range play reported.</summary>
    public class GatekeeperDeathAnimTests
    {
        const string PrefabPath = "Assets/Prefabs/EnemyGatekeeper.prefab";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeathPresentation_FlattensTheBody_NotAVerticalSink()
        {
            GameObject prefab = LoadPrefab(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath + " missing");

            GameObject go = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            var anim = go.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(anim, "prefab has no Animator");
            Assert.IsNotNull(anim.runtimeAnimatorController, "Animator has no controller");
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var rend = go.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(rend, "prefab has no Renderer");

            yield return null;
            yield return null;

            Bounds start = rend.bounds;
            var gk = go.GetComponent<EnemyGatekeeper>();
            Assert.IsNotNull(gk);
            gk.BeginDeathPresentation();

            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
                yield return null;
            }

            Bounds later = rend.bounds;
            float startH = start.size.y;
            float laterH = later.size.y;
            float laterPlanar = Mathf.Max(later.size.x, later.size.z);
            string numbers =
                $"startH={startH:0.00} laterH={laterH:0.00} laterPlanar={laterPlanar:0.00} " +
                $"startMinY={start.min.y:0.00} laterMinY={later.min.y:0.00} " +
                $"state={AnimatorStateName(anim)}";

            Assert.Less(laterH, startH * 0.75f,
                "death must flatten the body, not keep standing height while the mesh drops. " + numbers);
            Assert.Greater(laterPlanar, laterH,
                "a fallen body is longer in XZ than it is tall. " + numbers);
        }

        static string AnimatorStateName(Animator anim)
        {
            if (anim == null) return "(null)";
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Death")) return "Death";
            if (info.IsName("Locomotion")) return "Locomotion";
            if (info.IsName("Attack")) return "Attack";
            return "hash=" + info.shortNameHash;
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
