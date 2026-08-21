using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class PlayerWalletTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        [Test]
        public void AwardKill_Solo_CreditsSlot0_TwentyFive()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual(1, sim.PlayerCount);
            Assert.AreEqual(25L, sim.Unbanked);
            Assert.AreEqual(25L, sim.UnbankedOf(0));
            Assert.AreEqual(25L, sim.TotalUnbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void AwardKill_ThreePlayers_SplitsEqually_RemainderToHost()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual(9L, sim.UnbankedOf(0));
            Assert.AreEqual(8L, sim.UnbankedOf(1));
            Assert.AreEqual(8L, sim.UnbankedOf(2));
            Assert.AreEqual(25L, sim.TotalUnbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void WipeSlot_GuestDown_KeepsOthers()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            sim.TriggerDraw();
            sim.AwardKill();
            sim.WipeUnbankedSlot(1);
            Assert.AreEqual(9L, sim.UnbankedOf(0));
            Assert.AreEqual(0L, sim.UnbankedOf(1));
            Assert.AreEqual(8L, sim.UnbankedOf(2));
        }

        [Test]
        public void BankCap_Deepest2_IsWavesOneAndTwoAtDial0()
        {
            Assert.AreEqual(18L * 25L, HatchBank.BankCap(1));
            Assert.AreEqual((18L + 30L) * 25L, HatchBank.BankCap(2));
        }

        [Test]
        public void Convert_CapsEachSeatByItsOwnDepth_AndBanksThemAllIntoTheRealm()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            sim.SetSlotDeepest(1, 2);
            sim.TriggerDraw();
            for (int i = 0; i < 200; i++)
                sim.AwardKill();

            // Each seat's cap is set by its OWN depth, so seat 1 may convert more than the
            // other two. Every pile here is over its cap, which is what makes the cap the
            // thing being measured rather than the pile.
            long[] before = { sim.UnbankedOf(0), sim.UnbankedOf(1), sim.UnbankedOf(2) };
            long[] caps =
            {
                HatchBank.BankCap(meta.DeepestWaveReached),
                HatchBank.BankCap(2),
                HatchBank.BankCap(1)
            };
            Assert.Greater(before[1], caps[1], "seat 1's pile has to exceed its cap to test it");
            long expected = 0L;
            for (int i = 0; i < 3; i++)
                expected += before[i] < caps[i] ? before[i] : caps[i];

            long potBefore = meta.BankedSalvage;

            sim.NotifyAssaultCleared();
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);

            Assert.AreEqual(0L, sim.UnbankedOf(1), "the guest's pile converted");
            // Every seat banks into the realm's pot — a guest plays in somebody else's world
            // and it is that world's salvage that fills. What the guest takes home is its
            // Boarder (Docs/Milestones — the realm and the Boarder).
            Assert.AreEqual(expected, meta.BankedSalvage - potBefore,
                "all three seats converted into the realm, each capped by its own depth");
            Assert.Greater(meta.BankedSalvage - potBefore, caps[0],
                "the pot holds more than the host seat alone could have put there");
        }

        [Test]
        public void Convert_Solo_DoesNotApplyBankCap()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            for (int i = 0; i < 18; i++)
                sim.AwardKill();
            sim.NotifyAssaultCleared();
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
            Assert.AreEqual(18L * 25L, meta.BankedSalvage);
        }

        [Test]
        public void LaunchWave_ThreePlayers_SendsFiftyFour()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            WaveSpec spec = default;
            sim.WaveStarted += s => spec = s;
            sim.TriggerDraw();
            Assert.AreEqual(54, spec.enemyCount);
        }
    }
}
