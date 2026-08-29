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
            // DECOUPLED FROM THE LOG'S WORDING (2026-08-28). This pinned the literal text
            // "hatch finale send" and broke the moment that line was renamed to describe what
            // the beat actually is now — the elite on the control-room floor, not a send to the
            // Hatch. The CLAIM was never about the words: it is that an always-on Log.Event
            // precedes the voice line, so a muted or gated subtitle still leaves evidence in
            // the log that the beat fired. Anchored on the handler and the call, not the prose.
            string src = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            int handler = src.IndexOf("void OnMasterHatchHalfStarted()", System.StringComparison.Ordinal);
            Assert.Greater(handler, 0, "the finale's second-half handler is gone");
            int send = src.IndexOf("Log.Event(", handler, System.StringComparison.Ordinal);
            int line = src.IndexOf("DemoConversationIds.HatchFinale", handler, System.StringComparison.Ordinal);
            Assert.Greater(send, handler, "the handler logs nothing, so a gated subtitle would "
                + "leave no evidence the beat ever fired");
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

        /// <summary>Renamed 2026-08-28 from <c>DockOffer_IsRefusedOnTheFinale</c>. The guard
        /// used to read <c>_masterPhase == MasterControlPhase.Hatch</c> — the finale's last
        /// window and nothing else — because that was the only Master beat that could offer a
        /// dock. The finale has no halves now, so the guard widened to
        /// <see cref="GatekeepersProtocol.Sim.RunSim.MasterSequenceLive"/> and has to hold at
        /// BOTH offer sites: the bank window's and the zone-shutoff one added the same day.
        /// Walking home mid-finale must not be a way to dodge the elite.</summary>
        [Test]
        public void DockOffer_IsRefusedForTheWholeMasterSequence()
        {
            string src = Read("Assets/Scripts/Sim/Run/RunSim.Dock.cs");
            Assert.IsFalse(src.Contains("MasterControlPhase.Hatch"),
                "the retired Hatch phase is still named in the dock guard");

            // COUNT THEM. One guard passing is what the narrow old pin proved, and a second
            // offer site is exactly how the hole would reopen.
            int guards = 0;
            for (int i = src.IndexOf("if (MasterSequenceLive) return", System.StringComparison.Ordinal);
                 i >= 0;
                 i = src.IndexOf("if (MasterSequenceLive) return", i + 1, System.StringComparison.Ordinal))
                guards++;
            Assert.AreEqual(2, guards,
                "RunSim.Dock.cs has an offer site that does not refuse a live Master sequence");

            // And each guard sits INSIDE an offer method, ahead of the write it protects.
            foreach (string offer in new[] { "OfferDockIfBankSurvived", "OfferDockAfterZoneShutoff" })
            {
                int start = src.IndexOf("void " + offer, System.StringComparison.Ordinal);
                Assert.Greater(start, 0, offer + " is gone");
                int guard = src.IndexOf("if (MasterSequenceLive) return", start, System.StringComparison.Ordinal);
                int write = src.IndexOf("DockOffered = true", start, System.StringComparison.Ordinal);
                Assert.Greater(guard, start, offer + " does not refuse a live Master sequence");
                Assert.Greater(write, guard, offer + " offers the dock before it checks");
            }
        }
    }
}
