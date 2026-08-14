using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    public class HatchBankTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = new MetaState();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        static void TerminalKillsThenPrep(RunSim sim, int kills)
        {
            sim.TriggerDraw();
            Kill(sim, kills);
            sim.NotifyAssaultCleared();
        }

        static void RunHatchBankWindow(RunSim sim)
        {
            sim.Tick(HatchBank.DurationSeconds);
        }

        [Test]
        public void CarryConvert_Table()
        {
            Assert.AreEqual(1.00f, HatchBank.CarryConvert(0), 1e-5f);
            Assert.AreEqual(1.00f, HatchBank.CarryConvert(1), 1e-5f);
            Assert.AreEqual(1.10f, HatchBank.CarryConvert(2), 1e-5f);
            Assert.AreEqual(1.25f, HatchBank.CarryConvert(3), 1e-5f);
            Assert.AreEqual(1.35f, HatchBank.CarryConvert(4), 1e-5f);
            Assert.AreEqual(1.50f, HatchBank.CarryConvert(5), 1e-5f);
            Assert.AreEqual(1.50f, HatchBank.CarryConvert(99), 1e-5f);
            Assert.AreEqual(0, HatchBank.CarryWaveIndex(1));
            Assert.AreEqual(4, HatchBank.CarryWaveIndex(5));
        }

        [Test]
        public void AwardKill_CreditsUnbanked_NotShipBank()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual(25L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);
        }

        [Test]
        public void PlayerDown_WipesUnbanked_KeepsShipBank()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            sim.TriggerDraw();
            Kill(sim, 2);
            Assert.AreEqual(50L, sim.Unbanked);
            sim.NotifyPlayerDown();
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(1000L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
        }

        [Test]
        public void NibbleThreeTimes200_Banks600AtOneTimes()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            long ship = 0L;
            for (int i = 0; i < 3; i++)
            {
                TerminalKillsThenPrep(sim, 8); // 8 * 25 = 200
                Assert.AreEqual(200L, sim.Unbanked);
                sim.TriggerDraw(WaveMode.HatchBank);
                Assert.AreEqual(HatchBank.CarryConvert(1), sim.HatchConvertMultiplier, 1e-5f);
                RunHatchBankWindow(sim);
                ship += 200L;
                Assert.AreEqual(0L, sim.Unbanked);
                Assert.AreEqual(ship, meta.BankedSalvage);
                if (i < 2) sim.SeekWave(1);
            }
            Assert.AreEqual(600L, meta.BankedSalvage);
            Assert.AreEqual(0, sim.HatchBankSlotsLeft);
        }

        [Test]
        public void ThreeWaves_CarryConvert_Banks750()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8); // 600, carry 3 → ×1.25
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(3, sim.HatchCarry);
            Assert.AreEqual(HatchBank.MultMid, sim.HatchConvertMultiplier, 1e-5f);
            RunHatchBankWindow(sim);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(750L, meta.BankedSalvage);
            Assert.AreEqual(750L, sim.Salvage.RunSalvage);
        }

        [Test]
        public void ThreeWaves_DripThenDump_Still750()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(10f); // 200 dripped ×1.25 = 250 ship, 400 left
            Assert.AreEqual(400L, sim.Unbanked);
            Assert.AreEqual(250L, meta.BankedSalvage);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "timer, not enemy wipe");
            sim.Tick(HatchBank.DurationSeconds - 10f);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(750L, meta.BankedSalvage);
        }

        [Test]
        public void FourthHatchBank_Refused()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            for (int i = 0; i < 3; i++)
            {
                TerminalKillsThenPrep(sim, 8);
                sim.TriggerDraw(WaveMode.HatchBank);
                RunHatchBankWindow(sim);
                if (i < 2) sim.SeekWave(1);
            }
            sim.SeekWave(1);
            TerminalKillsThenPrep(sim, 8);
            Assert.AreEqual(0, sim.HatchBankSlotsLeft);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(200L, sim.Unbanked);
        }

        [Test]
        public void HatchBank_ZeroUnbanked_Refused()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
        }

        [Test]
        public void HatchBank_DoesNotChain()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.IsFalse(sim.ChainPending);
        }

        [Test]
        public void DamageHatch_AttritsUnbanked_InHatchBank()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 24);
            sim.TriggerDraw(WaveMode.HatchBank);
            float hp = sim.Ship.Hatch.Hp;
            sim.DamageHatch(50f);
            Assert.AreEqual(hp - 50f, sim.Ship.Hatch.Hp, 0.01f);
            Assert.AreEqual(550L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void DamageHatch_NoOpInTerminal_EvenWithUnbanked()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            Kill(sim, 2);
            float hp = sim.Ship.Hatch.Hp;
            sim.DamageHatch(25f);
            Assert.AreEqual(hp, sim.Ship.Hatch.Hp);
            Assert.AreEqual(50L, sim.Unbanked);
        }

        [Test]
        public void HatchBreach_WipesRemaining_KeepsConverted()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 24);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(5f); // 100 dripped ×1.00 (carry 1)
            Assert.AreEqual(100L, meta.BankedSalvage);
            sim.DamageHatch(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(100L, meta.BankedSalvage);
            Assert.AreEqual(100L, sim.LastRunReport.grossEarned);
        }

        [Test]
        public void HatchBank_DoesNotAdvanceCurrentWaveIndex()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            int before = sim.CurrentWaveIndex;
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(before, sim.CurrentWaveIndex);
            RunHatchBankWindow(sim);
            Assert.AreEqual(before, sim.CurrentWaveIndex);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void TerminalWave5Clear_KeepsUnbanked_DoesNotEscape()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.Greater(sim.Unbanked, 0L);
            Assert.IsFalse(sim.AllWavesCleared);
        }

        [Test]
        public void HatchBankAfterWave5_ConvertsAndStaysPrep()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }
            long pile = sim.Unbanked;
            Assert.Greater(pile, 0L);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
            RunHatchBankWindow(sim);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(HatchBank.BankedFrom(pile, HatchBank.CarryConvert(5)),
                meta.BankedSalvage);
            Assert.IsFalse(sim.AllWavesCleared);
        }

        [Test]
        public void HatchBankAfterWave5_ThenTerminalDrawStarts()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }
            sim.TriggerDraw(WaveMode.HatchBank);
            RunHatchBankWindow(sim);
            WaveSpec next = default;
            sim.WaveStarted += s => next = s;
            sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            Assert.AreEqual(director.NextWave(WaveDirector.MaxWaves - 1, 0).enemyCount, next.enemyCount);
            Assert.AreEqual(WaveDirector.MaxWaves + 1, sim.CurrentWaveIndex);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.AllWavesCleared);
        }

        [Test]
        public void HatchBank_OneWave_MatchesWave1()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var director = new WaveDirector();
            WaveSpec hatch = default;
            sim.WaveStarted += s => hatch = s;
            TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(1, sim.HatchCarry);
            Assert.AreEqual(director.NextWave(0, 0).enemyCount, hatch.enemyCount);
        }

        [Test]
        public void HatchBank_FiveWaves_MatchesWave5()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var director = new WaveDirector();
            WaveSpec hatch = default;
            sim.WaveStarted += s => hatch = s;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(5, sim.HatchCarry);
            Assert.AreEqual(director.NextWave(4, 0).enemyCount, hatch.enemyCount);
        }

        [Test]
        public void HatchBank_UnbankedZero_DoesNotEndWindow()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.DamageHatch(200f);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            RunHatchBankWindow(sim);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void HatchBank_DripMovesUnbankedToBanked()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 24);
            sim.TriggerDraw(WaveMode.HatchBank);
            long unbanked = sim.Unbanked;
            sim.Tick(1f);
            Assert.AreEqual(unbanked - 20L, sim.Unbanked);
            Assert.AreEqual(HatchBank.BankedFrom(20L, HatchBank.CarryConvert(1)), meta.BankedSalvage);
        }

        [Test]
        public void Withdraw_OneSecond_MovesSixteen()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            Assert.AreEqual(16f, HatchBank.WithdrawPerSecond, 1e-5f);
            long moved = sim.Withdraw(1f);
            Assert.AreEqual(16L, moved);
            Assert.AreEqual(16L, sim.Unbanked);
            Assert.AreEqual(984L, meta.BankedSalvage);
        }

        [Test]
        public void RepairHatch_Full_DoesNotWithdraw()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            Assert.AreEqual(0f, sim.RepairHatch(50f), 1e-5f);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(1000L, meta.BankedSalvage);
        }
    }
}
