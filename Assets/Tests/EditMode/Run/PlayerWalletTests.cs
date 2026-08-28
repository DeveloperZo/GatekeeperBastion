using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Per-seat piles. RE-KEYED 2026-08-25 (Zo, kills bank as they land): a kill no
    /// longer splits into the piles at all — it banks whole, at face value, into the realm.
    /// The piles still exist and still convert per-seat, but they are filled by the deliberate
    /// Withdraw at the Hatch, so every fixture below that used kills as a pile-filler now
    /// stakes through the real verb. The equal-split rule itself did not die with the kill
    /// path: <see cref="RunSim"/>'s endure bonus still runs through
    /// <see cref="PlayerWallets.AwardSplit"/>, so it is pinned directly here instead.</summary>
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

        /// <summary>Fill ONE seat's pile by holding the real slot-aware Withdraw. The shared
        /// <see cref="RunEconomyFixture.Stake"/> only aims at slot 0; a co-op fixture needs
        /// to say which seat is at the Hatch.</summary>
        static long StakeInto(RunSim sim, int slot, long want)
        {
            long before = sim.UnbankedOf(slot);
            long target = before + want;
            int steps = 0;
            while (sim.UnbankedOf(slot) < target)
            {
                if (sim.Withdraw(0.25f, slot) <= 0L)
                {
                    Assert.Fail("Withdraw refused for slot " + slot + " with "
                        + (target - sim.UnbankedOf(slot)) + " of " + want + " still to move"
                        + " — bank=" + sim.BankedSalvage + ", phase=" + sim.Phase
                        + ", assault=" + sim.Assault);
                }
                if (++steps > 4000) Assert.Fail("Withdraw never reached " + want);
            }
            return sim.UnbankedOf(slot) - before;
        }

        [Test]
        public void TakeTotal_SkipsADownSeat_EntirelyAndForLeftover()
        {
            var w = new PlayerWallets();
            w.SetCount(2);
            w.Add(0, 100L);
            w.Add(1, 400L);
            w.SetDown(1, true);

            // Ask for more than the live pile holds: slot 1 must not be touched, even by
            // the leftover pass — its pile is the purse its respawn is bought with.
            var taken = w.TakeTotal(300L);
            Assert.AreEqual(100L, taken[0], "the live seat gave its whole pile");
            Assert.AreEqual(0L, taken[1], "the down seat gave nothing");
            Assert.AreEqual(0L, w.Of(0));
            Assert.AreEqual(400L, w.Of(1), "the frozen pile is intact");
        }

        [Test]
        public void TakeTotal_ThawedSeatParticipatesAgain()
        {
            var w = new PlayerWallets();
            w.SetCount(2);
            w.Add(0, 100L);
            w.Add(1, 100L);
            w.SetDown(1, true);
            w.SetDown(1, false);

            var taken = w.TakeTotal(100L);
            Assert.AreEqual(50L, taken[0]);
            Assert.AreEqual(50L, taken[1], "a thawed pile converts like any other");
        }

        [Test]
        public void AwardSplit_ThreePlayers_SplitsEqually_RemainderToHost()
        {
            // The equal split used to be pinned through AwardKill. The kill path no longer
            // splits anything (2026-08-25), but the endure bonus still pays through this
            // verb, so the rule keeps its pin at the level where it still runs.
            var w = new PlayerWallets();
            w.SetCount(3);
            w.AwardSplit(25L);
            Assert.AreEqual(9L, w.Of(0), "the odd unit goes to the host, not to the floor");
            Assert.AreEqual(8L, w.Of(1));
            Assert.AreEqual(8L, w.Of(2));
            Assert.AreEqual(25L, w.Total, "no salvage is destroyed by the division");
        }

        /// <summary>Renamed FROM AwardKill_Solo_BanksTwentyFive_AndCarriesNothing on 2026-08-27.
        /// SalvagePerKill went 25 -> 42 with the roster cut — a kill is priced FLAT and never reads
        /// the body it came off, so cutting the roster ~40% would otherwise have cut run income by
        /// the same 40% as a side effect. The number in the name was the half of this pin that had
        /// no business being pinned; the FLATNESS is the claim.</summary>
        [Test]
        public void AwardKill_Solo_BanksTheFlatAward_AndCarriesNothing()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual(1, sim.PlayerCount);
            Assert.AreEqual((long)MetaUpgrades.SalvagePerKill(), meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(0L, sim.TotalUnbanked);
        }

        [Test]
        public void AwardKill_ThreePlayers_BanksTheWholeAward_Undivided()
        {
            // A kill is credited to the REALM, and the realm is one pot however many seats
            // are in it — so the seat count no longer divides the award, and the piles that
            // used to receive the thirds stay empty until somebody withdraws.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual((long)MetaUpgrades.SalvagePerKill(), meta.BankedSalvage,
                "three seats bank the SAME award a solo seat does - the realm is one pot");
            Assert.AreEqual(0L, sim.UnbankedOf(0));
            Assert.AreEqual(0L, sim.UnbankedOf(1));
            Assert.AreEqual(0L, sim.UnbankedOf(2));
        }

        [Test]
        public void WipeSlot_GuestDown_KeepsOthers()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            sim.TriggerDraw();
            for (int i = 0; i < 8; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();

            // Three seats each holding a staked pile: the wipe has to find slot 1 and only
            // slot 1, which a shared pot could never show.
            // Three unequal piles, so a wipe that hit the wrong seat could not hide behind a
            // matching number. The withdraw drip lands in whole units and may overshoot the
            // ask, so the piles are read back rather than assumed.
            StakeInto(sim, 0, 60L);
            StakeInto(sim, 1, 50L);
            StakeInto(sim, 2, 40L);
            long seat0 = sim.UnbankedOf(0);
            long seat2 = sim.UnbankedOf(2);
            Assert.Greater(sim.UnbankedOf(1), 0L, "slot 1 has something to lose");

            sim.WipeUnbankedSlot(1);
            Assert.AreEqual(seat0, sim.UnbankedOf(0));
            Assert.AreEqual(0L, sim.UnbankedOf(1));
            Assert.AreEqual(seat2, sim.UnbankedOf(2));
        }

        [Test]
        public void BankCap_Deepest2_IsWavesOneAndTwoAtDial0()
        {
            // The cap is "what the waves you have actually reached could have paid, at dial 0".
            // BOTH factors moved on 2026-08-27 and in opposite directions — the roster fell ~40%
            // and SalvagePerKill rose 5/3 — so the pair of literals here (18 and 25) would have
            // been re-typed to 11 and 42 and been just as brittle. Read off the source instead.
            var director = new WaveDirector();
            long perKill = (long)MetaUpgrades.SalvagePerKill();
            long waveOne = director.NextWave(0, 0).enemyCount * perKill;
            long waveTwo = director.NextWave(1, 0).enemyCount * perKill;

            Assert.AreEqual(waveOne, HatchBank.BankCap(1));
            Assert.AreEqual(waveOne + waveTwo, HatchBank.BankCap(2));

            // ...and the cap has to actually grow with depth, or "capped by its own depth" is a
            // sentence about a constant.
            Assert.Greater(HatchBank.BankCap(2), HatchBank.BankCap(1));
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
            sim.NotifyAssaultCleared();

            // Each seat's cap is set by its OWN depth, so seat 1 may convert more than the
            // other two. Every pile here is over its cap, which is what makes the cap the
            // thing being measured rather than the pile. The piles are staked out of the
            // bank those 200 kills filled — the route the player now takes to put anything
            // at risk at all.
            StakeInto(sim, 0, 600L);
            StakeInto(sim, 1, 1500L);
            StakeInto(sim, 2, 600L);
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
            long earned = RunEconomyFixture.KillAndStake(sim, 30);
            sim.NotifyAssaultCleared();
            long staked = RunEconomyFixture.Stake(sim, earned);

            // 30 kills is deliberately PAST BankCap(1): a solo seat that hit the cap would
            // bank only 450 back, so the whole stake returning is what proves the cap is
            // never reached for. (The old shape staked exactly one wave's worth, which the
            // cap would have passed either way.)
            Assert.Greater(staked, HatchBank.BankCap(1),
                "the stake has to exceed the cap for its absence to be visible");
            long potBefore = meta.BankedSalvage;

            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
            Assert.AreEqual(staked, meta.BankedSalvage - potBefore);
        }

        /// <summary>Renamed FROM LaunchWave_ThreePlayers_SendsFiftyFour: 54 was three times the
        /// old 18-body wave 1, and the roster cut took it to 33. The claim was never the number —
        /// it is that a three-seat draw is exactly THREE TIMES the solo draw.</summary>
        [Test]
        public void LaunchWave_ThreePlayers_SendsThreeTimesTheSoloWave()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetPlayerCount(3);
            WaveSpec spec = default;
            sim.WaveStarted += s => spec = s;
            sim.TriggerDraw();
            Assert.AreEqual(3 * new WaveDirector().NextWave(0, 0, 1).enemyCount, spec.enemyCount);
        }
    }
}
