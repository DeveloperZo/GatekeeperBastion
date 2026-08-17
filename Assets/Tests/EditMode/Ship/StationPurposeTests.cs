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
    }
}
