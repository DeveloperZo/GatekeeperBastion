using System.IO;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The automatic protocol grant (Zo, 2026-09-04) and the two log lines that make
    /// it readable afterwards.
    ///
    /// The grant exists because the play data could not tell a FORGE run from a protocol-less
    /// one: the 2026-09-04 Boarding was carried by Overcharge, Outrider and EMP, and neither
    /// the run-begins line nor the summary line said a protocol was equipped at all. Two
    /// populations were being averaged into one dial.</summary>
    public class ProtocolGrantTests
    {
        [Test]
        public void TheGrant_IsASoldProtocol_ThatOwnsNoPassive()
        {
            BoarderProtocolId grant = BoarderProtocol.GrantedProtocol;
            Assert.IsTrue(BoarderProtocol.IsCatalogued(grant),
                "the grant must be a protocol the build actually sells, or every switch on it "
                + "falls to a default arm");
            Assert.AreEqual(0, BoarderProtocol.PassiveCount(grant),
                "a grant that owned a passive would have to hand one out silently on load");
            Assert.AreNotEqual(BoarderAbilityId.None, BoarderProtocol.CrewAbilityOf(grant),
                "the grant is only worth granting if it carries the two abilities");
            Assert.AreNotEqual(BoarderAbilityId.None, BoarderProtocol.PersonalAbilityOf(grant));
        }

        [Test]
        public void NoLoadPath_ProducesABoarderWithoutAProtocol()
        {
            Assert.AreEqual(BoarderProtocol.GrantedProtocol,
                MetaState.CreateNew().EquippedProtocol, "a fresh Boarder");

            // An old save that predates the key entirely.
            var missing = BoarderState.FromJson("{\"SaveVersion\":13}");
            Assert.AreEqual(BoarderProtocol.GrantedProtocol, missing.EquippedProtocol,
                "a save with no protocol key");

            // A save that explicitly wrote None, which every save before 2026-09-04 could.
            var none = BoarderState.FromJson(
                "{\"SaveVersion\":13,\"EquippedProtocol\":0,\"EquippedPassive\":0}");
            Assert.AreEqual(BoarderProtocol.GrantedProtocol, none.EquippedProtocol,
                "a save that wrote None");

            // An id this build does not sell.
            var unknown = BoarderState.FromJson(
                "{\"SaveVersion\":13,\"EquippedProtocol\":77,\"EquippedPassive\":0}");
            Assert.AreEqual(BoarderProtocol.GrantedProtocol, unknown.EquippedProtocol,
                "a retired or over-sold id");
        }

        [Test]
        public void TheGrant_SurvivesARoundTrip_AndDoesNotOverwriteAClaim()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(BoarderProtocol.TryClaim(m, BoarderProtocolId.Forge));
            var loaded = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.AreEqual(BoarderProtocolId.Forge, loaded.EquippedProtocol,
                "the grant must not stamp over a protocol the player claimed");
        }

        /// <summary>Source pins. The two lines live in the Game layer, which the offline
        /// harness cannot compile, so the field's presence is pinned by reading the source
        /// rather than by running it.</summary>
        [Test]
        public void RunBeginsLine_CarriesTheProtocolAndPassive()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Bastion/GameController.Client.cs"));
            StringAssert.Contains("protocol=", src);
            StringAssert.Contains("passive=", src);
            StringAssert.Contains("BoarderProtocol.ProtocolName", src);
            StringAssert.Contains("BoarderProtocol.DisplayName", src);
        }

        [Test]
        public void SummaryLine_CarriesTheProtocol_AndMovedItsSchema()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Bastion/GameController.EndScreen.cs"));
            StringAssert.Contains("summary schema=2", src);
            StringAssert.Contains("protocol=", src);
            Assert.IsFalse(src.Contains("summary schema=1"),
                "the schema number moves with the field set or it is decoration");
        }
    }
}
