namespace GatekeepersProtocol.Game
{
    /// <summary>One frame of player input, produced once per frame by
    /// <see cref="Controls.Sample"/> — the single place UnityEngine.Input is read.
    /// Consumers read <see cref="Controls.Frame"/> (or the per-id accessors
    /// <see cref="Controls.Down"/> / <see cref="Controls.Held"/>, which read these bits).
    /// Serializable so a future co-op layer can ship the whole frame over the wire.</summary>
    [System.Serializable]
    public struct PlayerInputFrame
    {
        // Axes ("Horizontal"/"Vertical" and "Mouse X"/"Mouse Y") and mouse wheel.
        public float moveX;
        public float moveY;
        public float lookX;
        public float lookY;
        public float scroll;

        // OS cursor position in screen pixels (the engine mouse position, sampled by the
        // producer), for cursor-driven aim/interact under the isometric camera. Floats,
        // not a vector type, so the frame stays plain data for the offline harness.
        public float pointerX;
        public float pointerY;

        // Per-ControlId bits, computed from the Controls binding table (Key/AltKey,
        // DisplayOnly rows skipped, DevConsole suppression applied in dev builds).
        // Held/Down variants exist only for the ids gameplay actually asks for.
        public bool sprintHeld;
        public bool fireHeld;
        public bool fireDown;
        public bool altFireDown;
        public bool altFireHeld;
        public bool reloadDown;
        public bool slotPrimaryDown;
        public bool slotSecondaryDown;
        public bool slotDroneDown;
        public bool slotLastDown;
        public bool weaponAbilityDown;
        public bool crewAbilityDown;
        public bool personalAbilityDown;
        public bool interactHeld;
        public bool interactDown;
        public bool pullDrawDown;
        public bool dialUpDown;
        public bool dialDownDown;
        public bool wheelPrevDown;
        public bool wheelNextDown;
        public bool freeCursorDown;
        public bool confirmDown;
        public bool togglePanelDown;
        public bool toggleMissionDrawerDown;

        // Front-end bits, independent of the binding table: press-any-key screens,
        // click-to-relock, and each scene's Escape dispatcher.
        public bool anyPressDown;
        public bool click0Down;
        public bool click1Down;
        public bool escapeDown;
    }
}
