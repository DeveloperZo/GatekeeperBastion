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
    /// <summary>Host plus one guest: the hatch assignment recompute writes the mirror,
    /// the guest's clone reads the same packed value, and the "assigned" log lines fire
    /// once per seat. Seat slots here come from the fixture's connection-order fallback
    /// (no NetPlayerAvatar player objects); the avatar slot path is production-only and
    /// stays eyes-on with the look-at strings.</summary>
    public class NetHatchAssignTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _mirrorPrefab;

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _mirrorPrefab = CreateNetworkObjectPrefab("NetHatchMirror");
            _mirrorPrefab.AddComponent<NetRunMirror>();
        }

        [UnityTest]
        public IEnumerator TwoSeats_OuterPairOpens_AndTheGuestReadsIt()
        {
            var serverGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            var serverMirror = serverGo.GetComponent<NetRunMirror>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("mirror did not spawn on the guest");

            // One seat first: exclude the guest by id to drive the solo shape through
            // the same host core a party-change recompute uses.
            ulong guestId = m_ClientNetworkManagers[0].LocalClientId;
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=0 hatch=2 seats=1");
            int solo = ShipHatchAssignment.ServerRecompute(
                m_ServerNetworkManager, serverMirror, "solo probe", guestId);
            Assert.IsFalse(ShipHatches.IsOpen(solo, 0), "solo: hatch 1 must read sealed");
            Assert.IsTrue(ShipHatches.IsOpen(solo, 1), "solo: the middle hatch must be open");
            Assert.IsFalse(ShipHatches.IsOpen(solo, 2), "solo: hatch 3 must read sealed");
            Assert.AreEqual(0, ShipHatches.OwnerSlot(solo, 1));

            // Both seats: the outer pair opens, the middle seals, one line per seat.
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=0 hatch=1 seats=2");
            LogAssert.Expect(LogType.Log, "[Hatch] assigned slot=1 hatch=3 seats=2");
            int packed = ShipHatchAssignment.ServerRecompute(
                m_ServerNetworkManager, serverMirror, "party change");
            Assert.IsTrue(ShipHatches.IsOpen(packed, 0), "two seats: hatch 1 must be open");
            Assert.IsFalse(ShipHatches.IsOpen(packed, 1), "two seats: the middle must seal");
            Assert.IsTrue(ShipHatches.IsOpen(packed, 2), "two seats: hatch 3 must be open");
            Assert.AreEqual(0, ShipHatches.OwnerSlot(packed, 0));
            Assert.AreEqual(1, ShipHatches.OwnerSlot(packed, 2));
            Assert.AreEqual(packed, serverMirror.HatchAssign.Value,
                "the host mirror must carry the assignment it just computed");

            // The guest's clone converges on the same packed value.
            NetRunMirror clientMirror = null;
            yield return WaitForConditionOrTimeOut(() =>
            {
                if (!m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                        serverNet.NetworkObjectId, out var clientNet))
                    return false;
                clientMirror = clientNet.GetComponent<NetRunMirror>();
                return clientMirror != null && clientMirror.HatchAssign.Value == packed;
            });
            AssertOnTimeout("the guest's mirror clone did not receive the hatch assignment");
            Assert.IsTrue(ShipHatches.IsOpen(clientMirror.HatchAssign.Value, 0));
            Assert.IsFalse(ShipHatches.IsOpen(clientMirror.HatchAssign.Value, 1));
            Assert.IsTrue(ShipHatches.IsOpen(clientMirror.HatchAssign.Value, 2));
        }
    }
}
