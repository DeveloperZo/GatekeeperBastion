namespace GatekeeperBastion.Sim
{
    /// <summary>Catalog identity for a drone-bay payload. Values are written into save files —
    /// never renumbered, never reused. <see cref="None"/> is the empty-bay sentinel.</summary>
    public enum PayloadId
    {
        None = 0,
        Decoy = 1
    }
}
