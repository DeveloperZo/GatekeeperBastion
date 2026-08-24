using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The shared-realm rules: which copy is the realm, who it admits, what makes
    /// a claim stale, and who backs off when two members claim at once. Pure Sim — these run
    /// in the offline harness, and they are the whole of the lock's correctness argument,
    /// because the store cannot arbitrate (each member can only write their own copy)
    /// (Docs/Milestones — a realm you can open while its owner is offline).</summary>
    public class RealmShareTests
    {
        const long TicksPerSecond = 10000000L;
        const long Now = 640_000_000_000_000_000L; // an arbitrary but fixed UTC instant

        static RealmShareEnvelope Shared()
        {
            var e = new RealmShareEnvelope
            {
                RealmId = "realm1",
                RealmName = "ORION",
                OwnerId = "owner-A",
                Revision = 5
            };
            RealmShare.AddMember(e, "guest-B", "BEV");
            RealmShare.AddMember(e, "guest-C", "CAM");
            return e;
        }

        static long Ago(int seconds) { return Now - seconds * TicksPerSecond; }

        // ------------------------------------------------------------------ envelope

        [Test]
        public void TheEnvelopeSurvivesItsRoundTrip()
        {
            var e = Shared();
            e.LockHolderId = "guest-B";
            e.LockHolderName = "BEV";
            e.LockTakenUtcTicks = Ago(60);
            e.LockHeartbeatUtcTicks = Ago(30);

            var back = RealmShareEnvelope.FromJson(e.ToJson());

            Assert.IsNotNull(back);
            Assert.AreEqual("realm1", back.RealmId);
            Assert.AreEqual("ORION", back.RealmName);
            Assert.AreEqual("owner-A", back.OwnerId);
            Assert.AreEqual(2, back.MemberIds.Length);
            Assert.AreEqual("guest-B", back.MemberIds[0]);
            Assert.AreEqual("CAM", back.MemberNames[1]);
            Assert.AreEqual(5, back.Revision);
            Assert.AreEqual("guest-B", back.LockHolderId);
            Assert.AreEqual(Ago(30), back.LockHeartbeatUtcTicks);
        }

        [Test]
        public void ACopyFromANewerBuildIsNotACopy()
        {
            string json = Shared().ToJson().Replace("\"ShareVersion\":1", "\"ShareVersion\":99");
            Assert.IsNull(RealmShareEnvelope.FromJson(json));
            Assert.IsNull(RealmShareEnvelope.FromJson(null));
            Assert.IsNull(RealmShareEnvelope.FromJson(""));
        }

        [Test]
        public void MisalignedMemberNamesAreRepaired_NeverReassigned()
        {
            var e = Shared();
            string json = e.ToJson().Replace("\"MemberNames\":[\"BEV\",\"CAM\"]", "\"MemberNames\":[\"BEV\"]");
            var back = RealmShareEnvelope.FromJson(json);
            Assert.AreEqual(2, back.MemberNames.Length, "names padded to the ids, not the reverse");
            Assert.AreEqual("BEV", back.MemberNames[0]);
            Assert.AreEqual("", back.MemberNames[1]);
        }

        // ------------------------------------------------------------------ roster

        [Test]
        public void TheOwnerIsAlwaysAdmitted_AndStrangersNever()
        {
            var e = Shared();
            Assert.IsTrue(RealmShare.Admits(e, "owner-A"));
            Assert.IsTrue(RealmShare.Admits(e, "guest-B"));
            Assert.IsFalse(RealmShare.Admits(e, "stranger-Z"));
            Assert.IsFalse(RealmShare.Admits(e, ""));
            Assert.IsFalse(RealmShare.Admits(null, "owner-A"));
        }

        [Test]
        public void AddingTwiceIsOneRow_AndTheNameRefreshes()
        {
            var e = Shared();
            RealmShare.AddMember(e, "guest-B", "BEVERLY");
            Assert.AreEqual(2, e.MemberIds.Length);
            Assert.AreEqual("BEVERLY", e.MemberNames[0]);
        }

        [Test]
        public void TheOwnerIsNeverAlsoAMember()
        {
            var e = Shared();
            RealmShare.AddMember(e, "owner-A", "ALEX");
            Assert.AreEqual(2, e.MemberIds.Length);
        }

        [Test]
        public void RemovalDropsExactlyThatRow()
        {
            var e = Shared();
            Assert.IsTrue(RealmShare.RemoveMember(e, "guest-B"));
            Assert.IsFalse(RealmShare.RemoveMember(e, "guest-B"), "already gone");
            Assert.AreEqual(1, e.MemberIds.Length);
            Assert.AreEqual("guest-C", e.MemberIds[0]);
            Assert.AreEqual("CAM", e.MemberNames[0]);
        }

        [Test]
        public void TheReadSetIsTheOwnerPlusEveryMember()
        {
            var set = RealmShare.ReadSet(Shared());
            Assert.AreEqual(3, set.Length);
            Assert.AreEqual("owner-A", set[0]);
        }

        // ------------------------------------------------------------------ freshest

        [Test]
        public void TheHighestRevisionIsTheRealm()
        {
            var a = Shared(); a.Revision = 5;
            var b = Shared(); b.Revision = 7;
            Assert.AreEqual(1, RealmShare.PickFreshest(new[] { a, b }));
        }

        [Test]
        public void ACrashedHoldersCopyBeatsItsOwnOlderRelease()
        {
            // Same revision — the crashed session never closed — but the crashed copy's
            // heartbeat is later, and it carries the later body.
            var released = Shared(); released.Revision = 5;
            var crashed = Shared();
            crashed.Revision = 5;
            crashed.LockHolderId = "guest-B";
            crashed.LockHeartbeatUtcTicks = Ago(600);
            Assert.AreEqual(1, RealmShare.PickFreshest(new[] { released, crashed }));
        }

        [Test]
        public void MissingCopiesAreSkipped_AndNoCopiesIsNoRealm()
        {
            var only = Shared();
            Assert.AreEqual(1, RealmShare.PickFreshest(new RealmShareEnvelope[] { null, only }));
            Assert.AreEqual(-1, RealmShare.PickFreshest(new RealmShareEnvelope[] { null, null }));
            Assert.AreEqual(-1, RealmShare.PickFreshest(null));
        }

        // ------------------------------------------------------------------ staleness

        [Test]
        public void AClaimIsLiveInsideTheWindow_AndStaleBeyondIt()
        {
            var e = Shared();
            e.LockHolderId = "guest-B";
            e.LockHeartbeatUtcTicks = Ago(RealmShare.StaleSeconds - 10);
            Assert.IsTrue(RealmShare.ClaimLooksLive(e, Now));
            e.LockHeartbeatUtcTicks = Ago(RealmShare.StaleSeconds + 10);
            Assert.IsFalse(RealmShare.ClaimLooksLive(e, Now));
        }

        [Test]
        public void ABeatFromTheFutureIsAliveClock_NotAStaleClaim()
        {
            var e = Shared();
            e.LockHolderId = "guest-B";
            e.LockHeartbeatUtcTicks = Now + 300 * TicksPerSecond;
            Assert.IsTrue(RealmShare.ClaimLooksLive(e, Now),
                "the other machine's clock running ahead must not read as a breakable lock");
        }

        // ------------------------------------------------------------------ opening

        [Test]
        public void AFreeRealmOpens()
        {
            var d = RealmShare.DecideOpen(new[] { Shared() }, "guest-B", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.Open, d.Verdict);
            Assert.AreEqual(0, d.FreshestIndex);
        }

        [Test]
        public void AStrangerIsRefusedWithTheRealmsName()
        {
            var d = RealmShare.DecideOpen(new[] { Shared() }, "stranger-Z", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.NotAMember, d.Verdict);
            Assert.IsTrue(d.Refusal.Contains("ORION"), "the refusal names the realm: " + d.Refusal);
        }

        [Test]
        public void ARemovedMemberIsRefused_EvenHoldingAnOldCopyThatListsThem()
        {
            var current = Shared();
            RealmShare.RemoveMember(current, "guest-B");
            current.Revision = 9;
            var theirOldCopy = Shared(); // still lists guest-B, but rev 5
            var d = RealmShare.DecideOpen(new[] { current, theirOldCopy }, "guest-B", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.NotAMember, d.Verdict,
                "admission is read off the freshest copy, never off your own");
        }

        [Test]
        public void ALiveClaimRefusesAsLocked_NamingTheHolder()
        {
            var free = Shared(); free.Revision = 9;
            var holding = Shared();
            holding.LockHolderId = "guest-C";
            holding.LockHolderName = "CAM";
            holding.LockHeartbeatUtcTicks = Ago(20);
            // The claim sits on a LOWER-revision copy — the lock question is asked of every
            // copy, or a new holder would be invisible until their first release.
            var d = RealmShare.DecideOpen(new[] { free, holding }, "guest-B", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.Locked, d.Verdict);
            Assert.AreEqual("guest-C", d.HolderId);
            Assert.IsTrue(d.Refusal.Contains("CAM"), "the refusal names the holder: " + d.Refusal);
        }

        [Test]
        public void YourOwnLeftoverClaimDoesNotLockYouOut()
        {
            var mine = Shared();
            mine.LockHolderId = "guest-B";
            mine.LockHeartbeatUtcTicks = Ago(20);
            var d = RealmShare.DecideOpen(new[] { mine }, "guest-B", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.Open, d.Verdict,
                "a claim you already hold is yours to re-enter, not a rival");
        }

        [Test]
        public void AStaleLookingClaimAsksForObservation_ThenBreaksOnlyIfUnmoved()
        {
            var stale = Shared();
            stale.LockHolderId = "guest-C";
            stale.LockHolderName = "CAM";
            stale.LockHeartbeatUtcTicks = Ago(RealmShare.StaleSeconds + 60);

            var first = RealmShare.DecideOpen(new[] { stale }, "guest-B", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.ConfirmStale, first.Verdict);
            Assert.AreEqual(stale.LockHeartbeatUtcTicks, first.WitnessHeartbeatTicks);

            // Second look, heartbeat unmoved: the claim is dead and the open proceeds,
            // reporting whom it displaced (the break is logged with that name).
            long later = Now + RealmShare.ConfirmSeconds * TicksPerSecond;
            var second = RealmShare.DecideOpen(new[] { stale }, "guest-B", later,
                first.WitnessHeartbeatTicks);
            Assert.AreEqual(RealmShare.OpenVerdict.Open, second.Verdict);
            Assert.AreEqual("guest-C", second.HolderId, "the displaced holder is reported");
        }

        [Test]
        public void AClaimThatBeatsDuringObservationIsAliveAfterAll()
        {
            var claim = Shared();
            claim.LockHolderId = "guest-C";
            claim.LockHolderName = "CAM";
            claim.LockHeartbeatUtcTicks = Ago(RealmShare.StaleSeconds + 60);

            var first = RealmShare.DecideOpen(new[] { claim }, "guest-B", Now);
            Assert.AreEqual(RealmShare.OpenVerdict.ConfirmStale, first.Verdict);

            // The holder wakes and beats: the second look sees a fresh heartbeat.
            claim.LockHeartbeatUtcTicks = Now;
            long later = Now + RealmShare.ConfirmSeconds * TicksPerSecond;
            var second = RealmShare.DecideOpen(new[] { claim }, "guest-B", later,
                first.WitnessHeartbeatTicks);
            Assert.AreEqual(RealmShare.OpenVerdict.Locked, second.Verdict);
        }

        // ------------------------------------------------------------------ claim lifecycle

        [Test]
        public void AClaimCarriesTheFreshestHistoryIntoYourOwnCopy()
        {
            var freshest = Shared();
            freshest.Revision = 9;
            var mine = RealmShare.Claim(freshest, "guest-B", "BEV", Now);
            Assert.AreEqual(9, mine.Revision, "the claim continues the chain, not your old copy");
            Assert.AreEqual("guest-B", mine.LockHolderId);
            Assert.AreEqual(Now, mine.LockTakenUtcTicks);
            Assert.AreEqual(2, mine.MemberIds.Length, "the roster travels with the history");
        }

        [Test]
        public void ReleaseAdvancesTheChain_ConcedeLeavesItUntouched()
        {
            var mine = RealmShare.Claim(Shared(), "guest-B", "BEV", Now);
            long rev = mine.Revision;

            var conceded = RealmShare.Claim(Shared(), "guest-B", "BEV", Now);
            RealmShare.Concede(conceded);
            Assert.AreEqual(rev, conceded.Revision, "a conceded session never happened");
            Assert.IsFalse(conceded.HasLock);

            RealmShare.Beat(mine, Now + 45 * TicksPerSecond);
            Assert.AreEqual(rev, mine.Revision, "a heartbeat is the same session, not a new chapter");
            Assert.AreEqual(Now + 45 * TicksPerSecond, mine.LockHeartbeatUtcTicks);

            RealmShare.Release(mine);
            Assert.AreEqual(rev + 1, mine.Revision);
            Assert.IsFalse(mine.HasLock);
        }

        // ------------------------------------------------------------------ contest

        [Test]
        public void TheEarlierClaimWinsAContest()
        {
            var early = RealmShare.Claim(Shared(), "guest-B", "BEV", Ago(5));
            var late = RealmShare.Claim(Shared(), "guest-C", "CAM", Now);
            var copies = new[] { early, late };
            Assert.IsFalse(RealmShare.LosesContest(copies, "guest-B", Now));
            Assert.IsTrue(RealmShare.LosesContest(copies, "guest-C", Now));
        }

        [Test]
        public void ADeadEvenTieFallsToTheLowerId_OnBothSides()
        {
            var b = RealmShare.Claim(Shared(), "guest-B", "BEV", Now);
            var c = RealmShare.Claim(Shared(), "guest-C", "CAM", Now);
            var copies = new[] { b, c };
            Assert.IsFalse(RealmShare.LosesContest(copies, "guest-B", Now));
            Assert.IsTrue(RealmShare.LosesContest(copies, "guest-C", Now),
                "exactly one side concedes, decided from the same two numbers");
        }

        [Test]
        public void AStaleRivalIsNoContest()
        {
            var mine = RealmShare.Claim(Shared(), "guest-B", "BEV", Now);
            var dead = Shared();
            dead.LockHolderId = "guest-C";
            dead.LockTakenUtcTicks = Ago(3600);
            dead.LockHeartbeatUtcTicks = Ago(3600);
            Assert.IsFalse(RealmShare.LosesContest(new[] { mine, dead }, "guest-B", Now));
        }
    }
}
