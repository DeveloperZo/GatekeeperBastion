using GatekeepersProtocol.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Authors the two ends of the range door: the way in, on Ship - CoOp, and the way
    /// back, behind the range's firing line.
    ///
    /// A builder rather than a hand edit because Range.unity is regenerated wholesale by
    /// <c>BuildRangeInPlace</c> — a door dragged in by hand would be gone on the next rebuild —
    /// and because Ship - CoOp is usually open in front of somebody. Re-runnable: an existing
    /// door of the same name is replaced, so moving one is a re-run.</summary>
    public static partial class SceneBuilder
    {
        const string ShipCoopScenePathForDoor = "Assets/Scenes/Ship - CoOp.unity";
        const string RangeDoorName = "Range Door";

        /// <summary>Port side of deck 1, forward of the Fabrication station at z 17.5 and well
        /// clear of the stair well (x -5..-3, z 25..35), so a hull regen that opens the stair
        /// hole does not land on top of it.</summary>
        static readonly Vector3 ShipDoorPos = new Vector3(-9.2f, 1.15f, 7.5f);

        /// <summary>Behind the firing line at x -44, facing back down the lane, so the player
        /// turns away from the targets to leave.</summary>
        static readonly Vector3 RangeDoorPos = new Vector3(-47f, 1.15f, 0f);

        static readonly Vector3 DoorSize = new Vector3(0.3f, 2.3f, 1.4f);

        // No [MenuItem]. The menu fence in Docs/Architecture/README.md requires permission
        // before a row is added, and this builder is driven by the sentinel the other scene
        // builders use — a menu row would be a permanent shared surface bought for nothing.

        public static void BuildRangeDoorsInPlace()
        {
            Scene open = SceneManager.GetActiveScene();
            if (open.isDirty)
            {
                Debug.LogError("[SceneBuilder] Range doors refused — '" + open.name
                    + "' has unsaved changes. Save it first; nothing was touched.");
                return;
            }

            AuthorDoor(ShipCoopScenePathForDoor, ShipDoorPos, returns: false,
                label: "Hold E — practice range");
            AuthorDoor(RangeScenePath, RangeDoorPos, returns: true,
                label: "Hold E — back to the ship");
        }

        static void AuthorDoor(string scenePath, Vector3 pos, bool returns, string label)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning("[SceneBuilder] Range door: no scene at " + scenePath);
                return;
            }

            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // Replace rather than stack, so moving the door is a re-run of this builder.
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root != null && root.name == RangeDoorName)
                        Object.DestroyImmediate(root);
                }

                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = RangeDoorName;
                door.transform.SetPositionAndRotation(pos, Quaternion.identity);
                door.transform.localScale = DoorSize;

                // The Interactor raycasts against colliders; CreatePrimitive already gave us a
                // BoxCollider, and the slab is a wall panel rather than something to walk into.
                var rd = door.AddComponent<RangeDoor>();
                var so = new SerializedObject(rd);
                so.FindProperty("_returnsToShip").boolValue = returns;
                so.FindProperty("_label").stringValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log("[SceneBuilder] Range door authored into " + scenePath
                    + " at " + pos + (returns ? " (returns to ship)" : " (to the range)"));
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }
    }
}
