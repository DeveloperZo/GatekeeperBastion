namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Lifecycle phases of a single run. v0 uses Docked → Tethered → Lost (lost),
    /// Escaped (Master Control Hatch-bank survived) or Returned (walked home at the Hatch).
    /// Spooling is reserved for the future escape-window design and is UNUSED in v0 (kept so
    /// serialized values stay enum-stable when it lands). Append only.
    /// </summary>
    public enum RunPhase
    {
        /// <summary>Aboard your ship (menu/respawn screen). Not yet boarded the bastion.</summary>
        Docked,

        /// <summary>Fighting in the bastion: kills earn salvage, waves push toward the Hatch.</summary>
        Tethered,

        /// <summary>UNUSED in v0. Reserved for escape windows: tether cut, spool running.</summary>
        Spooling,

        /// <summary>Terminal ("Cleared"): Master Control Hatch-bank half survived.</summary>
        Escaped,

        /// <summary>Terminal ("Lost"): Hatch destroyed or player down. Costs nothing but the run —
        /// salvage was banked per kill and is kept in full.</summary>
        Lost,

        /// <summary>Terminal ("Docked"): the player held Interact at the Hatch after surviving a
        /// bank window and went home by choice. Not a loss — PodEjections does not move. Named
        /// Returned because Docked (0) already means aboard-the-ship-before-boarding; the
        /// player-facing word for this phase is still Docked.</summary>
        Returned
    }
}
