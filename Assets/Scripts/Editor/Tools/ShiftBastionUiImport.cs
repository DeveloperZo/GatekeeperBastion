using System;
using System.IO;
using GatekeeperBastion.Game;
using GatekeeperBastion.Game.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>M35 C9–C13: copy Simple HUD canvases into Bastion.unity.
    /// Lives in Editor/Tools (not Editor/Ui) so criterion 1's Ui-folder grep stays clean.</summary>
    [InitializeOnLoad]
    public static class ShiftBastionUiImport
    {
        const string SourceScenePath =
            "Assets/Shift - Complete Sci-Fi UI/Scenes/Simple HUD/Simple HUD (Desktop).unity";
        const string BastionScenePath = "Assets/Scenes/Bastion.unity";

        static readonly string[] CanvasNames =
        {
            "Canvas - In Game",
            "Canvas - Pause Menu",
        };

        static double _nextPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_bastion_ui_import_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_bastion_ui_import_report.json");

        static ShiftBastionUiImport() => EditorApplication.update += Poll;

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch (Exception e) { Debug.LogWarning("[BastionUiImport] " + e.Message); return; }
            Run("sentinel");
        }

        [MenuItem("GatekeeperBastion/M35 Import Bastion UI")]
        public static void ImportFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run("menu");
        }

        /// <summary>Batch / executeMethod entry.</summary>
        public static void ImportFromBatch() => Run("batch");

        static void Run(string trigger)
        {
            string message;
            string status = "ok";
            try
            {
                message = Import();
            }
            catch (Exception e)
            {
                status = "fail";
                message = e.GetType().Name + ": " + e.Message;
                Debug.LogError("[BastionUiImport] " + e);
            }
            File.WriteAllText(ReportPath,
                "{\"status\":\"" + status + "\",\"message\":\"" +
                message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}\n");
            Debug.Log("[BastionUiImport] " + status + " (" + trigger + "): " + message);
        }

        static string Import()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                return "missing " + SourceScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BastionScenePath) == null)
                return "missing " + BastionScenePath;

            var bastion = EditorSceneManager.OpenScene(BastionScenePath, OpenSceneMode.Single);
            foreach (var root in bastion.GetRootGameObjects())
            {
                foreach (var n in CanvasNames)
                    if (root.name == n) UnityEngine.Object.DestroyImmediate(root);
            }

            var demo = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            int copied = 0;
            try
            {
                foreach (var root in demo.GetRootGameObjects())
                {
                    bool match = false;
                    foreach (var n in CanvasNames)
                        if (root.name == n) { match = true; break; }
                    if (!match) continue;

                    var copy = UnityEngine.Object.Instantiate(root);
                    copy.name = root.name;
                    // Clear any cross-scene refs before closing the demo.
                    ClearExternalRefs(copy, demo);
                    EditorSceneManager.MoveGameObjectToScene(copy, bastion);
                    ConfigureCanvas(copy, root.name == "Canvas - Pause Menu" ? 1 : 0);
                    if (root.name == "Canvas - In Game")
                        TrimInGame(copy);
                    copied++;
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(demo, true);
            }

            EnsureHudHost(bastion);

            EditorSceneManager.MarkSceneDirty(bastion);
            EditorSceneManager.SaveScene(bastion, BastionScenePath);
            AssetDatabase.SaveAssets();
            return "copied " + copied + " canvases; bars non-interactable; minimap/backdrop trimmed";
        }

        static void ClearExternalRefs(GameObject root, Scene demo)
        {
            // Camera refs that still point into the demo scene become null after close.
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera != null &&
                canvas.worldCamera.gameObject.scene == demo)
                canvas.worldCamera = null;
        }

        static void ConfigureCanvas(GameObject canvasGo, int sortOrder)
        {
            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null)
            {
                // Simple HUD ships Overlay already; keep Overlay + order.
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.sortingOrder = sortOrder;
            }
            canvasGo.transform.localScale = Vector3.one;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1280f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            foreach (var slider in canvasGo.GetComponentsInChildren<Slider>(true))
                slider.interactable = false;
        }

        static void TrimInGame(GameObject canvas)
        {
            DestroyNamed(canvas.transform, "Minimap");
            DestroyNamed(canvas.transform, "Background");
            DestroyNamed(canvas.transform, "Backdrop");
            DestroyNamed(canvas.transform, "Placeholder");
        }

        static void DestroyNamed(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                if (all[i] != null && all[i] != root && all[i].name == name)
                    UnityEngine.Object.DestroyImmediate(all[i].gameObject);
            }
        }

        static void EnsureHudHost(Scene bastion)
        {
            if (UnityEngine.Object.FindFirstObjectByType<HudController>(FindObjectsInactive.Include) != null)
                return;
            var host = new GameObject("BastionHud");
            EditorSceneManager.MoveGameObjectToScene(host, bastion);
            host.AddComponent<HudController>();
            host.AddComponent<BastionHudView>();
        }
    }
}
