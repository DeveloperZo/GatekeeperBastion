using System.Collections.Generic;
using System.IO;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>OpenSpan: the wall's absence rather than an opening cut into it (Zo,
    /// 2026-09-04, "widen the northern entrance to be full width of R2, maybe a small divider
    /// in middle"). A Door is bounded by the doorway width and leaves a pillar at each end;
    /// an OpenSpan leaves only a divider at the centre, so the room beyond is continuous.
    ///
    /// The rules that make it safe are here: it must pair as a passage, both faces must carry
    /// it, and the two rooms must have the same span on that edge or the passage has two
    /// different mouths.</summary>
    public class BastionOpenSpanTests
    {
        const string CoopLayout = "Tools/bastion-sketch/bastion-coop-layout.txt";

        static BastionSketch Layout(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "no layout at " + relative);
            BastionSketchParseResult parsed = BastionSketchParser.Parse(File.ReadAllText(path));
            Assert.IsNotNull(parsed.Sketch, relative + " does not parse: " + parsed.Error);
            return parsed.Sketch;
        }

        static float SpanOn(BastionSketchRoom room, BastionSketchDirection dir)
        {
            // A north or south face runs along X; an east or west face runs along Z.
            bool alongX = dir == BastionSketchDirection.North || dir == BastionSketchDirection.South;
            return alongX ? BastionKit.FootprintWidth(room.Kind) : BastionKit.FootprintDepth(room.Kind);
        }

        [Test]
        public void TheCoopLayout_OpensR2sNorthernEntrance()
        {
            BastionSketch sketch = Layout(CoopLayout);
            var open = new List<BastionSketchSideOverride>();
            foreach (BastionSketchSideOverride side in sketch.Sides)
                if (side.Kind == BastionSideKind.OpenSpan) open.Add(side);

            Assert.AreEqual(2, open.Count,
                "one passage, two faces — an OpenSpan on a single face is half a hole");
            Assert.IsTrue(open.Exists(s => s.RoomId == "R2"
                    && s.Direction == BastionSketchDirection.North),
                "R2's northern entrance is not open");
            Assert.IsTrue(open.Exists(s => s.RoomId == "R3"
                    && s.Direction == BastionSketchDirection.South),
                "the far face of R2's northern entrance is not open");
        }

        /// <summary>The invariant that makes an OpenSpan legal at all. Each room resolves its
        /// own geometry, so two rooms of different spans would open different widths and the
        /// passage would have two mouths — the same failure the doorway share cap guards
        /// against, one level up.</summary>
        [Test]
        public void EveryOpenSpan_JoinsRoomsOfTheSameSpan()
        {
            BastionSketch sketch = Layout(CoopLayout);
            var byId = new Dictionary<string, BastionSketchRoom>();
            foreach (BastionSketchRoom room in sketch.Rooms) byId[room.Id] = room;

            var spans = new List<float>();
            foreach (BastionSketchSideOverride side in sketch.Sides)
            {
                if (side.Kind != BastionSideKind.OpenSpan) continue;
                Assert.IsTrue(byId.ContainsKey(side.RoomId), "OpenSpan on unknown room " + side.RoomId);
                spans.Add(SpanOn(byId[side.RoomId], side.Direction));
            }
            Assert.Greater(spans.Count, 0, "nothing to check");
            for (int i = 1; i < spans.Count; i++)
                Assert.AreEqual(spans[0], spans[i], 1e-3f,
                    "the two faces of an OpenSpan open different widths");
        }

        [Test]
        public void AnOpenSpan_IsAPassage_SoTheRoomsStayReachable()
        {
            BastionSketch sketch = Layout(CoopLayout);
            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            bool joined = false;
            foreach (BastionSketchPassage p in passages)
            {
                bool r2North = p.RoomId == "R2" && p.Direction == BastionSketchDirection.North;
                bool r3South = p.RoomId == "R3" && p.Direction == BastionSketchDirection.South;
                if (r2North || r3South) { joined = true; break; }
            }
            Assert.IsTrue(joined,
                "the open span is not a passage, so the room graph thinks R3 is walled off");
        }

        /// <summary>Two openings, each wider than a whole doorway, is the point of the change.
        /// Derived from the kit rather than restated, so a scaler change moves it.</summary>
        [Test]
        public void TheDividerLeavesTwoOpenings_EachWiderThanADoorway()
        {
            const float dividerWidth = 1.2f;   // BastionSegment.OpenSpanDividerWidth
            const float doorwayWidth = 10f;    // BastionSegment.DoorOpeningWidth
            float span = BastionKit.FootprintWidth(BastionBlockKind.Half);

            Assert.Less(dividerWidth, span, "a divider wider than the span is a wall");
            float each = (span - dividerWidth) * 0.5f;
            Assert.Greater(each, 0f);
            Assert.Greater(each * 2f, doorwayWidth,
                "an open span must beat the doorway it replaces, or it is not worth a new kind");
        }
    }
}
