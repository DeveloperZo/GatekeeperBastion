using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Mission contiguity, proven against the committed sketch documents rather
    /// than a copy: the run starts in the hatch room with at least one terminal reachable
    /// before any gate opens, every locked door's terminals are reachable before that door,
    /// every gate actually gates (opening it grows the reachable set — a gallery or stair
    /// that bypasses one fails here), the control room opens last behind the control door,
    /// and with everything open the control room and every spawn closet can reach the hatch
    /// room, where the final hatch wave is fought. The scene-binding tests then pin that the
    /// emitted scenes carry every terminal and door the sketch declares, with the sketch's
    /// unlock wiring.</summary>
    public class BastionMissionProgressionTests
    {
        const string DefenseSketch = "Tools/bastion-sketch/bastion-defense-layout.txt";
        const string CoopSketch = "Tools/bastion-sketch/bastion-coop-layout.txt";
        const string DefenseScene = "Assets/Scenes/Bastion.unity";
        const string CoopScene = "Assets/Scenes/Bastion_Coop.unity";

        [Test]
        public void DefenseLayout_MissionIsContiguous() => AssertMissionContiguity(DefenseSketch);

        [Test]
        public void CoopLayout_MissionIsContiguous() => AssertMissionContiguity(CoopSketch);

        [Test]
        public void DefenseScene_CarriesTheSketchTerminalsAndDoors()
            => AssertSceneBindsSketch(DefenseScene, DefenseSketch);

        [Test]
        public void CoopScene_CarriesTheSketchTerminalsAndDoors()
            => AssertSceneBindsSketch(CoopScene, CoopSketch);

        static void AssertMissionContiguity(string sketchPath)
        {
            BastionSketch sketch = ParseSketch(sketchPath);

            string hatchRoom = null;
            string masterRoom = null;
            string controlRoom = null;
            var terminalRooms = new Dictionary<string, string>();
            var spawnRooms = new List<string>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = sketch.Rooms[i];
                if (room.Kind == BastionBlockKind.ControlRoom)
                    controlRoom = room.Id;
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind == BastionSketchMarkKind.Hatch && hatchRoom == null)
                        hatchRoom = room.Id;
                    else if (mark.Kind == BastionSketchMarkKind.Hatch)
                        Assert.AreEqual(hatchRoom, room.Id,
                            "hatches span rooms " + hatchRoom + " and " + room.Id
                            + " — the run has no single start room");
                    if (mark.Kind == BastionSketchMarkKind.Master)
                        masterRoom = room.Id;
                    if (mark.Kind == BastionSketchMarkKind.Terminal)
                        terminalRooms[mark.RefId] = room.Id;
                    if (mark.Kind == BastionSketchMarkKind.Spawn)
                        spawnRooms.Add(room.Id);
                }
            }
            Assert.IsNotNull(hatchRoom, sketchPath + " has no hatch room to start in");
            Assert.IsNotNull(controlRoom, sketchPath + " has no ControlRoom");
            Assert.AreEqual(controlRoom, masterRoom,
                "the M mark must sit in the ControlRoom — the mission ends at master control");

            Dictionary<string, List<string>> open = BuildOpenEdges(sketch);
            var lockedEdges = new List<LockedEdge>();
            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                string partner = PartnerAcross(sketch, door.RoomId, door.Direction, door.Along);
                Assert.IsNotNull(partner,
                    "door " + door.DoorId + " on " + door.RoomId + " " + door.Direction
                    + " has no abutting room on the far side");
                lockedEdges.Add(new LockedEdge
                {
                    Door = door,
                    RoomA = door.RoomId,
                    RoomB = partner,
                });
            }

            // Stage 0: every gate shut. The level must offer a terminal from the start.
            HashSet<string> reach = Flood(hatchRoom, open, lockedEdges, openDoors: new HashSet<string>());
            var startTerminals = new List<string>();
            foreach (KeyValuePair<string, string> t in terminalRooms)
            {
                if (reach.Contains(t.Value))
                    startTerminals.Add(t.Key);
            }
            Assert.Greater(startTerminals.Count, 0,
                sketchPath + ": no terminal is reachable from " + hatchRoom
                + " while every gate is shut — the run cannot begin");

            // Unlock fixpoint: a door opens once its terminals are reachable, and each
            // opening must expose new rooms — a bypass makes a gate decorative.
            var openDoors = new HashSet<string>();
            bool progressed = true;
            while (progressed)
            {
                progressed = false;
                for (int i = 0; i < lockedEdges.Count; i++)
                {
                    BastionSketchDoor door = lockedEdges[i].Door;
                    if (openDoors.Contains(door.DoorId))
                        continue;
                    bool eligible = true;
                    for (int t = 0; t < door.UnlockTerminalIds.Count; t++)
                    {
                        if (!reach.Contains(terminalRooms[door.UnlockTerminalIds[t]]))
                        {
                            eligible = false;
                            break;
                        }
                    }
                    if (!eligible)
                        continue;

                    if (door.Control)
                        Assert.IsFalse(reach.Contains(controlRoom),
                            "the control room is reachable before " + door.DoorId
                            + " opens — the control gate is bypassed");

                    openDoors.Add(door.DoorId);
                    HashSet<string> grown = Flood(hatchRoom, open, lockedEdges, openDoors);
                    Assert.Greater(grown.Count, reach.Count,
                        "opening " + door.DoorId + " exposed no new room — the gate is "
                        + "bypassed by another route");
                    reach = grown;
                    progressed = true;
                }
            }

            for (int i = 0; i < lockedEdges.Count; i++)
            {
                Assert.IsTrue(openDoors.Contains(lockedEdges[i].Door.DoorId),
                    lockedEdges[i].Door.DoorId + " can never open: a required terminal sits "
                    + "behind it or behind a later gate — progression deadlocks");
            }
            Assert.IsTrue(reach.Contains(controlRoom),
                "every door is open yet " + controlRoom + " is unreachable");

            // The final hatch wave is fought back at the start: with the level fully open,
            // the control room and every spawn closet must reach the hatch room.
            HashSet<string> fromControl = Flood(controlRoom, open, lockedEdges, openDoors);
            Assert.IsTrue(fromControl.Contains(hatchRoom),
                "no route back from " + controlRoom + " to the hatch room " + hatchRoom);
            for (int i = 0; i < spawnRooms.Count; i++)
            {
                Assert.IsTrue(reach.Contains(spawnRooms[i]),
                    "spawn closet host " + spawnRooms[i] + " cannot reach the hatch room "
                    + hatchRoom + " even with every door open");
            }
        }

        static void AssertSceneBindsSketch(string scenePath, string sketchPath)
        {
            BastionSketch sketch = ParseSketch(sketchPath);
            string path = Path.GetFullPath(scenePath);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            string yaml = File.ReadAllText(path).Replace("\r\n", "\n");

            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                List<BastionSketchMark> marks = sketch.Rooms[i].Marks;
                for (int m = 0; m < marks.Count; m++)
                {
                    if (marks[m].Kind == BastionSketchMarkKind.Terminal)
                        StringAssert.Contains("terminalId: " + marks[m].RefId + "\n", yaml,
                            scenePath + " has no terminal bound as " + marks[m].RefId);
                    if (marks[m].Kind == BastionSketchMarkKind.Master)
                        StringAssert.Contains("terminalId: Master\n", yaml,
                            scenePath + " has no master control terminal");
                }
            }

            int controlDoors = Regex.Matches(yaml, @"^  opensControlRoom: 1$", RegexOptions.Multiline).Count;
            Assert.AreEqual(1, controlDoors,
                scenePath + " must carry exactly one door that opens the control room");

            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                var wiring = new System.Text.StringBuilder();
                wiring.Append("  doorId: ").Append(door.DoorId).Append("\n  requiredTerminalIds:\n");
                for (int t = 0; t < door.UnlockTerminalIds.Count; t++)
                    wiring.Append("  - ").Append(door.UnlockTerminalIds[t]).Append("\n");
                wiring.Append("  startsLocked: 1\n");
                wiring.Append("  opensControlRoom: ").Append(door.Control ? "1" : "0").Append("\n");
                StringAssert.Contains(wiring.ToString(), yaml,
                    scenePath + ": door " + door.DoorId + " is missing or its unlock wiring "
                    + "does not match the sketch (locked, unlock=" +
                    string.Join(",", door.UnlockTerminalIds) + ")");
            }
        }

        static BastionSketch ParseSketch(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            BastionSketchParseResult result = BastionSketchParser.Parse(File.ReadAllText(path));
            Assert.IsTrue(result.Success, relative + " does not parse: " + result.Error);
            return result.Sketch;
        }

        struct LockedEdge
        {
            public BastionSketchDoor Door;
            public string RoomA;
            public string RoomB;
        }

        static Dictionary<string, List<string>> BuildOpenEdges(BastionSketch sketch)
        {
            var edges = new Dictionary<string, List<string>>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                edges[sketch.Rooms[i].Id] = new List<string>();

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            for (int i = 0; i < passages.Count; i++)
            {
                BastionSketchPassage p = passages[i];
                string partner = PartnerAcross(sketch, p.RoomId, p.Direction, p.Along);
                Assert.IsNotNull(partner,
                    "passage " + p.Id + " on " + p.RoomId + " " + p.Direction
                    + " pairs with no room — the adjacency reading disagrees with the pairing");
                edges[p.RoomId].Add(partner);
                edges[partner].Add(p.RoomId);
            }

            for (int i = 0; i < sketch.Stairs.Count; i++)
            {
                BastionSketchStair s = sketch.Stairs[i];
                edges[s.LowerId].Add(s.UpperId);
                edges[s.UpperId].Add(s.LowerId);
            }
            return edges;
        }

        /// <summary>The room whose opposite footprint edge carries the aperture point —
        /// the far side of a passage or gate. Footprints only; no scene needed.</summary>
        static string PartnerAcross(
            BastionSketch sketch,
            string roomId,
            BastionSketchDirection direction,
            float along)
        {
            const float Eps = 0.05f;
            BastionSketchRoom room = null;
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                if (sketch.Rooms[i].Id == roomId)
                    room = sketch.Rooms[i];
            }
            if (room == null)
                return null;

            float halfW = BastionKit.FootprintWidth(room.Kind) * 0.5f;
            float halfD = BastionKit.FootprintDepth(room.Kind) * 0.5f;
            bool northSouth = direction == BastionSketchDirection.North
                || direction == BastionSketchDirection.South;
            float edge = northSouth
                ? room.CenterZ + (direction == BastionSketchDirection.North ? halfD : -halfD)
                : room.CenterX + (direction == BastionSketchDirection.East ? halfW : -halfW);
            float at = northSouth ? room.CenterX + along : room.CenterZ + along;

            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom other = sketch.Rooms[i];
                if (other.Id == roomId || other.Storey != room.Storey)
                    continue;
                float oHalfW = BastionKit.FootprintWidth(other.Kind) * 0.5f;
                float oHalfD = BastionKit.FootprintDepth(other.Kind) * 0.5f;
                if (northSouth)
                {
                    float oEdge = other.CenterZ
                        + (direction == BastionSketchDirection.North ? -oHalfD : oHalfD);
                    if (System.Math.Abs(oEdge - edge) < Eps
                        && System.Math.Abs(at - other.CenterX) <= oHalfW + Eps)
                        return other.Id;
                }
                else
                {
                    float oEdge = other.CenterX
                        + (direction == BastionSketchDirection.East ? -oHalfW : oHalfW);
                    if (System.Math.Abs(oEdge - edge) < Eps
                        && System.Math.Abs(at - other.CenterZ) <= oHalfD + Eps)
                        return other.Id;
                }
            }
            return null;
        }

        static HashSet<string> Flood(
            string start,
            Dictionary<string, List<string>> open,
            List<LockedEdge> lockedEdges,
            HashSet<string> openDoors)
        {
            var reach = new HashSet<string>();
            var queue = new Queue<string>();
            reach.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string room = queue.Dequeue();
                List<string> next = open[room];
                for (int i = 0; i < next.Count; i++)
                {
                    if (reach.Add(next[i]))
                        queue.Enqueue(next[i]);
                }
                for (int i = 0; i < lockedEdges.Count; i++)
                {
                    LockedEdge e = lockedEdges[i];
                    if (!openDoors.Contains(e.Door.DoorId))
                        continue;
                    string across = e.RoomA == room ? e.RoomB : (e.RoomB == room ? e.RoomA : null);
                    if (across != null && reach.Add(across))
                        queue.Enqueue(across);
                }
            }
            return reach;
        }
    }
}
