using GatekeepersProtocol.Sim;
using NUnit.Framework;
using System.Collections.Generic;

namespace GatekeepersProtocol.Tests.EditMode.Bastion
{
    /// <summary>Passage pairing: paired Door sides become one auto door, apertures claimed by
    /// a sketch door do not, and alignment via along= is what makes a pair a pair. Unit cases
    /// build the sketch directly — the parser's layout invariants (one hatch, one master,
    /// one control door) are a different contract, covered by the full-layout mirror below.</summary>
    [TestFixture]
    public sealed class BastionSketchPassageTests
    {
        static BastionSketchRoom AddRoom(BastionSketch sketch, string id, BastionBlockKind kind, float x, float z)
        {
            var room = new BastionSketchRoom { Id = id, Kind = kind, CenterX = x, CenterZ = z };
            sketch.Rooms.Add(room);
            return room;
        }

        static void AddDoorSide(BastionSketch sketch, string roomId, BastionSketchDirection dir, float along = 0f)
        {
            sketch.Sides.Add(new BastionSketchSideOverride
            {
                RoomId = roomId,
                Direction = dir,
                Kind = BastionSideKind.Door,
                Along = along,
            });
        }

        [Test]
        public void PairedDoorSides_YieldOnePassage_OwnedBySketchOrder()
        {
            // Full is 26 m at scaler 2: centres 26 apart abut exactly.
            var sketch = new BastionSketch();
            AddRoom(sketch, "A", BastionBlockKind.Full, 0f, 0f);
            AddRoom(sketch, "B", BastionBlockKind.Full, 26f, 0f);
            AddDoorSide(sketch, "A", BastionSketchDirection.East);
            AddDoorSide(sketch, "B", BastionSketchDirection.West);

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            Assert.AreEqual(1, passages.Count);
            Assert.AreEqual("P1", passages[0].Id);
            Assert.AreEqual("A", passages[0].RoomId);
            Assert.AreEqual(BastionSketchDirection.East, passages[0].Direction);
        }

        [Test]
        public void DoorClaimedAperture_YieldsNoPassage()
        {
            var sketch = new BastionSketch();
            AddRoom(sketch, "A", BastionBlockKind.Full, 0f, 0f);
            AddRoom(sketch, "B", BastionBlockKind.Full, 0f, 26f);
            AddDoorSide(sketch, "B", BastionSketchDirection.South);
            var door = new BastionSketchDoor
            {
                DoorId = "D1",
                RoomId = "A",
                Direction = BastionSketchDirection.North,
            };
            door.UnlockTerminalIds.Add("T1");
            sketch.Doors.Add(door);

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            Assert.AreEqual(0, passages.Count);
        }

        [Test]
        public void MisalignedApertures_DoNotPair()
        {
            // B's aperture sits 5 m off A's: same shared edge, different opening centres.
            var sketch = new BastionSketch();
            AddRoom(sketch, "A", BastionBlockKind.Full, 0f, 0f);
            AddRoom(sketch, "B", BastionBlockKind.Full, 26f, 0f);
            AddDoorSide(sketch, "A", BastionSketchDirection.East);
            AddDoorSide(sketch, "B", BastionSketchDirection.West, along: 5f);

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            Assert.AreEqual(0, passages.Count);
        }

        [Test]
        public void AlongOffsets_PairWhenOpeningsAlign()
        {
            // A is OpenArea (42 m), B is Full (26 m) centred 8 m south of A on the shared
            // edge axis: A carries along=-8 so both openings land at the same point.
            var sketch = new BastionSketch();
            AddRoom(sketch, "A", BastionBlockKind.OpenArea, 0f, 0f);
            AddRoom(sketch, "B", BastionBlockKind.Full, -34f, -8f);
            AddDoorSide(sketch, "A", BastionSketchDirection.West, along: -8f);
            AddDoorSide(sketch, "B", BastionSketchDirection.East);

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            Assert.AreEqual(1, passages.Count);
            Assert.AreEqual("A", passages[0].RoomId);
            Assert.AreEqual(-8f, passages[0].Along);
        }

        [Test]
        public void DefenseLayout_ElevenPassages_ThreeGates()
        {
            // Mirror of Tools/bastion-sketch/bastion-defense-layout.txt: 11 open passages
            // (14 paired apertures minus the 3 claimed by D1-D3). A regression here means
            // the sketch and the pairing disagree.
            BastionSketchParseResult result = BastionSketchParser.Parse(
                "room R1 OpenArea 0 -40 H\n" +
                "room R2 Full -34 -40 T:T1\n" +
                "room R3 Full 34 -40 T:T2\n" +
                "room R4 Half -34 -19 S:S1:W\n" +
                "room R5 Half 34 -19 S:S2:E\n" +
                "room R6 OpenArea 0 2\n" +
                "room R7 Half -29 2 S:S3:W\n" +
                "room R8 Half 29 2 S:S4:E\n" +
                "room R9 Full 0 36 T:T3\n" +
                "room R10 OpenArea 0 70\n" +
                "room R11 Full -34 62 T:T4\n" +
                "room R12 Full 34 62 T:T5\n" +
                "room R13 Half -29 83 S:S5:N\n" +
                "room R14 Half 29 83 S:S6:N\n" +
                "room R15 ControlRoom 0 129 M S:ControlW:W S:ControlE:E\n" +
                "side R2 E Door\nside R1 W Door\n" +
                "side R3 W Door\nside R1 E Door\n" +
                "side R4 S Door\nside R2 N Door\n" +
                "side R5 S Door\nside R3 N Door\n" +
                "side R6 S Door\n" +
                "side R7 E Door\nside R6 W Door\n" +
                "side R8 W Door\nside R6 E Door\n" +
                "side R6 N Door\nside R9 S Door\n" +
                "side R10 S Door\n" +
                "side R11 E Door\nside R10 W Door along=-8\n" +
                "side R12 W Door\nside R10 E Door along=-8\n" +
                "side R13 S Door\nside R11 N Door along=5\n" +
                "side R14 S Door\nside R12 N Door along=-5\n" +
                "side R10 N Door\n" +
                "side R1 S Window\n" +
                "door D1 R1 N unlock=T1,T2\n" +
                "door D2 R9 N unlock=T3\n" +
                "door D3 R15 S unlock=T4,T5 control\n");
            Assert.IsTrue(result.Success, result.Error);

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(result.Sketch);
            Assert.AreEqual(11, passages.Count);
            var ids = new HashSet<string>();
            for (int i = 0; i < passages.Count; i++)
                ids.Add(passages[i].Id);
            Assert.AreEqual(11, ids.Count, "passage ids must be unique");
        }
    }
}
