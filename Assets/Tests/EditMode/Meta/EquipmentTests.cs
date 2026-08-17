using System;
using System.Reflection;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M23: equipment slot + repair rig; deleted RepairRig/Engineering tracks.</summary>
    public class EquipmentTests
    {
        [Test]
        public void SlotLockedBeforeFirstLoss()
        {
            var m = MetaState.CreateNew();
            m.BankedSalvage = 10_000L;
            Assert.AreEqual(0, m.PodEjections);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "slot purchase refused at PodEjections == 0");
            m.PodEjections = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m));
        }

        [Test]
        public void SlotPurchaseRequiresUnlockAndSalvage()
        {
            var m = MetaState.CreateNew();
            m.BankedSalvage = EquipmentCatalog.SlotPrice - 1;
            m.PodEjections = 1;
            Assert.IsFalse(TryBuySlot(m), "short of slot price");
            Assert.IsFalse(m.EquipmentSlotPurchased);
            Assert.AreEqual(EquipmentCatalog.SlotPrice - 1, m.BankedSalvage);

            m.BankedSalvage = EquipmentCatalog.SlotPrice;
            Assert.IsTrue(TryBuySlot(m));
            Assert.IsTrue(m.EquipmentSlotPurchased);
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.AreEqual(EquipmentCatalog.SlotPrice, m.LifetimeSalvageSpent);

            Assert.IsFalse(TryBuySlot(m), "once only");
        }

        [Test]
        public void RigRequiresSlot()
        {
            var m = MetaState.CreateNew();
            m.PodEjections = 1;
            m.BankedSalvage = 10_000L;
            Assert.IsFalse(TryBuyEquipment(m, EquipmentId.RepairRig), "no slot yet");
            Assert.IsFalse(m.OwnsEquipment(EquipmentId.RepairRig));

            Assert.IsTrue(TryBuySlot(m));
            Assert.IsTrue(TryBuyEquipment(m, EquipmentId.RepairRig));
            Assert.IsTrue(m.OwnsEquipment(EquipmentId.RepairRig));
            Assert.AreEqual(EquipmentId.RepairRig, m.EquippedEquipment);
            Assert.IsTrue(m.HasRepairRigEquipped);
            Assert.AreEqual(8f, EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond, 1e-5f);
        }

        [Test]
        public void MigrationGrantsSlotAndRigAndBalances()
        {
            // Balanced v3 save that owned RepairRig L2. Criterion 8 grants slot + rig free;
            // spend on the deleted track is not refunded; ledger stays balanced.
            const string json =
                "{\"SaveVersion\":3,\"BankedSalvage\":1000,\"LifetimeSalvageEarned\":1600," +
                "\"LifetimeSalvageSpent\":600,\"TotalRuns\":2,\"CleanExits\":0,\"PodEjections\":1," +
                "\"DeepestWaveReached\":2,\"HatchArmorLevel\":0,\"RepairRigLevel\":2," +
                "\"VitalityLevel\":0,\"StaminaLevel\":0,\"EngineeringLevel\":0," +
                "\"ScavengingLevel\":0,\"LuckLevel\":0," +
                "\"WeaponsOwned\":[true,false,false,false,false]," +
                "\"WeaponTiers\":[0,0,0,0,0]," +
                "\"EquippedPrimary\":-1,\"EquippedSecondary\":0,\"EquippedDrone\":-1}";

            var m = MetaState.FromJson(json);
            Assert.IsTrue(m.EquipmentSlotPurchased);
            Assert.IsTrue(m.OwnsEquipment(EquipmentId.RepairRig));
            Assert.AreEqual(EquipmentId.RepairRig, m.EquippedEquipment);
            Assert.AreEqual(1000L, m.BankedSalvage);
            Assert.AreEqual(1600L, m.LifetimeSalvageEarned);
            Assert.AreEqual(600L, m.LifetimeSalvageSpent);
            Assert.IsTrue(m.SalvageLedgerBalances);
            Assert.IsFalse(m.SalvageStatsWereRepaired,
                "a balanced save with lifetime fields needs no repair");
            Assert.AreEqual(SaveData.CurrentVersion, m.SaveVersion);
        }

        [Test]
        public void GrantsNotCountedInSpendFloor()
        {
            var m = MetaState.CreateNew();
            m.EquipmentSlotPurchased = true;
            m.SetEquipmentOwned(EquipmentId.RepairRig, true);
            m.EquippedEquipment = EquipmentId.RepairRig;
            m.HatchArmorLevel = 1; // 200
            Assert.AreEqual(200L, m.ReconstructSpentFromLevels(),
                "granted slot and rig must not raise the spend floor");
        }

        [Test]
        public void RemovedTracksAreGone()
        {
            var upgrades = typeof(MetaUpgrades);
            Assert.IsNull(upgrades.GetMethod("RepairPerSecond",
                BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(upgrades.GetMethod("RepairSpeedMultiplier",
                BindingFlags.Public | BindingFlags.Static));

            Assert.IsNull(typeof(MetaState).GetField("RepairRigLevel"));
            Assert.IsNull(typeof(MetaState).GetField("EngineeringLevel"));

            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "RepairRig"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Engineering"));
        }

        [Test]
        public void EngineeringLevelAlone_AlsoGrants()
        {
            const string json =
                "{\"SaveVersion\":3,\"BankedSalvage\":500,\"LifetimeSalvageEarned\":700," +
                "\"LifetimeSalvageSpent\":200,\"PodEjections\":1,\"EngineeringLevel\":1," +
                "\"WeaponsOwned\":[false,false,false,false,false]," +
                "\"WeaponTiers\":[0,0,0,0,0]}";
            var m = MetaState.FromJson(json);
            Assert.IsTrue(m.EquipmentSlotPurchased);
            Assert.IsTrue(m.HasRepairRigEquipped);
        }

        [Test]
        public void RepairMissingStep_NamesNextGap()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual("equipment slot locked until first lost Run", m.RepairMissingStep());
            m.PodEjections = 1;
            Assert.AreEqual("equipment slot not purchased — buy it at FABRICATION",
                m.RepairMissingStep());
            m.EquipmentSlotPurchased = true;
            Assert.AreEqual("repair rig not owned — buy it at FABRICATION", m.RepairMissingStep());
            m.SetEquipmentOwned(EquipmentId.RepairRig, true);
            m.EquippedEquipment = EquipmentId.RepairRig;
            Assert.IsNull(m.RepairMissingStep());
        }

        /// <summary>In-process purchase without GameSession — mirrors ShipCommands rules.</summary>
        static bool TryBuySlot(MetaState m)
        {
            if (m.EquipmentSlotPurchased) return false;
            if (!MetaUpgrades.UpgradesUnlocked(m)) return false;
            long price = EquipmentCatalog.SlotPrice;
            if (!m.SpendSalvage(price)) return false;
            m.EquipmentSlotPurchased = true;
            return true;
        }

        static bool TryBuyEquipment(MetaState m, EquipmentId id)
        {
            if (!m.EquipmentSlotPurchased) return false;
            if (m.OwnsEquipment(id)) return false;
            long cost = EquipmentCatalog.Def(id).cost;
            if (cost > 0 && !m.SpendSalvage(cost)) return false;
            m.SetEquipmentOwned(id, true);
            m.SetEquippedEquipment(id);
            return true;
        }
    }
}
