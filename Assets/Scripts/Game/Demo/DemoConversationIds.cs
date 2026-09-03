namespace GatekeepersProtocol.Game
{
    /// <summary>The demo mission line ids — one const per id the beats fire, mirrored by
    /// <see cref="DemoLines"/> rows. Engine-free on purpose: the offline harness compiles
    /// this folder, so id/table drift fails in seconds, not in a review.
    /// `demo.door_d3` is deliberately absent — D3 open fires the Control Room line instead,
    /// and a dead id in this class would read as a line someone should wire.</summary>
    public static class DemoConversationIds
    {
        public const string WakeStir = "demo.wake_stir";
        public const string Wake = "demo.wake";
        /// <summary>Lost-player nudge: 45 quiet seconds before the first NAV repair.</summary>
        public const string ShipLost = "demo.ship_lost";
        public const string NavRepair = "demo.nav_repair";
        /// <summary>Terminal started: points at the ARMORY rack (the arming beat).</summary>
        public const string ArmoryCall = "demo.armory_call";
        public const string Intruders = "demo.intruders";
        public const string WeaponGrant = "demo.weapon_grant";
        public const string ShipClear = "demo.ship_clear";
        public const string EmbarkBlocked = "demo.embark_blocked";
        public const string EmbarkFirst = "demo.embark_first";
        public const string Explore = "demo.explore";
        public const string TerminalLook = "demo.terminal_look";
        public const string JuiceTeach = "demo.juice_teach";
        public const string AfterTerminal = "demo.after_terminal";
        public const string HatchBankTeach = "demo.hatch_bank_teach";
        public const string HatchFight = "demo.hatch_fight";
        public const string FirstHatchDown = "demo.first_hatch_down";
        public const string MpUnlocked = "demo.mp_unlocked";
        public const string CrewReady = "demo.crew_ready";
        public const string CrewClaim = "demo.crew_claim";
        public const string EmbarkWave2 = "demo.embark_wave2";
        public const string ZoneRetrigger = "demo.zone_retrigger";
        public const string DockOption = "demo.dock_option";
        public const string RepeatUnit = "demo.repeat_unit";
        public const string DoorD1 = "demo.door_d1";
        public const string DoorD2 = "demo.door_d2";
        public const string SecondLoss = "demo.second_loss";
        public const string ControlRoom = "demo.control_room";
        public const string MasterControl = "demo.master_control";
        public const string HatchFinale = "demo.hatch_finale";
        public const string Escaped = "demo.escaped";

        /// <summary>Every id above, for uniqueness/coverage tests and table audits.</summary>
        public static readonly string[] All =
        {
            WakeStir, Wake, ShipLost, NavRepair, ArmoryCall, Intruders, WeaponGrant, ShipClear,
            EmbarkBlocked, EmbarkFirst,
            Explore, TerminalLook, JuiceTeach, AfterTerminal,
            HatchBankTeach, HatchFight, FirstHatchDown,
            MpUnlocked,
            CrewReady, CrewClaim, EmbarkWave2, ZoneRetrigger, DockOption,
            RepeatUnit, DoorD1, DoorD2, SecondLoss,
            ControlRoom, MasterControl,
            HatchFinale, Escaped,
        };
    }
}
