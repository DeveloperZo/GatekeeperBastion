using System.Collections;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>M30 PlayMode: decoy has no Damageable (player fire cannot reduce HP).</summary>
    public class DecoyPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        public IEnumerator SpawnedDecoy_HasNoDamageable()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    var g = UnityEngine.Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);

            var go = new GameObject("DecoyProbe");
            var bayHost = new GameObject("BayProbe");
            var bay = bayHost.AddComponent<DecoyBay>();
            bay.ResetForRun(PayloadId.Decoy);
            Assert.IsTrue(bay.TryPlace(Vector3.zero));
            var decoy = bay.Live;
            Assert.IsNotNull(decoy);
            Assert.IsNull(decoy.GetComponent<Damageable>(),
                "player FireRay damages via Damageable — decoy must not carry one");
            Assert.AreEqual(120f, decoy.Hp, 1e-3f);
            decoy.ApplyDamage(10f);
            Assert.AreEqual(110f, decoy.Hp, 1e-3f);

            Object.Destroy(decoy.gameObject);
            Object.Destroy(bayHost);
            Object.Destroy(go);
            yield return null;
        }
    }
}
