using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The beat-relative config-taint rule (owner ruling 2026-08-24):
    /// configurations grow valid as the beats progress — a row granting a Primary is
    /// tainted before the Primary rung unlocks, and a kit the beat's progression could
    /// not have earned is tainted anywhere. Judged against the constructed beat state
    /// BEFORE the overlay. These pin the rule against the real beat builders, so a beat
    /// economy change moves the verdicts with it and a gutted rule fails both ways
    /// (always-clean fails the max-kit case, always-tainted fails the late-beat case).</summary>
    public class CheckpointConfigTaintTests
    {
        const int FirstPrimaryBeat = 5; // ApplyDoorTwoOpen — DemoRungTwoOpened flips here
        const int LastBeat = 8;

        static MetaState Beat(int digit)
        {
            var m = GameController.BuildDemoCheckpoint(digit);
            Assert.IsNotNull(m, "beat " + digit + " must construct");
            return m;
        }

        [Test]
        public void ConfigZero_AndOutOfRange_AreNeverTainted()
        {
            var m = Beat(1);
            Assert.IsFalse(GameController.CheckpointConfigTainted(m, 0));
            Assert.IsFalse(GameController.CheckpointConfigTainted(m, 99));
            Assert.IsFalse(GameController.CheckpointConfigTainted(null, 1));
        }

        [Test]
        public void Validity_IsMonotonic_AcrossBeats()
        {
            // Once a row reads clean at a beat, every later beat keeps it clean — "as you
            // progress through beats more configurations are valid", never fewer.
            for (int c = 1; c <= GameController.CheckpointConfigs.Length; c++)
            {
                bool seenClean = false;
                for (int d = 1; d <= LastBeat; d++)
                {
                    bool tainted = GameController.CheckpointConfigTainted(Beat(d), c);
                    if (!tainted) seenClean = true;
                    else if (seenClean)
                        Assert.Fail("config " + c + " went clean and then re-tainted at beat " + d);
                }
                Assert.IsTrue(seenClean, "config " + c + " never becomes valid at any beat");
            }
        }

        [Test]
        public void SecondaryRow_TaintedAtBeatOne_CleanOnceTheWheelEconomyExists()
        {
            // Row 1 is MK-1 T2: 450 on the Secondary curve. Beat 1 has earned nothing;
            // beat 3 (first return) has earned 500 and covers it.
            Assert.IsTrue(GameController.CheckpointConfigTainted(Beat(1), 1));
            Assert.IsFalse(GameController.CheckpointConfigTainted(Beat(3), 1));
        }

        [Test]
        public void PrimaryRows_TaintedBeforeTheRungUnlocks_CleanFromItOn()
        {
            for (int c = 2; c <= GameController.CheckpointConfigs.Length; c++)
            {
                for (int d = 1; d < FirstPrimaryBeat; d++)
                    Assert.IsTrue(GameController.CheckpointConfigTainted(Beat(d), c),
                        "config " + c + " grants a Primary and must be tainted at beat " + d);
                for (int d = FirstPrimaryBeat; d <= LastBeat; d++)
                    Assert.IsFalse(GameController.CheckpointConfigTainted(Beat(d), c),
                        "config " + c + " matches the beat-" + d + " economy and must be clean");
            }
        }

        [Test]
        public void MaxUpgradedKit_IsTaintedAtEveryBeat()
        {
            // The ruling's own example: a kit no beat could have earned. PRI-C T10 alone
            // costs 2,500 + the full Primary wheel spend — beyond beat 8's whole economy.
            var maxed = new GameController.CheckpointConfig("maxed — PRI-C T10, DEFAULT",
                BoarderProtocolId.Depot, WeaponId.PriC, 10, WeaponId.Mk1Sidearm, 10);
            for (int d = 1; d <= LastBeat; d++)
                Assert.IsTrue(GameController.ConfigTaintedFor(Beat(d), maxed),
                    "a max-upgraded kit must be tainted at beat " + d);
        }

        [Test]
        public void KitCost_PricesTheSlotAwareCurve()
        {
            // The affordability half prices at today's shop curve; the beats mint at the
            // historical (never cheaper) curve, so a beat's own kit can never be judged
            // unaffordable. Row 2 is PRI-C T2 + MK-1 T2.
            var row = GameController.CheckpointConfigs[1];
            long expected =
                WeaponCatalog.Def(WeaponId.PriC).cost
                + WeaponTier.SpentToReach(WeaponSlot.Primary, 2)
                + WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost
                + WeaponTier.SpentToReach(WeaponSlot.Secondary, 2);
            Assert.AreEqual(expected, GameController.ConfigKitCost(row));
        }
    }
}
