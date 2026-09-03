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

        /// <summary>What <paramref name="kills"/> bodies pay at dial 0, DERIVED from the live
        /// price of a kill. This fixture was written when that price was 25 and eight kills were
        /// the round 200 that half its test names still referred to; SalvagePerKill 25 -> 42 on
        /// 2026-08-27 moved every one of those figures at once. A kill is priced FLAT — it never
        /// reads the body it came off — so a wave's take is nothing but count times this.</summary>
        static long Award(int kills) => kills * (long)MetaUpgrades.SalvagePerKill();

        /// <summary>A field Wave run and cleared, leaving its pay STAKED — kills bank as they
        /// land now (Zo, 2026-08-25), so the pile a Convert window needs is withdrawn back out
        /// through the real Hatch verb rather than being left behind by the kills.</summary>
        static void TerminalKillsThenPrep(RunSim sim, int kills)
        {
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, kills);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
        }

        /// <summary>The same Wave, but the pay LEFT IN THE BANK — for the assertions about a
        /// Convert window that has nothing to convert.</summary>
        static void TerminalKillsThenPrepUnstaked(RunSim sim, int kills)
        {
            sim.TriggerDraw();
            Kill(sim, kills);
            sim.NotifyAssaultCleared();
        }

        static void RunHatchBankWindow(RunSim sim)
        {
            sim.Tick(HatchBank.DurationSeconds);
        }


        // ================================================================
        //  Walking to the terminal ENDS the wave, and banks (Zo, 2026-08-25)
        // ================================================================

        [Test]
        public void StakingLastRunsSalvage_OnlyCountsTheBonusAsThisRunsIncome()
        {
            // The pre-funded-bank door. A Run that stakes salvage a PREVIOUS Run banked has
            // taken nothing out of its own tally, so an Unearn that merely floored at zero
            // would let the conversion back re-Earn the whole pile as fresh income — the same
            // double-count Unearn exists to stop, arriving from the other side. The end
            // screen's grossEarned reads this tally, so it would have reported money the Run
            // never made.
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);          // last Run's money, before this one earns anything
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "this Run has earned nothing yet");

            RunEconomyFixture.Stake(sim, 400L);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage,
                "staking last Run's money must not push this Run's tally below nothing");

            // Convert it back. Face value returns the debt and nothing more; only a carry
            // multiplier above 1.0 would be income, and this window carries none.
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
            Assert.AreEqual(0L, sim.TotalUnbanked, "the window did not convert the stake");
            Assert.AreEqual(0L, sim.Salvage.RunSalvage,
                "converting last Run's own money back reported it as this Run's income");
        }

        [Test]
        public void Withdraw_ToAnUncountedSeat_DestroysNothing()
        {
            // PlayerWallets.Add silently returns for a slot the sim is not counting, so a
            // Withdraw that debited the bank before checking its destination would take the
            // salvage out of the bank AND out of the Run tally and hand it to nobody, with no
            // log. Reachable in the window where a seat has joined but RefreshPlayerCount has
            // not caught up — the kind of leak that reads as "the numbers are just wrong".
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            long bank = meta.BankedSalvage;
            long tally = sim.Salvage.RunSalvage;

            Assert.AreEqual(1, sim.PlayerCount, "solo: slot 1 is not a counted seat");
            Assert.AreEqual(0L, sim.Withdraw(1f, 1), "an uncounted seat moved units");
            Assert.AreEqual(bank, meta.BankedSalvage, "the refused withdraw still spent the bank");
            Assert.AreEqual(tally, sim.Salvage.RunSalvage, "the refused withdraw unearned anyway");
            Assert.AreEqual(0L, sim.TotalUnbanked, "units landed in a pile that is not counted");
        }

        [Test]
        public void ShutOffFieldWave_ReturnsToPrep_AndBanksEverythingUnbanked()
        {
            // The spine's loop is "start at the Hatch, end at a terminal". Reaching the
            // terminal is the whole walk's payoff, so the pile must not still be sitting
            // unbanked afterwards, waiting on a second trip back to the Hatch.
            //
            // Re-key 2026-08-25: the eight kills used to BE the carried pile. They bank on
            // landing now, so the pile the terminal secures has to be staked deliberately —
            // and a staked pile is the only thing left that a walk to the switch can save.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 8);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            sim.TriggerDraw();
            long carried = sim.TotalUnbanked;
            long banked = sim.Salvage.RunSalvage;
            Assert.Greater(carried, 0L, "nothing staked — the fixture proves nothing.");

            Assert.IsTrue(sim.ShutOffFieldWave(), "the terminal did not end the wave.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the shutoff did not reach Prep.");
            Assert.AreEqual(0L, sim.TotalUnbanked, "the terminal left the pile unbanked.");
            // MultLow is 1.00, so a terminal shutoff banks the pile at face value: it is a
            // walk, not a Hatch conversion, and must not pay a carry bonus on top.
            Assert.AreEqual(banked + carried, sim.Salvage.RunSalvage,
                "the terminal banked something other than exactly what was carried.");
        }

        [Test]
        public void ShutOffFieldWave_RefusesAHatchBankWindow_AndKeepsTheStake()
        {
            // A bank window is the one wave with a stake on it: its whole tension is that it
            // cannot be walked away from. An off switch there would make the ante free.
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            long staked = sim.TotalUnbanked;
            Assert.Greater(staked, 0L, "no stake — the window under test is not the real one.");

            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.IsFalse(sim.ShutOffFieldWave(), "the bank window accepted an off switch.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the refused shutoff left Assault.");
            Assert.AreEqual(staked, sim.TotalUnbanked, "the refused shutoff moved the stake.");
        }

        [Test]
        public void ShutOffFieldWave_InPrep_IsRefused_AndBanksNothing()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            long carried = sim.TotalUnbanked;

            Assert.IsFalse(sim.ShutOffFieldWave(), "Prep has no wave to shut off.");
            Assert.AreEqual(carried, sim.TotalUnbanked,
                "a refused shutoff banked the pile anyway — the refusal has a side effect.");
        }

        [Test]
        public void Zone2WindowResolved_OpensThePrimaryRack_EitherWay()
        {
            // Zone 2 RESOLVED — survived here (the lost half is EndRun's twin write) —
            // sets DemoZone2BankLost, the demo's Primary-rack gate (Zo, 2026-08-25).
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(meta.DemoActive, "a fresh realm runs the demo");

            // A zone-1 window (waves 1-2) survived must NOT open the rack.
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            RunHatchBankWindow(sim);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "a zone-1 window survived does not resolve zone 2");

            // Field waves 3 (zone 2 starts at wave 3), then a survived window: rack opens.
            TerminalKillsThenPrep(sim, 8);
            sim.TriggerDraw(WaveMode.HatchBank);
            RunHatchBankWindow(sim);
            Assert.IsTrue(meta.DemoZone2BankLost,
                "a zone-2 window survived resolves zone 2 and opens the rack");
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
        public void MasterTerminalUnbanked_IsTerminalWavesOfWaveFiveClears()
        {
            var director = new WaveDirector();
            long perKill = (long)System.Math.Floor((double)MetaUpgrades.SalvagePerKill());
            long waveFive = director.NextWave(WaveDirector.MaxWaves - 1, 0, 1).enemyCount * perKill;
            Assert.AreEqual(waveFive * MasterControl.TerminalWaves, HatchBank.MasterTerminalUnbanked());

            // THE LITERAL IS GONE. It read 8250 with "66 bodies x 25 x 5 terminal windows" beside
            // it, and on 2026-08-27 the roster cut took 66 to 39 while SalvagePerKill took 25 to
            // 42 — so both factors in the comment were wrong and the product moved to 8190. The
            // line above already derives the whole figure; a second copy could only ever disagree
            // with it. What is worth asserting beside it is the RELATION: the master pile is
            // TerminalWaves deepest waves (five until 2026-09-03, two since), so it must be a
            // multiple of one and strictly more than one.
            Assert.Greater(HatchBank.MasterTerminalUnbanked(), waveFive,
                "the master pile is more than a single wave's take");
            Assert.AreEqual(0L, HatchBank.MasterTerminalUnbanked() % waveFive,
                "and it is a whole number of them");
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
        public void AwardKill_BanksOnLanding_AndCarriesNothing()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual(0L, sim.Unbanked,
                "the kill left something carried — nothing may sit at risk that the player "
                + "did not choose to put at risk.");
            Assert.AreEqual(Award(1), meta.BankedSalvage,
                "the kill did not reach the realm's bank.");
            Assert.AreEqual(Award(1), sim.Salvage.RunSalvage,
                "the Run's tally must move with the bank, at face value — a kill carries no "
                + "conversion multiplier because nothing was carried.");
        }

        [Test]
        public void PlayerDown_AfterKills_StillLeavesTheRunAhead()
        {
            // THE INVARIANT (Zo, 2026-08-25): it must be impossible to die with no progress to
            // show for it. Before this rule the same eight kills and the same death left the
            // realm exactly where it started.
            MetaState meta;
            var sim = Tethered(out meta);
            long before = meta.BankedSalvage;
            sim.TriggerDraw();
            Kill(sim, 8);
            sim.NotifyPlayerDown();
            Assert.Greater(meta.BankedSalvage, before,
                "a Run with eight kills in it ended with nothing banked.");
            Assert.AreEqual(before + Award(8), meta.BankedSalvage,
                "the eight kills banked something other than their face value.");
        }

        [Test]
        public void PlayerDown_WipesUnbanked_KeepsShipBank()
        {
            // A pile that was deliberately STAKED is still wiped by death — that is what
            // makes the Withdraw a bet. What may not be wiped is the bank behind it.
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            // The withdraw accrues in whole units per tick, so it lands on or just past the ask;
            // the exact figure it moved is what the two sides of the ledger are checked against.
            long staked = RunEconomyFixture.Stake(sim, 50L);
            Assert.GreaterOrEqual(staked, 50L);
            Assert.AreEqual(staked, sim.Unbanked);
            Assert.AreEqual(1000L - staked, meta.BankedSalvage, "the stake came out of the bank.");
            sim.TriggerDraw();
            sim.NotifyPlayerDown();
            Assert.AreEqual(0L, sim.Unbanked, "the staked pile survived the death.");
            Assert.AreEqual(1000L - staked, meta.BankedSalvage, "death reached past the stake.");
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
        }

        /// <summary>Renamed FROM NibbleThreeTimes200_Banks600AtOneTimes. Both numbers were eight
        /// kills at 25 salvage; the same eight kills now pay 336, so the name described an
        /// arithmetic that no longer happens. The CLAIM is that banking early and often converts
        /// at x1.00 every time — carry never accumulates when the pile is emptied each window —
        /// and that survives any price of a kill.</summary>
        [Test]
        public void NibbleEachWave_BanksEveryPileAtOneTimes_CarryNeverBuilds()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            long ship = 0L;
            for (int i = 0; i < 3; i++)
            {
                TerminalKillsThenPrep(sim, 8);
                Assert.AreEqual(Award(8), sim.Unbanked);
                sim.TriggerDraw(WaveMode.HatchBank);
                Assert.AreEqual(HatchBank.CarryConvert(1), sim.HatchConvertMultiplier, 1e-5f);
                Assert.AreEqual(1.00f, sim.HatchConvertMultiplier, 1e-5f,
                    "nibbling must never pay a carry bonus - that is what makes waiting a bet");
                RunHatchBankWindow(sim);
                ship += Award(8);
                Assert.AreEqual(0L, sim.Unbanked);
                Assert.AreEqual(ship, meta.BankedSalvage);
                if (i < 2) sim.SeekWave(1);
            }
            Assert.AreEqual(Award(24), meta.BankedSalvage,
                "three nibbled windows bank exactly what the kills paid, and not a unit more");
        }

        /// <summary>Renamed FROM ThreeWaves_CarryConvert_Banks750: 750 was 24 kills at 25 salvage
        /// times MultMid, and every factor except MultMid moved on 2026-08-27. The claim is that
        /// holding three waves converts the WHOLE pile at the mid multiplier, once.</summary>
        [Test]
        public void ThreeWaves_CarryConvert_BanksTheWholePileAtMultMid()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            long pile = sim.Unbanked;
            Assert.AreEqual(Award(24), pile, "three waves of eight, all staked");
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(3, sim.HatchCarry);
            Assert.AreEqual(HatchBank.MultMid, sim.HatchConvertMultiplier, 1e-5f);
            RunHatchBankWindow(sim);
            Assert.AreEqual(0L, sim.Unbanked);

            long converted = HatchBank.BankedFrom(pile, HatchBank.MultMid);
            Assert.AreEqual(converted, meta.BankedSalvage);
            Assert.Greater(converted, pile, "MultMid has to actually pay a bonus, or waiting is free");

            // The bank and the tally agree. The kills banked the pile, the Withdraw took it back
            // OUT of the tally (SalvageLedger.Unearn) because it left the bank, and the Convert
            // returned it multiplied. Counting the kills AND the Convert would report this Run's
            // inflow as pile + converted — 2.25x what the realm actually gained — and EndRun's net
            // would carry the inflation with it.
            Assert.AreEqual(converted, sim.Salvage.RunSalvage,
                "the Run tally is what the realm gained, counted once");
            Assert.Less(sim.Salvage.RunSalvage, pile + converted,
                "the tally is counting the kills and the Convert both");
        }

        /// <summary>Renamed FROM ThreeWaves_DripThenDump_Still750 for the same reason: the
        /// destination is "the same total as converting in one go", not the number 750.</summary>
        [Test]
        public void ThreeWaves_DripThenDump_ReachesTheSameTotal()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            TerminalKillsThenPrep(sim, 8);
            long pile = sim.Unbanked;
            sim.TriggerDraw(WaveMode.HatchBank);

            const float dripSeconds = 10f;
            long dripped = (long)(HatchBank.DripPerSecond * dripSeconds);
            sim.Tick(dripSeconds);
            Assert.AreEqual(pile - dripped, sim.Unbanked, "the drip left the pile short");
            Assert.AreEqual(HatchBank.BankedFrom(dripped, HatchBank.MultMid), meta.BankedSalvage);
            Assert.Greater(sim.Unbanked, 0L,
                "the drip emptied the pile inside 10 s and the dump half is untested");

            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "timer, not enemy wipe");
            sim.Tick(HatchBank.DurationSeconds - dripSeconds);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(HatchBank.BankedFrom(pile, HatchBank.MultMid), meta.BankedSalvage,
                "dripping and dumping must reach the same total as converting in one go");
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
            Assert.AreEqual(Award(8), sim.Unbanked);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            RunHatchBankWindow(sim);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(Award(32), meta.BankedSalvage,
                "four windows of eight kills, every one converting at x1.00");
        }

        [Test]
        public void HatchBank_ZeroUnbanked_Refused()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);

            // Re-key 2026-08-25: an empty bank was the only way to reach this refusal before,
            // which made it indistinguishable from "the player has no money". The interesting
            // case is now the other one — a funded realm that simply has not staked anything.
            // A Convert is a bet, and there is nothing here to bet.
            TerminalKillsThenPrepUnstaked(sim, 8);
            Assert.AreEqual(Award(8), meta.BankedSalvage, "the kills banked; none of it is staked");
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "a funded bank is not a stake");
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
            long staked = sim.Unbanked;
            Assert.AreEqual(Award(24), staked);
            float hp = sim.Ship.Hatch.Hp;

            const float blow = 50f;
            sim.DamageHatch(blow);
            Assert.AreEqual(hp - blow, sim.Ship.Hatch.Hp, 0.01f);
            Assert.AreEqual(staked - HatchBank.AttritUnits(blow), sim.Unbanked,
                "a hit on the Hatch has to come out of the pile it is protecting");
            Assert.Less(sim.Unbanked, staked, "the blow attrited nothing");
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void DamageHatch_InTerminal_LandsAndBitesTheStake()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            // REVERSED 2026-08-27 (Zo): this used to pin the opposite — a field wave could
            // neither hurt the Hatch nor touch a carried stake. The Hatch is in the fight on
            // both modes now, and attrition rides the same call, so BOTH halves flip together.
            // That pairing is the point of keeping this fixture: if a later change re-guards
            // one without the other, the Hatch and the pile it protects have drifted apart.
            //
            // Kills bank as they land, so the only way a field wave has a pile at risk is a
            // stake made in Prep and carried INTO the terminal draw.
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 2);
            sim.NotifyAssaultCleared();
            long staked = RunEconomyFixture.Stake(sim, earned);
            sim.TriggerDraw();
            float hp = sim.Ship.Hatch.Hp;
            sim.DamageHatch(25f);
            Assert.AreEqual(hp - 25f * HatchBank.FieldWaveDamageScale, sim.Ship.Hatch.Hp, 0.01f,
                "a field wave could not hurt the Hatch");
            Assert.Less(sim.Unbanked, staked,
                "the Hatch took a hit and the carried stake did not pay for it");
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
            // 40 banked, the rest of the pile wiped with the breach — and gross says 40, not the
            // whole stake. What the kills banked left the tally when the player withdrew it to
            // stake it, so what a breach destroys never reads as income. The report is the loss,
            // honestly. (The figures used to be spelled out as 560 and 640; 24 kills pay 1008
            // rather than 600 since 2026-08-27, and the claim was never about those numbers.)
            Assert.AreEqual(40L, sim.LastRunReport.grossEarned,
                "gross is what the realm kept, not what passed through it");
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
            // Wave 5's clear does not END the Run — and the pay is already the realm's,
            // because kills bank on landing.
            Assert.Greater(meta.BankedSalvage, 0L, "five Waves of kills banked nothing.");
            Assert.AreEqual(0L, sim.Unbanked, "nothing was staked, so nothing may be carried.");
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
            // Re-key 2026-08-25: five cleared waves leave the take BANKED, not carried, so the
            // pile this window is about is the whole run's earnings pulled back out at the
            // Hatch — the deepest bet the demo can offer, which is what carry 5 is priced for.
            RunEconomyFixture.Stake(sim, meta.BankedSalvage);
            long pile = sim.Unbanked;
            Assert.Greater(pile, 0L);
            Assert.AreEqual(0L, meta.BankedSalvage, "the whole run is on the table");
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
            // Re-key 2026-08-25: without a stake the window is refused and this test quietly
            // became "five waves then a sixth" — the thing it is named for never happened.
            RunEconomyFixture.Stake(sim, meta.BankedSalvage);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the window under test must open");
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
            // Re-key 2026-08-25: the window's COMPOSITION is the subject, but a window with
            // nothing staked is refused outright and never draws — so the carry-5 shape can
            // only be read after the run's take is pulled back out at the Hatch.
            RunEconomyFixture.Stake(sim, meta.BankedSalvage);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(5, sim.HatchCarry);
            Assert.AreEqual(director.NextWave(4, 0).enemyCount, hatch.enemyCount);
        }

        [Test]
        public void HatchBank_UnbankedZero_DoesNotEndWindow()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            // THE STAKE IS SIZED TO THE HATCH, NOT TO A KILL COUNT. Attrition is 1:1 with hatch
            // damage, so emptying the pile costs the Hatch exactly the pile's worth — and this
            // test needs the pile gone WITHOUT a breach, because a breach ends the window for a
            // different reason and would prove nothing about the timer.
            //
            // It used to stake eight kills and swing the literal 200f, which was exactly what
            // those eight kills paid at 25 salvage. At 42 the same eight kills stake 336 against
            // a 300 hp Hatch: the old blow would have left 136 in the pile (so "unbanked zero"
            // never happened) and a blow big enough to clear it would breach. So the kill count is
            // derived from the Hatch instead of chosen.
            int kills = 1;
            while (Award(kills + 1) < ShipSystems.HatchMaxHp) kills++;
            TerminalKillsThenPrep(sim, kills);
            sim.TriggerDraw(WaveMode.HatchBank);

            long staked = sim.Unbanked;
            Assert.Greater(staked, 0L, "no stake, so 'unbanked reaches zero' is true on arrival");
            Assert.Less((float)staked, sim.Ship.Hatch.Hp,
                "the pile is worth more than the Hatch is - it cannot be attrited without a breach");

            sim.DamageHatch(staked);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.Greater(sim.Ship.Hatch.Hp, 0f, "the Hatch breached - this is the wrong ending");
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

    }
}
