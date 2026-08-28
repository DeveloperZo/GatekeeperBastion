using System.Collections.Generic;

namespace GatekeepersProtocol.Game
{
    /// <summary>How a demo line presents. Lock eats move/look/fire/interact and waits for
    /// Continue; Subtitle leaves all fight input live and auto-advances. The sim ticks under
    /// both — a mode that stopped time would be refused by Decisions.md.</summary>
    public enum DemoVoiceMode
    {
        Lock,
        Subtitle,
    }

    /// <summary>Who a line plays for. Local plays on this seat only; Party is started by the
    /// host and broadcast so every connected seat plays it locally.</summary>
    public enum DemoVoiceScope
    {
        Local,
        Party,
    }

    /// <summary>One row of the demo line table: id, speaker label, default mode/scope, copy.</summary>
    public readonly struct DemoLine
    {
        public readonly string Id;
        public readonly string Speaker;
        public readonly DemoVoiceMode Mode;
        public readonly DemoVoiceScope Scope;
        public readonly string Text;

        public DemoLine(string id, string speaker, DemoVoiceMode mode, DemoVoiceScope scope,
            string text)
        {
            Id = id;
            Speaker = speaker;
            Mode = mode;
            Scope = scope;
            Text = text;
        }
    }

    /// <summary>The demo mission line table: id → speaker, mode, scope, copy. Static code, not
    /// an asset — one author, no per-instance override to drift, and the offline harness can
    /// audit it against <see cref="DemoConversationIds"/>. Copy is stamped in the beat docs;
    /// this is the runtime copy source. Every speaker is Game until a guide is named.
    /// Draft copy ships as draft — a mute row would hide the trigger wiring behind it.</summary>
    public static class DemoLines
    {
        const string Game = "Game";

        static readonly DemoLine[] Rows =
        {
            // Beat 0 — wake on a dead Ship.
            new DemoLine(DemoConversationIds.WakeStir, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "You wake in a sealed capsule. The ship is dark. Nothing answers."),
            // Directions restamped 2026-08-26: the hull is ONE deck — deck 2, the stair and the
            // void are struck, and every station dropped to y=0 keeping its X/Z. No line may
            // name a stair; there is none to find. Bearings below are walked from the pose the
            // line is spoken at, in ship terms: bow is +z, port is -x, starboard is +x.
            // Wake speaks at the capsule (-7.5, 41.0) facing 180 deg — i.e. facing AFT, with
            // the NAV MAP (-7.5, 50.0) 9 m directly BEHIND the player at the bow end, port
            // side. "Turn around" is the whole instruction; anything else sends them sternward.
            new DemoLine(DemoConversationIds.Wake, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Capsule open. Ship power is dead. Find the NAV MAP — turn around, it is at the " +
                "bow end of the deck, port side. Hold Interact to repair it."),
            // The lost-player net: fires from anywhere, so the bearing has to be absolute
            // rather than relative to a pose. One deck means the port wall runs unbroken from
            // stern to the NAV MAP, so "follow it forward" always arrives.
            new DemoLine(DemoConversationIds.ShipLost, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Local,
                "The NAV MAP is right forward, port side. Find the port wall and follow it to the bow."),
            // Restamped 2026-08-18 (draft — Zo): the terminal grants the MK-1; the other
            // stations are hidden until they have a function, so no line names them early.
            // Spoken at the NAV MAP (-7.5, 50.0), facing it — so the player is facing the bow
            // and the ship terminal (-4.0, 21.0) is 29 m behind them, amidships and on the same
            // port side. Aft is the bearing that matters; the 3.5 m to starboard is not worth
            // a word.
            new DemoLine(DemoConversationIds.NavRepair, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "NAV MAP is live. Now the ship terminal — back down the deck toward the stern, " +
                "amidships, same port side. Select it, then start it. That is how every terminal works."),
            // The terminal's start points at the rack (2026-08-25 recut — the arming beat
            // moved to the ARMORY, the intruder fight is struck). Spoken at the terminal
            // (-4.0, 21.0); the ARMORY (10.0, 12.5) is 14 m to starboard and 8.5 m further
            // aft, so it is a crossing of the deck, not a walk along it.
            new DemoLine(DemoConversationIds.ArmoryCall, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Power routed. The ARMORY rack is live — cross to starboard and a little further " +
                "aft. Take the MK-1."),
            // Retired at the armory recut (2026-08-25): the intruder fight is struck from
            // the demo — no caller. Row kept so the id registry stays whole.
            new DemoLine(DemoConversationIds.Intruders, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Gatekeepers aboard. The terminal armed you — MK-1 Sidearm, loaded. Put them down."),
            // Retired at the terminal-grant cut (2026-08-18): no caller — the intruders
            // line carries the armed copy. Row kept so the id registry stays whole.
            new DemoLine(DemoConversationIds.WeaponGrant, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Local,
                "Loaded. Put them down."),
            new DemoLine(DemoConversationIds.ShipClear, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Clear. You took hits — CREW QUARTERS patches you up. " +
                "Then the NAV MAP: EMBARK when you are ready."),
            // One id, three reason strings: the caller passes the reason copy as an override.
            new DemoLine(DemoConversationIds.EmbarkBlocked, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "The Ship is not ready to EMBARK yet."),
            new DemoLine(DemoConversationIds.EmbarkFirst, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Quiet Hatch. Start the Wave here, then push up the lane to the terminal."),

            // Beat 1 — first terminals.
            new DemoLine(DemoConversationIds.Explore, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Local,
                "The Bastion is dormant. Wake it at the Hatch — the terminal up the lane " +
                "wears a T, and that is where the Wave ends."),
            new DemoLine(DemoConversationIds.TerminalLook, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "This terminal is the far end of the run, not the start. Begin at the Hatch, " +
                "fight up, then T here to shut the Wave down and bank what you are carrying."),
            new DemoLine(DemoConversationIds.JuiceTeach, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Kills paid you Unbanked Salvage. + and - spend it on the Ante: the next Wave " +
                "brings more Gatekeepers and pays more. Spending nothing is also a bet."),
            new DemoLine(DemoConversationIds.AfterTerminal, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Zone clear and the pile is banked. Back to the Hatch — that is where the " +
                "next one starts, and where you can ante up before it does."),

            // Beat 2 — the Hatch. The Convert window left the demo (Zo, 2026-08-25): kills
            // bank as they land, so there is no pile waiting at the Hatch to teach. The
            // HatchBankTeach row stays because the id registry must stay whole, and because
            // the mechanic still exists for anyone who Withdraws deliberately — nothing in
            // the demo plays it.
            new DemoLine(DemoConversationIds.HatchBankTeach, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Withdraw at the Hatch and that salvage is on the table again — hold the "
                + "Hatch while it converts and it comes back worth more. If the Hatch falls, "
                + "it is gone."),
            // Still played, but only by Master Control's Hatch phase now.
            new DemoLine(DemoConversationIds.HatchFight, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Local,
                "The Wave wants the Hatch now. Hold it."),
            new DemoLine(DemoConversationIds.FirstHatchDown, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "The Boarding ended. Everything you killed for is already banked aboard. " +
                "Spend it at the ARMORY."),

            // Beat 3 — Ship after first loss.
            // Re-keyed 2026-08-25 (Zo): fires on the ARMORY's MK-1, not the first wheel buy.
            new DemoLine(DemoConversationIds.MpUnlocked, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "MK-1 in your hands — you are a Boarder now. " +
                "And the Ship answers hails: friends can join you."),
            new DemoLine(DemoConversationIds.WheelCap, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Two purchases is the cap for now. The NAV MAP is ready when you are."),

            // Beat 4 — embark after the first loss. Draft copy (stamped drafts in the beat doc).
            new DemoLine(DemoConversationIds.CrewReady, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "CREW QUARTERS has more than a bunk now. Your protocol waits there."),
            new DemoLine(DemoConversationIds.CrewClaim, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "This is your protocol. Claim the Default — Barrage on G, Rush on H. " +
                "It costs nothing."),
            new DemoLine(DemoConversationIds.EmbarkWave2, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "The NAV MAP takes you back. The Bastion remembers what you cleared — " +
                "the terminal will offer it."),
            new DemoLine(DemoConversationIds.ZoneRetrigger, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "This terminal remembers the zone. Run it all again, or start deeper and finish " +
                "what's left. The Hatch re-arms when the zone is clear."),
            new DemoLine(DemoConversationIds.DockOption, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Party,
                "The Hatch can take you home — hold to dock. Or push on."),

            // Beat 5 — zone 2. Draft copy.
            new DemoLine(DemoConversationIds.RepeatUnit, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "Same job, deeper rooms: the zone's waves, then the Hatch. " +
                "The door opens when the bank survives."),
            new DemoLine(DemoConversationIds.DoorD1, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "That door is open. The next terminals are behind it."),
            new DemoLine(DemoConversationIds.DoorD2, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "That door is open. The next terminals are behind it."),
            // Restamped 2026-08-20 (draft — Zo): the id is kept, the trigger is now zone 2's
            // bank falling, and that may be this save's FIRST ejection. The shipped line opened
            // by asserting a second one, which the recut forbids. Cause-agnostic from here: a
            // loss that put heavier hardware on the rack, with no count of losses in it.
            new DemoLine(DemoConversationIds.SecondLoss, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Zone 2 is settled — held or lost, the Bastion has your measure now. What " +
                "transferred is aboard — and the rack answers with heavier hardware. " +
                "The PRIMARY slot is open at the ARMORY."),

            // Beats 6-7 — Control Room and finale.
            new DemoLine(DemoConversationIds.ControlRoom, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "This room seals when you pull Master Control. The fight is in here. " +
                "It is not the way home."),
            new DemoLine(DemoConversationIds.MasterControl, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "Master Control. Five Waves in a sealed room, then the door opens. " +
                "E, then T, when you are ready."),
            new DemoLine(DemoConversationIds.HatchFinale, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Party,
                "The door is open. Get to the Hatch — everything Unbanked rides on holding it."),
            new DemoLine(DemoConversationIds.Escaped, Game, DemoVoiceMode.Lock, DemoVoiceScope.Party,
                "Escaped. The Bastion is stripped and the Salvage is home. " +
                "The Ship is yours now — every console, every Wave."),
        };

        static readonly Dictionary<string, DemoLine> ById = BuildIndex();

        static bool _suppressedForTest;

        static Dictionary<string, DemoLine> BuildIndex()
        {
            var index = new Dictionary<string, DemoLine>(Rows.Length);
            for (int i = 0; i < Rows.Length; i++)
                index[Rows[i].Id] = Rows[i];
            return index;
        }

        /// <summary>Row count the runner checks for the table-missing guard. 0 while a test
        /// suppresses the table.</summary>
        public static int RowCount => _suppressedForTest ? 0 : Rows.Length;

        /// <summary>Every row, for coverage tests. Not a copy — do not mutate.</summary>
        public static IReadOnlyList<DemoLine> All => Rows;

        public static bool TryGet(string id, out DemoLine line)
        {
            if (_suppressedForTest || id == null)
            {
                line = default;
                return false;
            }
            return ById.TryGetValue(id, out line);
        }

        /// <summary>Test hook: make the table read as missing so the runner's
        /// table-missing guard is provable. Always undo in TearDown.</summary>
        public static void SuppressTableForTest(bool suppressed) => _suppressedForTest = suppressed;
    }
}
