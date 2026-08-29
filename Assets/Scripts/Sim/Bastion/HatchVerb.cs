namespace GatekeepersProtocol.Sim
{
    /// <summary>Which verb the Hatch is offering right now. The Hatch carries five of them and
    /// they are not equal: this enum plus <see cref="HatchVerbs.Choose"/> is the ORDER, stated
    /// once, in the only place a test can reach it.</summary>
    public enum HatchVerb : byte
    {
        /// <summary>Nothing to offer — no run, or nothing left to do here.</summary>
        None = 0,

        /// <summary>The Hatch is gone; there is no verb, only the fact.</summary>
        Destroyed = 1,

        /// <summary>It is damaged and repair is the offer.</summary>
        Repair = 2,

        /// <summary>A dock hold is already in progress. Answers the player about the thing in
        /// their hands and outranks everything, because interrupting a hold to tell them about
        /// a different verb is how a hold gets abandoned by accident.</summary>
        Docking = 3,

        /// <summary>THE LOOP'S START LINE. A wave begins at the Hatch.</summary>
        StartWave = 4,

        /// <summary>A Convert window may be staked.</summary>
        Convert = 5,

        /// <summary>Banked salvage may be withdrawn into the carry.</summary>
        Withdraw = 6,

        /// <summary>The way home, when there is nothing to start.</summary>
        Dock = 7,
    }

    /// <summary>The precedence rule for the Hatch's prompt — one pure function, so the order
    /// is a thing that can be asserted rather than a thing that emerges from the order of
    /// <c>if</c> statements in a MonoBehaviour the offline harness cannot compile.
    ///
    /// <para>WHY THIS EXISTS. On 2026-08-28 a Dock offer was added ABOVE the start branch in
    /// RepairInteractable.Prompt. The moment zone 1 closed, the Hatch stopped saying
    /// "[T] start the wave" and said "[hold E] Dock" instead — the start line was intact and
    /// simply never mentioned, which Zo reported as "I could not trigger a start at hatch".
    /// The rule it broke was already written down (Docs/Design/0-Gameplay.md: the Hatch's start
    /// prompt outranks Convert and Withdraw) and nothing could check it, because the ordering
    /// lived in a method no test could call.</para>
    ///
    /// <para>The Game layer still owns every string, the key names and the scene lookups. What
    /// moved here is only the DECISION, which is the part that is a rule.</para></summary>
    public static class HatchVerbs
    {
        /// <summary>The verb the Hatch leads with.</summary>
        /// <param name="runIsOver">No prompt at all once the run has ended.</param>
        /// <param name="destroyed">Hatch HP is gone.</param>
        /// <param name="hpFull">Hatch is at full HP, so repair is not on offer.</param>
        /// <param name="dockOffered">A zone has closed and the way home is open.</param>
        /// <param name="dockHoldInProgress">The player is mid-hold on the dock right now.</param>
        /// <param name="offersWaveStart">The current zone's trigger is available here.</param>
        /// <param name="prep">The run is between waves.</param>
        /// <param name="unbanked">Salvage carried, i.e. staked out of the bank.</param>
        /// <param name="bank">The realm's banked salvage.</param>
        public static HatchVerb Choose(
            bool runIsOver, bool destroyed, bool hpFull,
            bool dockOffered, bool dockHoldInProgress,
            bool offersWaveStart, bool prep, long unbanked, long bank)
        {
            if (runIsOver) return HatchVerb.None;

            // A hold in progress is the one thing that outranks the start line, and only
            // because it is already happening.
            if (dockHoldInProgress && dockOffered) return HatchVerb.Docking;

            if (destroyed) return HatchVerb.Destroyed;
            if (!hpFull) return HatchVerb.Repair;

            // THE START LINE OUTRANKS EVERYTHING ELSE THE HATCH CAN DO. Convert and Withdraw
            // were always below it; Dock joined them on 2026-08-28 after briefly sitting above.
            if (offersWaveStart) return HatchVerb.StartWave;

            if (prep && unbanked > 0L) return HatchVerb.Convert;
            if (prep && unbanked == 0L && bank > 0L) return HatchVerb.Withdraw;

            // Only once there is nothing to press on with does going home become the headline.
            if (dockOffered) return HatchVerb.Dock;
            return HatchVerb.None;
        }

        /// <summary>Should the way home be shown ALONGSIDE the leading verb rather than
        /// instead of it? Both are live at once between zones — T presses on, hold E goes home
        /// — and naming only one of them is what hid the dock before it was fixed and then hid
        /// the start line after.</summary>
        public static bool ShowsDockAlongside(HatchVerb leading, bool dockOffered)
        {
            return dockOffered && leading == HatchVerb.StartWave;
        }
    }
}
