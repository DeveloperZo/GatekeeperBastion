namespace GatekeepersProtocol.Sim
{
    /// <summary>Isometric Ship camera rig — one source for the Game rig and docgen, so the
    /// probe's numbers land in Docs/Design/4-Data.md generated rather than hand-written.
    /// Rotation is LOCKED: at 45° yaw the camera looks toward +X/+Z, so the ship's south and
    /// west walls are the camera-facing (occluding) sides. That constancy is what makes wall
    /// render state a bake decision; allowing rotation later means emitting all four wall
    /// sides and a runtime toggle.</summary>
    public static class ShipCamera
    {
        #region Constants

        /// <summary>World yaw of the view, degrees. Locked — which walls face the camera is a
        /// constant, never a runtime question.</summary>
        public const float YawDegrees = 45f;

        /// <summary>Downward pitch of the view, degrees. High angle — a narrow-FOV
        /// perspective camera needs it or near geometry looms.</summary>
        public const float PitchDegrees = 50f;

        /// <summary>Vertical field of view, degrees. Perspective, deliberately narrow —
        /// a wide FOV at this distance reads as a security camera, not an isometric game;
        /// the pushed-back narrow lens keeps parallax subtle.</summary>
        public const float FovDegrees = 30f;

        /// <summary>Back-off along the view ray from the focus point, metres. Constraint:
        /// framing-equivalent at the focus plane to the orthographic probe this replaces
        /// (half-height 10, i.e. 20 m vertical) — distance = 10 / tan(FovDegrees / 2)
        /// = 10 / tan(15°) ≈ 37.3, rounded to a clean 38. Perspective: framing DOES change
        /// with this value, so retune it and FovDegrees together.</summary>
        public const float FollowDistance = 38f;

        /// <summary>Dead-zone half-extent on world X, metres, measured on the ground plane.
        /// The camera holds still while the player stays inside the zone.</summary>
        public const float DeadZoneHalfX = 1.5f;

        /// <summary>Dead-zone half-extent on world Z, metres, measured on the ground plane.</summary>
        public const float DeadZoneHalfZ = 1.5f;

        /// <summary>Exponential follow rate once the player exits the dead zone, per second.
        /// Each second closes about 99.8% of the remaining gap at this value.</summary>
        public const float FollowLerpPerSecond = 6f;

        #endregion
    }
}
