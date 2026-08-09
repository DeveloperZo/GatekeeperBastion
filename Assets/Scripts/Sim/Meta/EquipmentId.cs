namespace GatekeeperBastion.Sim
{
    /// <summary>Catalog identity for a piece of equipment. Values are written into save files —
    /// never renumber, never reuse a retired id. <see cref="None"/> is the empty-slot sentinel.</summary>
    public enum EquipmentId
    {
        None = 0,
        RepairRig = 1
    }
}
