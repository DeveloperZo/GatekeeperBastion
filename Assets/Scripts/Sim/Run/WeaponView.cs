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

        /// <summary>MK-1 hip hold in camera space. The body SMR is culled, so bind-pose hand
        /// is behind the lens; this offset keeps a sidearm in the lower-right of a 60° FOV.
        /// Shop guns share it — a second pocket is a floating gun until arms exist.</summary>
        public const float LocalGunRight = 0.18f;
        public const float LocalGunDown = -0.14f;
        public const float LocalGunForward = 0.38f;

        /// <summary>Camera-local metres and degrees for the Grip. Clips still key wrapper
        /// roots; this is the hold the kick plays on top of.</summary>
        public struct GunHold
        {
            public float Right, Down, Forward;
            public float Pitch, Yaw, Roll;
        }

        /// <summary>Fbx-child local +Z. PriC 0.12 puts the pistol grip at the wrapper
        /// origin (stock 0.19 m behind after z-scale 1.4). 0.257 held the butt and the
        /// rifle floated in front of the MK-1 pocket.</summary>
        public const float PriCFbxLocalZ = 0.12f;
        public const float Mr35FbxLocalZ = 0.204f;

        /// <summary>Shop guns share the MK-1 pocket. PRI-C keeps that hand (Right/Forward)
        /// but sits lower and yaws in so the long mesh runs along the bottom toward the
        /// reticle instead of as a side-on plank. Cutter is melee. Zoom is unused until arms.</summary>
        public static GunHold Hold(WeaponId id, bool zoom = false)
        {
            if (id == WeaponId.None)
            {
                return new GunHold
                {
                    Right = 0.22f, Down = -0.08f, Forward = 0.30f,
                    Pitch = 18f, Yaw = -10f, Roll = 50f
                };
            }
            if (id == WeaponId.PriC)
            {
                return new GunHold
                {
                    Right = LocalGunRight, Down = -0.22f, Forward = LocalGunForward,
                    Pitch = -6f, Yaw = -14f, Roll = 0f
                };
            }
            return Mk1Hip();
        }

        static GunHold Mk1Hip()
        {
            return new GunHold
            {
                Right = LocalGunRight, Down = LocalGunDown, Forward = LocalGunForward,
                Pitch = 0f, Yaw = 2f, Roll = 10f
            };
        }

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
