using System.Collections;
using GatekeeperBastion.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>Friends-session lobby kernel: ESM true on NetSession; guest local Bastion
    /// load refused. Full host NetworkSceneManager party-load of Ship/Bastion across two
    /// NetcodeIntegrationTest managers is deferred — NGO multi-NM + ESM true needs build
    /// scenes registered on both and fights the M48 fixture's ESM-false contract
    /// (POC: dual NetworkManager scene sync in-process).</summary>
    public class NetLobbySessionTests
    {
        [SetUp]
        public void ClearJoinIntent() => NetJoinIntent.Clear();

        [UnityTest]
        public IEnumerator NetSession_StartHost_EnablesSceneManagement()
        {
            NetJoinIntent.Clear();
            var go = new GameObject("NetLobbyEsmHost");
            var nm = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            if (nm.NetworkConfig == null)
                nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.NetworkTransport = transport;
            var session = go.AddComponent<NetSession>();
            Assert.IsNotNull(session);
            yield return null;

            Assert.IsTrue(nm.NetworkConfig.EnableSceneManagement,
                "friends NetSession must enable scene management");

            session.StartHost();
            Assert.IsTrue(nm.IsListening);
            Assert.IsTrue(nm.NetworkConfig.EnableSceneManagement);

            if (nm.IsListening) nm.Shutdown();
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Guest_LocalBastionLoad_RefusedWhileSessionActive()
        {
            NetJoinIntent.Clear();
            var go = new GameObject("NetLobbyGuestRefuse");
            var nm = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            if (nm.NetworkConfig == null)
                nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.NetworkTransport = transport;
            // Keep ESM false here so StartClient does not pull build scenes in the fixture.
            nm.NetworkConfig.EnableSceneManagement = false;
            go.AddComponent<NetSession>();
            yield return null;

            // Force false again — NetSession.Awake sets true for production sessions.
            nm.NetworkConfig.EnableSceneManagement = false;
            var session = go.GetComponent<NetSession>();
            session.StartHost();
            Assert.IsTrue(session.Active);
            Assert.IsTrue(session.IsPartyLead);

            // Simulate a client-only seat by starting a second manager is heavy; assert the
            // session-active refuse. Lead sees "use party EMBARK"; guests see "not party lead"
            // (SceneFlow.LoadBastion branches on IsClientOnly).
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                "embark refused — use party EMBARK|embark refused — not party lead"));
            Assert.IsFalse(SceneFlow.LoadBastion());

            // POC: dual-manager NetworkSceneManager Ship/Bastion party load not covered —
            // NetcodeIntegrationTest fixtures force EnableSceneManagement=false (M48 contract);
            // ESM-true scene sync needs build-registered scenes on both managers in-process.

            if (nm.IsListening) nm.Shutdown();
            Object.Destroy(go);
            yield return null;
        }
    }
}
