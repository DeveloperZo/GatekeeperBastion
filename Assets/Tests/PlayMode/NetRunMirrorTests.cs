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
    /// <summary>In-process host + one client. Proves the run readout replicates and all three
    /// end kinds arrive as a ClientRpc.</summary>
    public class NetRunMirrorTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _mirrorPrefab;

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _mirrorPrefab = CreateNetworkObjectPrefab("NetRunMirrorTest");
            _mirrorPrefab.AddComponent<NetRunMirror>();
        }

        [UnityTest]
        public IEnumerator Mirror_ReplicatesReadout_DeliversEndSummary()
        {
            var serverGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("mirror did not spawn on client");

            var serverMirror = serverGo.GetComponent<NetRunMirror>();
            Assert.IsNotNull(serverMirror);
            serverMirror.Phase.Value = (byte)AssaultState.Assault;
            serverMirror.Wave.Value = 2;
            serverMirror.Alive.Value = 5;
            serverMirror.Salvage.Value = 250L;
            serverMirror.HatchHp.Value = 180f;
            serverMirror.HatchMax.Value = 300f;
            serverMirror.ElapsedSeconds.Value = 75;
            serverMirror.Banked.Value = 900L;
            serverMirror.Unbanked.Value = 40L;
            serverMirror.Mode.Value = (byte)WaveMode.HatchBank;

            yield return WaitForConditionOrTimeOut(() =>
            {
                if (!m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                        serverNet.NetworkObjectId, out var clientNet))
                    return false;
                var m = clientNet.GetComponent<NetRunMirror>();
                return m != null
                    && m.Phase.Value == (byte)AssaultState.Assault
                    && m.Wave.Value == 2
                    && m.Alive.Value == 5
                    && m.Salvage.Value == 250L
                    && Mathf.Abs(m.HatchHp.Value - 180f) < 0.01f
                    && Mathf.Abs(m.HatchMax.Value - 300f) < 0.01f
                    && m.ElapsedSeconds.Value == 75
                    && m.Banked.Value == 900L
                    && m.Unbanked.Value == 40L
                    && m.Mode.Value == (byte)WaveMode.HatchBank;
            });
            AssertOnTimeout("client mirror readout did not match the host");

            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientObj));
            var clientMirror = clientObj.GetComponent<NetRunMirror>();
            Assert.AreEqual((byte)AssaultState.Assault, clientMirror.Phase.Value);
            Assert.AreEqual(2, clientMirror.Wave.Value);
            Assert.AreEqual(5, clientMirror.Alive.Value);
            Assert.AreEqual(250L, clientMirror.Salvage.Value);
            Assert.AreEqual(180f, clientMirror.HatchHp.Value, 0.01f);
            Assert.AreEqual(300f, clientMirror.HatchMax.Value, 0.01f);
            Assert.AreEqual(75, clientMirror.ElapsedSeconds.Value);
            Assert.AreEqual(900L, clientMirror.Banked.Value);
            Assert.AreEqual(40L, clientMirror.Unbanked.Value);
            Assert.AreEqual((byte)WaveMode.HatchBank, clientMirror.Mode.Value);

            RunEndKind[] kinds = { RunEndKind.Cleared, RunEndKind.Hatch, RunEndKind.Player };
            for (int i = 0; i < kinds.Length; i++)
            {
                var summary = new EndSummary
                {
                    Kind = kinds[i],
                    GrossEarned = 40L + i,
                    RepairSpent = 3L,
                    Net = 37L + i,
                    DeepestWave = 4,
                    Banked = 200L
                };
                serverMirror.ServerShowEnd(summary);
                RunEndKind expected = kinds[i];
                long expectedGross = 40L + i;
                long expectedNet = 37L + i;
                yield return WaitForConditionOrTimeOut(() =>
                    clientMirror.HasEnd
                    && clientMirror.LastEnd.Kind == expected
                    && clientMirror.LastEnd.GrossEarned == expectedGross
                    && clientMirror.LastEnd.RepairSpent == 3L
                    && clientMirror.LastEnd.Net == expectedNet
                    && clientMirror.LastEnd.DeepestWave == 4
                    && clientMirror.LastEnd.Banked == 200L);
                AssertOnTimeout("client did not receive EndSummary kind=" + (byte)expected);
            }
        }
    }
}
