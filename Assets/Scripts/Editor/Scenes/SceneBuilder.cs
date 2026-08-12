using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Gray-box geometry rebuilds. "Build All Scenes" rebuilds the
    /// owned set — the <c>Environment</c> root, the <c>SpawnPoints</c> root, the
    /// <c>Directional Light</c> and the NavMesh asset — IN PLACE in Ship.unity and
    /// Bastion.unity, and registers Splash, MainMenu, Ship, Bastion in EditorBuildSettings.
    /// Everything else in those scenes is authored, persistent content this class never
    /// touches: station consoles, the Hatch, player rigs, ShipController/GameSystems,
    /// EventSystems, and every UI canvas. The owned set carries no serialized
    /// cross-references, which is what makes the rebuild safe; anything that gains one
    /// (the Hatch did — RepairInteractable.station) must move out of it. Materials and the
    /// three gatekeeper prefabs (SceneBuilder.Enemies.cs) are refreshed under
    /// Assets/Materials and Assets/Prefabs with stable GUIDs.</summary>
    [InitializeOnLoad]
    public static partial class SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Bastion.unity";
        const string SplashScenePath = "Assets/Scenes/Splash.unity";
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string ShipScenePath = "Assets/Scenes/Ship.unity";
        const string NavMeshDataPath = "Assets/Scenes/Bastion-NavMesh.asset";
        const string PrefabDir = "Assets/Prefabs";
        const string MaterialDir = "Assets/Materials";
        const string EnemyPrefabPath = PrefabDir + "/EnemyGatekeeper.prefab";
        const float WallHeight = 3.5f;
        const float WallThickness = 0.5f;

        /// <summary>The rebuildable roots. A root name in this list is owned by the builder
        /// and deleted before every rebuild; nothing else in the scene is touched.</summary>
        static readonly string[] OwnedRootNames = { "Environment", "SpawnPoints", "Directional Light" };

        static double _nextSentinelPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_scene_build_request.txt");

        static SceneBuilder()
        {
            EditorApplication.update += PollSentinel;
        }

        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextSentinelPoll) return;
            _nextSentinelPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch (Exception e) { Debug.LogWarning("[SceneBuilder] sentinel: " + e.Message); return; }
            BuildAllCore();
        }

        sealed class Palette
        {
            public Material Floor, Wall, Rib, Enemy, Hunter, Ram, Hatch, Player, Breach;
        }

        // ---------- menu items ----------

        [MenuItem("GatekeeperBastion/Build All Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAllCore();
        }

        /// <summary>Batchmode entry: rebuild geometry/spawns/NavMesh only. UI is authored in scenes.</summary>
        public static void BuildFromBatch()
        {
            BuildAllCore();
            EditorApplication.Exit(0);
        }

        static void BuildAllCore()
        {
            EnsureFolders();
            RebuildShipGeometry();     // SceneBuilder.Ship.cs
            RebuildBastionGeometry();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] In-place geometry rebuild done; EditorBuildSettings.scenes = Splash, MainMenu, Ship, Bastion.");
        }

        // Build All Scenes is the only entry point. A Bastion-only item existed until
        // 2026-08-04 and was a narrower duplicate of it: the full build takes seconds, and a
        // Bastion rebuilt without re-registering build settings is exactly the state that
        // hides gotcha 4 (Docs/Working-Rules.md, "Tools, scripts and the menu").
        static void RegisterBuildScenes()
        {
            // Boot order: Splash → MainMenu → Ship → Bastion.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SplashScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(ShipScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };
        }

        /// <summary>Opens the scene single (saving nothing) and deletes the owned roots.
        /// Returns the open scene. Only exact root-name matches in
        /// <see cref="OwnedRootNames"/> are deleted — authored content is never touched.</summary>
        static Scene OpenAndClearOwnedRoots(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (string owned in OwnedRootNames)
                {
                    if (root != null && root.name == owned)
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                        break;
                    }
                }
            }
            return scene;
        }

        // ---------- Bastion ----------

        static void RebuildBastionGeometry()
        {
            Scene scene = OpenAndClearOwnedRoots(ScenePath);
            Palette pal = CreatePalette();
            CreateEnemyPrefabs(pal); // SceneBuilder.Enemies.cs — asset refresh, stable GUIDs

            BuildLighting();
            GameObject env = BuildBastion(pal);
            BuildSpawnPoints(pal);
            BakeNavMesh(env);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SceneBuilder] Rebuilt {ScenePath} in place: Environment, SpawnPoints, Directional Light, NavMesh. Authored content untouched.");
        }

        // ---------- assets ----------

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(MaterialDir)) AssetDatabase.CreateFolder("Assets", "Materials");
        }

        /// <summary>Alien-bastion palette: dark violets and sickly teals, with the Hatch
        /// a loud safety-orange so "home" reads at a glance across the map.</summary>
        static Palette CreatePalette()
        {
            return new Palette
            {
                Floor = MakeMaterial("Floor", new Color(0.13f, 0.11f, 0.18f)),
                Wall = MakeMaterial("Wall", new Color(0.22f, 0.17f, 0.30f)),
                Rib = MakeMaterial("Rib", new Color(0.16f, 0.32f, 0.32f)),
                // The three gatekeeper kinds read by colour before anyone learns their names; see
                // SceneBuilder.Enemies.cs for why Ram's violet is brighter than the hull's.
                Enemy = MakeMaterial("Enemy", new Color(0.85f, 0.15f, 0.12f)),
                Hunter = MakeMaterial("Hunter", new Color(1f, 0.78f, 0.10f)),
                Ram = MakeMaterial("Ram", new Color(0.38f, 0.28f, 0.66f)),
                Hatch = MakeMaterial("Hatch", new Color(1f, 0.55f, 0.1f)),
                Player = MakeMaterial("Player", new Color(0.85f, 0.85f, 0.9f)),
                Breach = MakeMaterial("Breach", new Color(0.25f, 0.75f, 0.35f)),
            };
        }

        /// <summary>Creates or refreshes a saved material. URP Lit when available, else Standard.</summary>
        static Material MakeMaterial(string name, Color color)
        {
            string path = $"{MaterialDir}/Mat_{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // The three gatekeeper prefabs are built in SceneBuilder.Enemies.cs.

        // ---------- bastion scene content ----------

        static void BuildLighting()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.45f;
            light.color = new Color(0.75f, 0.7f, 1f);
            go.transform.rotation = Quaternion.Euler(60f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.07f, 0.15f);
        }

        /// <summary>
        /// The bastion interior. Coordinates (x: -15..15, z: -22..30):
        ///   Hatch bay (south, z -22..-10)  — Hatch (authored, scene root) + player spawn.
        ///   Main bore (z -10..2, 6 wide)   — single chokepoint away from home.
        ///   Mid chamber (z 2..16, full width) — first fighting room, breach at its west end.
        ///   NW / NE chambers (z 16..30, split by a spine) — deep rooms, breaches at far ends.
        /// Diagonal "rib" blocks in the chambers break up sightlines for the alien feel.
        /// </summary>
        static GameObject BuildBastion(Palette pal)
        {
            var env = new GameObject("Environment");
            Transform t = env.transform;
            float y = WallHeight * 0.5f;

            Box("Floor", new Vector3(0f, -0.25f, 4f), new Vector3(30f, 0.5f, 52f), pal.Floor, t);

            // Outer shell.
            Box("Shell_N", new Vector3(0f, y, 30.25f), new Vector3(30.5f, WallHeight, WallThickness), pal.Wall, t);
            Box("Shell_S", new Vector3(0f, y, -22.25f), new Vector3(30.5f, WallHeight, WallThickness), pal.Wall, t);
            Box("Shell_E", new Vector3(15.25f, y, 4f), new Vector3(WallThickness, WallHeight, 52f), pal.Wall, t);
            Box("Shell_W", new Vector3(-15.25f, y, 4f), new Vector3(WallThickness, WallHeight, 52f), pal.Wall, t);

            // Hatch bay narrows into the bore (gap x -3..3 at z = -10).
            Box("Bay_W", new Vector3(-9f, y, -10f), new Vector3(12f, WallHeight, WallThickness), pal.Wall, t);
            Box("Bay_E", new Vector3(9f, y, -10f), new Vector3(12f, WallHeight, WallThickness), pal.Wall, t);

            // Main bore corridor walls (z -10..2).
            Box("Bore_W", new Vector3(-3f, y, -4f), new Vector3(WallThickness, WallHeight, 12f), pal.Wall, t);
            Box("Bore_E", new Vector3(3f, y, -4f), new Vector3(WallThickness, WallHeight, 12f), pal.Wall, t);

            // Mid chamber / north chambers divider at z = 16, doorways at x = -8 and x = 8.
            Box("Div_W", new Vector3(-13f, y, 16f), new Vector3(4f, WallHeight, WallThickness), pal.Wall, t);
            Box("Div_C", new Vector3(0f, y, 16f), new Vector3(10f, WallHeight, WallThickness), pal.Wall, t);
            Box("Div_E", new Vector3(13f, y, 16f), new Vector3(4f, WallHeight, WallThickness), pal.Wall, t);

            // Spine splitting the two deep chambers (z 16..30).
            Box("Spine", new Vector3(0f, y, 23f), new Vector3(WallThickness, WallHeight, 14f), pal.Wall, t);

            // Alien ribs: canted blocks that break sightlines in the chambers.
            MakeRib(pal, t, "Rib_Mid_A", new Vector2(-5f, 8f), 30f);
            MakeRib(pal, t, "Rib_Mid_B", new Vector2(7f, 11f), -25f);
            MakeRib(pal, t, "Rib_NW", new Vector2(-8f, 21f), 40f);
            MakeRib(pal, t, "Rib_NE", new Vector2(8f, 24f), -35f);

            return env;
        }

        static void MakeRib(Palette pal, Transform parent, string name, Vector2 posXZ, float yaw)
        {
            GameObject rib = Box(name, new Vector3(posXZ.x, WallHeight * 0.4f, posXZ.y),
                new Vector3(3.5f, WallHeight * 0.8f, 0.8f), pal.Rib, parent);
            rib.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Three spawn points at the far ends of the chambers, deep in the bastion.
        /// The authored WaveSpawner's explicit array stays empty; it resolves these through
        /// the SpawnPoint registry at runtime, so the rebuild wires nothing.</summary>
        static SpawnPoint[] BuildSpawnPoints(Palette pal)
        {
            var root = new GameObject("SpawnPoints");
            return new[]
            {
                MakeSpawnPoint("Spawn_NW", new Vector3(-11f, 0f, 28f), pal, root.transform),
                MakeSpawnPoint("Spawn_NE", new Vector3(11f, 0f, 28f), pal, root.transform),
                MakeSpawnPoint("Spawn_MidWest", new Vector3(-13f, 0f, 9f), pal, root.transform),
            };
        }

        static SpawnPoint MakeSpawnPoint(string name, Vector3 pos, Palette pal, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            // Collider-free floor decal so breaches are visible in the gray-box.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.name = "Marker";
            marker.transform.SetParent(go.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            marker.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            marker.GetComponent<Renderer>().sharedMaterial = pal.Breach;

            return go.AddComponent<SpawnPoint>();
        }

        static void BakeNavMesh(GameObject env)
        {
            var surface = env.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                Debug.LogWarning("[SceneBuilder] NavMesh bake produced no data.");
                return;
            }
            AssetDatabase.DeleteAsset(NavMeshDataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshDataPath);
        }

        // ---------- helpers ----------

        static GameObject Box(string name, Vector3 center, Vector3 size, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
