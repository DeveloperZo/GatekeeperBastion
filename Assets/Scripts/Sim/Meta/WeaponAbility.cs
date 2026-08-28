namespace GatekeepersProtocol.Sim
{
    /// <summary>Activated weapon ability identity. Values reach save-adjacent logs — never
    /// renumber, never reuse a retired id. <see cref="None"/> is the empty descriptor.</summary>
    public enum AbilityId
    {
        None = 0,
        Br12Concussive = 1,
        Mr35Pin = 2,
        Mk1RegenSurge = 3,
        PriCRailshot = 4,
    }

    /// <summary>Pure arithmetic for weapon abilities. Mechanism lives in the Game layer;
    /// only magnitudes, thresholds and cooldowns live here. Accumulated wheel points drive
    /// <paramref name="dialPoints"/>; the curve must have no plateau across 0..MaxDialPoints.</summary>
    public static class WeaponAbility
    {
        /// <summary>Full ability focus at MaxPurchases. Matches <see cref="WeaponWheel.FocusAtMax"/>.</summary>
        public const int MaxDialPoints = WeaponWheel.FocusAtMax;

        /// <summary>Hits on one Gatekeeper before the MR-35 pin fires. Counts hits, never damage.</summary>
        public const int PinHitThreshold = 3;

        /// <summary>Seconds without a hit on the build target before pin stacks clear.</summary>
        public const float PinBuildDecaySeconds = 1.25f;

        /// <summary>Baseline Secondary clip regen (rounds/second).</summary>
        public const float SecondaryRegenPerSecond = 0.4f;

        /// <summary>Fixed regen multiplier while an MK-1 surge is live. Dial stretches duration,
        /// not rate (Zo, 2026-08-07).</summary>
        public const float Mk1SurgeMultiplier = 2.5f;

        /// <summary>Half-angle of the BR-12 concussive cone, degrees.</summary>
        public const float Br12ConeHalfAngleDegrees = 35f;

        /// <summary>Rough gatekeeper-seconds credited per surge-granted Secondary round (MK-1 log hook).
        /// Criterion 5 for the MK-1 is still measured in play — this is only the wave-line estimate.</summary>
        public const float Mk1GatekeeperSecondsPerSurgeRound = 0.5f;

        /// <summary>Armed Railshot lapses after this many seconds if the shot is not fired.</summary>
        public const float PriCRailshotArmedSeconds = 4f;

        /// <summary>Provisional gatekeeper-seconds per body damaged by a Railshot (the real bar is measured in play).</summary>
        public const float PriCRailshotSecondsPerBody = 1.0f;

        /// <summary>Effect magnitude at <paramref name="dialPoints"/> (0..MaxDialPoints).
        /// BR-12: shove metres. MR-35: pin seconds. MK-1: surge duration seconds.
        /// PRI-C: Railshot pierce body count at full damage.</summary>
        public static float Magnitude(AbilityId id, int dialPoints)
        {
            int p = ClampPoints(dialPoints);
            float t = p / (float)MaxDialPoints;
            switch (id)
            {
                case AbilityId.Br12Concussive:
                    // 3.5 m → 10.5 m. Walk-back at 2.8 m/s: ~1.3 s → ~3.8 s per body.
                    return 3.5f + 7f * t;
                case AbilityId.Mr35Pin:
                    // 1.5 s → 6.0 s held.
                    return 1.5f + 4.5f * t;
                case AbilityId.Mk1RegenSurge:
                    // Dial = duration: 4 s → 10 s of faster regen at fixed multiplier.
                    return 4f + 6f * t;
                case AbilityId.PriCRailshot:
                    // Bodies pierced at full damage: 3 → 8 across MaxDialPoints.
                    return 3f + 5f * t;
                default:
                    return 0f;
            }
        }

        /// <summary>Catalog cooldown seconds (wall clock). Independent of dial.</summary>
        public static float Cooldown(AbilityId id)
        {
            switch (id)
            {
                case AbilityId.Br12Concussive: return 10f;
                case AbilityId.Mr35Pin: return 6f;
                case AbilityId.Mk1RegenSurge: return 10f;
                case AbilityId.PriCRailshot: return 12f;
                default: return 0f;
            }
        }

        public static string DisplayName(AbilityId id)
        {
            switch (id)
            {
                case AbilityId.Br12Concussive: return "Concussive Breach";
                case AbilityId.Mr35Pin: return "Pin Shot";
                case AbilityId.Mk1RegenSurge: return "Regen Surge";
                case AbilityId.PriCRailshot: return "Railshot";
                default: return "";
            }
        }

        public static bool IsEmpty(AbilityId id) => id == AbilityId.None;

        static int ClampPoints(int dialPoints)
        {
            if (dialPoints < 0) return 0;
            if (dialPoints > MaxDialPoints) return MaxDialPoints;
            return dialPoints;
        }
    }

    /// <summary>MR-35 pin build tracker — hits only, never damage. Pure so Path 1 can pin the
    /// ring-integrity rule without a scene. Charge is weapon-scoped: target changes do not
    /// reset Hits (only <see cref="Tick"/> decay does). Same-target reset made the build
    /// unreachable once MR-35 TTK fell below <see cref="WeaponAbility.PinHitThreshold"/>.</summary>
    public struct PinBuildState
    {
        public int TargetKey;
        public int Hits;
        public float DecayRemaining;

        /// <summary>Record one hit on <paramref name="targetKey"/>. Damage is intentionally
        /// unused — callers must not pass it into the threshold math. Hits carry across
        /// targets; <see cref="TargetKey"/> is whoever was hit last (the pin recipient).</summary>
        public void RegisterHit(int targetKey, float decaySeconds)
        {
            TargetKey = targetKey;
            Hits++;
            DecayRemaining = decaySeconds;
        }

        public void Tick(float dt)
        {
            if (Hits <= 0) return;
            DecayRemaining -= dt;
            if (DecayRemaining <= 0f) Clear();
        }

        public bool Ready => Hits >= WeaponAbility.PinHitThreshold;

        public void Clear()
        {
            TargetKey = 0;
            Hits = 0;
            DecayRemaining = 0f;
        }
    }
}
