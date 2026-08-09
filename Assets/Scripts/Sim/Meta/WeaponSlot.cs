namespace GatekeeperBastion.Sim
{
    /// <summary>Where a carried item sits on the player. Fight-time keys switch slots; the
    /// ARMORY fills Primary/Secondary with weapons, FABRICATION fills Drone with a payload.</summary>
    public enum WeaponSlot
    {
        Primary = 0,
        Secondary = 1,
        Drone = 2
    }
}
