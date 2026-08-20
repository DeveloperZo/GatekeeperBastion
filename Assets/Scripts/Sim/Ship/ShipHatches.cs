namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The Ship's three hatch doors and the seat-count assignment rule
    /// (Docs/Demo - Coop/README.md): one seat opens the middle hatch, two seats open the
    /// outer pair, three open all three. Assignment, not choice — two seats land at the
    /// far ends of the hull by construction. Also owns the packed wire form the host
    /// replicates: bits 0..2 are the open mask by hatch index, bits 4+2i carry the owning
    /// seat slot per hatch (3 = none). Packed 0 means "not yet assigned" — every real
    /// assignment has a non-zero mask.
    /// </summary>
    public sealed class ShipHatches
    {
        /// <summary>Hatch doors on the hull. Fixed; the seat cap is derived from it.</summary>
        public const int Count = 3;

        /// <summary>Index of the middle hatch — the solo door.</summary>
        public const int MiddleIndex = 1;

        /// <summary>2-bit owner sentinel in the packed form: no seat owns this hatch.</summary>
        public const int NoOwner = 3;

        readonly bool[] _open = new bool[Count];

        /// <summary>Stable player-facing id of a hatch index: 1..3, bow to stern.</summary>
        public static int IdOf(int index)
        {
            return index + 1;
        }

        /// <summary>Which hatch indices open for a seat count: the middle for one seat,
        /// the outer pair for two, all three for three. Counts below one read as one
        /// (there is always a local seat); counts above three read as three (the guest
        /// cap holds the party at the hatch count).</summary>
        public static int[] OpenFor(int seatCount)
        {
            if (seatCount <= 1) return new[] { MiddleIndex };
            if (seatCount == 2) return new[] { 0, 2 };
            return new[] { 0, 1, 2 };
        }

        /// <summary>Packed assignment for the given seat slots in ascending order: the
        /// i-th slot owns the i-th open hatch, so at two seats the lower slot takes
        /// hatch 1 and the higher takes hatch 3.</summary>
        public static int PackFor(int[] seatSlotsAscending, int seatCount)
        {
            int packed = 0;
            for (int i = 0; i < Count; i++)
                packed |= NoOwner << OwnerShift(i);

            int[] open = OpenFor(seatCount);
            for (int i = 0; i < open.Length; i++)
            {
                int index = open[i];
                packed |= 1 << index;
                int owner = i < seatSlotsAscending.Length ? (seatSlotsAscending[i] & NoOwner) : NoOwner;
                packed &= ~(NoOwner << OwnerShift(index));
                packed |= owner << OwnerShift(index);
            }
            return packed;
        }

        /// <summary>True when the hatch at this index is open in the packed assignment.</summary>
        public static bool IsOpen(int packed, int index)
        {
            return (packed & (1 << index)) != 0;
        }

        /// <summary>Owning seat slot of the hatch at this index, or -1 when sealed.</summary>
        public static int OwnerSlot(int packed, int index)
        {
            if (!IsOpen(packed, index)) return -1;
            int owner = (packed >> OwnerShift(index)) & NoOwner;
            return owner == NoOwner ? -1 : owner;
        }

        static int OwnerShift(int index)
        {
            return 4 + 2 * index;
        }

        /// <summary>Adopt a packed assignment into the per-run open flags.</summary>
        public void Apply(int packed)
        {
            for (int i = 0; i < Count; i++)
                _open[i] = IsOpen(packed, i);
        }

        /// <summary>Open flag of the hatch at this index for the current run.</summary>
        public bool IsOpenAt(int index)
        {
            return _open[index];
        }
    }
}
