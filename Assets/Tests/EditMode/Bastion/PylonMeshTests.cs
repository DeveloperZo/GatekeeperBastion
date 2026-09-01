using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The turret's pylon meshes are built once and handed out by reference, and the
    /// crystal's faces do not share corners. Both claims are load-bearing: a turret is placed and
    /// despawned many times a Wave and exists twice over in a session, so a per-call build would
    /// leak two meshes per placement; and shared corners would let RecalculateNormals average the
    /// crystal into a rounded blob instead of the cut stone the machine's read depends on.
    /// <para>Unity Test Runner only — Mesh is a UnityEngine type, so the offline harness cannot
    /// compile this file and carries a row for it in its EXCLUDED table.</para></summary>
    public class PylonMeshTests
    {
        // ================================================================
        //  Built once, handed out by reference
        // ================================================================

        [Test]
        public void Crystal_IsTheSameInstanceEveryCall()
        {
            var first = PylonMesh.Crystal;
            Assert.IsNotNull(first, "the crystal mesh was never built");
            Assert.AreSame(first, PylonMesh.Crystal,
                "the crystal is rebuilt per call — every placement would allocate a fresh mesh "
                + "and leave the last one for the collector, twice over on a client that also "
                + "carries a presentation copy");
            Assert.AreSame(first, PylonMesh.Crystal, "the second call cached but the third did not");
        }

        [Test]
        public void Plinth_IsTheSameInstanceEveryCall()
        {
            var first = PylonMesh.Plinth;
            Assert.IsNotNull(first, "the plinth mesh was never built");
            Assert.AreSame(first, PylonMesh.Plinth,
                "the plinth is rebuilt per call — same leak as the crystal, on the part every "
                + "turret in the room stands on");
            Assert.AreSame(first, PylonMesh.Plinth, "the second call cached but the third did not");
        }

        [Test]
        public void TheTwoPartsAreNotTheSameMesh()
        {
            // Cheap, and it catches the caching field being crossed — one static handed out for
            // both parts would pass both tests above and stand a pylon made of two crystals.
            Assert.AreNotSame(PylonMesh.Crystal, PylonMesh.Plinth,
                "both parts came back as one mesh");
        }

        // ================================================================
        //  The shapes, counted
        // ================================================================

        [Test]
        public void Crystal_IsAnOctahedronWithUnsharedCorners()
        {
            var mesh = PylonMesh.Crystal;

            // 24, not 6. An octahedron has six corners, but the criterion asks for a FACETED
            // crystal and URP/Lit has no flat-shading path — six shared verts under
            // RecalculateNormals give radial per-vertex normals that shade as a sphere. So each
            // of the eight faces carries its own three corners.
            Assert.AreEqual(24, mesh.vertexCount,
                "the crystal's faces are sharing corners — RecalculateNormals will average them "
                + "and the crystal will shade as a rounded blob rather than as cut stone");
            Assert.AreEqual(24, mesh.triangles.Length,
                "the crystal is not eight triangles");
            Assert.AreEqual(8, mesh.triangles.Length / 3);
        }

        [Test]
        public void Plinth_IsACappedHexagonalFrustum()
        {
            var mesh = PylonMesh.Plinth;

            // Six side quads at four unshared verts each (24), plus a fan cap at either end
            // around its own centre vertex (7 each).
            Assert.AreEqual(38, mesh.vertexCount, "the plinth is not a six-sided frustum with "
                + "unshared side quads and a centre vertex per cap");

            // Twelve side triangles plus six per cap. The floor-facing cap is never seen in
            // play, but an open mesh reads as a hole the moment the camera dips below the
            // pedestal — so both caps are counted here rather than assumed.
            Assert.AreEqual(72, mesh.triangles.Length, "the plinth is not 24 triangles");
            Assert.AreEqual(24, mesh.triangles.Length / 3);
        }

        [Test]
        public void EveryTriangleIndexPointsAtARealVertex()
        {
            // A mesh built with an index past its own vertex array throws inside Unity rather
            // than returning, so this is really a guard on the arithmetic in both builders
            // staying in step with the array sizes they allocate.
            AssertIndicesInRange(PylonMesh.Crystal);
            AssertIndicesInRange(PylonMesh.Plinth);
        }

        static void AssertIndicesInRange(Mesh mesh)
        {
            int count = mesh.vertexCount;
            var tris = mesh.triangles;
            Assert.AreEqual(0, tris.Length % 3, mesh.name + " has a partial triangle");
            for (int i = 0; i < tris.Length; i++)
            {
                Assert.GreaterOrEqual(tris[i], 0, mesh.name + " index " + i + " is negative");
                Assert.Less(tris[i], count,
                    mesh.name + " index " + i + " points past its " + count + " vertices");
            }
        }
    }
}
