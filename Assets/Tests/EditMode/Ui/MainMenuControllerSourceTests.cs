using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Where hosting is allowed to start from. Source pins — starting a real session
    /// needs Relay, a bootstrap prefab and a scene load, none of which an EditMode test has, so
    /// what is guarded here is the shape of the call rather than its effect.</summary>
    public class MainMenuControllerSourceTests
    {
        const string Menu = "Assets/Scripts/Game/Flow/MainMenuController.cs";
        const string Chooser = "Assets/Scripts/Game/Flow/RealmSelectController.cs";

        static string Read(string path)
        {
            string full = Path.GetFullPath(path);
            Assert.IsTrue(File.Exists(full), full);
            return File.ReadAllText(full);
        }

        /// <summary>The menu cannot name a realm, so it must not be able to host one. It used
        /// to, by silently resolving whichever realm was opened last — a choice the player
        /// never made.</summary>
        [Test]
        public void TheMenu_CannotStartAHostSession()
        {
            string src = Read(Menu);
            StringAssert.DoesNotContain("CreateBootstrap", src);
            StringAssert.DoesNotContain("StartHostRelay", src);
            StringAssert.DoesNotContain("EnterSessionHost", src);
        }

        /// <summary>Joining is the one door to somebody else's world and needs no realm of your
        /// own, so it stays on the menu.</summary>
        [Test]
        public void TheMenu_StillJoins()
        {
            string src = Read(Menu);
            StringAssert.Contains("RelayJoin.TryJoin", src);
            StringAssert.Contains("EnterSessionJoinRelay", src);
            StringAssert.Contains("EnterSessionJoinLan", src);
        }

        /// <summary>The chooser hosts, and enters the picked realm BEFORE it starts the
        /// session. Reversing those two would host a realm nobody chose, which is the whole
        /// defect this arrangement exists to prevent.</summary>
        [Test]
        public void TheChooser_EntersThePickedRealm_BeforeItStartsHosting()
        {
            string src = Read(Chooser);
            int hosting = src.IndexOf("void StartHosting");
            Assert.Greater(hosting, 0, "the chooser must own a StartHosting path");
            int enter = src.IndexOf("GameSession.EnterRealm(realmId, boarderId)", hosting);
            int boot = src.IndexOf("NetSession.CreateBootstrap()", hosting);
            Assert.Greater(enter, 0, "StartHosting must enter the picked realm");
            Assert.Greater(boot, 0, "StartHosting must bootstrap a session");
            Assert.Less(enter, boot, "the realm is chosen before anyone hosts it");
        }

        /// <summary>Relay going down must not take co-op with it — the LAN fallback moved
        /// across with the rest of hosting.</summary>
        [Test]
        public void TheChooser_KeepsTheLanFallback()
        {
            string src = Read(Chooser);
            StringAssert.Contains("StartHosting(relay: false)", src);
            StringAssert.Contains("session.StartHost()", src);
        }
    }
}
