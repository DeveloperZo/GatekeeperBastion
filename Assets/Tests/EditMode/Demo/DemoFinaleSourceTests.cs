using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for the Game-side half of the finale. MonoBehaviours, so the
    /// offline harness cannot run them; what it can do is prove the voice hooks, the EndDemo
    /// call, and the escaped-line carrier are present.</summary>
    public class DemoFinaleSourceTests
    {
        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path);
        }

        [Test]
        public void D3_SpeaksControlRoom_NotADoorId()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            StringAssert.Contains("if (doorId == \"D3\") return DemoConversationIds.ControlRoom;", src);
            string ids = Read("Assets/Scripts/Game/Demo/DemoConversationIds.cs");
            Assert.IsFalse(ids.Contains("= \"demo.door_d3\""),
                "demo.door_d3 is deliberately absent — no const");
        }

        [Test]
        public void MasterStart_LogsTheRoomFight()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.MasterControl.cs");
            StringAssert.Contains("master-control room fight", src);
            StringAssert.Contains("DemoConversationIds.MasterControl", src);
        }

        [Test]
        public void HatchHalf_SendsFinaleSubtitle_BeforeAnyHalfFlipLogGate()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            int send = src.IndexOf("hatch finale send", System.StringComparison.Ordinal);
            int line = src.IndexOf("DemoConversationIds.HatchFinale", System.StringComparison.Ordinal);
            Assert.Greater(send, 0);
            Assert.Greater(line, send, "the send log is the always-on evidence; the line follows");
            StringAssert.Contains("DemoVoiceMode.Subtitle", src);
        }

        [Test]
        public void Escaped_CallsEndDemo_AndArmsTheShipLine()
        {
            string end = Read("Assets/Scripts/Game/Bastion/GameController.EndScreen.cs");
            int escaped = end.IndexOf("if (phase == RunPhase.Escaped)", System.StringComparison.Ordinal);
            int pending = end.IndexOf("PendingEscapedLine", escaped, System.StringComparison.Ordinal);
            int demo = end.IndexOf("DemoProgress.EndDemo", escaped, System.StringComparison.Ordinal);
            int returned = end.IndexOf("else if (phase == RunPhase.Returned", escaped, System.StringComparison.Ordinal);
            Assert.Greater(pending, escaped);
            Assert.Greater(demo, pending, "the carrier is armed before DemoActive flips");
            Assert.Greater(returned, demo, "EndDemo sits in the Escaped arm, before Returned/Lost");
            string after = end.Substring(returned);
            Assert.IsFalse(after.Contains("EndDemo("), "Lost and Docked must not end the demo");

            string ship = Read("Assets/Scripts/Game/Ship/DemoShipDirector.cs");
            int consume = ship.IndexOf("PendingEscapedLine", System.StringComparison.Ordinal);
            int play = ship.IndexOf("DemoConversationIds.Escaped", System.StringComparison.Ordinal);
            Assert.Greater(consume, 0);
            Assert.Greater(play, consume);
        }

        [Test]
        public void EliteSpawn_OverridesTheFirstThiefHp()
        {
            string src = Read("Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs");
            StringAssert.Contains("FinaleElitePending", src);
            StringAssert.Contains("MasterControl.FinaleEliteHp", src);
            StringAssert.Contains("finale elite hp=", src);
        }

        [Test]
        public void DockOffer_IsRefusedOnTheFinale()
        {
            string src = Read("Assets/Scripts/Sim/Run/RunSim.Dock.cs");
            int hatch = src.IndexOf("_masterPhase == MasterControlPhase.Hatch", System.StringComparison.Ordinal);
            Assert.Greater(hatch, 0, "a Docked end is unreachable on the finale, not unguarded");
        }
    }
}
