using System.Collections;
using GatekeeperBastion.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>In-process host + one client. Proves the remote avatar is huntable: the server
    /// registers it, NearestAlive prefers it when nearer, TakeDamage replicates to the owner,
    /// and death drops it from the targeting set.</summary>
    public class NetTargetingTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _avatarPrefab;
        GameObject _dummy;

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _avatarPrefab = CreateNetworkObjectPrefab("NetTargetAvatar");
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

        [UnityTearDown]
        public IEnumerator DestroyDummy()
        {
            if (_dummy != null)
            {
                Object.Destroy(_dummy);
                _dummy = null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Targeting_RegistersAvatar_MirrorsHp_DeathDropsTargeting()
        {
            _dummy = new GameObject("HostStandIn");
            _dummy.transform.position = new Vector3(80f, 0f, 0f);
            _dummy.AddComponent<PlayerHealth>();
            var dummyHealth = _dummy.GetComponent<PlayerHealth>();

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            Assert.IsNotNull(serverNet);
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("avatar did not spawn on client");

            serverGo.transform.position = Vector3.zero;
            serverNet.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);
            yield return WaitForConditionOrTimeOut(() =>
            {
                var avatar = serverGo.GetComponent<NetPlayerAvatar>();
                return avatar != null && !avatar.IsOwner
                    && PlayerRegistry.NearestAlive(Vector3.zero) == avatar.Body;
            });
            AssertOnTimeout("server did not register the remote avatar");

            var serverAvatar = serverGo.GetComponent<NetPlayerAvatar>();
            var serverHealth = serverAvatar.Body;
            Assert.IsNotNull(serverHealth);
            Assert.AreSame(serverHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "NearestAlive at the avatar prefers the avatar over a far dummy");
            Assert.AreSame(dummyHealth, PlayerRegistry.NearestAlive(new Vector3(80f, 0f, 0f)),
                "NearestAlive at the dummy still finds the dummy");

            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the avatar clone");
            var clientAvatar = clientNet.GetComponent<NetPlayerAvatar>();
            var clientHealth = clientNet.GetComponent<PlayerHealth>();
            Assert.IsFalse(RegistryHas(clientHealth),
                "client clone PlayerHealth is not in the targeting set");

            var collider = serverGo.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(collider, "avatar has a capsule collider");
            Assert.IsFalse(collider.isTrigger, "sight raycast ignores triggers");
            Assert.IsTrue(collider.enabled, "remote body collider is on");

            float before = serverHealth.Hp;
            const float Hit = 10f;
            serverHealth.TakeDamage(Hit);
            yield return WaitForConditionOrTimeOut(() =>
                Mathf.Abs(clientAvatar.Hp.Value - (before - Hit)) < 0.01f);
            AssertOnTimeout("owner replicated HP did not drop by the server TakeDamage");
            Assert.AreEqual(before - Hit, clientAvatar.Hp.Value, 0.01f);

            serverHealth.TakeDamage(serverHealth.Hp + 1f);
            yield return WaitForConditionOrTimeOut(() => serverHealth.IsDead);
            AssertOnTimeout("lethal TakeDamage did not kill the avatar");
            Assert.AreSame(dummyHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "dead avatar is skipped by NearestAlive");
            yield return WaitForConditionOrTimeOut(() => clientAvatar.Hp.Value <= 0f);
            AssertOnTimeout("owner replicated HP did not reach 0 on death");

            ulong id = serverNet.NetworkObjectId;
            serverNet.Despawn(true);
            yield return WaitForConditionOrTimeOut(() =>
                !m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(id)
                && PlayerRegistry.NearestAlive(Vector3.zero) == dummyHealth);
            AssertOnTimeout("despawn did not unregister the avatar");
            Assert.IsFalse(RegistryHas(serverHealth), "despawned avatar is not in the registry");
        }

        static bool RegistryHas(PlayerHealth health)
        {
            var all = PlayerRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == health) return true;
            }
            return false;
        }
    }
}
