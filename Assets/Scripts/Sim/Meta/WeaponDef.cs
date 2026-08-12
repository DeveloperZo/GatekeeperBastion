using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Immutable combat profile of one <see cref="WeaponId"/>, produced by
    /// <see cref="WeaponCatalog.Def"/>. Mirrors <see cref="GatekeeperStats"/>: if a firing mode
    /// is not expressible here it belongs in PlayerGun, not as a new subclass.</summary>
    [Serializable]
    public struct WeaponDef
    {
        public WeaponSlot slot;
        public string displayName;
        public long cost;

        public float damage;
        public float shotsPerSecond;
        public float clipSize;
        public float reloadSeconds;
        public float range;

        /// <summary>1 = single hitscan; &gt;1 = that many rays inside <see cref="spreadDegrees"/>.</summary>
        public int pellets;

        /// <summary>Cone half-angle in degrees; ignored when pellets == 1.</summary>
        public float spreadDegrees;

        /// <summary>Bodies past the first a ray may continue through (0 = stop on first hit).</summary>
        public int pierceCount;

        /// <summary>Flat fraction of falloff-scaled damage applied to every damaged body after
        /// the first. Not compounding — body 2 and body 3 take the same fraction. 1 = no
        /// pierce penalty.</summary>
        public float pierceDamageMul;

        /// <summary>Metres; damage scales linearly to zero from here to <see cref="range"/>.
        /// 0 or negative = no falloff.</summary>
        public float falloffStart;

        public float tracerR, tracerG, tracerB;
        public float tracerWidth;
        public WeaponCrosshair crosshair;

        /// <summary>Activated ability. <see cref="AbilityId.None"/> is an explicit empty
        /// descriptor — never invent a default for None / throwaway rows.</summary>
        public AbilityId abilityId;

        /// <summary>Player-facing ability name. Empty when <see cref="abilityId"/> is None.</summary>
        public string abilityName;

        /// <summary>Wall-clock cooldown in seconds. 0 when there is no ability.</summary>
        public float abilityCooldown;
    }
}
