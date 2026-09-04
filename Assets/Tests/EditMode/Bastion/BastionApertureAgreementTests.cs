using System.Collections.Generic;
using System.IO;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The two ends of a shared passage must agree on how wide it is.
    ///
    /// Every room builds its own walls knowing nothing about its neighbour, so the aperture
    /// width is resolved twice, once per side, from that room's own footprint. While the
    /// authored width was the binding term everywhere the two answers matched by accident.
    /// Raise it past the tightest block's share cap and they stop matching: an OpenArea would
    /// cut a 10 m mouth and the Half room facing it a 6.4 m one, and nothing in the geometry
    /// would notice. That is the failure the 2026-09-04 widening had to clear, and this is the
    /// gate on it.
    ///
    /// It reads the real sketch rather than a copy, so it follows the level. A room kind added
    /// to the layout is checked the moment it is authored.</summary>
    public class BastionApertureAgreementTests
    {
        const string LayoutPath = "Tools/bastion-sketch/bastion-defense-layout.txt";

        /// <summary>Kept in step with BastionSegment's constants, which live in the Game layer
        /// and cannot be referenced from the offline harness. The pin that they still agree is
        /// <c>BastionDoorApertureTests</c>, which runs inside Unity and derives from the real
        /// fields.</summary>
        const float AuthoredWidth = 10f;
        const float MaxShare = 0.65f;

        static List<BastionBlockKind> KindsInTheLayout()
        {
            string path = Path.GetFullPath(LayoutPath);
            Assert.IsTrue(File.Exists(path), "no layout at " + LayoutPath);
            BastionSketchParseResult parsed = BastionSketchParser.Parse(File.ReadAllText(path));
            Assert.IsNotNull(parsed.Sketch, "the shipped layout does not parse");
            var kinds = new List<BastionBlockKind>();
            foreach (BastionSketchRoom room in parsed.Sketch.Rooms)
                if (!kinds.Contains(room.Kind)) kinds.Add(room.Kind);
            Assert.Greater(kinds.Count, 1, "a layout of one room kind cannot disagree");
            return kinds;
        }

        [Test]
        public void EveryBlockKindTheLayoutUses_ResolvesTheSameAperture()
        {
            List<BastionBlockKind> kinds = KindsInTheLayout();
            float first = BastionKit.ApertureWidth(kinds[0], AuthoredWidth, MaxShare);
            Assert.AreEqual(AuthoredWidth, first, 1e-3f,
                "the authored width should be what survives on every block the Bastion uses");
            for (int i = 1; i < kinds.Count; i++)
            {
                float w = BastionKit.ApertureWidth(kinds[i], AuthoredWidth, MaxShare);
                Assert.AreEqual(first, w, 1e-3f,
                    kinds[i] + " cuts a " + w + " m mouth where " + kinds[0] + " cuts " + first
                    + " m — a passage between them would have two different widths");
            }
        }

        [Test]
        public void TheTightestBlock_StillHasWallLeftToBuildPillarsFrom()
        {
            List<BastionBlockKind> kinds = KindsInTheLayout();
            foreach (BastionBlockKind kind in kinds)
            {
                float width = BastionKit.FootprintWidth(kind);
                float depth = BastionKit.FootprintDepth(kind);
                float shortSide = width < depth ? width : depth;
                float opening = BastionKit.ApertureWidth(kind, AuthoredWidth, MaxShare);
                Assert.Less(opening, shortSide, kind + ": the opening is the whole wall");
                // TryDoorPillars needs span > opening on BOTH axes a door may sit on, or the
                // wall is emitted with no pillar and the room loses a corner.
                Assert.Less(opening, width, kind + ": no pillar left on the X span");
                Assert.Less(opening, depth, kind + ": no pillar left on the Z span");
            }
        }

        /// <summary>The margin is thin by design and this names it, so the next widening is a
        /// deliberate act rather than a silent disagreement.</summary>
        [Test]
        public void RaisingTheWidthPastTheTightestCap_IsWhatBreaksTheAgreement()
        {
            List<BastionBlockKind> kinds = KindsInTheLayout();
            float tightestCap = float.MaxValue;
            foreach (BastionBlockKind kind in kinds)
            {
                float width = BastionKit.FootprintWidth(kind);
                float depth = BastionKit.FootprintDepth(kind);
                float shortSide = width < depth ? width : depth;
                float cap = shortSide * MaxShare;
                if (cap < tightestCap) tightestCap = cap;
            }
            Assert.GreaterOrEqual(tightestCap, AuthoredWidth,
                "the authored width already exceeds the tightest block's cap");

            // One centimetre past it and two kinds disagree. This is the guard failing on
            // purpose, which is the proof the guard can fail at all.
            float overWide = tightestCap + 0.01f;
            var seen = new List<float>();
            foreach (BastionBlockKind kind in kinds)
            {
                float w = BastionKit.ApertureWidth(kind, overWide, MaxShare);
                if (!seen.Contains(w)) seen.Add(w);
            }
            Assert.Greater(seen.Count, 1,
                "past the tightest cap the kinds must disagree, or this test proves nothing");
        }

        /// <summary>The stamp that decides whether a segment rebuilds must move when a shared
        /// number moves. It did not, and that is why the played Bastion carried 3 m doorways
        /// with lintels for a week after the code said 6 m and full height.</summary>
        [Test]
        public void TheGeometryStamp_MovesWithEveryNumberItCovers()
        {
            int baseline = BastionKit.GeometryStamp(10f, 0.65f, 4.5f);
            Assert.AreNotEqual(0, baseline, "a zero stamp reads as never-built");

            Assert.AreNotEqual(baseline, BastionKit.GeometryStamp(6f, 0.65f, 4.5f),
                "a doorway width change must invalidate the stamp");
            Assert.AreNotEqual(baseline, BastionKit.GeometryStamp(10f, 0.4f, 4.5f),
                "a share-cap change must invalidate the stamp");
            Assert.AreNotEqual(baseline, BastionKit.GeometryStamp(10f, 0.65f, 3.5f),
                "a doorway height change must invalidate the stamp");

            Assert.AreEqual(baseline, BastionKit.GeometryStamp(10f, 0.65f, 4.5f),
                "the same inputs must give the same stamp, or every load rebuilds");
        }
    }
}
