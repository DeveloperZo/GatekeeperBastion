using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using GatekeepersProtocol.Game;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>The halo the look was judged under. The project's shared post profile shipped
    /// with no settings at all, so every emissive band was being seen without the bloom the
    /// gallery tuned it against. The pass writes Bloom into that shared profile, gives a scene
    /// that has no volume one under its own root, and puts a PostProcessLayer on any camera
    /// missing one, ordered above PixelRender so the halo resolves before the frame is crushed.</summary>
    public static class ShipLookPost
    {
        const string SharedProfile = "Assets/Settings/PostProcessing.asset";
        const string LayerName = "PostProcessing";

        public static void Ensure(Transform root, Scene scene, ShipLookProfile p)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(SharedProfile);
            if (profile == null)
            {
                Debug.LogWarning($"[ShipLook] {SharedProfile} missing; no bloom written.");
                return;
            }
            if (!profile.TryGetSettings(out Bloom bloom))
            {
                bloom = profile.AddSettings<Bloom>();
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }
            bloom.enabled.Override(p.bloom);
            bloom.intensity.Override(p.bloomIntensity);
            bloom.threshold.Override(p.bloomThreshold);
            bloom.softKnee.Override(0.5f);
            bloom.diffusion.Override(6f);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);

            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[ShipLook] No '{LayerName}' layer in this project; no volume placed.");
                return;
            }

            bool hasVolume = false;
            foreach (var v in Object.FindObjectsByType<PostProcessVolume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v.gameObject.scene == scene && v.isGlobal) { hasVolume = true; break; }
            if (!hasVolume)
            {
                var go = new GameObject("Post Process Volume") { layer = layer };
                go.transform.SetParent(root, false);
                var volume = go.AddComponent<PostProcessVolume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
                Undo.RegisterCreatedObjectUndo(go, "Post Process Volume");
            }

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.gameObject.scene != scene) continue;
                var ppl = cam.GetComponent<PostProcessLayer>();
                if (ppl == null)
                {
                    ppl = Undo.AddComponent<PostProcessLayer>(cam.gameObject);
                    ppl.volumeTrigger = cam.transform;
                    ppl.volumeLayer = 1 << layer;
                    ppl.antialiasingMode = PostProcessLayer.Antialiasing.None;
                    EditorUtility.SetDirty(ppl);
                }
                // last in the list, so the halo is computed at full resolution and then crushed
                var px = cam.GetComponent<PixelRender>();
                if (px != null)
                    while (ComponentUtility.MoveComponentDown(px)) { }
            }
        }
    }
}
