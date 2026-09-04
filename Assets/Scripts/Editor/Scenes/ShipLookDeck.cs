using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>The deck as the gallery's My Ship bay wears it: the Synty floor tile kept, its
    /// vents pulsing on GKB/EmissiveFlowAtlas, wet as smoothness on the tile itself. The look
    /// pass swaps only the material slot of each floor tile and swaps it back when the profile
    /// turns the flow off, so re-running never accumulates and the hand-placed geometry is
    /// otherwise untouched. One shared material, so the tiles still batch.</summary>
    public static class ShipLookDeck
    {
        const string FixtureDir = "Assets/Packs/ShipFixtures";
        const string FloorMesh  = "SM_Bld_Floor_01";
        const string SourceMat  = "Assets/Synty/PolygonSciFiSpace/Materials/PolygonScifiSpace_01_A.mat";
        const string DeckMat    = FixtureDir + "/Deck_Flow.mat";
        const string Albedo     = "Assets/Materials/PolygonSciFiSpace_01_Protocol_NoHazard.png";
        const string Mask       = "Assets/Materials/PolygonSciFiSpace_01_Protocol_Emissive_Mask.png";
        const string VentAxes   = "Assets/Materials/PolygonSciFiSpace_01_Floor_VentAxes.png";
        const string FlowShader = "GKB/EmissiveFlowAtlas";

        /// <summary>Where the accent's brightest channel lands, and the Accent Strength it was
        /// judged at. Same numbers as the gallery, so the deck here is the deck there.</summary>
        const float AccentPeak = 1.15f;
        const float AccentPeakAt = 0.35f;

        /// <summary>Puts every floor tile on the flow deck, or back on the pack material.
        /// Returns how many tiles changed.</summary>
        public static int Apply(List<MeshRenderer> dress, ShipLookProfile p)
        {
            var src = AssetDatabase.LoadAssetAtPath<Material>(SourceMat);
            if (src == null)
            {
                Debug.LogWarning($"[ShipLook] {SourceMat} missing; the deck keeps whatever it wears.");
                return 0;
            }
            var flow = p.deckFlow ? Build(src, p) : null;
            var stale = AssetDatabase.LoadAssetAtPath<Material>(DeckMat);

            int changed = 0;
            foreach (var r in dress)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith(FloorMesh)) continue;

                var mats = r.sharedMaterials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (flow != null && (m == src || m == stale) && m != flow) { mats[i] = flow; dirty = true; }
                    else if (flow == null && m != null && m == stale) { mats[i] = src; dirty = true; }
                }
                if (!dirty) continue;
                Undo.RecordObject(r, "Ship look deck");
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                changed++;
            }
            return changed;
        }

        static Material Build(Material src, ShipLookProfile p)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(DeckMat);
            if (mat == null)
            {
                mat = new Material(src) { name = "Deck_Flow" };   // atlas and maps come along
                AssetDatabase.CreateAsset(mat, DeckMat);
            }
            var shader = Shader.Find(FlowShader);
            if (shader == null)
            {
                Debug.LogWarning($"[ShipLook] Shader '{FlowShader}' not found; the deck stays on the graph.");
                return null;
            }
            if (mat.shader != shader)
            {
                mat.shader = shader;
                // the pack material's keywords mean nothing to this shader and are reported
                // against it every frame; that report crashed the editor once
                mat.shaderKeywords = new string[0];
            }

            var tint = Color.Lerp(Color.white, p.deckTint, p.deckTintAmount);
            tint.a = 1f;   // alpha is clip, not tint, on this deck
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", Mathf.Clamp01(p.deckWet));
            mat.SetFloat("_Metallic", 0f);

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Albedo);
            if (albedo != null) mat.SetTexture("_Albedo_Map", albedo);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Mask);
            if (mask == null && src.HasProperty("_Emission_Map")) mask = src.GetTexture("_Emission_Map") as Texture2D;
            if (mask != null) mat.SetTexture("_Emission_Map", mask);
            var vents = AssetDatabase.LoadAssetAtPath<Texture2D>(VentAxes);
            if (vents == null) Debug.LogWarning($"[ShipLook] {VentAxes} missing; both vent sets share one colour.");
            mat.SetTexture("_Vent_Map", vents);
            mat.SetFloat("_Vent_Tile", 5f);

            mat.SetColor("_Emission_Color", Peak(p.deckAccent, AccentPeak));
            mat.SetFloat("_Enable_Emission", 1f);
            p.deckPulse.Apply(mat, "_Flow", Vector3.forward);

            Vent(p, true, out var zHue, out float zStr);
            Vent(p, false, out var xHue, out float xStr);
            mat.SetColor("_VentZ_Color", Peak(zHue, AccentPeak * zStr / AccentPeakAt));
            p.ventZFlow.Apply(mat, "_VentZ_Flow", Vector3.forward);
            mat.SetColor("_VentX_Color", Peak(xHue, AccentPeak * xStr / AccentPeakAt));
            p.ventXFlow.Apply(mat, "_VentX_Flow", Vector3.right);

            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Vent(ShipLookProfile p, bool alongZ, out Color hue, out float strength)
        {
            bool linked = alongZ ? p.ventZLinked : p.ventXLinked;
            hue = linked ? p.deckAccent : (alongZ ? p.ventZColor : p.ventXColor);
            strength = linked ? p.deckAccentStrength : (alongZ ? p.ventZStrength : p.ventXStrength);
        }

        static Color Peak(Color hue, float peak)
        {
            float top = Mathf.Max(hue.r, Mathf.Max(hue.g, hue.b));
            var c = hue / Mathf.Max(top, 1e-4f) * peak;
            c.a = 1f;
            return c;
        }
    }
}
