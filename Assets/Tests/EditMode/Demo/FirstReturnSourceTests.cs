using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for M78's Game-side wiring — the first return to the Ship with
    /// salvage banked. These files are MonoBehaviours or GameSession-coupled statics the
    /// offline harness cannot run; what it can prove is that the wiring is present, ordered,
    /// and that no retired branch crept back. Behaviour is the editor's to confirm.</summary>
    public class FirstReturnSourceTests
    {
        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path);
        }

        [Test]
        public void WheelCap_RefusesBeforeSpendSalvage_AndDecrementsOnSuccess()
        {
            string src = Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            int buyStart = src.IndexOf("public static bool BuyWheelUpgrade", System.StringComparison.Ordinal);
            Assert.Greater(buyStart, -1, "the wheel command lives in the Weapons partial");
            int cap = src.IndexOf("wheel refused — demo cap", buyStart, System.StringComparison.Ordinal);
            int spend = src.IndexOf("m.SpendSalvage(price)", buyStart, System.StringComparison.Ordinal);
            Assert.Greater(cap, buyStart, "the cap refuse lives inside BuyWheelUpgrade");
            Assert.Less(cap, spend, "named cap refuse BEFORE any SpendSalvage (criterion 1)");
            StringAssert.Contains("m.DemoWheelBuysLeft--", src);
            StringAssert.Contains("demoBuysLeft=", src);
        }

        [Test]
        public void FirstBuy_UnlocksMultiplayer_OncePerSaveByConstruction()
        {
            string src = Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            StringAssert.Contains("m.DemoWheelBuysLeft == 1", src);
            StringAssert.Contains("\"multiplayer unlocked\"", src);
            StringAssert.Contains("DemoConversationIds.MpUnlocked", src);
            Assert.IsFalse(src.Contains("DemoWheelBuysLeft = 2"),
                "the count never resets per visit (beat 3, Traps)");
            Assert.IsFalse(Read("Assets/Scripts/Game/Ship/ShipCommands.cs")
                    .Contains("DemoWheelBuysLeft = 2"),
                "nor from the other command surface");
        }

        [Test]
        public void ExitStanding_IsStateKeyed_AndReevaluatedOnEveryMoneyPath()
        {
            string ship = Read("Assets/Scripts/Game/Ship/DemoShipStanding.cs");
            StringAssert.Contains("NoBuyableUpgradeRemains", ship);
            StringAssert.Contains("exit standing set — no buyable upgrades remain", ship);
            string commands = Read("Assets/Scripts/Game/Ship/ShipCommands.cs")
                + Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            int calls = 0;
            int at = 0;
            while ((at = commands.IndexOf("DemoShipStanding.Reevaluate()", at + 1,
                       System.StringComparison.Ordinal)) >= 0) calls++;
            Assert.GreaterOrEqual(calls, 5,
                "evaluated after every buy / refuse / spend (criterion 4)");
        }

        [Test]
        public void FirstDocked_PlaysTheLossLine_ThenCrew_ThenWritesTheUnlock()
        {
            string src = Read("Assets/Scripts/Game/Ship/DemoShipDirector.cs");
            int loss = src.IndexOf("DemoConversationIds.FirstHatchDown", System.StringComparison.Ordinal);
            int crew = src.IndexOf("DemoConversationIds.CrewReady", System.StringComparison.Ordinal);
            int flag = src.IndexOf("DemoCrewMenuUnlocked = true", System.StringComparison.Ordinal);
            Assert.Greater(loss, -1);
            Assert.Greater(crew, loss, "the first-loss line wins the frame; CREW queues");
            Assert.Greater(flag, crew, "the flag write is what makes both once-per-save");
            StringAssert.Contains("HatchConversions >= 1", src);
            StringAssert.Contains("\"CREW menu unlocked\"", src);
        }

        [Test]
        public void CrewPanel_FoldsTheHealIn_AndNeverAutoEquips()
        {
            string src = Read("Assets/Scripts/Game/Ship/CrewQuartersStation.cs");
            StringAssert.Contains("DemoStationGate.HealOnVisit", src);
            StringAssert.Contains("DemoConversationIds.CrewClaim", src);
            Assert.IsFalse(src.Contains("TryClaim"),
                "the claim is the walk — BuyProtocol only ever fires from the sheet's BUY");
        }

        [Test]
        public void Nav_IsPlainEmbark_WhileTheDemoRuns()
        {
            string src = Read("Assets/Scripts/Game/Ship/MapStation.cs");
            StringAssert.Contains("_startWave = 1", src);
            StringAssert.Contains("DemoConversationIds.EmbarkWave2", src);
            StringAssert.Contains("ShowStartWavePicker(false)", src);
            StringAssert.Contains("lead && !counting && !m.DemoActive", src,
                "the party lead's picker is suppressed too");
        }

        [Test]
        public void Bastion_LogsThePlainEmbark()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.cs");
            StringAssert.Contains("\"embark plain\"", src);
            StringAssert.Contains("RecordRetriggerClear()", src);
            StringAssert.Contains("ClearPendingRetrigger()", src);
        }

        [Test]
        public void Retrigger_LogsItsElection_AndCompletesTheZone()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.Retrigger.cs");
            StringAssert.Contains("\"zone retrigger zone=\"", src);
            StringAssert.Contains("mode=", src);
            StringAssert.Contains("TriggerCompositeDraw", src);
            StringAssert.Contains("\"zone terminals complete zone=\"", src);
            StringAssert.Contains("DemoConversationIds.ZoneRetrigger", src);
            StringAssert.Contains("SetZoneDeepest", src);
        }

        [Test]
        public void FieldClear_WritesThePerZoneDeepest()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.Terminals.cs");
            int clear = src.IndexOf("void RecordPendingTerminalClear", System.StringComparison.Ordinal);
            int write = src.IndexOf("SetZoneDeepest", clear, System.StringComparison.Ordinal);
            Assert.Greater(write, clear,
                "DemoZoneDeepestWave writes on field-Wave CLEAR, never on bank start");
            StringAssert.Contains("\"zone terminals complete zone=\"", src);
        }

        [Test]
        public void BankRefuse_NamesTheZone_NotTheTerminal()
        {
            string hatch = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            StringAssert.Contains("\"bank refused — zone incomplete\"", hatch);
            Assert.IsFalse(hatch.Contains("finish the terminal"), "superseded refuse copy");
            StringAssert.Contains("\"bank window survived zone=\"", hatch);
            string repair = Read("Assets/Scripts/Game/Bastion/RepairInteractable.cs");
            StringAssert.Contains("Hatch-bank — finish the zone", repair);
        }

        [Test]
        public void GuestSeat_CountsItsOwnHatchConversion()
        {
            string src = Read("Assets/Scripts/Game/Net/NetPlayerAvatar.cs");
            int credit = src.IndexOf("void CreditBankedClientRpc", System.StringComparison.Ordinal);
            int count = src.IndexOf("HatchConversions", credit, System.StringComparison.Ordinal);
            Assert.Greater(count, credit, "a guest's unlock rides their own credited convert");
        }

        [Test]
        public void ArmoryWheel_SpeaksTheCapLine_OncePerVisit()
        {
            string src = Read("Assets/Scripts/Game/Ship/ArmoryStation.cs");
            StringAssert.Contains("DemoConversationIds.WheelCap", src);
            StringAssert.Contains("_capLinePlayed", src);
            StringAssert.Contains("two purchases this demo — NAV is ready", src);
            Assert.IsFalse(src.Contains("survive a death first"),
                "the wheel lock copy follows the re-keyed gate");
        }
    }
}
