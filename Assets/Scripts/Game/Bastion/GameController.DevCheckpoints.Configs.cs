using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): checkpoint loadout configurations —
    /// F12, a beat letter, then a digit picks a configuration (Enter keeps 0, the beat
    /// exactly as authored). A configuration overlays protocol + weapons + tiers onto the
    /// constructed beat state through the same Earn/Spend pairing the beats use, so the
    /// state still resembles a played save and the ledger balances. Overlays are ADDITIVE:
    /// a row grants and upgrades, never un-owns or downgrades — subtractive semantics wait
    /// on the beat-context pass. PROVISIONAL ROWS: which protocol and upgrades a beat is
    /// allowed to carry is a pending owner ruling; these rows exist so the mechanism and
    /// the playthrough-bounds extraction have configurations to key on.</summary>
    public sealed partial class GameController
    {
        public readonly struct CheckpointConfig
        {
            public readonly string Name;
            public readonly BoarderProtocolId Protocol;
            public readonly WeaponId Primary;
            public readonly int PrimaryTier;
            public readonly WeaponId Secondary;
            public readonly int SecondaryTier;

            public CheckpointConfig(string name, BoarderProtocolId protocol,
                WeaponId primary, int primaryTier, WeaponId secondary, int secondaryTier)
            {
                Name = name;
                Protocol = protocol;
                Primary = primary;
                PrimaryTier = primaryTier;
                Secondary = secondary;
                SecondaryTier = secondaryTier;
            }
        }

        /// <summary>Row N is the digit N in the prompt; 0 is "as authored" and has no row.
        /// Whether a row is clean play data is BEAT-RELATIVE — see
        /// <see cref="CheckpointConfigTainted"/> — so the same row can be tainted at an
        /// early beat and clean at a later one.</summary>
        public static readonly CheckpointConfig[] CheckpointConfigs =
        {
            new CheckpointConfig("rung 2 — MK-1 T2, DEFAULT",
                BoarderProtocolId.Default, WeaponId.None, 0, WeaponId.Mk1Sidearm, 2),
            new CheckpointConfig("primary — PRI-C T2, DEFAULT",
                BoarderProtocolId.Default, WeaponId.PriC, 2, WeaponId.Mk1Sidearm, 2),
            new CheckpointConfig("primary — PRI-C T2, VEIL",
                BoarderProtocolId.Veil, WeaponId.PriC, 2, WeaponId.Mk1Sidearm, 2),
            new CheckpointConfig("primary — PRI-C T2, FORGE",
                BoarderProtocolId.Forge, WeaponId.PriC, 2, WeaponId.Mk1Sidearm, 2),
        };

        /// <summary>Beat-relative taint (owner ruling 2026-08-24): configurations grow
        /// valid as the beats progress. A row is TAINTED at a beat when it grants a
        /// Primary before the beat where the Primary rung unlocked, or when its whole kit
        /// costs more than the progression had earned by that beat. Both facts are read
        /// off the CONSTRUCTED beat state (<c>DemoRungTwoOpened</c>,
        /// <c>LifetimeSalvageEarned</c>) — the rule follows the beat builders, never a
        /// hardcoded beat table. 0 (beat as authored) and out-of-range are never
        /// tainted. Judged BEFORE the overlay is applied.</summary>
        public static bool CheckpointConfigTainted(MetaState beatState, int config)
        {
            if (beatState == null || config <= 0 || config > CheckpointConfigs.Length)
                return false;
            return ConfigTaintedFor(beatState, CheckpointConfigs[config - 1]);
        }

        /// <summary>Row form of the taint rule, public so the EditMode pins can judge
        /// rows that are not in the table (a max-upgraded kit, for one).</summary>
        public static bool ConfigTaintedFor(MetaState beatState, CheckpointConfig c)
        {
            if (GrantsPrimary(c) && !beatState.DemoRungTwoOpened) return true;
            return ConfigKitCost(c) > beatState.LifetimeSalvageEarned;
        }

        static bool GrantsPrimary(CheckpointConfig c)
        {
            return WeaponIsPrimary(c.Primary) || WeaponIsPrimary(c.Secondary);
        }

        static bool WeaponIsPrimary(WeaponId id)
        {
            if (id == WeaponId.None) return false;
            return WeaponCatalog.Def(id).slot == WeaponSlot.Primary;
        }

        /// <summary>The row's kit priced from scratch — weapon cost plus the slot-aware
        /// wheel spend to its tier. Today's shop price on purpose: the beats mint at the
        /// historical curve, which is never cheaper, so the affordability test cannot call
        /// a beat's own kit unaffordable.</summary>
        public static long ConfigKitCost(CheckpointConfig c)
        {
            return KitCost(c.Primary, c.PrimaryTier) + KitCost(c.Secondary, c.SecondaryTier);
        }

        static long KitCost(WeaponId id, int tier)
        {
            if (id == WeaponId.None) return 0;
            var def = WeaponCatalog.Def(id);
            return def.cost + WeaponTier.SpentToReach(def.slot, tier);
        }

        /// <summary>Overlay configuration <paramref name="config"/> (1-based) onto a beat
        /// state. 0 and out-of-range are the authored beat, untouched.</summary>
        public static void ApplyCheckpointConfig(MetaState m, int config)
        {
            if (m == null || config <= 0 || config > CheckpointConfigs.Length) return;
            CheckpointConfig c = CheckpointConfigs[config - 1];
            GrantWeapon(m, c.Primary, c.PrimaryTier);
            GrantWeapon(m, c.Secondary, c.SecondaryTier);
            if (c.Protocol != BoarderProtocolId.None
                && !BoarderProtocol.TryClaim(m, c.Protocol))
                Log.Warn(Tag, "checkpoint config — protocol "
                    + c.Protocol + " refused the claim");
            Log.Event("Dev", "checkpoint config " + config + " — " + c.Name);
        }

        /// <summary>Own, tier and equip through the ledger: the delta between what the beat
        /// already paid for and what the row asks for is earned and spent in pairs, so
        /// LifetimeSalvageEarned == BankedSalvage + LifetimeSalvageSpent keeps holding.</summary>
        static void GrantWeapon(MetaState m, WeaponId id, int tier)
        {
            if (id == WeaponId.None) return;
            long cost = 0;
            if (!m.Owns(id)) cost += WeaponCatalog.Def(id).cost;
            int current = m.TierOf(id);
            if (tier > current)
                cost += WeaponTier.SpentToReach(tier) - WeaponTier.SpentToReach(current);
            if (cost > 0)
            {
                m.EarnSalvage(cost);
                m.SpendSalvage(cost);
            }
            m.SetOwned(id, true);
            if (tier > current)
                m.ReplacePointsFromFlatTier(id, tier);
            m.SetEquipped(id);
        }
    }
}
