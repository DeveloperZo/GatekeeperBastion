using System;
using GatekeepersProtocol.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Regroups already-emitted rooms in the open scene: each segment's flat Gen_*
    /// pieces move under Walls/Floors/Ceiling without a rebuild, so hand edits survive.
    /// Rebuilds emit grouped natively — this exists for scenes authored before grouping
    /// landed. Idempotent; world transforms untouched (groups sit at local zero).</summary>
    public static class BastionGeomGroupTool
    {
        [MenuItem("GatekeepersProtocol/Build/Group Bastion Geom In Open Scene", false, 62)]
        public static void GroupOpenScene()
        {
            BastionSegment[] segments = UnityEngine.Object.FindObjectsByType<BastionSegment>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int moved = 0;
            for (int s = 0; s < segments.Length; s++)
            {
                Transform geom = segments[s].FindGeomRoot();
                if (geom == null)
                    continue;
                for (int i = geom.childCount - 1; i >= 0; i--)
                {
                    Transform child = geom.GetChild(i);
                    if (!child.name.StartsWith(BastionSegment.GenPrefix, StringComparison.Ordinal))
                        continue;
                    Transform group = BastionSegment.EnsureGeomGroup(
                        geom, BastionSegment.GeomGroupNameFor(child.name));
                    child.SetParent(group, true);
                    moved++;
                }
            }

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[BastionGeomGroupTool] Grouped " + moved + " piece(s) across "
                + segments.Length + " segment(s) in " + scene.name + ".");
        }

        /// <summary>Recovery: unpacks leftover pack instances under every segment's _geom.
        /// Emit and Rebuild already unpack Completely; this is for scenes that still have
        /// nested links. One-way: pack updates stop propagating — that is the point.</summary>
        [MenuItem("GatekeepersProtocol/Build/Unpack Bastion Geom In Open Scene", false, 63)]
        public static void UnpackOpenScene()
        {
            BastionSegment[] segments = UnityEngine.Object.FindObjectsByType<BastionSegment>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int unpacked = 0;
            for (int s = 0; s < segments.Length; s++)
            {
                Transform geom = segments[s].FindGeomRoot();
                if (geom == null)
                    continue;
                Transform[] all = geom.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    GameObject go = all[i].gameObject;
                    if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                        continue;
                    PrefabUtility.UnpackPrefabInstance(
                        go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    unpacked++;
                }
            }

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[BastionGeomGroupTool] Unpacked " + unpacked + " prefab instance(s) across "
                + segments.Length + " segment(s) in " + scene.name + ".");
        }
    }
}
