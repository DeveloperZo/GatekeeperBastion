using System.Collections;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>Client Prep RPCs apply once on the host: juice raises siphon; Field start
    /// enters Assault; empty-range start refuses without starting.</summary>
    public class NetPrepTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        GameObject _prepPrefab;
        GameController _gc;
        NetPrep _clientPrep;
        BastionTerminal _terminal;
        GameObject _layoutGo;

        [UnityTearDown]
        public IEnumerator DestroyHostRun()
        {
            if (_terminal != null)
            {
                Object.Destroy(_terminal.gameObject);
                _terminal = null;
            }
            if (_layoutGo != null)
            {
                Object.Destroy(_layoutGo);
                _layoutGo = null;
            }
            if (_gc != null)
            {
                Object.Destroy(_gc.gameObject);
                _gc = null;
            }
            _clientPrep = null;
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _prepPrefab = CreateNetworkObjectPrefab("NetPrepTest");
            _prepPrefab.AddComponent<NetPrep>();
        }

        [UnityTest]
        public IEnumerator Juice_ClientRpc_RaisesHostSiphonOnce()
        {
            yield return BootHostRun();
            long bankSnap = _gc.Meta.BankedSalvage;
            BankKills(10);
            // The Ante charges the BANK (Zo, 2026-08-25) — kills bank on landing, so the
            // carried pile this used to read is empty unless somebody deliberately Withdrew.
            long before = _gc.Sim.BankedSalvage;
            Assert.GreaterOrEqual(before, IntensityJuice.CostPerStep);
            Assert.AreEqual(0, _gc.Sim.Siphon.Level);

            yield return SpawnClientPrep();
            _clientPrep.RequestJuice(1);
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.AreEqual(1, _gc.Sim.Siphon.Level);
            Assert.AreEqual(before - IntensityJuice.CostPerStep, _gc.Sim.BankedSalvage);
            Assert.AreEqual(0L, _gc.Sim.Unbanked,
                "and never a pile nobody staked — the client's RPC cannot invent one");

            // The realm's pot outlives this fixture; hand it back what the Ante spent.
            _gc.Meta.BankedSalvage = bankSnap;
        }

        [UnityTest]
        public IEnumerator StartTerminal_ClientRpc_PutsHostInAssault()
        {
            yield return BootHostRun();
            PlaceFieldTerminal("T1");
            yield return SpawnClientPrepAt(Vector3.zero);

            _clientPrep.RequestStartTerminal("T1");
            yield return WaitForTicks(m_ServerNetworkManager, 6);

            Assert.AreEqual(AssaultState.Assault, _gc.Sim.Assault);
        }

        [UnityTest]
        public IEnumerator StartTerminal_OutOfRange_Refuses()
        {
            yield return BootHostRun();
            PlaceFieldTerminal("T1");
            yield return SpawnClientPrepAt(new Vector3(50f, 0f, 50f));

            LogAssert.Expect(LogType.Log, new Regex("out of range"));
            _clientPrep.RequestStartTerminal("T1");
            yield return WaitForTicks(m_ServerNetworkManager, 4);

            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
        }

        IEnumerator BootHostRun()
        {
            var go = new GameObject("HostGCPrep");
            _gc = go.AddComponent<GameController>();
            yield return null;
            yield return null;
            Assert.IsNotNull(_gc.Sim);
            Assert.AreEqual(RunPhase.Tethered, _gc.Sim.Phase);
            Assert.AreEqual(AssaultState.Prep, _gc.Sim.Assault);
        }

        /// <summary>Earn into the realm's pot the way the game does. Was CreditUnbanked:
        /// AwardKill banks at face value now (Zo, 2026-08-25) and never touches a pile.
        /// Ends back in Prep because the Ante is a Prep verb.</summary>
        void BankKills(int kills)
        {
            _gc.Sim.TriggerDraw();
            for (int i = 0; i < kills; i++)
                _gc.Sim.AwardKill();
            _gc.Sim.NotifyAssaultCleared();
        }

        void PlaceFieldTerminal(string id)
        {
            _layoutGo = new GameObject("BastionLayout_Prep");
            var layout = _layoutGo.AddComponent<BastionLayout>();
            var go = new GameObject("FieldTerminal_" + id);
            go.transform.position = Vector3.zero;
            _terminal = go.AddComponent<BastionTerminal>();
            _terminal.Configure(id, BastionTerminalKind.Field);
            _terminal.SetGameController(_gc);
            layout.RegisterTerminal(_terminal);
            var layoutField = typeof(GameController).GetField("_layout",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(layoutField);
            layoutField.SetValue(_gc, layout);
        }

        IEnumerator SpawnClientPrep()
        {
            yield return SpawnClientPrepAt(Vector3.zero);
        }

        IEnumerator SpawnClientPrepAt(Vector3 pos)
        {
            var serverGo = SpawnObject(_prepPrefab, m_ServerNetworkManager);
            serverGo.transform.position = pos;
            var serverNet = serverGo.GetComponent<NetworkObject>();
            serverGo.GetComponent<NetPrep>().Bind(_gc);
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("prep object did not spawn on client");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.TryGetValue(
                    serverNet.NetworkObjectId, out var clientNet),
                "client has prep clone");
            clientNet.transform.position = pos;
            _clientPrep = clientNet.GetComponent<NetPrep>();
            Assert.IsNotNull(_clientPrep);
        }
    }
}
