using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>Host plus two clients. Wave counter and ending match on all three;
    /// AwardKill banks whole into the realm's one pot however many seats are playing.</summary>
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

        /// <summary>RE-KEYED 2026-08-25 (Zo, kills bank as they land). Was
        /// ThreePlayers_KillSplits_WaveAndEndMatch, pinning 25 apportioned 9/8/8. The award
        /// is not divided any more: a guest plays in the host's world and it is that world's
        /// pot that fills, so the whole 25 lands in the one bank — the same TOTAL the split
        /// produced, which is why nobody is poorer for the change. The split rule itself is
        /// still live code (the faucet endure bonus calls PlayerWallets.AwardSplit) and is
        /// pinned directly at
        /// PlayerWalletTests.AwardSplit_ThreePlayers_SplitsEqually_RemainderToHost, so what
        /// belongs at this fixture's altitude is the three-seat claim it alone can make:
        /// three seats connected, one pot, and no seat holding anything it did not
        /// stake.</summary>
        [UnityTest]
        public IEnumerator ThreePlayers_KillBanksWhole_WaveAndEndMatch()
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

            long award = _gc.Sim.CurrentKillAward;
            Assert.AreEqual(25L, award,
                "the income model still pays 25 a body at both dials down — 9+8+8 as was");
            long bankSnap = _gc.Meta.BankedSalvage;

            _gc.Sim.AwardKill();
            Assert.AreEqual(bankSnap + award, _gc.Sim.BankedSalvage,
                "the whole award reached the realm's one pot, undivided by the three seats");
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(0), "and no seat's pile moved: slot 0");
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(1), "slot 1");
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(2), "slot 2");

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

            // The realm's pot is process-static and outlives this fixture.
            _gc.Meta.BankedSalvage = bankSnap;
        }
    }
}
