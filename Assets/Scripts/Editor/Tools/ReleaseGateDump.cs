using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>
    /// File bridge for the release gate. Agents write
    /// <c>gkb_release_gate_request.txt</c> at the project root; this dumps PlayerSettings
    /// and Windows build-profile flags to <c>gkb_release_gate.json</c>. No menu row —
    /// <c>Tools/release-gate/run.sh</c> reads the YAML assets directly and does not need
    /// this dump; the JSON is for inspection when the editor is open.
    /// </summary>
    [InitializeOnLoad]
    public static class ReleaseGateDump
    {
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_release_gate_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_release_gate.json");
        const string WindowsProfilePath = "Assets/Settings/Build Profiles/Windows.asset";

        static double _nextPoll;

        static ReleaseGateDump()
        {
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(SentinelPath)) return;

            try { File.Delete(SentinelPath); }
            catch (Exception e)
            {
                Debug.LogWarning("[ReleaseGateDump] Could not delete sentinel: " + e.Message);
                return;
            }

            try { WriteReport(); }
            catch (Exception e)
            {
                WriteError(e.GetType().Name + ": " + e.Message);
                Debug.LogError("[ReleaseGateDump] " + e.Message);
            }
        }

        static void WriteReport()
        {
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            var apiSb = new StringBuilder("[");
            for (int i = 0; i < apis.Length; i++)
            {
                if (i > 0) apiSb.Append(',');
                apiSb.Append(Quote(apis[i].ToString()));
            }
            apiSb.Append(']');

            bool autoGfx = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
            ReadBuildProfileFlags(out int development, out int connectProfiler,
                out int deepProfiling, out int allowDebugging);

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"status\":").Append(Quote("ok"));
            sb.Append(",\"timestampUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")));
            sb.Append(",\"unity\":").Append(Quote(Application.unityVersion));
            sb.Append(",\"companyName\":").Append(Quote(PlayerSettings.companyName));
            sb.Append(",\"productName\":").Append(Quote(PlayerSettings.productName));
            sb.Append(",\"bundleVersion\":").Append(Quote(PlayerSettings.bundleVersion));
            sb.Append(",\"fullscreenMode\":").Append((int)PlayerSettings.fullScreenMode);
            sb.Append(",\"defaultIsNativeResolution\":").Append(PlayerSettings.defaultIsNativeResolution ? 1 : 0);
            sb.Append(",\"resizableWindow\":").Append(PlayerSettings.resizableWindow ? 1 : 0);
            sb.Append(",\"allowFullscreenSwitch\":").Append(PlayerSettings.allowFullscreenSwitch ? 1 : 0);
            sb.Append(",\"runInBackground\":").Append(PlayerSettings.runInBackground ? 1 : 0);
            sb.Append(",\"useDefaultGraphicsAPIs\":").Append(autoGfx ? 1 : 0);
            sb.Append(",\"graphicsAPIs\":").Append(apiSb);
            sb.Append(",\"firstGraphicsAPI\":").Append(
                apis.Length > 0 ? Quote(apis[0].ToString()) : "null");
            sb.Append(",\"buildProfile\":{");
            sb.Append("\"path\":").Append(Quote(WindowsProfilePath));
            sb.Append(",\"m_Development\":").Append(development);
            sb.Append(",\"m_ConnectProfiler\":").Append(connectProfiler);
            sb.Append(",\"m_BuildWithDeepProfilingSupport\":").Append(deepProfiling);
            sb.Append(",\"m_AllowDebugging\":").Append(allowDebugging);
            sb.Append('}');
            sb.Append('}');

            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log("[ReleaseGateDump] Wrote " + ReportPath);
        }

        /// <summary>YAML scrape of the Windows Build Profile asset — same fields
        /// <c>Tools/release-gate/run.sh</c> reads. Avoids a Build Profile API dependency.</summary>
        static void ReadBuildProfileFlags(out int development, out int connectProfiler,
            out int deepProfiling, out int allowDebugging)
        {
            development = connectProfiler = deepProfiling = allowDebugging = -1;
            string abs = Path.Combine(Root, WindowsProfilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(abs)) return;

            foreach (string line in File.ReadAllLines(abs))
            {
                AssignInt(line, "m_Development:", ref development);
                AssignInt(line, "m_ConnectProfiler:", ref connectProfiler);
                AssignInt(line, "m_BuildWithDeepProfilingSupport:", ref deepProfiling);
                AssignInt(line, "m_AllowDebugging:", ref allowDebugging);
            }
        }

        static void AssignInt(string line, string key, ref int dest)
        {
            int i = line.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return;
            string rest = line.Substring(i + key.Length).Trim();
            if (int.TryParse(rest, out int v)) dest = v;
        }

        static void WriteError(string message)
        {
            string json = "{\"status\":\"error\",\"message\":" + Quote(message)
                          + ",\"timestampUtc\":" + Quote(DateTime.UtcNow.ToString("o")) + "}";
            try { File.WriteAllText(ReportPath, json); }
            catch (Exception e)
            {
                Debug.LogError("[ReleaseGateDump] Could not write report: " + e.Message);
            }
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
