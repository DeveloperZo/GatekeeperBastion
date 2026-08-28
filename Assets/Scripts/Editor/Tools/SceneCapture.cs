using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>
    /// File-bridge still-shot renderer: the visual counterpart of the IntegrationCheck test
    /// bridge, for the polish phase's before/after evidence. When
    /// <projectRoot>/gkb_capture_request.txt exists (consumed by IntegrationCheck's sentinel
    /// poll), opens the requested scene in EDIT mode, renders each requested viewpoint through
    /// a temporary camera, writes PNGs to <projectRoot>/Captures/ (gitignored output, one
    /// destination), restores the previously open scene, and writes
    /// <projectRoot>/gkb_capture_report.json.
    ///
    /// Edit-mode renders only: runtime-built UI (HUD) is NOT in these shots — this is for
    /// geometry, materials and lighting. Refuses to run over an unsaved scene rather than
    /// risk a save prompt.
    ///
    /// Request format, one directive per line ('#' comments, blanks ignored):
    ///   scene=Assets/Scenes/Bastion_Coop.unity
    ///   shot=name|x,y,z|yaw,pitch|fov      (fov optional, default 60)
    /// </summary>
    public static class SceneCapture
    {
        const int Width = 1600;
        const int Height = 900;

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        internal static string RequestPath => Path.Combine(Root, "gkb_capture_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_capture_report.json");
        static string OutDir => Path.Combine(Root, "Captures");

        /// <summary>Run a consumed request's text. Called by IntegrationCheck's poll after it
        /// deletes the sentinel; never enters play mode, never touches an unsaved scene.</summary>
        internal static void Run(string requestText)
        {
            var shots = new List<(string name, Vector3 pos, float yaw, float pitch, float fov)>();
            string scenePath = null;
            try
            {
                foreach (var raw in requestText.Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    if (line.StartsWith("scene=")) { scenePath = line.Substring(6).Trim(); continue; }
                    if (!line.StartsWith("shot=")) continue;
                    var parts = line.Substring(5).Split('|');
                    var xyz = parts[1].Split(',');
                    var yp = parts[2].Split(',');
                    shots.Add((parts[0].Trim(),
                        new Vector3(Parse(xyz[0]), Parse(xyz[1]), Parse(xyz[2])),
                        Parse(yp[0]), Parse(yp[1]),
                        parts.Length > 3 ? Parse(parts[3]) : 60f));
                }
            }
            catch (Exception e)
            {
                WriteReport("failed", null, null, "bad request line: " + e.Message);
                return;
            }
            if (scenePath == null || shots.Count == 0)
            {
                WriteReport("failed", scenePath, null, "request needs scene= and at least one shot=");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty)
            {
                WriteReport("refused", scenePath, null,
                    "active scene has unsaved changes — save or discard in the editor first");
                return;
            }
            string restore = active.path;
            var written = new List<string>();
            try
            {
                if (active.path != scenePath)
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Directory.CreateDirectory(OutDir);
                foreach (var s in shots)
                    written.Add(Render(s.name, s.pos, s.yaw, s.pitch, s.fov));
                WriteReport("complete", scenePath, written, null);
            }
            catch (Exception e)
            {
                WriteReport("failed", scenePath, written, e.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(restore) && restore != scenePath)
                    EditorSceneManager.OpenScene(restore, OpenSceneMode.Single);
            }
        }

        static string Render(string name, Vector3 pos, float yaw, float pitch, float fov)
        {
            var go = new GameObject("SceneCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(Width, Height, 24);
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 500f;
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.targetTexture = rt;
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    var req = new RenderPipeline.StandardRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(cam, req))
                        RenderPipeline.SubmitRenderRequest(cam, req);
                    else
                        cam.Render();
                }
                else
                    cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                string file = Path.Combine(OutDir, Sanitize(name) + ".png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                return file;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static float Parse(string s)
            => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture);

        static string Sanitize(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.Length > 0 ? sb.ToString() : "shot";
        }

        static void WriteReport(string status, string scene, List<string> files, string error)
        {
            var sb = new StringBuilder();
            sb.Append("{\"status\":\"").Append(status).Append('"');
            if (scene != null) sb.Append(",\"scene\":\"").Append(scene.Replace("\\", "/")).Append('"');
            if (files != null)
            {
                sb.Append(",\"files\":[");
                for (int i = 0; i < files.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(files[i].Replace("\\", "/")).Append('"');
                }
                sb.Append(']');
            }
            if (error != null) sb.Append(",\"error\":\"").Append(error.Replace("\"", "'")).Append('"');
            sb.Append('}');
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log("[SceneCapture] " + status + (error != null ? " — " + error : ""));
        }
    }
}
