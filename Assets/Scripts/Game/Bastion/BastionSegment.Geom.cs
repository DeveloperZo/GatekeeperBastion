using UnityEngine;
using UnityEngine.AI;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Game
{
    // Collision layer — the exact box math the primitive kit shipped, renderers removed.
    // Player physics and shot blocking are unchanged; the NavMesh ignores these (the bake
    // reads render meshes). The pack-mesh visual layer lives in BastionSegmentVisuals.
    public sealed partial class BastionSegment
    {
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
                case BastionSideKind.OpenSpan:
                    BuildOpenSpanWall(orientation, span, wallHeight, thickness, edgeOffset);
                    break;
            }
        }

        /// <summary>The wall's absence: one full-height divider at the centre of the span and
        /// nothing else. No pillars at the ends, because the span IS the opening — that is what
        /// separates this from a very wide Door.
        ///
        /// A divider wider than the span would be a solid wall wearing the wrong name, so that
        /// case builds the wall it actually is rather than emitting a box that overhangs the
        /// room. A divider of zero leaves a clear span, which is legal and emits nothing.</summary>
        void BuildOpenSpanWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float thickness,
            float edgeOffset)
        {
            float divider = OpenSpanDividerWidth;
            if (divider <= 0f)
                return;
            if (divider >= span)
            {
                BuildSolidWall(orientation, span, wallHeight, wallHeight * 0.5f, thickness, edgeOffset);
                return;
            }
            CreateBox(
                GenPrefix + orientation + "_Span_Divider",
                SideCenter(orientation, 0f, wallHeight * 0.5f, edgeOffset),
                SideSize(orientation, divider, wallHeight, thickness));
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

        internal static Vector3 SideCenter(SegmentEdge orientation, float along, float y, float edgeOffset)
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

        public const string GroupFloors = "Floors";
        public const string GroupCeiling = "Ceiling";
        public const string GroupWalls = "Walls";

        /// <summary>Which group node a generated piece belongs under (Zo 2026-08-14: geom
        /// grouped by category for authoring). Gen_Floor* → Floors, Gen_Ceiling* → Ceiling
        /// (tiles and lights), every other Gen_* — walls, doorframes, windows, corners,
        /// walk blocks — → Walls.</summary>
        public static string GeomGroupNameFor(string pieceName)
        {
            if (pieceName.StartsWith(GenPrefix + "Floor", System.StringComparison.Ordinal))
                return GroupFloors;
            if (pieceName.StartsWith(GenPrefix + "Ceiling", System.StringComparison.Ordinal))
                return GroupCeiling;
            return GroupWalls;
        }

        /// <summary>Group node under the geom root, created on first use. Carries the
        /// generated marker so <see cref="ClearGeneratedHierarchy"/> and rebuilds clear it
        /// wholesale — hand-placed content belongs outside _geom.</summary>
        public static Transform EnsureGeomGroup(Transform geomRoot, string groupName)
        {
            Transform existing = geomRoot.Find(groupName);
            if (existing != null)
                return existing;

            var go = new GameObject(groupName);
            go.transform.SetParent(geomRoot, false);
            RebuildObjectsCreated++;
            go.AddComponent<BastionGeneratedGeom>();
            RebuildObjectsMarked++;
            return go.transform;
        }

        void CreateBox(string name, Vector3 localCenter, Vector3 size)
        {
            if (_geomRoot == null)
                return;

            // Collider-only: the visual layer brings the pack meshes. The unit BoxCollider under
            // the box's localScale keeps the exact physics footprint the primitive kit shipped.
            var go = new GameObject(name);
            go.transform.SetParent(EnsureGeomGroup(_geomRoot, GeomGroupNameFor(name)), false);
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
            go.transform.SetParent(EnsureGeomGroup(_geomRoot, GeomGroupNameFor(name)), false);
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

        /// <summary>Hung spawn closets need a Door aperture on this edge.</summary>
        public void SetSideDoor(BastionDoorEdge edge)
        {
            switch (edge)
            {
                case BastionDoorEdge.North: north = BastionSideKind.Door; break;
                case BastionDoorEdge.South: south = BastionSideKind.Door; break;
                case BastionDoorEdge.East: east = BastionSideKind.Door; break;
                case BastionDoorEdge.West: west = BastionSideKind.Door; break;
            }
        }
    }
}
