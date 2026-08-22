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
    /// <summary>Host plus one guest, with the guest standing on the Ship rather than in the
    /// run. What is pinned here is that the doors follow the seats *playing* and not the seats
    /// *connected*: a late arrival holds no hatch open until it follows the party in, and a
    /// seat that drops keeps its door until the break. Seat slots come from the fixture's
    /// connection-order fallback, as in the assignment tests — no player avatars are spawned
    /// (Docs/Milestones — drop in between waves).</summary>
    public class NetFollowSeatTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _mirrorPrefab;

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _mirrorPrefab = CreateNetworkObjectPrefab("NetFollowMirror");
            _mirrorPrefab.AddComponent<NetRunMirror>();
            RunSeats.Reset();
        }

        protected override IEnumerator OnTearDown()
        {
            RunSeats.Reset();
            yield return base.OnTearDown();
        }

        [UnityTest]
        public IEnumerator ASeatWaitingOnTheShip_HoldsNoDoor_UntilItFollowsAtTheBreak()
        {
            var serverGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            var serverMirror = serverGo.GetComponent<NetRunMirror>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("mirror did not spawn on the guest");

            ulong hostId = m_ServerNetworkManager.LocalClientId;
            ulong guestId = m_ClientNetworkManagers[0].LocalClientId;

            // The host embarked alone. The guest connects during the run, so it is in the
            // connected list and out of the roster.
            RunSeats.BeginRun(null);
            Assert.IsTrue(RunSeats.Admit(hostId));
            Assert.IsFalse(RunSeats.IsAdmitted(guestId));

            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=0 hatch=2 seats=1");
            int waiting = ShipHatchAssignment.ServerRecompute(
                m_ServerNetworkManager, serverMirror, "guest waiting on the ship");
            Assert.IsTrue(ShipHatches.IsOpen(waiting, 1),
                "the seat in the run keeps the middle hatch");
            Assert.IsFalse(ShipHatches.IsOpen(waiting, 0),
                "a connected seat that has not followed holds no hatch open");
            Assert.IsFalse(ShipHatches.IsOpen(waiting, 2));

            // It follows at the break. Its door opens on the same recompute.
            Assert.IsTrue(RunSeats.Admit(guestId));
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=0 hatch=1 seats=2");
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=1 hatch=3 seats=2");
            int followed = ShipHatchAssignment.ServerRecompute(
                m_ServerNetworkManager, serverMirror, "seat followed the party");
            Assert.IsTrue(ShipHatches.IsOpen(followed, 0), "the arriving seat's hatch opens");
            Assert.IsTrue(ShipHatches.IsOpen(followed, 2));
            Assert.IsFalse(ShipHatches.IsOpen(followed, 1), "two seats seal the middle");
            Assert.AreEqual(followed, serverMirror.HatchAssign.Value,
                "the host mirror carries what it just computed");

            // And the guest's clone reads the same thing, so the door that opened is the
            // door the arriving player sees open.
            NetRunMirror clientMirror = null;
            yield return WaitForConditionOrTimeOut(() =>
            {
                if (!m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                        serverNet.NetworkObjectId, out var clientNet))
                    return false;
                clientMirror = clientNet.GetComponent<NetRunMirror>();
                return clientMirror != null && clientMirror.HatchAssign.Value == followed;
            });
            AssertOnTimeout("the guest never read the assignment the host wrote");
        }

        [UnityTest]
        public IEnumerator ASeatDropping_KeepsItsDoor_UntilTheBreakSealsIt()
        {
            var serverGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            var serverMirror = serverGo.GetComponent<NetRunMirror>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("mirror did not spawn on the guest");

            ulong hostId = m_ServerNetworkManager.LocalClientId;
            ulong guestId = m_ClientNetworkManagers[0].LocalClientId;

            RunSeats.BeginRun(null);
            RunSeats.Admit(hostId);
            RunSeats.Admit(guestId);

            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=0 hatch=1 seats=2");
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=1 hatch=3 seats=2");
            int both = ShipHatchAssignment.ServerRecompute(
                m_ServerNetworkManager, serverMirror, "party of two");
            Assert.IsTrue(ShipHatches.IsOpen(both, 0));
            Assert.IsTrue(ShipHatches.IsOpen(both, 2));

            // The seat drops mid-wave and the roster holds it. What is asserted here is the
            // ROSTER, not the doors: this fixture keeps the guest in ConnectedClientsList, so a
            // recompute would see it whether or not the roster held it, and asserting the doors
            // here would pass with the roster filter deleted. The real guarantee — that nothing
            // recomputes mid-Assault at all — lives in the disconnect path, which calls
            // NoteLeft and deliberately returns before Recompute (NetSession.Internal.cs).
            RunSeats.NoteLeft(guestId);
            Assert.IsTrue(RunSeats.IsAdmitted(guestId),
                "sealing a hatch under attack would be a free save — the seat is still counted");
            Assert.IsTrue(RunSeats.HasPending, "and the departure is waiting for the break");

            // The break applies it and the door seals there.
            Assert.IsTrue(RunSeats.ApplyBoundary());
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=0 hatch=2 seats=1");
            int afterBreak = ShipHatchAssignment.ServerRecompute(
                m_ServerNetworkManager, serverMirror, "seat left at the break");
            Assert.IsTrue(ShipHatches.IsOpen(afterBreak, 1), "one seat is the middle hatch again");
            Assert.IsFalse(ShipHatches.IsOpen(afterBreak, 0), "the leaver's door seals at the break");
            Assert.IsFalse(ShipHatches.IsOpen(afterBreak, 2));
        }
    }
}
