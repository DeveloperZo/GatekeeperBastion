using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The range's respawn bookkeeping. What is pinned is the set of ways a lane
    /// quietly stops making sense: a body that comes back twice, one that comes back after the
    /// lane was cleared, and a corpse whose return keeps being pushed further out because it
    /// went on being shot (Docs/Milestones — a range you can fight in).</summary>
    public class RespawnQueueTests
    {
        static RespawnQueue On()
        {
            var q = new RespawnQueue { Enabled = true, Delay = 2f };
            return q;
        }

        [Test]
        public void OffByDefault_AndQueuesNothingWhileOff()
        {
            var q = new RespawnQueue();
            Assert.IsFalse(q.Enabled, "a range that resurrects on entry is harder to reason about");
            Assert.IsFalse(q.NoteDown(1, 0f));
            Assert.AreEqual(0, q.PendingCount);
        }

        [Test]
        public void ABodyComesBackAfterTheDelay_AndNotBefore()
        {
            var q = On();
            Assert.IsTrue(q.NoteDown(1, 10f));
            CollectionAssert.IsEmpty(q.TakeDue(11.9f), "not yet");
            CollectionAssert.AreEquivalent(new[] { 1 }, q.TakeDue(12f), "due exactly on the delay");
        }

        [Test]
        public void ABodyIsHandedOverOnce()
        {
            var q = On();
            q.NoteDown(1, 0f);
            CollectionAssert.AreEquivalent(new[] { 1 }, q.TakeDue(5f));
            CollectionAssert.IsEmpty(q.TakeDue(6f), "taking it consumed it");
            Assert.AreEqual(0, q.PendingCount);
        }

        [Test]
        public void ShootingACorpse_DoesNotPushItsReturnOut()
        {
            // The lane is full of bullets. Re-queueing on every hit would keep a body down for
            // as long as anyone kept firing at it.
            var q = On();
            q.NoteDown(1, 0f);
            Assert.IsFalse(q.NoteDown(1, 1f), "already waiting");
            CollectionAssert.AreEquivalent(new[] { 1 }, q.TakeDue(2f), "back on the original delay");
        }

        [Test]
        public void ClearingTheLane_ClearsWhatWasComingBackToIt()
        {
            var q = On();
            q.NoteDown(1, 0f);
            q.NoteDown(2, 0f);
            q.Clear();
            Assert.AreEqual(0, q.PendingCount);
            CollectionAssert.IsEmpty(q.TakeDue(99f),
                "a clear must not be followed by arrivals from the run before it");
        }

        [Test]
        public void ADespawnedBody_CanBeForgotten()
        {
            var q = On();
            q.NoteDown(1, 0f);
            q.NoteDown(2, 0f);
            q.Forget(1);
            Assert.IsFalse(q.IsPending(1));
            CollectionAssert.AreEquivalent(new[] { 2 }, q.TakeDue(9f));
        }

        [Test]
        public void TurningItOn_DoesNotResurrectWhatDiedBeforehand()
        {
            var q = new RespawnQueue { Delay = 1f };
            q.NoteDown(1, 0f);          // off: ignored
            q.Enabled = true;
            q.NoteDown(2, 0f);
            CollectionAssert.AreEquivalent(new[] { 2 }, q.TakeDue(5f),
                "only what died under the rule the player had set comes back");
        }

        [Test]
        public void ZeroDelayIsAllowed_AndNegativeIsNot()
        {
            var q = new RespawnQueue { Enabled = true, Delay = 0f };
            q.NoteDown(1, 4f);
            CollectionAssert.AreEquivalent(new[] { 1 }, q.TakeDue(4f), "the very next tick");
            q.Delay = -5f;
            Assert.AreEqual(0f, q.Delay, "a negative delay would make everything permanently due");
        }
    }
}
