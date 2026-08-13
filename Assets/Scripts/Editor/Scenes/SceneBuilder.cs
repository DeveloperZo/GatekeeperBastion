using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
    /// Assets/Materials and Assets/Prefabs with stable GUIDs.
    /// Bastion kit segments under Environment are never wiped; Build All rebakes NavMesh only
    /// via <c>gkb_bastion_navmesh_request.txt</c> (or the kit path of Build All).</summary>
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

        /// <summary>The rebuildable roots. A root name in this list is owned by the builder
        /// and deleted before every rebuild; nothing else in the scene is touched.
        /// Bastion kit layout under Environment is never wiped (see RebuildBastionGeometry).</summary>
        static readonly string[] OwnedRootNames = { "Environment", "SpawnPoints", "Directional Light" };

        static double _nextSentinelPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_scene_build_request.txt");
        static string NavMeshSentinelPath => Path.Combine(Root, "gkb_bastion_navmesh_request.txt");
        static string ObstaclesSentinelPath => Path.Combine(Root, "gkb_bastion_obstacles_request.txt");
        static string TerminalsSentinelPath => Path.Combine(Root, "gkb_bastion_terminals_request.txt");

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

            if (File.Exists(TerminalsSentinelPath))
            {
                try { File.Delete(TerminalsSentinelPath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[SceneBuilder] terminals sentinel: " + e.Message);
                    return;
                }
                WireBastionFieldConsolesInPlace();
                return;
            }

            if (File.Exists(ObstaclesSentinelPath))
            {
                try { File.Delete(ObstaclesSentinelPath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[SceneBuilder] obstacles sentinel: " + e.Message);
                    return;
                }
                PlaceBastionObstaclesInPlace();
                return;
            }

            if (File.Exists(NavMeshSentinelPath))
            {
                try { File.Delete(NavMeshSentinelPath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[SceneBuilder] navmesh sentinel: " + e.Message);
                    return;
                }
                BakeBastionNavMeshInPlace();
                return;
            }

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

        /// <summary>Rebakes Bastion-NavMesh.asset from current Environment children without
        /// wiping kit / authored geometry. Prefer this after moving halves or floors.</summary>
        [MenuItem("GatekeeperBastion/Bake Bastion NavMesh")]
        public static void BakeBastionNavMeshMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BakeBastionNavMeshInPlace();
        }

        /// <summary>Idempotent graybox obstacles on kit segments (skips hatch bay 02 which is
        /// already authored). Names encode LongLane / Corner / Choke / Mask for the height-and-angles criteria.</summary>
        [MenuItem("GatekeeperBastion/Place Bastion Obstacles")]
        public static void PlaceBastionObstaclesMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PlaceBastionObstaclesInPlace();
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
            // M47: Bastion playable geometry is kit-authored. Never regenerate freestyle floors/walls.
            CreateEnemyPrefabs(CreatePalette()); // SceneBuilder.Enemies.cs — asset refresh, stable GUIDs

            if (BastionSceneHasKitSegments())
            {
                Debug.LogWarning(
                    "[SceneBuilder] Bastion.unity has BastionSegment under Environment — skipping geometry wipe. " +
                    "Drop gkb_bastion_navmesh_request.txt to rebake NavMesh only.");
                BakeBastionNavMeshInPlace();
                return;
            }

            Debug.LogError(
                "[SceneBuilder] Bastion.unity has no BastionSegment under Environment — Build All will not " +
                "generate freestyle bastion geometry (M47 closed). Author kit segments or restore from VCS.");
        }

        /// <summary>Rebake <c>Bastion-NavMesh.asset</c> from current Environment children (kit floors
        /// included). Does not rebuild or delete scene geometry.</summary>
        public static void BakeBastionNavMeshInPlace()
        {
            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject env = GameObject.Find("Environment");
                if (env == null)
                {
                    Debug.LogError("[SceneBuilder] No Environment root in Bastion.unity — cannot bake NavMesh.");
                    return;
                }

                BakeNavMesh(env);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[SceneBuilder] Bastion NavMesh rebaked in place from Environment children.");
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        static bool BastionSceneHasKitSegments()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BastionSegment[] segments = UnityEngine.Object.FindObjectsByType<BastionSegment>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            return segments != null && segments.Length > 0;
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

        // ---------- NavMesh ----------

        static void BakeNavMesh(GameObject env)
        {
            NavMeshSurface[] existing = env.GetComponents<NavMeshSurface>();
            for (int i = existing.Length - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(existing[i]);

            var surface = env.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                Debug.LogWarning("[SceneBuilder] NavMesh bake produced no data.");
                return;
            }
            AssetDatabase.DeleteAsset(NavMeshDataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshDataPath);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(NavMeshDataPath);
        }

        /// <summary>Attaches Ship Desk/Screen/Label graybox to every <see cref="BastionTerminal"/>
        /// and ensures <see cref="BastionLayout"/> on Environment. Idempotent by Desk child.</summary>
        public static void WireBastionFieldConsolesInPlace()
        {
            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject env = GameObject.Find("Environment");
                if (env == null)
                {
                    Debug.LogError("[SceneBuilder] No Environment root — cannot wire field consoles.");
                    return;
                }

                if (env.GetComponent<BastionLayout>() == null)
                    env.AddComponent<BastionLayout>();

                int wired = 0;
                BastionTerminal[] terminals = env.GetComponentsInChildren<BastionTerminal>(true);
                for (int i = 0; i < terminals.Length; i++)
                {
                    if (BastionFieldConsole.AttachIfMissing(terminals[i]))
                        wired++;
                    else if (terminals[i] != null)
                        BastionFieldConsole.ApplyScale(terminals[i].transform);
                }

                int gates = 0;
                Material gateMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Mat_Hatch.mat");
                BastionDoor[] doors = env.GetComponentsInChildren<BastionDoor>(true);
                for (int i = 0; i < doors.Length; i++)
                {
                    BastionDoor door = doors[i];
                    if (door == null)
                        continue;
                    if (gateMat != null)
                        door.SetGateMaterial(gateMat);
                    // Existing BastionDoor markers only — never rewrite segment Door/None sides
                    // or move hand-authored gate transforms.
                    door.EnsureGatePresentation();
                    if (door.StartsLocked)
                        door.SetOpen(false);
                    gates++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[SceneBuilder] Field consoles wired (" + wired + " new of "
                    + terminals.Length + " terminals; scale=" + BastionFieldConsole.Scale
                    + "); progression gates presented=" + gates + ". BastionLayout on Environment.");
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        /// <summary>Places named graybox obstacles under each kit segment's <c>obstacles</c>
        /// child. Leaves existing props (hatch-bay Div/Rib under 02) alone. Idempotent by name.</summary>
        public static void PlaceBastionObstaclesInPlace()
        {
            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject env = GameObject.Find("Environment");
                if (env == null)
                {
                    Debug.LogError("[SceneBuilder] No Environment root — cannot place obstacles.");
                    return;
                }

                Material rib = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Mat_Rib.mat");
                if (rib == null)
                {
                    Palette pal = CreatePalette();
                    rib = pal.Rib;
                }

                int placed = 0;
                BastionSegment[] segments = env.GetComponentsInChildren<BastionSegment>(true);
                for (int i = 0; i < segments.Length; i++)
                {
                    BastionSegment segment = segments[i];
                    if (segment == null)
                        continue;

                    // Hatch bay 02 already carries Div/Spine/Ribs — do not add or clear.
                    if (segment.gameObject.name.StartsWith("02", StringComparison.Ordinal))
                        continue;

                    Transform folder = EnsureObstaclesFolder(segment.transform);
                    placed += PlaceObstaclesForSegment(segment, folder, rib);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                BakeNavMesh(env);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[SceneBuilder] Bastion obstacles placed (" + placed + " new). NavMesh rebaked.");
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        static Transform EnsureObstaclesFolder(Transform segment)
        {
            Transform existing = segment.Find("obstacles");
            if (existing != null)
                return existing;

            var go = new GameObject("obstacles");
            go.transform.SetParent(segment, false);
            return go.transform;
        }

        static int PlaceObstaclesForSegment(BastionSegment segment, Transform folder, Material rib)
        {
            string name = segment.gameObject.name;
            if (name.StartsWith("01", StringComparison.Ordinal))
            {
                // Hatch south bay: light flanks only — keep the defend floor open.
                return PlaceIfMissing(folder, rib, "Cover_HatchFlank_W", new Vector3(-12f, 1.4f, 5f), new Vector3(6f, 2.8f, 0.8f), 15f)
                    + PlaceIfMissing(folder, rib, "Cover_HatchFlank_E", new Vector3(12f, 1.4f, 8f), new Vector3(6f, 2.8f, 0.8f), -20f);
            }

            if (name.StartsWith("03", StringComparison.Ordinal))
            {
                // Approach hub: long lane + corners; mask breaks hatch LOS with flank bypass.
                return PlaceIfMissing(folder, rib, "LongLane_Approach", new Vector3(-22f, 1.4f, -10f), new Vector3(1.2f, 2.8f, 90f), 0f)
                    + PlaceIfMissing(folder, rib, "Mask_ApproachToHatch", new Vector3(0f, 1.2f, -68f), new Vector3(28f, 2.4f, 1.2f), 0f)
                    + PlaceIfMissing(folder, rib, "Corner_ApproachSW", new Vector3(-38f, 1.4f, -45f), new Vector3(14f, 2.8f, 1.2f), -40f)
                    + PlaceIfMissing(folder, rib, "Corner_ApproachNE", new Vector3(32f, 1.4f, 35f), new Vector3(12f, 2.8f, 1.2f), 35f);
            }

            if (name.StartsWith("04", StringComparison.Ordinal))
            {
                return PlaceIfMissing(folder, rib, "Cover_WestFull", new Vector3(5f, 1.4f, -25f), new Vector3(18f, 2.8f, 1.2f), 8f)
                    + PlaceIfMissing(folder, rib, "Corner_WestFull", new Vector3(18f, 1.4f, 15f), new Vector3(10f, 2.8f, 1.2f), 28f);
            }

            if (name.StartsWith("05", StringComparison.Ordinal))
            {
                // West spawn alcove: screen the door so hatch cannot see the spawn pad.
                return PlaceIfMissing(folder, rib, "Mask_WestSpawn", new Vector3(12f, 1.4f, 0f), new Vector3(1.2f, 2.8f, 22f), 0f)
                    + PlaceIfMissing(folder, rib, "Corner_WestSpawn", new Vector3(-8f, 1.4f, 12f), new Vector3(8f, 2.8f, 1.2f), -30f);
            }

            if (name.StartsWith("06", StringComparison.Ordinal) && name.IndexOf("Quarter", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PlaceIfMissing(folder, rib, "Mask_EastStub", new Vector3(-8f, 1.4f, 0f), new Vector3(1.2f, 2.8f, 14f), 0f);
            }

            if (name.StartsWith("06", StringComparison.Ordinal))
            {
                return PlaceIfMissing(folder, rib, "Cover_EastFull", new Vector3(-22f, 1.4f, 5f), new Vector3(1.2f, 2.8f, 28f), 0f)
                    + PlaceIfMissing(folder, rib, "Corner_EastFull", new Vector3(12f, 1.4f, -28f), new Vector3(12f, 2.8f, 1.2f), -32f);
            }

            if (name.StartsWith("07", StringComparison.Ordinal))
            {
                // Staging: asymmetric choke + northbound lane.
                return PlaceIfMissing(folder, rib, "Choke_Staging_W", new Vector3(-28f, 1.4f, -5f), new Vector3(22f, 2.8f, 1.2f), 5f)
                    + PlaceIfMissing(folder, rib, "Choke_Staging_E", new Vector3(32f, 1.4f, 12f), new Vector3(20f, 2.8f, 1.2f), -8f)
                    + PlaceIfMissing(folder, rib, "LongLane_Staging", new Vector3(10f, 1.4f, 35f), new Vector3(1.2f, 2.8f, 55f), 0f);
            }

            if (name.StartsWith("08", StringComparison.Ordinal))
            {
                return PlaceIfMissing(folder, rib, "Corner_Spine", new Vector3(-18f, 1.4f, 8f), new Vector3(14f, 2.8f, 1.2f), 42f)
                    + PlaceIfMissing(folder, rib, "Cover_Spine", new Vector3(12f, 1.4f, -22f), new Vector3(20f, 2.8f, 1.2f), -6f);
            }

            if (name.StartsWith("09", StringComparison.Ordinal))
            {
                return PlaceIfMissing(folder, rib, "Corner_LateTerm", new Vector3(-5f, 1.4f, 18f), new Vector3(12f, 2.8f, 1.2f), -28f);
            }

            if (name.StartsWith("10", StringComparison.Ordinal))
            {
                // Control antechamber near the south door only.
                return PlaceIfMissing(folder, rib, "Cover_ControlEntry", new Vector3(0f, 1.4f, -150f), new Vector3(48f, 2.8f, 1.2f), 0f)
                    + PlaceIfMissing(folder, rib, "Corner_Control", new Vector3(40f, 1.4f, -120f), new Vector3(16f, 2.8f, 1.2f), 25f);
            }

            return 0;
        }

        static int PlaceIfMissing(
            Transform folder,
            Material rib,
            string name,
            Vector3 localPos,
            Vector3 scale,
            float yawDegrees)
        {
            if (folder.Find(name) != null)
                return 0;

            GameObject go = Box(name, localPos, scale, rib, folder);
            go.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            return 1;
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


