using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Numbered sketch hatch marks become <see cref="ShipHatchDoor"/> children
    /// on the south (dock) wall. Bare H stays a named marker. One HatchStation already
    /// exists in a copied Bastion scene — this snaps it onto hatch 2, never a second
    /// station, never split HP.</summary>
    static class BastionSketchHatchDoors
    {
        const string HatchOpenMatPath = "Assets/Materials/Mat_Hatch.mat";
        const string HatchSealedMatPath = "Assets/Materials/Mat_HatchSealed.mat";
        const int MiddleHatchId = 2;

        /// <summary>True when a numbered door was created. False means the caller should
        /// emit today's bare H marker.</summary>
        public static bool TryCreate(Transform room, BastionSketchMark mark)
        {
            if (string.IsNullOrEmpty(mark.RefId))
                return false;

            int hatchId;
            if (!int.TryParse(mark.RefId, out hatchId) || hatchId < 1 || hatchId > 3)
                return false;

            var go = new GameObject("Hatch_" + hatchId);
            go.transform.SetParent(room, false);
            go.AddComponent<BastionGeneratedGeom>();
            go.AddComponent<ShipHatchDoor>();
            PlaceOnSouthWall(go.transform, room, hatchId);
            Wire(go, hatchId);
            return true;
        }

        public static void SnapStationToMiddle()
        {
            var station = Object.FindAnyObjectByType<HatchStation>(FindObjectsInactive.Include);
            if (station == null)
                return;

            ShipHatchDoor[] doors = Object.FindObjectsByType<ShipHatchDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform target = null;
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null && doors[i].HatchId == MiddleHatchId)
                {
                    target = doors[i].transform;
                    break;
                }
            }

            if (target == null)
                return;

            Vector3 p = target.position;
            Vector3 cur = station.transform.position;
            station.transform.position = new Vector3(p.x, cur.y, p.z);
        }

        static void PlaceOnSouthWall(Transform hatch, Transform room, int hatchId)
        {
            var segment = room.GetComponent<BastionSegment>();
            BastionBlockKind kind = BastionBlockKind.OpenArea;
            if (segment != null)
                kind = segment.BlockKind;

            float along = 0f;
            if (hatchId == 1) along = -14f;
            else if (hatchId == 3) along = 14f;

            float z = -BastionKit.FootprintDepth(kind) * 0.5f + BastionKit.WallThickness;
            hatch.localPosition = new Vector3(along, 0f, z);
            hatch.localRotation = Quaternion.identity;
        }

        static void Wire(GameObject go, int hatchId)
        {
            var door = go.GetComponent<ShipHatchDoor>();
            if (door == null)
                return;

            Transform existingLeaf = go.transform.Find("Slab");
            GameObject slab;
            if (existingLeaf != null)
            {
                slab = existingLeaf.gameObject;
            }
            else
            {
                slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "Slab";
                slab.transform.SetParent(go.transform, false);
                slab.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                slab.transform.localScale = new Vector3(3.4f, 2.7f, 0.25f);
            }

            Material openMat = AssetDatabase.LoadAssetAtPath<Material>(HatchOpenMatPath);
            Material sealedMat = AssetDatabase.LoadAssetAtPath<Material>(HatchSealedMatPath);
            var leaf = slab.GetComponent<MeshRenderer>();

            var so = new SerializedObject(door);
            so.FindProperty("_hatchId").intValue = hatchId;
            if (leaf != null)
                so.FindProperty("_leaf").objectReferenceValue = leaf;
            if (openMat != null)
                so.FindProperty("_openMaterial").objectReferenceValue = openMat;
            if (sealedMat != null)
                so.FindProperty("_sealedMaterial").objectReferenceValue = sealedMat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
