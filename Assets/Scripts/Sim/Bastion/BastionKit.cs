namespace GatekeeperBastion.Sim
{
    /// <summary>Footprint multiples for bastion segment prefabs. Scaler retunes the whole kit.
    /// Pure floats — no UnityEngine (Sim seam + offline harness).
    /// Scaler 2 over metre multiples: derelict-station scale (Half room 16 m), one lever.
    /// Sketch room centres are world metres, so a scaler change re-authors the sketch too.
    /// ControlRoom is the wide finale hall — twice as wide as deep.</summary>
    public static class BastionKit
    {
        public const float ScalerX = 2f;
        public const float ScalerZ = 2f;

        public const float StoreyHeight = 5f;
        public const float WallThickness = 0.5f;
        public const float FloorSlabThickness = 0.5f;

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
