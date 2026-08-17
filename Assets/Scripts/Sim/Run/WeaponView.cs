namespace GatekeepersProtocol.Sim
{
    /// <summary>First-person view mapping: FOV, footstep clock, mesh prefab paths, and SFX
    /// resource names. Numbers live here so EditMode can assert them without MonoBehaviours.
    /// Wrapper prefabs under Packs/Weapons are distinct assets even when two rows share a
    /// Synty mesh (the pack ships pistol, rifle, shotgun, sword — not six silhouettes).</summary>
    public static class WeaponView
    {
        public const float HipFov = 60f;
        public const float ZoomFov = 40f;
        public const float WalkFootstepSeconds = 0.45f;
        public const float SprintFootstepSeconds = 0.32f;

        public const string Mk1Prefab = "Assets/Packs/Weapons/Mk1.prefab";
        public const string SecDPrefab = "Assets/Packs/Weapons/SecD.prefab";
        public const string Br12Prefab = "Assets/Packs/Weapons/Br12.prefab";
        public const string Mr35Prefab = "Assets/Packs/Weapons/Mr35.prefab";
        public const string PriCPrefab = "Assets/Packs/Weapons/PriC.prefab";
        public const string CutterPrefab = "Assets/Packs/Weapons/Cutter.prefab";

        public const string FirePistol = "Audio/Sfx/Fire-Pistol";
        public const string FireShotgun = "Audio/Sfx/Fire-Shotgun";
        public const string FireRifle = "Audio/Sfx/Fire-Rifle";
        public const string Reload = "Audio/Sfx/Reload";
        public const string FootstepWalk = "Audio/Sfx/Footstep-Walk";

        public const string RightHandBone = "mixamorig:RightHand";
        public const string LocalHeadLayer = "LocalHead";

        /// <summary>Local-owner gun pose in camera space. The body SMR is culled, so bind-pose
        /// hand is behind the lens; this offset keeps the mesh in the lower-right of a 60° FOV.</summary>
        public const float LocalGunRight = 0.18f;
        public const float LocalGunDown = -0.14f;
        public const float LocalGunForward = 0.38f;

        /// <summary>PlayOneShot volume scale on fire, reload, and footsteps. Placeholder PCM
        /// bursts at bus 0.9 were harsh; this is leverage #12, one number. Slider still multiplies.</summary>
        public const float SfxOneShotScale = 0.5f;

        /// <summary>Which hand mesh to show. Unowned on the Ship is empty hands. Unowned in a
        /// fight is the cutter (<see cref="WeaponId.None"/>). A shop mesh only when that id is
        /// equipped.</summary>
        /// <returns>False means hide every mesh. True means show <paramref name="id"/>.</returns>
        public static bool TryShownWeapon(bool ownsAny, bool fightHosted, WeaponId equipped,
            out WeaponId id)
        {
            if (!ownsAny)
            {
                id = WeaponId.None;
                return fightHosted;
            }
            id = equipped;
            return equipped != WeaponId.None;
        }

        /// <summary>Prefab path for the mesh shown for this catalog id. <see cref="WeaponId.None"/>
        /// is the improvised cutter.</summary>
        public static string MeshPrefab(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Mk1Sidearm: return Mk1Prefab;
                case WeaponId.SecD: return SecDPrefab;
                case WeaponId.Br12Shotgun: return Br12Prefab;
                case WeaponId.Mr35Rifle: return Mr35Prefab;
                case WeaponId.PriC: return PriCPrefab;
                default: return CutterPrefab;
            }
        }

        /// <summary>Resources path for the fire one-shot (pistol / shotgun / rifle classes).</summary>
        public static string FireClip(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Br12Shotgun: return FireShotgun;
                case WeaponId.Mr35Rifle:
                case WeaponId.Vk7Rifle:
                case WeaponId.PriC: return FireRifle;
                default: return FirePistol;
            }
        }

        /// <summary>The six mapped prefab paths, no duplicates.</summary>
        public static string[] AllMeshPrefabs()
        {
            return new[] { Mk1Prefab, SecDPrefab, Br12Prefab, Mr35Prefab, PriCPrefab, CutterPrefab };
        }
    }
}
