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
            Assert.AreEqual(3f, size.x, 1e-3f);
            Assert.AreEqual(BastionKit.StoreyHeight - BastionKit.FloorSlabThickness, size.y, 1e-3f);

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
