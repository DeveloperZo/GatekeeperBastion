using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// EditMode tests for the lifetime salvage trio on MetaState: Banked, LifetimeSalvageEarned,
    /// LifetimeSalvageSpent. The whole point of the trio is one invariant —
    /// Earned == Banked + Spent — so every test here is ultimately about that holding through
    /// earning, spending, a save/load round trip, and a pre-v2 save that never stored it.
    /// </summary>
    public class SalvageLedgerStatsTests
    {
        [Test]
        public void NewSave_StartsAtZero_NothingEarned()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.AreEqual(0L, m.LifetimeSalvageEarned);
            Assert.AreEqual(0L, m.LifetimeSalvageSpent);
            Assert.IsTrue(m.SalvageLedgerBalances);
            Assert.IsFalse(m.OwnsAnyWeapon);
        }

        [Test]
        public void EarnAndSpend_KeepTheLedgerBalanced()
        {
            var m = new MetaState();
            m.EarnSalvage(1000);
            Assert.IsTrue(m.SpendSalvage(250));
            Assert.AreEqual(750L, m.BankedSalvage);
            Assert.AreEqual(1000L, m.LifetimeSalvageEarned);
            Assert.AreEqual(250L, m.LifetimeSalvageSpent);
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void Earn_IgnoresNonPositive_Spend_RefusesOverdraftAndNonPositive()
        {
            var m = new MetaState();
            m.EarnSalvage(0);
            m.EarnSalvage(-50);
            Assert.AreEqual(0L, m.LifetimeSalvageEarned, "nothing was received");

            m.EarnSalvage(100);
            Assert.IsFalse(m.SpendSalvage(101), "overdraft");
            Assert.IsFalse(m.SpendSalvage(0), "a zero-cost purchase is not a transaction");
            Assert.IsFalse(m.SpendSalvage(-10));
            Assert.AreEqual(100L, m.BankedSalvage, "a refused spend must not mutate the bank");
            Assert.AreEqual(0L, m.LifetimeSalvageSpent, "nor the spent tally");
        }

        [Test]
        public void BuyingUpgrades_CreditsSpent()
        {
            var m = new MetaState();
            m.EarnSalvage(10000);
            Assert.IsTrue(MetaUpgrades.Buy(m, ref m.HatchArmorLevel));   // 200
            Assert.IsTrue(MetaUpgrades.Buy(m, ref m.HatchArmorLevel));   // 400
            // Free MK-1: own without SpendSalvage (cost 0).
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            Assert.IsTrue(m.SpendSalvage(WeaponTier.Cost(0))); // Tier 0→1 = 400
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 1);
            Assert.AreEqual(1000L, m.LifetimeSalvageSpent); // 200+400+400
            Assert.AreEqual(10000L - 1000L, m.BankedSalvage);
            Assert.AreEqual(10000L, m.LifetimeSalvageEarned);
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void RoundTrip_PreservesTheTrio()
        {
            var m = new MetaState();
            m.EarnSalvage(5000);
            MetaUpgrades.Buy(m, ref m.HatchArmorLevel);
            var loaded = MetaState.FromJson(m.ToJson());
            Assert.AreEqual(m.BankedSalvage, loaded.BankedSalvage);
            Assert.AreEqual(m.LifetimeSalvageEarned, loaded.LifetimeSalvageEarned);
            Assert.AreEqual(m.LifetimeSalvageSpent, loaded.LifetimeSalvageSpent);
            Assert.IsFalse(loaded.SalvageStatsWereRepaired, "a current-version save needs no repair");
        }

        [Test]
        public void ReconstructSpent_IncludesWeaponTiers()
        {
            var m = new MetaState();
            m.HatchArmorLevel = 1; // 200
            m.SetOwned(WeaponId.Br12Shotgun, true); // 900
            m.ReplacePointsFromFlatTier(WeaponId.Br12Shotgun, 2); // 400+600 = 1000
            Assert.AreEqual(200L + 900L + 1000L, m.ReconstructSpentFromLevels());
        }

        [Test]
        public void PreV2Save_RebuildsSpentFromLevelsAndMigratesWeapon()
        {
            // A version-1 save: banked and levels only, no lifetime fields at all.
            // Gun/Fire/Clip migrate to MK-1 Tier = min(7, max(3,3,2)) = 3 → SpentToReach = 400+600+800 = 1800.
            // Tracks: HatchArmor3=1400. Crew tracks refund: Vitality2=600, Stamina1=200, Luck1=200.
            // RepairRig2 is a deleted track: ReconstructSpentFromLevels does not count it, but
            // criterion 8 grants slot + rig free from the legacy level.
            const string v1 =
                "{\"SaveVersion\":1,\"BankedSalvage\":1739,\"TotalRuns\":16,\"CleanExits\":3," +
                "\"PodEjections\":8,\"GunDamageLevel\":3,\"FireRateLevel\":3,\"HatchArmorLevel\":3," +
                "\"RepairRigLevel\":2,\"WeaponOwned\":true,\"ClipSizeLevel\":2,\"VitalityLevel\":2," +
                "\"StaminaLevel\":1,\"EngineeringLevel\":0,\"ScavengingLevel\":0,\"LuckLevel\":1}";
            var m = MetaState.FromJson(v1);

            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(3, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.IsTrue(m.HasRepairRigEquipped, "legacy RepairRigLevel grants the equipment");
            long crewRefund = 600L + 200L + 200L;
            long expectedSpent = 1800L + 1400L; // weapon + hatch; crew refunded
            Assert.AreEqual(expectedSpent, m.LifetimeSalvageSpent);
            Assert.AreEqual(1739L + crewRefund, m.BankedSalvage);
            Assert.AreEqual(1739L + expectedSpent + crewRefund, m.LifetimeSalvageEarned);
            Assert.IsTrue(m.SalvageStatsWereRepaired);
            Assert.IsTrue(m.SalvageLedgerBalances);
            Assert.AreEqual(BoarderProtocolId.Default, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
            Assert.AreEqual(SaveData.CurrentVersion, m.SaveVersion, "loading migrates the save forward");
        }

        [Test]
        public void HandEditedSave_WithABrokenLedger_IsRepaired()
        {
            // WeaponOwned migrates to free MK-1 at T0 → reconstruct floor is 0.
            const string tampered =
                "{\"SaveVersion\":2,\"BankedSalvage\":1000,\"LifetimeSalvageEarned\":5," +
                "\"LifetimeSalvageSpent\":0,\"WeaponOwned\":true}";
            var m = MetaState.FromJson(tampered);
            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm));
            Assert.IsTrue(m.SalvageStatsWereRepaired);
            Assert.AreEqual(1000L, m.LifetimeSalvageEarned);
            Assert.AreEqual(0L, m.LifetimeSalvageSpent, "free MK-1 contributes 0 to the floor");
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void SaveDataDto_CarriesTheTrioBothWays()
        {
            var m = new MetaState();
            m.EarnSalvage(900);
            m.SpendSalvage(200);
            var back = MetaState.FromSaveData(m.ToSaveData());
            Assert.AreEqual(700L, back.BankedSalvage);
            Assert.AreEqual(900L, back.LifetimeSalvageEarned);
            Assert.AreEqual(200L, back.LifetimeSalvageSpent);
        }
    }
}
