namespace GatekeepersProtocol.Sim
{
    /// <summary>Catalog identity for a weapon. Values are written into save files — never
    /// renumber, never reuse a retired id. <see cref="None"/> is the empty-slot sentinel;
    /// <see cref="ThrowawayComposite"/> exists only so tests can prove a composite row
    /// without editing files outside <see cref="WeaponCatalog"/>. <see cref="SecD"/> is the
    /// close-quarters Secondary; <see cref="PriC"/> is the far-wall Primary (codenames until
    /// identity is solid).</summary>
    public enum WeaponId
    {
        None = -1,
        Mk1Sidearm = 0,
        Vk7Rifle = 1,
        Br12Shotgun = 2,
        Mr35Rifle = 3,
        /// <summary>Test-only composite row. Not listed in the ARMORY.</summary>
        ThrowawayComposite = 4,
        /// <summary>Close-quarters Secondary. Codename until play settles the name.</summary>
        SecD = 5,
        /// <summary>Far-wall Primary. Codename until play settles the name.</summary>
        PriC = 6
    }
}
