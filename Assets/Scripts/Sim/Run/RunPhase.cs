namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// Lifecycle phases of a single run. v0 uses Docked → Tethered → Lost (lost) or
    /// Escaped (all waves cleared). Spooling is reserved for the future escape-window
    /// design and is UNUSED in v0 (kept so serialized values stay enum-stable when it lands).
    /// </summary>
    public enum RunPhase
    {
        /// <summary>Aboard your ship (menu/respawn screen). Not yet boarded the bastion.</summary>
        Docked,

        /// <summary>Fighting in the bastion: kills earn salvage, waves push toward the Hatch.</summary>
        Tethered,

        /// <summary>UNUSED in v0. Reserved for escape windows: tether cut, spool running.</summary>
        Spooling,

        /// <summary>Terminal ("Cleared"): run completed — all WaveDirector.MaxWaves waves cleared.</summary>
        Escaped,

        /// <summary>Terminal ("Lost"): Hatch destroyed or player down. Costs nothing but the run —
        /// salvage was banked per kill and is kept in full.</summary>
        Lost
    }
}
