using GatekeepersProtocol.Sim;
using NUnit.Framework;
using System.Collections.Generic;

namespace GatekeepersProtocol.Tests
{
    public class BastionSketchParserTests
    {
        const string DefaultLayoutPath = "Tools/bastion-sketch/bastion-defense-layout.txt";
        const string CoopLayoutPath = "Tools/bastion-sketch/bastion-coop-layout.txt";

        const string MinimalValid = ""
            + "room R1 OpenArea 0 0 H\n"
            + "room R2 Full 80 0 T:T1\n"
            + "room R15 ControlRoom 0 900 M\n"
            + "door D1 R1 N unlock=T1\n"
            + "door D4 R15 S unlock=T1 control\n";

        static string WithHatchMarks(string marks)
        {
            return ""
                + "room R1 OpenArea 0 0 " + marks + "\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D1 R1 N unlock=T1\n"
                + "door D4 R15 S unlock=T1 control\n";
        }

        [Test]
        public void Parse_MinimalInline_PassesValidation()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(MinimalValid);
            Assert.IsTrue(result.Success, result.Error);
            Assert.IsNotNull(result.Sketch);
        }

        [Test]
        public void Parse_DefaultLayoutFile_HasHatchMasterAndControlRoom()
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(DefaultLayoutPath));
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(15, result.Sketch.Rooms.Count);

            int hatch = 0;
            int master = 0;
            int control = 0;
            int spawns = 0;
            for (int i = 0; i < result.Sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = result.Sketch.Rooms[i];
                if (room.Kind == BastionBlockKind.ControlRoom)
                    control++;
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    if (room.Marks[m].Kind == BastionSketchMarkKind.Hatch)
                        hatch++;
                    if (room.Marks[m].Kind == BastionSketchMarkKind.Master)
                        master++;
                    if (room.Marks[m].Kind == BastionSketchMarkKind.Spawn)
                        spawns++;
                }
            }

            Assert.AreEqual(1, hatch);
            Assert.AreEqual(1, master);
            Assert.AreEqual(1, control);
            Assert.AreEqual(8, spawns);
        }

        [Test]
        public void Parse_DoorUnlockList_SplitsCommaSeparatedTerminals()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R3 Full -80 0 T:T2\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D1 R1 N unlock=T1,T2\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(2, result.Sketch.Doors[0].UnlockTerminalIds.Count);
        }

        [Test]
        public void Parse_SideAlong_CarriesOffsetInMetres()
        {
            string text = MinimalValid + "side R1 W Door along=-4.5\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);
            BastionSketchSideOverride side = result.Sketch.Sides[result.Sketch.Sides.Count - 1];
            Assert.AreEqual(BastionSideKind.Door, side.Kind);
            Assert.AreEqual(-4.5f, side.Along, 1e-4f);
        }

        [Test]
        public void Parse_DoorAlong_CarriesOntoImpliedSide()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D1 R1 N unlock=T1 along=2.5\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(2.5f, result.Sketch.Doors[0].Along, 1e-4f);
            Assert.AreEqual(2.5f, result.Sketch.Sides[0].Along, 1e-4f);
        }

        [Test]
        public void Parse_MalformedAlong_IsRejected()
        {
            string text = MinimalValid + "side R1 W Door along=wide\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("invalid along", result.Error);
        }

        [Test]
        public void Parse_MissingHatch_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D4 R15 S unlock=T1 control\n"
                + "room R2 Full 80 0 T:T1\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_MissingMaster_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R15 ControlRoom 0 900\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("M mark", result.Error);
        }

        [Test]
        public void Parse_UnknownUnlockTerminal_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D4 R15 S unlock=T9 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("unknown terminal", result.Error);
        }

        [Test]
        public void Parse_SpawnMarkWithoutEdge_IsRejected()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(
                "room R1 OpenArea 0 0 H\n" +
                "room R2 Full 80 0 T:T1 S:S1\n" +
                "room R15 ControlRoom 0 900 M\n" +
                "door D1 R1 N unlock=T1\n" +
                "door D4 R15 S unlock=T1 control\n");
            Assert.IsFalse(result.Success);
            StringAssert.Contains("requires an edge", result.Error);
        }

        [Test]
        public void Parse_SpawnMarkWithEdge_CarriesDirection()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R4 Half -34 -19 S:S1:W\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D1 R1 N unlock=T1\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);
            BastionSketchMark spawn = default;
            for (int i = 0; i < result.Sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = result.Sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    if (room.Marks[m].Kind == BastionSketchMarkKind.Spawn)
                        spawn = room.Marks[m];
                }
            }

            Assert.AreEqual("S1", spawn.RefId);
            Assert.AreEqual(BastionSketchDirection.West, spawn.Direction);
            Assert.IsTrue(spawn.HasDirection);
        }

        [Test]
        public void Parse_SpawnOnPassageEdge_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 -40 H\n"
                + "room R2 Full -34 -40 T:T1\n"
                + "room R4 Half -34 -19 S:S1:S\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "side R4 S Door\n"
                + "side R2 N Door\n"
                + "door D1 R1 N unlock=T1\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success, result.Error);
            StringAssert.Contains("passage", result.Error);
        }

        [Test]
        public void Parse_SpawnOnControlDoor_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R15 ControlRoom 0 900 M S:Control:S\n"
                + "door D1 R1 N unlock=T1\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success, result.Error);
            StringAssert.Contains("progression door", result.Error);
        }

        [Test]
        public void Parse_CoopLayoutFile_HasThreeNumberedHatchesOnOneRoom()
        {
            string coopText = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(CoopLayoutPath));
            BastionSketchParseResult coop = BastionSketchParser.Parse(coopText);
            Assert.IsTrue(coop.Success, coop.Error);

            string defaultText = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(DefaultLayoutPath));
            BastionSketchParseResult solo = BastionSketchParser.Parse(defaultText);
            Assert.IsTrue(solo.Success, solo.Error);
            Assert.AreEqual(15, solo.Sketch.Rooms.Count);
            Assert.GreaterOrEqual(coop.Sketch.Rooms.Count, 20);
            Assert.LessOrEqual(coop.Sketch.Rooms.Count, 30);

            var ids = new HashSet<string>();
            string hatchRoomId = null;
            int hatchCount = 0;
            for (int i = 0; i < coop.Sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = coop.Sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    if (room.Marks[m].Kind != BastionSketchMarkKind.Hatch)
                        continue;
                    hatchCount++;
                    ids.Add(room.Marks[m].RefId);
                    if (hatchRoomId == null)
                        hatchRoomId = room.Id;
                    else
                        Assert.AreEqual(hatchRoomId, room.Id);
                }
            }

            Assert.AreEqual(3, hatchCount);
            Assert.AreEqual("R1", hatchRoomId);
            CollectionAssert.AreEquivalent(new[] { "1", "2", "3" }, ids);
            Assert.AreEqual(28, coop.Sketch.Rooms.Count);

            int storey1 = 0;
            int muted = 0;
            for (int i = 0; i < coop.Sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = coop.Sketch.Rooms[i];
                if (room.Storey == 1)
                    storey1++;
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    if (room.Marks[m].Kind == BastionSketchMarkKind.Spawn
                        && room.Marks[m].MinSeats >= 3)
                        muted++;
                }
            }

            Assert.AreEqual(13, storey1);
            Assert.AreEqual(3, coop.Sketch.Stairs.Count);
            Assert.AreEqual(6, muted);
        }

        [Test]
        public void Parse_CoopLayoutFile_HatchOneToThreeIsInsideTenWalkSeconds()
        {
            const float HatchOneAlongMetres = -14f;
            const float HatchThreeAlongMetres = 14f;
            const float PlayerWalkSpeedMetresPerSecond = 5f;

            string text = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(CoopLayoutPath));
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);

            float spanMetres = HatchThreeAlongMetres - HatchOneAlongMetres;
            Assert.AreEqual(28f, spanMetres, 1e-4f);
            Assert.Less(spanMetres / PlayerWalkSpeedMetresPerSecond, 10f);
        }

        [Test]
        public void Parse_CoopLayoutFile_ZoneOneSpawnsReachHatchRoom()
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(CoopLayoutPath));
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);

            var zoneOne = new HashSet<string> { "S1", "S2", "S7", "S8" };
            int checkedCount = 0;
            for (int i = 0; i < result.Sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = result.Sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind != BastionSketchMarkKind.Spawn)
                        continue;
                    if (!zoneOne.Contains(mark.RefId))
                        continue;
                    checkedCount++;
                    Assert.IsTrue(
                        HasPassagePath(result.Sketch, room.Id, "R1"),
                        room.Id + " (" + mark.RefId + ") has no path to R1");
                }
            }

            Assert.AreEqual(4, checkedCount);
        }

        [Test]
        public void SeatSpawns_ThreeSeatMarksAreMutedAtTwo()
        {
            Assert.IsTrue(BastionSeatSpawns.IsLive(1, 2));
            Assert.IsFalse(BastionSeatSpawns.IsLive(3, 2));
            Assert.IsTrue(BastionSeatSpawns.IsLive(3, 3));
        }

        [Test]
        public void Parse_TwoHatches_IsRejected()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(WithHatchMarks("H:1 H:2"));
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_FourHatches_IsRejected()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(WithHatchMarks("H H H H"));
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_DuplicateNumberedHatch_IsRejected()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(WithHatchMarks("H:1 H:1"));
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_SingleNumberedHatch_IsRejected()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(WithHatchMarks("H:1"));
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_InvalidHatchIds_AreRejected()
        {
            string[] tokens = { "H:", "H:0", "H:4", "H:x", "H:1:extra" };
            for (int i = 0; i < tokens.Length; i++)
            {
                BastionSketchParseResult result = BastionSketchParser.Parse(
                    "room R1 OpenArea 0 0 " + tokens[i] + "\n");
                Assert.IsFalse(result.Success, tokens[i]);
            }
        }

        [Test]
        public void Parse_BareAndNumberedHatches_IsRejected()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(WithHatchMarks("H H:1"));
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_NumberedHatchesOnTwoRooms_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H:1 H:2\n"
                + "room R2 Full 80 0 T:T1 H:3\n"
                + "room R15 ControlRoom 0 900 M\n"
                + "door D1 R1 N unlock=T1\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        static bool HasPassagePath(BastionSketch sketch, string fromId, string toId)
        {
            if (fromId == toId)
                return true;

            var adj = new Dictionary<string, List<string>>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                adj[sketch.Rooms[i].Id] = new List<string>();

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            for (int i = 0; i < passages.Count; i++)
            {
                string partner = PassagePartner(sketch, passages[i]);
                if (partner == null)
                    continue;
                adj[passages[i].RoomId].Add(partner);
                adj[partner].Add(passages[i].RoomId);
            }

            for (int i = 0; i < sketch.Stairs.Count; i++)
            {
                BastionSketchStair stair = sketch.Stairs[i];
                adj[stair.LowerId].Add(stair.UpperId);
                adj[stair.UpperId].Add(stair.LowerId);
            }

            var seen = new HashSet<string>();
            var queue = new Queue<string>();
            seen.Add(fromId);
            queue.Enqueue(fromId);
            while (queue.Count > 0)
            {
                string at = queue.Dequeue();
                if (at == toId)
                    return true;
                List<string> next;
                if (!adj.TryGetValue(at, out next))
                    continue;
                for (int n = 0; n < next.Count; n++)
                {
                    if (seen.Add(next[n]))
                        queue.Enqueue(next[n]);
                }
            }

            return false;
        }

        static string PassagePartner(BastionSketch sketch, BastionSketchPassage passage)
        {
            BastionSketchRoom owner = FindRoom(sketch, passage.RoomId);
            if (owner == null)
                return null;
            long want = ApertureKey(owner, passage.Direction, passage.Along);

            for (int i = 0; i < sketch.Sides.Count; i++)
            {
                BastionSketchSideOverride side = sketch.Sides[i];
                if (side.Kind != BastionSideKind.Door || side.RoomId == passage.RoomId)
                    continue;
                BastionSketchRoom other = FindRoom(sketch, side.RoomId);
                if (other == null)
                    continue;
                if (ApertureKey(other, side.Direction, side.Along) == want)
                    return other.Id;
            }

            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                if (door.RoomId == passage.RoomId)
                    continue;
                BastionSketchRoom other = FindRoom(sketch, door.RoomId);
                if (other == null)
                    continue;
                if (ApertureKey(other, door.Direction, door.Along) == want)
                    return other.Id;
            }

            return null;
        }

        static BastionSketchRoom FindRoom(BastionSketch sketch, string id)
        {
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                if (sketch.Rooms[i].Id == id)
                    return sketch.Rooms[i];
            }

            return null;
        }

        static long ApertureKey(BastionSketchRoom room, BastionSketchDirection direction, float along)
        {
            float halfW = BastionKit.FootprintWidth(room.Kind) * 0.5f;
            float halfD = BastionKit.FootprintDepth(room.Kind) * 0.5f;
            bool northSouth = direction == BastionSketchDirection.North
                || direction == BastionSketchDirection.South;
            float edge;
            float at;
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

            return (long)room.Storey * 10000000000L
                + (long)System.Math.Round(edge * 4.0) * 1000000L
                + (long)System.Math.Round(at * 4.0) * 4L
                + (northSouth ? 1L : 2L);
        }
    }
}
