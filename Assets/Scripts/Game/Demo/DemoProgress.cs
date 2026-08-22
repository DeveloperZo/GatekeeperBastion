using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>Why an EMBARK is refused while the demo's beat 0 is incomplete. Ordered:
    /// the first unmet gate is the one named (Docs/Demo - Single/0-wake-on-a-dead-ship.md).</summary>
    public enum DemoEmbarkRefusal
    {
        None,
        ShipTerminal,
        NoWeapon,
        Intruders,
    }

    /// <summary>What a Ship station may do right now under the demo gates.</summary>
    public enum DemoStationVerdict
    {
        /// <summary>No demo gate applies — live behaviour.</summary>
        Open,
        /// <summary>Dark NAV: named prompt, the hold-repair target, no panel.</summary>
        Dark,
        /// <summary>No name, no prompt, no interaction — the station does not surface
        /// until it has a live function (2026-08-18 revision: only NAV shows before
        /// its function exists).</summary>
        Hidden,
        /// <summary>CREW before its menu unlocks: restore health on a tap, never a panel.</summary>
        HealOnly,
        /// <summary>Powered NAV before the ship fight is done: prompts, no picker.</summary>
        NoPanelYet,
    }

    /// <summary>The demo's pure gate rules over <see cref="MetaState"/>'s v9 flags — engine-free
    /// on purpose so the offline harness pins every matrix. Unity routing (which interact does
    /// what) lives in DemoStationGate; this class only answers what is allowed.</summary>
    public static class DemoProgress
    {
        /// <summary>Friends unlock on the first wheel purchase (buys-left drops below 2),
        /// or the moment the demo ends. A null meta never blocks a session.</summary>
        public static bool MultiplayerUnlocked(MetaState m)
        {
            if (m == null) return true;
            if (!m.DemoActive) return true;
            return m.DemoWheelBuysLeft < 2;
        }

        /// <summary>Primary-slot rows are for sale only once zone 2's bank has fallen — that
        /// loss is what puts heavier hardware on the rack, and it is the event the player just
        /// played. Never re-key this to a count of endings: a save that docked through zone 1
        /// arrives on its first ejection and must still be offered the rack
        /// (Docs/Demo - Single/5-repeat-to-control-room.md). A null meta or a demo-off session never
        /// closes the rack — outside the demo the whole catalog is open.</summary>
        public static bool PrimaryRowsOpen(MetaState m)
        {
            if (m == null) return true;
            if (!m.DemoActive) return true;
            return m.DemoZone2BankLost;
        }

        /// <summary>First unmet beat 0 gate, in the refuse table's order. None when the
        /// demo is off or beat 0 is complete.</summary>
        public static DemoEmbarkRefusal EmbarkRefusal(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoEmbarkRefusal.None;
            if (!m.DemoArmoryPowered) return DemoEmbarkRefusal.ShipTerminal;
            if (!m.DemoWeaponGranted) return DemoEmbarkRefusal.NoWeapon;
            if (!m.DemoShipFightDone) return DemoEmbarkRefusal.Intruders;
            return DemoEmbarkRefusal.None;
        }

        /// <summary>NAV: dark until the hold-repair, then no panel until the ship is clear.</summary>
        public static DemoStationVerdict NavVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            if (!m.DemoNavPowered) return DemoStationVerdict.Dark;
            if (!m.DemoShipFightDone) return DemoStationVerdict.NoPanelYet;
            return DemoStationVerdict.Open;
        }

        /// <summary>ARMORY: hidden — the MK-1 comes from the ship terminal's start
        /// (2026-08-18 revision, no rack walk) — until the wheel unlocks on the first bank
        /// (UpgradesUnlocked re-key; Docs/Decisions.md).</summary>
        public static DemoStationVerdict ArmoryVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            if (!MetaUpgrades.UpgradesUnlocked(m)) return DemoStationVerdict.Hidden;
            return DemoStationVerdict.Open;
        }

        /// <summary>CREW: hidden until the ship fight is done (its first function is the
        /// post-fight heal); heal-on-tap until its menu unlocks (first Docked after the
        /// first bank — DemoShipDirector writes that flag).</summary>
        public static DemoStationVerdict CrewVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            if (!m.DemoShipFightDone) return DemoStationVerdict.Hidden;
            if (!m.DemoCrewMenuUnlocked) return DemoStationVerdict.HealOnly;
            return DemoStationVerdict.Open;
        }

        /// <summary>FABRICATION stays hidden for the whole demo.</summary>
        public static DemoStationVerdict FabricationVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            return DemoStationVerdict.Hidden;
        }

        /// <summary>Beat 3's exit-standing state key: true when no wheel upgrade can be
        /// bought right now — buys-left 0, or banked below the cheapest next wheel cost
        /// across owned weapons. An un-owned weapon row never counts, even at cost 0:
        /// SEC-D must not hold the exit standing open (beat 3, Traps). Evaluated on Docked
        /// and after every buy / refuse / spend; DemoShipStanding owns the flip.</summary>
        public static bool NoBuyableUpgradeRemains(MetaState m)
        {
            if (m == null) return true;
            if (!MetaUpgrades.UpgradesUnlocked(m)) return true;
            // Both ladder rungs, summed: rung 2 hands out buys on its own counter, and reading
            // rung 1 alone would flip the exit standing to EMBARK while those are still
            // affordable (Docs/Demo - Single/5-repeat-to-control-room.md).
            if (m.DemoActive && m.DemoWheelBuysLeft + m.DemoRungTwoBuysLeft <= 0) return true;

            long cheapest = long.MaxValue;
            for (int i = 0; i < WeaponCatalog.ArraySize; i++)
            {
                var id = (WeaponId)i;
                if (!m.Owns(id) || !WeaponCatalog.IsAvailable(id)) continue;
                if (WeaponWheel.RingOf(id) == null) continue;
                int tier = m.TierOf(id);
                if (tier >= WeaponTier.MaxTier) continue;
                long cost = WeaponTier.CheapestSpokeCost(m, id);
                if (cost < 0) continue;
                if (cost < cheapest) cheapest = cost;
            }
            if (cheapest == long.MaxValue) return true;
            return m.BankedSalvage < cheapest;
        }

        /// <summary>Escaped only. Flips the demo off and powers FAB; every other demo gate
        /// already no-ops when <see cref="MetaState.DemoActive"/> is false, so those two
        /// writes are the whole lift. Leaves <see cref="MetaState.DemoZone2BankLost"/>,
        /// <see cref="MetaState.DemoRungTwoOpened"/> and <see cref="MetaState.DemoRungTwoBuysLeft"/>
        /// alone — they are the record of what the player played, and every reader of them
        /// is already DemoActive-gated. A Lost or Docked end must not call this.</summary>
        public static void EndDemo(MetaState m)
        {
            if (m == null) return;
            if (!m.DemoActive) return;
            m.DemoActive = false;
            m.DemoFabPowered = true;
            Log.Event("Demo", "demo ended");
        }
    }
}
