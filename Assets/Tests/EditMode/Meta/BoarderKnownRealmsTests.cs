using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The Boarder's half of the shared-realm roster: rows the chooser lists, an
    /// index and never an authority — admission is the realm's member list. Rows survive the
    /// save round trip, refresh instead of duplicating, and prune cleanly
    /// (Docs/Milestones — a realm you can open while its owner is offline, criterion 8).</summary>
    public class BoarderKnownRealmsTests
    {
        static BoarderState Fresh() { return BoarderState.CreateNew("b1", "TESTER"); }

        [Test]
        public void AFreshBoarderRemembersNothing()
        {
            Assert.AreEqual(0, Fresh().KnownRealmCount);
        }

        [Test]
        public void RememberingTwiceIsOneRow_AndTheNameRefreshes()
        {
            var b = Fresh();
            b.RememberRealm("realm1", "owner-A", "ORION");
            b.RememberRealm("realm2", "owner-A", "TAURUS");
            b.RememberRealm("realm1", "owner-A", "ORION II");

            Assert.AreEqual(2, b.KnownRealmCount);
            Assert.AreEqual("ORION II", b.KnownRealmAt(0).Name);
            Assert.AreEqual("realm2", b.KnownRealmAt(1).RealmId);
        }

        [Test]
        public void ForgettingDropsExactlyThatRow()
        {
            var b = Fresh();
            b.RememberRealm("realm1", "owner-A", "ORION");
            b.RememberRealm("realm2", "owner-B", "TAURUS");

            Assert.IsTrue(b.ForgetRealm("realm1"));
            Assert.IsFalse(b.ForgetRealm("realm1"), "already gone");
            Assert.AreEqual(1, b.KnownRealmCount);
            Assert.AreEqual("realm2", b.KnownRealmAt(0).RealmId);
            Assert.AreEqual("owner-B", b.KnownRealmAt(0).OwnerId);
        }

        [Test]
        public void ARowNeedsBothIdentities()
        {
            var b = Fresh();
            b.RememberRealm("", "owner-A", "ORION");
            b.RememberRealm("realm1", "", "ORION");
            b.RememberRealm("realm1", null, "ORION");
            Assert.AreEqual(0, b.KnownRealmCount,
                "a row without realm and owner ids could never be fetched, so it is never kept");
        }

        [Test]
        public void TheRowsSurviveTheSaveRoundTrip()
        {
            var b = Fresh();
            b.RememberRealm("realm1", "owner-A", "ORION");
            b.RememberRealm("realm2", "owner-B", "TAURUS");

            var back = BoarderState.FromJson(b.ToJson());

            Assert.AreEqual(2, back.KnownRealmCount);
            Assert.AreEqual("realm1", back.KnownRealmAt(0).RealmId);
            Assert.AreEqual("owner-A", back.KnownRealmAt(0).OwnerId);
            Assert.AreEqual("TAURUS", back.KnownRealmAt(1).Name);
        }

        [Test]
        public void AnOlderDocumentWithoutTheKeysLoadsWithNone()
        {
            var b = Fresh();
            b.RememberRealm("realm1", "owner-A", "ORION");
            string json = b.ToJson()
                .Replace(",\"KnownRealmIds\":[\"realm1\"]", "")
                .Replace(",\"KnownRealmOwners\":[\"owner-A\"]", "")
                .Replace(",\"KnownRealmNames\":[\"ORION\"]", "");

            var back = BoarderState.FromJson(json);
            Assert.IsNotNull(back);
            Assert.AreEqual(0, back.KnownRealmCount);
        }

        [Test]
        public void MisalignedArraysReadAsNone_NeverAsReassignedRows()
        {
            var b = Fresh();
            b.RememberRealm("realm1", "owner-A", "ORION");
            b.RememberRealm("realm2", "owner-B", "TAURUS");
            string json = b.ToJson()
                .Replace("\"KnownRealmOwners\":[\"owner-A\",\"owner-B\"]",
                    "\"KnownRealmOwners\":[\"owner-A\"]");

            var back = BoarderState.FromJson(json);
            Assert.AreEqual(0, back.KnownRealmCount,
                "a broken alignment must not put realm2 beside owner-A");
        }
    }
}
