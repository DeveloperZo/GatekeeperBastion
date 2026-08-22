using System.Collections;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// CharacterController walk up the co-op ship stair.
    ///
    /// The first version of this test hardcoded its start position as a literal that
    /// happened to match wherever the (then-broken) ramp collider was sitting - not a
    /// position derived from the actual "Stair" mesh. That let it keep passing while the
    /// ramp was 1.8 m off the true tread line: the test and the bug shared the same wrong
    /// assumption, so the test could never catch it (found instead by a human walking up
    /// to the visible steps and hitting nothing). This version derives its approach from
    /// the live "Stair" renderer's world bounds every run, starts outside the room rather
    /// than already adjacent to the ramp (so it exercises the approach path and any
    /// partition in the way, not just the ramp itself), and sweeps three lateral offsets
    /// across the tread width so a partial misalignment fails instead of passing by luck.
    /// </summary>
    public class ShipCoopStairWalkTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        static IEnumerator WalkFrom(Vector3 start, PlayerHealth player, CharacterController cc)
        {
            player.transform.SetPositionAndRotation(start, Quaternion.identity);
            Physics.SyncTransforms();
            cc.Move(Vector3.zero);

            float yVel = -1f;
            const float gravity = -20f;
            const float speed = 5f;
            for (int i = 0; i < 400; i++)
            {
                float dt = Time.deltaTime;
                if (dt <= 0f)
                    dt = 0.02f;
                yVel = cc.isGrounded ? -1f : yVel + gravity * dt;
                cc.Move((Vector3.forward * speed + Vector3.up * yVel) * dt);
                if (player.transform.position.y >= 4.5f && player.transform.position.z >= 31.0f)
                    yield break;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CharacterController_ReachesDeckTwo_WalkingTheStair()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);

            var stairGo = GameObject.Find("Stair");
            Assert.IsNotNull(stairGo, "Ship - CoOp has no 'Stair' object to approach.");
            var stairRenderer = stairGo.GetComponent<Renderer>();
            Assert.IsNotNull(stairRenderer, "Stair has no Renderer to derive an approach from.");
            float stairCenterX = stairRenderer.bounds.center.x;
            float stairFootZ = stairRenderer.bounds.min.z;

            var walk = GameObject.Find("GStairWalk");
            Assert.IsNotNull(walk, "Ship - CoOp has no GStairWalk — the stair has no CharacterController surface.");
            var walkCol = walk.GetComponent<Collider>();
            Assert.IsNotNull(walkCol, "GStairWalk has no collider.");
            Assert.IsTrue(walkCol.enabled, "GStairWalk collider is disabled.");
            // The walk surface must actually sit under the visible mesh — this is exactly
            // the check that would have caught the 1.8 m X-misalignment before a human did.
            Assert.Less(Mathf.Abs(walkCol.bounds.center.x - stairCenterX), 1.0f,
                "GStairWalk is not aligned with the visible Stair mesh: walk surface x="
                + walkCol.bounds.center.x + " vs mesh x=" + stairCenterX
                + " — walking up to the actual steps would miss the collider.");

            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);
            var player = PlayerRegistry.Local;
            var fpc = player.GetComponent<FirstPersonController>();
            var cc = player.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "Player has no CharacterController.");
            if (fpc != null)
                fpc.enabled = false;

            // Three lateral offsets across the tread width, starting 2 m back from the
            // mesh's own leading edge so each run walks the approach, not just the ramp.
            foreach (float xOffset in new[] { -0.6f, 0f, 0.6f })
            {
                Vector3 start = new Vector3(stairCenterX + xOffset, 1.1f, stairFootZ - 2f);
                yield return WalkFrom(start, player, cc);

                Vector3 p = player.transform.position;
                Assert.GreaterOrEqual(p.y, 4.5f,
                    "CharacterController did not reach deck 2 from x-offset " + xOffset
                    + ". Started " + start + ", ended at " + p + " grounded=" + cc.isGrounded + ".");
                Assert.GreaterOrEqual(p.z, 30.5f,
                    "CharacterController did not travel the stair run from x-offset " + xOffset
                    + ". Ended at " + p + ".");
            }
        }
    }
}
