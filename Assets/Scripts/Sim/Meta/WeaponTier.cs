using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Weapon upgrade purchase cost curves. Attribute multipliers live in
    /// <see cref="WeaponWheel"/> — Tier is derived as total wheel points / 11.</summary>
    public static class WeaponTier
    {
        public const int MaxTier = 10;

        /// <summary>Primary-slot cost of the next purchase from <paramref name="centredCount"/>
        /// already centred on that spoke. Linear: 400 + 200 * centredCount → 400…2200.
        /// Cumulative 13,000 to stack ten on one spoke. Reconstruction of pre-wheel spends
        /// still reads this curve — those purchases had no per-spoke count.</summary>
        public static long Cost(int centredCount)
        {
            if (centredCount < 0 || centredCount >= MaxTier)
                throw new ArgumentOutOfRangeException("centredCount",
                    "Spoke cost is only defined for centredCount 0..9.");
            return 400L + 200L * centredCount;
        }

        /// <summary>Slot-aware cost of the next purchase centred on one spoke. Secondaries
        /// ride a cheaper lane: 150 + 150 * centredCount → 150…1500, cumulative 8,250 —
        /// the first two on the same spoke (150 + 300) equal one wave-1 clear at Scavenging 0
        /// (Zo, 2026-08-14). A first buy on a different spoke stays 150. Primaries keep
        /// <see cref="Cost(int)"/>. Neighbour spill does not raise a neighbour's price.</summary>
        public static long Cost(WeaponSlot slot, int centredCount)
        {
            if (slot != WeaponSlot.Secondary) return Cost(centredCount);
            if (centredCount < 0 || centredCount >= MaxTier)
                throw new ArgumentOutOfRangeException("centredCount",
                    "Spoke cost is only defined for centredCount 0..9.");
            return 150L + 150L * centredCount;
        }

        /// <summary>Cheapest remaining centred buy on <paramref name="id"/>, or -1 when the
        /// weapon is at MaxPurchases or has no purchasable spoke.</summary>
        public static long CheapestSpokeCost(MetaState meta, WeaponId id)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            if (id == WeaponId.None) return -1L;
            if (meta.TierOf(id) >= MaxTier) return -1L;
            var slot = WeaponCatalog.Def(id).slot;
            long cheapest = long.MaxValue;
            for (int a = 0; a < WeaponWheel.AttrCount; a++)
            {
                var attr = (WheelAttr)a;
                if (!WeaponWheel.SpokePurchasable(id, attr)) continue;
                long c = Cost(slot, meta.WheelCentresOf(id, attr));
                if (c < cheapest) cheapest = c;
            }
            return cheapest == long.MaxValue ? -1L : cheapest;
        }

        /// <summary>True when the weapon is below MaxTier and the bank covers <see cref="Cost(int)"/>.</summary>
        public static bool CanBuy(MetaState meta, int currentTier)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            if (currentTier < 0 || currentTier >= MaxTier) return false;
            return meta.BankedSalvage >= Cost(currentTier);
        }

        /// <summary>Total salvage paid to reach <paramref name="tier"/> from T0 on the
        /// primary/historical curve. <see cref="MetaState.ReconstructSpentFromLevels"/> reads
        /// this on purpose even for Secondaries: reconstruction only runs for pre-wheel
        /// migration and tamper repair, and every pre-wheel purchase was made at this curve.</summary>
        public static long SpentToReach(int tier)
        {
            if (tier < 0) tier = 0;
            if (tier > MaxTier) tier = MaxTier;
            long spent = 0;
            for (int t = 0; t < tier; t++)
                spent += Cost(t);
            return spent;
        }

        /// <summary>Slot-aware cumulative cost — sum of <see cref="Cost(WeaponSlot,int)"/>.</summary>
        public static long SpentToReach(WeaponSlot slot, int tier)
        {
            if (tier < 0) tier = 0;
            if (tier > MaxTier) tier = MaxTier;
            long spent = 0;
            for (int t = 0; t < tier; t++)
                spent += Cost(slot, t);
            return spent;
        }
    }
}
