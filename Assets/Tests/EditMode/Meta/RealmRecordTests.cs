using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The realm's record survives the trip to a guest's machine intact. A guest
    /// cannot read the host's RealmState, so what it displays comes through this packing —
    /// and a number that changes on the way is worse than one that is missing, because the
    /// guest has no way to tell (Docs/Milestones — the ship remembers).</summary>
    public class RealmRecordTests
    {
        static RealmState Played()
        {
            var r = RealmState.CreateNew("r1", "ALPHA");
            r.CleanExits = 3;
            r.PodEjections = 7;
            r.DeepestWaveReached = 4;
            r.HatchArmorLevel = 2;
            return r;
        }

        [Test]
        public void TheRecordReadsWhatTheRealmHasDone()
        {
            var rec = RealmRecord.From(Played());

            Assert.AreEqual(3, rec.CleanExits);
            Assert.AreEqual(7, rec.RunsLost, "runs lost is PodEjections, the loss counter");
            Assert.AreEqual(4, rec.DeepestWave);
            Assert.AreEqual(2, rec.HatchArmor);
            Assert.IsFalse(rec.IsUntouched);
        }

        [Test]
        public void ItSurvivesTheRoundTripToAGuest()
        {
            var sent = RealmRecord.From(Played());
            var got = RealmRecord.Unpack(sent.Pack());

            Assert.IsTrue(sent.Equals(got), "every field came back the same");
            Assert.AreEqual(3, got.CleanExits);
            Assert.AreEqual(7, got.RunsLost);
            Assert.AreEqual(4, got.DeepestWave);
            Assert.AreEqual(2, got.HatchArmor);
        }

        [Test]
        public void FieldsDoNotBleedIntoEachOther()
        {
            // One field at its maximum must not show up in any neighbour. This is the failure
            // that packing invites and the one a display would render as a plausible lie.
            var only = new RealmRecord { CleanExits = RealmRecord.FieldMax, DeepestWave = 1 };
            var back = RealmRecord.Unpack(only.Pack());

            Assert.AreEqual(RealmRecord.FieldMax, back.CleanExits);
            Assert.AreEqual(0, back.RunsLost);
            Assert.AreEqual(0, back.HatchArmor);

            var deep = new RealmRecord { DeepestWave = RealmRecord.FieldMax };
            var deepBack = RealmRecord.Unpack(deep.Pack());
            Assert.AreEqual(RealmRecord.FieldMax, deepBack.DeepestWave);
            Assert.AreEqual(0, deepBack.CleanExits);
            Assert.AreEqual(0, deepBack.RunsLost);
            Assert.AreEqual(0, deepBack.HatchArmor);
        }

        [Test]
        public void AnOverlongCampaignShowsTheCap_RatherThanWrappingToNothing()
        {
            var absurd = new RealmRecord
            {
                CleanExits = RealmRecord.FieldMax + 5000,
                RunsLost = int.MaxValue,
                DeepestWave = 3,
                HatchArmor = 1
            };
            var back = RealmRecord.Unpack(absurd.Pack());

            Assert.AreEqual(RealmRecord.FieldMax, back.CleanExits, "clamped, not truncated");
            Assert.AreEqual(RealmRecord.FieldMax, back.RunsLost);
            Assert.AreEqual(3, back.DeepestWave, "and the neighbours are undisturbed");
            Assert.AreEqual(1, back.HatchArmor);
        }

        [Test]
        public void ARealmNobodyHasPlayedReadsAsEmpty_NotAsBroken()
        {
            var fresh = RealmRecord.From(RealmState.CreateNew("r2", "BETA"));

            Assert.IsTrue(fresh.IsUntouched, "the display has one thing to ask, not four");
            Assert.AreEqual(0, fresh.CleanExits);
            Assert.AreEqual(0, fresh.RunsLost);
            Assert.AreEqual(1, fresh.DeepestWave,
                "a realm stands at its first wave, never at a zeroth one");

            // A guest that joins before the host has written anything reads a zero off the
            // wire. That has to mean "nothing yet", not "wave zero".
            var fromSilence = RealmRecord.Unpack(0L);
            Assert.IsTrue(fromSilence.IsUntouched);
            Assert.AreEqual(1, fromSilence.DeepestWave);
        }

        [Test]
        public void ANullRealmIsAFreshOne_NotACrash()
        {
            var rec = RealmRecord.From(null);

            Assert.IsTrue(rec.IsUntouched);
            Assert.AreEqual(1, rec.DeepestWave);
        }

        [Test]
        public void ADeepestWaveBelowOneIsRaised_WhereverItCameFrom()
        {
            var realm = RealmState.CreateNew("r3", "GAMMA");
            realm.DeepestWaveReached = 0;

            Assert.AreEqual(1, RealmRecord.From(realm).DeepestWave);
            Assert.AreEqual(1, RealmRecord.Unpack(new RealmRecord().Pack()).DeepestWave);
        }
    }
}
