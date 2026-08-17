using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>In-process host + one client. Protocol verbs apply once on the host:
    /// Anchor slows host bodies, Bulkhead negates host damage, Ghost bias is a set
    /// from equipped ids (0.70, never multiplied twice).</summary>
    public class NetProtocolTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _verbsPrefab;
        GameObject _enemyPrefab;
        GameObject _avatarPrefab;
        GameController _gc;
        NetVerbs _clientVerbs;
        PlayerHealth _serverHealth;

        [UnityTearDown]
        public IEnumerator DestroyHostRun()
        {
            if (_gc != null)
            {
                Object.Destroy(_gc.gameObject);
                _gc = null;
            }
            _clientVerbs = null;
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _verbsPrefab = CreateNetworkObjectPrefab("NetProtocolVerbs");
            var verbHealth = _verbsPrefab.AddComponent<PlayerHealth>();
            verbHealth.ContributeToRegistry = false;
            verbHealth.EndsTheRunOnDeath = false;
            _verbsPrefab.AddComponent<NetVerbs>();

            _enemyPrefab = CreateNetworkObjectPrefab("NetProtocolEnemy");
            _enemyPrefab.SetActive(false);
            _enemyPrefab.AddComponent<Damageable>().SetMaxHp(30f);
            var agent = _enemyPrefab.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            _enemyPrefab.AddComponent<EnemyGatekeeper>();
            var enemyNt = _enemyPrefab.AddComponent<NetworkTransform>();
            enemyNt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            _enemyPrefab.AddComponent<NetEnemy>();
            _enemyPrefab.SetActive(true);

            _avatarPrefab = CreateNetworkObjectPrefab("NetProtocolAvatar");
            _avatarPrefab.SetActive(false);
            _avatarPrefab.AddComponent<CapsuleCollider>();
            var health = _avatarPrefab.AddComponent<PlayerHealth>();
            health.ContributeToRegistry = false;
            health.EndsTheRunOnDeath = false;
            var nt = _avatarPrefab.AddComponent<NetworkTransform>();
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            _avatarPrefab.AddComponent<NetPlayerAvatar>();
            _avatarPrefab.SetActive(true);
        }

        [UnityTest]
        public IEnumerator Anchor_ClientRpc_SlowsHostOnce()
        {
            yield return BootHostRun();
            var serverEnemy = SpawnObject(_enemyPrefab, m_ServerNetworkManager);
            serverEnemy.transform.position = Vector3.zero;
            var serverNet = serverEnemy.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("enemy did not spawn on client");

            yield return SpawnClientVerbs();
            Assert.AreEqual(1f, AnchorField.SlowMulAt(Vector3.zero), 0.01f);

            _clientVerbs.RequestAnchor(Vector3.zero, true);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.AreEqual(BoarderProtocol.Magnitude(BoarderAbilityId.Anchor),
                AnchorField.SlowMulAt(Vector3.zero), 0.01f, "host field applies once");

            _clientVerbs.RequestAnchor(Vector3.zero, false);
            yield return WaitForTicks(m_ServerNetworkManager, 4);
            Assert.AreEqual(1f, AnchorField.SlowMulAt(Vector3.zero), 0.01f);
        }

        [UnityTest]
        public IEnumerator Bulkhead_ClientRpc_NegatesHostHitOnce()
        {
            yield return BootHostRun();
            var serverEnemy = SpawnObject(_enemyPrefab, m_ServerNetworkManager);
            var serverNet = serverEnemy.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("enemy did not spawn on client");

            yield return SpawnClientVerbs();
            Assert.IsNotNull(_serverHealth);
            float before = _serverHealth.Hp;

            _clientVerbs.RequestBulkhead();
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            _serverHealth.TakeDamage(10f, serverEnemy.transform);
            Assert.AreEqual(before, _serverHealth.Hp, 0.01f, "first Gatekeeper hit negated");

            _serverHealth.TakeDamage(10f, serverEnemy.transform);
            Assert.AreEqual(before - 10f, _serverHealth.Hp, 0.01f, "second hit lands");
        }

        [UnityTest]
        public IEnumerator Ghost_SpawnIds_SetAcquisitionOnce()
        {
            yield return BootHostRun();
            GameSession.EnsureLoaded();
            Assert.IsNotNull(GameSession.Meta);
            GameSession.Meta.EquippedProtocol = BoarderProtocolId.Default;
            GameSession.Meta.EquippedPassive = BoarderAbilityId.Ghost;

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("avatar did not spawn on client");

            serverNet.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);
            float expected = BoarderProtocol.Magnitude(BoarderAbilityId.Ghost);
            yield return WaitForConditionOrTimeOut(() =>
            {
                var health = serverGo.GetComponent<PlayerHealth>();
                var avatar = serverGo.GetComponent<NetPlayerAvatar>();
                return health != null && avatar != null
                    && Mathf.Abs(health.AcquisitionMul - expected) < 0.01f
                    && avatar.PassiveId.Value == (int)BoarderAbilityId.Ghost;
            });
            AssertOnTimeout("server did not set Ghost from equipped ids");

            var serverHealth = serverGo.GetComponent<PlayerHealth>();
            Assert.AreEqual(expected, serverHealth.AcquisitionMul, 0.01f);
            Assert.AreNotEqual(expected * expected, serverHealth.AcquisitionMul);
        }

        IEnumerator BootHostRun()
        {
            var go = new GameObject("HostGC");
            _gc = go.AddComponent<GameController>();
            yield return null;
            yield return null;
            Assert.IsNotNull(_gc.Sim, "GameController started a RunSim");
        }

        IEnumerator SpawnClientVerbs()
        {
            var serverGo = SpawnObject(_verbsPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            serverGo.GetComponent<NetVerbs>().Bind(_gc);
            _serverHealth = serverGo.GetComponent<PlayerHealth>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("verbs object did not spawn on client");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the verbs clone");
            _clientVerbs = clientNet.GetComponent<NetVerbs>();
            Assert.IsNotNull(_clientVerbs);
        }
    }
}
