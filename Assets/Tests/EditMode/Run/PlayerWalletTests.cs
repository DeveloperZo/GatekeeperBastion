using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class PlayerWalletTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = new MetaState();
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
        public void Convert_GuestDeepest2_CapsAndDoesNotWriteHostMeta()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            sim.SetSlotDeepest(1, 2);
            sim.TriggerDraw();
            for (int i = 0; i < 200; i++)
                sim.AwardKill();

            long guestBefore = sim.UnbankedOf(1);
            Assert.Greater(guestBefore, HatchBank.BankCap(2));

            long guestCredited = 0L;
            sim.GuestConverted += (slot, credited) =>
            {
                if (slot == 1) guestCredited += credited;
            };

            sim.NotifyAssaultCleared();
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);

            Assert.AreEqual(0L, sim.UnbankedOf(1));
            Assert.AreEqual(HatchBank.BankCap(2), guestCredited);
            Assert.Greater(guestCredited, 0L);
            Assert.LessOrEqual(meta.BankedSalvage, HatchBank.BankCap(1));
            Assert.Greater(meta.BankedSalvage, 0L);
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
