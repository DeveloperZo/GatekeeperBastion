namespace GatekeepersProtocol.Sim
{
    /// <summary>Pack prefab names for the bastion kit: one structural set for every block
    /// kind — per-kind variety is an authoring-pass decision. POLYGON Sci-Fi Space
    /// (Synty 138857). Plain strings — no UnityEngine (Sim seam + offline harness).</summary>
    public static class BastionMeshMap
    {
        public const string Floor = "SM_Bld_Floor_01";
        public const string Ceiling = "SM_Bld_Ceiling_01";
        public const string Wall = "SM_Bld_Wall_01";
        public const string Doorframe = "SM_Bld_Wall_Doorframe_01";

        /// <summary>The doorframe prefab carries a shut leaf child (SM_Bld_Wall_Door_01).
        /// The visuals layer disables it: BastionDoor's sliding slab is the one moving
        /// leaf, and a rendered shut leaf bakes a NavMesh wall across every doorway.</summary>
        public const string DoorframeLeafPrefix = "SM_Bld_Wall_Door_";
        public const string Window = "SM_Bld_Wall_Exterior_Window_01";
        public const string CornerPillar = "SM_Bld_Wall_Corner_Pillar_01";
        public const string CeilingLight = "SM_Prop_Light_Panel_01";

        /// <summary>Prefab name for a side kind; None means nothing is emitted and returns
        /// null. An unmapped kind throws — the loud fail, never a primitive.</summary>
        public static string ForSide(BastionSideKind kind)
        {
            switch (kind)
            {
                case BastionSideKind.Wall: return Wall;
                case BastionSideKind.Door: return Doorframe;
                case BastionSideKind.Window: return Window;
                case BastionSideKind.None: return null;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(kind), kind, "BastionSideKind has no mesh mapping");
            }
        }
    }
}
