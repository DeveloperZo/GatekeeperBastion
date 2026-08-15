namespace GatekeeperBastion.Sim
{
    /// <summary>Footprint multiples for bastion segment prefabs. Scaler retunes the whole kit.
    /// Pure floats — no UnityEngine (Sim seam + offline harness).
    /// Scaler 2 over metre multiples: big lost-station rooms (Half 16 m), one lever.
    /// Sketch room centres are world metres, so a scaler change re-authors the sketch too.
    /// ControlRoom is the wide finale hall — twice as wide as deep.</summary>
    public static class BastionKit
    {
        public const float ScalerX = 2f;
        public const float ScalerZ = 2f;

        public const float StoreyHeight = 5f;
        public const float WallThickness = 0.5f;
        public const float FloorSlabThickness = 0.5f;

        /// <summary>Field-terminal Desk/Screen/Label multiplier. 1 is Ship-console size (a
        /// Boarder can use the desk). StoreyHeight/2.5 (=2 at storey 5) was room-fill and put
        /// the desk top at 2.4 m.</summary>
        public const float FieldConsoleScale = 1f;
        public const float FieldConsoleDeskWidth = 1.6f;
        public const float FieldConsoleDeskHeight = 1.2f;
        public const float FieldConsoleDeskDepth = 0.7f;
        public const float FieldConsoleDeskCentreY = 0.6f;
        public const float FieldConsoleScreenCentreY = 1.7f;

        /// <summary>Depth-to-width ratio for the ControlRoom's landscape footprint.</summary>
        public const float ControlRoomAspect = 0.5f;

        public static int Multiple(BastionBlockKind kind)
        {
            switch (kind)
            {
                case BastionBlockKind.Quarter: return 5;
                case BastionBlockKind.Half: return 8;
                case BastionBlockKind.Full: return 13;
                case BastionBlockKind.OpenArea: return 21;
                case BastionBlockKind.ControlRoom: return 76;
                default: return 5;
            }
        }

        public static float FootprintWidth(BastionBlockKind kind)
        {
            return Multiple(kind) * ScalerX;
        }

        public static float FootprintDepth(BastionBlockKind kind)
        {
            float depth = Multiple(kind) * ScalerZ;
            return kind == BastionBlockKind.ControlRoom ? depth * ControlRoomAspect : depth;
        }
    }
}
