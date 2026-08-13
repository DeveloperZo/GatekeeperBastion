using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>SceneBuilder, enemy assets: the three gatekeeper prefabs and the materials that make
    /// them tellable apart. Same idempotent contract as the rest of the builder — each prefab is
    /// saved to a fixed path so re-running keeps its GUID and any scene reference to it.</summary>
    /// <remarks>
    /// COLOUR AND SIZE ARE THE ONLY TUTORIAL THIS GAME HAS. A player meets three kinds at once
    /// and has to triage them before knowing any of their names, so the read is built from two
    /// channels that survive a dark corridor and a glance:
    ///   Default — the red they already know, unchanged, at 1.0 scale.
    ///   Hunter  — bright amber, 0.85 scale. Small and loud: the fast fragile one coming for you.
    ///   Ram     — saturated violet-steel, 1.4 scale. Twice the silhouette of a Hunter.
    /// Ram's violet is deliberately brighter and more saturated than the bastion's own walls
    /// (0.22, 0.17, 0.30) — a genuinely dark violet body against a dark violet hull is invisible,
    /// which is the opposite of the point. Its size does the rest of the work.
    ///
    /// NUMBERS DO NOT LIVE HERE. Everything baked below (hp, agent speed) is read from
    /// WaveDirector.StatsFor so the saved asset matches the sim, and every one of them is pushed
    /// again onto the live instance by WaveSpawner at spawn (Docs/Architecture.md GOTCHA 1). Only the
    /// things the sim has no opinion about — colour, scale, collider geometry — are authored here.
    /// </remarks>
    public static partial class SceneBuilder
    {
        const string HunterPrefabPath = PrefabDir + "/HunterGatekeeper.prefab";
        const string RamPrefabPath = PrefabDir + "/RamGatekeeper.prefab";

        /// <summary>The three gatekeeper prefabs, wired into the WaveSpawner as a set.</summary>
        sealed class EnemyPrefabs
        {
            public GameObject Default, Hunter, Ram;
        }

        static EnemyPrefabs CreateEnemyPrefabs(Palette pal)
        {
            return new EnemyPrefabs
            {
                Default = CreateGatekeeperPrefab<EnemyGatekeeper>(
                    "EnemyGatekeeper", EnemyPrefabPath, pal.Enemy, GatekeeperKind.Default, 1f),
                Hunter = CreateGatekeeperPrefab<HunterGatekeeper>(
                    "HunterGatekeeper", HunterPrefabPath, pal.Hunter, GatekeeperKind.Hunter, 0.85f),
                Ram = CreateGatekeeperPrefab<RamGatekeeper>(
                    "RamGatekeeper", RamPrefabPath, pal.Ram, GatekeeperKind.Ram, 1.4f),
            };
        }

        /// <summary>One gatekeeper prefab: capsule, NavMeshAgent, Damageable, the kind's own
        /// EnemyGatekeeper subclass, and the net components (inert until a session Spawn()s).
        /// <paramref name="scale"/> scales the silhouette AND the agent footprint together,
        /// so a Ram takes up the room it looks like it takes up.</summary>
        static GameObject CreateGatekeeperPrefab<T>(string name, string path, Material material,
                                                 GatekeeperKind kind, float scale)
            where T : EnemyGatekeeper
        {
            GatekeeperStats stats = WaveDirector.StatsFor(kind);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<Renderer>().sharedMaterial = material;

            var agent = go.AddComponent<NavMeshAgent>();
            // Speed is baked from the kind's patrol speed for consistency only; WaveSpawner pushes
            // it (and the chase speed) again through EnemyGatekeeper.Configure at spawn.
            agent.speed = stats.patrolSpeed;
            agent.acceleration = 14f;
            agent.angularSpeed = 400f;
            // Geometry follows the capsule: a 1.4-scale Ram is 1.4x as wide and as tall, and its
            // origin sits at the capsule's centre, so baseOffset must match the half-height or the
            // body sinks into (or floats over) the floor.
            agent.radius = 0.4f * scale;
            agent.height = 2f * scale;
            agent.baseOffset = scale;
            agent.stoppingDistance = 1.6f;

            // Baked at the kind's own maxHp so the saved prefab matches the tuned baseline.
            // WaveSpawner still sets HP absolutely from the same source at spawn time, so the live
            // value can never drift with whatever this asset happens to carry.
            go.AddComponent<Damageable>().SetMaxHp(Mathf.Max(1f, stats.maxHp));
            go.AddComponent<T>();
            go.AddComponent<NetworkObject>();
            var netTransform = go.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            go.AddComponent<NetEnemy>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
