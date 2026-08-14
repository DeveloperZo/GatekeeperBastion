namespace GatekeeperBastion.Sim
{
    /// <summary>Master Control win sequence. #12 — 5 terminal then 5 Hatch-bank.
    /// Live phase and remaining counts sit on <see cref="RunSim"/>.</summary>
    public static class MasterControl
    {
        public const int TerminalWaves = 5;
        public const int HatchWaves = 5;
    }

    /// <summary>Which half of the Master Control sequence is live.</summary>
    public enum MasterControlPhase : byte
    {
        None = 0,
        Terminal = 1,
        Hatch = 2,
    }
}
