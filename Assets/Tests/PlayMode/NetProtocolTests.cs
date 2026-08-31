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
    /// <summary>In-process host + one client. Barrage plants once on the host list;
    /// overlapping plants SET 2.0; Rush is a personal window (no Bulkhead absorb);
    /// Default+None acquisition stays 1.0.</summary>
    public class NetProtocolTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _verbsPrefab;
        GameObject _enemyPrefab;
        GameObject _avatarPrefab;
        GameController _gc;
        NetVerbs _clientVerbs;
        NetVerbs _clientVerbs2;
        PlayerHealth _serverHealth;
        PlayerHealth _serverHealth2;

        [UnityTearDown]
        public IEnumerator DestroyHostRun()
        {
            if (_gc != null)
            {
                Object.Destroy(_gc.gameObject);
                _gc = null;
            }
            _clientVerbs = null;
            _clientVerbs2 = null;
            _serverHealth = null;
            _serverHealth2 = null;
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
        public IEnumerator Barrage_ClientRpc_SetsHostFireMulOnce()
        {
            yield return BootHostRun();
            yield return SpawnClientVerbs();
            Assert.AreEqual(1f, BarrageField.FireMulAt(Vector3.zero), 0.01f);

            _clientVerbs.RequestAnchor(Vector3.zero, true);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.AreEqual(BoarderProtocol.BarrageFireMul,
                BarrageField.FireMulAt(Vector3.zero), 0.01f, "host field applies once");
            Assert.AreEqual(2f, BarrageField.FireMulAt(Vector3.zero), 0.01f);

            _clientVerbs.RequestAnchor(Vector3.zero, false);
            yield return WaitForTicks(m_ServerNetworkManager, 4);
            Assert.AreEqual(1f, BarrageField.FireMulAt(Vector3.zero), 0.01f);
        }

        [UnityTest]
        public IEnumerator Barrage_OverlappingPlants_StayTwo()
        {
            yield return BootHostRun();
            yield return SpawnClientVerbs();
            yield return SpawnSecondClientVerbs();

            _clientVerbs.RequestAnchor(Vector3.zero, true);
            _clientVerbs2.RequestAnchor(Vector3.zero, true);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.AreEqual(BoarderProtocol.BarrageFireMul,
                BarrageField.FireMulAt(Vector3.zero), 0.01f, "SET max 2, never 2x2");
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(true, true, 2f), 1e-5f,
                "Feed no longer multiplies — Dump zone alone is 2");
        }

        [UnityTest]
        public IEnumerator Rush_RequestBulkhead_DoesNotAbsorb()
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
            Assert.AreEqual(before - 10f, _serverHealth.Hp, 0.01f, "Rush is not Bulkhead");
        }

        [UnityTest]
        public IEnumerator DefaultNone_SpawnIds_AcquisitionStaysOne()
        {
            yield return BootHostRun();
            GameSession.EnsureLoaded();
            Assert.IsNotNull(GameSession.Meta);
            GameSession.Meta.EquippedProtocol = BoarderProtocolId.Depot;
            GameSession.Meta.EquippedPassive = BoarderAbilityId.None;

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("avatar did not spawn on client");

            serverNet.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);
            yield return WaitForConditionOrTimeOut(() =>
            {
                var health = serverGo.GetComponent<PlayerHealth>();
                var avatar = serverGo.GetComponent<NetPlayerAvatar>();
                return health != null && avatar != null
                    && Mathf.Abs(health.AcquisitionMul - 1f) < 0.01f
                    && avatar.ProtocolId.Value == (int)BoarderProtocolId.Depot
                    && avatar.PassiveId.Value == (int)BoarderAbilityId.None;
            });
            AssertOnTimeout("server did not accept Default+None");

            var serverHealth = serverGo.GetComponent<PlayerHealth>();
            Assert.AreEqual(1f, serverHealth.AcquisitionMul, 0.01f);
        }

        /// <summary>Criterion 8: two seats, two protocols, and neither one's ability reaching
        /// the other. Both halves are asserted on the SERVER, because that is the only place
        /// they mean anything — gatekeepers are host-only, so a cloak or a turret that exists
        /// solely on the guest's own machine is a no-op dressed as an ability.</summary>
        [UnityTest]
        public IEnumerator TwoSeats_DifferentProtocols_KeepTheirOwnAbilities()
        {
            yield return BootHostRun();
            yield return SpawnClientVerbs();
            yield return SpawnSecondClientVerbs();
            Assert.IsNotNull(_serverHealth);
            Assert.IsNotNull(_serverHealth2);
            Assert.IsFalse(_serverHealth.Cloaked, "nobody starts cloaked");
            Assert.IsFalse(_serverHealth2.Cloaked);

            // Seat 1 runs VEIL and cloaks.
            _clientVerbs.RequestCloak(true);
            yield return WaitForTicks(m_ServerNetworkManager, 4);
            Assert.IsTrue(_serverHealth.Cloaked,
                "a guest's cloak has to reach the server or no gatekeeper can honour it");
            Assert.IsFalse(_serverHealth2.Cloaked, "seat 2 was not cloaked by its neighbour");

            // Seat 2 runs FORGE and stands a turret.
            _clientVerbs2.RequestTurret(new Vector3(5f, 0f, 5f), true);
            yield return WaitForTicks(m_ServerNetworkManager, 4);
            Assert.IsTrue(_clientVerbs2.ServerTurretStanding, "seat 2's machine is standing");
            Assert.IsFalse(_clientVerbs.ServerTurretStanding,
                "seat 1 did not gain a turret it never deployed");

            // And releasing one seat's ability leaves the other's alone.
            _clientVerbs.RequestCloak(false);
            yield return WaitForTicks(m_ServerNetworkManager, 4);
            Assert.IsFalse(_serverHealth.Cloaked, "seat 1 dropped its own cloak");
            Assert.IsTrue(_clientVerbs2.ServerTurretStanding, "seat 2 still has its machine");
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

        IEnumerator SpawnSecondClientVerbs()
        {
            var serverGo = SpawnObject(_verbsPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            serverGo.GetComponent<NetVerbs>().Bind(_gc);
            _serverHealth2 = serverGo.GetComponent<PlayerHealth>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("second verbs object did not spawn on client");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the second verbs clone");
            _clientVerbs2 = clientNet.GetComponent<NetVerbs>();
            Assert.IsNotNull(_clientVerbs2);
        }
    }
}
