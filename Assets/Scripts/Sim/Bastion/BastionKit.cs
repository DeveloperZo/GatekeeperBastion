namespace GatekeeperBastion.Sim
{
    /// <summary>Footprint multiples for bastion segment prefabs. Scaler retunes the whole kit.
    /// Pure floats — no UnityEngine (Sim seam + offline harness).</summary>
    public static class BastionKit
    {
        public const float ScalerX = 5f;
        public const float ScalerZ = 8f;

        public const float StoreyHeight = 5f;
        public const float WallThickness = 0.5f;
        public const float FloorSlabThickness = 0.5f;

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
            return Multiple(kind) * ScalerZ;
        }
    }
}
