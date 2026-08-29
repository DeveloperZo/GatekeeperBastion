using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The respawn rule: a dead seat buys its way back at an escalating price, and
    /// nothing else about the run's money moves.
    /// <para>RE-KEYED 2026-08-25 (Zo, kills bank as they land). The purse moved: the fee used
    /// to come out of the dying seat's own carry, which only worked while kills filled that
    /// carry for free. They bank now, so the carry is empty unless the seat deliberately
    /// Withdrew, and charging it would have meant a player who never gambled could never
    /// stand back up — exactly the no-progress death the rule change exists to forbid. The
    /// fee therefore charges <see cref="RunSim.BankedSalvage"/>. Every "out of its own pile"
    /// pin below is kept, pointed the other way: the fee comes from the bank and a staked
    /// carry is what must now survive the death untouched.</para></summary>
    public class RespawnsTests
    {
        static RunSim Tethered(out MetaState meta, int seats = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, 1);
            sim.SetPlayerCount(seats);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        /// <summary>Fill the bank the way the game does — kills, not a test-only setter, so
        /// these fixtures break if the earning path stops feeding the purse the rule spends.
        /// AwardKill pays only while a wave is on the floor, so this draws one first.</summary>
        static void Kills(RunSim sim, int n)
        {
            if (sim.Assault != AssaultState.Assault) sim.TriggerDraw();
            for (int i = 0; i < n; i++) sim.AwardKill();
        }

        /// <summary>The most kills a seat can bank and STILL not afford the base fee, and the
        /// one kill that tips it over. Both are searched for off the two live constants.
        ///
        /// They were written as "6 kills, 150 banked, exactly the base fee" and "5 kills, 125
        /// banked, one kill short". SalvagePerKill 25 -> 42 on 2026-08-27 broke both: six kills
        /// now bank 252 (so "exactly the fee" is 68% over it) and five bank 210, which AFFORDS the
        /// fee the test needed it to miss. Neither number could be re-typed without picking a new
        /// kill count by hand, and the price of a kill no longer divides the fee at all — so the
        /// boundary is found rather than named, and both sides of it are exercised.</summary>
        static int KillsJustShortOfTheFee()
        {
            long perKill = (long)MetaUpgrades.SalvagePerKill();
            Assert.Greater(perKill, 0L, "a kill has to be worth something");
            int kills = 0;
            while ((kills + 1) * perKill < Respawns.BaseCost) kills++;
            return kills;
        }

        static int KillsThatJustAffordTheFee() => KillsJustShortOfTheFee() + 1;

        /// <summary>Kills, then back to Prep so a carry can be staked out of what they banked.
        /// The slot-aware Withdraw is the only filler of a per-seat pile now.</summary>
        static long StakeInto(RunSim sim, int slot, long want)
        {
            long before = sim.UnbankedOf(slot);
            long target = before + want;
            int steps = 0;
            while (sim.UnbankedOf(slot) < target)
            {
                if (sim.Withdraw(0.25f, slot) <= 0L)
                    Assert.Fail("Withdraw refused for slot " + slot + " — bank="
                        + sim.BankedSalvage + ", phase=" + sim.Phase + "/" + sim.Assault);
                if (++steps > 4000) Assert.Fail("Withdraw never reached " + want);
            }
            return sim.UnbankedOf(slot) - before;
        }

        [Test]
        public void CostFor_EscalatesLinearlyFromBase()
        {
            Assert.AreEqual(150L, Respawns.BaseCost);
            Assert.AreEqual(150L, Respawns.CostFor(0));
            Assert.AreEqual(300L, Respawns.CostFor(1));
            Assert.AreEqual(450L, Respawns.CostFor(2));
            Assert.AreEqual(150L, Respawns.CostFor(-5), "negative taken clamps to the base");
        }

        // ================================================================
        //  Bleeding out (Zo, 2026-08-28)
        // ================================================================

        [Test]
        public void GoingDown_StartsTheBleedClock_AndItExpiresOnce()
        {
            // There was no clock at all before this: the DOWN overlay waited on ENTER forever,
            // so a death cost exactly as long as the player felt like taking. The countdown is
            // what makes buying back a decision under pressure.
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsFalse(sim.BleedOutLive, "the clock was running before anyone went down");

            sim.SetSeatDown(0, true);
            Assert.IsTrue(sim.BleedOutLive);
            Assert.AreEqual(Respawns.BleedOutSeconds, sim.BleedOutRemaining, 1e-4f);

            sim.Tick(Respawns.BleedOutSeconds * 0.5f);
            Assert.IsFalse(sim.BleedOutExpired, "the clock expired at the halfway mark");
            Assert.Greater(sim.BleedOutRemaining, 0f);

            sim.Tick(Respawns.BleedOutSeconds);
            Assert.IsTrue(sim.BleedOutExpired, "the clock ran out and nothing noticed");
            Assert.AreEqual(0f, sim.BleedOutRemaining, 1e-4f,
                "the clock ran past zero — the HUD would count into negatives");
        }

        [Test]
        public void TheSimReportsTheExpiry_ButNeverEndsTheRunItself()
        {
            // THE BOUNDARY IS THE POINT. Whether bleeding out ends the Boarding depends on a
            // crewmate still standing, and that is not a fact Sim/ holds — GameController owns
            // it (AnyRemoteSeatFighting). If this ever starts ending runs on its own, a co-op
            // revive becomes unreachable and solo would be the only mode the rule is correct in.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SetSeatDown(0, true);
            sim.Tick(Respawns.BleedOutSeconds * 2f);

            Assert.IsTrue(sim.BleedOutExpired);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase,
                "the sim ended the run on its own — the Game layer's revive decision is gone");
        }

        [Test]
        public void StandingBackUp_StopsTheClock_AndASecondSeatCannotRefillIt()
        {
            MetaState meta;
            var sim = Tethered(out meta, seats: 2);
            Kills(sim, 20);
            sim.SetSeatDown(0, true);
            sim.Tick(Respawns.BleedOutSeconds * 0.5f);
            float afterFirst = sim.BleedOutRemaining;

            // A party dropping one at a time must not buy fresh time by taking turns.
            sim.SetSeatDown(1, true);
            Assert.AreEqual(afterFirst, sim.BleedOutRemaining, 1e-4f,
                "a second seat going down refilled the clock — the run could bleed forever");

            Assert.IsTrue(sim.TryRespawn(0, out long _));
            Assert.IsFalse(sim.BleedOutLive, "the clock survived a seat standing back up");
            Assert.IsFalse(sim.BleedOutExpired, "a stale expiry survived the respawn");
        }

        [Test]
        public void Refuse_NamesEveryDisqualifier()
        {
            // The third argument is the purse, and since 2026-08-25 RunSim passes the bank
            // into it. The fourth is respawns already taken (2026-08-28). The refuse STRING
            // reads "not enough banked" — that is the sim's wording to change, not this
            // pin's, so it is asserted as it stands.
            Assert.AreEqual("no run", Respawns.Refuse(RunPhase.Docked, 150L, 1000L, 0));
            Assert.AreEqual("not enough banked",
                Respawns.Refuse(RunPhase.Tethered, 150L, 149L, 0));
            Assert.IsNull(Respawns.Refuse(RunPhase.Tethered, 150L, 150L, 0),
                "exactly the price is affordable");

            // THE CAP OUTRANKS THE PURSE. A rich player must still be stopped: the 07:17 run
            // held 12,660 banked, which is eighty-four returns at the top price, so the fee
            // alone could never end a Boarding. Asserted with an unlimited purse precisely so
            // affordability cannot be what produces the refusal.
            Assert.AreEqual("out of respawns",
                Respawns.Refuse(RunPhase.Tethered, 150L, long.MaxValue, Respawns.MaxRespawns),
                "the cap did not stop a seat that could afford the fee");
            Assert.IsNull(
                Respawns.Refuse(RunPhase.Tethered, 150L, 150L, Respawns.MaxRespawns - 1),
                "the last respawn inside the cap was refused");
        }

        [Test]
        public void Refuse_ReadsTheRealmsBank()
        {
            // INVERTED 2026-08-25. This pin was Refuse_IsBlindToTheRealmsBank, and while the
            // carry was the ration that was the whole point — a rich realm could not rescue
            // an empty-handed seat. Kills bank now, so the bank IS what a seat has been
            // earning all run, and blindness to it would mean a player who never gambled
            // could never buy a return no matter how well the run had gone.
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.AreEqual("not enough banked", sim.RespawnRefuse(0),
                "an empty realm still buys nobody a return");

            // One kill short is still a refusal — that is the half the old "exactly the base
            // fee" setup could not show, because it landed on the fee and never approached it.
            Kills(sim, KillsJustShortOfTheFee());
            Assert.Less(meta.BankedSalvage, Respawns.BaseCost);
            Assert.AreEqual("not enough banked", sim.RespawnRefuse(0),
                "one kill short of the fee still buys nobody a return");

            Kills(sim, 1); // the kill that tips it over
            Assert.GreaterOrEqual(meta.BankedSalvage, Respawns.BaseCost);
            Assert.AreEqual(0L, sim.UnbankedOf(0), "nothing was carried to pay with");
            Assert.IsNull(sim.RespawnRefuse(0));
        }

        [Test]
        public void TryRespawn_TakesTheFeeFromTheBankAndLeavesTheStakeAlone()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Kills(sim, 40); // comfortably past the base fee AND the doubled second
            sim.NotifyAssaultCleared();
            long staked = StakeInto(sim, 0, 200L); // and 200 of it deliberately put at risk
            long bankBefore = meta.BankedSalvage;

            long cost;
            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(150L, cost);
            Assert.AreEqual(bankBefore - 150L, meta.BankedSalvage,
                "the fee comes out of the bank");
            Assert.AreEqual(staked, sim.UnbankedOf(0),
                "the staked carry survives the death whole — the fee is not a second toll on the bet");
            Assert.AreEqual(1, sim.RespawnsTaken);

            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(300L, cost, "the second respawn costs double the first");
            Assert.AreEqual(bankBefore - 450L, meta.BankedSalvage);
            Assert.AreEqual(staked, sim.UnbankedOf(0));
        }

        [Test]
        public void TryRespawn_PricesTheSameInAssaultAsAtTheBreak()
        {
            // The charge pool is gone: mid-wave and at a break are one rule now.
            MetaState meta;
            var sim = Tethered(out meta);
            Kills(sim, 40); // comfortably funded
            sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            long cost;
            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(150L, cost);
            Assert.IsTrue(sim.TryRespawn(0, out cost), "no charge gates the second one");
            Assert.AreEqual(300L, cost);
            Assert.AreEqual(2, sim.RespawnsTaken);
        }

        [Test]
        public void TryRespawn_RefusesUnaffordableWithoutDebit()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Kills(sim, KillsJustShortOfTheFee()); // one kill short of the fee
            long banked = meta.BankedSalvage;
            Assert.Less(banked, Respawns.BaseCost);
            Assert.Greater(banked, 0L, "an EMPTY bank would be refused for a duller reason");

            Assert.AreEqual("not enough banked", sim.RespawnRefuse(0));
            long cost;
            Assert.IsFalse(sim.TryRespawn(0, out cost));
            Assert.AreEqual(banked, meta.BankedSalvage, "a refusal debits nothing");
            Assert.AreEqual(0, sim.RespawnsTaken);
        }

        [Test]
        public void TryRespawn_ChargesTheRealmsBank_NotAnySeatsCarry()
        {
            // Was TryRespawn_ChargesEachSeatItsOwnCarry. The per-seat purse is gone from this
            // verb (2026-08-25): a seat's carry is now a bet it chose to place, and taking the
            // fee out of it would tax the player who gambled and spare the one who did not.
            // The claim that survives is the separation — a respawn must not reach into ANY
            // pile, which two staked seats is the only way to show.
            MetaState meta;
            var sim = Tethered(out meta, 2);
            Kills(sim, 40); // 1000 banked
            sim.NotifyAssaultCleared();
            StakeInto(sim, 0, 100L);
            StakeInto(sim, 1, 200L);
            long seat0 = sim.UnbankedOf(0);
            long seat1 = sim.UnbankedOf(1);
            long bankBefore = meta.BankedSalvage;

            long cost;
            Assert.IsTrue(sim.TryRespawn(1, out cost));
            Assert.AreEqual(bankBefore - 150L, meta.BankedSalvage, "the realm paid");
            Assert.AreEqual(seat1, sim.UnbankedOf(1), "the dying seat's own stake is untouched");
            Assert.AreEqual(seat0, sim.UnbankedOf(0), "and so is the other seat's");
        }

        [Test]
        public void TryRespawn_CounterIsSharedAcrossSeats()
        {
            // One counter for the run: a party that keeps dying prices itself out together.
            MetaState meta;
            var sim = Tethered(out meta, 2);
            Kills(sim, 60);

            long cost;
            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(150L, cost);
            Assert.IsTrue(sim.TryRespawn(1, out cost));
            Assert.AreEqual(300L, cost, "the other seat pays the escalated price");
        }


        [Test]
        public void SeatDown_FreezesTheCarry_AndRespawnThaws()
        {
            MetaState meta;
            var sim = Tethered(out meta, 2);
            Kills(sim, 40); // 1000 banked
            sim.NotifyAssaultCleared();
            StakeInto(sim, 0, 100L);
            StakeInto(sim, 1, 200L);
            long seat1 = sim.UnbankedOf(1);

            // Down: a whole Convert window must not touch seat 1's pile. The freeze survived
            // the 2026-08-25 re-key even though the reason it exists changed — the pile is no
            // longer the purse the respawn is bought from, but it is still that seat's bet,
            // and a Convert its owner cannot decline is not a bet.
            sim.SetSeatDown(1, true);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
            Assert.AreEqual(0L, sim.UnbankedOf(0), "the live seat converted");
            Assert.AreEqual(seat1, sim.UnbankedOf(1), "the frozen pile is intact");

            long cost;
            Assert.IsTrue(sim.TryRespawn(1, out cost));
            Assert.AreEqual(seat1, sim.UnbankedOf(1),
                "the fee came from the bank, so the thawed pile is still whole");

            // Thawed: the same window that skipped it now takes it.
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
            Assert.AreEqual(0L, sim.UnbankedOf(1), "a thawed pile converts like any other");
        }

        [Test]
        public void Withdraw_CreditsTheOperatingSeat_NotSlotZero()
        {
            MetaState meta;
            var sim = Tethered(out meta, 2);
            meta.EarnSalvage(10000L);
            long before0 = sim.UnbankedOf(0);
            long before1 = sim.UnbankedOf(1);

            long moved = 0L;
            for (int i = 0; i < 40 && moved <= 0L; i++)
                moved += sim.Withdraw(0.5f, 1);
            Assert.Greater(moved, 0L, "the withdraw accumulator produced units");
            Assert.AreEqual(before1 + moved, sim.UnbankedOf(1),
                "the pot's units landed in the operating seat's pile");
            Assert.AreEqual(before0, sim.UnbankedOf(0), "slot 0 got nothing it did not earn");
        }

        [Test]
        public void Counter_ResetsOnANewRun()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Kills(sim, 20);
            long cost;
            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(1, sim.RespawnsTaken);

            var next = new RunSim(meta, 2);
            next.StartRun();
            next.EngageTether();
            Assert.AreEqual(0, next.RespawnsTaken, "the price resets with the run");
        }
    }
}
