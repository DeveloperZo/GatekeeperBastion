using System.Collections.Generic;

namespace GatekeepersProtocol.Sim
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
        Master = 3,
    }

    public struct BastionSketchMark
    {
        public BastionSketchMarkKind Kind;
        /// <summary>Spawn or terminal id from S:id / T:id. Empty for hatch and master.</summary>
        public string RefId;
        /// <summary>Spawn marks require an edge (S:S1:W). Unused for other kinds.</summary>
        public BastionSketchDirection Direction;
        public bool HasDirection;
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
        /// <summary>Metres along the edge from centre for the opening. N/S sides: +X; E/W: +Z.
        /// Abutting rooms whose centres differ on the shared axis need matching alongs or the
        /// two wall apertures miss each other.</summary>
        public float Along;
    }

    public sealed class BastionSketchDoor
    {
        public string DoorId;
        public string RoomId;
        public BastionSketchDirection Direction;
        public List<string> UnlockTerminalIds { get; } = new List<string>();
        public bool Control;
        /// <summary>Metres along the edge from centre. Same axis rule as side overrides.</summary>
        public float Along;
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
