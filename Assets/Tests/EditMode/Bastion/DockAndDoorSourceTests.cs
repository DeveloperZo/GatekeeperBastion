using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for the Game-side half of the Docked return. These files are
    /// MonoBehaviours, so the offline harness cannot run them; what it can do is prove the
    /// retired branch is gone and the new wiring is present. Behaviour is still the editor's
    /// to confirm — a pin that reads the source is not a pin that ran the game.</summary>
    public class DockAndDoorSourceTests
    {
        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path);
        }

        [Test]
        public void CompletedTerminal_NoLongerOffersToOpenADoor()
        {
            string src = Read("Assets/Scripts/Game/Bastion/BastionTerminal.cs");
            Assert.IsFalse(src.Contains("\"] Open door\""), "the Open door prompt is retired");
            Assert.IsFalse(src.Contains("unlocksDoor.SetOpen("),
                "a terminal may not open a door — the layout owns that now");
            Assert.IsFalse(src.Contains("opened \" + unlocksDoor.DoorId"),
                "and its log line goes with it");
        }

        [Test]
        public void OnlyTheLayoutOpensADoorAtRuntime()
        {
            string src = Read("Assets/Scripts/Game/Bastion/BastionLayout.cs");
            StringAssert.Contains("OpenEligibleDoors", src);
            StringAssert.Contains("RestoreOpenDoors", src);
            StringAssert.Contains("CopyOpenDoorIds", src);
        }

        [Test]
        public void ADoorOpening_LogsItsCause_AndSnapshotsIt()
        {
            // One helper, two causes (Zo, 2026-08-25): the terminal shutoff that completes a
            // zone, and a survived Convert. They shared nothing before, and when the Convert
            // left the demo it took the game's ONLY door-opening path with it.
            string shared = Read("Assets/Scripts/Game/Bastion/GameController.Terminals.cs");
            StringAssert.Contains("void OpenDoorsFor(string cause)", shared);
            StringAssert.Contains("\"door open \"", shared);
            StringAssert.Contains("Meta.SetOpenDoorIds(", shared);
            StringAssert.Contains("OpenDoorsFor(\"shutoff\")", shared);

            string bank = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            StringAssert.Contains("OnBankWindowSurvived", bank);
            StringAssert.Contains("OpenDoorsFor(\"bank\")", bank);
        }

        [Test]
        public void DoorLine_ThenDockOffer_InThatOrder()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            // The door line moved into OpenDoorsFor with the rest of the opening; the order
            // that matters here is still that the dock offer comes after it, and the call
            // that raises it is what precedes DockOption now.
            int door = src.IndexOf("OpenDoorsFor(", System.StringComparison.Ordinal);
            int dock = src.IndexOf("DemoConversationIds.DockOption", System.StringComparison.Ordinal);
            Assert.Greater(door, -1, "the door line must be raised on survival");
            Assert.Greater(dock, door, "the dock offer follows the door line, never precedes it");
            StringAssert.Contains("DoorLineFor(opened[0])",
                Read("Assets/Scripts/Game/Bastion/GameController.Terminals.cs"));
            // Both are Subtitles since 2026-08-25: every line that fires mid-run leaves
            // move/look/fire live (playtest: Lock lines "made the game feel frozen").
            StringAssert.Contains("DemoVoiceMode.Subtitle", src);
            Assert.IsFalse(src.Contains("DemoVoiceMode.Lock"),
                "nothing raised during a live run may lock input");
        }

        [Test]
        public void Embark_RestoresOpenDoors_AndSaysSo()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.cs");
            StringAssert.Contains("_layout.RestoreOpenDoors(", src);
            StringAssert.Contains("\"snapshot restored doors=\"", src);
            StringAssert.Contains("Sim.BankWindowSurvived += OnBankWindowSurvived", src);
            StringAssert.Contains("Sim.BankWindowSurvived -= OnBankWindowSurvived", src);
        }

        [Test]
        public void TheHatchOffersTheDockHold()
        {
            string src = Read("Assets/Scripts/Game/Bastion/RepairInteractable.cs");
            StringAssert.Contains("] Dock — return to Ship", src);
            StringAssert.Contains("gc.Sim.DockRun()", src);
            StringAssert.Contains("gc.Sim.DockOffered", src);
            StringAssert.Contains("IsClientOnly(gc)", src);
        }

        [Test]
        public void TheDockedEndScreenNeverBorrowsTheLoseFraming()
        {
            string src = Read("Assets/Scripts/Game/Bastion/HudController.EndScreens.cs");
            StringAssert.Contains("PushDocked", src);
            StringAssert.Contains("DockedFraming", src);
            int docked = src.IndexOf("void PushDocked", System.StringComparison.Ordinal);
            int nextMethod = src.IndexOf("void PushSuccess", System.StringComparison.Ordinal);
            Assert.Greater(nextMethod, docked);
            string body = src.Substring(docked, nextMethod - docked);
            Assert.IsFalse(body.Contains("LoseFraming"),
                "a Docked end is a choice; it may never wear the lose framing");
        }
    }
}
