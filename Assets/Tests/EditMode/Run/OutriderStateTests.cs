using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M107: charge rises only by firing, EMP refuses at zero charge, charge
    /// survives a live redeploy, Special ends Exhausted, Exhausted cannot re-enter Deployed.
    /// Mirrors <see cref="TurretStateTests"/>.</summary>
    public class OutriderStateTests
    {
        static OutriderState Standing()
        {
            var o = new OutriderState();
            o.ResetForRun();
            string reason;
            Assert.IsTrue(o.TryPlace(1f, 0f, 2f, out reason), reason);
            return o;
        }

        [Test]
        public void Charge_RisesOnlyThroughSpendShot()
        {
            var o = Standing();
            Assert.AreEqual(0f, o.Charge, 1e-5f, "a fresh machine came in charged");
            Assert.AreEqual(TurretPhase.Deployed, o.Phase);

            Assert.IsFalse(o.TickExhausted(60f), "a quiet Deployed machine expired on a clock");
            Assert.AreEqual(0f, o.Charge, 1e-5f);
            Assert.IsTrue(o.IsLive);
            Assert.IsTrue(o.MayFire);

            Assert.IsFalse(o.SpendShot());
            Assert.AreEqual(BoarderProtocol.OutriderChargePerShot, o.Charge, 1e-5f);
            Assert.IsFalse(o.SpendShot());
            Assert.AreEqual(BoarderProtocol.OutriderChargePerShot * 2f, o.Charge, 1e-5f);
        }

        [Test]
        public void Emp_RefusesAtZeroCharge_ByThatName()
        {
            var o = Standing();
            string reason;
            Assert.IsFalse(o.TryBeginEmp(out reason));
            Assert.AreEqual("no charge", reason);
            Assert.AreEqual(TurretPhase.Deployed, o.Phase);
            Assert.IsFalse(o.EmpActive);
        }

        [Test]
        public void Charge_SurvivesLiveRedeploy_AndIsZeroOnAFreshDeploy()
        {
            var o = Standing();
            o.SpendShot();
            o.SpendShot();
            o.TakeDamage(40f);
            float kept = o.Charge;
            Assert.Greater(kept, 0f);

            string reason;
            Assert.IsTrue(o.TryPlace(5f, 0f, 5f, out reason), reason);
            Assert.AreEqual(kept, o.Charge, 1e-5f, "redeploy reset charge");
            Assert.AreEqual(BoarderProtocol.OutriderMaxHp, o.Hp, 1e-4f);
            Assert.AreEqual(TurretPhase.Deployed, o.Phase);

            o.Clear();
            Assert.AreEqual(TurretPhase.Available, o.Phase);
            Assert.IsTrue(o.TryPlace(0f, 0f, 0f, out reason), reason);
            Assert.AreEqual(0f, o.Charge, 1e-5f, "a newly deployed machine inherited charge");
        }

        [Test]
        public void Special_EndsIntoExhausted_AndExhaustedCannotReEnterDeployed()
        {
            var o = Standing();
            int n = (int)(BoarderProtocol.OutriderChargeMax / BoarderProtocol.OutriderChargePerShot);
            for (int i = 0; i < n; i++)
                Assert.IsFalse(o.SpendShot(), "charging round " + (i + 1) + " spent the machine");

            string reason;
            Assert.IsTrue(o.TryBeginEmp(out reason), reason);
            Assert.AreEqual(TurretPhase.Special, o.Phase);
            Assert.IsTrue(o.EmpActive);
            Assert.IsTrue(o.MayFire);

            bool spent = false;
            for (int i = 0; i < n; i++)
            {
                spent = o.SpendShot();
                if (spent) break;
            }
            Assert.IsTrue(spent, "spending the meter did not exhaust the machine");
            Assert.AreEqual(TurretPhase.Exhausted, o.Phase);
            Assert.IsFalse(o.MayFire, "Exhausted still acquired");
            Assert.IsTrue(o.IsLive, "Exhausted despawned immediately");
            Assert.IsFalse(o.SpendShot(), "Exhausted fired");

            Assert.IsFalse(o.TryPlace(9f, 0f, 9f, out reason));
            StringAssert.Contains("exhausted", reason);
            Assert.AreEqual(TurretPhase.Exhausted, o.Phase);

            Assert.IsFalse(o.TickExhausted(BoarderProtocol.OutriderExhaustedSeconds - 0.05f));
            Assert.AreEqual(TurretPhase.Exhausted, o.Phase);
            Assert.IsTrue(o.TickExhausted(1f));
            Assert.AreEqual(TurretPhase.Despawned, o.Phase);
            Assert.IsFalse(o.IsLive);
        }

        [Test]
        public void EnemyDamage_StillKillsAChargedMachine()
        {
            var o = Standing();
            o.SpendShot();
            Assert.IsTrue(o.TakeDamage(BoarderProtocol.OutriderMaxHp));
            Assert.IsFalse(o.IsLive);
            Assert.AreEqual(TurretPhase.Despawned, o.Phase);
            Assert.Greater(o.Charge, 0f, "dying wiped the meter — charge is not HP");
        }

        [Test]
        public void FollowDistance_IsTheCatalogFigure()
        {
            var o = Standing();
            Assert.AreEqual(BoarderProtocol.OutriderFollowDistance, o.FollowDistance, 1e-5f);
            o.SetPosition(3f, 1f, 4f);
            Assert.AreEqual(3f, o.PosX, 1e-5f);
            Assert.AreEqual(4f, o.PosZ, 1e-5f);
        }
    }

    /// <summary>Criteria 5 and 6: inside the radius each special lifts the other; outside,
    /// neither does. Both orders, because the pairing is symmetrical.</summary>
    public class MachinePairingTests
    {
        static void ChargeAndOvercharge(TurretState t)
        {
            int n = (int)(BoarderProtocol.TurretChargeMax / BoarderProtocol.TurretChargePerShot);
            for (int i = 0; i < n; i++) t.SpendShot();
            string reason;
            Assert.IsTrue(t.TryBeginOvercharge(out reason), reason);
        }

        static void ChargeAndEmp(OutriderState o)
        {
            int n = (int)(BoarderProtocol.OutriderChargeMax / BoarderProtocol.OutriderChargePerShot);
            for (int i = 0; i < n; i++) o.SpendShot();
            string reason;
            Assert.IsTrue(o.TryBeginEmp(out reason), reason);
        }

        [Test]
        public void InsideRadius_OverchargeLiftsTheOutrider_AndEmpConvertsTheTurret()
        {
            var t = new TurretState();
            t.ResetForRun();
            string reason;
            Assert.IsTrue(t.TryPlace(0f, 0f, 0f, out reason), reason);
            var o = new OutriderState();
            o.ResetForRun();
            Assert.IsTrue(o.TryPlace(BoarderProtocol.PairingRadiusMetres * 0.5f, 0f, 0f, out reason), reason);

            bool paired = BoarderProtocol.MachinesPaired(
                t.PosX, t.PosY, t.PosZ, o.PosX, o.PosY, o.PosZ);
            Assert.IsTrue(paired);

            ChargeAndOvercharge(t);
            Assert.AreEqual(BoarderProtocol.OverchargeTurretRateMul,
                BoarderProtocol.PairedOutriderRateMul(t.Overcharged, paired), 1e-5f);

            ChargeAndEmp(o);
            Assert.IsTrue(BoarderProtocol.PairedTurretFiresEmp(o.EmpActive, paired));
        }

        [Test]
        public void OutsideRadius_EachSpecialAffectsOnlyItsOwnMachine()
        {
            var t = new TurretState();
            t.ResetForRun();
            string reason;
            Assert.IsTrue(t.TryPlace(0f, 0f, 0f, out reason), reason);
            var o = new OutriderState();
            o.ResetForRun();
            Assert.IsTrue(o.TryPlace(BoarderProtocol.PairingRadiusMetres + 1f, 0f, 0f, out reason), reason);

            bool paired = BoarderProtocol.MachinesPaired(
                t.PosX, t.PosY, t.PosZ, o.PosX, o.PosY, o.PosZ);
            Assert.IsFalse(paired);

            ChargeAndOvercharge(t);
            Assert.AreEqual(1f, BoarderProtocol.PairedOutriderRateMul(t.Overcharged, paired), 1e-5f);

            ChargeAndEmp(o);
            Assert.IsFalse(BoarderProtocol.PairedTurretFiresEmp(o.EmpActive, paired));
        }

        [Test]
        public void BothOrders_ReachTheSamePairing()
        {
            // Turret first, then Outrider.
            var t = new TurretState();
            t.ResetForRun();
            string reason;
            Assert.IsTrue(t.TryPlace(0f, 0f, 0f, out reason), reason);
            var o = new OutriderState();
            o.ResetForRun();
            Assert.IsTrue(o.TryPlace(1f, 0f, 0f, out reason), reason);
            Assert.IsTrue(BoarderProtocol.MachinesPaired(
                t.PosX, t.PosY, t.PosZ, o.PosX, o.PosY, o.PosZ));

            // Outrider first, then turret — same positions, opposite TryPlace order.
            var o2 = new OutriderState();
            o2.ResetForRun();
            Assert.IsTrue(o2.TryPlace(1f, 0f, 0f, out reason), reason);
            var t2 = new TurretState();
            t2.ResetForRun();
            Assert.IsTrue(t2.TryPlace(0f, 0f, 0f, out reason), reason);
            Assert.IsTrue(BoarderProtocol.MachinesPaired(
                t2.PosX, t2.PosY, t2.PosZ, o2.PosX, o2.PosY, o2.PosZ));
        }

        [Test]
        public void SpecialsEndIndependently()
        {
            var t = new TurretState();
            t.ResetForRun();
            string reason;
            Assert.IsTrue(t.TryPlace(0f, 0f, 0f, out reason), reason);
            var o = new OutriderState();
            o.ResetForRun();
            Assert.IsTrue(o.TryPlace(1f, 0f, 0f, out reason), reason);
            ChargeAndOvercharge(t);
            ChargeAndEmp(o);

            int n = (int)(BoarderProtocol.TurretChargeMax / BoarderProtocol.TurretChargePerShot);
            bool turretSpent = false;
            for (int i = 0; i < n; i++)
            {
                turretSpent = t.SpendShot();
                if (turretSpent) break;
            }
            Assert.IsTrue(turretSpent);
            Assert.AreEqual(TurretPhase.Exhausted, t.Phase);
            Assert.AreEqual(TurretPhase.Special, o.Phase, "turret Exhausted ended the Outrider special");

            int m = (int)(BoarderProtocol.OutriderChargeMax / BoarderProtocol.OutriderChargePerShot);
            bool outriderSpent = false;
            for (int i = 0; i < m; i++)
            {
                outriderSpent = o.SpendShot();
                if (outriderSpent) break;
            }
            Assert.IsTrue(outriderSpent);
            Assert.AreEqual(TurretPhase.Exhausted, o.Phase);
        }
    }

    public class GatekeeperRootTests
    {
        [Test]
        public void RootedBody_DoesNotClose_AndStillSwingsInReach()
        {
            Assert.IsFalse(GatekeeperRoot.MayAdvance(rooted: true));
            Assert.IsTrue(GatekeeperRoot.MayAdvance(rooted: false));
            Assert.IsTrue(GatekeeperRoot.MaySwing(inReach: true),
                "a rooted body already in reach must still finish the swing");
            Assert.IsFalse(GatekeeperRoot.MaySwing(inReach: false));
        }

        [Test]
        public void EmpCatches_UsesTheCatalogRadius()
        {
            Assert.IsTrue(BoarderProtocol.EmpCatches(0f, 0f, 0f, 0f, 0f, BoarderProtocol.EmpRadiusMetres * 0.5f));
            Assert.IsFalse(BoarderProtocol.EmpCatches(0f, 0f, 0f, 0f, 0f, BoarderProtocol.EmpRadiusMetres + 1f));
        }
    }
}
