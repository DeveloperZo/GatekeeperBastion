using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Graybox stair wells after kit Rebuild. A 0.05 m box at atan(rise/run)
    /// (~32° for StoreyHeight 5 over 8 m) stays under CharacterController slopeLimit 45.
    /// Kit stair MeshColliders wall the capsule; this walk box is the surface. Call after
    /// RebuildAllSegments + Settle or floor tiles come back.</summary>
    static class BastionSketchStairs
    {
        const float WalkThickness = 0.05f;
        const float MinRun = 8f;
        const float WalkWidth = 4f;
        const float WellPad = 0.3f;
        const string WalkPrefix = "GStairWalk_";

        public static int Emit(BastionSketch sketch, Dictionary<string, GameObject> roomObjects)
        {
            if (sketch == null || roomObjects == null)
                return 0;

            float rise = BastionKit.StoreyHeight;
            float run = MinRun;
            float angle = Mathf.Atan(rise / run) * Mathf.Rad2Deg;
            float hypot = Mathf.Sqrt(rise * rise + run * run);
            int placed = 0;

            for (int i = 0; i < sketch.Stairs.Count; i++)
            {
                BastionSketchStair stair = sketch.Stairs[i];
                if (!roomObjects.TryGetValue(stair.LowerId, out GameObject lower) || lower == null)
                    continue;
                if (!roomObjects.TryGetValue(stair.UpperId, out GameObject upper) || upper == null)
                    continue;

                string walkName = WalkPrefix + stair.LowerId + "_" + stair.UpperId;
                DestroyNamedUnder(lower.transform, walkName);
                DestroyNamedUnder(upper.transform, walkName);

                Vector3 lowerPos = lower.transform.position;
                float cx = lowerPos.x;
                float cz = lowerPos.z;
                float lowerY = lowerPos.y;
                float upperY = upper.transform.position.y;

                // North of centre, away from R1's south hatch wall. Foot at room centre.
                Vector3 foot = new Vector3(cx, lowerY, cz);
                Vector3 head = new Vector3(cx, upperY, cz + run);
                Vector3 mid = (foot + head) * 0.5f;

                float halfW = WalkWidth * 0.5f + WellPad;
                var well = new Bounds();
                well.SetMinMax(
                    new Vector3(cx - halfW, lowerY - 0.6f, cz - WellPad),
                    new Vector3(cx + halfW, upperY + 0.6f, cz + run + WellPad));

                DisableOverlappingTiles(upper, BastionSegment.GroupFloors, "FloorTile", well);
                DisableOverlappingTiles(lower, BastionSegment.GroupCeiling, "CeilingTile", well);
                PunchPhysicsFloor(upper, well);

                PlaceWalkBox(lower.transform, walkName, mid, angle, hypot);
                placed++;
            }

            return placed;
        }

        static void PlaceWalkBox(Transform parent, string walkName, Vector3 mid, float angle, float hypot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = walkName;
            go.layer = 0;
            go.transform.position = mid;
            go.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);
            go.transform.localScale = new Vector3(WalkWidth, WalkThickness, hypot);
            go.transform.SetParent(parent, true);
            go.AddComponent<BastionGeneratedGeom>();

            const StaticEditorFlags flags =
                StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.NavigationStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        static void DisableOverlappingTiles(
            GameObject room,
            string groupName,
            string nameContains,
            Bounds well)
        {
            Transform group = FindGeomGroup(room, groupName);
            if (group == null)
                return;

            Renderer[] renderers = group.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                    continue;
                if (r.gameObject.name.IndexOf(nameContains, System.StringComparison.Ordinal) < 0)
                    continue;
                if (!r.bounds.Intersects(well))
                    continue;
                r.gameObject.SetActive(false);
            }
        }

        static void PunchPhysicsFloor(GameObject upper, Bounds well)
        {
            Transform floors = FindGeomGroup(upper, BastionSegment.GroupFloors);
            if (floors == null)
                return;

            Transform genFloor = floors.Find(BastionSegment.GenPrefix + "Floor");
            if (genFloor == null || !genFloor.gameObject.activeSelf)
                return;

            var col = genFloor.GetComponent<Collider>();
            Bounds floorB = col != null
                ? col.bounds
                : new Bounds(genFloor.position, genFloor.lossyScale);

            float fx0 = floorB.min.x;
            float fx1 = floorB.max.x;
            float fz0 = floorB.min.z;
            float fz1 = floorB.max.z;
            float wx0 = Mathf.Max(well.min.x, fx0);
            float wx1 = Mathf.Min(well.max.x, fx1);
            float wz0 = Mathf.Max(well.min.z, fz0);
            float wz1 = Mathf.Min(well.max.z, fz1);
            if (wx1 - wx0 < 0.05f || wz1 - wz0 < 0.05f)
                return;

            float yCenter = floorB.center.y;
            float ySize = floorB.size.y;
            genFloor.gameObject.SetActive(false);

            AddFloorSlab(floors, "Gen_FloorWell_S", fx0, fx1, fz0, wz0, yCenter, ySize);
            AddFloorSlab(floors, "Gen_FloorWell_N", fx0, fx1, wz1, fz1, yCenter, ySize);
            AddFloorSlab(floors, "Gen_FloorWell_W", fx0, wx0, wz0, wz1, yCenter, ySize);
            AddFloorSlab(floors, "Gen_FloorWell_E", wx1, fx1, wz0, wz1, yCenter, ySize);
        }

        static void AddFloorSlab(
            Transform floors,
            string name,
            float xMin,
            float xMax,
            float zMin,
            float zMax,
            float yCenter,
            float ySize)
        {
            if (xMax - xMin < 0.05f || zMax - zMin < 0.05f)
                return;

            Transform existing = floors.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject(name);
            go.transform.SetParent(floors, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, yCenter, (zMin + zMax) * 0.5f);
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = new Vector3(xMax - xMin, ySize, zMax - zMin);
            var box = go.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = Vector3.one;
            go.AddComponent<BastionGeneratedGeom>();
        }

        static Transform FindGeomGroup(GameObject room, string groupName)
        {
            var segment = room.GetComponent<BastionSegment>();
            Transform geom = segment != null ? segment.FindGeomRoot() : room.transform.Find("_geom");
            if (geom == null)
                return null;
            return geom.Find(groupName);
        }

        static void DestroyNamedUnder(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root)
                    continue;
                if (t.name != name)
                    continue;
                Object.DestroyImmediate(t.gameObject);
            }
        }
    }
}
