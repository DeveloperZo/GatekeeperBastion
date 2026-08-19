using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>SaveVersion 9: the demo flags round-trip, fresh campaigns start on demo,
    /// and pre-v9 saves migrate demo-off exactly when they have already played
    /// (Docs/Demo/README.md, Shared flags).</summary>
    public class DemoSaveFlagsTests
    {
        [Test]
        public void FreshCampaign_StartsOnDemo()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(m.DemoActive);
            Assert.AreEqual(2, m.DemoWheelBuysLeft);
            Assert.IsFalse(m.DemoCapsuleOpened);
            Assert.IsFalse(m.DemoNavPowered);
            Assert.IsFalse(m.DemoArmoryPowered);
            Assert.IsFalse(m.DemoWeaponGranted);
            Assert.IsFalse(m.DemoShipFightDone);
            Assert.IsFalse(m.DemoCrewMenuUnlocked);
            Assert.IsFalse(m.DemoFabPowered);
            Assert.AreEqual(0, m.ZoneDeepest(1));
        }

        [Test]
        public void Json_RoundTrips_TheDemoFlags()
        {
            var m = MetaState.CreateNew();
            m.DemoCapsuleOpened = true;
            m.DemoNavPowered = true;
            m.DemoArmoryPowered = true;
            m.DemoWeaponGranted = true;
            m.DemoShipFightDone = true;
            m.DemoWheelBuysLeft = 1;
            m.SetZoneDeepest(1, 2);
            m.SetZoneDeepest(2, 3);

            string json = m.ToJson();
            Assert.IsTrue(json.Contains("\"SaveVersion\":10"), "v10 is the written schema");

            var back = MetaState.FromJson(json);
            Assert.IsTrue(back.DemoActive);
            Assert.IsTrue(back.DemoCapsuleOpened);
            Assert.IsTrue(back.DemoNavPowered);
            Assert.IsTrue(back.DemoArmoryPowered);
            Assert.IsTrue(back.DemoWeaponGranted);
            Assert.IsTrue(back.DemoShipFightDone);
            Assert.IsFalse(back.DemoCrewMenuUnlocked);
            Assert.IsFalse(back.DemoFabPowered);
            Assert.AreEqual(1, back.DemoWheelBuysLeft);
            Assert.AreEqual(2, back.ZoneDeepest(1));
            Assert.AreEqual(3, back.ZoneDeepest(2));
            Assert.AreEqual(0, back.ZoneDeepest(3));
        }

        [Test]
        public void PreV9_WithACleanExit_MigratesDemoOff()
        {
            var m = MetaState.FromJson(
                "{\"SaveVersion\":8,\"BankedSalvage\":500,\"CleanExits\":1,\"PodEjections\":2}");
            Assert.IsFalse(m.DemoActive, "an Escaped save must never re-run the capsule");
            Assert.IsTrue(m.DemoCapsuleOpened);
            Assert.IsTrue(m.DemoNavPowered);
            Assert.IsTrue(m.DemoArmoryPowered);
            Assert.IsTrue(m.DemoWeaponGranted);
            Assert.IsTrue(m.DemoShipFightDone);
            Assert.IsTrue(m.DemoCrewMenuUnlocked);
            Assert.IsTrue(m.DemoFabPowered);
            Assert.AreEqual(0, m.DemoWheelBuysLeft);
        }

        [Test]
        public void PreV9_DeepButNoExit_MigratesDemoOff()
        {
            var m = MetaState.FromJson(
                "{\"SaveVersion\":8,\"CleanExits\":0,\"DeepestWaveReached\":3}");
            Assert.IsFalse(m.DemoActive, "DeepestWaveReached > 1 means they have played");
        }

        [Test]
        public void PreV9_Unplayed_MigratesDemoOn()
        {
            var m = MetaState.FromJson(
                "{\"SaveVersion\":8,\"CleanExits\":0,\"DeepestWaveReached\":1}");
            Assert.IsTrue(m.DemoActive);
            Assert.AreEqual(2, m.DemoWheelBuysLeft);
            Assert.IsFalse(m.DemoCapsuleOpened);
            Assert.IsFalse(m.DemoShipFightDone);
        }

        [Test]
        public void ZoneDeepest_IsRaiseOnly_AndBounded()
        {
            var m = MetaState.CreateNew();
            m.SetZoneDeepest(1, 2);
            m.SetZoneDeepest(1, 1);
            Assert.AreEqual(2, m.ZoneDeepest(1), "a lower wave never overwrites");
            m.SetZoneDeepest(-1, 5);
            m.SetZoneDeepest(MetaState.DemoZoneCount, 5);
            Assert.AreEqual(0, m.ZoneDeepest(-1));
            Assert.AreEqual(0, m.ZoneDeepest(MetaState.DemoZoneCount));
        }

        [Test]
        public void VersionlessJson_Unplayed_MigratesDemoOn()
        {
            var m = MetaState.FromJson("{\"BankedSalvage\":100}");
            Assert.IsTrue(m.DemoActive, "missing SaveVersion is pre-v9, not a silent v9 demo-off");
            Assert.AreEqual(2, m.DemoWheelBuysLeft);
            Assert.IsFalse(m.DemoCapsuleOpened);
        }

        [Test]
        public void Json_RoundTrips_DemoActiveOff()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            var back = MetaState.FromJson(m.ToJson());
            Assert.IsFalse(back.DemoActive);
        }
    }
}
