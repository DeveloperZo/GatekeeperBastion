using System.Collections;
using GatekeeperBastion.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>Proves StartHost / StartClient push bind and join addresses onto UnityTransport.
    /// Does not connect to 10.0.0.1 — Shutdown runs before the timeout.</summary>
    public class NetSessionAddressTests
    {
        [SetUp]
        public void ClearJoinIntent() => NetJoinIntent.Clear();

        [Test]
        public void TryParseJoinCode_AcceptsSixAlphanumeric_CaseInsensitive()
        {
            Assert.IsTrue(NetJoinIntent.TryParseJoinCode("abc123", out string a));
            Assert.AreEqual("ABC123", a);
            Assert.IsTrue(NetJoinIntent.TryParseJoinCode("  Z9K2M1 ", out string b));
            Assert.AreEqual("Z9K2M1", b);
            Assert.IsFalse(NetJoinIntent.TryParseJoinCode("", out _));
            Assert.IsFalse(NetJoinIntent.TryParseJoinCode("abc12", out _));
            Assert.IsFalse(NetJoinIntent.TryParseJoinCode("abc1234", out _));
            Assert.IsFalse(NetJoinIntent.TryParseJoinCode("abc12!", out _));
            Assert.IsFalse(NetJoinIntent.TryParseJoinCode("10.0.0.1", out _));
        }

        [Test]
        public void TryParseIpv4_AcceptsDottedQuad_RejectsHostnames()
        {
            Assert.IsTrue(NetJoinIntent.TryParseIpv4("10.0.0.1", out string a));
            Assert.AreEqual("10.0.0.1", a);
            Assert.IsTrue(NetJoinIntent.TryParseIpv4(" 192.168.1.50 ", out string b));
            Assert.AreEqual("192.168.1.50", b);
            Assert.IsFalse(NetJoinIntent.TryParseIpv4("localhost", out _));
            Assert.IsFalse(NetJoinIntent.TryParseIpv4("10.0.0", out _));
            Assert.IsFalse(NetJoinIntent.TryParseIpv4("256.0.0.1", out _));
            Assert.IsFalse(NetJoinIntent.TryParseIpv4("", out _));
            Assert.IsFalse(NetJoinIntent.IsJoinableAddress("0.0.0.0"));
            Assert.IsTrue(NetJoinIntent.IsJoinableAddress("127.0.0.1"));
        }

        [Test]
        public void PlayerLaunch_TryParse_HostAndJoin()
        {
            Assert.IsTrue(PlayerLaunch.TryParse(
                new[] { "GatekeeperBastion.exe", PlayerLaunch.HostArg, PlayerLaunch.QuitAfterArg, "12" },
                out var host));
            Assert.AreEqual(NetJoinIntent.Kind.Host, host.Kind);
            Assert.AreEqual(12f, host.QuitAfterSeconds, 0.01f);

            Assert.IsTrue(PlayerLaunch.TryParse(
                new[] { PlayerLaunch.JoinArg, "127.0.0.1:7777" },
                out var join));
            Assert.AreEqual(NetJoinIntent.Kind.Join, join.Kind);
            Assert.AreEqual("127.0.0.1", join.Address);
            Assert.AreEqual((ushort)7777, join.Port);

            Assert.IsFalse(PlayerLaunch.TryParse(
                new[] { PlayerLaunch.JoinArg, "0.0.0.0" },
                out _));
            Assert.IsFalse(PlayerLaunch.TryParse(new[] { PlayerLaunch.JoinArg }, out _));
        }

        [UnityTest]
        public IEnumerator StartClient_SetsTransportAddress()
        {
            NetJoinIntent.Clear();
            var go = new GameObject("NetSessionAddressClient");
            var nm = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            if (nm.NetworkConfig == null)
                nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.EnableSceneManagement = false;
            nm.NetworkConfig.NetworkTransport = transport;
            var session = go.AddComponent<NetSession>();
            Assert.IsNotNull(session);
            yield return null;
            // NetSession.Awake enables scene management for friends sessions; fixtures force off.
            nm.NetworkConfig.EnableSceneManagement = false;

            session.StartClient("10.0.0.1", 7777);
            Assert.AreEqual("10.0.0.1", transport.ConnectionData.Address);
            Assert.AreEqual((ushort)7777, transport.ConnectionData.Port);

            if (nm.IsListening) nm.Shutdown();
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartHost_ListensOnAllInterfaces()
        {
            NetJoinIntent.Clear();
            var go = new GameObject("NetSessionAddressHost");
            var nm = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            if (nm.NetworkConfig == null)
                nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.EnableSceneManagement = false;
            nm.NetworkConfig.NetworkTransport = transport;
            var session = go.AddComponent<NetSession>();
            Assert.IsNotNull(session);
            yield return null;
            nm.NetworkConfig.EnableSceneManagement = false;

            session.StartHost();
            Assert.AreEqual("0.0.0.0", transport.ConnectionData.ServerListenAddress);
            Assert.AreEqual("127.0.0.1", transport.ConnectionData.Address);
            Assert.AreEqual((ushort)7777, transport.ConnectionData.Port);

            if (nm.IsListening) nm.Shutdown();
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartClient_RejectsListenAllAddress()
        {
            NetJoinIntent.Clear();
            var go = new GameObject("NetSessionRejectAll");
            var nm = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            if (nm.NetworkConfig == null)
                nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.EnableSceneManagement = false;
            nm.NetworkConfig.NetworkTransport = transport;
            var session = go.AddComponent<NetSession>();
            Assert.IsNotNull(session);
            yield return null;
            nm.NetworkConfig.EnableSceneManagement = false;

            session.StartClient("0.0.0.0", 7777);
            Assert.IsFalse(nm.IsListening, "0.0.0.0 must not start a client session");

            if (nm.IsListening) nm.Shutdown();
            Object.Destroy(go);
            yield return null;
        }
    }
}
