// Configurable control panels: a bezel plate carrying a grid of buttons, a row of indicator
// lamps, and knobs.
//
// Built rather than cropped. The atlas has one wide instrument and a lot of small ones, so a
// long panel assembled from atlas crops is always the same handful of pictures rearranged. A
// generator gives a panel that is the size you asked for, with the density you asked for, and
// no two need be alike.
//
// Colour lives in submeshes, not in vertex colours or per-element materials: the pack's shader
// does not read vertex colour, and one material per button would be hundreds of draw calls.
// Submesh 0 is the plate and every unlit element; submesh 1+n is palette colour n. The caller
// assigns one dark material and one emissive material per palette entry, so a panel of any
// button count costs palette-length-plus-one materials.
//
// Deterministic on `seed`: the same spec always produces the same panel, so a wall rebuilds
// identically and a station matches itself between sessions.
using System.Collections.Generic;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    public static class ControlPanels
    {
        /// <summary>What a panel is made of. Sizes are metres; counts are what they say.
        /// </summary>
        public struct Spec
        {
            public float width, height;

            /// <summary>The button grid. 0 columns leaves the upper area as bare plate.
            /// </summary>
            public int cols, rows;

            /// <summary>How many of the grid's buttons are lit, as a fraction. The reference
            /// panels are mostly dark with a few live — a fully lit grid reads as a test
            /// pattern rather than as equipment.</summary>
            [Range(0f, 1f)] public float litFraction;

            /// <summary>Round indicator lamps along the bottom edge. These are always lit;
            /// they are the thing that reads at distance.</summary>
            public int lamps;

            /// <summary>Round dials flanking the lower strip. 0, 1 or 2.</summary>
            public int knobs;

            /// <summary>A recessed frame around the grid, with the plate visible outside it.
            /// </summary>
            public bool bezel;

            public int seed;

            /// <summary>How many colours the lit elements draw from. The mesh gets this many
            /// submeshes plus one, and the caller supplies that many materials.</summary>
            public int paletteSize;
        }

        /// <summary>The reference panel: a 4x3 grid mostly dark, two knobs, eight lamps.
        /// </summary>
        public static Spec Default(float width, float height, int seed) => new Spec
        {
            width = width, height = height,
            cols = 4, rows = 3, litFraction = 0.45f,
            lamps = 8, knobs = 2, bezel = true,
            seed = seed, paletteSize = 4,
        };

        /// <summary>Submesh 0 is plate and unlit elements; 1..paletteSize are the lit ones by
        /// colour. Vertices are in the panel's own space: x right, y up, z out of the wall,
        /// origin at the bottom-left corner.</summary>
        public static Mesh Build(Spec s)
        {
            int palette = Mathf.Max(1, s.paletteSize);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>[palette + 1];
            for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();

            float w = Mathf.Max(0.05f, s.width);
            float h = Mathf.Max(0.05f, s.height);

            // the plate itself, and the recess the grid sits in
            Quad(v, n, uv, tris[0], 0f, 0f, w, h, 0f);
            float inset = s.bezel ? Mathf.Min(w, h) * 0.06f : 0f;
            if (s.bezel)
                Quad(v, n, uv, tris[0], inset, inset, w - inset * 2f, h - inset * 2f, 0.004f);

            uint rng = (uint)(s.seed * 2654435761u) | 1u;

            // the lower strip: knobs and lamps get the bottom third, the grid the rest
            float stripH = (s.lamps > 0 || s.knobs > 0) ? h * 0.34f : 0f;
            float gridY = inset + stripH;
            float gridH = h - inset - gridY;

            if (s.cols > 0 && s.rows > 0 && gridH > 0.01f)
            {
                float gx0 = inset + w * 0.04f;
                float gw = w - inset * 2f - w * 0.08f;
                float gap = Mathf.Min(gw / s.cols, gridH / s.rows) * 0.18f;
                float bw = (gw - gap * (s.cols - 1)) / s.cols;
                float bh = (gridH - gap * (s.rows + 1)) / s.rows;

                for (int r = 0; r < s.rows; r++)
                for (int c = 0; c < s.cols; c++)
                {
                    float bx = gx0 + c * (bw + gap);
                    float by = gridY + gap + r * (bh + gap);
                    bool lit = Next(ref rng) < s.litFraction;
                    int sub = lit ? 1 + (int)(Next32(ref rng) % (uint)palette) : 0;
                    Quad(v, n, uv, tris[sub], bx, by, bw, bh, 0.008f);
                }
            }

            if (s.knobs > 0)
            {
                float kr = stripH * 0.30f;
                float ky = inset + stripH * 0.55f;
                for (int i = 0; i < Mathf.Min(2, s.knobs); i++)
                {
                    float kx = i == 0 ? inset + w * 0.08f + kr : w - inset - w * 0.08f - kr;
                    // unlit: a dial is a knob, not a light
                    Disc(v, n, uv, tris[0], kx, ky, kr, 0.010f);
                }
            }

            if (s.lamps > 0)
            {
                float lr = Mathf.Min(stripH * 0.12f, w / (s.lamps * 3f));
                float ly = inset + stripH * 0.16f;
                float span = w - inset * 2f - w * 0.30f;
                float step = s.lamps > 1 ? span / (s.lamps - 1) : 0f;
                float lx0 = inset + w * 0.15f;
                for (int i = 0; i < s.lamps; i++)
                {
                    int sub = 1 + (int)(Next32(ref rng) % (uint)palette);
                    Disc(v, n, uv, tris[sub], lx0 + i * step, ly, lr, 0.012f);
                }
            }

            var mesh = new Mesh
            {
                name = $"Panel_{Mathf.RoundToInt(w * 100f)}x{Mathf.RoundToInt(h * 100f)}_" +
                       $"{s.cols}x{s.rows}_{s.lamps}L{s.knobs}K_{s.seed}"
            };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = tris.Length;
            for (int i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The palette the reference panel uses: warm cream, amber, a hot orange and a
        /// deep red that reads as an unlit-but-coloured cap. Returned as HDR values, so the
        /// bright ones bloom and the red does not.</summary>
        public static Color[] Palette(int size)
        {
            var full = new[]
            {
                new Color(1.00f, 0.96f, 0.72f) * 1.15f,   // cream
                new Color(1.00f, 0.60f, 0.18f) * 1.15f,   // amber
                new Color(1.00f, 0.42f, 0.10f) * 1.15f,   // hot orange
                new Color(0.42f, 0.08f, 0.05f) * 0.85f,   // deep red, below bloom on purpose
            };
            if (size >= full.Length) return full;
            var cut = new Color[Mathf.Max(1, size)];
            for (int i = 0; i < cut.Length; i++) cut[i] = full[i];
            return cut;
        }

        // ---- geometry ---------------------------------------------------------------------

        static void Quad(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris,
                         float x, float y, float w, float h, float z)
        {
            int i = v.Count;
            v.Add(new Vector3(x, y, -z));
            v.Add(new Vector3(x + w, y, -z));
            v.Add(new Vector3(x + w, y + h, -z));
            v.Add(new Vector3(x, y + h, -z));
            for (int k = 0; k < 4; k++) n.Add(Vector3.back);
            // a flat sample of the atlas: these elements are lit by their material, not by a map
            for (int k = 0; k < 4; k++) uv.Add(new Vector2(0.02f, 0.02f));
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
            tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
        }

        /// <summary>A 12-gon. At the resolution this look renders at, more sides is wasted.
        /// </summary>
        static void Disc(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris,
                         float cx, float cy, float r, float z)
        {
            const int Sides = 12;
            int centre = v.Count;
            v.Add(new Vector3(cx, cy, -z));
            n.Add(Vector3.back);
            uv.Add(new Vector2(0.02f, 0.02f));

            for (int i = 0; i < Sides; i++)
            {
                float a = i / (float)Sides * Mathf.PI * 2f;
                v.Add(new Vector3(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, -z));
                n.Add(Vector3.back);
                uv.Add(new Vector2(0.02f, 0.02f));
            }
            for (int i = 0; i < Sides; i++)
            {
                tris.Add(centre);
                tris.Add(centre + 1 + (i + 1) % Sides);
                tris.Add(centre + 1 + i);
            }
        }

        // ---- deterministic noise ----------------------------------------------------------

        static uint Next32(ref uint state)
        {
            unchecked
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
        }

        static float Next(ref uint state) => (Next32(ref state) & 0xFFFFFF) / (float)0x1000000;
    }
}
