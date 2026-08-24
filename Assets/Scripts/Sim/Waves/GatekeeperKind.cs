namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The four kinds of gatekeeper. EACH KIND IS DEFINED BY WHAT IT REFUSES TO DO — the refusal is
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

        /// <summary>Hatch-only. Ignores the player entirely. Introduced in wave 3. Its salvage
        /// rule — never damage while there is salvage to take — is recorded and NOT built
        /// (Docs/Brainstorms/gatekeepers-and-waves.md); today it still walks at the hatch.</summary>
        Thief = 2,

        /// <summary>Advances only while unobserved — a Boarder's gaze is the refusal. Watched
        /// (cone + range + line of sight), it targets nothing at all and holds position;
        /// unobserved, it targets exactly as a Default does. The refusal is a STATE that flips
        /// several times a second, so it is not a pushed flag: it lives in the Game layer's
        /// CloakerGatekeeper. Introduced in wave 3.</summary>
        Cloaker = 3,
    }
}
