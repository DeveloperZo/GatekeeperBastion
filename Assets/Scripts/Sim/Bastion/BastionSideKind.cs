namespace GatekeepersProtocol.Sim
{
    public enum BastionSideKind : byte
    {
        Wall = 0,
        Door = 1,
        Window = 2,
        None = 3,

        /// <summary>The wall is gone: the whole span is open, bar one divider pillar at its
        /// centre (Zo, 2026-09-04: "widen the northern entrance to be full width of R2, maybe
        /// a small divider in middle"). A Door cuts an opening INTO a wall and is bounded by
        /// <c>BastionSegment.DoorOpeningWidth</c>; this is the wall's absence, so the room
        /// beyond is continuous with this one and the pillar is all that reads as a threshold.
        ///
        /// A passage everywhere a Door is: it pairs in <see cref="BastionSketchPassages"/> and
        /// counts for reachability. It is NOT a gate — a `door D` line still needs a Door side,
        /// and <c>TryGetDoorAperture</c> answers only for Door, so nothing tries to hang a leaf
        /// in an opening that has no frame.
        ///
        /// Both faces of the passage must carry it and the two rooms must have the same span on
        /// that edge, or one room opens 16 m and the other 42 and the passage has two mouths.
        /// Never renumber: this is serialized on every segment.</summary>
        OpenSpan = 4,
    }
}
