using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class HatchBankTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
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
            Assert.AreEqual(1, HatchBank.ExtraThief(3));
            Assert.AreEqual(0, HatchBank.ExtraThief(1));
            Assert.AreEqual(0, HatchBank.ExtraThief(5));
        }

        [Test]
        public void MasterTerminalUnbanked_IsFiveWaveFiveClears()
        {
            var director = new WaveDirector();
            long perKill = (long)System.Math.Floor((double)MetaUpgrades.SalvagePerKill());
            long waveFive = director.NextWave(WaveDirector.MaxWaves - 1, 0, 1).enemyCount * perKill;
            Assert.AreEqual(waveFive * MasterControl.TerminalWaves, HatchBank.MasterTerminalUnbanked());
            Assert.AreEqual(8250L, HatchBank.MasterTerminalUnbanked(),
                "66 bodies × 25 × 5 terminal windows at dial 0");
        }

        [Test]
        public void SeedBankPile_SetsUnbankedAndCarry()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SeedBankPile(HatchBank.BankCap(2), 2);
            Assert.AreEqual(HatchBank.BankCap(2), sim.Unbanked);
            Assert.AreEqual(2, sim.WavesSinceBank);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            sim.SeedBankPile(HatchBank.BankCap(5) - HatchBank.BankCap(2), 3, WaveDirector.MaxWaves);
            Assert.AreEqual(HatchBank.BankCap(5) - HatchBank.BankCap(2), sim.Unbanked);
            Assert.AreEqual(3, sim.WavesSinceBank);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
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
            sim.Tick(10f); // 80 dripped ×1.25 = 100 ship, 520 left
            Assert.AreEqual(520L, sim.Unbanked);
            Assert.AreEqual(100L, meta.BankedSalvage);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "timer, not enemy wipe");
            sim.Tick(HatchBank.DurationSeconds - 10f);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(750L, meta.BankedSalvage);
        }

        /// <summary>Banking is uncapped per Run: the 4th window converts like the first three.
        /// This is what the old slots-per-Run refuse used to forbid
        /// (Docs/Demo - Single/5-repeat-to-control-room.md, Delete).</summary>
        [Test]
        public void FourthHatchBank_StillConverts()
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
            Assert.AreEqual(200L, sim.Unbanked);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            RunHatchBankWindow(sim);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(800L, meta.BankedSalvage);
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
            sim.Tick(5f); // 40 dripped ×1.00 (carry 1)
            Assert.AreEqual(40L, meta.BankedSalvage);
            sim.DamageHatch(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(40L, meta.BankedSalvage);
            Assert.AreEqual(40L, sim.LastRunReport.grossEarned);
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
        public void HatchBank_ThreeWaves_AddsExtraThief()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var director = new WaveDirector();
            WaveSpec hatch = default;
            sim.WaveStarted += s => hatch = s;
            for (int i = 0; i < 3; i++)
                TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(3, sim.HatchCarry);
            WaveSpec field = director.NextWave(2, 0);
            Assert.AreEqual(field.enemyCount + HatchBank.Carry3ExtraThief, hatch.enemyCount);
            Assert.AreEqual(field.thiefCount + HatchBank.Carry3ExtraThief, hatch.thiefCount);
            Assert.AreEqual(field.defaultCount, hatch.defaultCount);
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
            Assert.AreEqual(unbanked - 8L, sim.Unbanked);
            Assert.AreEqual(HatchBank.BankedFrom(8L, HatchBank.CarryConvert(1)), meta.BankedSalvage);
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

        [Test]
        public void BuyBackRefuse_LiveWindow_AllowsGuestWhoCanPay()
        {
            Assert.AreEqual(450L, HatchBank.BuyBackCost);
            Assert.IsTrue(HatchBank.IsBuyBackWindow(WaveMode.HatchBank, AssaultState.Assault));
            Assert.IsFalse(HatchBank.IsBuyBackWindow(WaveMode.HatchBank, AssaultState.Prep));
            Assert.IsFalse(HatchBank.IsBuyBackWindow(WaveMode.Terminal, AssaultState.Assault));
            Assert.IsTrue(HatchBank.IsFreeEmbarkWindow(WaveMode.HatchBank, AssaultState.Prep));
            Assert.IsFalse(HatchBank.IsFreeEmbarkWindow(WaveMode.HatchBank, AssaultState.Assault));
            Assert.IsNull(HatchBank.BuyBackRefuse(
                false, true, WaveMode.HatchBank, AssaultState.Assault, HatchBank.BuyBackCost));
            Assert.AreEqual("no live window", HatchBank.BuyBackRefuse(
                false, true, WaveMode.HatchBank, AssaultState.Prep, HatchBank.BuyBackCost));
            Assert.AreEqual("no live window", HatchBank.BuyBackRefuse(
                false, true, WaveMode.Terminal, AssaultState.Assault, HatchBank.BuyBackCost));
            Assert.AreEqual("cannot afford", HatchBank.BuyBackRefuse(
                false, true, WaveMode.HatchBank, AssaultState.Assault, HatchBank.BuyBackCost - 1));
            Assert.AreEqual("host cannot buy back", HatchBank.BuyBackRefuse(
                true, true, WaveMode.HatchBank, AssaultState.Assault, HatchBank.BuyBackCost));
            Assert.AreEqual("not waiting", HatchBank.BuyBackRefuse(
                false, false, WaveMode.HatchBank, AssaultState.Assault, HatchBank.BuyBackCost));
        }
    }
}
