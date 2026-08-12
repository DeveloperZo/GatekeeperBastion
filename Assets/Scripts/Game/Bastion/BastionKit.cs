using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Footprint multiples for bastion segment prefabs. Scaler retunes the whole kit.</summary>
    public static class BastionKit
    {
        public static readonly Vector2 Scaler = new Vector2(5f, 8f);

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

        /// <summary>Footprint in local XZ: width (x) × depth (z).</summary>
        public static Vector2 Footprint(BastionBlockKind kind)
        {
            int multiple = Multiple(kind);
            return new Vector2(multiple * Scaler.x, multiple * Scaler.y);
        }
    }
}
