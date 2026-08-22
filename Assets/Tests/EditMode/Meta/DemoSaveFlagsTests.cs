using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>SaveVersion 9: the demo flags round-trip, fresh campaigns start on demo,
    /// and pre-v9 saves migrate demo-off exactly when they have already played
    /// (Docs/Demo - Single/README.md, Shared flags). The rung-2 flags were added to the v9 block
    /// without a version bump, so a v9-era file missing their keys is pinned here too
    /// (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
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
            Assert.IsFalse(m.DemoZone2BankLost);
            Assert.IsFalse(m.DemoRungTwoOpened);
            Assert.AreEqual(0, m.DemoRungTwoBuysLeft, "rung 2 has no buys until it opens");
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
            m.DemoZone2BankLost = true;
            m.DemoRungTwoOpened = true;
            m.DemoRungTwoBuysLeft = 2;
            m.SetZoneDeepest(1, 2);
            m.SetZoneDeepest(2, 3);

            string json = m.ToJson();
            Assert.IsTrue(json.Contains("\"SaveVersion\":12"), "v12 is the written schema");

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
            Assert.IsTrue(back.DemoZone2BankLost);
            Assert.IsTrue(back.DemoRungTwoOpened);
            Assert.AreEqual(2, back.DemoRungTwoBuysLeft,
                "rung 2's counter is its own — the rung-1 counter above stays at 1");
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
            // The three rung-2 flags are the exception to the played-migrates-everything
            // rule: a pre-v9 save never played zone 2's bank, so they load at their
            // defaults, not at played.
            Assert.IsFalse(m.DemoZone2BankLost, "a pre-v9 save never reached zone 2's bank");
            Assert.IsFalse(m.DemoRungTwoOpened);
            Assert.AreEqual(0, m.DemoRungTwoBuysLeft);
        }

        [Test]
        public void V9Era_MissingTheRungTwoKeys_LoadsThemAtDefaults()
        {
            // The rung-2 flags were added to the v9 demo block with no version bump. Every
            // file written before them is a v9-or-later save with those three keys absent;
            // the extractors' fallbacks are what makes that load correct, so pin it.
            var m = MetaState.FromJson(
                "{\"SaveVersion\":11,\"BankedSalvage\":400,\"CleanExits\":0,"
                + "\"DemoActive\":true,\"DemoCapsuleOpened\":true,\"DemoNavPowered\":true,"
                + "\"DemoWheelBuysLeft\":2}");
            Assert.IsFalse(m.DemoZone2BankLost);
            Assert.IsFalse(m.DemoRungTwoOpened);
            Assert.AreEqual(0, m.DemoRungTwoBuysLeft, "a missing int key defaults to 0");
            Assert.IsTrue(m.DemoActive, "the rest of the v9 block still reads as written");
            Assert.IsTrue(m.DemoCapsuleOpened);
            Assert.AreEqual(2, m.DemoWheelBuysLeft, "rung 1's counter is untouched");
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

        [Test]
        public void HatchConversions_RoundTrips()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0, m.HatchConversions, "a fresh campaign has banked nothing");
            m.HatchConversions = 3;
            var back = MetaState.FromJson(m.ToJson());
            Assert.AreEqual(3, back.HatchConversions);
        }

        [Test]
        public void PreV11_LossOrExit_MigratesTheWheelOpen()
        {
            // Pre-v11 saves ran under the loss key: PodEjections >= 1 had the wheel open,
            // and CleanExits >= 1 means the final bank converted. The re-key must not
            // re-lock either. An unplayed save stays locked.
            var lost = MetaState.FromJson("{\"SaveVersion\":10,\"PodEjections\":2}");
            Assert.AreEqual(1, lost.HatchConversions);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(lost));

            var escaped = MetaState.FromJson("{\"SaveVersion\":10,\"CleanExits\":1}");
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(escaped));

            var fresh = MetaState.FromJson("{\"SaveVersion\":10}");
            Assert.AreEqual(0, fresh.HatchConversions);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(fresh));
        }
    }
}
