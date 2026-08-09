using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// The ship-shop: permanent upgrade tracks bought with BANKED salvage between runs.
    /// Pure static arithmetic — the LEVELS live on MetaState (so they persist with the save);
    /// this class only prices, validates, and applies purchases, and maps levels to effects.
    /// </summary>
    /// <remarks>
    /// Three invariants, each of which has a failure behind it. Docs/Design/4-Data.md has the
    /// effect table and the reasoning; do not restate either here.
    ///   1. ONE SHAPE FOR EVERY (attribute/fab) TRACK: base * (1 + 0.05 * level). No per-track
    ///      slopes — a uniform ramp is what keeps the shop legible. CritChance is additive at
    ///      the same slope only because a chance of zero has nothing to scale.
    ///   2. Cost is exactly 200 * 2^currentLevel. Never "roughly doubles".
    ///   3. ATTRIBUTES ARE NOT GATED behind the first death. They are the answer to dying, so
    ///      they must be buyable before you have died. Weapon Tiers AND the equipment slot ARE
    ///      gated (UpgradesUnlocked).
    /// Weapon damage / fire rate / clip no longer live here — see WeaponCatalog + WeaponWheel.
    /// Repair rate no longer lives here — see EquipmentCatalog (flat 8 HP/s on the rig).
    /// </remarks>
    public static class MetaUpgrades
    {
        /// <summary>Highest level of every attribute/fab track.</summary>
        public const int MaxLevel = 5;

        /// <summary>Cost of level 0 → 1; each further level doubles.</summary>
        public const long BaseCost = 200;

        /// <summary>Salvage cost to buy the next level from currentLevel: 200 * 2^currentLevel.
        /// Only defined below MaxLevel — there is nothing to buy at 5.</summary>
        public static long NextCost(int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= MaxLevel)
                throw new ArgumentOutOfRangeException("currentLevel");
            return BaseCost << currentLevel; // exact 200 * 2^currentLevel
        }

        /// <summary>True when the track is below MaxLevel and the bank covers NextCost.</summary>
        public static bool CanBuy(MetaState meta, int currentLevel)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            if (currentLevel < 0 || currentLevel >= MaxLevel) return false;
            return meta.BankedSalvage >= NextCost(currentLevel);
        }

        /// <summary>
        /// Buy the next level of a track: pass the MetaState and a ref to its level field
        /// (e.g. Buy(meta, ref meta.VitalityLevel)). Deducts BankedSalvage and increments
        /// the level. Returns false (no mutation) when maxed or unaffordable.
        /// </summary>
        public static bool Buy(MetaState meta, ref int levelField)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            if (!CanBuy(meta, levelField)) return false;
            // Goes through SpendSalvage so LifetimeSalvageSpent can never drift from the bank.
            if (!meta.SpendSalvage(NextCost(levelField))) return false;
            levelField++;
            return true;
        }

        /// <summary>
        /// True once weapon Tier purchases and the equipment slot are unlocked — gated until
        /// the first death. Buying a weapon itself is NOT gated, and neither are attributes.
        /// </summary>
        public static bool UpgradesUnlocked(MetaState m)
        {
            if (m == null) throw new ArgumentNullException("m");
            return m.PodEjections >= 1;
        }

        // ---- FABRICATION ----

        /// <summary>Hatch max HP. RunSim's constructor reads this.</summary>
        public static float HatchHp(int lvl)
        {
            return 300f * (1f + 0.05f * lvl);
        }

        // ---- PLAYER ATTRIBUTES ----

        /// <summary>VITALITY — player max HP.</summary>
        public static float PlayerMaxHp(int level)
        {
            return 100f * (1f + 0.05f * ClampLevel(level));
        }

        /// <summary>STAMINA — max stamina pool.</summary>
        public static float StaminaMax(int level)
        {
            return 100f * (1f + 0.05f * ClampLevel(level));
        }

        /// <summary>LUCK — critical-hit chance as a FRACTION. A crit deals DOUBLE damage.
        /// THE RULE: Luck is the only source of crit chance or crit damage in the game.</summary>
        public static float CritChance(int level)
        {
            return 0.05f * ClampLevel(level);
        }

        /// <summary>SCAVENGING — base salvage for one downed gatekeeper, BEFORE the siphon
        /// multiplier.</summary>
        public static float SalvagePerKill(int level)
        {
            return 25f * (1f + 0.05f * ClampLevel(level));
        }

        /// <summary>Clamp a track level into 0..MaxLevel.</summary>
        private static int ClampLevel(int level)
        {
            if (level < 0) return 0;
            if (level > MaxLevel) return MaxLevel;
            return level;
        }
    }
}
