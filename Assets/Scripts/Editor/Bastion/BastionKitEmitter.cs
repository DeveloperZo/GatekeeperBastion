using System;
using System.IO;
using GatekeeperBastion.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Emits the five bastion kit prefabs under Assets/Prefabs/Bastion/Kit/. Trigger via
    /// gkb_bastion_kit_request.txt at the project root or
    /// -executeMethod GatekeeperBastion.EditorTools.BastionKitEmitter.BuildFromBatch.
    /// Also sweeps orphan Gen_* litter left by earlier emits (gkb_bastion_kit_cleanup_request.txt).</summary>
    [InitializeOnLoad]
    public static class BastionKitEmitter
    {
        const string KitDir = "Assets/Prefabs/Bastion/Kit";
        const string MaterialDir = "Assets/Materials";

        static readonly (BastionBlockKind kind, string prefabName)[] Blocks =
        {
            (BastionBlockKind.Quarter, "Quarter"),
            (BastionBlockKind.Half, "Half"),
            (BastionBlockKind.Full, "Full"),
            (BastionBlockKind.OpenArea, "OpenArea"),
            (BastionBlockKind.ControlRoom, "ControlRoom"),
        };

        static double _nextSentinelPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_bastion_kit_request.txt");
        static string CleanupSentinelPath => Path.Combine(Root, "gkb_bastion_kit_cleanup_request.txt");

        static BastionKitEmitter()
        {
            EditorApplication.update += PollSentinel;
        }

        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextSentinelPoll) return;
            _nextSentinelPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            if (File.Exists(CleanupSentinelPath))
            {
                try { File.Delete(CleanupSentinelPath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[BastionKitEmitter] cleanup sentinel: " + e.Message);
                    return;
                }
                CleanupOrphanGeneratedGeom();
            }

            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch (Exception e)
            {
                Debug.LogWarning("[BastionKitEmitter] sentinel: " + e.Message);
                return;
            }
            BuildKitPrefabs();
        }

        public static void BuildKitPrefabs()
        {
            EnsureFolders();
            Material floor = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Mat_Floor.mat");
            Material wall = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Mat_Wall.mat");

            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                foreach ((BastionBlockKind kind, string prefabName) in Blocks)
                {
                    string path = KitDir + "/" + prefabName + ".prefab";
                    var root = new GameObject(prefabName);
                    var segment = root.AddComponent<BastionSegment>();
                    segment.BlockKind = kind;
                    segment.North = BastionSideKind.Wall;
                    segment.South = BastionSideKind.Wall;
                    segment.East = BastionSideKind.Wall;
                    segment.West = BastionSideKind.Wall;
                    if (floor != null) segment.FloorMaterial = floor;
                    if (wall != null) segment.WallMaterial = wall;
                    // Prefab asset is component-only. Meshes rebuild on scene instances (OnEnable).
                    segment.ClearGeneratedHierarchy();

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    UnityEngine.Object.DestroyImmediate(root);
                    Debug.Log("[BastionKitEmitter] Wrote " + path);
                }
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }

            CleanupOrphanGeneratedGeom();
            AssetDatabase.SaveAssets();
            Debug.Log("[BastionKitEmitter] Kit prefab build done (" + Blocks.Length + " blocks).");
        }

        /// <summary>Removes Gen_* / BastionGeneratedGeom objects that are not under a BastionSegment
        /// (litter from OnValidate + CreatePrimitive during prefab emit).</summary>
        public static void CleanupOrphanGeneratedGeom()
        {
            int removed = 0;
            BastionGeneratedGeom[] markers = UnityEngine.Object.FindObjectsByType<BastionGeneratedGeom>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < markers.Length; i++)
            {
                BastionGeneratedGeom marker = markers[i];
                if (marker == null)
                    continue;

                BastionSegment owner = marker.GetComponentInParent<BastionSegment>();
                if (owner != null)
                    continue;

                UnityEngine.Object.DestroyImmediate(marker.gameObject);
                removed++;
            }

            // Also catch Gen_* roots with no marker (partial litter).
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject root = roots[i];
                    if (root == null)
                        continue;
                    if (!root.name.StartsWith("Gen_", StringComparison.Ordinal))
                        continue;
                    if (root.GetComponentInParent<BastionSegment>() != null)
                        continue;
                    UnityEngine.Object.DestroyImmediate(root);
                    removed++;
                }
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("[BastionKitEmitter] Removed " + removed + " orphan kit mesh(es) from the open scene.");
            }
            else
                Debug.Log("[BastionKitEmitter] No orphan kit meshes found.");
        }

        /// <summary>Batchmode entry: emit kit prefabs then exit the editor with code 0.</summary>
        public static void BuildFromBatch()
        {
            BuildKitPrefabs();
            EditorApplication.Exit(0);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Bastion"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Bastion");
            if (!AssetDatabase.IsValidFolder(KitDir))
                AssetDatabase.CreateFolder("Assets/Prefabs/Bastion", "Kit");
        }
    }
}
