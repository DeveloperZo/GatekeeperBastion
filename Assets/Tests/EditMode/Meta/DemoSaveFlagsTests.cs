using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The demo flags round-trip and a fresh campaign starts on demo
    /// (Docs/Demo - Single/README.md, Shared flags). The flags ride the REALM: the demo is the
    /// first Bastion, and a Bastion is a world. A key may still be added to that document
    /// without a version bump when its absent value is also its correct value, which is why a
    /// realm missing the rung-2 keys is pinned here
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

            string json = m.ToSnapshotJson();
            Assert.IsTrue(
                json.Contains("\"SaveVersion\":" + SaveFormat.CurrentVersion),
                "the snapshot carries the schema this build writes");

            var back = MetaState.FromSnapshotJson(json);
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
        public void RealmMissingTheRungTwoKeys_LoadsThemAtDefaults()
        {
            // A key may be added to the realm document without a version bump WHEN its absent
            // value is also its correct value. That is the whole licence for doing so, and the
            // extractors' fallbacks are what make it true, so pin it against a current document
            // rather than against a version nothing loads any more.
            var m = RealmState.FromJson(
                "{\"SaveVersion\":13,\"BankedSalvage\":400,\"CleanExits\":0,"
                + "\"DemoActive\":true,\"DemoCapsuleOpened\":true,\"DemoNavPowered\":true,"
                + "\"DemoWheelBuysLeft\":2}");
            Assert.IsNotNull(m, "a current-version realm document loads");
            Assert.IsFalse(m.DemoZone2BankLost);
            Assert.IsFalse(m.DemoRungTwoOpened);
            Assert.AreEqual(0, m.DemoRungTwoBuysLeft, "a missing int key defaults to 0");
            Assert.IsTrue(m.DemoActive, "the rest of the v9 block still reads as written");
            Assert.IsTrue(m.DemoCapsuleOpened);
            Assert.AreEqual(2, m.DemoWheelBuysLeft, "rung 1's counter is untouched");
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
        public void Json_RoundTrips_DemoActiveOff()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            var back = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.IsFalse(back.DemoActive);
        }

        [Test]
        public void HatchConversions_RoundTrips()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0, m.HatchConversions, "a fresh campaign has banked nothing");
            m.HatchConversions = 3;
            var back = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.AreEqual(3, back.HatchConversions);
        }

    }
}
