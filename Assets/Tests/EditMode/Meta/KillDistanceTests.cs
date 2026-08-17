using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Planar hatchDist / spawnDist arithmetic — no formula, no clamp.</summary>
    public class KillDistanceTests
    {
        [Test]
        public void BodyOnHatchFace_ReadsUnderOneMetre()
        {
            // Hatch surface at the south face; body pressed against it.
            float d = KillLog.PlanarMetres(0f, 1f, -20f, 0f, 1.5f, -20f);
            Assert.Less(d, 1f);
        }

        [Test]
        public void BodyAtFarWall_ReadsRoughlyFiftyMetres()
        {
            // Floor box depth is 52 m (SceneBuilder); hatch face near z=-20, far wall ~z=30.
            float d = KillLog.PlanarMetres(0f, 1f, 30f, 0f, 1.5f, -20f);
            Assert.AreEqual(50f, d, 1f);
        }

        [Test]
        public void YDifferenceDoesNotCount()
        {
            float d = KillLog.PlanarMetres(0f, 0f, 0f, 0f, 100f, 10f);
            Assert.AreEqual(10f, d, 1e-4f);
        }
    }
}
