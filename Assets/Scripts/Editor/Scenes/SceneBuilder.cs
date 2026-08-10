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

namespace GatekeeperBastion.EditorTools
{
    /// <summary>One-click gray-box scene builders for the project. "Build All Scenes"
    /// (re)builds Splash, MainMenu, Ship and Bastion into Assets/Scenes and registers
    /// them in EditorBuildSettings in that order. "Build Bastion Scene" rebuilds just
    /// the BASTION interior: the Hatch (umbilical back to your ship) sits at the south
    /// end next to the player spawn; a main bore corridor runs north and widens into
    /// three chambers whose far ends hold the spawn points. Alien feel comes from
    /// layout + a dark violet/teal palette. The Ship scene (see SceneBuilder.Ship.cs)
    /// is the cleaner steel-blue home hull with the three meta stations. Materials and
    /// the THREE gatekeeper prefabs (Default / Hunter / Ram — see SceneBuilder.Enemies.cs)
    /// are generated under Assets/Prefabs, materials under Assets/Materials. Safe to re-run (idempotent; existing assets
    /// are updated in place so GUIDs stay stable).</summary>
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
            // M35: SceneBuilder owns geometry, spawns and NavMesh — not UI.
            // Splash/MainMenu scene files are not regenerated (UI lives in Ship/Bastion).
            BuildShipScene();
            BuildBastionScene();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] EditorBuildSettings.scenes = Splash, MainMenu, Ship, Bastion (geometry only).");
        }

        // Build All Scenes is the only entry point. A Bastion-only item existed until
        // 2026-08-04 and was a narrower duplicate of it: the full build takes seconds, and a
        // Bastion rebuilt without re-registering build settings is exactly the state that
        // hides gotcha 4 (Docs/Working-Rules.md, "Tools, scripts and the menu").

        static void RegisterBuildScenes()
        {
            // M35: menu Canvas lives in Ship. Splash/MainMenu stay registered only while those
            // scene files still exist so old PlayMode tests can load them; boot prefers Ship via
            // SceneFlow.FrontEndScene when MainMenu is empty of controllers.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SplashScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(ShipScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };
        }

        /// <summary>uGUI buttons need an EventSystem when geometry-only scenes are opened alone.</summary>
        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        /// <summary>Logs if an expected child is missing.</summary>
        static void EnsureNestedNamed(GameObject host, string childName, string label)
        {
            if (host == null) return;
            var t = host.transform.Find(childName);
            if (t == null)
            {
                foreach (var tr in host.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name == childName) { t = tr; break; }
                }
            }
            if (t == null)
                Debug.LogWarning($"[SceneBuilder] {label}: '{childName}' missing under {host.name}.");
        }

        /// <summary>Standalone solid-color camera for the non-gameplay scenes.</summary>
        static void MakeSoloCamera(Color background)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.AddComponent<AudioListener>();
        }

        // ---------- Bastion ----------

        static readonly string[] BastionUiRootNames =
        {
            "Canvas - In Game",
            "Canvas - Pause Menu",
            "BastionHud",
        };

        static void BuildBastionScene()
        {
            var preserved = CaptureUiRoots(ScenePath, BastionUiRootNames);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureEventSystem();
            Palette pal = CreatePalette();
            EnemyPrefabs enemies = CreateEnemyPrefabs(pal); // SceneBuilder.Enemies.cs

            BuildLighting();
            GameObject env = BuildBastion(pal);
            BuildHatch(pal, env.transform);
            SpawnPoint[] breaches = BuildSpawnPoints(pal);
            BuildPlayerRig(pal);
            // Restore UI before GameSystems so HudController on the authored canvas is found.
            RestoreUiRoots(preserved);
            BuildGameSystems(enemies, breaches);
            BakeNavMesh(env);
            EnsureEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SceneBuilder] Built {ScenePath}: EventSystem + bastion interior, hatch, 3 breaches, player rig, GameSystems, NavMesh (UI roots preserved if present).");
        }

        /// <summary>M35: geometry rebuild must not wipe authored Shift UI. Clone matching roots
        /// with HideAndDontSave before NewScene, then re-parent into the new scene.</summary>
        static GameObject[] CaptureUiRoots(string scenePath, string[] rootNames)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                return System.Array.Empty<GameObject>();

            var open = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var captured = new System.Collections.Generic.List<GameObject>();
            foreach (var root in open.GetRootGameObjects())
            {
                if (!IsPreservedUiRoot(root.name, rootNames)) continue;
                var clone = UnityEngine.Object.Instantiate(root);
                clone.name = root.name;
                SetHideFlagsRecursive(clone, HideFlags.HideAndDontSave);
                captured.Add(clone);
            }
            return captured.ToArray();
        }

        static void SetHideFlagsRecursive(GameObject go, HideFlags flags)
        {
            if (go == null) return;
            go.hideFlags = flags;
            var transforms = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                    transforms[i].gameObject.hideFlags = flags;
            }
        }

        static bool IsPreservedUiRoot(string name, string[] rootNames)
        {
            foreach (var n in rootNames)
                if (name == n) return true;
            if (name == "Canvas") return true;
            return false;
        }

        static void RestoreUiRoots(GameObject[] preserved)
        {
            if (preserved == null || preserved.Length == 0) return;
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var clone in preserved)
            {
                if (clone == null) continue;
                var go = UnityEngine.Object.Instantiate(clone);
                go.name = clone.name;
                SetHideFlagsRecursive(go, HideFlags.None);
                EditorSceneManager.MoveGameObjectToScene(go, scene);
                UnityEngine.Object.DestroyImmediate(clone);
            }
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
        ///   Hatch bay (south, z -22..-10)  — Hatch + player spawn.
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

        /// <summary>The defended objective: a loud orange block at the south end, wearing
        /// HatchStation (enemy target / sim proxy) and RepairInteractable (hold E).</summary>
        static void BuildHatch(Palette pal, Transform parent)
        {
            GameObject hatch = Box("Hatch", new Vector3(0f, 1.5f, -20.5f), new Vector3(4f, 3f, 1.5f), pal.Hatch, parent);
            hatch.AddComponent<HatchStation>();
            var repair = hatch.AddComponent<RepairInteractable>();
            repair.station = hatch.GetComponent<HatchStation>();
        }

        /// <summary>Three spawn points at the far ends of the chambers, deep in the bastion.</summary>
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

        /// <summary>Player spawns in the hatch bay, right next to home. Combat rig: gun on.</summary>
        static void BuildPlayerRig(Palette pal)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            UnityEngine.Object.DestroyImmediate(player.GetComponent<Collider>());
            player.transform.position = new Vector3(0f, 1.1f, -16f);
            player.GetComponent<Renderer>().sharedMaterial = pal.Player;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PlayerGun>();
            camGo.AddComponent<PlayerAbility>();
            camGo.AddComponent<Interactor>();

            var fpc = player.AddComponent<FirstPersonController>();
            fpc.cameraTransform = camGo.transform;
            player.AddComponent<PlayerHealth>();
        }

        static void BuildGameSystems(EnemyPrefabs enemies, SpawnPoint[] breaches)
        {
            var go = new GameObject("GameSystems");
            go.AddComponent<GameController>();
            // M35: Bastion UI (Canvas - In Game / Pause) is authored in Bastion.unity — not spawned here.
            // HudController must already exist on that authored hierarchy after the Shift HUD copy.
            if (UnityEngine.Object.FindFirstObjectByType<HudController>(FindObjectsInactive.Include) == null)
            {
                var hudHost = new GameObject("BastionHud");
                hudHost.transform.SetParent(go.transform, false);
                hudHost.AddComponent<HudController>();
                Debug.LogWarning(
                    "[SceneBuilder] No HudController in scene — stub host added. Copy Simple HUD canvases into Bastion.unity (M35 C).");
            }
            go.AddComponent<DevConsole>();
            var spawner = go.AddComponent<WaveSpawner>();
            // All three kinds, or the spawner falls back to the default prefab and the wave is
            // the right fight in the wrong colours.
            spawner.enemyPrefab = enemies.Default;
            spawner.hunterPrefab = enemies.Hunter;
            spawner.ramPrefab = enemies.Ram;
            spawner.spawnPoints = breaches;
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
