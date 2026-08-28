// Wall light strips: the lit dashes that run along a ship or station wall.
//
// One generator, because these get drawn on every vessel and the whole value of the look is
// that a strip on a station reads as the same fixture as a strip on a ship. The vignette
// gallery and the production look pass both call this; neither owns its own copy.
//
// Two rules make a strip sit ON the wall rather than across it:
//
//   Position comes from `Rows`, a table of places the wall art already has, not from dividing
//   the wall into N. A row carries a tilt as well as a height, because the Synty wall is not
//   one flat plane and a strip only reads as part of it when it lies on the surface it meets.
//
//   Every dash edge lands on a multiple of `Pitch`, the panel rhythm of the 5 m Synty module.
//   The rhythm this replaced was a seeded random walk, which is why its dashes never agreed
//   with the panels behind them at any height.
//
// Deterministic: the same arguments always produce the same mesh, so a station built today
// matches one built next month.
using System.Collections.Generic;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    public static class WallStrips
    {
        /// <summary>Arrangement. Runs are measured in whole pitch units and cycle along the
        /// wall, separated by a one-unit gap — except Solid, which never breaks.</summary>
        public enum Pattern { Even, Long, Morse, Dense, Ticks, Solid }

        /// <summary>The panel rhythm of the 5 m Synty module. Every dash edge is a multiple of
        /// this, which is what makes a strip line up with the wall behind it.</summary>
        public const float Pitch = 0.5f;

        /// <summary>Where a strip sits on the wall. Height alone was not enough: the Synty
        /// wall is not one flat plane, so a strip that reads as part of it has to lie ON the
        /// surface it is nearest, which means a tilt as well as a height.</summary>
        public struct Row
        {
            /// <summary>Metres above the floor.</summary>
            public float height;
            /// <summary>Degrees about the strip's long axis. 0 is flat against a vertical
            /// wall; negative lays the strip back onto a surface that slopes away.</summary>
            public float tilt;
            /// <summary>Metres out of the wall face, along the strip's own normal. Positive
            /// moves it into the room.</summary>
            public float outward;
        }

        /// <summary>The placement table, and the only place a strip position is written.
        /// Adding an entry adds a row everywhere — the Rows slider and the row cap both read
        /// their maximum from this array.
        ///
        /// Row 0 is the wall-to-floor junction. Row 1's tilt is measured off the band hand-set
        /// on the east wall to lie on the upper chamfer; its height and outward offset are
        /// still the flat-wall values and want the same treatment.</summary>
        public static readonly Row[] Rows =
        {
            new Row { height = 0.35f, tilt = 0f,      outward = 0f },
            new Row { height = 1.42f, tilt = -51.57f, outward = 0f },
        };

        public const float DefaultThickness = 0.18f;
        public const float DefaultDepth = 0.025f;

        /// <summary>One lit rectangle on a wall: where it sits, how it lies, and how big it is.
        /// Local to the wall piece's own origin, x running along it and z out of it.</summary>
        public struct Dash
        {
            public float x, y, z;
            /// <summary>Degrees about the dash's long axis. 0 lies flat on a vertical face;
            /// positive lays it back onto a surface that slopes away from the room.</summary>
            public float tilt;
            public float length, thickness;
        }



        /// <summary>The Synty module. A strip run is laid out against this rather than against
        /// whatever length a wall happens to be, because the walls tile on it.</summary>
        public const float Module = 5f;

        // The shell is shared across Wall_01 to Wall_06: same module, same chamfer, so the
        // heights, the 40-degree tilt and the two z offsets carry over from the Wall_01
        // measurement unchanged. What varies per piece is the rhythm — how many dashes, how
        // wide the gap, whether a centre block appears.
        //
        // Every row below is built by Run or RunSplit, never by hand-written x values. That is
        // what makes the strips tile: both helpers leave HALF a gap at each end of the module,
        // so where two modules meet the two halves add up to exactly one internal gap and the
        // rhythm carries across the seam instead of restarting. Hand numbers cannot hold that
        // property — the measured Wall_01 placement had a 0.16 internal gap and a 1.39 gap at
        // the seam, which is what made the repeat announce itself.

        const float LoY = 0.500f, LoZ = 4.950f;    // lower row, flat on the vertical face
        const float HiY = 1.500f, HiZ = 5.100f;    // upper row, laid onto the chamfer
        const float HiTilt = 40f;
        const float MidY = 0.700f, MidZ = 4.860f;  // the centre block

        const float LoThick = 0.180f, HiThick = 0.270f;

        /// <summary>`count` dashes filling one module, with `gap` between them and half a gap at
        /// each end. Dash length falls out of the arithmetic, so the run always spans the module
        /// exactly and always meets the next one correctly.</summary>
        static Dash[] Run(float y, float z, float tilt, float thickness, int count, float gap)
        {
            var outp = new Dash[count];
            float len = (Module - count * gap) / count;
            for (int i = 0; i < count; i++)
                outp[i] = new Dash
                {
                    x = gap * 0.5f + i * (len + gap),
                    y = y, z = z, tilt = tilt, length = len, thickness = thickness,
                };
            return outp;
        }

        /// <summary>One dash either side of a centre block, same edge rule. The block is a
        /// feature, so it keeps its own width and the dashes give up length to it.</summary>
        static Dash[] RunSplit(float y, float z, float tilt, float thickness,
                               float blockWidth, float blockTall, float gap)
        {
            float len = (Module - blockWidth - 3f * gap) * 0.5f;
            float x0 = gap * 0.5f;
            float xb = x0 + len + gap;
            float x1 = xb + blockWidth + gap;
            return new[]
            {
                new Dash { x = x0, y = y, z = z, tilt = tilt, length = len, thickness = thickness },
                new Dash { x = xb, y = MidY, z = MidZ, tilt = 0f,
                           length = blockWidth, thickness = blockTall },
                new Dash { x = x1, y = y, z = z, tilt = tilt, length = len, thickness = thickness },
            };
        }

        static Dash[] Join(params Dash[][] parts)
        {
            var all = new List<Dash>();
            foreach (var p in parts) all.AddRange(p);
            return all.ToArray();
        }

        /// <summary>Wall_01. Heights, tilt and the block's proportions are the hand placement;
        /// the x values are the run arithmetic, which is the only change — they had to move for
        /// the rhythm to survive the seam.</summary>
        public static readonly Dash[] Wall01 = Join(
            RunSplit(LoY, LoZ, 0f, LoThick, 0.80f, 0.864f, 0.16f),
            Run(HiY, HiZ, HiTilt, HiThick, 2, 0.75f));

        /// <summary>Wall_02 — busier along the deck, emptier at eye height.</summary>
        public static readonly Dash[] Wall02 = Join(
            Run(LoY, LoZ, 0f, LoThick, 3, 0.30f),
            Run(HiY, HiZ, HiTilt, HiThick, 2, 0.60f));

        /// <summary>Wall_03 — chamfer only, so it can sit behind a console run without the
        /// strips fighting the silhouette.</summary>
        public static readonly Dash[] Wall03 =
            Run(HiY, HiZ, HiTilt, HiThick, 3, 0.45f);

        /// <summary>Wall_04 — a wide centre block with the rows giving way to it, so the piece
        /// reads as a bay with something in the middle.</summary>
        public static readonly Dash[] Wall04 = Join(
            RunSplit(LoY, LoZ, 0f, LoThick, 1.10f, 1.050f, 0.24f),
            Run(HiY, HiZ, HiTilt, HiThick, 2, 1.20f));

        /// <summary>Wall_04_Alt — the same deck, one long chamfer instead of two. Beside a
        /// Wall_04 it reads as a related piece rather than the same one twice.</summary>
        public static readonly Dash[] Wall04Alt = Join(
            RunSplit(LoY, LoZ, 0f, LoThick, 1.10f, 0.700f, 0.24f),
            Run(HiY, HiZ, HiTilt, HiThick, 1, 0.90f));

        /// <summary>Wall_05 — a tick rhythm along the deck under one unbroken chamfer. The most
        /// rhythmic of the set, for a length of corridor you move through.</summary>
        public static readonly Dash[] Wall05 = Join(
            Run(LoY, LoZ, 0f, LoThick, 5, 0.40f),
            Run(HiY, HiZ, HiTilt, HiThick, 1, 0.45f));

        /// <summary>Wall_06 — the inverse of Wall_05, so the two can face each other across a
        /// room without echoing.</summary>
        public static readonly Dash[] Wall06 = Join(
            Run(LoY, LoZ, 0f, LoThick, 1, 0.50f),
            Run(HiY, HiZ, HiTilt, HiThick, 3, 0.35f));

        /// <summary>Which arrangement belongs to which wall piece. A piece is in here only once
        /// somebody has placed dashes on it and those have been measured back; the rest get
        /// nothing, deliberately. Copying one wall's numbers onto a piece with different
        /// geometry produces dashes floating off the surface, which reads as a working
        /// arrangement from a distance and is worse than an empty wall.</summary>
        static readonly Dictionary<string, Dash[]> ByWall = new Dictionary<string, Dash[]>
        {
            { "SM_Bld_Wall_01",     Wall01 },
            { "SM_Bld_Wall_01_Alt", Wall01 },
            { "SM_Bld_Wall_02",     Wall02 },
            { "SM_Bld_Wall_03",     Wall03 },
            { "SM_Bld_Wall_04",     Wall04 },
            { "SM_Bld_Wall_04_Alt", Wall04Alt },
            { "SM_Bld_Wall_05",     Wall05 },
            { "SM_Bld_Wall_06",     Wall06 },
        };

        /// <summary>The arrangement for a wall piece, or an empty one where none is measured
        /// yet. Empty is the honest answer: it says the wall is still to be authored.</summary>
        public static Dash[] For(string wallName)
        {
            return ByWall.TryGetValue(wallName, out var d) ? d : System.Array.Empty<Dash>();
        }

        /// <summary>The pieces that still have no arrangement, for a tool that wants to say so.
        /// </summary>
        public static bool HasArrangement(string wallName) => ByWall.ContainsKey(wallName);

        static readonly Dictionary<Pattern, int[]> Runs = new Dictionary<Pattern, int[]>
        {
            { Pattern.Even,  new[] { 2, 2, 2, 2 } },
            { Pattern.Long,  new[] { 6, 3 } },
            { Pattern.Morse, new[] { 1, 3, 1, 2 } },
            { Pattern.Dense, new[] { 3, 2, 4, 1 } },
            { Pattern.Ticks, new[] { 1, 1, 1, 1 } },
            { Pattern.Solid, new[] { 0 } },
        };

        /// <summary>A stable name for the mesh this call produces, so callers can cache the
        /// asset without two configurations colliding on one path.</summary>
        public static string NameFor(float length, int row, Pattern pattern, float depth)
        {
            return $"Strip_{Mathf.RoundToInt(length * 100f)}_{row}_{pattern}_" +
                   $"{Mathf.RoundToInt(depth * 1000f)}";
        }

        /// <summary>Two submeshes: 0 is the recess the strip sits in, 1 is the lit dashes. That
        /// matches the body/glow material pair every caller already assigns.</summary>
        public static Mesh Build(float length, int row, Pattern pattern,
                                 float thickness = DefaultThickness, float depth = DefaultDepth)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var body = new List<int>();
            var glow = new List<int>();

            float half = Mathf.Max(0.01f, thickness) * 0.5f;
            float d = Mathf.Max(0.001f, depth);

            // the recess, a shade proud of the wall so the strip has something to sit in
            Box(v, n, uv, body, 0f, length, -half - 0.05f, half + 0.05f, 0f, d * 0.6f);

            if (pattern == Pattern.Solid)
            {
                Box(v, n, uv, glow, Pitch * 0.5f, length - Pitch * 0.5f, -half, half, d * 0.6f, d);
            }
            else
            {
                var run = Runs[pattern];
                float edge = Pitch * 0.5f;
                float x = edge;
                // phase by row, so a second strip is not a copy of the first directly above it
                int i = Mathf.Abs(row);
                int guard = 0;
                while (x < length - edge && guard++ < 512)
                {
                    float w = run[i % run.Length] * Pitch;
                    if (x + w > length - edge) w = length - edge - x;
                    if (w > 0.05f)
                        Box(v, n, uv, glow, x, x + w, -half, half, d * 0.6f, d);
                    x += w + Pitch;
                    i++;
                }
            }

            var mesh = new Mesh { name = NameFor(length, row, pattern, depth) };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(body, 0);
            mesh.SetTriangles(glow, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>One dash on its own, for placing by hand. Same cross-section as a dash cut
        /// from a generated strip, so a transform measured off this one transfers directly into
        /// the Rows table without any correction.</summary>
        public static Mesh BuildDash(float length = Pitch * 2f,
                                     float thickness = DefaultThickness,
                                     float depth = DefaultDepth)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();

            float half = Mathf.Max(0.01f, thickness) * 0.5f;
            float d = Mathf.Max(0.001f, depth);
            Box(v, n, uv, tris, 0f, length, -half, half, 0f, d);

            var mesh = new Mesh
            {
                name = $"Dash_{Mathf.RoundToInt(length * 100f)}_" +
                       $"{Mathf.RoundToInt(thickness * 100f)}_{Mathf.RoundToInt(depth * 1000f)}"
            };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);   // one submesh: a dash is lit, it has no recess
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>An axis-aligned box, appended into one submesh's index list. Local space is
        /// x along the wall, y up from the strip centre, z out of the wall.</summary>
        static void Box(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris,
                        float x0, float x1, float y0, float y1, float z0, float z1)
        {
            AddFace(v, n, uv, tris, new Vector3(x0, y0, z1), new Vector3(x1, y0, z1),
                    new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.forward);
            AddFace(v, n, uv, tris, new Vector3(x1, y0, z0), new Vector3(x0, y0, z0),
                    new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), Vector3.back);
            AddFace(v, n, uv, tris, new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
                    new Vector3(x1, y1, z0), new Vector3(x0, y1, z0), Vector3.up);
            AddFace(v, n, uv, tris, new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
                    new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), Vector3.down);
            AddFace(v, n, uv, tris, new Vector3(x0, y0, z0), new Vector3(x0, y0, z1),
                    new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), Vector3.left);
            AddFace(v, n, uv, tris, new Vector3(x1, y0, z1), new Vector3(x1, y0, z0),
                    new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), Vector3.right);
        }

        static void AddFace(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris,
                            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) n.Add(normal);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }
    }
}
