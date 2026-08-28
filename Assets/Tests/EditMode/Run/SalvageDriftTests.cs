using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>AwardKill floors ONCE per body — the point of these pins is that a
    /// fractional dial never accumulates drift, whichever pile the award lands in.
    /// Re-keyed 2026-08-25 (Zo, kills bank as they land): the destination moved from the
    /// carried pile to the realm's bank, at face value with no carry multiplier in the way,
    /// so the floor is still readable end-to-end. The carried pile is now the number that
    /// must stay 0, because only a deliberate Withdraw fills it.</summary>
    public class SalvageDriftTests
    {
        /// <summary>What one body pays at <paramref name="siphonLevel"/>, DERIVED from the two
        /// constants that decide it. The 31.25 that used to sit inline here was 25 x 1.25, and
        /// SalvagePerKill 25 -> 42 on 2026-08-27 took it to 52.5 — still a fraction, which is the
        /// whole reason this fixture works at siphon 3, and the reason the FLOOR is what it
        /// asserts rather than the value.</summary>
        static float KillValue(int siphonLevel) =>
            MetaUpgrades.SalvagePerKill() * TetherSiphon.RewardMultiplierFor(siphonLevel);

        static long FlooredKillValue(int siphonLevel) => (long)KillValue(siphonLevel);

        static RunSim LiveAssault(MetaState meta, int siphonLevel)
        {
            var sim = new RunSim(meta, seed: 42);
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(siphonLevel);
            sim.TriggerDraw();
            return sim;
        }

        [Test]
        public void MixedKills_AtFractionalDial_BankedEqualsFlooredAwards()
        {
            var meta = MetaState.CreateNew();
            long bankedBefore = meta.BankedSalvage;
            var sim = LiveAssault(meta, siphonLevel: 3);

            const int kills = 17;
            for (int i = 0; i < kills; i++) sim.AwardKill();

            long expected = kills * FlooredKillValue(3);
            Assert.AreEqual(KillValue(3), sim.CurrentKillValue, 1e-5f);
            Assert.AreEqual(bankedBefore + expected, meta.BankedSalvage);

            // THE DRIFT ITSELF, asserted rather than described. Flooring per body must lose
            // strictly more than flooring the sum, or this fixture is watching nothing: at 42 x
            // 1.25 that is 17 x 52 = 884 against floor(17 x 52.5) = 892, eight salvage of
            // difference. If a retune ever makes the kill value a whole number the two agree and
            // this pin goes quiet, so the gap is checked, not assumed.
            long asOneSum = (long)(kills * KillValue(3));
            Assert.Less(expected, asOneSum,
                $"floor-per-body ({expected}) must differ from floor-of-the-sum ({asOneSum}); at a "
                + $"kill value of {KillValue(3)} they agree and this fixture proves nothing - pick "
                + "a siphon level whose award is fractional");
            Assert.AreEqual(0L, sim.Unbanked,
                "kills bank at the Hatch, not into the carry — only Withdraw fills that pile");
            Assert.AreEqual(expected, sim.Salvage.RunSalvage);
        }

        [Test]
        public void FractionalDial_BankAgreesWithFloor()
        {
            var meta = MetaState.CreateNew();
            var sim = LiveAssault(meta, siphonLevel: 3);
            Assert.AreEqual(KillValue(3), sim.CurrentKillValue, 1e-4f);

            long before = meta.BankedSalvage;
            const int kills = 9;
            for (int i = 0; i < kills; i++) sim.AwardKill();

            long expected = kills * FlooredKillValue(3);
            Assert.AreEqual(before + expected, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
        }
    }
}
