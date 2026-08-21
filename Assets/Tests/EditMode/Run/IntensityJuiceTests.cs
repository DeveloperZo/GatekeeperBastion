using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class IntensityJuiceTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static void CreditKills(RunSim sim, int count)
        {
            sim.TriggerDraw();
            for (int i = 0; i < count; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
        }

        [Test]
        public void Unbanked250_JuicesTwice_ThirdRefuses()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            Assert.AreEqual(250L, sim.Unbanked);

            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsFalse(sim.TryJuice(1, true));
            Assert.AreEqual(2, sim.Siphon.Level);
            Assert.AreEqual(50L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(200L, sim.JuiceSpentThisPull);
        }

        [Test]
        public void Juice_LeavesBankedSalvageUntouched()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.EarnSalvage(1000L);
            CreditKills(sim, 10);
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.AreEqual(1000L, meta.BankedSalvage);
            Assert.AreEqual(150L, sim.Unbanked);
        }

        [Test]
        public void TryJuice_RefusedDuringAssault()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            long pile = sim.Unbanked;
            sim.TriggerDraw();
            Assert.IsFalse(sim.TryJuice(1, true));
            Assert.AreEqual(pile, sim.Unbanked);
            Assert.AreEqual(0, sim.Siphon.Level);
        }

        [Test]
        public void TryJuice_RefusedAwayFromTrigger()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            Assert.IsFalse(sim.TryJuice(1, false));
            Assert.AreEqual(250L, sim.Unbanked);
            Assert.AreEqual(0, sim.Siphon.Level);
            Assert.AreEqual(0L, sim.JuiceSpentThisPull);
        }

        [Test]
        public void Lowering_IsFree_DoesNotRefund()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(-1, true));
            Assert.AreEqual(1, sim.Siphon.Level);
            Assert.AreEqual(50L, sim.Unbanked);
            Assert.AreEqual(200L, sim.JuiceSpentThisPull);
        }

        [Test]
        public void IdlePrepClear_ResetsJuiceForNextPull()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            CreditKills(sim, 10);
            Assert.IsTrue(sim.TryJuice(1, true));
            Assert.IsTrue(sim.TryJuice(1, true));
            sim.TriggerDraw();
            sim.NotifyAssaultCleared();
            Assert.AreEqual(0, sim.Siphon.Level);
            Assert.AreEqual(0L, sim.JuiceSpentThisPull);
        }
    }
}
