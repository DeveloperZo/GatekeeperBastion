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
        /// <summary>Visible and named, selling full-version breadth — never a panel or any
        /// interaction. Locked content the player should SEE, not wonder about.</summary>
        LockedPreview,
    }

    /// <summary>The demo's pure gate rules over <see cref="MetaState"/>'s v9 flags — engine-free
    /// on purpose so the offline harness pins every matrix. Unity routing (which interact does
    /// what) lives in DemoStationGate; this class only answers what is allowed.</summary>
    public static class DemoProgress
    {
        /// <summary>Friends unlock the moment the player is ARMED — the ARMORY's MK-1 is
        /// beat 0's last gate, and a Boarder with a weapon is one who can be joined
        /// (Zo, 2026-08-25; was the first wheel purchase, which stranded co-op behind a
        /// bank). Or the moment the demo ends. A null meta never blocks a session.</summary>
        public static bool MultiplayerUnlocked(MetaState m)
        {
            if (m == null) return true;
            return MultiplayerUnlocked(m.DemoActive, m.DemoWeaponGranted);
        }

        /// <summary>The same rule against a realm that has not been entered, so a chooser can
        /// refuse BEFORE handing out a join code rather than after.</summary>
        public static bool MultiplayerUnlocked(bool demoActive, bool weaponGranted)
        {
            if (!demoActive) return true;
            return weaponGranted;
        }

        /// <summary>Salvage the realm must have EARNED, lifetime, before Primary-slot rows go
        /// on the rack: the cheapest available Primary's price, read off the catalog so the
        /// price is the gate and a price move never leaves this behind (Zo, 2026-09-02).</summary>
        public static long PrimaryRackSalvage
        {
            get
            {
                long cheapest = long.MaxValue;
                foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
                {
                    if (!WeaponCatalog.IsAvailable(id)) continue;
                    long cost = WeaponCatalog.Def(id).cost;
                    if (cost < cheapest) cheapest = cost;
                }
                return cheapest == long.MaxValue ? 0L : cheapest;
            }
        }

        /// <summary>Primary-slot rows are for sale once the realm has EARNED the cheapest
        /// Primary's price, lifetime (<see cref="PrimaryRackSalvage"/>).
        ///
        /// <para>Re-keyed 2026-09-02 (Zo: "rack opened when player earns enough"). It was the
        /// deepest Wave ENTERED reaching zone 1's end (2026-08-25), and before that a zone-2
        /// Convert window the demo no longer has. Lifetime earned, like the wheel's own lock:
        /// spending the bank to zero cannot re-lock the rack, and a pile that never came
        /// through the ledger opens nothing. The price is the gate, so the rack opens on the
        /// Boarding that earned it and never refuses a Primary the bank could buy.</para>
        ///
        /// <para>Never a count of endings, never a depth. A null meta or a demo-off session
        /// never closes the rack — outside the demo the whole catalog is open.</para></summary>
        public static bool PrimaryRowsOpen(MetaState m)
        {
            if (m == null) return true;
            if (!m.DemoActive) return true;
            return m.LifetimeSalvageEarned >= PrimaryRackSalvage;
        }

        /// <summary>Mark beat 0 — the ship tutorial — consumed, without playing it.
        ///
        /// <para>Beat 0 is an ARC: it teaches where the stations are, how an interact works,
        /// and what firing feels like. An arc is consumed once by a PLAYER, but these flags
        /// live on the realm, so every new world replays a lesson its owner already had. This
        /// is the one place that says "consider it taught", so the ship's gates, the dev
        /// checkpoints and any skip offered to the player cannot drift apart.</para>
        ///
        /// <para>It grants the MK-1 because the tutorial's terminal hands it over: skipping the
        /// lesson must not skip the equipment, or the player embarks with the improvised cutter
        /// and no way to know why. What it does NOT touch is the rest of the demo — the zones,
        /// the wheel rungs, the rack that opens on zone 2's loss — because those are the game,
        /// not the lesson.</para></summary>
        /// <returns>False when there was nothing to skip.</returns>
        public static bool SkipShipTutorial(MetaState m)
        {
            if (m == null || !m.DemoActive)
                return false;
            if (EmbarkRefusal(m) == DemoEmbarkRefusal.None)
                return false;

            m.DemoCapsuleOpened = true;
            m.DemoNavPowered = true;
            m.DemoArmoryPowered = true;
            m.DemoWeaponGranted = true;
            m.DemoShipFightDone = true;
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.SetEquipped(WeaponId.Mk1Sidearm);
            return true;
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

        /// <summary>ARMORY: the arming beat (2026-08-25 recut — the terminal routes power
        /// and points here; the rack's free MK-1 buy readies embark, no ship fight).
        /// Hidden only until the terminal powers it; open from then on.</summary>
        public static DemoStationVerdict ArmoryVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            if (!m.DemoArmoryPowered && !MetaUpgrades.UpgradesUnlocked(m))
                return DemoStationVerdict.Hidden;
            return DemoStationVerdict.Open;
        }

        /// <summary>The one thing the gated CREW still opens for: a Boarder with no Protocol
        /// claimed (Zo, 2026-08-26).
        ///
        /// <para>CREW's menu is gated until the first Docked return, and the 2026-08-27
        /// playthrough is what that cost: the player embarked <c>[Protocol] embark none</c>,
        /// ran the whole Boarding without one, and only saw <c>CREW menu unlocked</c> fire on
        /// the way home. A first-run Boarder is EXACTLY the player who has no Protocol, so the
        /// gate was shut precisely when the claim was wanted.</para>
        ///
        /// <para>This opens the door for the claim and nothing else. Once a Protocol is held
        /// the station returns to heal-on-tap until the menu unlocks on its own schedule, so
        /// the demo's pacing is unchanged for every other row — and mid-Boarding it stays shut,
        /// because <see cref="ProtocolOffer.IsPickPending"/> refuses a locked loadout.</para></summary>
        public static bool CrewOpensForProtocolClaim(MetaState m)
        {
            if (m == null || !m.DemoActive) return false;
            if (!m.DemoShipFightDone) return false;   // hidden outranks this; no early door
            if (m.DemoCrewMenuUnlocked) return false; // already open on its own terms
            return ProtocolOffer.IsPickPending(m);
        }

        /// <summary>CREW: hidden until the ship fight is done (its first function is the
        /// post-fight heal); heal-on-tap until its menu unlocks (first Docked after the
        /// first bank — DemoShipDirector writes that flag), EXCEPT for an unclaimed Protocol —
        /// see <see cref="CrewOpensForProtocolClaim"/>.</summary>
        public static DemoStationVerdict CrewVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            if (!m.DemoShipFightDone) return DemoStationVerdict.Hidden;
            if (!m.DemoCrewMenuUnlocked) return DemoStationVerdict.HealOnly;
            return DemoStationVerdict.Open;
        }

        /// <summary>FABRICATION is visible-but-locked for the whole demo — the prompt sells
        /// the full version instead of the console reading as dark or dead.</summary>
        public static DemoStationVerdict FabricationVerdict(MetaState m)
        {
            if (m == null || !m.DemoActive) return DemoStationVerdict.Open;
            return DemoStationVerdict.LockedPreview;
        }

        /// <summary>Beat 3's exit-standing state key: true when no wheel upgrade can be
        /// bought right now — banked below the cheapest next wheel cost
        /// across owned weapons. An un-owned weapon row never counts, even at cost 0:
        /// SEC-D must not hold the exit standing open (beat 3, Traps). Evaluated on Docked
        /// and after every buy / refuse / spend; DemoShipStanding owns the flip.</summary>
        public static bool NoBuyableUpgradeRemains(MetaState m)
        {
            if (m == null) return true;
            if (!MetaUpgrades.UpgradesUnlocked(m)) return true;
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
        /// writes are the whole lift. Leaves <see cref="MetaState.DemoZone2BankLost"/> and
        /// <see cref="MetaState.DemoRungTwoOpened"/>
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
