namespace GatekeeperBastion.Sim
{
    /// <summary>Who a Wave is for. Terminal = Boarder hunt (field terminal). HatchBank = Hatch
    /// extract (M53); unused until then.</summary>
    public enum WaveMode : byte
    {
        Terminal = 0,
        HatchBank = 1,
    }
}
