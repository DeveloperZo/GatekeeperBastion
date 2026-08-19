using System.Collections;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>Voice pipe rows only Unity can check: a Lock line never writes
    /// Time.timeScale, and a mirror-broadcast Party line starts on the client seat via
    /// the ClientRpc. In-process loopback shares the runner's statics between the two
    /// seats, so the host-skip half of the RPC handler is playthrough evidence, not
    /// something this fixture can distinguish.</summary>
    public class DemoVoicePlayModeTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _mirrorPrefab;

        [SetUp]
        public void ResetRunner()
        {
            DemoVoice.ResetForTest();
            DemoVoice.ViewAttached();
        }

        [TearDown]
        public void ClearRunner()
        {
            DemoLines.SuppressTableForTest(false);
            DemoVoice.ResetForTest();
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _mirrorPrefab = CreateNetworkObjectPrefab("DemoLineMirror");
            _mirrorPrefab.AddComponent<NetRunMirror>();
        }

        [Test]
        public void LockLine_NeverTouchesTimeScale()
        {
            float before = Time.timeScale;
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            Assert.IsTrue(DemoVoice.LocksInput);
            Assert.AreEqual(before, Time.timeScale, 0f,
                "a Lock line is gated execution, never a pause");
            DemoVoice.Continue();
            Assert.AreEqual(before, Time.timeScale, 0f);
        }

        [UnityTest]
        public IEnumerator PartyBroadcast_StartsTheLineOnTheClientSeat()
        {
            var serverGo = SpawnObject(_mirrorPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("mirror did not spawn on the client");

            var serverMirror = serverGo.GetComponent<NetRunMirror>();
            // Direct server broadcast (not PlayLocalAndBroadcast): in-process the two seats
            // share DemoVoice's statics, so only the client handler may play or the local
            // play would mask whether the RPC did anything.
            serverMirror.ServerBroadcastDemoLine(DemoConversationIds.DoorD1, DemoVoiceMode.Lock);

            yield return WaitForConditionOrTimeOut(() =>
                DemoVoice.IsLive && DemoVoice.CurrentId == DemoConversationIds.DoorD1);
            AssertOnTimeout("client seat never started the broadcast line");
            Assert.IsTrue(DemoVoice.LocksInput, "the Party line arrived as a Lock line");
        }
    }
}
