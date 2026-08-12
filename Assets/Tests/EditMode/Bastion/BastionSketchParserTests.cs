using GatekeeperBastion.Sim;
using NUnit.Framework;

namespace GatekeeperBastion.Tests
{
    public class BastionSketchParserTests
    {
        const string MinimalValid = ""
            + "room R1 OpenArea 0 0 H\n"
            + "room R2 Full 80 0 T:T1\n"
            + "room R15 ControlRoom 0 900\n"
            + "door D1 R1 N unlock=T1\n"
            + "door D4 R15 S unlock=T1 control\n";

        [Test]
        public void Parse_MinimalInline_PassesValidation()
        {
            BastionSketchParseResult result = BastionSketchParser.Parse(MinimalValid);
            Assert.IsTrue(result.Success, result.Error);
            Assert.IsNotNull(result.Sketch);
        }

        [Test]
        public void Parse_ExampleFile_HasOneHatchAndOneControlRoom()
        {
            string text = System.IO.File.ReadAllText(
                System.IO.Path.GetFullPath("Tools/bastion-sketch/example-zo-layout.txt"));
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);

            int hatch = 0;
            int control = 0;
            for (int i = 0; i < result.Sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = result.Sketch.Rooms[i];
                if (room.Kind == BastionBlockKind.ControlRoom)
                    control++;
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    if (room.Marks[m].Kind == BastionSketchMarkKind.Hatch)
                        hatch++;
                }
            }

            Assert.AreEqual(1, hatch);
            Assert.AreEqual(1, control);
        }

        [Test]
        public void Parse_DoorUnlockList_SplitsCommaSeparatedTerminals()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R3 Full -80 0 T:T2\n"
                + "room R15 ControlRoom 0 900\n"
                + "door D1 R1 N unlock=T1,T2\n"
                + "door D4 R15 S unlock=T1 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(2, result.Sketch.Doors[0].UnlockTerminalIds.Count);
        }

        [Test]
        public void Parse_MissingHatch_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0\n"
                + "room R15 ControlRoom 0 900\n"
                + "door D4 R15 S unlock=T1 control\n"
                + "room R2 Full 80 0 T:T1\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("H mark", result.Error);
        }

        [Test]
        public void Parse_UnknownUnlockTerminal_IsRejected()
        {
            string text = ""
                + "room R1 OpenArea 0 0 H\n"
                + "room R2 Full 80 0 T:T1\n"
                + "room R15 ControlRoom 0 900\n"
                + "door D4 R15 S unlock=T9 control\n";
            BastionSketchParseResult result = BastionSketchParser.Parse(text);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("unknown terminal", result.Error);
        }
    }
}
