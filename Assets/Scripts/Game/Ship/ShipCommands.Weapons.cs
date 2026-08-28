using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of ShipCommands): the weapon commands — catalog buy, the
    /// wheel purchase with the demo ladder on it, and slot equip. Split from ShipCommands.cs
    /// when the demo cap landed and the file passed the Game line cap — one catalog per
    /// file, same command surface and the same log contract.</summary>
    public static partial class ShipCommands
    {
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
            // Ladder rung 2 sells the Primaries. Placed after "not available" and "already
            // owned" — those are facts about the weapon, and an owned Primary has nothing to
            // refuse — and BEFORE any price check or SpendSalvage, so a player who is also
            // short hears the lock, which is the reason still true after they bank more
            // (Docs/Demo - Single/5-repeat-to-control-room.md).
            if (def.slot == WeaponSlot.Primary && !DemoProgress.PrimaryRowsOpen(m))
            {
                Log.Event("Demo", "weapon refused — primary locked");
                return false;
            }

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
            // Beat 0's arming beat (2026-08-25 recut — Zo): the ARMORY rack hands over the
            // MK-1 and that alone readies embark — the ship intruder fight is struck.
            bool armingBeat = m.DemoActive && id == WeaponId.Mk1Sidearm && !m.DemoWeaponGranted;
            if (armingBeat)
            {
                m.DemoWeaponGranted = true;
                m.DemoShipFightDone = true;
            }
            GameSession.Save();
            if (armingBeat)
            {
                DemoShipFight.NotifyWeaponGranted();
                // Armed is the multiplayer key now — say so once, here.
                Log.Event("Demo", "multiplayer unlocked");
                DemoVoice.Play(DemoConversationIds.MpUnlocked, DemoVoiceMode.Subtitle);
            }
            Log.Event("Ship",
                $"bought {def.displayName} for {cost} — equipped {def.slot}, banked {m.BankedSalvage} left track=weapon");
            DemoShipStanding.Reevaluate();
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
            if (!WeaponWheel.SpokePurchasable(id, centre))
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — " +
                    $"{WeaponWheel.AttrLabel(centre)} is not purchasable on this weapon");
                return false;
            }
            if (!MetaUpgrades.UpgradesUnlocked(m))
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — locked until this realm has earned salvage");
                return false;
            }
            // Wheel ladder, two rungs on two counters: rung 1's two demo purchases are spent
            // first, then rung 2's own counter, opened by zone 2's bank falling. Named refuse
            // BEFORE any SpendSalvage, and which name depends on how far the ladder got —
            // "demo cap" while rung 2 has not opened, "ladder rung spent" once it has and its
            // buys are gone. Neither count resets per visit (beat 3, Traps;
            // Docs/Demo - Single/5-repeat-to-control-room.md).
            if (m.DemoActive && m.DemoWheelBuysLeft <= 0 && m.DemoRungTwoBuysLeft <= 0)
            {
                Log.Event("Demo", m.DemoRungTwoOpened
                    ? "wheel refused — ladder rung spent"
                    : "wheel refused — demo cap");
                DemoShipStanding.Reevaluate();
                return false;
            }

            int tier = m.TierOf(id);
            if (tier >= WeaponTier.MaxTier)
            {
                Log.Event("Ship",
                    $"tried to wheel-up {WeaponCatalog.Def(id).displayName} — already T{tier}");
                return false;
            }

            long price = WeaponTier.Cost(WeaponCatalog.Def(id).slot, m.WheelCentresOf(id, centre));
            if (m.BankedSalvage < price)
            {
                Log.Event("Ship",
                    $"tried to buy {WeaponCatalog.Def(id).displayName} {WeaponWheel.AttrLabel(centre)} " +
                    $"({price}) — {price - m.BankedSalvage} salvage short");
                DemoShipStanding.Reevaluate();
                return false;
            }
            if (!m.SpendSalvage(price))
            {
                Log.Warn(Tag, $"BuyWheelUpgrade({id}) SpendSalvage failed despite precondition check.");
                return false;
            }

            m.AwardWheel(id, centre);
            if (m.DemoActive)
            {
                // Rung 1 first, rung 2 only once rung 1 is exhausted. The two counters stay
                // separate: rung 1's absolute value is the multiplayer key, so a rung-2 buy
                // must never write it (Docs/Demo - Single/5-repeat-to-control-room.md).
                if (m.DemoWheelBuysLeft > 0)
                {
                    m.DemoWheelBuysLeft--;
                }
                else if (m.DemoRungTwoBuysLeft > 0)
                {
                    m.DemoRungTwoBuysLeft--;
                }
            }
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

            string demoSuffix = m.DemoActive
                ? $" demoBuysLeft={m.DemoWheelBuysLeft} rungTwoBuysLeft={m.DemoRungTwoBuysLeft}"
                : "";
            Log.Event("Ship",
                $"bought {WeaponCatalog.Def(id).displayName} focus {WeaponWheel.AttrLabel(centre)} " +
                $"(+{WeaponWheel.AttrLabel(n0)}, +{WeaponWheel.AttrLabel(n1)}) for {price} — " +
                $"T{m.TierOf(id)}, banked {m.BankedSalvage} left track=weapon{demoSuffix}");
            DemoShipStanding.Reevaluate();
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
    }
}
