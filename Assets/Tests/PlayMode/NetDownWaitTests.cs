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
    /// paid respawn returns it at any Prep and mid-Assault alike — the charge pool is gone,
    /// and each respawn escalates in cost out of the realm's pot (Zo, 2026-08-25: kills bank
    /// as they land, so the bank is the purse and a staked pile is only ever a bet). Host
    /// death holds the run open only while a respawn can be honored. Does not load the Ship
    /// scene.</summary>
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

        /// <summary>Fill the realm's bank the way the game does — kills during an Assault,
        /// which bank at face value the moment the body falls (Zo, 2026-08-25) — so these
        /// fixtures break if the earning path stops feeding the purse the respawn rule
        /// spends. Was FundSeat: kills no longer reach a seat's own pile at all, so a loop
        /// waiting on UnbankedOf could never terminate. Leaves the run in Assault; a test
        /// that wants the break calls <see cref="ToPrep"/> itself.</summary>
        void FundBank(long atLeast)
        {
            if (_gc.Sim.Assault != AssaultState.Assault) _gc.Sim.TriggerDraw();
            int guard = 0;
            while (_gc.Sim.BankedSalvage < atLeast && guard++ < 2000) _gc.Sim.AwardKill();
            Assert.GreaterOrEqual(_gc.Sim.BankedSalvage, atLeast,
                "could not fund the realm's bank to " + atLeast);
        }

        /// <summary>Stake <paramref name="want"/> out of the bank into <paramref name="slot"/>'s
        /// carried pile by holding the real Withdraw verb — the only filler of a pile since
        /// kills started banking, and so the only honest way to put salvage at risk. A back
        /// door onto the wallets would keep passing after the withdraw gate broke. Prep only,
        /// and Withdraw debits the bank before it credits the slot, so an uncounted seat
        /// destroys the units instead of receiving them: both preconditions are checked.</summary>
        long StakeInto(int slot, long want)
        {
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault, "Withdraw is a Prep verb");
            Assert.Greater(_gc.Sim.PlayerCount, slot,
                "the sim must be counting seat " + slot + " or the withdrawn units vanish");
            long before = _gc.Sim.UnbankedOf(slot);
            long target = before + want;
            int steps = 0;
            while (_gc.Sim.UnbankedOf(slot) < target)
            {
                if (_gc.Sim.Withdraw(0.25f, slot) <= 0L)
                    Assert.Fail("Withdraw refused for slot " + slot + " — bank="
                        + _gc.Sim.BankedSalvage + ", phase=" + _gc.Sim.Phase + "/"
                        + _gc.Sim.Assault + ", hatch=" + _gc.Sim.Ship.Hatch.Hp + "/"
                        + _gc.Sim.Ship.Hatch.MaxHp);
                if (++steps > 4000) Assert.Fail("Withdraw never reached " + want);
            }
            return _gc.Sim.UnbankedOf(slot) - before;
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

            // An empty realm: the offer stands but nothing can pay for it.
            int slot = serverAvatar.Slot.Value;
            LogAssert.Expect(LogType.Log, "[RunSim] respawn refused — not enough banked");
            clientAvatar.RequestRespawn();
            yield return WaitForTicks(m_ServerNetworkManager, 8);
            Assert.IsTrue(serverAvatar.WaitingOnShip.Value, "unaffordable respawn is refused");
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage,
                "a refusal debits nothing");

            // Fund the REALM: the fee charges the bank since 2026-08-25. Any Prep is a
            // window, Hatch-bank or not.
            FundBank(Respawns.CostFor(0));
            ToPrep();
            long bankBefore = _gc.Sim.BankedSalvage;
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + Respawns.CostFor(0));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("Terminal Prep respawn did not restore HP");
            Assert.AreEqual(bankBefore - Respawns.CostFor(0), _gc.Sim.BankedSalvage,
                "the fee came out of the realm's bank and the rest of the bank survived");
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(slot),
                "a seat that never Withdrew carries nothing, and is charged nothing");
            Assert.AreEqual(1, _gc.Sim.RespawnsTaken);
            Assert.AreSame(serverHealth, PlayerRegistry.NearestAlive(Vector3.zero),
                "revived avatar is huntable again");

            // The second respawn costs double — the counter is the run's.
            yield return DownGuest(serverAvatar, clientAvatar);
            FundBank(Respawns.CostFor(1));
            ToPrep();
            long bankBeforeSecond = _gc.Sim.BankedSalvage;
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + serverAvatar.Slot.Value + " cost=" + Respawns.CostFor(1));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("second respawn did not land");
            Assert.AreEqual(bankBeforeSecond - Respawns.CostFor(1), _gc.Sim.BankedSalvage,
                "the escalated cost came out of the bank too");
            Assert.AreEqual(2, _gc.Sim.RespawnsTaken);

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator DownWait_MidAssaultAndBreakPriceIdentically()
        {
            // The charge pool is retired: the bank is the only ration, so a mid-wave return
            // and a return at a break differ in nothing at all.
            yield return BootHostRun();
            long bankSnap = GameSession.Meta.BankedSalvage;
            GameSession.Meta.BankedSalvage = 0L;

            yield return SpawnGuestAvatar();
            var serverAvatar = _guestServer;
            var clientAvatar = _guestClient;
            int slot = serverAvatar.Slot.Value;

            FundBank(Respawns.CostFor(0) + Respawns.CostFor(1) + 200L);
            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);

            // Mid-Assault.
            yield return DownGuest(serverAvatar, clientAvatar);
            long bank = _gc.Sim.BankedSalvage;
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + slot + " cost=" + Respawns.CostFor(0));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("mid-Assault respawn did not land");
            Assert.AreEqual(bank - Respawns.CostFor(0), _gc.Sim.BankedSalvage,
                "mid-wave charges the price and nothing else");

            // Same run, at the break: same rule, only the counter has moved.
            ToPrep();
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
            yield return DownGuest(serverAvatar, clientAvatar);
            bank = _gc.Sim.BankedSalvage;
            LogAssert.Expect(LogType.Log,
                "[Demo] respawn seat=" + slot + " cost=" + Respawns.CostFor(1));
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() => !serverAvatar.WaitingOnShip.Value);
            AssertOnTimeout("break respawn did not land");
            Assert.AreEqual(bank - Respawns.CostFor(1), _gc.Sim.BankedSalvage,
                "the break charges the same way, at the escalated price");
            Assert.AreEqual(2, _gc.Sim.RespawnsTaken);
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(slot),
                "neither return reached a pile this seat never staked");

            GameSession.Meta.BankedSalvage = bankSnap;
        }

        /// <summary>INVERTED 2026-08-25 (Zo, kills bank as they land). This was
        /// Respawn_PaidByTheSeatsOwnPile_NeverTheRealmPot, and while kills filled a seat's
        /// carry for free that was exactly right — a rich realm could not rescue an
        /// empty-handed seat, and the carry was the ration. Kills bank now, so a carry only
        /// exists where the player deliberately Withdrew: charging it would tax the seat
        /// that gambled, spare the seat that did not, and price a never-gambling seat's
        /// return at infinity — the no-progress death the ruling exists to forbid. The
        /// SEPARATION is what survives, pointed the other way: the pot pays the whole fee
        /// and the staked pile behind it does not move a single unit, which is what keeps a
        /// Withdraw a real bet rather than a second toll on dying.</summary>
        [UnityTest]
        public IEnumerator Respawn_PaidByTheRealmPot_NeverTheSeatsStakedPile()
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

            // A pot fat enough to pay many times over, with 200 of it deliberately pulled
            // out into this seat's own pile — the only salvage a death may still take.
            FundBank(Respawns.CostFor(0) * 10L + 200L);
            ToPrep();
            long staked = StakeInto(slot, 200L);
            Assert.AreEqual(200L, staked, "the withdraw verb staked what was asked for");

            long potBefore = _gc.Sim.BankedSalvage;
            long cost = _gc.Sim.NextRespawnCost;

            yield return DownGuest(serverAvatar, clientAvatar);
            LogAssert.Expect(LogType.Log, "[Demo] respawn seat=" + slot + " cost=" + cost);
            clientAvatar.RequestRespawn();
            yield return WaitForConditionOrTimeOut(() =>
                !serverAvatar.WaitingOnShip.Value
                && !serverHealth.IsDead
                && serverHealth.Hp >= serverHealth.MaxHp - 0.01f);
            AssertOnTimeout("respawn did not restore HP");

            Assert.AreEqual(potBefore - cost, _gc.Sim.BankedSalvage,
                "the realm's pot paid exactly the price");
            Assert.AreEqual(staked, _gc.Sim.UnbankedOf(slot),
                "the seat's staked bet came through the death whole");

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
            FundBank(Respawns.CostFor(0));

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

            // An empty realm is what makes this death final now — and no stake behind it
            // either, so there is genuinely nothing anywhere that could buy the seat back.
            Assert.AreEqual(0L, _gc.Sim.BankedSalvage, "the realm's bank is empty");
            Assert.AreEqual(0L, _gc.Sim.UnbankedOf(0), "and the host seat staked nothing");
            LogAssert.Expect(LogType.Log, "[Demo] no respawn — not enough banked");
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
            // The bank is the purse now, so it has to cover the price for the offer to
            // stand at all. 100 of it is then staked, so the death has something it could
            // wipe and can be caught not wiping it.
            FundBank(Respawns.CostFor(0) + 200L);
            ToPrep();
            long staked = StakeInto(0, 100L);
            yield return null;

            LogAssert.Expect(LogType.Log,
                "[Demo] down — respawn offered at " + Respawns.CostFor(0));
            rig.TakeDamage(rig.Hp + 1f);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.IsTrue(rig.IsDead);
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase,
                "a bank that covers the price holds the run open behind the DOWN overlay");
            long bankWhileDown = _gc.Sim.BankedSalvage;
            Assert.GreaterOrEqual(bankWhileDown, Respawns.CostFor(0),
                "going down must NOT drain the bank — it is the purse the respawn buys from");
            Assert.AreEqual(staked, _gc.Sim.UnbankedOf(0),
                "nor wipe the stake: a death that can still buy back is not a loss yet");

            LogAssert.Expect(LogType.Log, "[Demo] respawn seat=0 cost=" + Respawns.CostFor(0));
            Assert.IsTrue(_gc.TryLocalRespawn(), "the funded respawn is granted");
            Assert.IsFalse(rig.IsDead);
            Assert.AreEqual(rig.MaxHp, rig.Hp, 0.01f);
            Assert.AreEqual(bankWhileDown - Respawns.CostFor(0), _gc.Sim.BankedSalvage,
                "the fee came out of the bank and the remainder of it survived the death");
            Assert.AreEqual(staked, _gc.Sim.UnbankedOf(0),
                "the stake stood up with the Boarder — the fee is not a second toll on the bet");
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
