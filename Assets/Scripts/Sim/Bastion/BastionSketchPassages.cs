using System.Collections.Generic;

namespace GatekeepersProtocol.Sim
{
    /// <summary>An open passage between two abutting rooms: both faces carry a Door side and
    /// no sketch door claims the shared aperture. The emitter mounts one auto door here.</summary>
    public struct BastionSketchPassage
    {
        public string Id;
        public string RoomId;
        public BastionSketchDirection Direction;
        public float Along;
    }

    /// <summary>Pure pairing of Door sides into shared apertures. Lives in Sim so the offline
    /// harness can assert passage detection without a scene.</summary>
    public static class BastionSketchPassages
    {
        /// <summary>One passage per shared aperture, owned by the first room in sketch order.
        /// Apertures claimed by a sketch door (locked gate) are excluded. Ids run P1, P2, ...
        /// in sketch room order, so re-emits are stable.</summary>
        public static List<BastionSketchPassage> Find(BastionSketch sketch)
        {
            var passages = new List<BastionSketchPassage>();
            var roomById = new Dictionary<string, BastionSketchRoom>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                roomById[sketch.Rooms[i].Id] = sketch.Rooms[i];

            var doorSides = new List<DoorSide>();
            for (int i = 0; i < sketch.Sides.Count; i++)
            {
                BastionSketchSideOverride side = sketch.Sides[i];
                if (side.Kind != BastionSideKind.Door)
                    continue;
                if (roomById.TryGetValue(side.RoomId, out BastionSketchRoom room))
                    doorSides.Add(MakeSide(room, side.Direction, side.Along, sketch));
            }

            // Door lines also open the side on their room (the emitter's ApplyDoorSides).
            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                if (roomById.TryGetValue(door.RoomId, out BastionSketchRoom room))
                    doorSides.Add(MakeSide(room, door.Direction, door.Along, sketch));
            }

            var claimed = new HashSet<long>();
            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                if (roomById.TryGetValue(door.RoomId, out BastionSketchRoom room))
                    claimed.Add(MakeSide(room, door.Direction, door.Along, sketch).Key);
            }

            var seen = new HashSet<long>();
            var partnered = new HashSet<long>();
            for (int i = 0; i < doorSides.Count; i++)
            {
                long key = doorSides[i].Key;
                if (seen.Contains(key))
                    partnered.Add(key);
                else
                    seen.Add(key);
            }

            int next = 1;
            var emitted = new HashSet<long>();
            for (int i = 0; i < doorSides.Count; i++)
            {
                DoorSide side = doorSides[i];
                if (!partnered.Contains(side.Key) || claimed.Contains(side.Key) || emitted.Contains(side.Key))
                    continue;
                emitted.Add(side.Key);
                passages.Add(new BastionSketchPassage
                {
                    Id = "P" + next++,
                    RoomId = side.RoomId,
                    Direction = side.Direction,
                    Along = side.Along,
                });
            }

            return passages;
        }

        /// <summary>True when this room edge already pairs as an open passage.</summary>
        public static bool EdgeIsPaired(
            BastionSketch sketch,
            string roomId,
            BastionSketchDirection direction)
        {
            if (sketch == null || string.IsNullOrEmpty(roomId))
                return false;

            BastionSketchRoom room = null;
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                if (sketch.Rooms[i].Id == roomId)
                {
                    room = sketch.Rooms[i];
                    break;
                }
            }
            if (room == null)
                return false;

            float along = 0f;
            for (int i = 0; i < sketch.Sides.Count; i++)
            {
                BastionSketchSideOverride side = sketch.Sides[i];
                if (side.RoomId == roomId && side.Direction == direction)
                    along = side.Along;
            }

            long want = MakeSide(room, direction, along, sketch).Key;

            var roomById = new Dictionary<string, BastionSketchRoom>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                roomById[sketch.Rooms[i].Id] = sketch.Rooms[i];

            var doorSides = new List<DoorSide>();
            for (int i = 0; i < sketch.Sides.Count; i++)
            {
                BastionSketchSideOverride side = sketch.Sides[i];
                if (side.Kind != BastionSideKind.Door)
                    continue;
                if (roomById.TryGetValue(side.RoomId, out BastionSketchRoom r))
                    doorSides.Add(MakeSide(r, side.Direction, side.Along, sketch));
            }
            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                if (roomById.TryGetValue(door.RoomId, out BastionSketchRoom r))
                    doorSides.Add(MakeSide(r, door.Direction, door.Along, sketch));
            }

            var seen = new HashSet<long>();
            var partnered = new HashSet<long>();
            for (int i = 0; i < doorSides.Count; i++)
            {
                long key = doorSides[i].Key;
                if (seen.Contains(key))
                    partnered.Add(key);
                else
                    seen.Add(key);
            }

            return partnered.Contains(want);
        }

        struct DoorSide
        {
            public string RoomId;
            public BastionSketchDirection Direction;
            public float Along;
            public long Key;
        }

        static DoorSide MakeSide(
            BastionSketchRoom room,
            BastionSketchDirection direction,
            float along,
            BastionSketch sketch)
        {
            float halfW = BastionKit.FootprintWidth(room.Kind) * 0.5f;
            float halfD = BastionKit.FootprintDepth(room.Kind) * 0.5f;

            // Shared-aperture key: the footprint edge line plus the opening centre on it,
            // quantised to 0.25 m. Two abutting Door sides land on the same key only when
            // their alongs actually line the openings up (the sketch grammar's contract).
            float edge, at;
            bool northSouth = direction == BastionSketchDirection.North || direction == BastionSketchDirection.South;
            if (northSouth)
            {
                edge = room.CenterZ + (direction == BastionSketchDirection.North ? halfD : -halfD);
                at = room.CenterX + along;
            }
            else
            {
                edge = room.CenterX + (direction == BastionSketchDirection.East ? halfW : -halfW);
                at = room.CenterZ + along;
            }

            long q = (long)room.Storey * 10000000000L
                + (long)System.Math.Round(edge * 4.0) * 1000000L
                + (long)System.Math.Round(at * 4.0) * 4L
                + (northSouth ? 1L : 2L);

            return new DoorSide { RoomId = room.Id, Direction = direction, Along = along, Key = q };
        }
    }
}
