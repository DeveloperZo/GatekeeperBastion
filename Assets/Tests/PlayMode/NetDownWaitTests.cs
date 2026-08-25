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

        /// <summary>Fill a seat's own pile the way the game does — kills during an
        /// Assault, split across the seats — so these fixtures break if the earning path
        /// stops feeding the purse the respawn rule spends. Leaves the run in Prep.</summary>
        void FundSeat(int slot, long atLeast)
        {
            if (_gc.Sim.Assault != AssaultState.Assault) _gc.Sim.TriggerDraw();
            int guard = 0;
            while (_gc.Sim.UnbankedOf(slot) < atLeast && guard++ < 2000) _gc.Sim.AwardKill();
            Assert.GreaterOrEqual(_gc.Sim.UnbankedOf(slot), atLeast,
                "could not fund seat " + slot + " to " + atLeast);
        }

        void ToPrep()
        {
            if (_gc.Sim.Assault == AssaultState.Assault) _gc.Sim.NotifyAssaultCleared();
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

            // Carrying nothing: the offer stands but this seat cannot take it.
            int slot = serverAvatar.Slot.Value;
            LogAssert.Expect(LogType.Log, "[RunSim] respawn refused — not enough unbanked");
            clientAvatar.RequestRespawn();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value, "unaffordable respawn is refused");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage,
                "a refusal does not reach the realm's bank either");

            // Fund the SEAT, not the realm. Any Prep is a window, Hatch-bank or not.
            FundSeat(slot, Respawns.CostFor(0));
            ToPrep();
            long carryBefore = _gc.Sim.UnbankedOf(slot);
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + Respawns.CostFor(0));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("Terminal Prep respawn did not restore HP");
            Assert.AreEqual(carryBefore - Respawns.CostFor(0), _gc.Sim.UnbankedOf(slot),
                "the fee came out of the seat's own carry and the rest survived");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "the realm's bank is untouched");
            Assert.AreEqual(1, _gc.Sim.RespawnsTaken);
            Assert.AreSame(serverHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "revived avatar is huntable again");

            // The second respawn costs double — the counter is the run's.
            yield return DownGuest(serverAvatar, clientAvatar);
            FundSeat(slot, Respawns.CostFor(1));
            ToPrep();
            long carryBeforeSecond = _gc.Sim.UnbankedOf(slot);
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + Respawns.CostFor(1));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("second respawn did not land");
            Assert.AreEqual(carryBeforeSecond - Respawns.CostFor(1), _gc.Sim.UnbankedOf(slot),
                "the escalated cost came out of the carry too");
            Assert.AreEqual(2, _gc.Sim.RespawnsTaken);

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator DownWait_MidAssaultAndBreakPriceIdentically()
        {
            // The charge pool is retired: the carry is the only ration, so a mid-wave return
            // and a return at a break differ in nothing at all.
            yield return BootHostRun();
            long bankSnap = GameSession.Meta.BankedSalvage;
            GameSession.Meta.BankedSalvage = 0L;

            yield return SpawnGuestAvatar();
            var serverAvatar = _guestServer;
            var clientAvatar = _guestClient;
            int slot = serverAvatar.Slot.Value;

            FundSeat(slot, Respawns.CostFor(0) + Respawns.CostFor(1) + 200L);
            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);

            // Mid-Assault.
            yield return DownGuest(serverAvatar, clientAvatar);
            long carry = _gc.Sim.UnbankedOf(slot);
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + slot + " cost=" + Respawns.CostFor(0));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("mid-Assault respawn did not land");
            Assert.AreEqual(carry - Respawns.CostFor(0), _gc.Sim.UnbankedOf(slot),
                "mid-wave charges the price and nothing else");

            // Same run, at the break: same rule, only the counter has moved.
            ToPrep();
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
            yield return DownGuest(serverAvatar, clientAvatar);
            carry = _gc.Sim.UnbankedOf(slot);
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + slot + " cost=" + Respawns.CostFor(1));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("break respawn did not land");
            Assert.AreEqual(carry - Respawns.CostFor(1), _gc.Sim.UnbankedOf(slot),
                "the break charges the same way, at the escalated price");
            Assert.AreEqual(2, _gc.Sim.RespawnsTaken);
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage,
                "neither return touched the realm's bank");

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator Respawn_PaidByTheSeatsOwnPile_NeverTheRealmPot()
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
            int slot = serverAvatar.Slot.Value;

            FundSeat(slot, Respawns.CostFor(0) + 200L);
            ToPrep();

            // A bank fat enough to pay many times over must still pay nothing: the purse
            // moved to the seat, and a rich realm may not rescue anybody.
            GameSession.Meta.BankedSalvage = Respawns.CostFor(0) * 10L;
            long potBefore = GameSession.Meta.BankedSalvage;
            long carryBefore = _gc.Sim.UnbankedOf(slot);
            long cost = _gc.Sim.NextRespawnCost;

            yield return DownGuest(serverAvatar, clientAvatar);
            LogAssert.Expect(LogType.Log, "[Demo] respawn seat=" + slot + " cost=" + cost);
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("respawn did not restore HP");

            Assert.AreEqual(carryBefore - cost, _gc.Sim.UnbankedOf(slot),
                "the seat's own pile paid exactly the price");
            Assert.AreEqual(potBefore, GameSession.Meta.BankedSalvage,
                "the realm's bank did not move a single unit");

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
            // Funded or not, the seat check fires first: this refusal is about WHO asked.
            FundSeat(0, Respawns.CostFor(0));

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

            // Carrying nothing is what makes this death final now.
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(0), "the host seat carries nothing");
            LogAssert.Expect(LogType.Log, "[Demo] no respawn — not enough unbanked");
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
            GameSession.Meta.BankedSalvage = 0L;

            _dummy = new GameObject("HostRig");
            var rig = _dummy.AddComponent<PlayerHealth>();
            rig.Bind(_gc);
            // The carry is the purse now, so it has to exist for the offer to stand at all.
            FundSeat(0, Respawns.CostFor(0) + 100L);
            ToPrep();
            yield return null;

            LogAssert.Expect(LogType.Log,
                "[Demo] down — respawn offered at " + Respawns.CostFor(0));
            rig.TakeDamage(rig.Hp + 1f);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.IsTrue(rig.IsDead);
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase,
                "a carry that covers the price holds the run open behind the DOWN overlay");
            long carryWhileDown = _gc.Sim.UnbankedOf(0);
            Assert.Greater(carryWhileDown, 0L,
                "going down must NOT wipe the pile — it is the purse the respawn buys from");

            LogAssert.Expect(LogType.Log, "[Demo] respawn seat=0 cost=" + Respawns.CostFor(0));
            Assert.IsTrue(_gc.TryLocalRespawn(), "the funded respawn is granted");
            Assert.IsFalse(rig.IsDead);
            Assert.AreEqual(rig.MaxHp, rig.Hp, 0.01f);
            Assert.AreEqual(carryWhileDown - Respawns.CostFor(0), _gc.Sim.UnbankedOf(0),
                "the fee came out of the carry and the remainder stood up with the Boarder");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "the realm's bank never paid");
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
