using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>One spoke on the upgrade wheel. Order is the enum ordinal used in save arrays —
    /// never renumber.</summary>
    public enum WheelAttr
    {
        Reload = 0,
        Clip = 1,
        Damage = 2,
        FireRate = 3,
        Ability = 4,
    }

    /// <summary>Pure arithmetic for the five-spoke upgrade wheel. Rings are per weapon; the
    /// per-point table is global. No state.</summary>
    public static class WeaponWheel
    {
        public const int AttrCount = 5;
        public const int FocusAward = 5;
        public const int NeighbourAward = 3;
        public const int PointsPerPurchase = 11; // 5 + 3 + 3
        public const int MaxPurchases = WeaponTier.MaxTier;

        /// <summary>Points on one spoke after MaxPurchases all centred there (5 × 10).</summary>
        public const int FocusAtMax = FocusAward * MaxPurchases; // 50
        /// <summary>Points on each neighbour after the same history (3 × 10).</summary>
        public const int NeighbourAtMax = NeighbourAward * MaxPurchases; // 30

        // Compound per-point multipliers. Step size frozen so one purchase still clears ~20% noise;
        // focus-at-max (50) is above the old T7 focus-35 anchors — Wave sizing unknown, needs play data.
        public const float DamagePerPoint = 1.0456f;
        public const float ClipPerPoint = 1.0283f;
        public const float ReloadPerPoint = 0.98035f;  // -1.965% → focus 35 ≈ ×0.50; focus 50 ≈ ×0.37
        public const float FireRatePerPoint = 1.0117f;

        /// <summary>Cyclic ring for a weapon, or null when the ring is not authored yet
        /// (SEC-D until its ability is decided).</summary>
        public static WheelAttr[] RingOf(WeaponId id)
        {
            switch (id)
            {
                // Strength Reload, weakness Damage — two apart. Ability adjacent to Reload
                // so the readiness build feeds Regen Surge; Damage's neighbours exclude Clip
                // to blunt duty-cycle compounding on a 1.4 s reload (never-rival).
                case WeaponId.Mk1Sidearm:
                    return new[]
                    {
                        WheelAttr.Damage, WheelAttr.Ability, WheelAttr.Reload,
                        WheelAttr.Clip, WheelAttr.FireRate
                    };
                // Strength Damage, weakness Reload — two apart. Ability adjacent to Damage.
                case WeaponId.Br12Shotgun:
                    return new[]
                    {
                        WheelAttr.Reload, WheelAttr.Clip, WheelAttr.Damage,
                        WheelAttr.Ability, WheelAttr.FireRate
                    };
                // Strength Damage, weakness FireRate — two apart. Ability adjacent to FireRate.
                case WeaponId.Mr35Rifle:
                    return new[]
                    {
                        WheelAttr.Reload, WheelAttr.Damage, WheelAttr.Clip,
                        WheelAttr.Ability, WheelAttr.FireRate
                    };
                // Strength Damage, weakness FireRate — two apart. Ability adjacent to FireRate.
                // Clip/Reload swapped vs MR-35 so rings stay unique (RingsAreUniqueAcrossShopWeapons).
                case WeaponId.PriC:
                    return new[]
                    {
                        WheelAttr.Clip, WheelAttr.Damage, WheelAttr.Reload,
                        WheelAttr.Ability, WheelAttr.FireRate
                    };
                default:
                    return null;
            }
        }

        public static bool HasRing(WeaponId id) => RingOf(id) != null;

        /// <summary>Declared identity for criterion 7. False when the weapon has no ring.</summary>
        public static bool TryIdentity(WeaponId id, out WheelAttr strength, out WheelAttr weakness)
        {
            switch (id)
            {
                case WeaponId.Mk1Sidearm:
                    strength = WheelAttr.Reload;
                    weakness = WheelAttr.Damage;
                    return true;
                case WeaponId.Br12Shotgun:
                    strength = WheelAttr.Damage;
                    weakness = WheelAttr.Reload;
                    return true;
                case WeaponId.Mr35Rifle:
                    strength = WheelAttr.Damage;
                    weakness = WheelAttr.FireRate;
                    return true;
                case WeaponId.PriC:
                    strength = WheelAttr.Damage;
                    weakness = WheelAttr.FireRate;
                    return true;
                default:
                    strength = WheelAttr.Reload;
                    weakness = WheelAttr.Reload;
                    return false;
            }
        }

        /// <summary>Award one purchase centred on <paramref name="centre"/>. Mutates a length-5
        /// counter array indexed by <see cref="WheelAttr"/>.</summary>
        public static void Award(int[] points, WheelAttr[] ring, WheelAttr centre)
        {
            if (points == null || points.Length != AttrCount)
                throw new ArgumentException("points must be length " + AttrCount, "points");
            if (ring == null || ring.Length != AttrCount)
                throw new ArgumentException("ring must be length " + AttrCount, "ring");

            int centreIndex = -1;
            for (int i = 0; i < AttrCount; i++)
            {
                if (ring[i] == centre) { centreIndex = i; break; }
            }
            if (centreIndex < 0)
                throw new ArgumentOutOfRangeException("centre", centre, "not on this ring");

            for (int i = 0; i < AttrCount; i++)
            {
                int dist = RingDistance(centreIndex, i, AttrCount);
                int add = dist == 0 ? FocusAward : dist == 1 ? NeighbourAward : 0;
                points[(int)ring[i]] += add;
            }
        }

        /// <summary>Shortest arc distance on a cycle of <paramref name="len"/>.</summary>
        public static int RingDistance(int a, int b, int len)
        {
            int d = Math.Abs(a - b);
            int other = len - d;
            return d < other ? d : other;
        }

        /// <summary>Pre-wheel flat Tier N → N×11 points as damage+clip only (6N / 5N). Matches
        /// "old curve spent them" — every historical purchase raised both, none of the others.</summary>
        public static void DistributeFlatTier(int[] points, int tier)
        {
            if (points == null || points.Length != AttrCount)
                throw new ArgumentException("points must be length " + AttrCount, "points");
            if (tier < 0) tier = 0;
            if (tier > MaxPurchases) tier = MaxPurchases;
            for (int i = 0; i < AttrCount; i++) points[i] = 0;
            points[(int)WheelAttr.Damage] = 6 * tier;
            points[(int)WheelAttr.Clip] = 5 * tier;
        }

        public static int TotalPoints(int[] points)
        {
            if (points == null) return 0;
            int t = 0;
            for (int i = 0; i < points.Length; i++) t += points[i];
            return t;
        }

        public static int DerivedTier(int[] points) => TotalPoints(points) / PointsPerPurchase;

        public static float DamageMul(int points) => (float)Math.Pow(DamagePerPoint, ClampPts(points));
        public static float ClipMul(int points) => (float)Math.Pow(ClipPerPoint, ClampPts(points));
        public static float ReloadMul(int points) => (float)Math.Pow(ReloadPerPoint, ClampPts(points));
        public static float FireRateMul(int points) => (float)Math.Pow(FireRatePerPoint, ClampPts(points));

        public static float EffectiveDamage(WeaponDef def, int damagePoints) =>
            def.damage * DamageMul(damagePoints);

        public static int EffectiveClip(WeaponDef def, int clipPoints)
        {
            // No catalog product lands on exact .5 today; round halves away from zero and floor at 1.
            int clip = (int)Math.Round(
                def.clipSize * ClipMul(clipPoints), MidpointRounding.AwayFromZero);
            return clip < 1 ? 1 : clip;
        }

        public static float EffectiveFireRate(WeaponDef def, int fireRatePoints)
        {
            float rate = def.shotsPerSecond * FireRateMul(fireRatePoints);
            return rate < 0.01f ? 0.01f : rate;
        }

        public static float EffectiveReload(WeaponDef def, int reloadPoints)
        {
            float r = def.reloadSeconds * ReloadMul(reloadPoints);
            return r < 0.01f ? 0.01f : r;
        }

        /// <summary>Seconds of continuous fire before the clip empties.</summary>
        public static float BurstSeconds(WeaponDef def, int clipPoints, int fireRatePoints)
        {
            return EffectiveClip(def, clipPoints) / EffectiveFireRate(def, fireRatePoints);
        }

        public static float EffectiveDps(WeaponDef def, int[] points)
        {
            if (points == null || points.Length != AttrCount)
                throw new ArgumentException("points must be length " + AttrCount, "points");
            float dmgPerPull = EffectiveDamage(def, points[(int)WheelAttr.Damage]) * Math.Max(1, def.pellets);
            int clip = EffectiveClip(def, points[(int)WheelAttr.Clip]);
            float rate = EffectiveFireRate(def, points[(int)WheelAttr.FireRate]);
            float reload = EffectiveReload(def, points[(int)WheelAttr.Reload]);
            float cycle = clip / rate + reload;
            return clip * dmgPerPull / cycle;
        }

        /// <summary>Damage multiplier at <paramref name="distance"/>. Negative and out-of-range
        /// distances deal nothing; otherwise falloff is linear from falloffStart to range.</summary>
        public static float FalloffFactor(WeaponDef def, float distance)
        {
            if (distance < 0f) return 0f;
            if (distance > def.range) return 0f;
            if (def.falloffStart <= 0f || distance <= def.falloffStart) return 1f;
            float span = def.range - def.falloffStart;
            if (span <= 0.01f) return 0f;
            float t = (distance - def.falloffStart) / span;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return 1f - t;
        }

        /// <summary>Damage at range after falloff, using the damage spoke only.</summary>
        public static float DamageAtRange(WeaponDef def, int damagePoints, float distance) =>
            EffectiveDamage(def, damagePoints) * FalloffFactor(def, distance);

        /// <summary>Per-body damage for a pierce ray: falloff at <paramref name="distance"/>,
        /// then flat <see cref="WeaponDef.pierceDamageMul"/> on every body after the first.
        /// <paramref name="damagedBodyIndex"/> is 0 for the first damaged body.</summary>
        public static float DamageOnPiercedBody(
            WeaponDef def, int damagePoints, float distance, int damagedBodyIndex)
        {
            float dmg = DamageAtRange(def, damagePoints, distance);
            if (damagedBodyIndex > 0)
                dmg *= def.pierceDamageMul;
            return dmg;
        }

        /// <summary>Build a length-5 counter array from N purchases centred on one attribute.</summary>
        public static int[] FocusHistory(WeaponId id, WheelAttr centre, int purchases)
        {
            var ring = RingOf(id);
            if (ring == null) throw new InvalidOperationException(id + " has no ring");
            var pts = new int[AttrCount];
            if (purchases < 0) purchases = 0;
            if (purchases > MaxPurchases) purchases = MaxPurchases;
            for (int i = 0; i < purchases; i++)
                Award(pts, ring, centre);
            return pts;
        }

        /// <summary>Canonical fingerprint of a ring up to rotation and reflection — used by
        /// uniqueness tests.</summary>
        public static string CanonicalRingKey(WheelAttr[] ring)
        {
            if (ring == null || ring.Length != AttrCount) return "";
            string best = null;
            for (int start = 0; start < AttrCount; start++)
            {
                string fwd = EncodeFrom(ring, start, 1);
                string rev = EncodeFrom(ring, start, -1);
                if (best == null || string.CompareOrdinal(fwd, best) < 0) best = fwd;
                if (string.CompareOrdinal(rev, best) < 0) best = rev;
            }
            return best;
        }

        public static string AttrLabel(WheelAttr a)
        {
            switch (a)
            {
                case WheelAttr.Reload: return "reload";
                case WheelAttr.Clip: return "clip";
                case WheelAttr.Damage: return "damage";
                case WheelAttr.FireRate: return "fire rate";
                case WheelAttr.Ability: return "ability";
                default: return a.ToString();
            }
        }

        static string EncodeFrom(WheelAttr[] ring, int start, int step)
        {
            var chars = new char[AttrCount];
            for (int i = 0; i < AttrCount; i++)
            {
                int idx = ((start + i * step) % AttrCount + AttrCount) % AttrCount;
                chars[i] = (char)('0' + (int)ring[idx]);
            }
            return new string(chars);
        }

        static int ClampPts(int points)
        {
            if (points < 0) return 0;
            // Soft cap at a full focus; excess from mixed histories still compounds.
            return points;
        }
    }
}
