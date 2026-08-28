namespace GatekeepersProtocol.Sim
{
    /// <summary>The three-card offer at CREW QUARTERS: whether the offer stands, which
    /// protocols are on the cards, and whether a card may be taken.
    ///
    /// These are questions about the save, not about a panel, which is why they are here and
    /// not in the view. <see cref="RefuseReason"/> asks exactly what
    /// <see cref="BoarderProtocol.TryClaim"/> asks, in the same order, so a lit card and the
    /// command behind it cannot disagree — a card that takes the press and does nothing is
    /// the defect this type exists to make impossible.</summary>
    public static class ProtocolOffer
    {
        /// <summary>Cards dealt when the offer stands. The authored deck holds this many
        /// slots; <see cref="Deal"/> is the only thing allowed to decide it is fewer.</summary>
        public const int CardCount = 3;

        /// <summary>True while the Boarder has no protocol and is in a position to take one.
        ///
        /// Mid-Boarding the claim is refused (the loadout is locked for the Run), and three
        /// dead cards read worse than the catalog, which at least says what it is showing.
        /// A null document is not pending: nothing is decidable without a save.</summary>
        public static bool IsPickPending(MetaState m)
        {
            if (m == null) return false;
            if (m.PayloadLoadoutLocked) return false;
            return m.EquippedProtocol == BoarderProtocolId.None;
        }

        /// <summary>Fill <paramref name="into"/> with the protocols to put on cards, in
        /// catalog order, and return how many were written. The remainder is cleared to
        /// None so a stale id from a previous deal cannot be painted.
        ///
        /// The one held is never dealt: an offer to take what you already have is not an
        /// offer. Caller owns the buffer, so a repaint allocates nothing of its own.</summary>
        public static int Deal(MetaState m, BoarderProtocolId[] into)
        {
            if (into == null) return 0;
            BoarderProtocolId held = m != null ? m.EquippedProtocol : BoarderProtocolId.None;
            BoarderProtocolId[] shop = BoarderProtocol.ShopProtocols();
            int n = 0;
            for (int i = 0; i < shop.Length && n < into.Length; i++)
            {
                if (shop[i] == held) continue;
                into[n++] = shop[i];
            }
            for (int i = n; i < into.Length; i++)
                into[i] = BoarderProtocolId.None;
            return n;
        }

        public static bool IsLegalPick(MetaState m, BoarderProtocolId id)
        {
            return RefuseReason(m, id) == null;
        }

        /// <summary>Why this card cannot be taken, or null when it can. Player-facing copy:
        /// the view paints it under the card rather than dimming a button with no reason.</summary>
        public static string RefuseReason(MetaState m, BoarderProtocolId id)
        {
            if (m == null) return "no save loaded";
            if (m.PayloadLoadoutLocked) return "the Boarding is live";
            if (id == BoarderProtocolId.None) return "no protocol on this card";
            if (m.EquippedProtocol == id) return "already yours";
            if (!BoarderProtocol.IsCatalogued(id)) return "not offered in this build";
            return null;
        }
    }
}
