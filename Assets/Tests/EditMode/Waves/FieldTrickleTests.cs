using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// The drip that holds a field Wave open until its terminal is shut off (Zo, 2026-08-26;
    /// Docs/Design/0-Gameplay.md). Two claims are load-bearing and both are pinned here:
    /// the cadence never bursts, and a trickle body pays NOTHING.
    ///
    /// The pay rule is the one worth guarding. The drip exists so the 227 m walk to a terminal
    /// is fought rather than strolled; if its bodies paid, the walk would become a farm and a
    /// player who never advances could out-earn one who does — the exact stall the trickle is
    /// there to prevent.
    /// </summary>
    public class FieldTrickleTests
    {
        static RunSim Tethered(out MetaState meta)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, 1);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        // ================================================================
        //  The cadence
        // ================================================================

        [Test]
        public void DueIn_PaysOneBodyPerInterval_AndNeverBursts()
        {
            // A long frame must not release a wall of bodies: the drip is a cadence, not a
            // budget. One interval is consumed per true, so a 10x frame still yields one.
            float acc = 0f;
            Assert.IsFalse(FieldTrickle.DueIn(FieldTrickle.SecondsBetweenBodies * 0.5f, 0, ref acc),
                "half an interval produced a body");
            Assert.IsTrue(FieldTrickle.DueIn(FieldTrickle.SecondsBetweenBodies * 0.5f, 0, ref acc),
                "the interval completed and produced nothing");
            Assert.IsFalse(FieldTrickle.DueIn(0.01f, 0, ref acc), "the clock did not reset");

            acc = 0f;
            Assert.IsTrue(FieldTrickle.DueIn(FieldTrickle.SecondsBetweenBodies * 10f, 0, ref acc),
                "a long frame produced nothing");
            Assert.IsFalse(FieldTrickle.DueIn(0f, 0, ref acc),
                "a zero-length frame produced a body");
        }

        [Test]
        public void DueIn_HoldsAtTheCap_AndDoesNotBankUpWhileHeld()
        {
            // A player who stops to fight faces a STANDING cost, never a growing one — and the
            // held time may not be banked and released the instant they kill one.
            float acc = 0f;
            for (int i = 0; i < 20; i++)
            {
                Assert.IsFalse(FieldTrickle.DueIn(1f, FieldTrickle.MaxLive, ref acc),
                    "the drip kept spawning past its cap");
            }
            Assert.AreEqual(0f, acc, 1e-6f, "held time was banked while the lane was full");
            Assert.IsFalse(FieldTrickle.DueIn(0.01f, 0, ref acc),
                "killing one body released a burst that had been banking up while held");
        }

        [Test]
        public void MaxLive_IsWellUnderTheGlobalEnemyCap()
        {
            // The drip must never be the thing that hits the global cap — if it were, it would
            // be starving the authored wave of its own bodies.
            Assert.Less(FieldTrickle.MaxLive, WaveDirector.MaxEnemyCount,
                "the trickle cap is not below the global live cap");
            Assert.Greater(FieldTrickle.SecondsBetweenBodies, 0f,
                "a non-positive interval would spawn every tick");
        }

        // ================================================================
        //  The pay rule
        // ================================================================

        [Test]
        public void ATrickleBody_PaysNothing_AndTheWaveResumesPayingWhenItEnds()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();

            long banked = meta.BankedSalvage;
            sim.AwardKill();
            long authored = meta.BankedSalvage - banked;
            Assert.Greater(authored, 0L, "an authored body paid nothing");

            sim.SetFieldTrickle(true);
            Assert.AreEqual(0L, sim.CurrentKillAward,
                "a trickle body is quoted a price — the HUD would promise pay that never lands");
            banked = meta.BankedSalvage;
            sim.AwardKill();
            Assert.AreEqual(banked, meta.BankedSalvage,
                "a trickle body paid: walking the lane is now farmable, which is the stall the "
                + "drip exists to prevent");

            sim.SetFieldTrickle(false);
            sim.AwardKill();
            Assert.AreEqual(banked + authored, meta.BankedSalvage,
                "the wave did not resume paying once the drip was turned off");
        }

        [Test]
        public void TheDripIsAClockNotAnIncome_AndNotAMedic()
        {
            // The payout and the HEAL ride the same seam in GameController.GatekeeperKilled,
            // and until 2026-08-26 only the payout was zeroed — so the drip fed the regen
            // boost and the walk up the lane became a healing station (modelled at +21 HP net
            // across zone 1's 66 m: the player arrived at the terminal healthier than they
            // left the Hatch). The award half is pinned here; the heal half is a Game-layer
            // gate on this same flag, which is the only reason this test can state the rule.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.SetFieldTrickle(true);

            Assert.IsTrue(sim.FieldTrickleLive,
                "the flag the heal gate reads is not set, so nothing gates the heal either");
            Assert.AreEqual(0L, sim.CurrentKillAward,
                "a trickle body is quoted a price");

            long banked = meta.BankedSalvage;
            sim.AwardKill();
            Assert.AreEqual(banked, meta.BankedSalvage, "a trickle body paid");
        }

        [Test]
        public void EndingTheRun_ClearsTheDrip()
        {
            // No drip outlives the Run it was holding open — a stale flag would silently zero
            // the next Run's first kills.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.SetFieldTrickle(true);
            Assert.IsTrue(sim.FieldTrickleLive);

            sim.NotifyPlayerDown();
            Assert.IsFalse(sim.FieldTrickleLive, "the drip survived the Run that owned it");
        }

        [Test]
        public void TheConvertWindow_PaysNothingWithOrWithoutTheDrip()
        {
            // Two independent reasons to pay zero must not cancel each other out.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.AwardKill();
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, 25L);

            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(0L, sim.CurrentKillAward, "a Convert window paid for a kill");
            sim.SetFieldTrickle(true);
            Assert.AreEqual(0L, sim.CurrentKillAward, "the drip flag un-zeroed a Convert window");
        }
    }
}
