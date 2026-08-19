using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>In-process host + one client. Proves the kill loop without MPPM: a server-spawned
    /// gatekeeper clone arrives on the client with AI and NavMeshAgent off; SubmitHitServerRpc
    /// writes host Damageable only; lethal damage despawns the clone; mirror Wave/Alive match.</summary>
    public class NetKillTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _enemyPrefab;
        GameObject _mirrorPrefab;

        protected override void OnServerAndClientsCreated()
        {
            // NGO defaults this on; the fixture scene is not in Build Settings, so a client
            // sync would throw "Scene '' couldn't be loaded".
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _enemyPrefab = CreateNetworkObjectPrefab("NetKillEnemy");
            _enemyPrefab.SetActive(false);
            _enemyPrefab.AddComponent<Damageable>().SetMaxHp(30f);
            var agent = _enemyPrefab.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            _enemyPrefab.AddComponent<EnemyGatekeeper>();
            var enemyNt = _enemyPrefab.AddComponent<NetworkTransform>();
            enemyNt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            _enemyPrefab.AddComponent<NetEnemy>();
            _enemyPrefab.SetActive(true);

            _mirrorPrefab = CreateNetworkObjectPrefab("NetKillMirror");
            _mirrorPrefab.AddComponent<NetRunMirror>();
        }

        [UnityTest]
        public IEnumerator KillLoop_AppliesOnceOnHost_DespawnsAndMirrors()
        {
            var serverEnemy = SpawnObject(_enemyPrefab, m_ServerNetworkManager);
            var serverNet = serverEnemy.GetComponent<NetworkObject>();
            Assert.IsNotNull(serverNet);
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("enemy did not spawn on client");

            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the enemy clone");
            var clientEnemy = clientNet.gameObject;

            var clientAi = clientEnemy.GetComponent<EnemyGatekeeper>();
            var clientAgent = clientEnemy.GetComponent<NavMeshAgent>();
            Assert.IsNotNull(clientAi);
            Assert.IsNotNull(clientAgent);
            Assert.IsFalse(clientAi.enabled, "client clone AI is disabled");
            Assert.IsFalse(clientAgent.enabled, "client clone NavMeshAgent is disabled");

            var hostBody = serverEnemy.GetComponent<Damageable>();
            var clientBody = clientEnemy.GetComponent<Damageable>();
            Assert.IsNotNull(hostBody);
            Assert.IsNotNull(clientBody);
            float hostHp = hostBody.Hp;
            float clientHp = clientBody.Hp;
            const float Hit = 5f;

            var clientNetEnemy = clientEnemy.GetComponent<NetEnemy>();
            clientNetEnemy.SubmitHitServerRpc(Hit, (byte)WeaponSlot.Primary);
            yield return WaitForConditionOrTimeOut(() =>
                Mathf.Abs(hostBody.Hp - (hostHp - Hit)) < 0.01f
                && Mathf.Abs(clientNetEnemy.Hp.Value - (hostHp - Hit)) < 0.05f);
            AssertOnTimeout("host HP or replicated clone Hp did not drop");

            Assert.AreEqual(hostHp - Hit, hostBody.Hp, 0.01f, "host Damageable dropped by the sent amount");
            Assert.AreEqual(clientHp, clientBody.Hp, 0.01f, "client clone Damageable was not written");
            Assert.IsFalse(clientBody.IsDead, "client clone IsDead stays false after a non-lethal hit");
            Assert.AreEqual(hostBody.Hp, clientNetEnemy.Hp.Value, 0.05f,
                "replicated Hp matches host after hit");

            float remain = hostBody.Hp;
            clientNetEnemy.SubmitHitServerRpc(remain + 1f, (byte)WeaponSlot.Primary);
            ulong deadId = serverNet.NetworkObjectId;
            yield return WaitForTicks(m_ServerNetworkManager, 2);
            if (m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(deadId))
            {
                Assert.IsFalse(clientBody.IsDead, "client clone IsDead stays false until despawn");
                Assert.AreEqual(clientHp, clientBody.Hp, 0.01f, "lethal RPC must not TakeDamage the clone");
            }
            yield return WaitForConditionOrTimeOut(() =>
                !m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(deadId));
            AssertOnTimeout("lethal hit did not despawn the clone on the client");
            Assert.IsFalse(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(deadId),
                "client clone is gone after lethal damage");

            var serverMirrorGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverMirrorNet = serverMirrorGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverMirrorNet);
            AssertOnTimeout("mirror did not spawn on client");

            var serverMirror = serverMirrorGo.GetComponent<NetRunMirror>();
            Assert.IsNotNull(serverMirror);
            serverMirror.Wave.Value = 3;
            serverMirror.Alive.Value = 7;

            yield return WaitForConditionOrTimeOut(() =>
            {
                if (!m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                        serverMirrorNet.NetworkObjectId, out var clientMirrorNet))
                    return false;
                var clientMirror = clientMirrorNet.GetComponent<NetRunMirror>();
                return clientMirror != null
                    && clientMirror.Wave.Value == 3
                    && clientMirror.Alive.Value == 7;
            });
            AssertOnTimeout("client mirror Wave/Alive did not match the host");

            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverMirrorNet.NetworkObjectId, out var clientMirrorObj));
            var clientMirrorFinal = clientMirrorObj.GetComponent<NetRunMirror>();
            Assert.AreEqual(serverMirror.Wave.Value, clientMirrorFinal.Wave.Value);
            Assert.AreEqual(serverMirror.Alive.Value, clientMirrorFinal.Alive.Value);
        }
    }
}
