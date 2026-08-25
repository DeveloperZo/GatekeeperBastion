using UnityEngine;
using UnityEngine.Rendering;
using GatekeepersProtocol.Sim;
using SegmentEdge = GatekeepersProtocol.Game.BastionSegment.SegmentEdge;

namespace GatekeepersProtocol.Game
{
    /// <summary>Visual layer for a bastion segment: pack prefab instances, colliders stripped,
    /// tiled over the footprint whose physics the segment's collision boxes own. Separate type
    /// because dressing shares nothing with the box math beyond the segment's serialized fields.
    /// Load-time code — allocation is acceptable, there is no per-frame path.</summary>
    internal static partial class BastionSegmentVisuals
    {
        internal static void Build(
            BastionSegment segment,
            Transform geomRoot,
            float width,
            float depth,
            float wallHeight,
            float thickness)
        {
            if (segment.FloorPiece == null || segment.CeilingPiece == null || segment.WallPiece == null
                || segment.DoorframePiece == null || segment.WindowPiece == null
                || segment.CornerPiece == null || segment.CeilingLightPiece == null)
            {
                // No primitive fallback: half-dressed reads worse than undressed. Collision still
                // builds, so the segment stays playable while the kit is re-emitted.
                Debug.LogError(
                    "[BastionSegment] Unmapped kit piece(s) on " + segment.name
                    + " — re-run the kit emitter (gkb_bastion_kit_request.txt)");
                return;
            }

            Bounds floorB = BastionPackTiling.Measure(segment.FloorPiece);
            Bounds ceilingB = BastionPackTiling.Measure(segment.CeilingPiece);
            Bounds wallB = BastionPackTiling.Measure(segment.WallPiece);
            Bounds doorframeB = BastionPackTiling.Measure(segment.DoorframePiece);
            Bounds windowB = BastionPackTiling.Measure(segment.WindowPiece);
            Bounds cornerB = BastionPackTiling.Measure(segment.CornerPiece);
            Bounds lightB = BastionPackTiling.Measure(segment.CeilingLightPiece);

            // Pieces group by category (Walls/Floors/Ceiling) — the builders parent to
            // whatever transform they are handed. Lights ride Ceiling, corners ride Walls.
            Transform floors = BastionSegment.EnsureGeomGroup(geomRoot, BastionSegment.GroupFloors);
            Transform ceiling = BastionSegment.EnsureGeomGroup(geomRoot, BastionSegment.GroupCeiling);
            Transform walls = BastionSegment.EnsureGeomGroup(geomRoot, BastionSegment.GroupWalls);

            // Floor walking surface at local y = 0; ceiling underside at wallHeight.
            BuildSlabGrid(floors, segment.FloorPiece, floorB, width, depth, -floorB.max.y, "FloorTile");
            BuildSlabGrid(ceiling, segment.CeilingPiece, ceilingB, width, depth, wallHeight - ceilingB.min.y, "CeilingTile");

            BuildFace(segment, walls, SegmentEdge.North, segment.North, segment.NorthDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
            BuildFace(segment, walls, SegmentEdge.South, segment.South, segment.SouthDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
            BuildFace(segment, walls, SegmentEdge.East, segment.East, segment.EastDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
            BuildFace(segment, walls, SegmentEdge.West, segment.West, segment.WestDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);

            BuildCorners(segment, walls, width, depth, wallHeight, cornerB);
            BuildLights(segment, ceiling, width, depth, wallHeight, lightB, BastionPackTiling.Step(ceilingB.size.x));
        }

        static void BuildSlabGrid(
            Transform geomRoot,
            GameObject prefab,
            Bounds b,
            float width,
            float depth,
            float rootY,
            string role)
        {
            float stepX = BastionPackTiling.Step(b.size.x);
            float stepZ = BastionPackTiling.Step(b.size.z);
            int totalX = BastionPackTiling.RunCount(width, stepX, out float tileStepX);
            int totalZ = BastionPackTiling.RunCount(depth, stepZ, out float tileStepZ);
            float startX = -width * 0.5f;
            float startZ = -depth * 0.5f;
            int index = 0;

            for (int ix = 0; ix < totalX; ix++)
            {
                BastionPackTiling.RunAt(ix, startX, tileStepX, stepX, out float cx, out float sx);
                for (int iz = 0; iz < totalZ; iz++)
                {
                    BastionPackTiling.RunAt(iz, startZ, tileStepZ, stepZ, out float cz, out float sz);

                    // Pack slab pivots are corner pivots — position from the measured bounds
                    // centre, never the pivot.
                    Vector3 pos = new Vector3(cx - sx * b.center.x, rootY, cz - sz * b.center.z);
                    SpawnPiece(
                        geomRoot,
                        prefab,
                        BastionSegment.GenPrefix + role + "_" + index,
                        pos,
                        Quaternion.identity,
                        new Vector3(sx, 1f, sz),
                        true);
                    index++;
                }
            }
        }

        static void BuildFace(
            BastionSegment segment,
            Transform geomRoot,
            SegmentEdge orientation,
            BastionSideKind kind,
            float doorAlong,
            float width,
            float depth,
            float wallHeight,
            float thickness,
            Bounds wallB,
            Bounds doorframeB,
            Bounds windowB)
        {
            if (kind == BastionSideKind.None)
                return;

            bool alongX = orientation == SegmentEdge.North || orientation == SegmentEdge.South;
            float span = alongX ? width : depth;
            float edgeOffset = alongX ? depth * 0.5f + thickness * 0.5f : width * 0.5f + thickness * 0.5f;
            float step = BastionPackTiling.Step(wallB.size.x);
            float wallScaleY = wallHeight / Mathf.Max(wallB.max.y, 0.01f);
            int index = 0;

            if (kind == BastionSideKind.Wall)
            {
                BuildWallRun(geomRoot, segment.WallPiece, orientation, -span * 0.5f, span, edgeOffset, step, wallB, wallScaleY, ref index);
                return;
            }

            // Door / Window: split around one full module (step), NOT the 3 m logical opening, so
            // the frame piece lands exactly one module wide with the logical opening centred in
            // it. The collision layer keeps the 3 m opening; sim and BastionDoor untouched.
            if (!BastionSegment.TryDoorPillars(span, step, doorAlong, out float openCenter, out float westWidth, out float eastWidth))
            {
                BuildWallRun(geomRoot, segment.WallPiece, orientation, -span * 0.5f, span, edgeOffset, step, wallB, wallScaleY, ref index);
                return;
            }

            if (westWidth > BastionPackTiling.RemainderEpsilon)
                BuildWallRun(geomRoot, segment.WallPiece, orientation, -span * 0.5f, westWidth, edgeOffset, step, wallB, wallScaleY, ref index);
            if (eastWidth > BastionPackTiling.RemainderEpsilon)
                BuildWallRun(geomRoot, segment.WallPiece, orientation, span * 0.5f - eastWidth, eastWidth, edgeOffset, step, wallB, wallScaleY, ref index);

            bool isDoor = kind == BastionSideKind.Door;
            GameObject piece = isDoor ? segment.DoorframePiece : segment.WindowPiece;
            Bounds pieceB = isDoor ? doorframeB : windowB;
            string role = isDoor ? "_Doorframe_0" : "_WindowPiece_0";

            // Frame/window scaled along the run to exactly one module (doorframe measures ~4.57)
            // and Y-scaled like walls.
            float pieceScaleAlong = step / Mathf.Max(pieceB.size.x, 0.01f);
            float pieceScaleY = wallHeight / Mathf.Max(pieceB.max.y, 0.01f);
            GameObject spawned = SpawnFacePiece(
                geomRoot,
                piece,
                pieceB,
                orientation,
                openCenter,
                edgeOffset,
                new Vector3(pieceScaleAlong, pieceScaleY, 1f),
                BastionSegment.GenPrefix + orientation + role);
            if (isDoor)
                DisableBuiltInLeaf(spawned);
        }

        /// <summary>The pack doorframe ships with a shut leaf baked in. Deactivate it (prefab
        /// instances forbid child destroy) so the doorway is open: BastionDoor's slab is the
        /// moving leaf, and an active leaf both reads as sealed and bakes a NavMesh wall.</summary>
        static void DisableBuiltInLeaf(GameObject frame)
        {
            if (frame == null)
                return;
            Transform[] children = frame.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.StartsWith(BastionMeshMap.DoorframeLeafPrefix, System.StringComparison.Ordinal))
                    children[i].gameObject.SetActive(false);
            }
        }

        static void BuildWallRun(
            Transform geomRoot,
            GameObject wallPiece,
            SegmentEdge orientation,
            float start,
            float length,
            float edgeOffset,
            float step,
            Bounds wallB,
            float scaleY,
            ref int index)
        {
            int total = BastionPackTiling.RunCount(length, step, out float tileStep);
            for (int i = 0; i < total; i++)
            {
                BastionPackTiling.RunAt(i, start, tileStep, step, out float center, out float scaleAlong);
                SpawnFacePiece(
                    geomRoot,
                    wallPiece,
                    wallB,
                    orientation,
                    center,
                    edgeOffset,
                    new Vector3(scaleAlong, scaleY, 1f),
                    BastionSegment.GenPrefix + orientation + "_WallPiece_" + index);
                index++;
            }
        }

        // Pack walls run along local X with the detailed interior face on local -Z. Yaw points
        // each piece's -Z at the room centre: North 0, South 180, East 90, West -90.
        static Quaternion EdgeYaw(SegmentEdge orientation)
        {
            switch (orientation)
            {
                case SegmentEdge.North:
                    return Quaternion.identity;
                case SegmentEdge.South:
                    return Quaternion.Euler(0f, 180f, 0f);
                case SegmentEdge.East:
                    return Quaternion.Euler(0f, 90f, 0f);
                default:
                    return Quaternion.Euler(0f, -90f, 0f);
            }
        }

        static GameObject SpawnFacePiece(
            Transform geomRoot,
            GameObject prefab,
            Bounds b,
            SegmentEdge orientation,
            float alongCenter,
            float edgeOffset,
            Vector3 pieceScale,
            string goName)
        {
            Quaternion rot = EdgeYaw(orientation);

            // Bounds-centre compensation in the edge plane (pack pivots are end pivots). Y anchors
            // at 0: the Y scale puts measured max.y at wallHeight, the skirt below y = 0 sinks
            // into the floor slab.
            Vector3 offset = rot * Vector3.Scale(pieceScale, new Vector3(b.center.x, 0f, b.center.z));
            Vector3 desired = BastionSegment.SideCenter(orientation, alongCenter, 0f, edgeOffset);
            Vector3 pos = new Vector3(desired.x - offset.x, 0f, desired.z - offset.z);
            return SpawnPiece(geomRoot, prefab, goName, pos, rot, pieceScale, true);
        }

        static void BuildCorners(
            BastionSegment segment,
            Transform geomRoot,
            float width,
            float depth,
            float wallHeight,
            Bounds cornerB)
        {
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;
            float scaleY = wallHeight / Mathf.Max(cornerB.max.y, 0.01f);
            int index = 0;

            // One pillar per footprint corner where both adjacent sides exist.
            if (segment.North != BastionSideKind.None && segment.East != BastionSideKind.None)
                SpawnCorner(geomRoot, segment.CornerPiece, halfW, halfD, scaleY, cornerB, ref index);
            if (segment.North != BastionSideKind.None && segment.West != BastionSideKind.None)
                SpawnCorner(geomRoot, segment.CornerPiece, -halfW, halfD, scaleY, cornerB, ref index);
            if (segment.South != BastionSideKind.None && segment.East != BastionSideKind.None)
                SpawnCorner(geomRoot, segment.CornerPiece, halfW, -halfD, scaleY, cornerB, ref index);
            if (segment.South != BastionSideKind.None && segment.West != BastionSideKind.None)
                SpawnCorner(geomRoot, segment.CornerPiece, -halfW, -halfD, scaleY, cornerB, ref index);
        }

        static void SpawnCorner(
            Transform geomRoot,
            GameObject cornerPiece,
            float x,
            float z,
            float scaleY,
            Bounds b,
            ref int index)
        {
            Vector3 pos = new Vector3(x - b.center.x, 0f, z - b.center.z);
            SpawnPiece(
                geomRoot,
                cornerPiece,
                BastionSegment.GenPrefix + "CornerPillar_" + index,
                pos,
                Quaternion.identity,
                new Vector3(1f, scaleY, 1f),
                true);
            index++;
        }

        static void BuildLights(
            BastionSegment segment,
            Transform geomRoot,
            float width,
            float depth,
            float wallHeight,
            Bounds lightB,
            float moduleStep)
        {
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;

            // Flush under the ceiling plane. 2-module (10 m) grid, inset one module from each
            // edge; identity rotation — orientation gets one eyeball pass later. No Light
            // components exist in the pack prefabs and none are added.
            float y = wallHeight - lightB.max.y;
            float spacing = moduleStep * 2f;
            int index = 0;

            for (float x = -halfW + moduleStep; x <= halfW - moduleStep + 0.001f; x += spacing)
            {
                for (float z = -halfD + moduleStep; z <= halfD - moduleStep + 0.001f; z += spacing)
                {
                    Vector3 pos = new Vector3(x - lightB.center.x, y, z - lightB.center.z);
                    SpawnPiece(
                        geomRoot,
                        segment.CeilingLightPiece,
                        BastionSegment.GenPrefix + "CeilingLight_" + index,
                        pos,
                        Quaternion.identity,
                        Vector3.one,
                        false);
                    index++;
                }
            }

            // Rooms narrower than the grid (under ~2 modules a side) get one centred light
            // instead of none.
            if (index == 0)
            {
                Vector3 pos = new Vector3(-lightB.center.x, y, -lightB.center.z);
                SpawnPiece(
                    geomRoot,
                    segment.CeilingLightPiece,
                    BastionSegment.GenPrefix + "CeilingLight_0",
                    pos,
                    Quaternion.identity,
                    Vector3.one,
                    false);
            }
        }

        static GameObject SpawnPiece(
            Transform geomRoot,
            GameObject prefab,
            string goName,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            bool castShadows)
        {
            if (geomRoot == null)
                return null;

            GameObject go;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, geomRoot);
                // Kit pieces are templates. Completely unpacks so Rebuild cannot leave
                // packed Gen_* in an authored scene.
                if (go != null)
                    UnityEditor.PrefabUtility.UnpackPrefabInstance(
                        go,
                        UnityEditor.PrefabUnpackMode.Completely,
                        UnityEditor.InteractionMode.AutomatedAction);
            }
            else
                go = Object.Instantiate(prefab, geomRoot);
#else
            go = Object.Instantiate(prefab, geomRoot);
#endif
            if (go == null)
                return null;

            go.name = goName;
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            BastionSegment.RebuildObjectsCreated++;

            // Pack colliders stripped — the segment's box colliders own collision. Unstripped
            // convex hulls sit ~0.9 m proud of a visually thin wall.
            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(colliders[i]);
                    continue;
                }
#endif
                Object.Destroy(colliders[i]);
            }

            // Pack Animators stripped for the same reason as the colliders: the window piece
            // ships one with no controller, which costs a per-frame update on every generated
            // face and reads as broken wiring to anything auditing the scene.
            Animator[] animators = go.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(animators[i]);
                    continue;
                }
#endif
                Object.Destroy(animators[i]);
            }

            ShadowCastingMode mode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].shadowCastingMode = mode;

            go.AddComponent<BastionGeneratedGeom>();
            BastionSegment.RebuildObjectsMarked++;

#if UNITY_EDITOR
            // Editor-only static flags on root and children. Lightmap deferred with baked
            // lighting; runtime rebuilds just lack the flags.
            if (!Application.isPlaying)
            {
                const UnityEditor.StaticEditorFlags flags =
                    UnityEditor.StaticEditorFlags.BatchingStatic
                    | UnityEditor.StaticEditorFlags.OccluderStatic
                    | UnityEditor.StaticEditorFlags.OccludeeStatic
                    | UnityEditor.StaticEditorFlags.NavigationStatic;
                Transform[] all = go.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(all[i].gameObject, flags);
            }
#endif
            return go;
        }
    }
}
