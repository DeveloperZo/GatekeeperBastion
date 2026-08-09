using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>EditMode tests for the Subsystem primitive, the Hatch aggregate, and the per-run
    /// salvage tally. The ledger has no banking API: currency is credited to MetaState per kill
    /// by RunSim, and SalvageLedger only tallies what a run earned.</summary>
    public class ShipAndLedgerTests
    {
        [Test]
        public void Subsystem_Damage_ReducesHp()
        {
            var sub = new Subsystem("Test", 100f);
            sub.Damage(30f);
            Assert.AreEqual(70f, sub.Hp, 1e-5f);
            Assert.IsFalse(sub.IsDestroyed);
        }

        [Test]
        public void Subsystem_Damage_ClampsAtZero_AndSetsDestroyed()
        {
            var sub = new Subsystem("Test", 100f);
            sub.Damage(500f);
            Assert.AreEqual(0f, sub.Hp, 1e-5f);
            Assert.IsTrue(sub.IsDestroyed);
        }

        [Test]
        public void Subsystem_Repair_ClampsAtMaxHp()
        {
            var sub = new Subsystem("Test", 100f);
            sub.Damage(10f);
            sub.Repair(500f);
            Assert.AreEqual(100f, sub.Hp, 1e-5f);
        }

        [Test]
        public void Subsystem_NonPositiveAmounts_Ignored()
        {
            var sub = new Subsystem("Test", 100f);
            sub.Damage(-5f);
            sub.Repair(-5f);
            Assert.AreEqual(100f, sub.Hp, 1e-5f);
        }

        [Test]
        public void Subsystem_DestroyedEvent_FiresOncePerDestruction()
        {
            var sub = new Subsystem("Test", 100f);
            int fired = 0;
            sub.Destroyed += s => fired++;
            sub.Damage(200f);
            sub.Damage(200f); // already destroyed, ignored
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Subsystem_Repair_RevivesDestroyedSystem()
        {
            var sub = new Subsystem("Test", 100f);
            sub.Damage(200f);
            sub.Repair(40f);
            Assert.IsFalse(sub.IsDestroyed);
            Assert.AreEqual(40f, sub.Hp, 1e-5f);
        }

        [Test]
        public void ShipSystems_Hatch_StartsAtFullDefaultHp()
        {
            var ship = new ShipSystems();
            Assert.AreEqual("Hatch", ship.Hatch.Name);
            Assert.AreEqual(ShipSystems.HatchMaxHp, ship.Hatch.MaxHp, 1e-5f);
            Assert.AreEqual(ShipSystems.HatchMaxHp, ship.Hatch.Hp, 1e-5f);
            Assert.IsFalse(ship.HatchIsDestroyed);
        }

        [Test]
        public void ShipSystems_HatchDestroyedEvent_FiresAndFlagSets()
        {
            var ship = new ShipSystems();
            bool fired = false;
            ship.HatchDestroyed += () => fired = true;
            ship.Hatch.Damage(99999f);
            Assert.IsTrue(fired);
            Assert.IsTrue(ship.HatchIsDestroyed);
        }

        [Test]
        public void Ledger_Earn_AddsWholeUnits()
        {
            var ledger = new SalvageLedger();
            ledger.Earn(40L);
            Assert.AreEqual(40L, ledger.RunSalvage);
        }

        [Test]
        public void Ledger_Earn_IgnoresNonPositiveInput()
        {
            var ledger = new SalvageLedger();
            ledger.Earn(-10L);
            ledger.Earn(0L);
            Assert.AreEqual(0L, ledger.RunSalvage);
        }

        [Test]
        public void Ledger_Accumulates_AndOnlyEverGrows()
        {
            // No banking API exists any more: the tally is monotonic for the life of a run.
            var ledger = new SalvageLedger();
            ledger.Earn(25L);
            ledger.Earn(200L);
            Assert.AreEqual(225L, ledger.RunSalvage); // 25 + 200
        }

        [Test]
        public void Ledger_HasNoBankingApi()
        {
            // The stake is gone, so the ledger no longer moves currency at all: its whole
            // public surface is RunSalvage + Earn. If a Bank* method comes back, this fails.
            var type = typeof(SalvageLedger);
            foreach (var m in type.GetMethods(System.Reflection.BindingFlags.Public
                                              | System.Reflection.BindingFlags.Instance
                                              | System.Reflection.BindingFlags.DeclaredOnly))
                Assert.IsFalse(m.Name.StartsWith("Bank"), "unexpected banking method: " + m.Name);
        }
    }
}
