using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY command layer for ship-hub purchases: the ONLY path by which station UI
    /// mutates MetaState. Each command applies MetaUpgrades / WeaponWheel / EquipmentCatalog
    /// arithmetic then persists via GameSession. Split: the weapon commands (buy / wheel /
    /// equip) live in ShipCommands.Weapons.cs — one catalog per file, same command surface.</summary>
    /// <remarks>Every refusal is logged with its reason — "I clicked BUY and nothing happened"
    /// has to be answerable from the log alone. Bastion code must never call EquipWeapon
    /// (criterion 4: Primary is fixed for the run).</remarks>
    public static partial class ShipCommands
    {
        const string Tag = "ShipCommands";

        /// <summary>Buy the equipment slot once. Locked until salvage first banks through
        /// the Hatch (UpgradesUnlocked); costs <see cref="EquipmentCatalog.SlotPrice"/>.</summary>
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
                Log.Event("Ship",
                    "tried to buy equipment slot — locked until this realm has earned salvage");
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
            // Hatch armour is the realm's, and it is on the plate. Without this the buyer sees
            // the new level and everyone else aboard keeps reading the old one until the next
            // embark — a number that is wrong for the rest of the session.
            RealmRecordPublisher.Publish("upgrade bought");
            Log.Event("Ship",
                $"equipment slot unlocked — purchased for {price}, banked {m.BankedSalvage} left");
            DemoShipStanding.Reevaluate();
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
            DemoShipStanding.Reevaluate();
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
            DemoShipStanding.Reevaluate();
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

        /// <summary>0-salvage CREW claim. Owns and equips. Sim has no shop.</summary>
        public static bool BuyProtocol(BoarderProtocolId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, "BuyProtocol refused: no session loaded.");
                return false;
            }
            if (!BoarderProtocol.TryClaim(m, id))
                return false;
            GameSession.Save();
            Log.Event("Ship", "claimed " + BoarderProtocol.ProtocolName(id) + " track=crew");
            DemoShipStanding.Reevaluate();
            return true;
        }

        /// <summary>Ship-only passive re-pick. Refusals logged with track=crew.</summary>
        public static bool PickPassive(BoarderAbilityId id)
        {
            var m = GameSession.Meta;
            if (m == null)
            {
                Log.Warn(Tag, "PickPassive refused: no session loaded.");
                return false;
            }
            if (!m.SetEquippedPassive(id))
                return false;
            GameSession.Save();
            Log.Event("Ship", "equipped " + BoarderProtocol.DisplayName(id) + " track=crew");
            return true;
        }

        public static bool BuyUpgrade(UpgradeTrack track)
        {
            // The hatch belongs to the world, not to the visitor. A guest buying it spent their
            // own salvage to raise their own realm's armour while standing on somebody else's
            // ship — the run they were about to play took its hatch HP from the host, so the
            // purchase bought them nothing and put two different armour numbers in one room.
            if (!RealmRecordSource.OwnsThisRealm())
            {
                Log.Event(Tag, "upgrade refused — this realm is not yours to upgrade");
                return false;
            }
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
                case UpgradeTrack.HatchArmor: bought = MetaUpgrades.Buy(m, ref m.Realm.HatchArmorLevel); break;
                default: bought = false; break;
            }

            if (bought)
            {
                GameSession.Save();
                Log.Event("Ship",
                    $"bought {Label(track)} L{LevelOf(track)} for {price} — banked {m.BankedSalvage} left {TrackToken(track)}");
                DemoShipStanding.Reevaluate();
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
        public static string Label(UpgradeTrack track) => UpgradeTracks.Label(track);

        /// <summary>Spend attribution token on every successful bought line.</summary>
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
                default: return 0;
            }
        }
    }
}
