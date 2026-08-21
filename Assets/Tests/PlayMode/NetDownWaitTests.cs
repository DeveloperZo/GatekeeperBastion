using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>In-process host + one client. Client lethal HP waits on the Ship flag;
    /// Hatch-bank Prep embark restores HP; Assault and Terminal-Prep refuse; host death
    /// still ends the run. Does not load the Ship scene.</summary>
    public class NetDownWaitTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _avatarPrefab;
        GameObject _dummy;
        GameController _gc;

        [UnityTearDown]
        public IEnumerator DestroyHosts()
        {
            if (_dummy != null)
            {
                Object.Destroy(_dummy);
                _dummy = null;
            }
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

            _avatarPrefab = CreateNetworkObjectPrefab("NetDownWaitAvatar");
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
        public IEnumerator DownWait_HatchBankPrepEmbarks_RefusesOtherwise()
        {
            _dummy = new GameObject("HostStandIn");
            _dummy.transform.position = new Vector3(80f, 0f, 0f);
            _dummy.AddComponent<PlayerHealth>();
            var dummyHealth = _dummy.GetComponent<PlayerHealth>();

            yield return BootHostRun();

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
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
            serverAvatar.Bind(_gc);
            var serverHealth = serverAvatar.Body;

            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the avatar clone");
            var clientAvatar = clientNet.GetComponent<NetPlayerAvatar>();

            serverHealth.TakeDamage(serverHealth.Hp + 1f);
            yield return WaitForConditionOrTimeOut(() =>
                serverAvatar.WaitingOnShip.Value && serverHealth.IsDead);
            AssertOnTimeout("lethal damage did not set WaitingOnShip");
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value);
            Assert.AreSame(dummyHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "dead waiter is skipped by NearestAlive");
            yield return WaitForConditionOrTimeOut(() => clientAvatar.WaitingOnShip.Value);
            AssertOnTimeout("WaitingOnShip did not replicate to the owner");

            clientAvatar.RequestEmbark();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value,
                "Terminal Prep embark is refused");
            Assert.IsTrue(serverHealth.IsDead);

            _gc.Sim.TriggerDraw();
            _gc.Sim.AwardKill();
            _gc.Sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, _gc.Sim.Mode);

            _gc.Sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);
            Assert.AreEqual(WaveMode.HatchBank, _gc.Sim.Mode);
            clientAvatar.RequestEmbark();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value,
                "Assault-time embark is refused");

            _gc.Sim.Tick(HatchBank.DurationSeconds);
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
            Assert.AreEqual(WaveMode.HatchBank, _gc.Sim.Mode);

            clientAvatar.RequestEmbark();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("Hatch-bank Prep embark did not restore HP");
            Assert.IsFalse(serverAvatar.WaitingOnShip.Value);
            Assert.IsFalse(serverHealth.IsDead);
            Assert.AreEqual(serverHealth.MaxHp, serverHealth.Hp, 0.01f);
            yield return WaitForConditionOrTimeOut(() =>
                !clientAvatar.WaitingOnShip.Value
                && clientAvatar.Hp.Value >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("embark HP did not replicate to the owner");
            Assert.AreSame(serverHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "revived avatar is huntable again");
        }

        [UnityTest]
        public IEnumerator BuyBack_HatchBankAssaultRejoins_RefusesOtherwise()
        {
            _dummy = new GameObject("HostStandIn");
            _dummy.transform.position = new Vector3(80f, 0f, 0f);
            _dummy.AddComponent<PlayerHealth>();

            yield return BootHostRun();
            Assert.IsNotNull(GameSession.Meta, "buy-back needs a Meta purse");
            long bankSnap = GameSession.Meta.BankedSalvage;
            GameSession.Meta.BankedSalvage = 0L;

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
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
            serverAvatar.Bind(_gc);
            var serverHealth = serverAvatar.Body;
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the avatar clone");
            var clientAvatar = clientNet.GetComponent<NetPlayerAvatar>();

            serverHealth.TakeDamage(serverHealth.Hp + 1f);
            yield return WaitForConditionOrTimeOut(() =>
                serverAvatar.WaitingOnShip.Value && serverHealth.IsDead);
            AssertOnTimeout("lethal damage did not set WaitingOnShip");

            LogAssert.Expect(LogType.Log, "[Demo] buyback refused — no live window");
            clientAvatar.RequestBuyBack();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value, "Terminal buy-back is refused");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "refuse must not debit");

            _gc.Sim.SeedBankPile(100L, 1);
            _gc.Sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);
            Assert.AreEqual(WaveMode.HatchBank, _gc.Sim.Mode);
            long guestUnbanked = _gc.Sim.UnbankedOf(serverAvatar.Slot.Value);

            LogAssert.Expect(LogType.Log, "[Demo] buyback refused — cannot afford");
            clientAvatar.RequestBuyBack();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value, "unaffordable buy-back is refused");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage);

            // Fund the REALM's pot, which is what pays: a guest is playing in somebody else's
            // world, and the cost of putting it back on its feet comes out of that world
            // (Docs/Milestones — the realm and the Boarder). The gate is not poked directly
            // here any more, because checking and debiting are now one call and a bare probe
            // would spend the pot the request underneath is about to need.
            GameSession.Meta.BankedSalvage = HatchBank.BuyBackCost;

            LogAssert.Expect(LogType.Log,
                "[Demo] buyback seat=" + serverAvatar.Slot.Value + " cost=" + HatchBank.BuyBackCost);
            clientAvatar.RequestBuyBack();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("Hatch-bank Assault buy-back did not restore HP");
            Assert.AreEqual(guestUnbanked, _gc.Sim.UnbankedOf(serverAvatar.Slot.Value),
                "buy-back must not touch Unbanked");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage,
                "the realm's pot paid for the rejoin");

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator BuyBack_HostAttempt_RefusedNamed()
        {
            yield return BootHostRun();
            long bankSnap = GameSession.Meta != null ? GameSession.Meta.BankedSalvage : 0L;
            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("host avatar did not spawn");

            var serverAvatar = serverGo.GetComponent<NetPlayerAvatar>();
            serverAvatar.Bind(_gc);
            _gc.Sim.TriggerDraw(WaveMode.HatchBank);
            if (GameSession.Meta != null)
                GameSession.Meta.BankedSalvage = HatchBank.BuyBackCost;

            LogAssert.Expect(LogType.Log, "[Demo] buyback refused — host cannot buy back");
            serverAvatar.RequestBuyBack();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsFalse(serverAvatar.WaitingOnShip.Value);
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase);
            if (GameSession.Meta != null)
                GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator DownWait_HostDie_EndsRunWithoutWait()
        {
            yield return BootHostRun();

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("host avatar did not spawn");

            var serverAvatar = serverGo.GetComponent<NetPlayerAvatar>();
            var serverHealth = serverAvatar.Body;
            serverHealth.EndsTheRunOnDeath = true;
            serverHealth.Bind(_gc);
            serverAvatar.Bind(_gc);
            Assert.IsTrue(serverAvatar.IsOwner, "server-owned body is the host");
            Assert.IsTrue(serverHealth.EndsTheRunOnDeath);

            serverHealth.TakeDamage(serverHealth.Hp + 1f);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.IsTrue(serverHealth.IsDead);
            Assert.IsFalse(serverAvatar.WaitingOnShip.Value,
                "host-owned body does not wait on the Ship");
            Assert.AreEqual(RunPhase.Lost, _gc.Sim.Phase);
        }

        IEnumerator BootHostRun()
        {
            var go = new GameObject("HostGC");
            _gc = go.AddComponent<GameController>();
            yield return null;
            yield return null;
            Assert.IsNotNull(_gc.Sim, "GameController started a RunSim");
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase);
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
        }
    }
}
