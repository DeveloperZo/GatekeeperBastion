using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>You can see which of your realms is open to friends WITHOUT entering it.
    ///
    /// Sharing has no mark on the realm document — it lives in a sidecar beside it — so the
    /// only way a player could tell a co-op realm from a solo one was to open it and watch
    /// what the store did. Two of these pin the fact reaching the row; the rest pin the
    /// wording, including the one rule that is invisible until a player sees a blank where a
    /// symbol should be: the mark is ASCII, because this screen's font atlas is static.</summary>
    public class RealmSelectCoopMarkTests
    {
        const string Solo = "t-mark-solo";
        const string Open = "t-mark-open";
        const string Friend = "t-mark-friend";

        string _boarderId;

        [SetUp]
        public void NothingIsSharedYet()
        {
            SaveService.DeleteShareSidecar(Solo);
            SaveService.DeleteShareSidecar(Open);
            _boarderId = null;
        }

        [TearDown]
        public void TakeTheFixtureBackOff()
        {
            SaveService.DeleteShareSidecar(Solo);
            SaveService.DeleteShareSidecar(Open);
            if (_boarderId != null) RealmRegistry.DeleteBoarder(_boarderId);
        }

        /// <summary>Rows as the chooser hands them in: on disk, this build can open them, and
        /// nothing yet says anything about co-op.</summary>
        static RealmRegistry.SaveRow[] LocalRows()
        {
            return new[]
            {
                new RealmRegistry.SaveRow(Solo, "SOLO", true),
                new RealmRegistry.SaveRow(Open, "OPEN", true)
            };
        }

        static void ShareIt(string realmId, string name)
        {
            var env = new RealmShareEnvelope
            {
                RealmId = realmId,
                RealmName = name,
                OwnerId = "owner-me",
                Revision = 1
            };
            Assert.IsTrue(SaveService.SaveShareSidecar(realmId, env.ToJson()),
                "the fixture could not write a share sidecar");
        }

        // ------------------------------------------------------------------ the fact

        [Test]
        public void ASharedRealmsRowCarriesTheMark_AndAnUnsharedOneDoesNot()
        {
            ShareIt(Open, "OPEN");

            var rows = new RealmSelectShared().BuildRows(LocalRows(), null);

            Assert.AreEqual(RealmRegistry.CoopRole.None, rows[0].Coop,
                "a realm with no sidecar is not shared and must not claim to be");
            Assert.AreEqual(RealmRegistry.CoopRole.Owned, rows[1].Coop,
                "a sidecar IS the definition of shared — the row has to say so");
            StringAssert.DoesNotContain("co-op", RealmSelectView.LabelFor(rows[0]));
            StringAssert.Contains("co-op", RealmSelectView.LabelFor(rows[1]));
        }

        /// <summary>The mark is a property of the realm, not of who is going in. Deciding it
        /// only when a Boarder is picked would blank every mark on the empty-pick path the
        /// screen starts on.</summary>
        [Test]
        public void TheMarkDoesNotDependOnWhichBoarderIsPicked()
        {
            ShareIt(Open, "OPEN");
            var boarder = RealmRegistry.CreateBoarder("MARKTEST");
            _boarderId = boarder.Id;

            var withNobody = new RealmSelectShared().BuildRows(LocalRows(), null);
            var withBoarder = new RealmSelectShared().BuildRows(LocalRows(), boarder.Id);

            Assert.AreEqual(RealmRegistry.CoopRole.Owned, withNobody[1].Coop);
            Assert.AreEqual(RealmRegistry.CoopRole.Owned, withBoarder[1].Coop);
        }

        /// <summary>A friend's realm is co-op too, but it is not the same row: DELETE here
        /// forgets a memory, where DELETE on an owned realm destroys the document. One mark on
        /// both would invite exactly the mistake that cannot be taken back.</summary>
        [Test]
        public void AFriendsRealmIsMarkedApartFromYourOwn_AndKeepsItsPlainName()
        {
            ShareIt(Open, "OPEN");
            var boarder = RealmRegistry.CreateBoarder("MARKTEST");
            _boarderId = boarder.Id;
            boarder.RememberRealm(Friend, "owner-Z", "VEGA");
            Assert.IsTrue(SaveService.SaveBoarder(boarder), "the fixture could not save the Boarder");

            var shared = new RealmSelectShared();
            var rows = shared.BuildRows(LocalRows(), boarder.Id);

            Assert.AreEqual(3, rows.Length, "the remembered realm is a row of its own");
            Assert.AreEqual(RealmRegistry.CoopRole.Guest, rows[2].Coop);
            Assert.IsTrue(shared.IsRemote(2), "the borrowed row still sits past the local ones");
            Assert.AreNotEqual(rows[1].Coop, rows[2].Coop,
                "yours-and-shared and a friend's are different rows and must read differently");

            // The name is the realm's name and nothing else — it travels into the store's
            // refusals and into the FORGET confirmation, where a glued-on badge reads as
            // part of what the player named the world.
            Assert.AreEqual("VEGA", rows[2].Name);
            StringAssert.Contains("VEGA", RealmSelectView.LabelFor(rows[2]));
            Assert.AreNotEqual(RealmSelectView.LabelFor(rows[1]),
                RealmSelectView.LabelFor(rows[2]));
        }

        // ------------------------------------------------------------------ the wording

        /// <summary>The regression this file exists for. Every label on RealmSelect draws in
        /// LiberationSans SDF, whose atlas is STATIC — a codepoint outside it renders as a
        /// blank or a box, which is worse than no mark at all. HudController.Weapon already
        /// lost U+25B8 to this at three sites. ASCII cannot be caught out by it.</summary>
        [Test]
        public void EveryMarkIsAscii_BecauseTheAtlasIsStatic()
        {
            foreach (RealmRegistry.CoopRole role in System.Enum.GetValues(typeof(RealmRegistry.CoopRole)))
            {
                string label = RealmSelectView.LabelFor(
                    new RealmRegistry.SaveRow("t-glyph", "NAME", true, role));
                for (int i = 0; i < label.Length; i++)
                    Assert.Less((int)label[i], 127,
                        role + " puts U+" + ((int)label[i]).ToString("X4")
                        + " in a row label; the row font's atlas will not have it");
            }
        }

        /// <summary>Co-op and "this build refuses it" are independent facts, and a realm can
        /// carry both. The refusal goes last because it is the one that changes what ENTER
        /// will do.</summary>
        [Test]
        public void ARefusedRealmStillShowsItsCoopMark_WithTheRefusalLast()
        {
            string label = RealmSelectView.LabelFor(new RealmRegistry.SaveRow(
                Open, "OPEN", false, RealmRegistry.CoopRole.Owned));

            StringAssert.StartsWith("OPEN", label);
            StringAssert.Contains("co-op", label);
            StringAssert.EndsWith("(cannot be opened)", label);
        }

        /// <summary>An unshared, openable realm gets its name and nothing else. A screen that
        /// badges every row has not told the player anything.</summary>
        [Test]
        public void APlainRealmGetsAPlainLabel()
        {
            Assert.AreEqual("SOLO", RealmSelectView.LabelFor(
                new RealmRegistry.SaveRow(Solo, "SOLO", true)));
        }
    }
}
