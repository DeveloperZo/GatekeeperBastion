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
            new DemoLine(DemoConversationIds.Wake, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Capsule open. Ship power is dead. Find the NAV MAP — hold Interact to repair it."),
            // Restamped 2026-08-18 (draft — Zo): the terminal grants the MK-1; the other
            // stations are hidden until they have a function, so no line names them early.
            new DemoLine(DemoConversationIds.NavRepair, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "NAV MAP is live. Now the ship terminal — select it, then start it. " +
                "That is how every terminal works."),
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
                "Quiet Hatch. Find a field terminal. Select it, then start — wake the place."),

            // Beat 1 — first terminals.
            new DemoLine(DemoConversationIds.Explore, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Local,
                "The Bastion is dormant. Field terminals wear a T. Find one."),
            new DemoLine(DemoConversationIds.TerminalLook, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "A field terminal. E selects it. T starts a Wave. " +
                "The Wave hunts you — the Hatch is not its target."),
            new DemoLine(DemoConversationIds.JuiceTeach, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Kills paid you Unbanked Salvage. + and - spend it on the Ante: the next Wave " +
                "brings more Gatekeepers and pays more. Spending nothing is also a bet."),
            new DemoLine(DemoConversationIds.AfterTerminal, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "The terminals are done. Your Salvage is still Unbanked. Go to the Hatch and " +
                "hold it — the pile transfers home while you defend. E, then T."),

            // Beat 2 — first Hatch-bank.
            new DemoLine(DemoConversationIds.HatchBankTeach, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "Hold the Hatch and the pile transfers home while you defend. " +
                "If the Hatch falls, whatever has not moved is gone. T when ready."),
            new DemoLine(DemoConversationIds.HatchFight, Game, DemoVoiceMode.Subtitle, DemoVoiceScope.Local,
                "The Wave wants the Hatch now. Hold it — the pile is moving."),
            new DemoLine(DemoConversationIds.FirstHatchDown, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "The Boarding ended. What dripped home is banked aboard. The rest is gone. " +
                "Spend it at the ARMORY."),

            // Beat 3 — Ship after first loss.
            new DemoLine(DemoConversationIds.MpUnlocked, Game, DemoVoiceMode.Lock, DemoVoiceScope.Local,
                "The upgrade stays on the weapon in your hands. " +
                "And the Ship answers hails now — friends can join you."),
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
                "That one took the Boarding off you. What transferred is aboard — and the rack " +
                "answers with heavier hardware. The PRIMARY slot is open at the ARMORY."),

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
