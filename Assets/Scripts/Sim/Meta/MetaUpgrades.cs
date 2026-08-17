using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The ship-shop: Hatch Armor bought with BANKED salvage between runs, plus the
    /// Boarder's frozen HP / stamina / kill-value constants (crew tracks deleted in save v8).
    /// Pure static arithmetic — Hatch Armor LEVEL lives on MetaState; this class prices,
    /// validates, and applies that purchase, and maps the remaining constants.
    /// </summary>
    /// <remarks>
    /// Cost is exactly 200 * 2^currentLevel. Never "roughly doubles".
    /// Weapon Tiers AND the equipment slot are gated (UpgradesUnlocked).
    /// Weapon damage / fire rate / clip no longer live here — see WeaponCatalog + WeaponWheel.
    /// Repair rate no longer lives here — see EquipmentCatalog (flat 8 HP/s on the rig).
    /// Luck is the only source of crit chance or crit damage in the game, even at chance 0.
    /// </remarks>
    public static class MetaUpgrades
    {
        /// <summary>Highest level of Hatch Armor.</summary>
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
        /// (e.g. Buy(meta, ref meta.HatchArmorLevel)). Deducts BankedSalvage and increments
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
        /// the first death. Buying a weapon itself is NOT gated.
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

        // ---- BOARDER CONSTANTS (crew tracks deleted) ----

        /// <summary>Player max HP. Frozen at the old level-0 value.</summary>
        public static float PlayerMaxHp()
        {
            return 100f;
        }

        /// <summary>Sprint stamina pool. Frozen at the old level-0 value.</summary>
        public static float StaminaMax()
        {
            return 100f;
        }

        /// <summary>LUCK — critical-hit chance as a FRACTION. Frozen at 0.
        /// THE RULE: Luck is the only source of crit chance or crit damage in the game.
        /// The 2× roll in PlayerGun.Firing stays.</summary>
        public static float CritChance()
        {
            return 0f;
        }

        /// <summary>Base salvage for one downed gatekeeper, BEFORE the siphon multiplier.</summary>
        public static float SalvagePerKill()
        {
            return 25f;
        }
    }
}
