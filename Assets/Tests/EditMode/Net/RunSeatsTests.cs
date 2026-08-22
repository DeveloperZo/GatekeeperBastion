using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The seat count a run is played at, which stops being the connected count the
    /// moment somebody can arrive after the party has embarked. Wave composition, the wallet
    /// split and the hatch doors all read it, so what is being pinned here is *when* it may
    /// move: at a break, and nowhere else (Docs/Milestones — drop in between waves).</summary>
    public class RunSeatsTests
    {
        [SetUp]
        public void Reset()
        {
            RunSeats.Reset();
        }

        [TearDown]
        public void Clear()
        {
            RunSeats.Reset();
        }

        [Test]
        public void NoRun_IsNotLive_AndCountsOneSeat()
        {
            Assert.IsFalse(RunSeats.IsLive, "a roster with no run under way is not authoritative");
            Assert.AreEqual(1, RunSeats.Count, "empty is one seat, never zero");
        }

        [Test]
        public void Solo_EmbarksWithNoRoster_AndStaysOneSeat()
        {
            // No manager is the solo shape: nothing to enumerate, and the count must not
            // change, because a solo run's wave table is the one every baseline was taken on.
            RunSeats.BeginRun(null);
            Assert.IsTrue(RunSeats.IsLive);
            Assert.AreEqual(1, RunSeats.Count);
        }

        [Test]
        public void ALiveRunWithAnEmptyRoster_TreatsNobodyAsAnOutsider()
        {
            // Solo, or a run whose seats were never enumerated because there was no listening
            // server. Reading "not admitted" off that marked every seat as a late arrival and
            // put the host on the Ship at its own embark, so the consumers ask HasRoster.
            RunSeats.BeginRun(null);
            Assert.IsTrue(RunSeats.IsLive);
            Assert.IsFalse(RunSeats.HasRoster,
                "an empty roster is not evidence that a seat arrived late");

            RunSeats.Admit(7);
            Assert.IsTrue(RunSeats.HasRoster, "one named seat and the roster governs");
        }

        [Test]
        public void ArrivingMidRun_IsNotASeatUntilItFollows()
        {
            RunSeats.BeginRun(null);
            Assert.IsFalse(RunSeats.IsAdmitted(7), "a connected seat is not a seat in the run");

            Assert.IsTrue(RunSeats.Admit(7), "the break admits it");
            Assert.IsTrue(RunSeats.IsAdmitted(7));
        }

        [Test]
        public void AdmittingTwice_CountsOnePlayer()
        {
            RunSeats.BeginRun(null);
            Assert.IsTrue(RunSeats.Admit(7));
            Assert.IsFalse(RunSeats.Admit(7), "a second press must not seat the same player twice");
            Assert.AreEqual(1, RunSeats.Count);
        }

        [Test]
        public void AdmitOutsideARun_IsRefused()
        {
            // Nothing to join. Admitting here would leave a roster that outlives the run and
            // then answers for the Ship, where the doors are read.
            Assert.IsFalse(RunSeats.Admit(7));
            Assert.IsFalse(RunSeats.IsAdmitted(7));
        }

        [Test]
        public void LeavingMidRun_IsHeldUntilTheBreak()
        {
            RunSeats.BeginRun(null);
            RunSeats.Admit(7);
            RunSeats.Admit(8);
            Assert.AreEqual(2, RunSeats.Count);

            RunSeats.NoteLeft(8);
            Assert.AreEqual(2, RunSeats.Count,
                "sealing a hatch under attack would be a free save — the seat is held");
            Assert.IsTrue(RunSeats.HasPending);

            Assert.IsTrue(RunSeats.ApplyBoundary(), "the break is where it lands");
            Assert.AreEqual(1, RunSeats.Count);
            Assert.IsFalse(RunSeats.IsAdmitted(8));
            Assert.IsFalse(RunSeats.HasPending);
        }

        [Test]
        public void ABoundaryWithNothingHeld_ChangesNothing()
        {
            RunSeats.BeginRun(null);
            RunSeats.Admit(7);
            Assert.IsFalse(RunSeats.ApplyBoundary(),
                "no held departure means no recompute and no log line");
            Assert.AreEqual(1, RunSeats.Count);
        }

        [Test]
        public void ASeatThatNeverFollowed_LeavesWithoutADeparture()
        {
            RunSeats.BeginRun(null);
            RunSeats.Admit(7);

            // Connected, waiting on the Ship, then gone. Nothing counted it, so the break has
            // nothing to give back and must not report a seat change.
            RunSeats.NoteLeft(9);
            Assert.IsFalse(RunSeats.HasPending);
            Assert.IsFalse(RunSeats.ApplyBoundary());
            Assert.AreEqual(1, RunSeats.Count);
        }

        [Test]
        public void RejoiningBeforeTheBreak_CancelsTheDeparture()
        {
            RunSeats.BeginRun(null);
            RunSeats.Admit(7);
            RunSeats.NoteLeft(7);
            Assert.IsTrue(RunSeats.HasPending);

            Assert.IsFalse(RunSeats.Admit(7), "still seated, so this is not a new seat");
            Assert.IsFalse(RunSeats.HasPending, "and the pending departure is off");
            Assert.IsTrue(RunSeats.IsAdmitted(7));
            Assert.AreEqual(1, RunSeats.Count);
        }

        [Test]
        public void TheRunEnding_ClearsTheRoster()
        {
            RunSeats.BeginRun(null);
            RunSeats.Admit(7);
            RunSeats.EndRun();

            Assert.IsFalse(RunSeats.IsLive,
                "on the Ship the connected list is the seat count again");
            Assert.IsFalse(RunSeats.IsAdmitted(7));
            Assert.AreEqual(1, RunSeats.Count);
        }

        [Test]
        public void TheRosterNeverExceedsWhatWavesCanBeComposedFor()
        {
            RunSeats.BeginRun(null);
            for (ulong id = 0; id < (ulong)WaveDirector.PlayerCapacity + 3; id++)
                RunSeats.Admit(id);

            Assert.AreEqual(WaveDirector.PlayerCapacity, RunSeats.Count);
            Assert.IsFalse(RunSeats.IsAdmitted((ulong)WaveDirector.PlayerCapacity),
                "past capacity the seat is refused rather than silently clamped away");
        }

        [Test]
        public void ASeatArrivingAtTheBreak_ChangesTheNextWaveAndNotTheOneOnTheFloor()
        {
            // The composition maths is untouched by any of this; what moves is the count it
            // is handed, and when. Drawing before the admission and after it is the whole
            // difference a late arrival makes.
            var director = new WaveDirector();
            RunSeats.BeginRun(null);
            RunSeats.Admit(7);

            int onTheFloor = director.NextWave(3, 0, RunSeats.Count).enemyCount;

            RunSeats.Admit(8);
            int next = director.NextWave(4, 0, RunSeats.Count).enemyCount;
            int nextWithoutTheArrival = director.NextWave(4, 0, 1).enemyCount;

            Assert.AreEqual(director.NextWave(3, 0, 1).enemyCount, onTheFloor,
                "the wave already drawn was composed for the party that drew it");
            Assert.Greater(next, nextWithoutTheArrival,
                "the next one is composed for the larger party");
        }
    }
}
