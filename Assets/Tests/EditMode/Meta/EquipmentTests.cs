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
        public void SlotLockedBeforeFirstBank()
        {
            var m = MetaState.CreateNew();
            m.BankedSalvage = 10_000L;
            Assert.AreEqual(0, m.HatchConversions);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "slot purchase refused at HatchConversions == 0");
            m.PodEjections = 1;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "a loss alone no longer unlocks — the gate re-keyed to the first bank");
            m.HatchConversions = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m));
        }

        [Test]
        public void SlotPurchaseRequiresUnlockAndSalvage()
        {
            var m = MetaState.CreateNew();
            m.BankedSalvage = EquipmentCatalog.SlotPrice - 1;
            m.HatchConversions = 1;
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
            m.HatchConversions = 1;
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
        public void RepairMissingStep_NamesNextGap()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual("equipment slot locked until salvage banks through the Hatch",
                m.RepairMissingStep());
            m.HatchConversions = 1;
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
