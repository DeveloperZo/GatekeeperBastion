using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Weapon upgrade purchase cost curve. Attribute multipliers live in
    /// <see cref="WeaponWheel"/> — Tier is derived as total wheel points / 11.</summary>
    public static class WeaponTier
    {
        public const int MaxTier = 10;

        /// <summary>Salvage cost to buy the next purchase from <paramref name="currentTier"/>.
        /// Linear: 400 + 200 * currentTier → 400…2200. Cumulative 13,000.</summary>
        public static long Cost(int currentTier)
        {
            if (currentTier < 0 || currentTier >= MaxTier)
                throw new ArgumentOutOfRangeException("currentTier",
                    "Tier cost is only defined for currentTier 0..9.");
            return 400L + 200L * currentTier;
        }

        /// <summary>True when the weapon is below MaxTier and the bank covers <see cref="Cost"/>.</summary>
        public static bool CanBuy(MetaState meta, int currentTier)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            if (currentTier < 0 || currentTier >= MaxTier) return false;
            return meta.BankedSalvage >= Cost(currentTier);
        }

        /// <summary>Total salvage paid to reach <paramref name="tier"/> from T0 (sum of step costs).</summary>
        public static long SpentToReach(int tier)
        {
            if (tier < 0) tier = 0;
            if (tier > MaxTier) tier = MaxTier;
            long spent = 0;
            for (int t = 0; t < tier; t++)
                spent += Cost(t);
            return spent;
        }
    }
}
