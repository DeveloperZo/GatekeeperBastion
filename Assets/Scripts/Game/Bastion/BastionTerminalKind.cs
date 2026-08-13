namespace GatekeeperBastion.Game
{
    /// <summary>What Interact does at this console. Field terminals also author
    /// <see cref="BastionTerminal.UnlocksDoor"/> and <see cref="BastionTerminal.UnlockedSpawns"/>.</summary>
    public enum BastionTerminalKind : byte
    {
        /// <summary>Boarder-hunt Wave; N clears mark this id done (opens matching D).</summary>
        Field = 0,
        /// <summary>Hatch-bank convert Wave.</summary>
        HatchBank = 1,
        /// <summary>Resources / boosts — parked.</summary>
        Resource = 2,
        /// <summary>Win sequence.</summary>
        Master = 3,
    }
}
