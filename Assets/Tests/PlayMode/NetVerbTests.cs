using System.Collections;
using System.Text.RegularExpressions;
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
    /// <summary>In-process host + one client. Client verb RPCs apply once on the host sim:
    /// repair spends host banked, decoy spawns on both, pin/concussive hit the host body.</summary>
    public class NetVerbTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _verbsPrefab;
        GameObject _decoyPrefab;
        GameObject _enemyPrefab;
        GameController _gc;
        NetVerbs _clientVerbs;

        [UnityTearDown]
        public IEnumerator DestroyHostRun()
        {
            if (_gc != null)
            {
                Object.Destroy(_gc.gameObject);
                _gc = null;
            }
            _clientVerbs = null;
            if (GameSession.Meta != null)
                GameSession.Meta.PayloadLoadoutLocked = false;
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _verbsPrefab = CreateNetworkObjectPrefab("NetVerbsTest");
            _verbsPrefab.AddComponent<NetVerbs>();

            _decoyPrefab = CreateNetworkObjectPrefab("NetDecoyTest");
            _decoyPrefab.AddComponent<DecoyObject>();
            var decoyNt = _decoyPrefab.AddComponent<NetworkTransform>();
            decoyNt.AuthorityMode = NetworkTransform.AuthorityModes.Server;

            _enemyPrefab = CreateNetworkObjectPrefab("NetVerbEnemy");
            _enemyPrefab.SetActive(false);
            _enemyPrefab.AddComponent<Damageable>().SetMaxHp(30f);
            var agent = _enemyPrefab.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            _enemyPrefab.AddComponent<EnemyGatekeeper>();
            var enemyNt = _enemyPrefab.AddComponent<NetworkTransform>();
            enemyNt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            _enemyPrefab.AddComponent<NetEnemy>();
            _enemyPrefab.SetActive(true);
        }

        [UnityTest]
        public IEnumerator Repair_ClientChannel_SpendsHostWalletAndStops()
        {
            yield return BootHostRun();
            SetBanked(400L);
            var hatch = _gc.Sim.Ship.Hatch;
            hatch.Damage(80f);
            float hpBefore = hatch.Hp;
            long bankBefore = GameSession.Meta.BankedSalvage;
            Assert.Less(hpBefore, hatch.MaxHp - 1f);

            yield return SpawnClientVerbs();
            _clientVerbs.SetRepairing(true);
            yield return WaitForTicks(m_ServerNetworkManager, 30);

            Assert.Greater(hatch.Hp, hpBefore, "host hatch HP rose");
            Assert.Less(GameSession.Meta.BankedSalvage, bankBefore, "host banked fell");

            _clientVerbs.SetRepairing(false);
            // The release is an RPC; the drip keeps healing until it lands on the host.
            // Capture the resting point after it has, then hold — the pre-capture race
            // was this test's recurring flake.
            yield return WaitForTicks(m_ServerNetworkManager, 5);
            float hpAtStop = hatch.Hp;
            long bankAtStop = GameSession.Meta.BankedSalvage;
            yield return WaitForTicks(m_ServerNetworkManager, 15);

            Assert.AreEqual(hpAtStop, hatch.Hp, 0.001f, "HP stops rising after release");
            Assert.AreEqual(bankAtStop, GameSession.Meta.BankedSalvage, "bank stops falling after release");
        }

        [UnityTest]
        public IEnumerator Repair_EmptyBank_HealsNothing()
        {
            yield return BootHostRun();
            SetBanked(0L);
            var hatch = _gc.Sim.Ship.Hatch;
            hatch.Damage(40f);
            float hp = hatch.Hp;

            yield return SpawnClientVerbs();
            _clientVerbs.SetRepairing(true);
            yield return WaitForTicks(m_ServerNetworkManager, 20);

            Assert.AreEqual(hp, hatch.Hp, 0.01f, "empty wallet does not heal");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage);
        }

        [UnityTest]
        public IEnumerator Decoy_ClientPlace_SpawnsOnBoth()
        {
            yield return BootHostRun();
            var bayGo = new GameObject("TestBay");
            var bay = bayGo.AddComponent<DecoyBay>();
            bay.Bind(_gc, _decoyPrefab);
            bay.ResetForRun(PayloadId.Decoy);
            Assert.AreEqual(3, bay.State.ChargesRemaining);

            yield return SpawnClientVerbs();
            _clientVerbs.RequestPlaceDecoy(Vector3.zero);
            yield return WaitForTicks(m_ServerNetworkManager, 8);

            Assert.IsNotNull(bay.Live, "host bay has a live decoy");
            var net = bay.Live.GetComponent<NetworkObject>();
            Assert.IsNotNull(net);
            Assert.IsTrue(net.IsSpawned);
            Assert.AreEqual(2, bay.State.ChargesRemaining);

            yield return WaitForSpawnedOnAllOrTimeOut(net);
            AssertOnTimeout("decoy did not spawn on client");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(net.NetworkObjectId),
                "client has the decoy clone");
        }

        [UnityTest]
        public IEnumerator Pin_ClientRpc_PinsHostNotClone()
        {
            yield return BootHostRun();
            var serverEnemy = SpawnObject(_enemyPrefab, m_ServerNetworkManager);
            var serverNet = serverEnemy.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("enemy did not spawn on client");

            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet));
            var serverGk = serverEnemy.GetComponent<EnemyGatekeeper>();
            var clientGk = clientNet.GetComponent<EnemyGatekeeper>();
            Assert.IsFalse(serverGk.IsPinned);
            Assert.IsFalse(clientGk.IsPinned);

            yield return SpawnClientVerbs();
            float seconds = WeaponAbility.Magnitude(AbilityId.Mr35Pin, 0);
            _clientVerbs.RequestPin(serverNet.NetworkObjectId, seconds);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.IsTrue(serverGk.IsPinned, "host body is pinned");
            Assert.IsFalse(clientGk.IsPinned, "client clone was not pinned");
        }

        [UnityTest]
        public IEnumerator Concussive_ClientRpc_HitsHostLedger()
        {
            yield return BootHostRun();
            var serverEnemy = SpawnObject(_enemyPrefab, m_ServerNetworkManager);
            serverEnemy.transform.position = new Vector3(0f, 0f, 4f);
            var serverNet = serverEnemy.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("enemy did not spawn on client");

            LogAssert.Expect(LogType.Log, new Regex("concussive shove"));

            yield return SpawnClientVerbs();
            _clientVerbs.RequestConcussive(Vector3.zero, Vector3.forward, 6f, 14f);
            yield return WaitForTicks(m_ServerNetworkManager, 4);
        }

        IEnumerator BootHostRun()
        {
            var go = new GameObject("HostGC");
            _gc = go.AddComponent<GameController>();
            yield return null;
            yield return null;
            Assert.IsNotNull(_gc.Sim, "GameController started a RunSim");
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase);
        }

        IEnumerator SpawnClientVerbs()
        {
            var serverGo = SpawnObject(_verbsPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            serverGo.GetComponent<NetVerbs>().Bind(_gc);
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("verbs object did not spawn on client");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has verbs clone");
            _clientVerbs = clientNet.GetComponent<NetVerbs>();
            Assert.IsNotNull(_clientVerbs);
        }

        static void SetBanked(long amount)
        {
            var meta = GameSession.Meta;
            Assert.IsNotNull(meta);
            if (meta.BankedSalvage < amount)
                meta.EarnSalvage(amount - meta.BankedSalvage);
            else if (meta.BankedSalvage > amount)
                Assert.IsTrue(meta.SpendSalvage(meta.BankedSalvage - amount));
            Assert.AreEqual(amount, meta.BankedSalvage);
        }
    }
}
