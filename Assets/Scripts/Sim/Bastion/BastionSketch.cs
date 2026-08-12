using System.Collections.Generic;

namespace GatekeeperBastion.Sim
{
    /// <summary>Structured bastion layout from a line-oriented sketch file.</summary>
    public sealed class BastionSketch
    {
        public List<BastionSketchRoom> Rooms { get; } = new List<BastionSketchRoom>();
        public List<BastionSketchSideOverride> Sides { get; } = new List<BastionSketchSideOverride>();
        public List<BastionSketchDoor> Doors { get; } = new List<BastionSketchDoor>();
    }

    public sealed class BastionSketchRoom
    {
        public string Id;
        public BastionBlockKind Kind;
        public float CenterX;
        public float CenterZ;
        public List<BastionSketchMark> Marks { get; } = new List<BastionSketchMark>();
    }

    public enum BastionSketchMarkKind : byte
    {
        Hatch = 0,
        Spawn = 1,
        Terminal = 2,
    }

    public struct BastionSketchMark
    {
        public BastionSketchMarkKind Kind;
        /// <summary>Spawn or terminal id from S:id / T:id. Empty for hatch.</summary>
        public string RefId;
    }

    public enum BastionSketchDirection : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public sealed class BastionSketchSideOverride
    {
        public string RoomId;
        public BastionSketchDirection Direction;
        public BastionSideKind Kind;
        public bool Locked;
    }

    public sealed class BastionSketchDoor
    {
        public string DoorId;
        public string RoomId;
        public BastionSketchDirection Direction;
        public List<string> UnlockTerminalIds { get; } = new List<string>();
        public bool Control;
    }

    public struct BastionSketchParseResult
    {
        public bool Success;
        public string Error;
        public BastionSketch Sketch;

        public static BastionSketchParseResult Ok(BastionSketch sketch)
        {
            return new BastionSketchParseResult { Success = true, Sketch = sketch };
        }

        public static BastionSketchParseResult Fail(string error)
        {
            return new BastionSketchParseResult { Success = false, Error = error };
        }
    }
}
