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

        public const string LocalHeadLayer = "LocalHead";

        /// <summary>MK-1 hip hold in camera space. The body SMR is culled, so bind-pose hand
        /// is behind the lens; this offset keeps a sidearm in the lower-LEFT of a 60° FOV.
        /// The Boarder is left-handed (Zo, 2026-09-02): the M113 arms were posed with the gun
        /// in hand.l, and a left hand cannot read in a right-hand pocket, so Right is
        /// negative and the yaw/roll below are mirrored with it. Shop guns share this one
        /// pocket, so the arms prop riding the wrapper root sits the same way for every
        /// mesh.</summary>
        public const float LocalGunRight = -0.18f;
        public const float LocalGunDown = -0.14f;
        public const float LocalGunForward = 0.38f;

        /// <summary>Long-gun pocket (rifle and shotgun classes), camera space. Held across
        /// the body: nearer the centreline than the sidearm and pulled back toward the
        /// camera so the arms prop's elbow cuts fall below the frame edge. **These three are
        /// the tuning surface** — <see cref="Hold"/> returns them unchanged and
        /// WeaponViewTests.Hold_LongGunKeepsThePocketConstants asserts exactly that, so
        /// moving a number here never breaks a test; the relation test
        /// (nearer and closer than the sidearm) is the only guard. Tuned by eye against the
        /// MR-35 (Zo, 2026-09-02).</summary>
        public const float LongGunRight = -0.02f;
        public const float LongGunDown = -0.17f;
        public const float LongGunForward = 0.16f;

        /// <summary>Which arms prop a weapon shows. One baked pose per class, sorted the way
        /// <see cref="FireClip"/> already sorts the catalog. A class with no bake shows no
        /// arms rather than another class's hands on this gun. SecD is a two-handed SMG and
        /// shares the rifle grip until its criterion-3 rows say otherwise.</summary>
        public enum ArmsClass { Pistol, Rifle, Shotgun, Melee }

        public const string ArmsPistol = "Pistol";
        public const string ArmsRifle = "Rifle";
        public const string ArmsShotgun = "Shotgun";
        public const string ArmsMelee = "Melee";

        public static ArmsClass ArmsClassFor(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.None: return ArmsClass.Melee;
                case WeaponId.Br12Shotgun: return ArmsClass.Shotgun;
                case WeaponId.Mr35Rifle:
                case WeaponId.Vk7Rifle:
                case WeaponId.PriC:
                case WeaponId.SecD: return ArmsClass.Rifle;
                default: return ArmsClass.Pistol;
            }
        }

        /// <summary>The child name under the Arms prefab root for a class. Const strings, so
        /// the per-switch lookup in the viewmodel allocates nothing.</summary>
        public static string ArmsClassName(ArmsClass c)
        {
            switch (c)
            {
                case ArmsClass.Rifle: return ArmsRifle;
                case ArmsClass.Shotgun: return ArmsShotgun;
                case ArmsClass.Melee: return ArmsMelee;
                default: return ArmsPistol;
            }
        }

        /// <summary>Camera-local metres and degrees for the Grip. Clips still key wrapper
        /// roots; this is the hold the kick plays on top of.</summary>
        public struct GunHold
        {
            public float Right, Down, Forward;
            public float Pitch, Yaw, Roll;
        }

        /// <summary>Fbx-child local +Z so the stock sits at the Grip origin. PRI-C uses
        /// the same shift and 1,1,1 scale as MR-35 — the 1.4 z-stretch made a plank.</summary>
        public const float PriCFbxLocalZ = 0.204f;
        public const float Mr35FbxLocalZ = 0.204f;

        /// <summary>Shop guns share the MK-1 pocket. Cutter is melee.
        /// <paramref name="zoom"/> is unused here: the Grip hold does not change on zoom, and
        /// <see cref="ArmsOffset"/> is what takes the stance.</summary>
        public static GunHold Hold(WeaponId id, bool zoom = false)
        {
            if (id == WeaponId.None)
            {
                return new GunHold
                {
                    Right = -0.22f, Down = -0.08f, Forward = 0.30f,
                    Pitch = 18f, Yaw = 10f, Roll = -50f
                };
            }
            ArmsClass c = ArmsClassFor(id);
            if (c == ArmsClass.Rifle || c == ArmsClass.Shotgun) return LongGunHip();
            return Mk1Hip();
        }

        /// <summary>The long-gun pocket from the three <c>LongGun*</c> constants. The stock
        /// sits at the pocket origin (the wrapper's fbx child is shifted +0.204), so the
        /// muzzle lands ahead of the hands.</summary>
        static GunHold LongGunHip()
        {
            return new GunHold
            {
                Right = LongGunRight, Down = LongGunDown, Forward = LongGunForward,
                Pitch = 0f, Yaw = 0f, Roll = 0f
            };
        }

        static GunHold Mk1Hip()
        {
            return new GunHold
            {
                Right = LocalGunRight, Down = LocalGunDown, Forward = LocalGunForward,
                Pitch = 0f, Yaw = -2f, Roll = -10f
            };
        }

        /// <summary>Where the arms prop sits on the weapon it is parented to. The TRS is local
        /// to the wrapper root (the transform the viewmodel clips key), not to the camera, so
        /// the kick and the bob carry the hands with the gun. The switch mirrors the mesh
        /// mapping: five catalog ids and a default covering <see cref="WeaponId.None"/> — the
        /// cutter — plus <see cref="WeaponId.Vk7Rifle"/> and
        /// <see cref="WeaponId.ThrowawayComposite"/>. Every branch returns zero today: the six
        /// floats are an open decision owned by Zo and are set by the play pass against a real
        /// arms mesh, so zero is a placeholder rather than a guess dressed as a value. Add a
        /// per-class branch only when a weapon's hands are seen to clip or float.</summary>
        // POC: every branch returns zero and zoom is not read yet. The switch is the
        // extension point, kept so a per-class tune is a one-line swap; the floats are set
        // against a real arms mesh in M113 step 4. SecD's wrapper root is scaled 0.88, so it
        // needs its own branch before its rows can pass.
        public static GunHold ArmsOffset(WeaponId id, bool zoom = false)
        {
            switch (id)
            {
                case WeaponId.Mk1Sidearm: return TwoHandGrip();
                case WeaponId.SecD: return TwoHandGrip();
                case WeaponId.Br12Shotgun: return TwoHandGrip();
                case WeaponId.Mr35Rifle: return TwoHandGrip();
                case WeaponId.PriC: return TwoHandGrip();
                default: return TwoHandGrip();
            }
        }

        static GunHold TwoHandGrip()
        {
            return new GunHold
            {
                Right = 0f, Down = 0f, Forward = 0f,
                Pitch = 0f, Yaw = 0f, Roll = 0f
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
