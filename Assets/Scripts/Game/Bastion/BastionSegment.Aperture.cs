using System;
using UnityEngine;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Game
{
    public sealed partial class BastionSegment
    {
        /// <summary>True when this edge already has a pack doorframe (leaf stripped). Used so
        /// a spawn closet does not re-stamp an opening that is already there.</summary>
        public bool HasDoorOpeningOn(BastionDoorEdge edge)
        {
            Transform geom = FindGeomRoot();
            if (geom == null)
                return false;
            string prefix = GenPrefix + ToSegmentEdge(edge) + "_Doorframe";
            Transform[] all = geom.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>Replace one edge's solid wall with a Door aperture (collision pillars plus
        /// pack doorframe). Authored rooms refuse full <see cref="Rebuild"/>; this is the
        /// surgical path so a hung closet is not sealed by the wall it sits on. NavMesh
        /// bakes render meshes — the frame leaf is deactivated like a passage door.</summary>
        public void PunchDoorEdge(BastionDoorEdge edge)
        {
            if (!CanMutateHierarchy())
                return;

            SetSideDoor(edge);
            EnsureGeomRoot();
            if (_geomRoot == null)
                return;

            SegmentEdge se = ToSegmentEdge(edge);
            ClearGeneratedOnEdge(se);

            Vector2 footprint = new Vector2(
                BastionKit.FootprintWidth(blockKind),
                BastionKit.FootprintDepth(blockKind));
            float width = footprint.x;
            float depth = footprint.y;
            float wallHeight = BastionKit.StoreyHeight - BastionKit.FloorSlabThickness;
            float wallCenterY = wallHeight * 0.5f;
            float thickness = BastionKit.WallThickness;
            // ONE copy of the rule, in the Sim: the two ends of a shared passage must agree.
            float openingWidth = BastionKit.ApertureWidth(
                blockKind, DoorOpeningWidth, DoorOpeningMaxShare);

            BuildSide(se, SideKind(se), SideAlong(se), width, depth, wallHeight, wallCenterY, thickness, openingWidth);
            BastionSegmentVisuals.RebuildFace(this, _geomRoot, se);
            rebuildStamp = SettingsHash();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        static SegmentEdge ToSegmentEdge(BastionDoorEdge edge)
        {
            switch (edge)
            {
                case BastionDoorEdge.North: return SegmentEdge.North;
                case BastionDoorEdge.South: return SegmentEdge.South;
                case BastionDoorEdge.East: return SegmentEdge.East;
                default: return SegmentEdge.West;
            }
        }

        BastionSideKind SideKind(SegmentEdge se)
        {
            switch (se)
            {
                case SegmentEdge.North: return north;
                case SegmentEdge.South: return south;
                case SegmentEdge.East: return east;
                default: return west;
            }
        }

        float SideAlong(SegmentEdge se)
        {
            switch (se)
            {
                case SegmentEdge.North: return northDoorAlong;
                case SegmentEdge.South: return southDoorAlong;
                case SegmentEdge.East: return eastDoorAlong;
                default: return westDoorAlong;
            }
        }

        void ClearGeneratedOnEdge(SegmentEdge se)
        {
            string prefix = GenPrefix + se + "_";
            Transform[] all = _geomRoot.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                Transform t = all[i];
                if (t == null || t == _geomRoot)
                    continue;
                if (!t.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                t.SetParent(null);
                DestroyObject(t.gameObject);
            }
        }
    }
}
