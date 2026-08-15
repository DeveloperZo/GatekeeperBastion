using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Game
{
    public sealed partial class BastionSegment
    {
        // Snap grid the pack is authored on: 5 m large, 2.5 m small. The tile step is the snap
        // size; the measured bounds are the sanity check (pieces measure ~5.08 with overlap lips).
        const float PackSnapGrid = 2.5f;
        const float TileRemainderEpsilon = 0.01f;

        // ---------------------------------------------------------------------------------------
        // Collision layer — M47's exact box math, renderers removed. Player physics and shot
        // blocking are unchanged; the NavMesh ignores these (the bake reads render meshes).
        // ---------------------------------------------------------------------------------------

        void BuildFloor(float width, float depth)
        {
            CreateBox(
                GenPrefix + "Floor",
                new Vector3(0f, -BastionKit.FloorSlabThickness * 0.5f, 0f),
                new Vector3(width, BastionKit.FloorSlabThickness, depth));
        }

        void BuildSide(
            SegmentEdge orientation,
            BastionSideKind kind,
            float doorAlong,
            float width,
            float depth,
            float wallHeight,
            float wallCenterY,
            float thickness,
            float openingWidth)
        {
            if (kind == BastionSideKind.None)
                return;

            bool alongX = orientation == SegmentEdge.North || orientation == SegmentEdge.South;
            float span = alongX ? width : depth;
            float edgeOffset = alongX ? depth * 0.5f + thickness * 0.5f : width * 0.5f + thickness * 0.5f;

            switch (kind)
            {
                case BastionSideKind.Wall:
                    BuildSolidWall(orientation, span, wallHeight, wallCenterY, thickness, edgeOffset);
                    break;
                case BastionSideKind.Door:
                    BuildDoorWall(orientation, span, wallHeight, thickness, edgeOffset, openingWidth, doorAlong);
                    break;
                case BastionSideKind.Window:
                    BuildWindowWall(orientation, span, wallHeight, thickness, edgeOffset, openingWidth, doorAlong);
                    break;
            }
        }

        void BuildSolidWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float wallCenterY,
            float thickness,
            float edgeOffset)
        {
            Vector3 size = SideSize(orientation, span, wallHeight, thickness);
            Vector3 center = SideCenter(orientation, 0f, wallCenterY, edgeOffset);
            CreateBox(GenPrefix + orientation + "_Wall", center, size);
        }

        void BuildDoorWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float thickness,
            float edgeOffset,
            float openingWidth,
            float doorAlong)
        {
            if (!TryDoorPillars(span, openingWidth, doorAlong, out float openCenter, out float westWidth, out float eastWidth))
            {
                BuildSolidWall(orientation, span, wallHeight, wallHeight * 0.5f, thickness, edgeOffset);
                return;
            }

            float lintelHeight = wallHeight - DoorOpeningHeight;

            if (westWidth > 0.001f)
            {
                float westCenter = -span * 0.5f + westWidth * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Door_West",
                    SideCenter(orientation, westCenter, wallHeight * 0.5f, edgeOffset),
                    SideSize(orientation, westWidth, wallHeight, thickness));
            }

            if (eastWidth > 0.001f)
            {
                float eastCenter = span * 0.5f - eastWidth * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Door_East",
                    SideCenter(orientation, eastCenter, wallHeight * 0.5f, edgeOffset),
                    SideSize(orientation, eastWidth, wallHeight, thickness));
            }

            if (lintelHeight > 0f)
            {
                float lintelCenterY = DoorOpeningHeight + lintelHeight * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Door_Lintel",
                    SideCenter(orientation, openCenter, lintelCenterY, edgeOffset),
                    SideSize(orientation, openingWidth, lintelHeight, thickness));
            }
        }

        void BuildWindowWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float thickness,
            float edgeOffset,
            float openingWidth,
            float doorAlong)
        {
            float bandHeight = wallHeight - WindowSillHeight - WindowLintelHeight;
            if (bandHeight <= 0f)
            {
                BuildSolidWall(orientation, span, wallHeight, wallHeight * 0.5f, thickness, edgeOffset);
                return;
            }

            if (!TryDoorPillars(span, openingWidth, doorAlong, out float openCenter, out float westWidth, out float eastWidth))
            {
                BuildSolidWall(orientation, span, wallHeight, wallHeight * 0.5f, thickness, edgeOffset);
                return;
            }

            float sillCenterY = WindowSillHeight * 0.5f;
            CreateBox(
                GenPrefix + orientation + "_Window_Sill",
                SideCenter(orientation, 0f, sillCenterY, edgeOffset),
                SideSize(orientation, span, WindowSillHeight, thickness));

            float lintelCenterY = wallHeight - WindowLintelHeight * 0.5f;
            CreateBox(
                GenPrefix + orientation + "_Window_Lintel",
                SideCenter(orientation, 0f, lintelCenterY, edgeOffset),
                SideSize(orientation, span, WindowLintelHeight, thickness));

            float bandCenterY = WindowSillHeight + bandHeight * 0.5f;

            if (westWidth > 0.001f)
            {
                float westCenter = -span * 0.5f + westWidth * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Window_West",
                    SideCenter(orientation, westCenter, bandCenterY, edgeOffset),
                    SideSize(orientation, westWidth, bandHeight, thickness));
            }

            if (eastWidth > 0.001f)
            {
                float eastCenter = span * 0.5f - eastWidth * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Window_East",
                    SideCenter(orientation, eastCenter, bandCenterY, edgeOffset),
                    SideSize(orientation, eastWidth, bandHeight, thickness));
            }

            Vector3 blockCenter = SideCenter(orientation, openCenter, WindowWalkBlockHeight * 0.5f, edgeOffset);
            Vector3 blockSize = SideSize(orientation, openingWidth, WindowWalkBlockHeight, thickness);
            CreateWalkBlock(GenPrefix + orientation + "_Window_WalkBlock", blockCenter, blockSize);
        }

        /// <summary>Splits an edge into west/east pillars around an opening centred at
        /// <paramref name="doorAlong"/> (metres from edge centre). Returns false if the opening
        /// cannot fit.</summary>
        public static bool TryDoorPillars(
            float span,
            float openingWidth,
            float doorAlong,
            out float openCenter,
            out float westWidth,
            out float eastWidth)
        {
            openCenter = 0f;
            westWidth = 0f;
            eastWidth = 0f;
            if (span <= openingWidth)
                return false;

            float maxAlong = (span - openingWidth) * 0.5f;
            openCenter = doorAlong;
            if (openCenter > maxAlong)
                openCenter = maxAlong;
            else if (openCenter < -maxAlong)
                openCenter = -maxAlong;

            westWidth = (openCenter - openingWidth * 0.5f) - (-span * 0.5f);
            eastWidth = (span * 0.5f) - (openCenter + openingWidth * 0.5f);
            return true;
        }

        static Vector3 SideSize(SegmentEdge orientation, float span, float height, float thickness)
        {
            if (orientation == SegmentEdge.North || orientation == SegmentEdge.South)
                return new Vector3(span, height, thickness);
            return new Vector3(thickness, height, span);
        }

        static Vector3 SideCenter(SegmentEdge orientation, float along, float y, float edgeOffset)
        {
            switch (orientation)
            {
                case SegmentEdge.North:
                    return new Vector3(along, y, edgeOffset);
                case SegmentEdge.South:
                    return new Vector3(along, y, -edgeOffset);
                case SegmentEdge.East:
                    return new Vector3(edgeOffset, y, along);
                default:
                    return new Vector3(-edgeOffset, y, along);
            }
        }

        void CreateBox(string name, Vector3 localCenter, Vector3 size)
        {
            if (_geomRoot == null)
                return;

            // Collider-only: the visual layer brings the pack meshes. The unit BoxCollider under
            // the box's localScale keeps M47's exact physics footprint.
            var go = new GameObject(name);
            go.transform.SetParent(_geomRoot, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.layer = 0;
            RebuildObjectsCreated++;

            var box = go.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = Vector3.one;

            go.AddComponent<BastionGeneratedGeom>();
            RebuildObjectsMarked++;
        }

        void CreateWalkBlock(string name, Vector3 localCenter, Vector3 size)
        {
            if (_geomRoot == null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(_geomRoot, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.layer = 0;
            RebuildObjectsCreated++;

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.center = Vector3.zero;
            obstacle.size = Vector3.one;

            go.AddComponent<BastionGeneratedGeom>();
            RebuildObjectsMarked++;
        }

        // ---------------------------------------------------------------------------------------
        // Visual layer — pack prefab instances, no colliders, carrying the look. Load-time code:
        // allocation is acceptable here, there is no per-frame path.
        // ---------------------------------------------------------------------------------------

        static readonly Dictionary<GameObject, Bounds> _pieceBoundsCache = new Dictionary<GameObject, Bounds>();
        static readonly HashSet<string> _pieceMetricsLogged = new HashSet<string>();

        void BuildVisuals(float width, float depth, float wallHeight, float thickness)
        {
            if (floorPiece == null || ceilingPiece == null || wallPiece == null
                || doorframePiece == null || windowPiece == null || cornerPiece == null
                || ceilingLightPiece == null)
            {
                // No primitive fallback: half-dressed reads worse than undressed. Collision still
                // builds above, so the segment stays playable while the kit is re-emitted.
                Debug.LogError(
                    "[BastionSegment] Unmapped kit piece(s) on " + name
                    + " — re-run the kit emitter (gkb_bastion_kit_request.txt)");
                return;
            }

            Bounds floorB = MeasurePiece(floorPiece);
            Bounds ceilingB = MeasurePiece(ceilingPiece);
            Bounds wallB = MeasurePiece(wallPiece);
            Bounds doorframeB = MeasurePiece(doorframePiece);
            Bounds windowB = MeasurePiece(windowPiece);
            Bounds cornerB = MeasurePiece(cornerPiece);
            Bounds lightB = MeasurePiece(ceilingLightPiece);

            LogPieceMetrics(floorPiece, TileStep(floorB.size.x), floorB);
            LogPieceMetrics(ceilingPiece, TileStep(ceilingB.size.x), ceilingB);
            LogPieceMetrics(wallPiece, TileStep(wallB.size.x), wallB);
            LogPieceMetrics(doorframePiece, TileStep(doorframeB.size.x), doorframeB);
            LogPieceMetrics(windowPiece, TileStep(windowB.size.x), windowB);
            LogPieceMetrics(cornerPiece, TileStep(cornerB.size.x), cornerB);
            LogPieceMetrics(ceilingLightPiece, TileStep(lightB.size.x), lightB);

            // Floor walking surface at local y = 0; ceiling underside at wallHeight.
            BuildVisualSlabGrid(floorPiece, floorB, width, depth, -floorB.max.y, "FloorTile");
            BuildVisualSlabGrid(ceilingPiece, ceilingB, width, depth, wallHeight - ceilingB.min.y, "CeilingTile");

            BuildVisualSide(SegmentEdge.North, north, northDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
            BuildVisualSide(SegmentEdge.South, south, southDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
            BuildVisualSide(SegmentEdge.East, east, eastDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
            BuildVisualSide(SegmentEdge.West, west, westDoorAlong, width, depth, wallHeight, thickness, wallB, doorframeB, windowB);

            BuildVisualCorners(width, depth, wallHeight, cornerB);
            BuildVisualLights(width, depth, wallHeight, lightB, TileStep(ceilingB.size.x));
        }

        /// <summary>Local AABB of a piece prefab, computed without instantiating: every child
        /// MeshFilter's sharedMesh.bounds transformed into the prefab root's frame (root treated
        /// as identity). Cached per prefab asset.</summary>
        static Bounds MeasurePiece(GameObject prefab)
        {
            if (_pieceBoundsCache.TryGetValue(prefab, out Bounds cached))
                return cached;

            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            Matrix4x4 worldToRoot = prefab.transform.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds total = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null)
                    continue;

                Matrix4x4 toRoot = worldToRoot * filters[i].transform.localToWorldMatrix;
                Bounds meshBounds = mesh.bounds;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = new Vector3(
                        (c & 1) == 0 ? min.x : max.x,
                        (c & 2) == 0 ? min.y : max.y,
                        (c & 4) == 0 ? min.z : max.z);
                    Vector3 point = toRoot.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        total = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                        total.Encapsulate(point);
                }
            }

            if (!hasBounds)
                total = new Bounds(Vector3.zero, Vector3.one);

            _pieceBoundsCache[prefab] = total;
            return total;
        }

        /// <summary>Snap-grid tile step for a measured piece size: nearest multiple of the pack's
        /// 2.5 m small grid, minimum one small grid. ~5.08 m pieces (overlap lips) step at 5.</summary>
        static float TileStep(float measuredSize)
        {
            float step = PackSnapGrid * Mathf.Round(measuredSize / PackSnapGrid);
            if (step < PackSnapGrid)
                step = PackSnapGrid;
            return step;
        }

        static void LogPieceMetrics(GameObject prefab, float step, Bounds bounds)
        {
            if (!_pieceMetricsLogged.Add(prefab.name))
                return;

            Debug.Log(
                "[BastionSegment] piece " + prefab.name + " step=" + step
                + " measured=" + bounds.size.x + "x" + bounds.size.y + "x" + bounds.size.z);
        }

        /// <summary>Full pieces of <paramref name="step"/> along <paramref name="length"/>.
        /// <paramref name="remainder"/> is the leftover metres (0 when negligible), filled by one
        /// extra piece scaled remainder/step along the run — non-uniform scale, no small-piece
        /// infill.</summary>
        static int TileRunCount(float length, float step, out float remainder)
        {
            int full = Mathf.FloorToInt(length / step + 0.001f);
            if (full < 0)
                full = 0;
            remainder = length - full * step;
            if (remainder <= TileRemainderEpsilon)
                remainder = 0f;
            return full;
        }

        /// <summary>Centre and along-axis scale of tile <paramref name="index"/> in a run starting
        /// at <paramref name="start"/>. Indices past <paramref name="fullCount"/> address the one
        /// remainder tile.</summary>
        static void TileRunAt(
            int index,
            int fullCount,
            float start,
            float step,
            float remainder,
            out float center,
            out float scale)
        {
            if (index < fullCount)
            {
                center = start + (index + 0.5f) * step;
                scale = 1f;
                return;
            }

            center = start + fullCount * step + remainder * 0.5f;
            scale = remainder / step;
        }

        void BuildVisualSlabGrid(GameObject prefab, Bounds b, float width, float depth, float rootY, string role)
        {
            float stepX = TileStep(b.size.x);
            float stepZ = TileStep(b.size.z);
            int fullX = TileRunCount(width, stepX, out float remX);
            int fullZ = TileRunCount(depth, stepZ, out float remZ);
            int totalX = fullX + (remX > 0f ? 1 : 0);
            int totalZ = fullZ + (remZ > 0f ? 1 : 0);
            float startX = -width * 0.5f;
            float startZ = -depth * 0.5f;
            int index = 0;

            for (int ix = 0; ix < totalX; ix++)
            {
                TileRunAt(ix, fullX, startX, stepX, remX, out float cx, out float sx);
                for (int iz = 0; iz < totalZ; iz++)
                {
                    TileRunAt(iz, fullZ, startZ, stepZ, remZ, out float cz, out float sz);

                    // Pack slab pivots are corner pivots — position from the measured bounds
                    // centre, never the pivot.
                    Vector3 pos = new Vector3(cx - sx * b.center.x, rootY, cz - sz * b.center.z);
                    SpawnPiece(
                        prefab,
                        GenPrefix + role + "_" + index,
                        pos,
                        Quaternion.identity,
                        new Vector3(sx, 1f, sz),
                        true);
                    index++;
                }
            }
        }

        void BuildVisualSide(
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
            float step = TileStep(wallB.size.x);
            float wallScaleY = wallHeight / Mathf.Max(wallB.max.y, 0.01f);
            int index = 0;

            if (kind == BastionSideKind.Wall)
            {
                BuildVisualWallRun(orientation, -span * 0.5f, span, edgeOffset, step, wallB, wallScaleY, ref index);
                return;
            }

            // Door / Window: split around one full module (step), NOT the 3 m logical opening, so
            // the frame piece lands exactly one module wide with the logical opening centred in
            // it. The collision layer above keeps the 3 m opening; sim and BastionDoor untouched.
            if (!TryDoorPillars(span, step, doorAlong, out float openCenter, out float westWidth, out float eastWidth))
            {
                BuildVisualWallRun(orientation, -span * 0.5f, span, edgeOffset, step, wallB, wallScaleY, ref index);
                return;
            }

            if (westWidth > TileRemainderEpsilon)
                BuildVisualWallRun(orientation, -span * 0.5f, westWidth, edgeOffset, step, wallB, wallScaleY, ref index);
            if (eastWidth > TileRemainderEpsilon)
                BuildVisualWallRun(orientation, span * 0.5f - eastWidth, eastWidth, edgeOffset, step, wallB, wallScaleY, ref index);

            bool isDoor = kind == BastionSideKind.Door;
            GameObject piece = isDoor ? doorframePiece : windowPiece;
            Bounds pieceB = isDoor ? doorframeB : windowB;
            string role = isDoor ? "_Doorframe_0" : "_WindowPiece_0";

            // Frame/window scaled along the run to exactly one module (doorframe measures ~4.57)
            // and Y-scaled like walls.
            float pieceScaleAlong = step / Mathf.Max(pieceB.size.x, 0.01f);
            float pieceScaleY = wallHeight / Mathf.Max(pieceB.max.y, 0.01f);
            SpawnFacePiece(
                piece,
                pieceB,
                orientation,
                openCenter,
                edgeOffset,
                new Vector3(pieceScaleAlong, pieceScaleY, 1f),
                GenPrefix + orientation + role);
        }

        void BuildVisualWallRun(
            SegmentEdge orientation,
            float start,
            float length,
            float edgeOffset,
            float step,
            Bounds wallB,
            float scaleY,
            ref int index)
        {
            int full = TileRunCount(length, step, out float remainder);
            int total = full + (remainder > 0f ? 1 : 0);
            for (int i = 0; i < total; i++)
            {
                TileRunAt(i, full, start, step, remainder, out float center, out float scaleAlong);
                SpawnFacePiece(
                    wallPiece,
                    wallB,
                    orientation,
                    center,
                    edgeOffset,
                    new Vector3(scaleAlong, scaleY, 1f),
                    GenPrefix + orientation + "_WallPiece_" + index);
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

        void SpawnFacePiece(
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
            Vector3 desired = SideCenter(orientation, alongCenter, 0f, edgeOffset);
            Vector3 pos = new Vector3(desired.x - offset.x, 0f, desired.z - offset.z);
            SpawnPiece(prefab, goName, pos, rot, pieceScale, true);
        }

        void BuildVisualCorners(float width, float depth, float wallHeight, Bounds cornerB)
        {
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;
            float scaleY = wallHeight / Mathf.Max(cornerB.max.y, 0.01f);
            int index = 0;

            // One pillar per footprint corner where both adjacent sides exist.
            if (north != BastionSideKind.None && east != BastionSideKind.None)
                SpawnVisualCorner(halfW, halfD, scaleY, cornerB, ref index);
            if (north != BastionSideKind.None && west != BastionSideKind.None)
                SpawnVisualCorner(-halfW, halfD, scaleY, cornerB, ref index);
            if (south != BastionSideKind.None && east != BastionSideKind.None)
                SpawnVisualCorner(halfW, -halfD, scaleY, cornerB, ref index);
            if (south != BastionSideKind.None && west != BastionSideKind.None)
                SpawnVisualCorner(-halfW, -halfD, scaleY, cornerB, ref index);
        }

        void SpawnVisualCorner(float x, float z, float scaleY, Bounds b, ref int index)
        {
            Vector3 pos = new Vector3(x - b.center.x, 0f, z - b.center.z);
            SpawnPiece(
                cornerPiece,
                GenPrefix + "CornerPillar_" + index,
                pos,
                Quaternion.identity,
                new Vector3(1f, scaleY, 1f),
                true);
            index++;
        }

        void BuildVisualLights(float width, float depth, float wallHeight, Bounds lightB, float moduleStep)
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
                        ceilingLightPiece,
                        GenPrefix + "CeilingLight_" + index,
                        pos,
                        Quaternion.identity,
                        Vector3.one,
                        false);
                    index++;
                }
            }
        }

        GameObject SpawnPiece(
            GameObject prefab,
            string goName,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            bool castShadows)
        {
            if (_geomRoot == null)
                return null;

            GameObject go;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, _geomRoot);
            else
                go = Instantiate(prefab, _geomRoot);
#else
            go = Instantiate(prefab, _geomRoot);
#endif
            if (go == null)
                return null;

            go.name = goName;
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            RebuildObjectsCreated++;

            // Pack colliders stripped — the segment's box colliders own collision. Unstripped
            // convex hulls sit ~0.9 m proud of a visually thin wall.
            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(colliders[i]);
                    continue;
                }
#endif
                Destroy(colliders[i]);
            }

            ShadowCastingMode mode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].shadowCastingMode = mode;

            go.AddComponent<BastionGeneratedGeom>();
            RebuildObjectsMarked++;

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
