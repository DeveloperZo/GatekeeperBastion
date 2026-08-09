namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// The three kinds of gatekeeper. EACH KIND IS DEFINED BY WHAT IT REFUSES TO DO — the refusal is
    /// the contract; the stat line is only tuning. Stats live in
    /// <see cref="WaveDirector.StatsFor"/>, per-wave counts in <see cref="WaveSpec"/>, and the
    /// design reasoning in Docs/Design/1-Experience.md.
    /// </summary>
    public enum GatekeeperKind
    {
        /// <summary>Hatch-seeker, provokable. The original gatekeeper; the only kind in wave 1.</summary>
        Default = 0,

        /// <summary>Player-seeker. Ignores the hatch entirely. Introduced in wave 2.</summary>
        Hunter = 1,

        /// <summary>Hatch-only battering ram. Ignores the player entirely. Introduced in wave 3.</summary>
        Ram = 2,
    }
}
