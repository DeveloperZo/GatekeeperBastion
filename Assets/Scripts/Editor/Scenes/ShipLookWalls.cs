using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>The wall's lit strip, in the deck's accent instead of the pack's cyan.
    ///
    /// SM_Bld_Wall_01 samples eleven atlas texels and exactly one of them emits — the strip,
    /// two faces of it. So swapping that material's emissive atlas for the grey mask and
    /// letting <c>_Emission_Color</c> carry the hue recolours the strip and nothing else on the
    /// piece. That is why this needs no repainted texture of its own and cannot fall out of
    /// step with the profile the way a baked one would.
    ///
    /// Walls only, and a material of their own, because the pack material is shared with
    /// consoles and screens whose coloured emissive artwork is the whole reason they read as
    /// electronics — the grey mask would flatten those to one hue.</summary>
    public static class ShipLookWalls
    {
        const string FixtureDir = "Assets/Packs/ShipFixtures";
        const string WallMesh   = "SM_Bld_Wall_01";
        const string SourceMat  = "Assets/Synty/PolygonSciFiSpace/Materials/PolygonScifiSpace_01_A.mat";
        const string AccentMat  = FixtureDir + "/Wall_Accent.mat";
        const string Mask       = "Assets/Materials/PolygonSciFiSpace_01_Protocol_Emissive_Mask.png";

        /// <summary>What the pack ships the strip at. Kept, so this changes the hue and not the
        /// level — the strip reads as the same fixture, lit in the deck's colour.</summary>
        const float StripPeak = 2f;

        /// <summary>Puts every wall on the accent strip, or back on the pack material. Returns
        /// how many renderers changed.</summary>
        public static int Apply(List<MeshRenderer> dress, ShipLookProfile p)
        {
            var src = AssetDatabase.LoadAssetAtPath<Material>(SourceMat);
            if (src == null)
            {
                Debug.LogWarning($"[ShipLook] {SourceMat} missing; the walls keep what they wear.");
                return 0;
            }
            var accent = p.wallStripAccent ? Build(src, p) : null;
            var stale = AssetDatabase.LoadAssetAtPath<Material>(AccentMat);

            int changed = 0;
            foreach (var r in dress)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith(WallMesh)) continue;

                var mats = r.sharedMaterials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (accent != null && (m == src || m == stale) && m != accent) { mats[i] = accent; dirty = true; }
                    else if (accent == null && m != null && m == stale) { mats[i] = src; dirty = true; }
                }
                if (!dirty) continue;
                Undo.RecordObject(r, "Ship look walls");
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                changed++;
            }
            return changed;
        }

        static Material Build(Material src, ShipLookProfile p)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AccentMat);
            if (mat == null)
            {
                mat = new Material(src) { name = "Wall_Accent" };   // atlas and maps come along
                AssetDatabase.CreateAsset(mat, AccentMat);
            }
            else
            {
                // the pack material is the source of truth for everything except the strip
                EditorUtility.CopySerialized(src, mat);
                mat.name = "Wall_Accent";
            }
            mat.shaderKeywords = new string[0];

            // grey, so the hue is entirely the accent's. The pack's atlas is cyan, and a
            // coloured atlas multiplies the chosen colour rather than carrying it — the same
            // trap that ate the red out of the deck accent before the mask went in there.
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Mask);
            if (mask == null)
                Debug.LogWarning($"[ShipLook] {Mask} missing; the wall strip keeps the pack's cyan.");
            else if (mat.HasProperty("_Emission_Map"))
                mat.SetTexture("_Emission_Map", mask);

            var hue = p.deckAccent;
            float top = Mathf.Max(hue.r, Mathf.Max(hue.g, hue.b));
            var glow = hue / Mathf.Max(top, 1e-4f) * StripPeak;
            glow.a = 1f;
            if (mat.HasProperty("_Emission_Color")) mat.SetColor("_Emission_Color", glow);
            if (mat.HasProperty("_Enable_Emission")) mat.SetFloat("_Enable_Emission", 1f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
