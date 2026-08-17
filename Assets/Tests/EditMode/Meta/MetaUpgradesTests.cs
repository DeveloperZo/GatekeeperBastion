using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>EditMode tests for the ship-shop: the exact doubling cost curve, purchase
    /// rules, the pinned attribute/fab effect formulas, and persistence of upgrade levels.</summary>
    public class MetaUpgradesTests
    {
        [Test]
        public void NextCost_ExactDoublingCurve()
        {
            long[] expected = { 200L, 400L, 800L, 1600L, 3200L };
            for (int level = 0; level < MetaUpgrades.MaxLevel; level++)
                Assert.AreEqual(expected[level], MetaUpgrades.NextCost(level), "level " + level);
        }

        [Test]
        public void NextCost_OutOfRange_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MetaUpgrades.NextCost(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MetaUpgrades.NextCost(MetaUpgrades.MaxLevel));
        }

        [Test]
        public void CanBuy_TrueOnlyWhenAffordableAndBelowMax()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 199;
            Assert.IsFalse(MetaUpgrades.CanBuy(meta, 0), "one short of the first level");
            meta.BankedSalvage = 200;
            Assert.IsTrue(MetaUpgrades.CanBuy(meta, 0), "exactly affordable");
            meta.BankedSalvage = 1000000;
            Assert.IsFalse(MetaUpgrades.CanBuy(meta, MetaUpgrades.MaxLevel), "maxed track");
        }

        [Test]
        public void Buy_DeductsBankedAndIncrementsLevel()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 1000;

            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel));
            Assert.AreEqual(1, meta.HatchArmorLevel);
            Assert.AreEqual(800L, meta.BankedSalvage); // -200

            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel));
            Assert.AreEqual(2, meta.HatchArmorLevel);
            Assert.AreEqual(400L, meta.BankedSalvage); // -400
        }

        [Test]
        public void Buy_FailsWhenPoor_NoMutation()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 399;
            meta.HatchArmorLevel = 1; // next level costs 400
            Assert.IsFalse(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel));
            Assert.AreEqual(1, meta.HatchArmorLevel);
            Assert.AreEqual(399L, meta.BankedSalvage);
        }

        [Test]
        public void Buy_CannotPassLevelFive()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 1000000;
            meta.HatchArmorLevel = MetaUpgrades.MaxLevel;
            Assert.IsFalse(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel));
            Assert.AreEqual(MetaUpgrades.MaxLevel, meta.HatchArmorLevel);
            Assert.AreEqual(1000000L, meta.BankedSalvage);
        }

        [Test]
        public void Buy_FullTrack_TotalCostIs6200()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 6200; // 200+400+800+1600+3200
            for (int i = 0; i < MetaUpgrades.MaxLevel; i++)
                Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel), "buy " + i);
            Assert.AreEqual(MetaUpgrades.MaxLevel, meta.HatchArmorLevel);
            Assert.AreEqual(0L, meta.BankedSalvage, "the doubling curve sums to exactly 6200");
        }

        private const float Tol = 1e-5f;

        [Test]
        public void Effect_HatchHp_Exact300Base_Plus5PercentPerLevel()
        {
            float[] expected = { 300f, 315f, 330f, 345f, 360f, 375f };
            for (int l = 0; l <= 5; l++)
                Assert.AreEqual(expected[l], MetaUpgrades.HatchHp(l), Tol, "level " + l);
        }

        [Test]
        public void EquipmentCatalog_RepairRig_Flat8HpPerSecond()
        {
            Assert.AreEqual(8f, EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond, Tol);
            Assert.AreEqual(300L, EquipmentCatalog.Def(EquipmentId.RepairRig).cost);
            Assert.AreEqual(300L, EquipmentCatalog.SlotPrice);
        }

        [Test]
        public void Json_RoundTrip_PreservesUpgradeLevels()
        {
            var meta = new MetaState();
            meta.HatchArmorLevel = 3;
            meta.EquipmentSlotPurchased = true;
            meta.SetEquipmentOwned(EquipmentId.RepairRig, true);
            meta.EquippedEquipment = EquipmentId.RepairRig;

            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(3, loaded.HatchArmorLevel);
            Assert.IsTrue(loaded.EquipmentSlotPurchased);
            Assert.IsTrue(loaded.HasRepairRigEquipped);
        }

        [Test]
        public void Json_OldSaveWithoutUpgradeFields_DefaultsToZero()
        {
            var loaded = MetaState.FromJson("{\"SaveVersion\":1,\"BankedSalvage\":5000,\"TotalRuns\":9}");
            Assert.AreEqual(5000L, loaded.BankedSalvage);
            Assert.AreEqual(0, loaded.HatchArmorLevel);
            Assert.IsFalse(loaded.EquipmentSlotPurchased);
            Assert.IsFalse(loaded.OwnsEquipment(EquipmentId.RepairRig));
        }

        [Test]
        public void SaveData_RoundTrip_PreservesUpgradeLevels()
        {
            var meta = new MetaState();
            meta.HatchArmorLevel = 4;
            meta.EquipmentSlotPurchased = true;
            meta.SetEquipmentOwned(EquipmentId.RepairRig, true);
            meta.EquippedEquipment = EquipmentId.RepairRig;
            var restored = MetaState.FromSaveData(meta.ToSaveData());
            Assert.AreEqual(4, restored.HatchArmorLevel);
            Assert.IsTrue(restored.HasRepairRigEquipped);
        }

        [Test]
        public void ShipSystems_DefaultCtor_Keeps300Baseline()
        {
            var ship = new ShipSystems();
            Assert.AreEqual(300f, ship.Hatch.MaxHp, 1e-5f);
            Assert.AreEqual(ShipSystems.HatchMaxHp, ship.Hatch.MaxHp, 1e-5f);
        }

        [Test]
        public void RunSim_UsesHatchHpFromArmorLevel()
        {
            var meta = new MetaState();
            meta.HatchArmorLevel = 5;
            var sim = new RunSim(meta, 9);
            Assert.AreEqual(375f, sim.Ship.Hatch.MaxHp, 1e-5f); // 300 * (1 + 0.05*5)
        }
    }
}
