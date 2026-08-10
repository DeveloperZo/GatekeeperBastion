using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>
    /// Builds skeleton prefab variants under Assets/Prefabs/Ui/ from Shift source prefabs.
    /// One menu entry for all UI assemble work. File-bridge: gkb_ui_build_request.txt →
    /// gkb_ui_build_report.json. Never edits Shift prefabs in place (UIManager.asset colours/fonts
    /// are the only Shift asset touch).
    /// </summary>
    [InitializeOnLoad]
    public static class UiPrefabBuilder
    {
        const string PrefabFolder = "Assets/Prefabs/Ui";
        const string ShiftCanvas =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Other/Canvas.prefab";
        const string ShiftMainButton =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Main Button.prefab";
        const string ShiftPanelButton =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Panel Button.prefab";
        const string ShiftModalExit =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Modal Window/Exit.prefab";

        static readonly (string PrefabName, string SourcePath)[] Skeletons =
        {
            ("Splash", ShiftCanvas),
            ("MainMenu", ShiftCanvas),
            ("Options", ShiftCanvas),
            ("ShipHud", ShiftCanvas),
            ("StationPanel", ShiftCanvas),
            ("BastionHud", ShiftCanvas),
            ("ShortcutPanel", ShiftPanelButton),
        };

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_ui_build_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_ui_build_report.json");

        static double _nextSentinelPoll;

        static UiPrefabBuilder()
        {
            EditorApplication.update += PollSentinel;
        }

        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextSentinelPoll)
                return;
            _nextSentinelPoll = EditorApplication.timeSinceStartup + 1.0;

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            if (!File.Exists(SentinelPath))
                return;

            try { File.Delete(SentinelPath); }
            catch (Exception e) { Debug.LogWarning("[UiPrefabBuilder] Could not delete sentinel: " + e.Message); }

            BuildAll("sentinel");
        }

        [MenuItem("GatekeeperBastion/Build UI Prefabs")]
        public static void BuildFromMenu() => BuildAll("menu");

        /// <summary>Batchmode entry: build prefab variants then exit.</summary>
        public static void BuildFromBatch()
        {
            BuildAll("batch");
            bool ok = File.Exists(ReportPath) &&
                      File.ReadAllText(ReportPath).Contains("\"status\":\"ok\"");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void BuildAll(string trigger)
        {
            EnsureTmpEssentials();
            EnsurePrefabFolder();

            var built = new List<string>();
            var errors = new List<string>();

            try
            {
                ShiftFrontEndAssembler.BuildAll(trigger + "+frontend");
                built.Add($"{PrefabFolder}/Splash.prefab");
                built.Add($"{PrefabFolder}/MainMenu.prefab");
                built.Add($"{PrefabFolder}/Options.prefab");
                built.Add($"{PrefabFolder}/ConfirmModal.prefab");
            }
            catch (Exception e)
            {
                errors.Add("frontend: " + FormatEx(e));
            }

            foreach (var (prefabName, sourcePath) in Skeletons)
            {
                if (prefabName == "Splash" || prefabName == "MainMenu" || prefabName == "Options")
                    continue;

                string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
                try
                {
                    // Ship screens are fully authored by ShipUiAssembler (content + wiring).
                    if (prefabName == "ShipHud" || prefabName == "StationPanel" ||
                        prefabName == "ShortcutPanel")
                    {
                        if (ShipUiAssembler.TryAssemble(prefabName, out string shipErr))
                            built.Add(prefabPath);
                        else
                            errors.Add($"{prefabName}: {shipErr}");
                        continue;
                    }

                    // BastionHud must NOT go through TryBuildVariant — that saves an empty
                    // Shift Canvas and wiped the assembled fight/end HUD (M35 regression).
                    if (prefabName == "BastionHud")
                    {
                        BastionUiAssembler.AssembleAll(trigger + "+bastion");
                        built.Add(prefabPath);
                        built.Add($"{PrefabFolder}/EndScreens.prefab");
                        built.Add($"{PrefabFolder}/ArmoryWheel.prefab");
                        continue;
                    }

                    if (TryBuildVariant(sourcePath, prefabPath, prefabName, out string error))
                        built.Add(prefabPath);
                    else
                        errors.Add($"{prefabName}: {error}");
                }
                catch (Exception e)
                {
                    errors.Add($"{prefabName}: {FormatEx(e)}");
                }
            }

            // Main Button reference variant for screen agents wiring menu entries.
            try
            {
                string btnPath = $"{PrefabFolder}/MainButton.prefab";
                if (TryBuildVariant(ShiftMainButton, btnPath, "MainButton", out string btnErr))
                    built.Add(btnPath);
                else
                    errors.Add("MainButton: " + btnErr);
            }
            catch (Exception e)
            {
                errors.Add("MainButton: " + FormatEx(e));
            }

            string status = errors.Count == 0 ? "ok" : "error";
            string message = errors.Count == 0
                ? $"Built {built.Count} Shift prefab variant(s) ({trigger})."
                : string.Join("; ", errors);

            WriteReport(status, message, built, errors);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (errors.Count == 0)
                Debug.Log("[UiPrefabBuilder] " + message);
            else
                Debug.LogWarning("[UiPrefabBuilder] " + message);
        }

        /// <summary>
        /// Instantiate a Shift prefab then save as a Prefab Variant (m_SourcePrefab → Shift GUID).
        /// </summary>
        static bool TryBuildVariant(string sourcePath, string destPath, string rootName, out string error)
        {
            error = null;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                error = $"missing Shift source {sourcePath}";
                return false;
            }

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                error = "InstantiatePrefab returned null";
                return false;
            }

            try
            {
                instance.name = rootName;
                if (sourcePath == ShiftCanvas)
                    UiPrefabVariant.EnsureCanvasScaler(instance);
                UiPrefabVariant.Save(instance, destPath);
                instance = null; // Save destroys the instance
                return true;
            }
            catch (Exception e)
            {
                error = FormatEx(e);
                return false;
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static void EnsureTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro/Resources"))
                return;
            try
            {
                TMPro.TMP_PackageResourceImporter.ImportResources(
                    importEssentials: true, importExamples: false, interactive: false);
                AssetDatabase.Refresh();
                Debug.Log("[UiPrefabBuilder] Imported TextMesh Pro essentials.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UiPrefabBuilder] TMP essentials import failed: " + FormatEx(e));
            }
        }

        static string FormatEx(Exception e)
        {
            if (e.InnerException != null)
                e = e.InnerException;
            return e.GetType().Name + ": " + e.Message;
        }

        static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Ui");
        }

        static void WriteReport(string status, string message, List<string> prefabs, List<string> errors)
        {
            var sb = new StringBuilder();
            sb.Append("{\"status\":").Append(Quote(status))
              .Append(",\"message\":").Append(Quote(Truncate(message ?? "", 800)))
              .Append(",\"unity\":").Append(Quote(Application.unityVersion))
              .Append(",\"timestampUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")))
              .Append(",\"prefabs\":").Append(StringList(prefabs))
              .Append(",\"errors\":").Append(StringList(errors))
              .Append('}');

            try { File.WriteAllText(ReportPath, sb.ToString()); }
            catch (Exception e) { Debug.LogError("[UiPrefabBuilder] Could not write report: " + e.Message); }
        }

        static string StringList(List<string> items)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Quote(items[i]));
            }
            return sb.Append(']').ToString();
        }

        static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') { }
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }
    }
}
