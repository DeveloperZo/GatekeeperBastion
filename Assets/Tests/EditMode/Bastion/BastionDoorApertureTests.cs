using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    public class BastionDoorApertureTests
    {
        [Test]
        public void TryGetDoorAperture_NorthDoor_AtHalfDepth()
        {
            var go = new GameObject("seg");
            var segment = go.AddComponent<BastionSegment>();
            segment.BlockKind = BastionBlockKind.OpenArea;
            segment.North = BastionSideKind.Door;

            Assert.IsTrue(segment.TryGetDoorAperture(
                BastionDoorEdge.North, out Vector3 localPos, out float yawY, out Vector3 size));
            float halfD = BastionKit.FootprintDepth(BastionBlockKind.OpenArea) * 0.5f;
            Assert.AreEqual(halfD, localPos.z, 1e-3f);
            Assert.AreEqual(0f, yawY, 1e-3f);
            // DERIVED, NOT RESTATED. This pinned a literal 3 and went stale the moment the
            // doorways were widened 3 -> 6 on 2026-08-28 (Zo: "widen the doorways... more
            // consistent firing angles"), and the clamp's 0.4 was restated here until it moved
            // with the 6 -> 10 widening on 2026-09-04. Both halves now come off the constants:
            // an OpenArea is 42 m across so the clamp allows far more than the authored width,
            // and the authored width is what survives — on a small block the clamp wins
            // instead, and that is the rule, not the number.
            float shortSide = Mathf.Min(BastionKit.FootprintWidth(BastionBlockKind.OpenArea),
                                        BastionKit.FootprintDepth(BastionBlockKind.OpenArea));
            Assert.AreEqual(
                Mathf.Min(BastionSegment.DoorOpeningWidth,
                          shortSide * BastionSegment.DoorOpeningMaxShare),
                size.x, 1e-3f, "the aperture is not the authored width on a block big enough "
                + "to carry it");
            Assert.AreEqual(BastionSegment.DoorOpeningHeight, size.y, 1e-3f);

            // FULL HEIGHT, so BuildDoorWall emits no lintel. A metre of geometry hanging across
            // every opening is invisible to a floor-plan check and directly in the path of a
            // shot, which is what it was until the doorways were opened up.
            Assert.AreEqual(BastionKit.StoreyHeight - BastionKit.FloorSlabThickness,
                BastionSegment.DoorOpeningHeight, 1e-3f,
                "the doorway is not full height, so every one of them carries a lintel again");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryGetDoorAperture_WallSide_ReturnsFalse()
        {
            var go = new GameObject("seg");
            var segment = go.AddComponent<BastionSegment>();
            segment.BlockKind = BastionBlockKind.Full;
            segment.North = BastionSideKind.Wall;

            Assert.IsFalse(segment.TryGetDoorAperture(
                BastionDoorEdge.North, out _, out _, out _));
            Object.DestroyImmediate(go);
        }
    }
}
