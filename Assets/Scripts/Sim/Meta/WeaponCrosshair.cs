namespace GatekeeperBastion.Sim
{
    /// <summary>Crosshair shape drawn by the Bastion HUD for a weapon. Identity cue, not
    /// accuracy — spread lives on <see cref="WeaponDef.spreadDegrees"/>.</summary>
    public enum WeaponCrosshair
    {
        Dot = 0,
        Cross = 1,
        Brackets = 2
    }
}
