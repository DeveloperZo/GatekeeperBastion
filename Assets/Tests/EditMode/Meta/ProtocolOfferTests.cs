using NUnit.Framework;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Game;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The three-card offer as a rule: when it stands, who is on the cards, and
    /// whether a card may be taken. The last of those is the one that matters — a card the
    /// panel lights and TryClaim then refuses is a button that eats the press, so every
    /// refusal these two can reach is checked against the other here.</summary>
    public class ProtocolOfferTests
    {
        static MetaState Fresh()
        {
            var m = MetaState.CreateNew();
            m.EquippedProtocol = BoarderProtocolId.None;
            m.PayloadLoadoutLocked = false;
            return m;
        }

        [Test]
        public void OfferStands_OnlyWhileUnequippedAndNotMidBoarding()
        {
            Assert.IsFalse(ProtocolOffer.IsPickPending(null), "no save is not a pending pick");

            var m = Fresh();
            Assert.IsTrue(ProtocolOffer.IsPickPending(m));

            m.EquippedProtocol = BoarderProtocolId.Veil;
            Assert.IsFalse(ProtocolOffer.IsPickPending(m), "already chosen");

            m.EquippedProtocol = BoarderProtocolId.None;
            m.PayloadLoadoutLocked = true;
            Assert.IsFalse(ProtocolOffer.IsPickPending(m), "the Boarding is live");
        }

        /// <summary>The deck is authored with CardCount slots. If the shop ever widens past
        /// them this fails here rather than by silently dropping a protocol nobody can pick.</summary>
        [Test]
        public void CardCount_MatchesTheShop()
        {
            Assert.AreEqual(BoarderProtocol.ShopProtocols().Length, ProtocolOffer.CardCount);
        }

        [Test]
        public void Deal_FillsCatalogOrder_AndClearsTheRemainder()
        {
            var m = Fresh();
            var into = new BoarderProtocolId[ProtocolOffer.CardCount];
            int n = ProtocolOffer.Deal(m, into);

            var shop = BoarderProtocol.ShopProtocols();
            Assert.AreEqual(shop.Length, n);
            for (int i = 0; i < n; i++)
                Assert.AreEqual(shop[i], into[i], "card " + i + " is out of catalog order");

            // A wider buffer than the deal is cleared, so a stale id from a previous deal
            // cannot be painted onto a card the offer no longer fills.
            var wide = new BoarderProtocolId[ProtocolOffer.CardCount + 2];
            for (int i = 0; i < wide.Length; i++) wide[i] = BoarderProtocolId.Forge;
            int wideCount = ProtocolOffer.Deal(m, wide);
            for (int i = wideCount; i < wide.Length; i++)
                Assert.AreEqual(BoarderProtocolId.None, wide[i], "slot " + i + " kept a stale id");
        }

        [Test]
        public void Deal_NeverOffersTheOneAlreadyHeld()
        {
            var m = Fresh();
            m.EquippedProtocol = BoarderProtocolId.Veil;
            var into = new BoarderProtocolId[ProtocolOffer.CardCount];
            int n = ProtocolOffer.Deal(m, into);

            Assert.AreEqual(BoarderProtocol.ShopProtocols().Length - 1, n);
            for (int i = 0; i < n; i++)
                Assert.AreNotEqual(BoarderProtocolId.Veil, into[i]);
            Assert.AreEqual(BoarderProtocolId.None, into[n], "the vacated slot was not cleared");
        }

        [Test]
        public void Deal_TakesANullBufferAndANullSave()
        {
            Assert.AreEqual(0, ProtocolOffer.Deal(Fresh(), null));
            var into = new BoarderProtocolId[ProtocolOffer.CardCount];
            Assert.AreEqual(BoarderProtocol.ShopProtocols().Length, ProtocolOffer.Deal(null, into));
        }

        /// <summary>The load-bearing one. Every id and every save state either lights the card
        /// AND is claimable, or refuses the card AND is refused by the command.</summary>
        [Test]
        public void LegalPick_AgreesWithTryClaim_OnEveryIdAndEveryState()
        {
            var ids = new[]
            {
                BoarderProtocolId.None,
                BoarderProtocolId.Depot,
                BoarderProtocolId.Veil,
                BoarderProtocolId.Forge,
                (BoarderProtocolId)99,
            };
            var held = new[]
            {
                BoarderProtocolId.None,
                BoarderProtocolId.Depot,
                BoarderProtocolId.Veil,
            };

            foreach (var id in ids)
            {
                foreach (var have in held)
                {
                    for (int lockedStep = 0; lockedStep < 2; lockedStep++)
                    {
                        var m = Fresh();
                        m.EquippedProtocol = have;
                        m.PayloadLoadoutLocked = lockedStep == 1;

                        bool legal = ProtocolOffer.IsLegalPick(m, id);
                        bool claimed = BoarderProtocol.TryClaim(m, id);
                        Assert.AreEqual(legal, claimed,
                            "card and command disagree: id=" + id + " held=" + have
                            + " locked=" + (lockedStep == 1));
                    }
                }
            }
        }

        [Test]
        public void RefuseReason_IsNullOnlyWhenTheCardCanBeTaken_AndNeverEmpty()
        {
            var m = Fresh();
            Assert.IsNull(ProtocolOffer.RefuseReason(m, BoarderProtocolId.Forge));

            Assert.IsNotNull(ProtocolOffer.RefuseReason(null, BoarderProtocolId.Forge));
            Assert.IsNotNull(ProtocolOffer.RefuseReason(m, BoarderProtocolId.None));
            Assert.IsNotNull(ProtocolOffer.RefuseReason(m, (BoarderProtocolId)99));

            m.EquippedProtocol = BoarderProtocolId.Forge;
            Assert.IsNotNull(ProtocolOffer.RefuseReason(m, BoarderProtocolId.Forge));

            m.EquippedProtocol = BoarderProtocolId.None;
            m.PayloadLoadoutLocked = true;
            string locked = ProtocolOffer.RefuseReason(m, BoarderProtocolId.Forge);
            Assert.IsNotNull(locked);
            // The view paints this string under the card. An empty one is a dark button with
            // no reason, which is the defect the refusal line exists to prevent.
            Assert.IsFalse(string.IsNullOrEmpty(locked));
        }

        /// <summary>Every dealt card can answer every row the view writes. An empty row on a
        /// dealt id is the card reading as broken, the same rule BoarderSheets states.</summary>
        [Test]
        public void EveryDealtCard_HasAFullSheet()
        {
            var m = Fresh();
            var into = new BoarderProtocolId[ProtocolOffer.CardCount];
            int n = ProtocolOffer.Deal(m, into);
            Assert.Greater(n, 0);
            for (int i = 0; i < n; i++)
            {
                var sheet = BoarderProtocol.SheetOf(into[i]);
                Assert.IsFalse(string.IsNullOrEmpty(sheet.Name), "name on " + into[i]);
                Assert.IsFalse(string.IsNullOrEmpty(sheet.Doctrine), "doctrine on " + into[i]);
                Assert.IsFalse(string.IsNullOrEmpty(sheet.Crew), "crew on " + into[i]);
                Assert.IsFalse(string.IsNullOrEmpty(sheet.Personal), "personal on " + into[i]);
                Assert.IsFalse(string.IsNullOrEmpty(sheet.Passive), "passive on " + into[i]);
            }
        }

        // ================================================================
        //  The early door: CREW opens for the claim, and for nothing else
        // ================================================================

        /// <summary>A Boarder with the ship fight behind them and no Protocol gets into CREW
        /// before the menu unlocks. This is the 2026-08-27 playthrough's bug: the gate held
        /// the claim shut for the whole first Boarding, which is exactly the Boarding a
        /// player has no Protocol for.</summary>
        [Test]
        public void CrewOpensEarly_ForAnUnclaimedProtocol_AndShutsAgainOnceOneIsHeld()
        {
            var m = MetaState.CreateNew();
            DemoProgress.SkipShipTutorial(m);   // the ship fight is behind us; CREW is not hidden
            Assert.IsFalse(m.DemoCrewMenuUnlocked,
                "the fixture starts past the unlock, so it proves nothing about the early door");

            Assert.IsTrue(DemoProgress.CrewOpensForProtocolClaim(m),
                "a Boarder with no Protocol is refused the one room that hands them out");
            Assert.AreEqual(DemoStationVerdict.HealOnly, DemoProgress.CrewVerdict(m),
                "the early door must not promote the whole menu — only the claim comes through");

            Assert.IsTrue(BoarderProtocol.TryClaim(m, BoarderProtocolId.Depot),
                "the claim this door exists for was refused");
            Assert.IsFalse(DemoProgress.CrewOpensForProtocolClaim(m),
                "the door stayed open after the claim — the demo's pacing is gone for every "
                + "other row in the station");
        }

        [Test]
        public void CrewEarlyDoor_IsShut_WhileTheStationIsHidden_AndOnceTheMenuUnlocks()
        {
            // Hidden outranks it: before the ship fight CREW is not a room yet, and an early
            // door into a station the player cannot see is a station that answers a key press
            // from nowhere.
            var hidden = MetaState.CreateNew();
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.CrewVerdict(hidden));
            Assert.IsFalse(DemoProgress.CrewOpensForProtocolClaim(hidden),
                "the early door opened a station that is meant to be invisible");

            // And once the menu unlocks on its own schedule the special case retires — the
            // station is Open, so a second path into it would be a rule with nothing to do.
            var unlocked = MetaState.CreateNew();
            DemoProgress.SkipShipTutorial(unlocked);
            unlocked.DemoCrewMenuUnlocked = true;
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.CrewVerdict(unlocked));
            Assert.IsFalse(DemoProgress.CrewOpensForProtocolClaim(unlocked),
                "the early door is still live after the real one opened");
        }

        [Test]
        public void CrewEarlyDoor_IsShut_MidBoarding()
        {
            // The loadout is locked for the Run, so three dead cards would read worse than the
            // closed door. IsPickPending already refuses this; the door inherits that refusal
            // rather than restating it.
            var m = MetaState.CreateNew();
            DemoProgress.SkipShipTutorial(m);
            m.PayloadLoadoutLocked = true;
            Assert.IsFalse(DemoProgress.CrewOpensForProtocolClaim(m),
                "CREW opened mid-Boarding for a claim the Run will refuse");
        }
    }
}
