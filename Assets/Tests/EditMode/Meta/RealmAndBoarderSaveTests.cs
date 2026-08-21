using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The save is two documents: a Boarder that travels with the player and a realm
    /// that stays with the world. These pin which column a fact belongs to, that two realms
    /// cannot reach into each other, and that a document this build cannot honestly read is
    /// refused rather than reshaped (Docs/Milestones — the realm and the Boarder).</summary>
    public class RealmAndBoarderSaveTests
    {
        static BoarderState WellEquipped()
        {
            var b = BoarderState.CreateNew("b1", "VESSEL");
            b.SetOwned(WeaponId.Mk1Sidearm, true);
            b.SetOwned(WeaponId.Br12Shotgun, true);
            b.SetEquipped(WeaponId.Br12Shotgun);
            b.SetEquipped(WeaponId.Mk1Sidearm);
            b.AwardWheel(WeaponId.Br12Shotgun, WheelAttr.Damage);
            b.AwardWheel(WeaponId.Br12Shotgun, WheelAttr.Clip);
            b.EquippedProtocol = BoarderProtocolId.Default;
            b.EquipmentSlotPurchased = true;
            b.SetEquipmentOwned(EquipmentId.RepairRig, true);
            b.SetEquippedEquipment(EquipmentId.RepairRig);
            return b;
        }

        static RealmState Played(string id, string name)
        {
            var r = RealmState.CreateNew(id, name);
            r.EarnSalvage(900L);
            r.HatchConversions = 2;
            r.DeepestWaveReached = 4;
            r.CleanExits = 1;
            r.HatchArmorLevel = 2;
            r.SetCompletedTerminalIds(new[] { "T1", "T2" });
            r.SetOpenDoorIds(new[] { "D1" });
            r.SetZoneDeepest(1, 3);
            return r;
        }

        [Test]
        public void TheBoarderDocument_CarriesTheKit_AndNothingOfTheWorld()
        {
            var back = BoarderState.FromJson(WellEquipped().ToJson());

            Assert.IsNotNull(back);
            Assert.IsTrue(back.Owns(WeaponId.Mk1Sidearm));
            Assert.IsTrue(back.Owns(WeaponId.Br12Shotgun));
            Assert.AreEqual(WeaponId.Br12Shotgun, back.EquippedPrimary);
            Assert.AreEqual(WeaponId.Mk1Sidearm, back.EquippedSecondary);
            Assert.AreEqual(BoarderProtocolId.Default, back.EquippedProtocol);
            Assert.IsTrue(back.EquipmentSlotPurchased);
            Assert.IsTrue(back.OwnsEquipment(EquipmentId.RepairRig));
            Assert.Greater(WeaponWheel.TotalPoints(back.PointsOf(WeaponId.Br12Shotgun)), 0,
                "focus-ring points travel with the weapon they were spent on");

            string json = WellEquipped().ToJson();
            Assert.IsFalse(json.Contains("BankedSalvage"), "the pot is the world's");
            Assert.IsFalse(json.Contains("OpenDoorIds"), "doors are the world's");
            Assert.IsFalse(json.Contains("DemoActive"), "the first Bastion is a world");
        }

        [Test]
        public void TheRealmDocument_CarriesTheWorld_AndNothingOfTheKit()
        {
            var back = RealmState.FromJson(Played("r1", "ALPHA").ToJson());

            Assert.IsNotNull(back);
            Assert.AreEqual("ALPHA", back.Name);
            Assert.AreEqual(900L, back.BankedSalvage);
            Assert.AreEqual(2, back.HatchConversions);
            Assert.AreEqual(4, back.DeepestWaveReached);
            Assert.AreEqual(2, back.HatchArmorLevel);
            Assert.AreEqual(2, back.CompletedTerminalIds.Length);
            Assert.AreEqual(1, back.OpenDoorIds.Length);
            Assert.AreEqual(3, back.ZoneDeepest(1));

            string json = Played("r1", "ALPHA").ToJson();
            Assert.IsFalse(json.Contains("WeaponsOwned"), "weapons are the Boarder's");
            Assert.IsFalse(json.Contains("EquippedProtocol"), "the protocol is the Boarder's");
        }

        [Test]
        public void ABoarderCarriesItsKitIntoARealmThatHasNeverSeenIt()
        {
            var boarder = BoarderState.FromJson(WellEquipped().ToJson());
            var fresh = RealmState.CreateNew("r2", "BETA");
            var session = MetaState.Compose(boarder, fresh);

            Assert.IsTrue(session.Owns(WeaponId.Br12Shotgun), "the weapon came with the player");
            Assert.AreEqual(WeaponId.Br12Shotgun, session.EquippedPrimary);
            Assert.AreEqual(BoarderProtocolId.Default, session.EquippedProtocol);
            Assert.Greater(WeaponWheel.TotalPoints(session.PointsOf(WeaponId.Br12Shotgun)), 0);
        }

        [Test]
        public void AFreshRealmStartsWithNoneOfTheWorldsProgress()
        {
            var fresh = RealmState.CreateNew("r3", "GAMMA");

            Assert.AreEqual(0L, fresh.BankedSalvage, "the starting pot, not a carried balance");
            Assert.AreEqual(0, fresh.CompletedTerminalIds.Length, "no bastion cleared");
            Assert.AreEqual(0, fresh.OpenDoorIds.Length, "no door unlocked");
            Assert.AreEqual(0, fresh.HatchArmorLevel, "no ship upgrade");
            Assert.AreEqual(0, fresh.HatchConversions);
            Assert.AreEqual(1, fresh.DeepestWaveReached);
            Assert.AreEqual(0, fresh.CleanExits);
        }

        [Test]
        public void TwoRealmsDoNotReachIntoEachOther()
        {
            var alpha = Played("r1", "ALPHA");
            var beta = RealmState.CreateNew("r2", "BETA");
            var boarder = WellEquipped();

            // The same Boarder plays both. Only the realm it is standing in may change.
            MetaState.Compose(boarder, alpha).EarnSalvage(100L);
            MetaState.Compose(boarder, beta).EarnSalvage(7L);

            Assert.AreEqual(1000L, alpha.BankedSalvage);
            Assert.AreEqual(7L, beta.BankedSalvage);
            Assert.AreEqual(0, beta.CompletedTerminalIds.Length, "alpha's clears stayed in alpha");
            Assert.AreEqual(0, beta.OpenDoorIds.Length);
            Assert.AreEqual(0, beta.HatchArmorLevel);
            Assert.AreEqual(1, beta.DeepestWaveReached);
        }

        [Test]
        public void AWellEquippedBoarderDoesNotInflateAFreshRealmsLedger()
        {
            // The regression the split exists to prevent. The old spend floor was rebuilt from
            // weapons owned and derived Tiers, which are the Boarder's — so carrying kit into a
            // new world would have raised that world's floor, and the ledger would have claimed
            // salvage the realm never earned. The floor now counts only the realm's own track.
            var boarder = WellEquipped();
            var fresh = RealmState.FromJson(RealmState.CreateNew("r4", "DELTA").ToJson());
            var session = MetaState.Compose(boarder, fresh);

            Assert.IsNotNull(fresh);
            Assert.AreEqual(0L, session.LifetimeSalvageEarned,
                "a fresh realm has earned nothing, however well equipped the visitor is");
            Assert.AreEqual(0L, session.LifetimeSalvageSpent);
            Assert.AreEqual(0L, session.BankedSalvage);
            Assert.IsTrue(session.SalvageLedgerBalances);
            Assert.IsFalse(fresh.SalvageStatsWereRepaired,
                "nothing to repair — the numbers were consistent to begin with");
            Assert.Greater(boarder.ReconstructSpentOnKit(), 0L,
                "the kit did cost something; it is simply not this realm's debt");
        }

        [Test]
        public void ADocumentFromBeforeTheSplitIsRefused_NotReshaped()
        {
            const string oldBoarder = "{\"SaveVersion\":11,\"EquippedPrimary\":2}";
            const string oldRealm = "{\"SaveVersion\":11,\"BankedSalvage\":5000}";

            Assert.IsNull(BoarderState.FromJson(oldBoarder), "an older Boarder document is refused");
            Assert.IsNull(RealmState.FromJson(oldRealm), "an older realm document is refused");

            string why = SaveFormat.RefusalFor(11);
            Assert.IsNotEmpty(why);
            StringAssert.Contains("11", why, "the refusal names the version it found");
            StringAssert.Contains(SaveFormat.CurrentVersion.ToString(), why,
                "and the version it wanted");
            Assert.IsFalse(SaveFormat.IsSupported(11));
            Assert.IsTrue(SaveFormat.IsSupported(SaveFormat.CurrentVersion));
        }

        [Test]
        public void ARealmNameSurvivesQuotesAndBackslashes()
        {
            // Player-typed text reaches the serializer. An unescaped quote would truncate the
            // document and take the rest of the world's progress with it.
            var r = RealmState.CreateNew("r5", "the \"deep\" hold\\wing");
            var back = RealmState.FromJson(r.ToJson());

            Assert.IsNotNull(back);
            Assert.AreEqual("the \"deep\" hold\\wing", back.Name);
        }
    }
}
