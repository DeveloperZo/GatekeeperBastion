using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// The Ante's purse. RE-KEYED 2026-08-25 (Zo): kills bank as they land, so the carried
    /// pile is empty unless the player deliberately Withdrew, and <see cref="IntensityJuice"/>
    /// now charges <see cref="RunSim.SpendBanked"/> instead of decrementing Unbanked. Every
    /// pin below that used to read the carry reads the bank, and the one that used to prove
    /// the bank was NEVER touched now proves the opposite on purpose — see
    /// <see cref="Juice_ChargesTheBank"/>. The affordability rule itself is unchanged: the
    /// Ante is paid up front out of money you already have, and refuses when you do not.
    /// </summary>
    public class IntensityJuiceTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        /// <summary>Bodies at the base dial, back in Prep with the takings banked.
        /// Returns what landed, so a pin can price the Ante against it rather than
        /// restating the kill award.</summary>
        static long CreditKills(RunSim sim, int count)
        {
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, count);
            sim.NotifyAssaultCleared();
            return earned;
        }

        /// <summary>The smallest number of dial-0 kills that funds exactly TWO Ante steps and
        /// not a third — the shape every pin below is written around.
        ///
        /// IT USED TO BE THE LITERAL 10, and ten kills paid 250 against a 100 step: two steps and
        /// 50 left over. SalvagePerKill 25 -> 42 on 2026-08-27 took the same ten kills to 420,
        /// which buys FOUR steps, and "the third refuses" stopped being true of the fixture's own
        /// setup. Searching for the count off the two live constants is what keeps the SHAPE
        /// pinned instead of the arithmetic that happened to produce it — and it is checked in
        /// both directions, because a count that funds three steps tests nothing.</summary>
        static int KillsThatFundExactlyTwoSteps()
        {
            long perKill = (long)MetaUpgrades.SalvagePerKill();
            Assert.Greater(perKill, 0L, "a kill has to be worth something");
            int kills = 0;
            while (kills * perKill < 2L * IntensityJuice.CostPerStep) kills++;
            Assert.Less(kills * perKill, 3L * IntensityJuice.CostPerStep,
                $"{kills} kills at {perKill} funds a third {IntensityJuice.CostPerStep} step, so "
                + "no whole number of kills lands between two and three - the pins below would be "
                + "asserting a refusal that cannot happen and must be re-keyed to a new shape");
            return kills;
        }

        [Test]
        public void BankedTwoStepsWorth_JuicesTwice_ThirdRefuses()
        {
            // Renamed FROM Banked250_JuicesTwice_ThirdRefuses: 250 was ten kills at 25, and the
            // salvage raise made both halves of that number wrong at once.
            MetaState meta;
            var sim = Tethered(out meta);
            long earned = CreditKills(sim, KillsThatFundExactlyTwoSteps());
            Assert.AreEqual(earned, meta.BankedSalvage);

            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsFalse(sim.TryJuice(1, true),
                $"{earned - 2L * IntensityJuice.CostPerStep} banked cannot buy a "
                + $"{IntensityJuice.CostPerStep} step — the Ante is paid up front or not at all");
            Assert.AreEqual(2, sim.Siphon.Level);
            Assert.AreEqual(earned - 2L * IntensityJuice.CostPerStep, meta.BankedSalvage);
            Assert.AreEqual(2L * IntensityJuice.CostPerStep, sim.JuiceSpentThisPull);
            // The refused third step must not have nibbled the remainder.
            Assert.AreEqual(0L, sim.Unbanked, "the Ante never reaches the carried pile");
        }

        [Test]
        public void Juice_ChargesTheBank()
        {
            // INVERTED 2026-08-25 (Zo). This pin was Juice_LeavesBankedSalvageUntouched, and
            // it was right while the Ante ate an at-risk carry: charging the bank would have
            // let a player spend safe money on a bet. Now that kills bank on landing there is
            // no other purse to charge, and the bank IS what the Ante costs — so the pin is
            // kept pointing the other way rather than dropped, to catch the day it silently
            // stops charging anything at all.
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            long earned = CreditKills(sim, KillsThatFundExactlyTwoSteps());
            Assert.AreEqual(1000L + earned, meta.BankedSalvage);

            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.AreEqual(1000L + earned - IntensityJuice.CostPerStep, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
        }

        [Test]
        public void TryJuice_RefusedDuringAssault()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            long purse = meta.BankedSalvage;
            sim.TriggerDraw();
            Assert.IsFalse(sim.TryJuice(1, true));
            Assert.AreEqual(purse, meta.BankedSalvage);
            Assert.AreEqual(0, sim.Siphon.Level);
        }

        [Test]
        public void TryJuice_RefusedAwayFromTrigger()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            long earned = CreditKills(sim, KillsThatFundExactlyTwoSteps());
            Assert.IsFalse(sim.TryJuice(1, false));
            Assert.AreEqual(earned, meta.BankedSalvage);
            Assert.AreEqual(0, sim.Siphon.Level);
            Assert.AreEqual(0L, sim.JuiceSpentThisPull);
        }

        [Test]
        public void Lowering_IsFree_DoesNotRefund()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            long earned = CreditKills(sim, KillsThatFundExactlyTwoSteps());
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(-1, true));
            Assert.AreEqual(1, sim.Siphon.Level);
            // Walking the dial back returns the risk but not the money: the purse still shows
            // TWO steps gone while the dial reads one.
            Assert.AreEqual(earned - 2L * IntensityJuice.CostPerStep, meta.BankedSalvage);
            Assert.AreEqual(2L * IntensityJuice.CostPerStep, sim.JuiceSpentThisPull);
        }

        [Test]
        public void IdlePrepClear_ResetsJuiceForNextPull()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(1, true));
            sim.TriggerDraw();
            sim.NotifyAssaultCleared();
            Assert.AreEqual(0, sim.Siphon.Level);
            Assert.AreEqual(0L, sim.JuiceSpentThisPull);
        }
    }
}
