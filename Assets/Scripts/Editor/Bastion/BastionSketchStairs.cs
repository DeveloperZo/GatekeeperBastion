using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Stair wells after kit Rebuild. Dress is SM_Prop_Stairs_01 (same object
    /// as Ship - CoOp's Stair). Walk is a child box — kit MeshColliders wall the capsule.
    /// Call after RebuildAllSegments, before Settle so the mesh rides the room into
    /// Authored. Rooms already under Authored keep existing Stair_* objects.</summary>
    public static class BastionSketchStairs
    {
        // SM_Prop_Stairs_01 — Tools/ship-hull/build_hull.py MESH['Stairs'].
        const string StairMeshGuid = "3a9167268fb73cb48b6f3226667f05f2";
        const float WalkThickness = 0.35f;
        const float WalkWidth = 2.85f;
        const float WellPad = 0.3f;
        const float BuryDepth = 2f;
        const float LandingDepth = 2f;
        const string WalkPrefix = "GStairWalk_";
        const string LandPrefix = "GStairLand_";
        const string StairPrefix = "Stair_";

        public static int Emit(BastionSketch sketch, Dictionary<string, GameObject> roomObjects)
        {
            if (sketch == null || roomObjects == null)
                return 0;

            GameObject stairSource = LoadStairSource();
            if (stairSource == null)
                Debug.LogError("[BastionSketchStairs] SM_Prop_Stairs_01 (guid "
                    + StairMeshGuid + ") missing — walk boxes only.");

            float rise = BastionKit.StoreyHeight;
            int placed = 0;

            for (int i = 0; i < sketch.Stairs.Count; i++)
            {
                BastionSketchStair stair = sketch.Stairs[i];
                if (!roomObjects.TryGetValue(stair.LowerId, out GameObject lower) || lower == null)
                    continue;
                if (!roomObjects.TryGetValue(stair.UpperId, out GameObject upper) || upper == null)
                    continue;

                string walkName = WalkPrefix + stair.LowerId + "_" + stair.UpperId;
                string stairName = StairPrefix + stair.LowerId + "_" + stair.UpperId;
                // Authored rooms keep hand-finished stairs. Re-emit used to
                // DestroyNamedUnder then stamp a fresh kit mesh.
                if (IsUnderAuthored(lower.transform)
                    && (HasNamedUnder(lower.transform, stairName)
                        || HasNamedUnder(upper.transform, stairName)))
                {
                    placed++;
                    continue;
                }

                DestroyNamedUnder(lower.transform, walkName);
                DestroyNamedUnder(lower.transform, stairName);
                DestroyNamedUnder(upper.transform, walkName);
                DestroyNamedUnder(upper.transform, stairName);

                Vector3 lowerPos = lower.transform.position;
                float cx = lowerPos.x;
                float cz = lowerPos.z;
                float lowerY = lowerPos.y;
                float upperY = upper.transform.position.y;

                float run;
                float wellWidth;
                Transform stairRoot = PlaceStairMesh(
                    stairSource,
                    lower.transform,
                    stairName,
                    cx,
                    lowerY,
                    cz,
                    rise,
                    out run,
                    out wellWidth);
                if (run < 0.5f)
                    run = 8f;
                if (wellWidth < WalkWidth)
                    wellWidth = WalkWidth;

                float angle = Mathf.Atan(rise / run) * Mathf.Rad2Deg;
                float hypot = Mathf.Sqrt(rise * rise + run * run);
                Vector3 foot = new Vector3(cx, lowerY, cz);
                Vector3 head = new Vector3(cx, upperY, cz + run);
                Vector3 mid = (foot + head) * 0.5f;

                float halfW = wellWidth * 0.5f + WellPad;
                var well = new Bounds();
                well.SetMinMax(
                    new Vector3(cx - halfW, lowerY - 0.6f, cz - WellPad),
                    new Vector3(cx + halfW, upperY + 0.6f, cz + run + WellPad));

                DisableOverlappingTiles(upper, BastionSegment.GroupFloors, "FloorTile", well);
                DisableOverlappingTiles(lower, BastionSegment.GroupCeiling, "CeilingTile", well);
                PunchPhysicsFloor(upper, well);

                Transform walkParent = stairRoot != null ? stairRoot : lower.transform;
                PlaceWalkBox(walkParent, walkName, mid, angle, hypot, wellWidth);
                placed++;
            }

            return placed;
        }

        /// <summary>Keep the dress mesh pose. Rebuild the walk box (foot buried so the
        /// end-cap is not a wall), add a landing flush with the upper floor, and re-punch
        /// the well on the live mesh bounds.</summary>
        public static void FitWalkAndWellToDress(GameObject stair, GameObject lower, GameObject upper)
        {
            if (stair == null || lower == null || upper == null)
                return;

            Renderer dress = stair.GetComponent<Renderer>();
            if (dress == null)
            {
                Debug.LogError("[BastionSketchStairs] " + stair.name + " has no Renderer.");
                return;
            }

            Bounds mesh = dress.bounds;
            float rise = BastionKit.StoreyHeight;
            float lowerY = lower.transform.position.y;
            float upperY = upper.transform.position.y;

            string suffix = stair.name.StartsWith(StairPrefix, System.StringComparison.Ordinal)
                ? stair.name.Substring(StairPrefix.Length)
                : stair.name;
            string walkName = WalkPrefix + suffix;
            string landName = LandPrefix + suffix;

            Vector3 climb = Vector3.forward;
            Transform existingWalk = stair.transform.Find(walkName);
            if (existingWalk != null)
            {
                climb = existingWalk.forward;
                climb.y = 0f;
                if (climb.sqrMagnitude > 0.01f)
                    climb.Normalize();
                else
                    climb = Vector3.forward;
            }

            float halfRun = mesh.extents.x * Mathf.Abs(climb.x) + mesh.extents.z * Mathf.Abs(climb.z);
            float run = halfRun * 2f;
            if (run < 0.5f)
                run = 8f;

            float angle = Mathf.Atan(rise / run) * Mathf.Rad2Deg;
            float hypot = Mathf.Sqrt(rise * rise + run * run);
            float extra = BuryDepth / Mathf.Max(0.2f, Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 slopeDir = (climb * run + Vector3.up * rise).normalized;
            Vector3 mid = mesh.center - slopeDir * (extra * 0.5f);
            float width = Mathf.Max(WalkWidth, mesh.size.x);

            DestroyNamedUnder(stair.transform, walkName);
            DestroyNamedUnder(stair.transform, landName);
            PlaceWalkBox(stair.transform, walkName, mid, angle, hypot + extra, width);

            Vector3 landPos = mesh.center + climb * (halfRun + LandingDepth * 0.45f);
            landPos.y = upperY - WalkThickness * 0.5f;
            PlaceLanding(stair.transform, landName, landPos, width, LandingDepth);

            float halfW = width * 0.5f + WellPad;
            var well = new Bounds();
            well.SetMinMax(
                new Vector3(mesh.center.x - halfW, lowerY - 0.6f, mesh.min.z - WellPad),
                new Vector3(mesh.center.x + halfW, upperY + 0.6f, mesh.max.z + WellPad));

            EnableTiles(upper, BastionSegment.GroupFloors, "FloorTile");
            EnableTiles(lower, BastionSegment.GroupCeiling, "CeilingTile");
            DisableOverlappingTiles(upper, BastionSegment.GroupFloors, "FloorTile", well);
            DisableOverlappingTiles(lower, BastionSegment.GroupCeiling, "CeilingTile", well);
            ReactivateGenFloor(upper);
            PunchPhysicsFloor(upper, well);
        }

        static GameObject LoadStairSource()
        {
            string path = AssetDatabase.GUIDToAssetPath(StairMeshGuid);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static Transform PlaceStairMesh(
            GameObject source,
            Transform parent,
            string stairName,
            float cx,
            float lowerY,
            float cz,
            float rise,
            out float run,
            out float wellWidth)
        {
            run = 0f;
            wellWidth = WalkWidth;
            if (source == null)
                return null;

            GameObject stair = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (stair == null)
                stair = Object.Instantiate(source, parent);
            if (stair == null)
                return null;

            stair.name = stairName;
            if (PrefabUtility.IsPartOfPrefabInstance(stair))
                PrefabUtility.UnpackPrefabInstance(
                    stair, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            StripColliders(stair);

            Mesh mesh = MeshOf(stair);
            if (mesh == null)
            {
                Debug.LogError("[BastionSketchStairs] " + stairName + " has no mesh.");
                return stair.transform;
            }

            Bounds local = mesh.bounds;
            float nativeRise = local.size.y;
            if (nativeRise < 0.01f)
                nativeRise = 5.798f;
            float scale = rise / nativeRise;
            stair.transform.localScale = new Vector3(scale, scale, scale);
            stair.transform.rotation = Quaternion.identity;
            stair.transform.position = new Vector3(
                cx - local.center.x * scale,
                lowerY - local.min.y * scale,
                cz - local.min.z * scale);

            run = local.size.z * scale;
            wellWidth = Mathf.Max(WalkWidth, local.size.x * scale);
            if (stair.GetComponent<BastionGeneratedGeom>() == null)
                stair.AddComponent<BastionGeneratedGeom>();
            return stair.transform;
        }

        static Mesh MeshOf(GameObject stair)
        {
            MeshFilter filter = stair.GetComponentInChildren<MeshFilter>();
            if (filter == null)
                return null;
            return filter.sharedMesh;
        }

        static void StripColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    Object.DestroyImmediate(colliders[i]);
            }
        }

        static void PlaceWalkBox(
            Transform parent,
            string walkName,
            Vector3 mid,
            float angle,
            float hypot,
            float width)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = walkName;
            go.layer = 0;
            go.transform.position = mid;
            go.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);
            go.transform.localScale = new Vector3(width, WalkThickness, hypot);
            go.transform.SetParent(parent, true);
            go.AddComponent<BastionGeneratedGeom>();

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            const StaticEditorFlags flags =
                StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.NavigationStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        static void PlaceLanding(
            Transform parent,
            string landName,
            Vector3 worldPos,
            float width,
            float depth)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = landName;
            go.layer = 0;
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = new Vector3(width, WalkThickness, depth);
            go.transform.SetParent(parent, true);
            go.AddComponent<BastionGeneratedGeom>();

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            const StaticEditorFlags flags =
                StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.NavigationStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        static void EnableTiles(
            GameObject room,
            string groupName,
            string nameContains)
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
                r.gameObject.SetActive(true);
            }
        }

        static void ReactivateGenFloor(GameObject upper)
        {
            Transform floors = FindGeomGroup(upper, BastionSegment.GroupFloors);
            if (floors == null)
                return;
            Transform genFloor = floors.Find(BastionSegment.GenPrefix + "Floor");
            if (genFloor != null)
                genFloor.gameObject.SetActive(true);
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
                // A graze must not cost a whole tile: a sliver of overlap still SEALS the
                // deck visually, and killing the tile leaves metres of walkable unrendered
                // hole (the stair audit found 5.3 m holes bought by 0.02-0.25 m grazes).
                // Only a bite deep enough to collide with the stair itself takes it out.
                Bounds b = r.bounds;
                float biteX = Mathf.Min(b.max.x, well.max.x) - Mathf.Max(b.min.x, well.min.x);
                float biteZ = Mathf.Min(b.max.z, well.max.z) - Mathf.Max(b.min.z, well.min.z);
                if (biteX < MinTileBite || biteZ < MinTileBite)
                    continue;
                r.gameObject.SetActive(false);
            }
        }

        /// <summary>Minimum XZ overlap (metres, both axes) between the padded well and a
        /// tile before the tile is deactivated. Below this the stair dress hides the graze.</summary>
        const float MinTileBite = 0.5f;

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

        static bool HasNamedUnder(Transform root, string name)
        {
            if (root == null)
                return false;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root)
                    continue;
                if (t.name == name)
                    return true;
            }
            return false;
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
