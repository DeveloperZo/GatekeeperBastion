using System.Collections;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>Host plus two clients. Wave counter and ending match on all three;
    /// AwardKill splits three ways.</summary>
    public class NetThreePlayerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        GameObject _mirrorPrefab;
        GameController _gc;

        [UnityTearDown]
        public IEnumerator DestroyHostRun()
        {
            if (_gc != null)
            {
                Object.Destroy(_gc.gameObject);
                _gc = null;
            }
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _mirrorPrefab = CreateNetworkObjectPrefab("NetThreeMirror");
            _mirrorPrefab.AddComponent<NetRunMirror>();
        }

        [UnityTest]
        public IEnumerator ThreePlayers_KillSplits_WaveAndEndMatch()
        {
            var go = new GameObject("HostGC3");
            _gc = go.AddComponent<GameController>();
            yield return null;
            yield return null;
            Assert.IsNotNull(_gc.Sim);
            _gc.Sim.SetPlayerCount(3);
            WaveSpec spec = default;
            _gc.Sim.WaveStarted += s => spec = s;
            _gc.Sim.TriggerDraw();
            Assert.AreEqual(54, spec.enemyCount);

            _gc.Sim.AwardKill();
            Assert.AreEqual(9L, _gc.Sim.UnbankedOf(0));
            Assert.AreEqual(8L, _gc.Sim.UnbankedOf(1));
            Assert.AreEqual(8L, _gc.Sim.UnbankedOf(2));

            // No Bind: LateUpdate would overwrite Wave from the live sim (index 1 after
            // TriggerDraw). NetRunMirrorTests uses the same unbound pattern.
            var serverGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            var serverMirror = serverGo.GetComponent<NetRunMirror>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("mirror did not spawn on both clients");

            serverMirror.Wave.Value = 2;
            var summary = new EndSummary
            {
                Kind = RunEndKind.Cleared,
                GrossEarned = 100L,
                RepairSpent = 10L,
                Net = 90L,
                DeepestWave = 2,
                Banked = 50L
            };
            serverMirror.ServerShowEnd(summary);

            yield return WaitForConditionOrTimeOut(() =>
            {
                for (int c = 0; c < 2; c++)
                {
                    if (!m_ClientNetworkManagers[c].SpawnManager.SpawnedObjects.TryGetValue(
                            serverNet.NetworkObjectId, out var clientNet))
                        return false;
                    var m = clientNet.GetComponent<NetRunMirror>();
                    if (m == null || m.Wave.Value != 2 || !m.HasEnd) return false;
                    if (m.LastEnd.Kind != RunEndKind.Cleared) return false;
                }
                return true;
            });
            AssertOnTimeout("both clients did not see wave=2 and the same ending");
        }
    }
}
