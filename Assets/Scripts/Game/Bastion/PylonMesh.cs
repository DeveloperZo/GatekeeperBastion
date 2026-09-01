using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The two pieces FORGE's turret is made of, as procedural meshes: a dark tapered
    /// plinth and the crystal that floats over it. Neither shape exists as a Unity primitive and
    /// neither may come from a vendor pack — a machine whose read depends on an unimported art
    /// pack is a machine that is invisible on a fresh clone — so they are built here in code.
    /// <para>Both are built once and handed out by reference. A turret is spawned and despawned
    /// many times a Wave and exists twice over in a session (the server's gun plus a scenery copy
    /// on every client), so a per-spawn build would allocate two meshes per placement and leave
    /// them for the collector. Every pylon in the room shares these two.</para>
    /// <para>Presentation only. Nothing here crosses into Sim.</para></summary>
    public static class PylonMesh
    {
        // Authored to sit on the turret root, which stands half a metre off the floor. The plinth
        // mesh is built with its base at local y = 0 and grows upward, so the spawner drops it by
        // that half metre and the pedestal meets the floor exactly.
        const float PlinthBaseRadius = 0.46f;
        const float PlinthTopRadius = 0.30f;
        const float PlinthHeight = 0.55f;
        const int PlinthSides = 6;

        // The crystal is built about its own origin so the spawner can raise, lower and spin it
        // by moving one transform. Taller above than below: a crystal that reads as pointing up
        // rather than as a floating diamond.
        const float CrystalRadius = 0.18f;
        const float CrystalTopApex = 0.30f;
        const float CrystalBottomApex = 0.20f;

        /// <summary>Half the crystal's body below its origin. The spawner needs this to seat the
        /// crystal on the plinth when the machine is spent, and exposing it beats two files
        /// carrying the same literal and drifting apart.</summary>
        public static float CrystalDrop => CrystalBottomApex;

        /// <summary>How far the plinth rises above its own base. The spawner reads it to place
        /// the crystal's float height above the pedestal rather than guessing at a gap.</summary>
        public static float PlinthRise => PlinthHeight;

        static Mesh _crystal;
        static Mesh _plinth;

        /// <summary>The faceted octahedron that carries charge. Every face gets its own three
        /// vertices: shared corners would let Unity average the normals and the crystal would
        /// shade as a lumpy sphere instead of reading as cut stone, which is the whole point of
        /// the shape. 24 vertices, 8 triangles.</summary>
        public static Mesh Crystal
        {
            get
            {
                // == null rather than a null-coalescing form: a destroyed Mesh leaves a live C#
                // wrapper behind and only Unity's overloaded comparison sees that.
                if (_crystal == null) _crystal = BuildCrystal();
                return _crystal;
            }
        }

        /// <summary>The dark pedestal: a hexagonal frustum, wide at the floor and narrowing to a
        /// smaller top ring, capped at both ends. Faceted for the same reason as the crystal —
        /// the flat faces are what make the taper legible from across a room. 38 vertices,
        /// 24 triangles.</summary>
        public static Mesh Plinth
        {
            get
            {
                if (_plinth == null) _plinth = BuildPlinth();
                return _plinth;
            }
        }

        static Mesh BuildCrystal()
        {
            // Ring in increasing angle about +Y, which fixes the winding rule used below: an
            // upper face is wound (apex, ring[i + 1], ring[i]) to face outward, a lower one the
            // other way round.
            var ring = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float a = i * (Mathf.PI * 0.5f);
                ring[i] = new Vector3(Mathf.Cos(a) * CrystalRadius, 0f, Mathf.Sin(a) * CrystalRadius);
            }
            var top = new Vector3(0f, CrystalTopApex, 0f);
            var bottom = new Vector3(0f, -CrystalBottomApex, 0f);

            var verts = new Vector3[24];
            var tris = new int[24];
            int v = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = ring[i];
                Vector3 q = ring[(i + 1) & 3];
                verts[v] = top; verts[v + 1] = q; verts[v + 2] = p;
                tris[v] = v; tris[v + 1] = v + 1; tris[v + 2] = v + 2;
                v += 3;
                verts[v] = bottom; verts[v + 1] = p; verts[v + 2] = q;
                tris[v] = v; tris[v + 1] = v + 1; tris[v + 2] = v + 2;
                v += 3;
            }
            return Finish("PylonCrystal", verts, tris);
        }

        static Mesh BuildPlinth()
        {
            var baseRing = new Vector3[PlinthSides];
            var topRing = new Vector3[PlinthSides];
            for (int i = 0; i < PlinthSides; i++)
            {
                float a = i * (Mathf.PI * 2f / PlinthSides);
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                baseRing[i] = new Vector3(c * PlinthBaseRadius, 0f, s * PlinthBaseRadius);
                topRing[i] = new Vector3(c * PlinthTopRadius, PlinthHeight, s * PlinthTopRadius);
            }

            // Six side quads at four unshared vertices each, then a fan cap at each end around
            // its own centre vertex. The floor-facing cap is never seen in play, but a mesh left
            // open reads as a hole the moment the camera dips below the pedestal.
            var verts = new Vector3[PlinthSides * 4 + (PlinthSides + 1) * 2];
            var tris = new int[PlinthSides * 6 + PlinthSides * 6];
            int v = 0;
            int t = 0;
            for (int i = 0; i < PlinthSides; i++)
            {
                int n = (i + 1) % PlinthSides;
                verts[v] = baseRing[i];
                verts[v + 1] = baseRing[n];
                verts[v + 2] = topRing[i];
                verts[v + 3] = topRing[n];
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
                v += 4;
                t += 6;
            }

            int topCentre = v;
            verts[v] = new Vector3(0f, PlinthHeight, 0f);
            v++;
            for (int i = 0; i < PlinthSides; i++) verts[v + i] = topRing[i];
            for (int i = 0; i < PlinthSides; i++)
            {
                int n = (i + 1) % PlinthSides;
                tris[t] = topCentre;
                tris[t + 1] = v + n;
                tris[t + 2] = v + i;
                t += 3;
            }
            v += PlinthSides;

            int baseCentre = v;
            verts[v] = Vector3.zero;
            v++;
            for (int i = 0; i < PlinthSides; i++) verts[v + i] = baseRing[i];
            for (int i = 0; i < PlinthSides; i++)
            {
                int n = (i + 1) % PlinthSides;
                tris[t] = baseCentre;
                tris[t + 1] = v + i;
                tris[t + 2] = v + n;
                t += 3;
            }

            return Finish("PylonPlinth", verts, tris);
        }

        /// <summary>Seal a built mesh. RecalculateNormals is safe to lean on here precisely
        /// because no vertex is shared between faces — it hands every triangle its own face
        /// normal, which is the faceted look, with none of the averaging that would round it off.
        /// <para>HideAndDontSave matches every other runtime-built shared asset in this tree: the
        /// mesh belongs to the session, not to a scene, and must not be written into one.</para>
        /// </summary>
        static Mesh Finish(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
