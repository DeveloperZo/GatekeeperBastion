using System.Collections.Generic;
using GatekeepersProtocol.Game;
using Michsky.UI.Shift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Authors the two ends of the range door: the way in, on Ship - CoOp, and the way
    /// back, behind the range's firing line.
    ///
    /// The way in is a station. Its body is the slab; its panel is the ARMORY's panel cloned
    /// and stripped to the shared chrome plus the rows, so the door's loadout panel lands in
    /// the same chrome the four stations use without a second authored panel to drift. The way
    /// back stays a plain <see cref="RangeDoor"/> slab.
    ///
    /// A builder rather than a hand edit because Range.unity is regenerated wholesale by
    /// <c>BuildRangeInPlace</c> — a door dragged in by hand would be gone on the next rebuild —
    /// and because Ship - CoOp is usually open in front of somebody. Re-runnable: an existing
    /// door or panel of the same name is replaced, so moving one is a re-run.</summary>
    public static partial class SceneBuilder
    {
        const string ShipCoopScenePathForDoor = "Assets/Scenes/Ship - CoOp.unity";
        const string RangeDoorName = "Range Door";
        const string RangePanelName = "PRACTICE RANGE";

        /// <summary>The partition wall that closes the ARMORY's bay (GPart1_250_100, inner
        /// face z 25.0, x 5–10), 2.5 m to the station's left. The station's own wall (x 10)
        /// has no room: the airlock Hatch_1 and its 4 m frame fill z 15.5–19.5, a window sits at
        /// z 20.25 and the ARMORY desk spans z 21.7–23.3. The first cut at (9.2, 17.5) sat in the
        /// airlock; the run before it at (-9.2, 7.5) hung outside the hull over no floor at
        /// all, which is why nothing could reach the range on 2026-09-03.</summary>
        static readonly Vector3 ShipDoorPos = new Vector3(7.5f, 1.15f, 24.8f);

        /// <summary>The slab's width runs along z; turned to run along x for a wall across z.</summary>
        const float ShipDoorYaw = 90f;

        /// <summary>How far the way back stands proud of the range's near wall — the whole
        /// slab, plus a hair. The first cut centred it ON the wall plane, which put all but 15 cm
        /// of it inside the wall and left nothing to look at or reach.</summary>
        const float RangeDoorInset = 0.02f;

        static readonly Vector3 DoorSize = new Vector3(0.3f, 2.3f, 1.4f);

        /// <summary>ARMORY body elements the door's panel has no use for. Read off the cloned
        /// ArmoryPanelView by field name rather than by object name, so the strip follows the
        /// authored bindings whatever the objects are called.</summary>
        static readonly string[] ArmoryBodyFields =
        {
            "_primaryButton", "_secondaryButton", "_equippedLine", "_statLine", "_rotateHint",
            "_ringNote", "_rowsMissing", "_catalogHeading", "_backButton",
        };

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

            AuthorShipDoorStation();
            AuthorRangeReturnDoorInScene();
        }

        /// <summary>The way in: a slab carrying <see cref="RangeDoorStation"/>, wired to the
        /// same canvas, panel manager and ship controller the ARMORY uses, with its own panel
        /// cloned from the ARMORY's.</summary>
        static void AuthorShipDoorStation()
        {
            if (!System.IO.File.Exists(ShipCoopScenePathForDoor))
            {
                Debug.LogWarning("[SceneBuilder] Range door: no scene at " + ShipCoopScenePathForDoor);
                return;
            }

            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ShipCoopScenePathForDoor, OpenSceneMode.Single);

                var armory = Object.FindFirstObjectByType<ArmoryStation>(FindObjectsInactive.Include);
                if (armory == null)
                {
                    Debug.LogError("[SceneBuilder] Range door refused — no ArmoryStation in " + ShipCoopScenePathForDoor);
                    return;
                }
                var soArmory = new SerializedObject(armory);
                var armoryView = soArmory.FindProperty("_view").objectReferenceValue as StationPanelView;
                var canvasRoot = soArmory.FindProperty("_canvasRoot").objectReferenceValue as GameObject;
                var panels = soArmory.FindProperty("_panels").objectReferenceValue as MainPanelManager;
                var ship = soArmory.FindProperty("_ship").objectReferenceValue as ShipController;
                if (armoryView == null || canvasRoot == null || panels == null || ship == null)
                {
                    Debug.LogError("[SceneBuilder] Range door refused — ARMORY station is missing a serialized ref "
                        + "(_view / _canvasRoot / _panels / _ship); nothing was touched.");
                    return;
                }

                // Replace rather than stack, so moving the door is a re-run of this builder.
                RemoveRoot(scene, RangeDoorName);
                RemoveChild(armoryView.transform.parent, RangePanelName);

                GameObject panel = ClonePanel(armoryView.gameObject);
                PanelRowView[] rows = StripArmoryBody(panel);
                RegisterPanel(panels, panel);

                GameObject door = MakeSlab(ShipDoorPos, ShipDoorYaw);
                var station = door.AddComponent<RangeDoorStation>();
                var so = new SerializedObject(station);
                so.FindProperty("_view").objectReferenceValue = panel.GetComponent<StationPanelView>();
                so.FindProperty("_canvasRoot").objectReferenceValue = canvasRoot;
                so.FindProperty("_panels").objectReferenceValue = panels;
                so.FindProperty("_ship").objectReferenceValue = ship;
                var rowsProp = so.FindProperty("_rows");
                rowsProp.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                    rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ShipCoopScenePathForDoor);
                Debug.Log("[SceneBuilder] Range door station authored into " + ShipCoopScenePathForDoor
                    + " at " + ShipDoorPos + " — panel '" + RangePanelName + "' cloned from the ARMORY's with "
                    + rows.Length + " rows");
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        static GameObject ClonePanel(GameObject source)
        {
            // Instantiate under the same parent keeps the local rect, so the clone sits exactly
            // where the ARMORY panel sits among Main Panels.
            GameObject clone = Object.Instantiate(source, source.transform.parent);
            clone.name = RangePanelName;
            clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            clone.SetActive(source.activeSelf);
            return clone;
        }

        /// <summary>Drop the ARMORY body view and every element only it bound, and the wheel
        /// host; keep the catalog rows and grow them to <see cref="RangeDoorStation.RowCount"/>
        /// by cloning the last one.</summary>
        static PanelRowView[] StripArmoryBody(GameObject panel)
        {
            var rows = new List<PanelRowView>(RangeDoorStation.RowCount);
            var body = panel.GetComponent<ArmoryPanelView>();
            if (body != null)
            {
                var soBody = new SerializedObject(body);
                for (int i = 0; i < ArmoryBodyFields.Length; i++)
                    StripReferenced(soBody.FindProperty(ArmoryBodyFields[i]));
                var catalog = soBody.FindProperty("_catalogRows");
                for (int i = 0; catalog != null && i < catalog.arraySize; i++)
                {
                    var row = catalog.GetArrayElementAtIndex(i).objectReferenceValue as PanelRowView;
                    if (row != null) rows.Add(row);
                }
                Object.DestroyImmediate(body);
            }

            var view = panel.GetComponent<StationPanelView>();
            if (view != null)
            {
                var soView = new SerializedObject(view);
                StripReferenced(soView.FindProperty("_wheelSlot"));
                soView.FindProperty("_wheelSlot").objectReferenceValue = null;
                soView.FindProperty("_wheel").objectReferenceValue = null;
                soView.ApplyModifiedPropertiesWithoutUndo();
            }

            if (rows.Count == 0)
            {
                Debug.LogError("[SceneBuilder] Range door: the ARMORY panel carried no catalog rows to clone — "
                    + "the station will refuse to bind.");
                return rows.ToArray();
            }
            while (rows.Count < RangeDoorStation.RowCount)
            {
                PanelRowView last = rows[rows.Count - 1];
                GameObject extra = Object.Instantiate(last.gameObject, last.transform.parent);
                extra.name = "Row " + (rows.Count + 1);
                extra.transform.SetSiblingIndex(last.transform.GetSiblingIndex() + 1);
                rows.Add(extra.GetComponent<PanelRowView>());
            }
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].name = "Row " + (i + 1);
                rows[i].gameObject.SetActive(true);
            }
            return rows.ToArray();
        }

        /// <summary>Destroy the object a serialized reference points at. A component reference
        /// takes its whole object; an already-destroyed one (a child of something stripped
        /// earlier) is skipped. A clone keeps prefab links, and DestroyImmediate on a child
        /// inside a prefab instance throws, so any instance is unpacked first.</summary>
        static void StripReferenced(SerializedProperty prop)
        {
            if (prop == null) return;
            Object target = prop.objectReferenceValue;
            if (target == null) return;
            GameObject go = target as GameObject;
            if (go == null)
            {
                var c = target as Component;
                if (c != null) go = c.gameObject;
            }
            if (go == null) return;
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            Object.DestroyImmediate(go);
        }

        /// <summary>The MainPanelManager list is where the ARMORY's panel is registered;
        /// StationTerminal disables the manager while a panel is open, so the entry is for
        /// the manager's own culling and the scene gate, not for opening.</summary>
        static void RegisterPanel(MainPanelManager panels, GameObject panel)
        {
            var so = new SerializedObject(panels);
            var list = so.FindProperty("panels");
            if (list == null || !list.isArray)
            {
                Debug.LogWarning("[SceneBuilder] Range door: MainPanelManager.panels missing — panel not registered");
                return;
            }
            int at = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("panelName").stringValue == RangePanelName)
                {
                    at = i;
                    break;
                }
            }
            if (at < 0)
            {
                list.arraySize++;
                at = list.arraySize - 1;
            }
            var item = list.GetArrayElementAtIndex(at);
            item.FindPropertyRelative("panelName").stringValue = RangePanelName;
            item.FindPropertyRelative("panelObject").objectReferenceValue = panel;
            item.FindPropertyRelative("buttonObject").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RemoveRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == name)
                    Object.DestroyImmediate(root);
            }
        }

        static void RemoveChild(Transform parent, string name)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == name)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>The Interactor raycasts against colliders; CreatePrimitive already gives a
        /// BoxCollider, and the slab is a wall panel rather than something to walk into.</summary>
        static GameObject MakeSlab(Vector3 pos, float yaw)
        {
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = RangeDoorName;
            door.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            door.transform.localScale = DoorSize;
            return door;
        }

        /// <summary>The way back, into an already-built Range.unity: re-measure the hall's floor
        /// and place the slab off its near wall. The range build authors the same door itself,
        /// so this path exists for moving the door without rebuilding the lane.</summary>
        static void AuthorRangeReturnDoorInScene()
        {
            if (!System.IO.File.Exists(RangeScenePath))
            {
                Debug.LogWarning("[SceneBuilder] Range door: no scene at " + RangeScenePath);
                return;
            }

            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(RangeScenePath, OpenSceneMode.Single);
                GameObject hall = GameObject.Find("Range Hall");
                Bounds floor;
                if (hall == null || !HallFloorBounds(hall, out floor))
                {
                    Debug.LogError("[SceneBuilder] Range door refused — no Range Hall floor to measure in "
                        + RangeScenePath + "; build the Range Scene first.");
                    return;
                }
                AuthorRangeReturnDoor(floor.min.x, floor.center.z);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, RangeScenePath);
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        /// <summary>The way back in the OPEN scene: a hold-to-load slab standing proud of the
        /// near wall at the lane's centre, behind the firing line, so the player turns away
        /// from the targets to leave. Framed in wall colour so it reads as a doorway from the
        /// firing line and not as a stripe on the wall. Replaces any door of the same name.</summary>
        static void AuthorRangeReturnDoor(float floorMinX, float laneZ)
        {
            RemoveRoot(SceneManager.GetActiveScene(), RangeDoorName);
            RemoveRoot(SceneManager.GetActiveScene(), RangeDoorName + " Frame");

            var pos = new Vector3(floorMinX + DoorSize.x * 0.5f + RangeDoorInset, DoorSize.y * 0.5f, laneZ);
            GameObject door = MakeSlab(pos, 0f);
            var rd = door.AddComponent<RangeDoor>();
            var so = new SerializedObject(rd);
            so.FindProperty("_returnsToShip").boolValue = true;
            so.FindProperty("_label").stringValue = "Hold E — back to the ship";
            so.ApplyModifiedPropertiesWithoutUndo();

            // The frame is a sibling root at the origin, not a child: a child of the slab would
            // inherit its scale, and Box places children by LOCAL position, so world is local here.
            var frame = new GameObject(RangeDoorName + " Frame");
            frame.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Material wall = CreatePalette().Wall;
            float half = DoorSize.z * 0.5f;
            Box("Frame_Top", new Vector3(pos.x, DoorSize.y + 0.15f, pos.z),
                new Vector3(0.45f, 0.3f, DoorSize.z + 0.9f), wall, frame.transform);
            Box("Frame_L", new Vector3(pos.x, DoorSize.y * 0.5f, pos.z + half + 0.15f),
                new Vector3(0.45f, DoorSize.y, 0.3f), wall, frame.transform);
            Box("Frame_R", new Vector3(pos.x, DoorSize.y * 0.5f, pos.z - half - 0.15f),
                new Vector3(0.45f, DoorSize.y, 0.3f), wall, frame.transform);

            Debug.Log("[SceneBuilder] Range door authored at " + pos.ToString("0.00")
                + " (returns to ship), " + (pos.x - floorMinX).ToString("0.00") + " m off the near wall");
        }
    }
}
