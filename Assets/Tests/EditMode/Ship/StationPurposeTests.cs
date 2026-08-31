using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Station purpose and deal copy match the language table and The-Ship Gaps this
    /// slice closed. Empty purpose fails.</summary>
    public class StationPurposeTests
    {
        [Test]
        public void PurposeLines_MatchLanguageTable_AndAreNonEmpty()
        {
            Assert.AreEqual("the weapon in your hands", ArmoryStation.PurposeLine);
            Assert.AreEqual("you", CrewQuartersStation.PurposeLine);
            Assert.AreEqual("the Hatch and the Ship's gear", EngineeringStation.PurposeLine);
            Assert.AreEqual("where you go, and going there", MapStation.PurposeLine);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ArmoryStation.PurposeLine));
            Assert.IsFalse(string.IsNullOrWhiteSpace(CrewQuartersStation.PurposeLine));
            Assert.IsFalse(string.IsNullOrWhiteSpace(EngineeringStation.PurposeLine));
            Assert.IsFalse(string.IsNullOrWhiteSpace(MapStation.PurposeLine));
        }

        [Test]
        public void DealAndFooter_StateTransferRigNoSellBackAndMovementLock()
        {
            Assert.AreEqual("Upgrades stay on this weapon.", ArmoryStation.TransferLine);
            string rig = EngineeringStation.RigDealLine();
            Assert.IsTrue(rig.IndexOf("Repair Rig", System.StringComparison.Ordinal) >= 0);
            Assert.IsTrue(rig.IndexOf(
                ShipSystems.RepairSalvagePerHp.ToString("0"),
                System.StringComparison.Ordinal) >= 0);
            Assert.AreEqual("No sell-back.", StationPanelView.NoSellBack);
            Assert.AreEqual("Opening a console locks movement.", StationPanelView.MovementLock);
        }

        [Test]
        public void NavMapStartWave_NamesCleared()
        {
            string line = MapStation.StartWaveLine(2, 4);
            Assert.IsTrue(line.IndexOf("cleared", System.StringComparison.Ordinal) >= 0);
            Assert.IsTrue(line.IndexOf("2", System.StringComparison.Ordinal) >= 0);
            Assert.IsTrue(line.IndexOf("4", System.StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void ProfileRank_IsNotLv1()
        {
            Assert.AreEqual("BOARDER", BastionHudView.ProfileRank);
            Assert.AreEqual(BastionHudView.ProfileRank, ShipHudView.ProfileRank);
            Assert.AreNotEqual("LV 1", BastionHudView.ProfileRank);
        }

        [Test]
        public void CrewQuarters_ComingSoonCopy_IsNotACatalogId()
        {
            // VEIL left this list by entering the catalog; AEGIS and LEDGER are still copy.
            Assert.AreEqual("AEGIS", CrewQuartersStation.StubAegis);
            Assert.AreEqual("LEDGER", CrewQuartersStation.StubLedger);
            Assert.AreEqual("Coming Soon", CrewQuartersStation.ComingSoon);
            Assert.AreEqual("Coming Soon — no sheet yet", CrewQuartersStation.StubSheet);
            Assert.AreEqual("SELECT", CrewQuartersStation.SelectLabel);
            Assert.AreEqual("EQUIP", CrewQuartersStation.EquipLabel);
            Assert.AreEqual("EQUIPPED", CrewQuartersStation.EquippedLabel);
            Assert.AreEqual("BACK", ProtocolSheetView.BackLabel);
            Assert.AreEqual("Protocol Title", ProtocolSheetView.TitleHeading);
            Assert.AreEqual("Protocol Description", ProtocolSheetView.DescriptionHeading);
            Assert.AreEqual("Crew Ability", ProtocolSheetView.CrewHeading);
            Assert.AreEqual("Personal Ability", ProtocolSheetView.PersonalHeading);
            Assert.AreEqual("Passive Ability", ProtocolSheetView.PassiveHeading);
            Assert.AreEqual(BoarderProtocol.CrewSheetDefault, BoarderProtocol.CrewSheet(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderProtocol.PersonalSheetDefault, BoarderProtocol.PersonalSheet(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderProtocol.PassiveSheetDefault, BoarderProtocol.PassiveSheet(BoarderProtocolId.Depot));
            // The stub copy must never name a protocol the catalog sells. AEGIS and LEDGER
            // are the two left; VEIL became a catalog id and its row is a real sheet now.
            BoarderProtocolId aegis;
            Assert.IsFalse(System.Enum.TryParse("AEGIS", true, out aegis));
            Assert.AreEqual(BoarderProtocolId.Depot, BoarderProtocol.ShopProtocols()[0]);
            Assert.AreEqual(3, BoarderProtocol.ShopProtocols().Length);
        }
    }
}
