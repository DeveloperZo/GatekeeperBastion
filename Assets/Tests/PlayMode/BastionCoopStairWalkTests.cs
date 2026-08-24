using System.Collections;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// CharacterController climb of the three co-op bastion stairs.
    /// Approach and climb direction come from each stair's live Renderer.bounds
    /// and GStairWalk forward — not from numbers copied out of the emitter.
    /// </summary>
    public class BastionCoopStairWalkTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        const float StoreyOneY = 4.9f;
        const float PastHead = 0.75f;

        static readonly string[] DressNames =
        {
            "Stair_R1_U1", "Stair_R6_U6", "Stair_R10_U10"
        };

        static readonly string[] WalkNames =
        {
            "GStairWalk_R1_U1", "GStairWalk_R6_U6", "GStairWalk_R10_U10"
        };

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
                if (OnStoreyOne(player.transform.position, climb, head))
                    yield break;
                yield return null;
            }
        }

        static bool OnStoreyOne(Vector3 p, Vector3 climb, Vector3 head)
        {
            return p.y >= StoreyOneY && Vector3.Dot(p - head, climb) > PastHead;
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator CharacterController_ReachesStoreyOne_OnEachStair()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);

            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);
            var player = PlayerRegistry.Local;
            if (player == null)
            {
                var go = GameObject.Find("Player");
                Assert.IsNotNull(go, "Bastion_Coop has no PlayerRegistry.Local and no Player object.");
                player = go.GetComponent<PlayerHealth>();
            }

            Assert.IsNotNull(player, "Bastion_Coop has no PlayerHealth to walk.");
            var fpc = player.GetComponent<FirstPersonController>();
            var cc = player.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "Player has no CharacterController.");
            if (fpc != null)
                fpc.enabled = false;

            for (int s = 0; s < DressNames.Length; s++)
                yield return ClimbOne(DressNames[s], WalkNames[s], player, cc);
        }

        static IEnumerator ClimbOne(
            string dressName, string walkName, PlayerHealth player, CharacterController cc)
        {
            var stairGo = GameObject.Find(dressName);
            Assert.IsNotNull(stairGo, "Bastion_Coop has no " + dressName + ".");
            var stairRenderer = stairGo.GetComponent<Renderer>();
            Assert.IsNotNull(stairRenderer, dressName + " has no Renderer to derive an approach from.");

            var walk = GameObject.Find(walkName);
            Assert.IsNotNull(walk, dressName + " has no " + walkName + " — no CharacterController surface.");
            var walkCol = walk.GetComponent<Collider>();
            Assert.IsNotNull(walkCol, walkName + " has no collider.");
            Assert.IsTrue(walkCol.enabled, walkName + " collider is disabled.");

            Vector3 climb = walk.transform.forward;
            climb.y = 0f;
            Assert.Greater(climb.sqrMagnitude, 0.01f, walkName + " forward is vertical.");
            climb.Normalize();

            Bounds mesh = stairRenderer.bounds;
            float halfRun = mesh.extents.x * Mathf.Abs(climb.x) + mesh.extents.z * Mathf.Abs(climb.z);
            Vector3 foot = mesh.center - climb * halfRun;
            Vector3 head = mesh.center + climb * halfRun;

            Assert.GreaterOrEqual(mesh.center.x, walkCol.bounds.min.x - 0.1f,
                walkName + " west edge is east of " + dressName + " mesh centre.");
            Assert.LessOrEqual(mesh.center.x, walkCol.bounds.max.x + 0.1f,
                walkName + " east edge is west of " + dressName + " mesh centre.");

            Vector3 approach = foot - climb;
            approach.y = 0.5f;
            Assert.IsTrue(
                Physics.Raycast(approach, climb, out RaycastHit face, 8f),
                "No collider on the " + dressName + " approach from " + approach + ".");
            Assert.AreEqual(walk.name, face.collider.name,
                dressName + " approach hit " + face.collider.name + " at " + face.point);
            float slope = Vector3.Angle(face.normal, Vector3.up);
            Assert.Less(slope, 45f,
                dressName + " approach slope " + slope.ToString("F1")
                + " at " + face.point + " — foot end-cap is a wall.");

            Vector3 start = foot - climb * 2f;
            start.y = 1.1f;
            yield return WalkFrom(start, climb, head, player, cc);

            Vector3 p = player.transform.position;
            float past = Vector3.Dot(p - head, climb);
            Assert.GreaterOrEqual(p.y, StoreyOneY,
                "CharacterController did not reach storey 1 on " + dressName
                + ". Started " + start + ", ended at " + p + " grounded=" + cc.isGrounded + ".");
            Assert.Greater(past, PastHead,
                "CharacterController reached storey-1 height on " + dressName
                + " but never got off the flight — ended at " + p + ", only "
                + past.ToString("F2") + " m past the top tread.");
        }
    }
}
