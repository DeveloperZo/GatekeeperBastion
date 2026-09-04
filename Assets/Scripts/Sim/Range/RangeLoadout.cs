using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The kit a practice range session opens with, chosen at the door: one protocol,
    /// one Primary, one Secondary, and the wheel points each weapon carries.
    ///
    /// A weapon the realm OWNS carries the realm's own wheel points, so the range answers the
    /// question about the gun in the Boarder's hands. A weapon it does not own arrives at base
    /// (zero points) and is flagged, so the readout says which of the two it was judging. The
    /// protocol is any of the three the shop sells, claimed or not — the range is the one
    /// place VEIL and FORGE can be felt before they are on sale.
    ///
    /// Plain data. It is built on the Ship, carried across the scene load on
    /// <c>GameSession.PendingRangeLoadout</c>, and adopted by the range console into a fresh,
    /// tainted state; nothing here touches the realm it was read from.</summary>
    public struct RangeLoadout
    {
        public BoarderProtocolId Protocol;
        public WeaponId Primary;
        public WeaponId Secondary;

        /// <summary>Length <see cref="WeaponWheel.AttrCount"/>, indexed by <see cref="WheelAttr"/>.</summary>
        public int[] PrimaryPoints;
        public int[] SecondaryPoints;

        /// <summary>True when the realm did not own the weapon, so its points are base.</summary>
        public bool PrimaryIsBase;
        public bool SecondaryIsBase;

        public int PrimaryTier { get { return WeaponWheel.DerivedTier(PrimaryPoints); } }
        public int SecondaryTier { get { return WeaponWheel.DerivedTier(SecondaryPoints); } }

        /// <summary>Build the kit from a realm's ownership. Refuses (throws) an id that is not
        /// a catalogued shop weapon, a weapon in the wrong slot, or a protocol the shop does
        /// not sell — the door only offers ids that pass, so a throw here is a caller bug.</summary>
        public static RangeLoadout From(MetaState realm, BoarderProtocolId protocol,
            WeaponId primary, WeaponId secondary)
        {
            if (realm == null) throw new ArgumentNullException("realm");
            if (!BoarderProtocol.IsCatalogued(protocol))
                throw new ArgumentException(protocol + " is not a protocol the shop sells", "protocol");
            RequireSlot(primary, WeaponSlot.Primary, "primary");
            RequireSlot(secondary, WeaponSlot.Secondary, "secondary");

            var kit = new RangeLoadout
            {
                Protocol = protocol,
                Primary = primary,
                Secondary = secondary,
                PrimaryPoints = new int[WeaponWheel.AttrCount],
                SecondaryPoints = new int[WeaponWheel.AttrCount],
            };
            kit.PrimaryIsBase = !FillPoints(realm, primary, kit.PrimaryPoints);
            kit.SecondaryIsBase = !FillPoints(realm, secondary, kit.SecondaryPoints);
            return kit;
        }

        /// <summary>One line for the log and the door: protocol, then each weapon with its
        /// derived Tier and " base" when the realm did not own it.</summary>
        public string Summary()
        {
            return BoarderProtocol.ProtocolName(Protocol)
                + ", " + WeaponLine(Primary, PrimaryTier, PrimaryIsBase)
                + ", " + WeaponLine(Secondary, SecondaryTier, SecondaryIsBase);
        }

        static string WeaponLine(WeaponId id, int tier, bool isBase)
        {
            return WeaponCatalog.Def(id).displayName + " T" + tier + (isBase ? " base" : "");
        }

        /// <summary>Copies the realm's points when it owns the weapon and returns true; leaves
        /// zeros and returns false when it does not.</summary>
        static bool FillPoints(MetaState realm, WeaponId id, int[] dest)
        {
            if (!realm.Owns(id))
            {
                for (int a = 0; a < dest.Length; a++) dest[a] = 0;
                return false;
            }
            realm.CopyPointsInto(id, dest);
            return true;
        }

        static void RequireSlot(WeaponId id, WeaponSlot slot, string param)
        {
            if (!WeaponCatalog.IsAvailable(id))
                throw new ArgumentException(id + " is not a weapon the ARMORY sells", param);
            if (WeaponCatalog.Def(id).slot != slot)
                throw new ArgumentException(id + " is not a " + slot, param);
        }
    }
}
