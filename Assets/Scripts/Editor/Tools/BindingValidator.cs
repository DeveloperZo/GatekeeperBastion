using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using GatekeeperBastion.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>
    /// Headless binding validation: a reflection-based walk over the four
    /// build scenes that reports every null serialized UnityEngine.Object field on non-vendor
    /// MonoBehaviours, excluding fields marked [OptionalBinding]. Reflection-based so it runs
    /// before any view changes. Results go to <projectRoot>/gkb_bindings_report.json — a
    /// separate file, so the integration check's fixed report schema is untouched. Every missed wire
    /// across the ~200 serialized references the authored scenes carry — and every future scene edit —
    /// becomes a headless failure instead of a mid-playthrough discovery.
    /// </summary>
    public static class BindingValidator
    {
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string ReportPath => Path.Combine(Root, "gkb_bindings_report.json");

        sealed class MissingBinding
        {
            public string Scene;
            public string Path;
            public string Component;
            public string Field;
        }

        [MenuItem("GatekeeperBastion/Validate/Scene Bindings", false, 101)]
        public static void RunFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int missing = Run();
            Debug.Log($"[BindingValidator] {missing} missing binding(s). Report: {ReportPath}");
        }

        /// <summary>Batch entry: -executeMethod GatekeeperBastion.EditorTools.BindingValidator.RunFromBatch.
        /// Exit code 0 always — the report carries the verdict; a red report is data, not a crash.</summary>
        public static void RunFromBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }

        /// <summary>In-editor entry with no save dialog and no exit, for editor automation.
        /// Returns the missing-binding count; the JSON report is written either way.</summary>
        public static int RunHeadless() => Run();

        static int Run()
        {
            var missing = new List<MissingBinding>();
            var scenesChecked = new List<string>();
            string error = null;

            try
            {
                foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
                {
                    if (!entry.enabled) continue;
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path) == null)
                    {
                        error = (error == null ? "" : error + "; ") + $"missing scene asset {entry.path}";
                        continue;
                    }
                    Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                    scenesChecked.Add(scene.name);
                    ValidateScene(scene, missing);
                }
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
            }

            WriteReport(missing, scenesChecked, error);
            return missing.Count;
        }

        static void ValidateScene(Scene scene, List<MissingBinding> missing)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue; // missing script — WP-2/3/4 delete these per-scene
                    if (!IsOurs(mb.GetType())) continue;
                    ValidateComponent(scene, mb, missing);
                }
            }
        }

        /// <summary>Non-vendor = the game's own runtime assembly. Shift, TMP, MCP and package
        /// code ship null serialized fields by design and are not ours to wire.</summary>
        static bool IsOurs(Type type) =>
            type.Assembly.GetName().Name == "GatekeeperBastion";

        static void ValidateComponent(Scene scene, MonoBehaviour mb, List<MissingBinding> missing)
        {
            for (Type type = mb.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.DeclaredOnly))
                {
                    if (!IsSerializedObjectField(field)) continue;
                    if (field.IsDefined(typeof(OptionalBindingAttribute), inherit: false)) continue;

                    object value = field.GetValue(mb);
                    if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    {
                        if ((UnityEngine.Object)value == null)
                            Report(scene, mb, field.Name, missing);
                    }
                    else // array/list of UnityEngine.Object — report null elements
                    {
                        var list = value as System.Collections.IList;
                        if (list == null) continue;
                        for (int i = 0; i < list.Count; i++)
                        {
                            if ((UnityEngine.Object)list[i] == null)
                                Report(scene, mb, $"{field.Name}[{i}]", missing);
                        }
                    }
                }
            }
        }

        /// <summary>Unity-serialized fields of UnityEngine.Object type (or arrays/lists of it):
        /// public, or private with [SerializeField]; never static or [NonSerialized].</summary>
        static bool IsSerializedObjectField(FieldInfo field)
        {
            if (field.IsStatic) return false;
            if (field.IsDefined(typeof(NonSerializedAttribute), inherit: false)) return false;
            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), inherit: false)) return false;

            Type t = field.FieldType;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return true;
            if (t.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(t.GetElementType())) return true;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)
                && typeof(UnityEngine.Object).IsAssignableFrom(t.GetGenericArguments()[0])) return true;
            return false;
        }

        static void Report(Scene scene, MonoBehaviour mb, string fieldName, List<MissingBinding> missing)
        {
            missing.Add(new MissingBinding
            {
                Scene = scene.name,
                Path = HierarchyPath(mb.transform),
                Component = mb.GetType().Name,
                Field = fieldName,
            });
        }

        static string HierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        static void WriteReport(List<MissingBinding> missing, List<string> scenes, string error)
        {
            var sb = new StringBuilder();
            sb.Append("{\"status\":").Append(Quote(error == null ? "complete" : "error"))
              .Append(",\"timestampUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")))
              .Append(",\"scenes\":[");
            for (int i = 0; i < scenes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Quote(scenes[i]));
            }
            sb.Append("],\"missingCount\":").Append(missing.Count);
            if (error != null)
                sb.Append(",\"error\":").Append(Quote(error));
            sb.Append(",\"missing\":[");
            for (int i = 0; i < missing.Count; i++)
            {
                if (i > 0) sb.Append(',');
                MissingBinding m = missing[i];
                sb.Append("{\"scene\":").Append(Quote(m.Scene))
                  .Append(",\"path\":").Append(Quote(m.Path))
                  .Append(",\"component\":").Append(Quote(m.Component))
                  .Append(",\"field\":").Append(Quote(m.Field))
                  .Append('}');
            }
            sb.Append("]}");
            File.WriteAllText(ReportPath, sb.ToString());
        }

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
