namespace GatekeepersProtocol.Sim
{
    /// <summary>Footprint multiples for bastion segment prefabs. Scaler retunes the whole kit.
    /// Pure floats — no UnityEngine (Sim seam + offline harness).
    /// Scaler 2 over metre multiples: big lost-station rooms (Half 16 m), one lever.
    /// Sketch room centres are world metres, so a scaler change re-authors the sketch too.
    /// ControlRoom is the wide finale hall — twice as wide as deep (multiple 47, aspect 0.5).</summary>
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
                case BastionBlockKind.ControlRoom: return 47;
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

        /// <summary>A hash of every SHARED number a segment's geometry is built from, so a
        /// build that changes one can be told apart from the build that generated what is
        /// already in the scene.
        ///
        /// It exists because omitting it cost two shipped changes. A segment stamped itself
        /// with a hash of its own serialized fields only — kind, the four sides, the four
        /// door offsets — and rebuilt only when that stamp changed. Constants are not fields,
        /// so widening the doorways never invalidated anything: on 2026-09-04 Bastion_Coop was
        /// still carrying 3 m openings with a lintel hung across each one, against code that
        /// had said 6 m and full height since 2026-08-28. The scene had been right about what
        /// it was built from and wrong about the game for a week.
        ///
        /// Pass the Game layer's aperture constants in; the kit's own are read here.</summary>
        public static int GeometryStamp(float doorWidth, float doorMaxShare, float doorHeight)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + doorWidth.GetHashCode();
                h = h * 31 + doorMaxShare.GetHashCode();
                h = h * 31 + doorHeight.GetHashCode();
                h = h * 31 + ScalerX.GetHashCode();
                h = h * 31 + ScalerZ.GetHashCode();
                h = h * 31 + StoreyHeight.GetHashCode();
                h = h * 31 + WallThickness.GetHashCode();
                h = h * 31 + FloorSlabThickness.GetHashCode();
                return h == 0 ? 1 : h;
            }
        }

        /// <summary>The clear width a doorway or window resolves to on this block: the authored
        /// width, or the share cap of the block's SHORT side when that is tighter.
        ///
        /// It lives here, in pure arithmetic on the footprint, because it is the rule that
        /// decides whether the two ends of a shared passage agree. Each room builds its own
        /// walls knowing nothing about its neighbour, so if two kinds resolve differently the
        /// passage grows two different mouths and nothing in the geometry notices. The Game
        /// layer's BastionSegment holds the two inputs and calls this; the offline suite
        /// checks the agreement across every kind the sketch authors.</summary>
        public static float ApertureWidth(BastionBlockKind kind, float authoredWidth, float maxShare)
        {
            float width = FootprintWidth(kind);
            float depth = FootprintDepth(kind);
            float shortSide = width < depth ? width : depth;
            float cap = shortSide * maxShare;
            return authoredWidth < cap ? authoredWidth : cap;
        }
    }
}
