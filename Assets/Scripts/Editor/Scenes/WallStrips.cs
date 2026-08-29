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

        static readonly Dictionary<Pattern, int[]> Runs = new Dictionary<Pattern, int[]>
        {
            { Pattern.Even,  new[] { 2, 2, 2, 2 } },
            { Pattern.Long,  new[] { 6, 3 } },
            { Pattern.Morse, new[] { 1, 3, 1, 2 } },
            { Pattern.Dense, new[] { 3, 2, 4, 1 } },
            { Pattern.Ticks, new[] { 1, 1, 1, 1 } },
            { Pattern.Solid, new[] { 0 } },
        };

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

        // The shell is shared across the Wall_0x family: same module, same chamfer, so the
        // heights, the 40-degree tilt and the two z offsets carry over from the Wall_01
        // measurement unchanged. What varies per piece is the rhythm.
        //
        // Every row is built by Run or RunSplit, never by hand-written x values. That is what
        // makes the strips tile: both leave HALF a gap at each end of the module, so where two
        // modules meet the halves add up to one internal gap and the rhythm carries across the
        // seam instead of restarting. The measured placement had a 0.16 internal gap against a
        // 1.39 gap at the seam, which is what made the repeat announce itself.

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

        /// <summary>SM_Bld_Wall_01. Heights, tilt and the block's proportions are the hand
        /// placement; the x values are the run arithmetic, which is the only change — they had
        /// to move for the rhythm to survive the seam.</summary>
        public static readonly Dash[] Wall01 = Join(
            RunSplit(LoY, LoZ, 0f, LoThick, 0.80f, 0.864f, 0.16f),
            Run(HiY, HiZ, HiTilt, HiThick, 2, 0.75f));

        /// <summary>Which arrangement belongs to which wall piece. A piece is in here only once
        /// somebody has placed dashes on it and those have been measured back; the rest get
        /// nothing, deliberately. Copying one wall's numbers onto a piece with different
        /// geometry produces dashes floating off the surface, which reads as a working
        /// arrangement from a distance and is worse than an empty wall.</summary>
        static readonly Dictionary<string, Dash[]> ByWall = new Dictionary<string, Dash[]>
        {
            { "SM_Bld_Wall_01",     Wall01 },
            { "SM_Bld_Wall_01_Alt", Wall01 },
        };

        /// <summary>The arrangement for a wall piece, or an empty one where none is measured
        /// yet. Empty is the honest answer: it says the wall is still to be authored.</summary>
        public static Dash[] For(string wallName)
        {
            return ByWall.TryGetValue(wallName, out var d) ? d : System.Array.Empty<Dash>();
        }

        public static bool HasArrangement(string wallName) => ByWall.ContainsKey(wallName);

        /// <summary>A stable name for the mesh a Build call produces, so callers can cache the
        /// asset without two configurations colliding on one path.</summary>
        public static string NameFor(float length, int row, Pattern pattern, float depth)
        {
            return $"Strip_{Mathf.RoundToInt(length * 100f)}_{row}_{pattern}_" +
                   $"{Mathf.RoundToInt(depth * 1000f)}";
        }

        /// <summary>The generated strip: a recess with lit dashes in it, two submeshes to match
        /// the body/glow material pair. Still used by the vignette gallery's wall bands; the
        /// wall board paints BuildDecal instead.</summary>
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

        /// <summary>Where the pack's electronics artwork lives in the atlas, in UV.
        ///
        /// Found by flood-filling the lit texels of PolygonSciFiSpace_01_Protocol_Emissive: what
        /// glows in that map IS the screens, readouts and indicator lights, so the lit regions
        /// are the artwork without anyone having to identify it by eye. 101 regions; these are
        /// the three whose proportions suit a strip.
        ///
        /// A strip samples one of these rather than carrying its own geometry. The knobs and
        /// readouts are painted on, the emissive map lights them through the same grey mask the
        /// walls use, and the accent hue drives them for free.</summary>
        public struct Art
        {
            public Rect uv;
            /// <summary>Width over height of the source region. A decal repeats the rect along
            /// its length to hold this, because a stretched readout is as obvious as a
            /// stretched knob.</summary>
            public float aspect;
        }

        // Picked by looking at the albedo, not by matching aspect ratios in the emissive map.
        // The earlier method chose the radar scope three times running purely because it was
        // square, and a wall of radar scopes is not an electrical panel.
        //
        // Pixel rects, not UVs, because that is what the atlas is measured in and what an image
        // editor reports. A() does the conversion, including the flip — Unity's UV origin is
        // bottom-left and every image editor counts from the top.

        const float AtlasPx = 2048f;

        static Art A(int x, int y, int w, int h) => new Art
        {
            uv = new Rect(x / AtlasPx, 1f - (y + h) / AtlasPx, w / AtlasPx, h / AtlasPx),
            aspect = (float)w / h,
        };

        /// <summary>Long instrument readouts and lit lozenges — anything that wants to run
        /// along a wall. The first is a segmented readout; the rest are plain lit pills, which
        /// are the honest choice where a strip should read as a light and not a display.
        /// </summary>
        public static readonly Art[] Wide =
        {
            A(504, 1912, 532, 104),   // the only wide instrument in the atlas
        };

        /// <summary>Blank lit lozenges. Kept, but out of the assortment: they carry no
        /// instrumentation, and a run of them is the white-slab-in-a-recess look rather than an
        /// electrical panel. Use them where a strip should read as a light.</summary>
        public static readonly Art[] Pills =
        {
            A(152, 424, 180, 52),
            A(152, 328, 180, 52),
            A(156, 376, 176, 52),
        };

        /// <summary>What a long slot is actually built from: small screens and indicator lamps
        /// laid in a row, which is how the pack's own consoles are put together. The atlas has
        /// exactly one wide instrument and dozens of small ones, so composing a row is the only
        /// way to fill a 10:1 slot without stretching something.</summary>
        public static readonly Art[] Pieces =
        {
            A(1060, 376, 60, 48), A(1160, 376, 60, 48), A(1112, 376, 56, 48),
            A(648, 1236, 56, 52), A(1264, 444, 60, 44), A(1164, 444, 60, 44),
            A(1064, 444, 60, 44), A(1216, 444, 56, 44), A(1116, 444, 56, 44),
            A(1464, 1472, 28, 60), A(1292, 760, 28, 60), A(1284, 1176, 32, 60),
            A(1376, 1228, 28, 56), A(1304, 996, 28, 48), A(1240, 1484, 28, 48),
            A(1512, 1088, 40, 28), A(1272, 1396, 40, 28), A(1532, 732, 28, 28),
        };

        /// <summary>A piece for position `i` of a composed row. Deterministic, so a wall
        /// rebuilds identically.</summary>
        public static Art PieceAt(int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return Pieces[(int)(h % (uint)Pieces.Length)];
            }
        }

        /// <summary>Screens and instrument clusters. The first is the big text-and-chart panel;
        /// the rest are the small lit screens that sit in rows on the pack's own consoles, which
        /// is exactly the assortment a wall panel wants.</summary>
        public static readonly Art[] Mid =
        {
            A(760,    0, 280, 168),   // text columns with a bar chart
            A(1060, 376,  60,  48),
            A(1160, 376,  60,  48),
            A(1112, 376,  56,  48),
            A(648, 1236,  56,  52),   // the one with an orange block in it
            A(1264, 444,  60,  44),
            A(1164, 444,  60,  44),
            A(1064, 444,  60,  44),
            A(1216, 444,  56,  44),
            A(1116, 444,  56,  44),
        };

        /// <summary>Single vertical indicator lamps, for punctuating a run rather than filling
        /// it.</summary>
        public static readonly Art[] Tall =
        {
            A(1464, 1472, 28, 60),
            A(1292,  760, 28, 60),
            A(1284, 1176, 32, 60),
            A(1376, 1228, 28, 56),
        };

        /// <summary>One piece of artwork for a slot of this shape. The band is chosen by the
        /// slot's proportions so nothing is badly stretched, and the piece within the band by a
        /// hash of the slot — deterministic, so a wall rebuilds identically and a station
        /// matches itself between sessions, but varied enough that a run does not read as one
        /// panel repeated.</summary>
        public static Art PickFor(float slotAspect, int seed)
        {
            // 0.8, not 1.0: the centre block measures 0.80 x 0.864, which is 0.93:1 and was
            // falling through to the lamps — the one slot that most wants a screen was getting
            // a single indicator stretched square.
            var band = slotAspect >= 3f   ? Wide
                     : slotAspect >= 0.8f ? Mid
                                          : Tall;
            unchecked
            {
                uint h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return band[(int)(h % (uint)band.Length)];
            }
        }

        /// <summary>A single quad carrying one region of the atlas. One face, because this is
        /// paint on a wall and not a box bolted to it — the sides and back of a 25 mm box are
        /// never seen and cost four faces each.
        ///
        /// The rect repeats along the length however many times keeps the artwork close to its
        /// own proportions, so a long strip reads as a row of readouts rather than as one
        /// readout smeared out.</summary>
        public static Mesh BuildDecal(float length, float height, Art art)
        {
            int repeats = Mathf.Max(1, Mathf.RoundToInt((length / Mathf.Max(height, 1e-4f))
                                                        / Mathf.Max(art.aspect, 1e-4f)));

            var mesh = new Mesh
            {
                name = $"Decal_{Mathf.RoundToInt(length * 100f)}x" +
                       $"{Mathf.RoundToInt(height * 100f)}_{repeats}_" +
                       $"{Mathf.RoundToInt(art.uv.x * 2048f)}_{Mathf.RoundToInt(art.uv.y * 2048f)}"
            };

            float half = height * 0.5f;
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(0f, -half, 0f), new Vector3(length, -half, 0f),
                new Vector3(length, half, 0f), new Vector3(0f, half, 0f),
            });
            mesh.SetNormals(new List<Vector3>
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            });
            var r = art.uv;
            mesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(r.x, r.y),
                new Vector2(r.x + r.width * repeats, r.y),
                new Vector2(r.x + r.width * repeats, r.y + r.height),
                new Vector2(r.x, r.y + r.height),
            });
            mesh.SetTriangles(new List<int> { 0, 2, 1, 0, 3, 2 }, 0);
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
