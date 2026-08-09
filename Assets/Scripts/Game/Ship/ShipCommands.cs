using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;

namespace GatekeeperBastion.Game
{
    /// <summary>Meta-upgrade tracks purchasable at ship stations. Weapons go through BuyWeapon /
    /// BuyWheelUpgrade / EquipWeapon. The repair rig is equipment (BuyEquipmentSlot / BuyEquipment).</summary>
    public enum UpgradeTrack
    {
        /// <summary>Hatch max HP (Fabrication).</summary>
        HatchArmor,

        // ---- PLAYER ATTRIBUTES (Crew Quarters) ----
        // Same Buy/NextCost curve, deliberately NOT gated behind UpgradesUnlocked.

        /// <summary>Player max HP (Crew Quarters).</summary>
        Vitality,
        /// <summary>Sprint stamina pool (Crew Quarters).</summary>
        Stamina,
        /// <summary>Base salvage per kill (Crew Quarters).</summary>
        Scavenging,
        /// <summary>Critical-hit chance (Crew Quarters).</summary>
        Luck
    }

    /// <summary>HOST-ONLY command layer for ship-hub purchases: the ONLY path by which station UI
    /// mutates MetaState. Each command applies MetaUpgrades / WeaponWheel / EquipmentCatalog
    /// arithmetic then persists via GameSession.</summary>
    /// <remarks>Every refusal is logged with its reason — "I clicked BUY and nothing happened"
    /// has to be answerable from the log alone. Bastion code must never call EquipWeapon
    /// (criterion 4: Primary is fixed for the run).</remarks>
    public static class ShipCommands
    {
        const string Tag = "ShipCommands";

        /// <summary>Buy a catalog weapon for its def.cost. Cost 0 (MK-1) is a free purchase —
        /// must not be treated as already-owned or not-for-sale. Auto-equips into the def's slot.</summary>
        public static bool BuyWeapon(WeaponId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"BuyWeapon({id}) refused: no session loaded.");
                return false;
            }
            if (id == WeaponId.None)
            {
                Log.Warn(Tag, "BuyWeapon(None) refused.");
                return false;
            }
            if (!WeaponCatalog.IsAvailable(id))
            {
                Log.Event("Ship", $"tried to buy {WeaponCatalog.Def(id).displayName} — not available");
                return false;
            }
            if (m.Owns(id))
            {
                Log.Event("Ship", $"tried to buy {WeaponCatalog.Def(id).displayName} — already owned");
                return false;
            }

            var def = WeaponCatalog.Def(id);
            long cost = def.cost;
            // Cost 0 is affordable at BankedSalvage 0 — do NOT require SpendSalvage (it rejects <=0).
            if (cost > 0)
            {
                if (m.BankedSalvage < cost)
                {
                    Log.Event("Ship",
                        $"tried to buy {def.displayName} ({cost}) — {cost - m.BankedSalvage} salvage short");
                    return false;
                }
                if (!m.SpendSalvage(cost))
                {
                    Log.Warn(Tag, $"BuyWeapon({id}) SpendSalvage failed despite precondition check.");
                    return false;
                }
            }

            m.SetOwned(id, true);
            m.SetEquipped(id);
            GameSession.Save();
            Log.Event("Ship",
                $"bought {def.displayName} for {cost} — equipped {def.slot}, banked {m.BankedSalvage} left track=weapon");
            return true;
        }

        /// <summary>Buy one wheel purchase centred on <paramref name="centre"/> for an owned
        /// weapon. Gated on UpgradesUnlocked. No unfocused purchase exists.</summary>
        public static bool BuyWheelUpgrade(WeaponId id, WheelAttr centre)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"BuyWheelUpgrade({id},{centre}) refused: no session loaded.");
                return false;
            }
            if (id == WeaponId.None || !m.Owns(id))
            {
                Log.Event("Ship", $"tried to wheel-up {id} — not owned");
                return false;
            }
            if (!WeaponCatalog.IsAvailable(id))
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — not available");
                return false;
            }
            var ring = WeaponWheel.RingOf(id);
            if (ring == null)
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — no ring authored");
                return false;
            }
            bool onRing = false;
            for (int i = 0; i < ring.Length; i++)
                if (ring[i] == centre) { onRing = true; break; }
            if (!onRing)
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — {centre} not on ring");
                return false;
            }
            if (!MetaUpgrades.UpgradesUnlocked(m))
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — locked until first death");
                return false;
            }

            int tier = m.TierOf(id);
            if (tier >= WeaponTier.MaxTier)
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — already T{tier}");
                return false;
            }

            long price = WeaponTier.Cost(tier);
            if (m.BankedSalvage < price)
            {
                Log.Event("Ship",
                    $"tried to buy {WeaponCatalog.Def(id).displayName} {WeaponWheel.AttrLabel(centre)} " +
                    $"({price}) — {price - m.BankedSalvage} salvage short");
                return false;
            }
            if (!m.SpendSalvage(price))
            {
                Log.Warn(Tag, $"BuyWheelUpgrade({id}) SpendSalvage failed despite precondition check.");
                return false;
            }

            m.AwardWheel(id, centre);
            GameSession.Save();

            // Neighbours that came along — ring distance 1 from centre.
            int centreIndex = 0;
            for (int i = 0; i < ring.Length; i++)
                if (ring[i] == centre) { centreIndex = i; break; }
            WheelAttr n0 = default, n1 = default;
            int got = 0;
            for (int i = 0; i < ring.Length; i++)
            {
                if (WeaponWheel.RingDistance(centreIndex, i, ring.Length) != 1) continue;
                if (got == 0) n0 = ring[i];
                else n1 = ring[i];
                got++;
            }

            Log.Event("Ship",
                $"bought {WeaponCatalog.Def(id).displayName} focus {WeaponWheel.AttrLabel(centre)} " +
                $"(+{WeaponWheel.AttrLabel(n0)}, +{WeaponWheel.AttrLabel(n1)}) for {price} — " +
                $"T{m.TierOf(id)}, banked {m.BankedSalvage} left track=weapon");
            return true;
        }

        /// <summary>Equip an owned weapon into its own def.slot. Impossible into the wrong slot.
        /// Ship / ARMORY only — never call from Bastion.</summary>
        public static bool EquipWeapon(WeaponId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"EquipWeapon({id}) refused: no session loaded.");
                return false;
            }
            if (id == WeaponId.None || !m.Owns(id))
            {
                Log.Event("Ship", $"tried to equip {id} — not owned");
                return false;
            }
            if (!WeaponCatalog.IsAvailable(id))
            {
                Log.Event("Ship", $"tried to equip {WeaponCatalog.Def(id).displayName} — not available");
                return false;
            }

            m.SetEquipped(id);
            GameSession.Save();
            var def = WeaponCatalog.Def(id);
            Log.Event("Ship", $"equipped {def.displayName} in {def.slot}");
            return true;
        }

        /// <summary>Buy the equipment slot once. Locked until first lost Run; costs
        /// <see cref="EquipmentCatalog.SlotPrice"/>.</summary>
        public static bool BuyEquipmentSlot()
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, "BuyEquipmentSlot refused: no session loaded.");
                return false;
            }
            if (m.EquipmentSlotPurchased)
            {
                Log.Event("Ship", "tried to buy equipment slot — already owned");
                return false;
            }
            if (!MetaUpgrades.UpgradesUnlocked(m))
            {
                Log.Event("Ship", "tried to buy equipment slot — locked until first lost Run");
                return false;
            }

            long price = EquipmentCatalog.SlotPrice;
            if (m.BankedSalvage < price)
            {
                Log.Event("Ship",
                    $"tried to buy equipment slot ({price}) — {price - m.BankedSalvage} salvage short");
                return false;
            }
            if (!m.SpendSalvage(price))
            {
                Log.Warn(Tag, "BuyEquipmentSlot SpendSalvage failed despite precondition check.");
                return false;
            }

            m.EquipmentSlotPurchased = true;
            GameSession.Save();
            Log.Event("Ship",
                $"equipment slot unlocked — purchased for {price}, banked {m.BankedSalvage} left");
            return true;
        }

        /// <summary>Buy a catalog equipment piece. Requires the slot; buying equips it.</summary>
        public static bool BuyEquipment(EquipmentId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"BuyEquipment({id}) refused: no session loaded.");
                return false;
            }
            if (id == EquipmentId.None)
            {
                Log.Warn(Tag, "BuyEquipment(None) refused.");
                return false;
            }
            if (!m.EquipmentSlotPurchased)
            {
                Log.Event("Ship",
                    $"tried to buy {EquipmentCatalog.Def(id).displayName} — equipment slot not purchased");
                return false;
            }
            if (m.OwnsEquipment(id))
            {
                Log.Event("Ship", $"tried to buy {EquipmentCatalog.Def(id).displayName} — already owned");
                return false;
            }

            var def = EquipmentCatalog.Def(id);
            long cost = def.cost;
            if (cost > 0)
            {
                if (m.BankedSalvage < cost)
                {
                    Log.Event("Ship",
                        $"tried to buy {def.displayName} ({cost}) — {cost - m.BankedSalvage} salvage short");
                    return false;
                }
                if (!m.SpendSalvage(cost))
                {
                    Log.Warn(Tag, $"BuyEquipment({id}) SpendSalvage failed despite precondition check.");
                    return false;
                }
            }

            m.SetEquipmentOwned(id, true);
            m.SetEquippedEquipment(id);
            GameSession.Save();
            Log.Event("Ship",
                $"bought {def.displayName} for {cost} — equipped, banked {m.BankedSalvage} left track=equipment");
            return true;
        }

        /// <summary>Buy a drone-bay payload. Buying equips it (one payload per Run once embarked).</summary>
        public static bool BuyPayload(PayloadId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"BuyPayload({id}) refused: no session loaded.");
                return false;
            }
            if (id == PayloadId.None)
            {
                Log.Warn(Tag, "BuyPayload(None) refused.");
                return false;
            }
            if (m.OwnsPayload(id))
            {
                Log.Event("Ship", $"tried to buy {PayloadCatalog.Def(id).displayName} — already owned");
                return false;
            }

            var def = PayloadCatalog.Def(id);
            long cost = def.cost;
            if (cost > 0)
            {
                if (m.BankedSalvage < cost)
                {
                    Log.Event("Ship",
                        $"tried to buy {def.displayName} ({cost}) — {cost - m.BankedSalvage} salvage short");
                    return false;
                }
                if (!m.SpendSalvage(cost))
                {
                    Log.Warn(Tag, $"BuyPayload({id}) SpendSalvage failed despite precondition check.");
                    return false;
                }
            }

            m.SetPayloadOwned(id, true);
            if (!m.SetEquippedPayload(id))
            {
                // Owned but equip refused (run locked) — still a successful purchase.
                Log.Event("Ship",
                    $"bought {def.displayName} for {cost} — owned, equip deferred, banked {m.BankedSalvage} left track=equipment");
                GameSession.Save();
                return true;
            }
            GameSession.Save();
            Log.Event("Ship",
                $"bought {def.displayName} for {cost} — equipped in bay, banked {m.BankedSalvage} left track=equipment");
            return true;
        }

        /// <summary>Equip an owned payload into the bay. No-op mid-Run.</summary>
        public static bool EquipPayload(PayloadId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"EquipPayload({id}) refused: no session loaded.");
                return false;
            }
            if (id != PayloadId.None && !m.OwnsPayload(id))
            {
                Log.Event("Ship",
                    $"tried to equip {PayloadCatalog.Def(id).displayName} — not owned");
                return false;
            }
            if (!m.SetEquippedPayload(id))
                return false;
            GameSession.Save();
            Log.Event("Ship", id == PayloadId.None
                ? "bay cleared"
                : $"equipped {PayloadCatalog.Def(id).displayName} in bay");
            return true;
        }

        /// <summary>Buy the next level of an attribute/fab upgrade track.</summary>
        public static bool BuyUpgrade(UpgradeTrack track)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, $"BuyUpgrade({track}) refused: no session loaded.");
                return false;
            }

            int levelBefore = LevelOf(track);
            long price = levelBefore < MetaUpgrades.MaxLevel ? MetaUpgrades.NextCost(levelBefore) : 0L;

            bool bought;
            switch (track)
            {
                case UpgradeTrack.HatchArmor: bought = MetaUpgrades.Buy(m, ref m.HatchArmorLevel); break;
                case UpgradeTrack.Vitality: bought = MetaUpgrades.Buy(m, ref m.VitalityLevel); break;
                case UpgradeTrack.Stamina: bought = MetaUpgrades.Buy(m, ref m.StaminaLevel); break;
                case UpgradeTrack.Scavenging: bought = MetaUpgrades.Buy(m, ref m.ScavengingLevel); break;
                case UpgradeTrack.Luck: bought = MetaUpgrades.Buy(m, ref m.LuckLevel); break;
                default: bought = false; break;
            }

            if (bought)
            {
                GameSession.Save();
                Log.Event("Ship",
                    $"bought {Label(track)} L{LevelOf(track)} for {price} — banked {m.BankedSalvage} left {TrackToken(track)}");
            }
            else if (levelBefore >= MetaUpgrades.MaxLevel)
            {
                Log.Event("Ship", $"tried to buy {Label(track)} — already maxed at L{levelBefore}");
            }
            else
            {
                Log.Event("Ship",
                    $"tried to buy {Label(track)} L{levelBefore + 1} ({price}) — {price - m.BankedSalvage} salvage short");
            }
            return bought;
        }

        /// <summary>Console display name of a track.</summary>
        public static string Label(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.HatchArmor: return "Hatch Armor";
                case UpgradeTrack.Vitality: return "Vitality";
                case UpgradeTrack.Stamina: return "Stamina";
                case UpgradeTrack.Scavenging: return "Scavenging";
                case UpgradeTrack.Luck: return "Luck";
                default: return track.ToString();
            }
        }

        /// <summary>M29 criterion 13 — spend attribution token on every successful bought line.</summary>
        static string TrackToken(UpgradeTrack track)
        {
            return track == UpgradeTrack.HatchArmor ? "track=fabrication" : "track=crew";
        }

        /// <summary>Current level of a track, read from GameSession.Meta (0 when no session).</summary>
        public static int LevelOf(UpgradeTrack track)
        {
            var m = GameSession.Meta;
            if (m == null) return 0;
            switch (track)
            {
                case UpgradeTrack.HatchArmor: return m.HatchArmorLevel;
                case UpgradeTrack.Vitality: return m.VitalityLevel;
                case UpgradeTrack.Stamina: return m.StaminaLevel;
                case UpgradeTrack.Scavenging: return m.ScavengingLevel;
                case UpgradeTrack.Luck: return m.LuckLevel;
                default: return 0;
            }
        }
    }
}
