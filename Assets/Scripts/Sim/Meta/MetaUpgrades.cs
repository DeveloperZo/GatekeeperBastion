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
        /// the realm has EARNED anything at all.
        ///
        /// <para>Re-keyed 2026-08-25 (Zo): this was <c>HatchConversions >= 1</c>, and the
        /// Convert window that raised that counter is out of the demo. Kills bank as they
        /// land now, so the honest question is no longer "did salvage come through the
        /// Hatch" — every kill does — but "has this realm earned anything to spend". It is a
        /// LIFETIME figure on purpose: spending down to zero must not re-lock the wheel.
        /// It was <c>PodEjections >= 1</c> before that, which never fired for a player who
        /// banked and docked (Docs/Decisions.md). Buying a weapon itself is NOT gated.</para>
        /// </summary>
        public static bool UpgradesUnlocked(MetaState m)
        {
            if (m == null) throw new ArgumentNullException("m");
            return m.LifetimeSalvageEarned > 0L;
        }

        // ---- FABRICATION ----

        /// <summary>Hatch max HP. RunSim's constructor reads this.</summary>
        public static float HatchHp(int lvl)
        {
            // ONE COPY OF THE POOL. This read a hardcoded 300 — a second copy of
            // ShipSystems.HatchMaxHp, and the one the live hatch was actually built from. Raising
            // the constant on 2026-08-28 moved nothing in play until this line was found, because
            // the number the tests read and the number the game used were different variables
            // that happened to agree. lint-constants cannot catch this: it compares numeric ARRAY
            // literals, and a lone scalar restated in another file is invisible to it.
            return ShipSystems.HatchMaxHp * (1f + 0.05f * lvl);
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

        /// <summary>Base salvage for one downed gatekeeper, BEFORE the siphon multiplier.
        /// <para>25 -> 42 WITH THE ~40% ROSTER CUT (2026-08-27). A kill is priced FLAT — it does
        /// not read the body's HP — so cutting the roster 18+12i -> 11+7i cut run income by the
        /// same 40% as a side effect nobody asked for. 5/3 is the same factor every kind's maxHp
        /// rose by, so a wave pays what it used to: wave 5 was 66 x 25 = 1650 and is now
        /// 39 x 42 = 1638.</para>
        /// <para>THE FLATNESS IS THE TRAP. Every price in the game — respawns at 150, Primaries
        /// at 1500-2500, tracks at 200 x 2^level — is denominated in kills, so any change to the
        /// number of bodies in a wave silently re-prices all of them. Move this WITH the roster
        /// or the shop moves without anyone deciding it should.</para></summary>
        public static float SalvagePerKill()
        {
            return 42f;
        }
    }
}
