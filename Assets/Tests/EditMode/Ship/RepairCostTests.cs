using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>M19 — hatch repair costs salvage. Clamp order, bank floor, and the cost table
    /// in Docs/Milestones/.../M19-repair-costs-salvage.md.</summary>
    public class RepairCostTests
    {
        static RunSim LiveRun(MetaState meta, out RunSim sim)
        {
            sim = new RunSim(meta, seed: 1);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        [Test]
        public void RepairSalvagePerHp_IsTwo()
        {
            Assert.AreEqual(2f, ShipSystems.RepairSalvagePerHp, 1e-5f);
        }

        [Test]
        public void FullHealthHatch_ChargesNothing()
        {
            var meta = new MetaState();
            meta.EarnSalvage(1000);
            LiveRun(meta, out var sim);

            float applied = sim.RepairHatch(50f);

            Assert.AreEqual(0f, applied, 1e-5f);
            Assert.AreEqual(0L, sim.RepairSalvageSpent);
            Assert.AreEqual(1000L, meta.BankedSalvage);
            Assert.AreEqual(ShipSystems.HatchMaxHp, sim.Ship.Hatch.Hp, 1e-5f);
        }

        [Test]
        public void PartialRepair_AtInsufficientFunds_LandsBankAtExactlyZero()
        {
            var meta = new MetaState();
            meta.EarnSalvage(5);
            LiveRun(meta, out var sim);
            sim.Ship.Hatch.Damage(100f);

            float applied = sim.RepairHatch(100f);

            Assert.AreEqual(2.5f, applied, 1e-5f);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(5L, sim.RepairSalvageSpent);
            Assert.AreEqual(ShipSystems.HatchMaxHp - 100f + 2.5f, sim.Ship.Hatch.Hp, 1e-4f);
        }

        [Test]
        public void ZeroBank_HealsNothing()
        {
            var meta = new MetaState();
            LiveRun(meta, out var sim);
            sim.Ship.Hatch.Damage(40f);
            float before = sim.Ship.Hatch.Hp;

            float applied = sim.RepairHatch(40f);

            Assert.AreEqual(0f, applied, 1e-5f);
            Assert.AreEqual(before, sim.Ship.Hatch.Hp, 1e-5f);
            Assert.AreEqual(0L, sim.RepairSalvageSpent);
        }

        [Test]
        public void ContinuousTicks_ChargeAsApplied_NotAsLump()
        {
            var meta = new MetaState();
            meta.EarnSalvage(1000);
            LiveRun(meta, out var sim);
            sim.Ship.Hatch.Damage(80f);

            // Two half-repairs of 20 HP each at 2/HP → 40 salvage each, not one 80-HP lump.
            float a1 = sim.RepairHatch(20f);
            long spent1 = sim.RepairSalvageSpent;
            long bank1 = meta.BankedSalvage;
            float a2 = sim.RepairHatch(20f);

            Assert.AreEqual(20f, a1, 1e-4f);
            Assert.AreEqual(40L, spent1);
            Assert.AreEqual(960L, bank1);
            Assert.AreEqual(20f, a2, 1e-4f);
            Assert.AreEqual(80L, sim.RepairSalvageSpent);
            Assert.AreEqual(920L, meta.BankedSalvage);
        }

        [Test]
        public void CostTable_WaveDamageFigures_MatchMilestoneArithmetic()
        {
            // M19 Why-it-matters table: 88 HP after wave 1, +276 after wave 2, at 2 salvage/HP.
            Assert.AreEqual(176L, (long)(88 * ShipSystems.RepairSalvagePerHp));
            Assert.AreEqual(728L, (long)((88 + 276) * ShipSystems.RepairSalvagePerHp));
            Assert.AreEqual(1488L, 744L * (long)ShipSystems.RepairSalvagePerHp);
            Assert.AreEqual(2448L, 1224L * (long)ShipSystems.RepairSalvagePerHp);
        }

        [Test]
        public void EndReport_EarnedSubtractsRepairSpend()
        {
            var meta = new MetaState();
            meta.EarnSalvage(500);
            LiveRun(meta, out var sim);
            sim.Ship.Hatch.Damage(50f);
            sim.RepairHatch(50f); // 100 salvage
            Assert.AreEqual(100L, sim.RepairSalvageSpent);

            // One kill at dial 0 / scavenging 0 = 25, then die.
            sim.TriggerDraw();
            sim.AwardKill();
            sim.NotifyPlayerDown();

            Assert.AreEqual(100L, sim.LastLossReport.repairSpent);
            Assert.AreEqual(25L, sim.LastLossReport.grossEarned);
            Assert.AreEqual(25L - 100L, sim.LastLossReport.grossEarned - sim.LastLossReport.repairSpent);
        }

        [Test]
        public void UsesSpendSalvage_LifetimeSpentTracks()
        {
            var meta = new MetaState();
            meta.EarnSalvage(200);
            LiveRun(meta, out var sim);
            sim.Ship.Hatch.Damage(30f);
            sim.RepairHatch(30f);

            Assert.AreEqual(60L, meta.LifetimeSalvageSpent);
            Assert.AreEqual(140L, meta.BankedSalvage);
            Assert.IsTrue(meta.SalvageLedgerBalances);
        }
    }
}
