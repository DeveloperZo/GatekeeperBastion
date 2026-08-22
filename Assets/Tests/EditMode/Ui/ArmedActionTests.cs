using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The two-press rule behind DELETE REALM. Deleting a world throws away every run
    /// anyone banked into it and there is no undo inside the game, so what matters is not that
    /// a second press exists but that the second press means the same thing the first one
    /// did — the arming names a document, and a player who has moved on must not commit to a
    /// target they are no longer looking at.</summary>
    public class ArmedActionTests
    {
        [Test]
        public void OnePress_DoesNotFire()
        {
            var a = new ArmedAction();
            Assert.IsFalse(a.Fire("ORION", 0f), "the first press arms, it does not act");
            Assert.AreEqual("ORION", a.Target, "and it says what it is armed against");
        }

        [Test]
        public void TwoPresses_Fire_AndLeaveNothingArmed()
        {
            var a = new ArmedAction();
            a.Fire("ORION", 0f);
            Assert.IsTrue(a.Fire("ORION", 1f), "the second press inside the window acts");
            Assert.IsNull(a.Target, "and the arming is spent, so a third press does not act again");
            Assert.IsFalse(a.Fire("ORION", 1.1f), "which it does not");
        }

        [Test]
        public void APressAtADifferentTarget_ArmsThatOne_AndNeverFiresTheOld()
        {
            // The defect this exists to prevent: arm on one realm, click another row, press
            // again, delete the wrong world.
            var a = new ArmedAction();
            a.Fire("ORION", 0f);
            Assert.IsFalse(a.Fire("VEGA", 0.5f), "a different target is a first press, not a confirm");
            Assert.AreEqual("VEGA", a.Target);
            Assert.IsFalse(a.IsArmed("ORION", 0.6f), "and the old target is no longer armed at all");
        }

        [Test]
        public void AnArmingRunsOut()
        {
            var a = new ArmedAction();
            a.Fire("ORION", 0f);
            float past = ArmedAction.WindowSeconds + 0.01f;
            Assert.IsFalse(a.IsArmed("ORION", past), "past the window it is not armed");
            Assert.IsFalse(a.Fire("ORION", past), "so that press arms afresh rather than acting");
            Assert.IsTrue(a.Fire("ORION", past + 0.1f), "and the one after it acts");
        }

        [Test]
        public void ExpiryIsReportedOnce_SoTheLabelCanBeRepainted()
        {
            var a = new ArmedAction();
            a.Fire("ORION", 0f);
            Assert.IsFalse(a.TickExpiry(1f), "still inside the window, nothing changed");
            Assert.IsTrue(a.TickExpiry(ArmedAction.WindowSeconds + 0.01f), "it ran out just now");
            Assert.IsFalse(a.TickExpiry(ArmedAction.WindowSeconds + 1f),
                "and having already been dropped, it does not keep reporting");
        }

        [Test]
        public void DisarmingCancels()
        {
            var a = new ArmedAction();
            a.Fire("ORION", 0f);
            a.Disarm();
            Assert.IsNull(a.Target);
            Assert.IsFalse(a.Fire("ORION", 0.1f), "after a cancel the next press is a first press");
        }

        [Test]
        public void NothingArmsAgainstNothing()
        {
            var a = new ArmedAction();
            Assert.IsFalse(a.Fire(null, 0f));
            Assert.IsNull(a.Target);
            Assert.IsFalse(a.IsArmed(null, 0f), "an empty target must never read as armed");
        }
    }
}
