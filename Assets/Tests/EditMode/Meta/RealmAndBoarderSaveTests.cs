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
            // Played() grants hatch armour without spending for it, so the load clamps the
            // ledger up to the floor that armour proves. Assert it rather than let a real
            // ledger bug hide inside an unexamined round trip.
            Assert.AreEqual(600L, back.LifetimeSalvageSpent, "raised to the armour floor");
            Assert.AreEqual(1500L, back.LifetimeSalvageEarned);
            Assert.IsTrue(back.SalvageLedgerBalances);

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

            // The load-bearing assertion. If the kit term ever creeps back into the realm's
            // floor, THIS is what moves: the floor is what the repair clamps the ledger up to,
            // so a non-zero floor here is exactly the inflation the split exists to prevent.
            // The ledger assertions below cannot catch it on their own, because the repair has
            // already run inside FromJson before the Boarder is anywhere near this session.
            Assert.AreEqual(0L, session.ReconstructSpentFromLevels(),
                "a realm that has sold nothing owes nothing, whatever the visitor is carrying");
            Assert.Greater(boarder.ReconstructSpentOnKit(), 0L,
                "the kit did cost something; it is simply not this realm's debt");

            Assert.AreEqual(0L, session.LifetimeSalvageEarned,
                "a fresh realm has earned nothing, however well equipped the visitor is");
            Assert.AreEqual(0L, session.LifetimeSalvageSpent);
            Assert.AreEqual(0L, session.BankedSalvage);
            Assert.IsTrue(session.SalvageLedgerBalances);
            Assert.IsFalse(fresh.SalvageStatsWereRepaired,
                "nothing to repair — the numbers were consistent to begin with");
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
        public void ADocumentCarryingNoVersionAtAllIsRefused()
        {
            // A missing key must not read as "current". Both loaders fall back to 0 for the
            // version precisely so an unversioned file cannot impersonate one this build wrote.
            Assert.IsNull(BoarderState.FromJson("{\"EquippedPrimary\":2}"));
            Assert.IsNull(RealmState.FromJson("{\"BankedSalvage\":5000}"));
            Assert.IsFalse(SaveFormat.IsSupported(0));
            StringAssert.Contains("no readable version", SaveFormat.RefusalFor(0));
        }

        [Test]
        public void ADocumentFromANewerBuildIsRefused_AndSaysSo()
        {
            int newer = SaveFormat.CurrentVersion + 1;
            Assert.IsNull(RealmState.FromJson("{\"SaveVersion\":" + newer + "}"));
            Assert.IsFalse(SaveFormat.IsSupported(newer));
            StringAssert.Contains("newer build", SaveFormat.RefusalFor(newer),
                "a save from the future is a different problem than a save from the past");
        }

        [Test]
        public void ABoarderMissingEveryOptionalKeyLoadsCarryingNothing()
        {
            var b = BoarderState.FromJson("{\"SaveVersion\":" + SaveFormat.CurrentVersion + "}");

            Assert.IsNotNull(b);
            Assert.IsFalse(b.OwnsAnyWeapon);
            Assert.AreEqual(WeaponId.None, b.EquippedPrimary);
            Assert.AreEqual(WeaponId.None, b.EquippedSecondary);
            Assert.IsFalse(b.EquipmentSlotPurchased);
            Assert.AreEqual(EquipmentId.None, b.EquippedEquipment);
            Assert.AreEqual(PayloadId.None, b.EquippedDrone);
            Assert.AreEqual(0, b.OwnedPayloadMask);
            Assert.AreEqual(BoarderProtocolId.None, b.EquippedProtocol);
        }

        [Test]
        public void ATamperedLedgerIsRaisedToWhatTheRealmsOwnTrackProves()
        {
            // The clamp is what makes a hand-edited ledger safe, and it is the realm's OWN
            // floor doing it. Hatch armour 3 costs 200 * (2^3 - 1); a file claiming nothing
            // was ever spent cannot be true while that armour is on the hatch.
            var r = RealmState.FromJson(
                "{\"SaveVersion\":" + SaveFormat.CurrentVersion + ",\"BankedSalvage\":100,"
                + "\"LifetimeSalvageEarned\":100,\"LifetimeSalvageSpent\":0,"
                + "\"HatchArmorLevel\":3}");

            Assert.IsNotNull(r);
            Assert.AreEqual(1400L, r.ReconstructSpentFromLevels(), "the realm's own floor");
            Assert.AreEqual(1400L, r.LifetimeSalvageSpent, "spent was raised to the floor");
            Assert.AreEqual(1500L, r.LifetimeSalvageEarned, "and earned follows so the trio adds up");
            Assert.IsTrue(r.SalvageLedgerBalances);
            Assert.IsTrue(r.SalvageStatsWereRepaired, "and the repair is flagged, not silent");
        }

        [Test]
        public void AnEquippedIdThisBuildDoesNotCarry_DoesNotSurviveALoad()
        {
            // Not a migration rule. A hand-edited document, or one from a build that sold more
            // than this one does, can put any integer in these fields at the CURRENT version,
            // and an uncatalogued id reaches a switch with no arm for it.
            var b = BoarderState.FromJson(
                "{\"SaveVersion\":" + SaveFormat.CurrentVersion + ",\"EquippedDrone\":99,"
                + "\"EquippedEquipment\":98,\"EquippedPrimary\":97,\"EquippedSecondary\":96}");

            Assert.IsNotNull(b);
            Assert.AreEqual(PayloadId.None, b.EquippedDrone, "an unknown payload leaves the bay");
            Assert.AreEqual(EquipmentId.None, b.EquippedEquipment);
            Assert.AreEqual(WeaponId.None, b.EquippedPrimary);
            Assert.AreEqual(WeaponId.None, b.EquippedSecondary);
        }

        [Test]
        public void AWeaponEquippedIntoTheWrongSlot_DoesNotSurviveALoad()
        {
            // Br12Shotgun is a Primary. A document claiming it as the Secondary is malformed,
            // and honouring it would put a Primary in a slot the wheel and the HUD do not
            // expect to find one in.
            var b = BoarderState.FromJson(
                "{\"SaveVersion\":" + SaveFormat.CurrentVersion
                + ",\"EquippedSecondary\":" + (int)WeaponId.Br12Shotgun + "}");

            Assert.IsNotNull(b);
            Assert.AreEqual(WeaponId.None, b.EquippedSecondary);
        }

        [Test]
        public void TheVersionLiteralsInThisSuite_TrackTheSchema()
        {
            // Several tests here and elsewhere spell the current version into a JSON literal,
            // because a document has to carry SOME version to be loadable at all. When the
            // schema bumps, grep the EditMode suite for the old number and bump it — a stale
            // literal gets null back and then fails as a NullReferenceException, which reads
            // like a crash rather than like the version mismatch it is.
            Assert.AreEqual(13, SaveFormat.CurrentVersion,
                "schema bumped — update the version literals across the EditMode suite");
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
