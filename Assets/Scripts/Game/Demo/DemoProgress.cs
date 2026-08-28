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

        /// <summary>The Wave the player must have REACHED before Primary-slot rows go on the
        /// rack. Zone 1's last Wave: getting there is the moment the MK-1 stops being
        /// enough.</summary>
        public static int PrimaryRowsWave { get { return DemoZones.EndWave(1); } }

        /// <summary>Primary-slot rows are for sale once the player has REACHED the end of
        /// zone 1's Wave span — whether they cleared it, died on it, or docked out.
        ///
        /// <para>Re-keyed 2026-08-25 (Zo). This was <c>DemoZone2BankLost</c>, resolved by
        /// zone 2's Convert window, and the Convert is out of the demo — so that flag can
        /// never be written and the rack could never open. Worse, it was already the wrong
        /// shape: the 2026-08-25 playthrough died on Wave 2 with 227 banked and
        /// "no buyable upgrades remain", because the only kit that could carry zone 1 sat
        /// behind clearing zone 1. Gating on the Wave REACHED rather than a Wave survived is
        /// what makes that impossible — the run that proves you need a Primary is the run
        /// that sells you one.</para>
        ///
        /// <para>Reached, never a count of endings: a save that docked through zone 1 arrives
        /// on its first ejection and must still be offered the rack
        /// (Docs/Demo - Single/5-repeat-to-control-room.md). A null meta or a demo-off session
        /// never closes the rack — outside the demo the whole catalog is open.</para></summary>
        public static bool PrimaryRowsOpen(MetaState m)
        {
            if (m == null) return true;
            if (!m.DemoActive) return true;
            // ENTERED, not Reached: DeepestWaveReached rises only on a CLEAR because deep
            // start reads it as a permission, so it cannot tell "died on Wave 2" from "never
            // drew Wave 2" — which is the whole case this gate exists for.
            // The legacy flag still opens it, so a save written before the re-key keeps its rack.
            return m.DemoZone2BankLost || m.DeepestWaveEntered >= PrimaryRowsWave;
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
