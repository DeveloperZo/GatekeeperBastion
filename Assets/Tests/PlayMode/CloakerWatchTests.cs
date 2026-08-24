using System.Collections;
using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// THE CLOAKER'S WATCH RULE, through the real MonoBehaviour layer: a Cloaker in the live
    /// Bastion scene advances while the player looks away and holds position while the player
    /// centres it. The pure cone arithmetic is pinned in EditMode (CloakerWatchRuleTests);
    /// what only PlayMode can see is the seam this test drives — gaze poll -> immediate
    /// Retarget -> NavMeshAgent actually stopping and re-pathing.
    ///
    /// The actor is built by hand (AddComponent + Configure), not spawned from the prefab:
    /// same path a WaveSpawner-spawned body takes through Configure, no dependency on wave
    /// state, and the presentation component is deliberately absent — CloakerGatekeeper is
    /// null-safe without it, and the material swap is prefab wiring pinned elsewhere.
    /// </summary>
    public class CloakerWatchTests
    {
        readonly List<GameObject> _spawned = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.Destroy(go);
            _spawned.Clear();
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        public IEnumerator Cloaker_StopsWhenCentred_AndWalksWhenLookedAwayFrom()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            using (var logs = new PlayTestUtil.ErrorLogCapture())
            {
                yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
                yield return PlayTestUtil.WaitUntil(
                    () =>
                    {
                        var g = Object.FindFirstObjectByType<GameController>();
                        return g != null && g.Sim != null;
                    }, 10f);

                GameController gc = Object.FindFirstObjectByType<GameController>();
                Assert.IsNotNull(gc, "Bastion scene has no GameController.");
                PlayerHealth player = PlayerRegistry.Local;
                Assert.IsNotNull(player, "No local player registered in the Bastion scene.");

                // A spot the player can actually see: walk candidate distances down the
                // player's forward until one lands on the navmesh with a clear line back to
                // the eye. Bastion corridors guarantee at least the short ones.
                if (!TryFindWatchableSpot(player, out Vector3 spot))
                    Assert.Fail("No navmesh point with line of sight found in front of the player.");

                CloakerGatekeeper cloaker = SpawnCloaker(gc, spot);
                yield return null; // Awake/Start, first Retarget

                // Look away first: with no Hatch in the fight an unobserved Cloaker hunts the
                // player, so it should path and move.
                FaceAwayFrom(player, cloaker.transform.position);
                yield return PlayTestUtil.WaitUntil(
                    () => AgentOf(cloaker) != null && !AgentOf(cloaker).isStopped
                          && AgentOf(cloaker).hasPath, 3f);
                Assert.IsFalse(AgentOf(cloaker).isStopped, "unobserved, the Cloaker must advance");

                // Centre it: the freeze. Design budget is 0.25 s (0.1 s gaze poll + the
                // immediate Retarget); the wait allows editor frame jitter on top, and the
                // felt 0.25 s is the playthrough's to confirm.
                FaceToward(player, cloaker.transform.position);
                float faced = Time.time;
                yield return PlayTestUtil.WaitUntil(() => AgentOf(cloaker).isStopped, 2f);
                Assert.LessOrEqual(Time.time - faced, 0.75f,
                    "the stop must land close on the heels of the look (design budget 0.25 s)");
                Assert.AreEqual("idle", cloaker.DevTargetLabel,
                    "watched, the Cloaker targets nothing at all");

                // Held gaze does not strobe: still stopped after several polls.
                yield return new WaitForSeconds(0.5f);
                Assert.IsTrue(AgentOf(cloaker).isStopped, "held in the cone, it stays held");

                // Look away: it resumes and re-paths.
                FaceAwayFrom(player, cloaker.transform.position);
                yield return PlayTestUtil.WaitUntil(
                    () => !AgentOf(cloaker).isStopped && AgentOf(cloaker).hasPath, 2f);
                Assert.IsFalse(AgentOf(cloaker).isStopped, "looked away from, it walks again");

                logs.AssertNoErrors("cloaker watch rule (" + logs.NoiseSummary() + ")");
            }
        }

        static NavMeshAgent AgentOf(CloakerGatekeeper cloaker)
        {
            return cloaker != null ? cloaker.GetComponent<NavMeshAgent>() : null;
        }

        static bool TryFindWatchableSpot(PlayerHealth player, out Vector3 spot)
        {
            float[] distances = { 20f, 14f, 10f, 7f, 5f };
            Vector3 eye = player.transform.position + Vector3.up * 1.6f;
            for (int i = 0; i < distances.Length; i++)
            {
                Vector3 want = player.transform.position + player.transform.forward * distances[i];
                if (!NavMesh.SamplePosition(want, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                    continue;
                Vector3 body = hit.position + Vector3.up * 1.2f;
                Vector3 to = body - eye;
                if (Physics.Raycast(eye, to.normalized, to.magnitude - 0.5f,
                                    ~0, QueryTriggerInteraction.Ignore))
                    continue; // something solid in between — the gaze could never land
                spot = hit.position;
                return true;
            }
            spot = default;
            return false;
        }

        CloakerGatekeeper SpawnCloaker(GameController gc, Vector3 at)
        {
            var go = new GameObject("PlayTest_Cloaker");
            _spawned.Add(go);
            go.transform.position = at + Vector3.up; // baseOffset height, set before the agent binds
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.baseOffset = 1f;
            var cloaker = go.AddComponent<CloakerGatekeeper>(); // RequireComponent adds Damageable
            cloaker.Configure(WaveDirector.StatsFor(GatekeeperKind.Cloaker), gc);
            return cloaker;
        }

        static void FaceToward(PlayerHealth player, Vector3 target)
        {
            Vector3 to = target - player.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.001f) return;
            player.transform.rotation = Quaternion.LookRotation(to);
        }

        static void FaceAwayFrom(PlayerHealth player, Vector3 target)
        {
            Vector3 to = target - player.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.001f) return;
            player.transform.rotation = Quaternion.LookRotation(-to);
        }
    }
}
