using UnityEngine;
using GatekeepersProtocol.Sim;
using SegmentEdge = GatekeepersProtocol.Game.BastionSegment.SegmentEdge;

namespace GatekeepersProtocol.Game
{
    internal static partial class BastionSegmentVisuals
    {
        /// <summary>Re-dress one edge after a spawn closet punches a Door aperture.
        /// Other faces stay as authored.</summary>
        internal static void RebuildFace(BastionSegment segment, Transform geomRoot, SegmentEdge orientation)
        {
            if (segment.WallPiece == null || segment.DoorframePiece == null || segment.WindowPiece == null)
                return;

            Bounds wallB = BastionPackTiling.Measure(segment.WallPiece);
            Bounds doorframeB = BastionPackTiling.Measure(segment.DoorframePiece);
            Bounds windowB = BastionPackTiling.Measure(segment.WindowPiece);

            Vector2 footprint = new Vector2(
                BastionKit.FootprintWidth(segment.BlockKind),
                BastionKit.FootprintDepth(segment.BlockKind));
            float width = footprint.x;
            float depth = footprint.y;
            float wallHeight = BastionKit.StoreyHeight - BastionKit.FloorSlabThickness;
            float thickness = BastionKit.WallThickness;
            Transform walls = BastionSegment.EnsureGeomGroup(geomRoot, BastionSegment.GroupWalls);

            BastionSideKind kind;
            float along;
            switch (orientation)
            {
                case SegmentEdge.North:
                    kind = segment.North;
                    along = segment.NorthDoorAlong;
                    break;
                case SegmentEdge.South:
                    kind = segment.South;
                    along = segment.SouthDoorAlong;
                    break;
                case SegmentEdge.East:
                    kind = segment.East;
                    along = segment.EastDoorAlong;
                    break;
                default:
                    kind = segment.West;
                    along = segment.WestDoorAlong;
                    break;
            }

            BuildFace(
                segment, walls, orientation, kind, along,
                width, depth, wallHeight, thickness, wallB, doorframeB, windowB);
        }
    }
}
