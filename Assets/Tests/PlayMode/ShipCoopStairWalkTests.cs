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
    /// Climb direction is GStairWalk's planar forward (the ramp collider). Using
    /// Stair.forward walked the back of SM_Prop_Stairs_01 — the kit treads face
    /// the other way from the transform.
    /// </summary>
    public class ShipCoopStairWalkTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>Height that counts as standing on deck 2 rather than still on the flight.</summary>
        const float DeckTwoY = 4.9f;

        /// <summary>How far past the top tread the walk has to get before the climb counts.
        /// y alone is not enough: the ramp REACHES deck height while still inside the stair
        /// well, so a barrier across the head of the flight — which is exactly what the void
        /// railing was — lets a y-only assertion pass on a stair you cannot actually leave.</summary>
        const float PastHead = 0.75f;

        static IEnumerator WalkFrom(Vector3 start, Vector3 climb, Vector3 head,
            PlayerHealth player, CharacterController cc)
        {
            player.transform.SetPositionAndRotation(start, Quaternion.LookRotation(climb));
            Physics.SyncTransforms();
            cc.Move(Vector3.zero);

            float yVel = -1f;
            const float gravity = -20f;
            const float speed = 5f;
            for (int i = 0; i < 600; i++)
            {
                float dt = Time.deltaTime;
                if (dt <= 0f)
                    dt = 0.02f;
                yVel = cc.isGrounded ? -1f : yVel + gravity * dt;
                cc.Move((climb * speed + Vector3.up * yVel) * dt);
                if (OnDeckTwo(player.transform.position, climb, head))
                    yield break;
                yield return null;
            }
        }

        static bool OnDeckTwo(Vector3 p, Vector3 climb, Vector3 head)
        {
            return p.y >= DeckTwoY && Vector3.Dot(p - head, climb) > PastHead;
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

            var walk = GameObject.Find("GStairRamp");
            if (walk == null)
                walk = GameObject.Find("GStairWalk");
            Assert.IsNotNull(walk, "Ship - CoOp Stair has no GStairRamp / GStairWalk — no CharacterController surface.");
            var walkCol = walk.GetComponent<Collider>();
            Assert.IsNotNull(walkCol, "GStairWalk has no collider.");
            Assert.IsTrue(walkCol.enabled, "GStairWalk collider is disabled.");

            Vector3 climb = walk.transform.forward;
            climb.y = 0f;
            Assert.Greater(climb.sqrMagnitude, 0.01f, "GStairWalk forward is vertical.");
            climb.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, climb);

            Bounds mesh = stairRenderer.bounds;
            float halfRun = mesh.extents.x * Mathf.Abs(climb.x) + mesh.extents.z * Mathf.Abs(climb.z);
            Vector3 foot = mesh.center - climb * halfRun;
        Vector3 head = mesh.center + climb * halfRun;

            Assert.GreaterOrEqual(mesh.center.x, walkCol.bounds.min.x - 0.1f,
                "GStairWalk's west edge is east of the visible Stair mesh's centre.");
            Assert.LessOrEqual(mesh.center.x, walkCol.bounds.max.x + 0.1f,
                "GStairWalk's east edge is west of the visible Stair mesh's centre.");

            foreach (float side in new[] { walkCol.bounds.min.x - 0.4f, walkCol.bounds.max.x + 0.4f })
            {
                Vector3 beside = new Vector3(side, 2f, walkCol.bounds.center.z);
                Assert.IsTrue(
                    Physics.Raycast(beside, Vector3.down, out RaycastHit deck, 4f),
                    "No deck-1 collider beside the stair at " + beside + " — the well has a hole.");
                Assert.Greater(deck.point.y, -0.6f,
                    "Beside-stair hit at y=" + deck.point.y + " on " + deck.collider.name
                    + " — not deck 1.");
            }

            Vector3 approach = foot - climb;
            approach.y = 0.5f;
            Assert.IsTrue(
                Physics.Raycast(approach, climb, out RaycastHit face, 8f),
                "No collider on the stair approach from " + approach + ".");
            Assert.IsTrue(
                face.collider.name == "GStairRamp" || face.collider.name == "GStairWalk",
                "Approach hit " + face.collider.name + " at " + face.point);
            float slope = Vector3.Angle(face.normal, Vector3.up);
            Assert.Less(slope, 45f,
                "Approach slope " + slope.ToString("F1")
                + " at " + face.point + " — foot end-cap is a wall.");

            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);
            var player = PlayerRegistry.Local;
            var fpc = player.GetComponent<FirstPersonController>();
            var cc = player.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "Player has no CharacterController.");
            if (fpc != null)
                fpc.enabled = false;

            float inner = walkCol.bounds.extents.x - 0.45f;
            Assert.Greater(inner, 0.2f, "GStairWalk is narrower than the capsule plus clearance.");
            foreach (float lat in new[] { -inner, 0f, inner })
            {
                Vector3 start = foot - climb * 2f + right * lat;
                start.y = 1.1f;
                yield return WalkFrom(start, climb, head, player, cc);

                Vector3 p = player.transform.position;
                float past = Vector3.Dot(p - head, climb);
                Assert.GreaterOrEqual(p.y, DeckTwoY,
                    "CharacterController did not reach deck 2 from lateral " + lat
                    + ". Started " + start + ", ended at " + p + " grounded=" + cc.isGrounded + ".");
                Assert.Greater(past, PastHead,
                    "CharacterController reached deck height from lateral " + lat
                    + " but never got off the flight — ended at " + p + ", only "
                    + past.ToString("F2") + " m past the top tread. Something is walling the "
                    + "head of the stair.");
            }
        }
    }
}
