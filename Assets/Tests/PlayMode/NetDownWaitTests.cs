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
    /// <summary>In-process host + one client. Client lethal HP waits on the Ship flag; a
    /// paid respawn returns it at any Prep, and mid-Assault while the pool has charges —
    /// each respawn escalating in cost out of the realm's pot. Host death holds the run
    /// open only while a respawn can be honored. Does not load the Ship scene.</summary>
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
        public IEnumerator DownWait_PaidRespawnAtAnyPrep_EscalatesCost()
        {
            _dummy = new GameObject("HostStandIn");
            _dummy.transform.position = new Vector3(80f, 0f, 0f);
            _dummy.AddComponent<PlayerHealth>();
            var dummyHealth = _dummy.GetComponent<PlayerHealth>();

            yield return BootHostRun();
            Assert.IsNotNull(GameSession.Meta, "respawn needs a Meta purse");
            long bankSnap = GameSession.Meta.BankedSalvage;
            GameSession.Meta.BankedSalvage = 0L;

            yield return SpawnGuestAvatar();
            var serverAvatar = _guestServer;
            var clientAvatar = _guestClient;
            var serverHealth = serverAvatar.Body;

            yield return DownGuest(serverAvatar, clientAvatar);
            Assert.AreSame(dummyHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "dead waiter is skipped by NearestAlive");

            // Terminal Prep, pot empty: the window is open but nobody can pay.
            LogAssert.Expect(LogType.Log, "[RunSim] respawn refused — cannot afford");
            clientAvatar.RequestRespawn();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value, "unaffordable respawn is refused");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "refuse must not debit");

            // Fund exactly the first respawn: any Prep is a window now, Hatch-bank or not.
            GameSession.Meta.BankedSalvage = Respawns.CostFor(0);
            int stockBefore = _gc.Sim.RespawnStock;
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + Respawns.CostFor(0));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("Terminal Prep respawn did not restore HP");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "the pot paid the cost");
            Assert.AreEqual(stockBefore, _gc.Sim.RespawnStock,
                "a break respawn never burns a pool charge");
            Assert.AreEqual(1, _gc.Sim.RespawnsTaken);
            Assert.AreSame(serverHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "revived avatar is huntable again");

            // The second respawn costs double — the counter is the run's.
            yield return DownGuest(serverAvatar, clientAvatar);
            GameSession.Meta.BankedSalvage = Respawns.CostFor(1);
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + Respawns.CostFor(1));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("second respawn did not land");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "the escalated cost was paid");
            Assert.AreEqual(2, _gc.Sim.RespawnsTaken);

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator DownWait_MidAssaultDrawsOnPool_RefusesWhenDry()
        {
            _dummy = new GameObject("HostStandIn");
            _dummy.transform.position = new Vector3(80f, 0f, 0f);
            _dummy.AddComponent<PlayerHealth>();

            yield return BootHostRun();
            long bankSnap = GameSession.Meta.BankedSalvage;

            yield return SpawnGuestAvatar();
            var serverAvatar = _guestServer;
            var clientAvatar = _guestClient;
            var serverHealth = serverAvatar.Body;

            _gc.Sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);

            // Burn the pool down mid-wave, one funded respawn per charge.
            int guard = 0;
            while (_gc.Sim.RespawnStock > 0 && guard++ < 8)
            {
                yield return DownGuest(serverAvatar, clientAvatar);
                int stock = _gc.Sim.RespawnStock;
                GameSession.Meta.BankedSalvage = _gc.Sim.NextRespawnCost;
                clientAvatar.RequestRespawn();
                yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
                AssertOnTimeout("mid-Assault respawn did not land with stock=" + stock);
                Assert.AreEqual(stock - 1, _gc.Sim.RespawnStock,
                    "a mid-wave respawn burns exactly one charge");
            }
            Assert.AreEqual(0, _gc.Sim.RespawnStock, "the pool drained");

            // Dry pool, funded pot: mid-wave is refused named; the break is not.
            yield return DownGuest(serverAvatar, clientAvatar);
            GameSession.Meta.BankedSalvage = _gc.Sim.NextRespawnCost;
            long potBefore = GameSession.Meta.BankedSalvage;
            LogAssert.Expect(LogType.Log, "[RunSim] respawn refused — no charges until the break");
            clientAvatar.RequestRespawn();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value, "dry-pool mid-wave respawn is refused");
            Assert.AreEqual(potBefore, GameSession.Meta.BankedSalvage, "refuse must not debit");

            _gc.Sim.AwardKill();
            _gc.Sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value && !serverHealth.IsDead);
            AssertOnTimeout("the break respawn did not land after the dry-pool refusal");
            Assert.AreEqual(0, _gc.Sim.RespawnStock, "the break tier spent no charge it did not have");

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator Respawn_PaidByRealmPot_NeverTheFieldPile()
        {
            _dummy = new GameObject("HostStandIn");
            _dummy.transform.position = new Vector3(80f, 0f, 0f);
            _dummy.AddComponent<PlayerHealth>();

            yield return BootHostRun();
            long bankSnap = GameSession.Meta.BankedSalvage;
            GameSession.Meta.BankedSalvage = 0L;

            yield return SpawnGuestAvatar();
            var serverAvatar = _guestServer;
            var clientAvatar = _guestClient;
            var serverHealth = serverAvatar.Body;

            yield return DownGuest(serverAvatar, clientAvatar);

            // A Hatch-bank Assault with a live drip: every pile number is moving, which is
            // exactly why the claim has to be bracketed — the respawn must be paid by the
            // pot, and the field pile may only ever fall by conversion.
            _gc.Sim.SeedBankPile(100L, 1);
            _gc.Sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);
            Assert.Greater(_gc.Sim.RespawnStock, 0, "the seeded pool covers one mid-wave return");

            long cost = _gc.Sim.NextRespawnCost;
            GameSession.Meta.BankedSalvage = cost;
            long unbankedBeforeRejoin = _gc.Sim.UnbankedOf(serverAvatar.Slot.Value);
            long potBeforeRejoin = GameSession.Meta.BankedSalvage;

            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + cost);
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("Hatch-bank Assault respawn did not restore HP");

            long tookFromThePile = unbankedBeforeRejoin - _gc.Sim.UnbankedOf(serverAvatar.Slot.Value);
            Assert.Less(tookFromThePile, cost,
                "the respawn is paid by the realm, never out of the field pile");
            long dripAdded = GameSession.Meta.BankedSalvage - (potBeforeRejoin - cost);
            Assert.GreaterOrEqual(dripAdded, 0L,
                "the realm's pot cannot have paid more than the respawn cost");
            Assert.Less(dripAdded, cost,
                "the realm's pot paid for the rejoin — what is left of it is conversion");

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator Respawn_HostRpcAttempt_RefusedNamed()
        {
            yield return BootHostRun();
            long bankSnap = GameSession.Meta != null ? GameSession.Meta.BankedSalvage : 0L;
            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("host avatar did not spawn");

            var serverAvatar = serverGo.GetComponent<NetPlayerAvatar>();
            serverAvatar.Bind(_gc);
            if (GameSession.Meta != null)
                GameSession.Meta.BankedSalvage = Respawns.CostFor(0);

            LogAssert.Expect(LogType.Log,
                "[Demo] respawn refused — the host respawns at the DOWN overlay");
            serverAvatar.RequestRespawn();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsFalse(serverAvatar.WaitingOnShip.Value);
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase);
            if (GameSession.Meta != null)
                GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator DownWait_HostDie_EndsRunWhenNothingCanPay()
        {
            yield return BootHostRun();
            long bankSnap = GameSession.Meta != null ? GameSession.Meta.BankedSalvage : 0L;
            if (GameSession.Meta != null)
                GameSession.Meta.BankedSalvage = 0L;

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

            LogAssert.Expect(LogType.Log, "[Demo] no respawn — cannot afford");
            serverHealth.TakeDamage(serverHealth.Hp + 1f);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.IsTrue(serverHealth.IsDead);
            Assert.IsFalse(serverAvatar.WaitingOnShip.Value,
                "host-owned body does not wait on the Ship");
            Assert.AreEqual(RunPhase.Lost, _gc.Sim.Phase,
                "a death nothing can pay for still ends the run");
            if (GameSession.Meta != null)
                GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator Down_HostFundedPot_HoldsRunOpenAndRespawns()
        {
            yield return BootHostRun();
            Assert.IsNotNull(GameSession.Meta, "respawn needs a Meta purse");
            long bankSnap = GameSession.Meta.BankedSalvage;
            GameSession.Meta.BankedSalvage = Respawns.CostFor(0);

            _dummy = new GameObject("HostRig");
            var rig = _dummy.AddComponent<PlayerHealth>();
            rig.Bind(_gc);
            // Kills only pay during an Assault, and the pile has to exist to prove the
            // down entry wipes it.
            _gc.Sim.TriggerDraw();
            _gc.Sim.AwardKill();
            Assert.Greater(_gc.Sim.UnbankedOf(0), 0L, "there is a pile to wipe");
            yield return null;

            LogAssert.Expect(LogType.Log,
                "[Demo] down — respawn offered at " + Respawns.CostFor(0));
            rig.TakeDamage(rig.Hp + 1f);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.IsTrue(rig.IsDead);
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase,
                "a funded pot holds the run open behind the DOWN overlay");
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(0), "dying wiped the seat's pile");

            LogAssert.Expect(LogType.Log, "[Demo] respawn seat=0 cost=" + Respawns.CostFor(0));
            Assert.IsTrue(_gc.TryLocalRespawn(), "the funded respawn is granted");
            Assert.IsFalse(rig.IsDead);
            Assert.AreEqual(rig.MaxHp, rig.Hp, 0.01f);
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "the pot paid the cost");
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase);

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        // Coroutine results — an iterator cannot carry out/ref locals across yields.
        NetPlayerAvatar _guestServer;
        NetPlayerAvatar _guestClient;

        IEnumerator SpawnGuestAvatar()
        {
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

            var sa = serverGo.GetComponent<NetPlayerAvatar>();
            sa.Bind(_gc);
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has the avatar clone");
            _guestServer = sa;
            _guestClient = clientNet.GetComponent<NetPlayerAvatar>();
        }

        IEnumerator DownGuest(NetPlayerAvatar serverAvatar, NetPlayerAvatar clientAvatar)
        {
            var health = serverAvatar.Body;
            health.TakeDamage(health.Hp + 1f);
            yield return WaitForConditionOrTimeOut(() =>
                serverAvatar.WaitingOnShip.Value && health.IsDead);
            AssertOnTimeout("lethal damage did not set WaitingOnShip");
            yield return WaitForConditionOrTimeOut(() => clientAvatar.WaitingOnShip.Value);
            AssertOnTimeout("WaitingOnShip did not replicate to the owner");
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
