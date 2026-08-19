using System;
using System.IO;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Emits the five bastion kit prefabs under Assets/Prefabs/Bastion/Kit/. Trigger via
    /// gkb_bastion_kit_request.txt at the project root or
    /// -executeMethod GatekeepersProtocol.EditorTools.BastionKitEmitter.BuildFromBatch.
    /// Also sweeps orphan Gen_* litter left by earlier emits (gkb_bastion_kit_cleanup_request.txt).</summary>
    [InitializeOnLoad]
    public static class BastionKitEmitter
    {
        const string KitDir = "Assets/Prefabs/Bastion/Kit";
        const string MaterialDir = "Assets/Materials";
        const string PackBuildingsDir = "Assets/Synty/PolygonSciFiSpace/Prefabs/Buildings";
        const string PackPropsDir = "Assets/Synty/PolygonSciFiSpace/Prefabs/Props";

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

            // Pack piece prefabs, resolved once and serialized onto every segment — runtime
            // rebuilds have no AssetDatabase. Any miss aborts the whole build before a single
            // prefab is written: half a kit reads worse than none.
            string floorPiecePath = PackBuildingsDir + "/" + BastionMeshMap.Floor + ".prefab";
            string ceilingPiecePath = PackBuildingsDir + "/" + BastionMeshMap.Ceiling + ".prefab";
            string wallPiecePath = PackBuildingsDir + "/" + BastionMeshMap.Wall + ".prefab";
            string doorframePiecePath = PackBuildingsDir + "/" + BastionMeshMap.Doorframe + ".prefab";
            string windowPiecePath = PackBuildingsDir + "/" + BastionMeshMap.Window + ".prefab";
            string cornerPiecePath = PackBuildingsDir + "/" + BastionMeshMap.CornerPillar + ".prefab";
            string ceilingLightPiecePath = PackPropsDir + "/" + BastionMeshMap.CeilingLight + ".prefab";

            GameObject floorPiece = AssetDatabase.LoadAssetAtPath<GameObject>(floorPiecePath);
            GameObject ceilingPiece = AssetDatabase.LoadAssetAtPath<GameObject>(ceilingPiecePath);
            GameObject wallPiece = AssetDatabase.LoadAssetAtPath<GameObject>(wallPiecePath);
            GameObject doorframePiece = AssetDatabase.LoadAssetAtPath<GameObject>(doorframePiecePath);
            GameObject windowPiece = AssetDatabase.LoadAssetAtPath<GameObject>(windowPiecePath);
            GameObject cornerPiece = AssetDatabase.LoadAssetAtPath<GameObject>(cornerPiecePath);
            GameObject ceilingLightPiece = AssetDatabase.LoadAssetAtPath<GameObject>(ceilingLightPiecePath);

            string missing = null;
            if (floorPiece == null) missing = AppendMissing(missing, floorPiecePath);
            if (ceilingPiece == null) missing = AppendMissing(missing, ceilingPiecePath);
            if (wallPiece == null) missing = AppendMissing(missing, wallPiecePath);
            if (doorframePiece == null) missing = AppendMissing(missing, doorframePiecePath);
            if (windowPiece == null) missing = AppendMissing(missing, windowPiecePath);
            if (cornerPiece == null) missing = AppendMissing(missing, cornerPiecePath);
            if (ceilingLightPiece == null) missing = AppendMissing(missing, ceilingLightPiecePath);
            if (missing != null)
            {
                Debug.LogError(
                    "[BastionKitEmitter] Missing pack piece prefab(s): " + missing
                    + " — kit build aborted, no prefabs written.");
                return;
            }

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
                    segment.FloorPiece = floorPiece;
                    segment.CeilingPiece = ceilingPiece;
                    segment.WallPiece = wallPiece;
                    segment.DoorframePiece = doorframePiece;
                    segment.WindowPiece = windowPiece;
                    segment.CornerPiece = cornerPiece;
                    segment.CeilingLightPiece = ceilingLightPiece;
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

        static string AppendMissing(string missing, string path)
        {
            return missing == null ? path : missing + ", " + path;
        }

        /// <summary>Removes Gen_* litter that is not under a BastionSegment (OnValidate
        /// + CreatePrimitive during prefab emit). Never touches Authored rooms — those
        /// carry BastionGeneratedGeom on the room root, and a missing script during
        /// compile would otherwise destroy the whole room.</summary>
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
                if (IsUnderAuthored(marker.transform))
                    continue;
                if (!marker.gameObject.name.StartsWith(BastionSegment.GenPrefix, StringComparison.Ordinal))
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

        static bool IsUnderAuthored(Transform t)
        {
            while (t != null)
            {
                if (t.name == BastionSegment.AuthoredParentName)
                    return true;
                t = t.parent;
            }
            return false;
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
