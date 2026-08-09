using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Saves a live Shift prefab instance as a Prefab Variant (m_SourcePrefab → Shift GUID).
    /// Overwriting a flattened regular prefab with SaveAsPrefabAsset alone often stays regular —
    /// delete the destination first, never unpack the root, then SaveAsPrefabAssetAndConnect.</summary>
    public static class UiPrefabVariant
    {
        public static string Save(GameObject instance, string destPath)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(destPath)) throw new ArgumentException("destPath required");

            var status = PrefabUtility.GetPrefabInstanceStatus(instance);
            if (status == PrefabInstanceStatus.NotAPrefab)
                throw new InvalidOperationException(
                    "Root is not a prefab instance — cannot save as Shift variant: " + destPath);

            // Overwriting a flattened regular prefab often stays regular — delete first.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destPath) != null)
                AssetDatabase.DeleteAsset(destPath);

            var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                instance, destPath, InteractionMode.AutomatedAction);
            if (connected == null)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, destPath, out bool success);
                if (!success)
                    throw new InvalidOperationException("Failed to save prefab variant: " + destPath);
            }
            UnityEngine.Object.DestroyImmediate(instance);

            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(destPath);
            if (saved == null)
                throw new InvalidOperationException("Prefab variant missing after save: " + destPath);

            var assetType = PrefabUtility.GetPrefabAssetType(saved);
            if (assetType != PrefabAssetType.Variant)
            {
                Debug.LogWarning(
                    $"[UiPrefabVariant] Expected Variant at {destPath}, got {assetType}. " +
                    "Check that the root came from PrefabUtility.InstantiatePrefab and was never unpacked.");
            }
            return destPath;
        }

        public static void EnsureCanvasScaler(GameObject canvasRoot)
        {
            if (canvasRoot == null) return;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
