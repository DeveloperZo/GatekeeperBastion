using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The respawn rule: a dead seat buys its way back out of its OWN Unbanked at
    /// an escalating price, and the remainder of that carry survives the death with it.
    /// No charge pool and no other purse — the carry is the whole ration
    /// (Docs/Milestones — drop in between waves).</summary>
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

        /// <summary>Fill the piles the way the game does — kills, not a test-only setter, so
        /// these fixtures break if the earning path stops feeding the purse the rule spends.
        /// AwardKill pays only while a wave is on the floor, so this draws one first.</summary>
        static void Kills(RunSim sim, int n)
        {
            if (sim.Assault != AssaultState.Assault) sim.TriggerDraw();
            for (int i = 0; i < n; i++) sim.AwardKill();
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

        [Test]
        public void Refuse_NamesEveryDisqualifier()
        {
            Assert.AreEqual("no run", Respawns.Refuse(RunPhase.Docked, 150L, 1000L));
            Assert.AreEqual("not enough unbanked",
                Respawns.Refuse(RunPhase.Tethered, 150L, 149L));
            Assert.IsNull(Respawns.Refuse(RunPhase.Tethered, 150L, 150L),
                "exactly the price is affordable");
        }

        [Test]
        public void Refuse_IsBlindToTheRealmsBank()
        {
            // The pot used to pay for this. It must not any more, in either direction: a rich
            // realm cannot rescue an empty-handed seat, and a poor one cannot strand a seat
            // that is carrying enough.
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(100000L);
            Assert.AreEqual("not enough unbanked", sim.RespawnRefuse(0),
                "a full bank buys nobody a return");

            Kills(sim, 6); // 150 carried
            Assert.AreEqual(150L, sim.UnbankedOf(0));
            Assert.IsNull(sim.RespawnRefuse(0));
        }

        [Test]
        public void TryRespawn_TakesTheFeeFromTheSeatAndLeavesTheRest()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Kills(sim, 20); // 500 carried
            Assert.AreEqual(500L, sim.UnbankedOf(0));
            long bankBefore = meta.BankedSalvage;

            long cost;
            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(150L, cost);
            Assert.AreEqual(350L, sim.UnbankedOf(0),
                "the fee comes out of the carry and the remainder survives the death");
            Assert.AreEqual(bankBefore, meta.BankedSalvage, "the realm's bank is never touched");
            Assert.AreEqual(1, sim.RespawnsTaken);

            Assert.IsTrue(sim.TryRespawn(0, out cost));
            Assert.AreEqual(300L, cost, "the second respawn costs double the first");
            Assert.AreEqual(50L, sim.UnbankedOf(0));
        }

        [Test]
        public void TryRespawn_PricesTheSameInAssaultAsAtTheBreak()
        {
            // The charge pool is gone: mid-wave and at a break are one rule now.
            MetaState meta;
            var sim = Tethered(out meta);
            Kills(sim, 40); // 1000 carried
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
            Kills(sim, 5); // 125 carried, one kill short of the fee
            Assert.AreEqual(125L, sim.UnbankedOf(0));

            Assert.AreEqual("not enough unbanked", sim.RespawnRefuse(0));
            long cost;
            Assert.IsFalse(sim.TryRespawn(0, out cost));
            Assert.AreEqual(125L, sim.UnbankedOf(0), "a refusal debits nothing");
            Assert.AreEqual(0, sim.RespawnsTaken);
        }

        [Test]
        public void TryRespawn_ChargesEachSeatItsOwnCarry()
        {
            MetaState meta;
            var sim = Tethered(out meta, 2);
            Kills(sim, 40); // split across both seats
            long seat0 = sim.UnbankedOf(0);
            long seat1 = sim.UnbankedOf(1);
            Assert.Greater(seat1, 150L, "seat 1 can afford the fee");

            long cost;
            Assert.IsTrue(sim.TryRespawn(1, out cost));
            Assert.AreEqual(seat1 - 150L, sim.UnbankedOf(1), "seat 1 paid out of its own pile");
            Assert.AreEqual(seat0, sim.UnbankedOf(0), "seat 0's pile is untouched");
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
            Kills(sim, 40);
            long seat1 = sim.UnbankedOf(1);
            Assert.Greater(seat1, 150L);

            // Down: a Convert-style total take must not touch seat 1's pile.
            sim.SetSeatDown(1, true);
            long seat0Before = sim.UnbankedOf(0);
            // (The drip path routes through the same TakeTotal the wallet tests pin; here
            // the claim is the SIM surface: the flag reaches the wallets and the respawn
            // clears it.)
            long cost;
            Assert.IsTrue(sim.TryRespawn(1, out cost), "the frozen carry still pays the fee");
            Assert.AreEqual(seat1 - cost, sim.UnbankedOf(1));
            Assert.AreEqual(seat0Before, sim.UnbankedOf(0));
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
