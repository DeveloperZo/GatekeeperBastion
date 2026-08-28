using System;
using System.Reflection;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M23: equipment slot + repair rig; deleted RepairRig/Engineering tracks.</summary>
    public class EquipmentTests
    {
        /// <summary>Re-keyed from SlotLockedBeforeFirstBank (Zo, 2026-08-25). The slot's key was
        /// the first Convert through the Hatch, and the Convert window is out of the demo — kills
        /// bank as they land now, so the honest question is whether the realm has EARNED, and
        /// the answer for anyone who has played at all is yes. The two negatives are what the
        /// test was always for: an assigned bank is not an earned one, and an ending is not
        /// earning.</summary>
        [Test]
        public void SlotLockedUntilTheRealmHasEarned()
        {
            var m = MetaState.CreateNew();
            m.BankedSalvage = 10_000L; // assigned past the ledger, so nothing was earned
            Assert.AreEqual(0L, m.LifetimeSalvageEarned);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "slot purchase refused at LifetimeSalvageEarned == 0");
            m.PodEjections = 1;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "a loss alone does not unlock — dying is not earning");
            m.EarnSalvage(1L);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "one earned unit is the whole gate");
        }

        /// <summary>LIFETIME, not the pocket: spending a realm back down to nothing must not
        /// take the slot and the wheel away with the salvage (Zo, 2026-08-25).</summary>
        [Test]
        public void SlotStaysUnlocked_AfterTheBankIsSpentToZero()
        {
            var m = MetaState.CreateNew();
            m.EarnSalvage(500L);
            Assert.IsTrue(m.SpendSalvage(500L));
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m));
        }

        [Test]
        public void SlotPurchaseRequiresUnlockAndSalvage()
        {
            var m = MetaState.CreateNew();
            // Earned, not assigned: earning is the unlock since 2026-08-25 (Zo) AND the only
            // way to arrive at a price-shy bank with the ledger balanced, which is the state a
            // player short of the slot is actually in.
            m.EarnSalvage(EquipmentCatalog.SlotPrice - 1);
            Assert.IsFalse(TryBuySlot(m), "short of slot price");
            Assert.IsFalse(m.EquipmentSlotPurchased);
            Assert.AreEqual(EquipmentCatalog.SlotPrice - 1, m.BankedSalvage);

            m.EarnSalvage(1L);
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
            m.EarnSalvage(10_000L); // earning is the unlock now (Zo, 2026-08-25)
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
            Assert.AreEqual("equipment slot locked until this realm has earned salvage",
                m.RepairMissingStep());
            // Both keys turned, deliberately. MetaState.RepairMissingStep still reads
            // HatchConversions while the purchase it explains reads
            // MetaUpgrades.UpgradesUnlocked, which re-keyed to LifetimeSalvageEarned on
            // 2026-08-25 — REPORTED, not fixed here. Turning both is what keeps this ladder a
            // test of the MESSAGE ORDER rather than a second vote on which gate is right.
            m.EarnSalvage(1L);
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
