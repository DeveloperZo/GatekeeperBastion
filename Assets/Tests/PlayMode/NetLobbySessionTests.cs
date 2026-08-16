using System.Collections;
using System.IO;
using GatekeeperBastion.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>Friends-session lobby kernel: ESM true on NetSession; host local Bastion
    /// load refused. Full host NetworkSceneManager party-load of Ship/Bastion across two
    /// NetcodeIntegrationTest managers is deferred — NGO multi-NM + ESM true needs build
    /// scenes registered on both and fights the M48 fixture's ESM-false contract
    /// (POC: dual NetworkManager scene sync in-process). Guest IsClientOnly refuse is
    /// covered by SceneFlow source strings (this fixture only starts a host).</summary>
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

        /// <summary>Lead-only fixture: StartHost → IsClientOnly is false, so LoadBastion
        /// logs "use party EMBARK". Guest "not party lead" is asserted via source strings
        /// in <see cref="SceneFlow_LoadBastion_RefuseLines_PresentInSource"/>.</summary>
        [UnityTest]
        public IEnumerator Host_LocalBastionLoad_RefusedWhileSessionActive()
        {
            NetJoinIntent.Clear();
            var go = new GameObject("NetLobbyHostRefuse");
            var nm = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            if (nm.NetworkConfig == null)
                nm.NetworkConfig = new NetworkConfig();
            // Keep ESM false here so StartHost does not pull build scenes in the fixture.
            nm.NetworkConfig.EnableSceneManagement = false;
            go.AddComponent<NetSession>();
            yield return null;

            // Force false again — NetSession.Awake sets true for production sessions.
            nm.NetworkConfig.EnableSceneManagement = false;
            var session = go.GetComponent<NetSession>();
            session.StartHost();
            Assert.IsTrue(session.Active);
            Assert.IsTrue(session.IsPartyLead);
            Assert.IsFalse(session.IsClientOnly,
                "this fixture is host-only; guest IsClientOnly refuse is source-string covered");

            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                "embark refused — use party EMBARK"));
            Assert.IsFalse(SceneFlow.LoadBastion());

            // POC: dual-manager NetworkSceneManager Ship/Bastion party load not covered —
            // NetcodeIntegrationTest fixtures force EnableSceneManagement=false (M48 contract);
            // ESM-true scene sync needs build-registered scenes on both managers in-process.

            if (nm.IsListening) nm.Shutdown();
            Object.Destroy(go);
            yield return null;
        }

        /// <summary>Guest refuse line cannot be exercised without a second listening client
        /// (IsClientOnly = Active &amp;&amp; IsClient &amp;&amp; !IsServer). Lock both SceneFlow
        /// refuse strings in source so a rename cannot drop the guest branch silently.</summary>
        [Test]
        public void SceneFlow_LoadBastion_RefuseLines_PresentInSource()
        {
            string path = Path.GetFullPath("Assets/Scripts/Game/Flow/SceneFlow.cs");
            Assert.IsTrue(File.Exists(path), path);
            string src = File.ReadAllText(path);
            StringAssert.Contains("embark refused — not party lead", src);
            StringAssert.Contains("embark refused — use party EMBARK", src);
            StringAssert.Contains("IsClientOnly", src);
        }
    }
}
