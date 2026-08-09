namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// Sub-state of RunPhase.Tethered under the draw-triggered wave model.
    /// PREP: no waves, NO siphon income (idle prep earns nothing); repair, reposition,
    /// set the dial, then pull the trigger. ASSAULT: a Siphon Draw is running — the wave
    /// is live and siphon income flows until the game layer reports the wave cleared.
    /// </summary>
    public enum AssaultState
    {
        /// <summary>Between draws: quiet, dial adjustable, zero income.</summary>
        Prep,

        /// <summary>A draw is live: wave spawned/spawning, income flowing, dial locked.</summary>
        Assault
    }
}
