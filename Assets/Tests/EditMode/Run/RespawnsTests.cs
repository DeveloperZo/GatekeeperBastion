using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The respawn rule: escalating cost from the realm's pot, a per-seat pool
    /// gating mid-Assault returns, break returns unlimited. One rule for every dead seat
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
            Assert.AreEqual("no run",
                Respawns.Refuse(RunPhase.Docked, AssaultState.Prep, 1, 150L, 1000L));
            Assert.AreEqual("no charges until the break",
                Respawns.Refuse(RunPhase.Tethered, AssaultState.Assault, 0, 150L, 1000L));
            Assert.AreEqual("cannot afford",
                Respawns.Refuse(RunPhase.Tethered, AssaultState.Prep, 0, 150L, 149L));
            Assert.IsNull(
                Respawns.Refuse(RunPhase.Tethered, AssaultState.Assault, 1, 150L, 150L));
            Assert.IsNull(
                Respawns.Refuse(RunPhase.Tethered, AssaultState.Prep, 0, 150L, 150L),
                "Prep never asks the pool — the break tier is unlimited");
        }

        [Test]
        public void Pool_SeedsPerSeatAtTether()
        {
            MetaState soloMeta;
            var solo = Tethered(out soloMeta);
            Assert.AreEqual(Respawns.PoolPerSeat, solo.RespawnStock);
            Assert.AreEqual(0, solo.RespawnsTaken);

            MetaState partyMeta;
            var party = Tethered(out partyMeta, 3);
            Assert.AreEqual(Respawns.PoolPerSeat * 3, party.RespawnStock);
        }

        [Test]
        public void Pool_GrowsOnMidRunAdmit_NeverShrinks()
        {
            MetaState meta;
            var sim = Tethered(out meta, 2);
            int seeded = sim.RespawnStock;

            sim.SetPlayerCount(3);
            Assert.AreEqual(seeded + Respawns.PoolPerSeat, sim.RespawnStock,
                "a seat that follows the party in brings its charge");

            sim.SetPlayerCount(2);
            Assert.AreEqual(seeded + Respawns.PoolPerSeat, sim.RespawnStock,
                "a leaving seat refunds nothing, or leave-and-refollow farms charges");
        }

        [Test]
        public void TryRespawn_PrepDebitsPotWithoutBurningStock()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            int stock = sim.RespawnStock;

            long cost;
            Assert.IsTrue(sim.TryRespawn(out cost));
            Assert.AreEqual(150L, cost);
            Assert.AreEqual(850L, meta.BankedSalvage);
            Assert.AreEqual(stock, sim.RespawnStock, "the break tier never touches the pool");
            Assert.AreEqual(1, sim.RespawnsTaken);

            Assert.IsTrue(sim.TryRespawn(out cost));
            Assert.AreEqual(300L, cost, "the second respawn costs double the first");
            Assert.AreEqual(550L, meta.BankedSalvage);
        }

        [Test]
        public void TryRespawn_AssaultBurnsStockThenRefusesNamed()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(Respawns.PoolPerSeat, sim.RespawnStock);

            long cost;
            Assert.IsTrue(sim.TryRespawn(out cost));
            Assert.AreEqual(0, sim.RespawnStock, "a mid-wave respawn burns a charge");

            long potBefore = meta.BankedSalvage;
            Assert.AreEqual("no charges until the break", sim.RespawnRefuse());
            Assert.IsFalse(sim.TryRespawn(out cost));
            Assert.AreEqual(potBefore, meta.BankedSalvage, "a refusal debits nothing");
            Assert.AreEqual(1, sim.RespawnsTaken);
        }

        [Test]
        public void TryRespawn_RefusesUnaffordableWithoutDebit()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(149L);

            Assert.AreEqual("cannot afford", sim.RespawnRefuse());
            long cost;
            Assert.IsFalse(sim.TryRespawn(out cost));
            Assert.AreEqual(149L, meta.BankedSalvage);
            Assert.AreEqual(0, sim.RespawnsTaken);
        }

        [Test]
        public void Pool_ReseedsOnANewRun()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            long cost;
            Assert.IsTrue(sim.TryRespawn(out cost));
            Assert.AreEqual(1, sim.RespawnsTaken);

            var next = new RunSim(meta, 2);
            next.StartRun();
            next.EngageTether();
            Assert.AreEqual(Respawns.PoolPerSeat, next.RespawnStock);
            Assert.AreEqual(0, next.RespawnsTaken, "the price resets with the run");
        }
    }
}
