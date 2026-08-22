namespace GatekeepersProtocol.Sim
{
    /// <summary>Static pure catalog of drone-bay payloads. One row today (the decoy). Holds no
    /// state. A payload is not a gun — no damage, clip, or reload fields belong here.</summary>
    public static class PayloadCatalog
    {
        /// <summary>One payload row. Unknown ids return an empty stub.</summary>
        public struct PayloadDef
        {
            public PayloadId id;
            public string displayName;
            public long cost;
            public int charges;
            public float hp;
            public float pullRadius;
            public float placeRange;
            public int maxLive;
        }

        /// <summary>Measured Default effective Hatch dps used to convert decoy damage into
        /// gatekeeper-seconds (<c>4 / 0.7 * 0.6 ≈ 3.43</c>). Shared by <see cref="DecoyState"/>.</summary>
        public const float DecoyEffectiveDps = 3.43f;

        public static PayloadDef Def(PayloadId id)
        {
            switch (id)
            {
                case PayloadId.Decoy:
                    return new PayloadDef
                    {
                        id = PayloadId.Decoy,
                        displayName = "Decoy",
                        cost = 250L,
                        charges = 3,
                        hp = 120f,
                        pullRadius = 12f,
                        placeRange = 20f,
                        maxLive = 1
                    };
                default:
                    return new PayloadDef
                    {
                        id = PayloadId.None,
                        displayName = "None",
                        cost = 0L,
                        charges = 0,
                        hp = 0f,
                        pullRadius = 0f,
                        placeRange = 0f,
                        maxLive = 0
                    };
            }
        }

        /// <summary>True when this build has a row for the id. NOT the same question as
        /// <see cref="OwnershipBit"/>, which is a mask helper: C# masks a shift count, so
        /// <c>1 &lt;&lt; 99</c> is a perfectly non-zero bit for an id that does not exist.</summary>
        public static bool IsCatalogued(PayloadId id)
        {
            return id == PayloadId.None || id == PayloadId.Decoy;
        }

        /// <summary>Bit in <see cref="MetaState.OwnedPayloadMask"/> for this id. None → 0.</summary>
        public static int OwnershipBit(PayloadId id)
        {
            if (id == PayloadId.None) return 0;
            return 1 << (int)id;
        }
    }
}
