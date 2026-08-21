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
            var m = MetaState.CreateNew();
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
            var m = MetaState.CreateNew();
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
            var m = MetaState.CreateNew();
            m.EarnSalvage(10000);
            Assert.IsTrue(MetaUpgrades.Buy(m, ref m.Realm.HatchArmorLevel));   // 200
            Assert.IsTrue(MetaUpgrades.Buy(m, ref m.Realm.HatchArmorLevel));   // 400
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
            var m = MetaState.CreateNew();
            m.EarnSalvage(5000);
            MetaUpgrades.Buy(m, ref m.Realm.HatchArmorLevel);
            var loaded = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.AreEqual(m.BankedSalvage, loaded.BankedSalvage);
            Assert.AreEqual(m.LifetimeSalvageEarned, loaded.LifetimeSalvageEarned);
            Assert.AreEqual(m.LifetimeSalvageSpent, loaded.LifetimeSalvageSpent);
            Assert.IsFalse(loaded.SalvageStatsWereRepaired, "a current-version save needs no repair");
        }

        [Test]
        public void ReconstructSpent_IncludesWeaponTiers()
        {
            var m = MetaState.CreateNew();
            m.HatchArmorLevel = 1; // 200
            m.SetOwned(WeaponId.Br12Shotgun, true); // 900
            m.ReplacePointsFromFlatTier(WeaponId.Br12Shotgun, 2); // 400+600 = 1000
            Assert.AreEqual(200L + 900L + 1000L, m.ReconstructSpentFromLevels());
        }

        [Test]
        public void HandEditedSave_WithABrokenLedger_IsRepaired()
        {
            // Nothing bought on this realm's own track, so its spend floor is 0 — and a
            // ledger claiming 5 earned against 1000 banked cannot be true whatever the floor.
            const string tampered =
                "{\"SaveVersion\":13,\"BankedSalvage\":1000,\"LifetimeSalvageEarned\":5," +
                "\"LifetimeSalvageSpent\":0}";
            var m = RealmState.FromJson(tampered);
            Assert.IsNotNull(m, "a current-version realm document loads");
            Assert.IsTrue(m.SalvageStatsWereRepaired);
            Assert.AreEqual(1000L, m.LifetimeSalvageEarned);
            Assert.AreEqual(0L, m.LifetimeSalvageSpent, "nothing on this realm's track to owe for");
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void SaveDataDto_CarriesTheTrioBothWays()
        {
            var m = MetaState.CreateNew();
            m.EarnSalvage(900);
            m.SpendSalvage(200);
            var back = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.AreEqual(700L, back.BankedSalvage);
            Assert.AreEqual(900L, back.LifetimeSalvageEarned);
            Assert.AreEqual(200L, back.LifetimeSalvageSpent);
        }
    }
}
