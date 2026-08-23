using System;
using System.Collections.Generic;
using System.Globalization;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Parses bastion sketch v1 lines into <see cref="BastionSketch"/>.</summary>
    public static class BastionSketchParser
    {
        public static BastionSketchParseResult Parse(string text)
        {
            if (text == null)
                return BastionSketchParseResult.Fail("sketch text is null");

            var sketch = new BastionSketch();
            var roomIds = new HashSet<string>();
            var doorIds = new HashSet<string>();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string raw = lines[lineIndex];
                if (raw == null)
                    continue;

                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                string[] tokens = SplitTokens(line);
                if (tokens.Length == 0)
                    continue;

                string verb = tokens[0];
                if (string.Equals(verb, "room", StringComparison.OrdinalIgnoreCase))
                {
                    BastionSketchParseResult roomResult = ParseRoom(tokens, lineIndex + 1, roomIds, sketch);
                    if (!roomResult.Success)
                        return roomResult;
                }
                else if (string.Equals(verb, "side", StringComparison.OrdinalIgnoreCase))
                {
                    BastionSketchParseResult sideResult = ParseSide(tokens, lineIndex + 1, sketch);
                    if (!sideResult.Success)
                        return sideResult;
                }
                else if (string.Equals(verb, "door", StringComparison.OrdinalIgnoreCase))
                {
                    BastionSketchParseResult doorResult = ParseDoor(tokens, lineIndex + 1, doorIds, sketch);
                    if (!doorResult.Success)
                        return doorResult;
                }
                else if (string.Equals(verb, "stair", StringComparison.OrdinalIgnoreCase))
                {
                    BastionSketchParseResult stairResult = ParseStair(tokens, lineIndex + 1, sketch);
                    if (!stairResult.Success)
                        return stairResult;
                }
                else
                {
                    return BastionSketchParseResult.Fail("line " + (lineIndex + 1) + ": unknown verb '" + verb + "'");
                }
            }

            return Validate(sketch);
        }

        static BastionSketchParseResult ParseRoom(
            string[] tokens,
            int lineNumber,
            HashSet<string> roomIds,
            BastionSketch sketch)
        {
            if (tokens.Length < 5)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": room requires id, kind, x, z");

            string id = tokens[1];
            if (!roomIds.Add(id))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": duplicate room id '" + id + "'");

            if (!TryParseBlockKind(tokens[2], out BastionBlockKind kind))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": unknown block kind '" + tokens[2] + "'");

            if (!TryParseFloat(tokens[3], out float x))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid x '" + tokens[3] + "'");
            if (!TryParseFloat(tokens[4], out float z))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid z '" + tokens[4] + "'");

            var room = new BastionSketchRoom
            {
                Id = id,
                Kind = kind,
                CenterX = x,
                CenterZ = z,
            };

            bool sawStorey = false;
            for (int i = 5; i < tokens.Length; i++)
            {
                if (tokens[i].StartsWith("storey=", StringComparison.OrdinalIgnoreCase))
                {
                    if (sawStorey)
                        return BastionSketchParseResult.Fail("line " + lineNumber + ": duplicate storey=");
                    if (!TryParseStorey(tokens[i], out int storey))
                        return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid storey '" + tokens[i] + "'");
                    room.Storey = storey;
                    sawStorey = true;
                    continue;
                }

                BastionSketchParseResult markResult = ParseMark(tokens[i], lineNumber, room);
                if (!markResult.Success)
                    return markResult;
            }

            sketch.Rooms.Add(room);
            return BastionSketchParseResult.Ok(sketch);
        }

        static BastionSketchParseResult ParseMark(string token, int lineNumber, BastionSketchRoom room)
        {
            if (string.Equals(token, "H", StringComparison.OrdinalIgnoreCase))
            {
                room.Marks.Add(new BastionSketchMark { Kind = BastionSketchMarkKind.Hatch });
                return BastionSketchParseResult.Ok(null);
            }

            if (token.StartsWith("H:", StringComparison.OrdinalIgnoreCase))
                return ParseNumberedHatch(token, lineNumber, room);

            if (string.Equals(token, "M", StringComparison.OrdinalIgnoreCase))
            {
                room.Marks.Add(new BastionSketchMark { Kind = BastionSketchMarkKind.Master });
                return BastionSketchParseResult.Ok(null);
            }

            int colon = token.IndexOf(':');
            if (colon <= 0 || colon >= token.Length - 1)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid mark '" + token + "'");

            string prefix = token.Substring(0, colon);
            string refId = token.Substring(colon + 1);
            if (refId.Length == 0)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": empty mark id in '" + token + "'");

            if (string.Equals(prefix, "S", StringComparison.OrdinalIgnoreCase))
            {
                int edgeColon = refId.IndexOf(':');
                if (edgeColon <= 0 || edgeColon >= refId.Length - 1)
                {
                    return BastionSketchParseResult.Fail(
                        "line " + lineNumber + ": spawn mark '" + token + "' requires an edge (S:S1:W)");
                }

                string spawnId = refId.Substring(0, edgeColon);
                string edgeToken = refId.Substring(edgeColon + 1);
                int minSeats = 1;
                int minColon = edgeToken.IndexOf(':');
                if (minColon >= 0)
                {
                    string minRaw = edgeToken.Substring(minColon + 1);
                    edgeToken = edgeToken.Substring(0, minColon);
                    if (!TryParseMinSeats(minRaw, out minSeats))
                    {
                        return BastionSketchParseResult.Fail(
                            "line " + lineNumber + ": invalid spawn minSeats '" + minRaw + "'");
                    }
                }

                if (!TryParseDirection(edgeToken, out BastionSketchDirection edge))
                {
                    return BastionSketchParseResult.Fail(
                        "line " + lineNumber + ": invalid spawn edge '" + edgeToken + "'");
                }

                room.Marks.Add(new BastionSketchMark
                {
                    Kind = BastionSketchMarkKind.Spawn,
                    RefId = spawnId,
                    Direction = edge,
                    HasDirection = true,
                    MinSeats = minSeats,
                });
                return BastionSketchParseResult.Ok(null);
            }

            if (string.Equals(prefix, "T", StringComparison.OrdinalIgnoreCase))
            {
                room.Marks.Add(new BastionSketchMark { Kind = BastionSketchMarkKind.Terminal, RefId = refId });
                return BastionSketchParseResult.Ok(null);
            }

            return BastionSketchParseResult.Fail("line " + lineNumber + ": unknown mark prefix '" + prefix + "'");
        }

        static BastionSketchParseResult ParseNumberedHatch(string token, int lineNumber, BastionSketchRoom room)
        {
            string raw = token.Substring(2);
            if (raw.Length == 0)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": empty hatch id in '" + token + "'");
            if (raw.IndexOf(':') >= 0)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid hatch id '" + token + "'");
            if (!TryParseHatchId(raw, out string id))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid hatch id '" + token + "'");

            room.Marks.Add(new BastionSketchMark { Kind = BastionSketchMarkKind.Hatch, RefId = id });
            return BastionSketchParseResult.Ok(null);
        }

        static bool TryParseHatchId(string text, out string id)
        {
            id = null;
            if (text == null || text.Length == 0)
                return false;
            if (text.Length > 1 && text[0] == '0')
                return false;

            int value = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < '0' || c > '9')
                    return false;
                value = value * 10 + (c - '0');
            }

            for (int i = 0; i < ShipHatches.Count; i++)
            {
                if (ShipHatches.IdOf(i) == value)
                {
                    id = text;
                    return true;
                }
            }

            return false;
        }

        static BastionSketchParseResult ParseSide(string[] tokens, int lineNumber, BastionSketch sketch)
        {
            if (tokens.Length < 4)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": side requires roomId, direction, kind");

            if (!TryParseDirection(tokens[2], out BastionSketchDirection direction))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid direction '" + tokens[2] + "'");
            if (!TryParseSideKind(tokens[3], out BastionSideKind kind))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid side kind '" + tokens[3] + "'");

            bool locked = false;
            float along = 0f;
            for (int i = 4; i < tokens.Length; i++)
            {
                if (string.Equals(tokens[i], "locked", StringComparison.OrdinalIgnoreCase))
                {
                    locked = true;
                }
                else if (IsAlongToken(tokens[i]))
                {
                    if (!TryParseAlong(tokens[i], out along))
                        return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid along '" + tokens[i] + "'");
                }
                else
                {
                    return BastionSketchParseResult.Fail("line " + lineNumber + ": unknown side flag '" + tokens[i] + "'");
                }
            }

            sketch.Sides.Add(new BastionSketchSideOverride
            {
                RoomId = tokens[1],
                Direction = direction,
                Kind = kind,
                Locked = locked,
                Along = along,
            });
            return BastionSketchParseResult.Ok(sketch);
        }

        static BastionSketchParseResult ParseDoor(
            string[] tokens,
            int lineNumber,
            HashSet<string> doorIds,
            BastionSketch sketch)
        {
            if (tokens.Length < 4)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": door requires id, roomId, direction");

            string doorId = tokens[1];
            if (!doorIds.Add(doorId))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": duplicate door id '" + doorId + "'");

            if (!TryParseDirection(tokens[3], out BastionSketchDirection direction))
                return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid direction '" + tokens[3] + "'");

            var door = new BastionSketchDoor
            {
                DoorId = doorId,
                RoomId = tokens[2],
                Direction = direction,
            };

            for (int i = 4; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (string.Equals(token, "control", StringComparison.OrdinalIgnoreCase))
                {
                    door.Control = true;
                    continue;
                }

                if (IsAlongToken(token))
                {
                    if (!TryParseAlong(token, out float doorAlong))
                        return BastionSketchParseResult.Fail("line " + lineNumber + ": invalid along '" + token + "'");
                    door.Along = doorAlong;
                    continue;
                }

                if (token.StartsWith("unlock=", StringComparison.OrdinalIgnoreCase))
                {
                    string list = token.Substring("unlock=".Length);
                    if (list.Length == 0)
                        return BastionSketchParseResult.Fail("line " + lineNumber + ": empty unlock list");

                    string[] parts = list.Split(',');
                    for (int p = 0; p < parts.Length; p++)
                    {
                        string termId = parts[p].Trim();
                        if (termId.Length == 0)
                            return BastionSketchParseResult.Fail("line " + lineNumber + ": empty terminal id in unlock list");
                        door.UnlockTerminalIds.Add(termId);
                    }
                    continue;
                }

                return BastionSketchParseResult.Fail("line " + lineNumber + ": unknown door token '" + token + "'");
            }

            if (door.UnlockTerminalIds.Count == 0)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": door requires unlock=terminals");

            sketch.Doors.Add(door);

            // Door line implies that side is Door and starts locked.
            sketch.Sides.Add(new BastionSketchSideOverride
            {
                RoomId = door.RoomId,
                Direction = door.Direction,
                Kind = BastionSideKind.Door,
                Locked = true,
                Along = door.Along,
            });

            return BastionSketchParseResult.Ok(sketch);
        }

        static BastionSketchParseResult Validate(BastionSketch sketch)
        {
            if (sketch.Rooms.Count == 0)
                return BastionSketchParseResult.Fail("sketch has no rooms");

            int masterCount = 0;
            int controlRoomCount = 0;
            var terminalIds = new HashSet<string>();

            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = sketch.Rooms[i];
                if (room.Kind == BastionBlockKind.ControlRoom)
                    controlRoomCount++;

                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind == BastionSketchMarkKind.Master)
                        masterCount++;
                    else if (mark.Kind == BastionSketchMarkKind.Terminal)
                        terminalIds.Add(mark.RefId);
                }
            }

            BastionSketchParseResult hatches = ValidateHatches(sketch);
            if (!hatches.Success)
                return hatches;

            if (masterCount != 1)
                return BastionSketchParseResult.Fail("sketch must have exactly one M mark (found " + masterCount + ")");

            if (controlRoomCount != 1)
                return BastionSketchParseResult.Fail("sketch must have exactly one ControlRoom (found " + controlRoomCount + ")");

            int controlDoorCount = 0;
            var roomIds = new HashSet<string>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                roomIds.Add(sketch.Rooms[i].Id);

            for (int i = 0; i < sketch.Sides.Count; i++)
            {
                if (!roomIds.Contains(sketch.Sides[i].RoomId))
                {
                    return BastionSketchParseResult.Fail(
                        "side references unknown room '" + sketch.Sides[i].RoomId + "'");
                }
            }

            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                if (!roomIds.Contains(door.RoomId))
                {
                    return BastionSketchParseResult.Fail(
                        "door '" + door.DoorId + "' references unknown room '" + door.RoomId + "'");
                }

                if (door.Control)
                    controlDoorCount++;

                for (int t = 0; t < door.UnlockTerminalIds.Count; t++)
                {
                    string termId = door.UnlockTerminalIds[t];
                    if (!terminalIds.Contains(termId))
                    {
                        return BastionSketchParseResult.Fail(
                            "door '" + door.DoorId + "' references unknown terminal '" + termId + "'");
                    }
                }
            }

            if (controlDoorCount != 1)
            {
                return BastionSketchParseResult.Fail(
                    "sketch must have exactly one door with control flag (found " + controlDoorCount + ")");
            }

            BastionSketchParseResult spawnEdge = ValidateSpawnEdges(sketch);
            if (!spawnEdge.Success)
                return spawnEdge;

            BastionSketchParseResult stairs = ValidateStairs(sketch);
            if (!stairs.Success)
                return stairs;

            return BastionSketchParseResult.Ok(sketch);
        }

        static BastionSketchParseResult ValidateHatches(BastionSketch sketch)
        {
            int hatchCount = 0;
            int bareCount = 0;
            int numberedCount = 0;
            string hatchRoomId = null;
            bool multipleRooms = false;
            var numberedIds = new HashSet<string>();
            bool duplicateId = false;

            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind != BastionSketchMarkKind.Hatch)
                        continue;

                    hatchCount++;
                    if (string.IsNullOrEmpty(mark.RefId))
                    {
                        bareCount++;
                    }
                    else
                    {
                        numberedCount++;
                        if (!numberedIds.Add(mark.RefId))
                            duplicateId = true;
                    }

                    if (hatchRoomId == null)
                        hatchRoomId = room.Id;
                    else if (hatchRoomId != room.Id)
                        multipleRooms = true;
                }
            }

            if (hatchCount == 1 && bareCount == 1)
                return BastionSketchParseResult.Ok(sketch);

            if (bareCount > 0 && numberedCount > 0)
            {
                return BastionSketchParseResult.Fail(
                    "sketch must not mix a bare H mark with numbered hatch marks");
            }

            if (duplicateId)
                return BastionSketchParseResult.Fail("sketch has a duplicate H mark id");

            if (hatchCount == 1 && numberedCount == 1)
                return BastionSketchParseResult.Fail("sketch with one H mark must be a bare H");

            if (hatchCount == ShipHatches.Count && numberedCount == ShipHatches.Count)
            {
                if (multipleRooms)
                    return BastionSketchParseResult.Fail("numbered H marks must all be on the same room");

                bool complete = true;
                for (int i = 0; i < ShipHatches.Count; i++)
                {
                    string id = ShipHatches.IdOf(i).ToString(CultureInfo.InvariantCulture);
                    if (!numberedIds.Contains(id))
                        complete = false;
                }

                if (complete)
                    return BastionSketchParseResult.Ok(sketch);

                return BastionSketchParseResult.Fail(
                    "numbered H marks must be ids 1, 2 and 3");
            }

            return BastionSketchParseResult.Fail(
                "sketch must have exactly one H mark (found " + hatchCount + ")");
        }

        static BastionSketchParseResult ValidateSpawnEdges(BastionSketch sketch)
        {
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind != BastionSketchMarkKind.Spawn)
                        continue;
                    if (!mark.HasDirection)
                    {
                        return BastionSketchParseResult.Fail(
                            "spawn '" + mark.RefId + "' on " + room.Id + " is missing an edge");
                    }

                    for (int d = 0; d < sketch.Doors.Count; d++)
                    {
                        BastionSketchDoor door = sketch.Doors[d];
                        if (door.RoomId == room.Id && door.Direction == mark.Direction)
                        {
                            return BastionSketchParseResult.Fail(
                                "spawn '" + mark.RefId + "' on " + room.Id + " "
                                + mark.Direction + " is a progression door");
                        }
                    }

                    if (BastionSketchPassages.EdgeIsPaired(sketch, room.Id, mark.Direction))
                    {
                        return BastionSketchParseResult.Fail(
                            "spawn '" + mark.RefId + "' on " + room.Id + " "
                            + mark.Direction + " is a passage");
                    }
                }
            }

            return BastionSketchParseResult.Ok(sketch);
        }

        static BastionSketchParseResult ParseStair(string[] tokens, int lineNumber, BastionSketch sketch)
        {
            if (tokens.Length != 3)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": stair requires two room ids");

            string a = tokens[1];
            string b = tokens[2];
            if (a == b)
                return BastionSketchParseResult.Fail("line " + lineNumber + ": stair cannot join a room to itself");

            for (int i = 0; i < sketch.Stairs.Count; i++)
            {
                BastionSketchStair existing = sketch.Stairs[i];
                bool same = (existing.LowerId == a && existing.UpperId == b)
                    || (existing.LowerId == b && existing.UpperId == a);
                if (same)
                    return BastionSketchParseResult.Fail("line " + lineNumber + ": duplicate stair");
            }

            sketch.Stairs.Add(new BastionSketchStair { LowerId = a, UpperId = b });
            return BastionSketchParseResult.Ok(sketch);
        }

        static BastionSketchParseResult ValidateStairs(BastionSketch sketch)
        {
            var byId = new Dictionary<string, BastionSketchRoom>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                byId[sketch.Rooms[i].Id] = sketch.Rooms[i];

            for (int s = 0; s < sketch.Stairs.Count; s++)
            {
                BastionSketchStair stair = sketch.Stairs[s];
                if (!byId.TryGetValue(stair.LowerId, out BastionSketchRoom a))
                    return BastionSketchParseResult.Fail("stair references unknown room '" + stair.LowerId + "'");
                if (!byId.TryGetValue(stair.UpperId, out BastionSketchRoom b))
                    return BastionSketchParseResult.Fail("stair references unknown room '" + stair.UpperId + "'");

                int diff = a.Storey - b.Storey;
                if (diff < 0)
                    diff = -diff;
                if (diff != 1)
                {
                    return BastionSketchParseResult.Fail(
                        "stair " + a.Id + "-" + b.Id + " must join adjacent storeys");
                }

                if (!FootprintsOverlap(a, b))
                {
                    return BastionSketchParseResult.Fail(
                        "stair " + a.Id + "-" + b.Id + " rooms do not overlap in XZ");
                }

                if (a.Storey < b.Storey)
                {
                    stair.LowerId = a.Id;
                    stair.UpperId = b.Id;
                }
                else
                {
                    stair.LowerId = b.Id;
                    stair.UpperId = a.Id;
                }

                sketch.Stairs[s] = stair;
            }

            return BastionSketchParseResult.Ok(sketch);
        }

        static bool FootprintsOverlap(BastionSketchRoom a, BastionSketchRoom b)
        {
            float dx = a.CenterX - b.CenterX;
            if (dx < 0f)
                dx = -dx;
            float dz = a.CenterZ - b.CenterZ;
            if (dz < 0f)
                dz = -dz;
            float maxX = (BastionKit.FootprintWidth(a.Kind) + BastionKit.FootprintWidth(b.Kind)) * 0.5f;
            float maxZ = (BastionKit.FootprintDepth(a.Kind) + BastionKit.FootprintDepth(b.Kind)) * 0.5f;
            return dx < maxX && dz < maxZ;
        }

        static string[] SplitTokens(string line)
        {
            var tokens = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && char.IsWhiteSpace(line[i]))
                    i++;
                if (i >= line.Length)
                    break;

                int start = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]))
                    i++;
                tokens.Add(line.Substring(start, i - start));
            }

            return tokens.ToArray();
        }

        static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static bool IsAlongToken(string token)
        {
            return token.StartsWith("along=", StringComparison.OrdinalIgnoreCase);
        }

        static bool TryParseAlong(string token, out float along)
        {
            return TryParseFloat(token.Substring("along=".Length), out along);
        }

        static bool TryParseStorey(string token, out int storey)
        {
            storey = 0;
            string raw = token.Substring("storey=".Length);
            if (raw.Length == 0)
                return false;
            if (raw[0] == '-')
                return false;
            int value = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c < '0' || c > '9')
                    return false;
                value = value * 10 + (c - '0');
            }

            storey = value;
            return true;
        }

        static bool TryParseMinSeats(string text, out int minSeats)
        {
            minSeats = 1;
            if (text == null || text.Length == 0)
                return false;
            int value = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < '0' || c > '9')
                    return false;
                value = value * 10 + (c - '0');
            }

            if (value < 1 || value > ShipHatches.Count)
                return false;
            minSeats = value;
            return true;
        }

        static bool TryParseBlockKind(string text, out BastionBlockKind kind)
        {
            if (Enum.TryParse(text, true, out kind))
                return true;

            kind = default;
            return false;
        }

        static bool TryParseDirection(string text, out BastionSketchDirection direction)
        {
            switch (text.ToUpperInvariant())
            {
                case "N": direction = BastionSketchDirection.North; return true;
                case "E": direction = BastionSketchDirection.East; return true;
                case "S": direction = BastionSketchDirection.South; return true;
                case "W": direction = BastionSketchDirection.West; return true;
                default: direction = default; return false;
            }
        }

        static bool TryParseSideKind(string text, out BastionSideKind kind)
        {
            if (Enum.TryParse(text, true, out kind))
                return true;

            kind = default;
            return false;
        }
    }
}
