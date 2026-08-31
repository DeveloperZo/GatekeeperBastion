using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M106: charge rises only by firing, Overcharge refuses at zero charge,
    /// charge survives a live redeploy, Special ends Exhausted, Exhausted cannot re-enter
    /// Deployed.</summary>
    public class TurretStateTests
    {
        static TurretState Standing()
        {
            var t = new TurretState();
            t.ResetForRun();
            string reason;
            Assert.IsTrue(t.TryPlace(1f, 0f, 2f, out reason), reason);
            return t;
        }

        [Test]
        public void Charge_RisesOnlyThroughSpendShot()
        {
            var t = Standing();
            Assert.AreEqual(0f, t.Charge, 1e-5f, "a fresh machine came in charged");
            Assert.AreEqual(TurretPhase.Deployed, t.Phase);

            Assert.IsFalse(t.TickExhausted(60f), "a quiet Deployed machine expired on a clock");
            Assert.AreEqual(0f, t.Charge, 1e-5f);
            Assert.IsTrue(t.IsLive);
            Assert.IsTrue(t.MayFire);

            Assert.IsFalse(t.SpendShot());
            Assert.AreEqual(BoarderProtocol.TurretChargePerShot, t.Charge, 1e-5f);
            Assert.IsFalse(t.SpendShot());
            Assert.AreEqual(BoarderProtocol.TurretChargePerShot * 2f, t.Charge, 1e-5f);
        }

        [Test]
        public void Overcharge_RefusesAtZeroCharge_ByThatName()
        {
            var t = Standing();
            string reason;
            Assert.IsFalse(t.TryBeginOvercharge(out reason));
            Assert.AreEqual("no charge", reason);
            Assert.AreEqual(TurretPhase.Deployed, t.Phase);
            Assert.IsFalse(t.Overcharged);
        }

        [Test]
        public void Charge_SurvivesLiveRedeploy_AndIsZeroOnAFreshDeploy()
        {
            var t = Standing();
            t.SpendShot();
            t.SpendShot();
            t.TakeDamage(40f);
            float kept = t.Charge;
            Assert.Greater(kept, 0f);

            string reason;
            Assert.IsTrue(t.TryPlace(5f, 0f, 5f, out reason), reason);
            Assert.AreEqual(kept, t.Charge, 1e-5f, "redeploy reset charge");
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, t.Hp, 1e-4f);
            Assert.AreEqual(0f, t.DamageTaken, 1e-5f);
            Assert.AreEqual(5f, t.PosX, 1e-5f);
            Assert.AreEqual(TurretPhase.Deployed, t.Phase);

            t.Clear();
            Assert.AreEqual(TurretPhase.Available, t.Phase);
            Assert.IsTrue(t.TryPlace(0f, 0f, 0f, out reason), reason);
            Assert.AreEqual(0f, t.Charge, 1e-5f, "a newly deployed machine inherited charge");
        }

        [Test]
        public void Special_EndsIntoExhausted_AndExhaustedCannotReEnterDeployed()
        {
            var t = Standing();
            int n = (int)(BoarderProtocol.TurretChargeMax / BoarderProtocol.TurretChargePerShot);
            for (int i = 0; i < n; i++)
                Assert.IsFalse(t.SpendShot(), "charging round " + (i + 1) + " spent the machine");

            string reason;
            Assert.IsTrue(t.TryBeginOvercharge(out reason), reason);
            Assert.AreEqual(TurretPhase.Special, t.Phase);
            Assert.IsTrue(t.Overcharged);
            Assert.IsTrue(t.MayFire);

            bool spent = false;
            for (int i = 0; i < n; i++)
            {
                spent = t.SpendShot();
                if (spent) break;
            }
            Assert.IsTrue(spent, "spending the meter did not exhaust the machine");
            Assert.AreEqual(TurretPhase.Exhausted, t.Phase);
            Assert.IsFalse(t.MayFire, "Exhausted still acquired");
            Assert.IsTrue(t.IsLive, "Exhausted despawned immediately");
            Assert.IsFalse(t.SpendShot(), "Exhausted fired");

            Assert.IsFalse(t.TryPlace(9f, 0f, 9f, out reason));
            StringAssert.Contains("exhausted", reason);
            Assert.AreEqual(TurretPhase.Exhausted, t.Phase);

            Assert.IsFalse(t.TickExhausted(BoarderProtocol.TurretExhaustedSeconds - 0.05f));
            Assert.AreEqual(TurretPhase.Exhausted, t.Phase);
            Assert.IsTrue(t.TickExhausted(1f));
            Assert.AreEqual(TurretPhase.Despawned, t.Phase);
            Assert.IsFalse(t.IsLive);
        }

        [Test]
        public void EnemyDamage_StillKillsAChargedMachine()
        {
            var t = Standing();
            t.SpendShot();
            Assert.IsTrue(t.TakeDamage(BoarderProtocol.TurretMaxHp));
            Assert.IsFalse(t.IsLive);
            Assert.AreEqual(TurretPhase.Despawned, t.Phase);
            Assert.Greater(t.Charge, 0f, "dying wiped the meter — charge is not HP");
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, t.DamageTaken, 1e-4f);
        }

        [Test]
        public void Charge_ClampsAtMax()
        {
            var t = Standing();
            int n = (int)(BoarderProtocol.TurretChargeMax / BoarderProtocol.TurretChargePerShot) + 5;
            for (int i = 0; i < n; i++)
                t.SpendShot();
            Assert.AreEqual(BoarderProtocol.TurretChargeMax, t.Charge, 1e-5f);
        }
    }
}
