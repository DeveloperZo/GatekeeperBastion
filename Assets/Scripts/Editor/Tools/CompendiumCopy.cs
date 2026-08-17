using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Copies Docs/Play/*.md into Assets/StreamingAssets/Compendium for player builds.
    /// Strips HTML comments and ## Gaps; keeps Game + Reference + See also so the reader
    /// can select the Game dimension at runtime. Does not rewrite voice.</summary>
    public static class CompendiumCopy
    {
        const string StreamingRel = "Assets/StreamingAssets/Compendium";

        static readonly Regex HtmlComment = new Regex(
            @"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex GapsHeading = new Regex(
            @"^##\s+Gaps\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex H2Heading = new Regex(
            @"^##\s+", RegexOptions.Multiline | RegexOptions.Compiled);

        [MenuItem("GatekeepersProtocol/Build/Copy Compendium Pages", false, 64)]
        public static void CopyFromMenu()
        {
            int n = CopyPages(log: true);
            EditorUtility.DisplayDialog(
                "Compendium",
                n + " page(s) copied to " + StreamingRel + ".",
                "OK");
        }

        /// <summary>Player-build pipeline entry. Returns pages written.</summary>
        public static int CopyPages(bool log = false)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string source = Path.Combine(projectRoot, "Docs", "Play");
            string dest = Path.Combine(Application.dataPath, "StreamingAssets", "Compendium");

            if (!Directory.Exists(source))
            {
                Debug.LogError("[CompendiumCopy] Source missing: " + source);
                return 0;
            }

            Directory.CreateDirectory(dest);

            // Remove stale stems so the StreamingAssets set matches Docs/Play.
            foreach (string existing in Directory.GetFiles(dest, "*.md"))
            {
                string name = Path.GetFileName(existing);
                if (!File.Exists(Path.Combine(source, name)))
                    File.Delete(existing);
            }

            int written = 0;
            foreach (string srcPath in Directory.GetFiles(source, "*.md"))
            {
                string name = Path.GetFileName(srcPath);
                string raw = File.ReadAllText(srcPath);
                string cleaned = StripForPlayer(raw);
                string outPath = Path.Combine(dest, name);
                File.WriteAllText(outPath, cleaned);
                written++;
            }

            AssetDatabase.Refresh();
            if (log)
                Debug.Log("[CompendiumCopy] Copied " + written + " page(s) → " + StreamingRel);
            return written;
        }

        /// <summary>Same strip as <c>CompendiumCatalog.StripBuilderAudit</c>.</summary>
        public static string StripForPlayer(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return "";
            string s = HtmlComment.Replace(markdown, "");
            var match = GapsHeading.Match(s);
            if (!match.Success) return s.Trim();

            int start = match.Index;
            int searchFrom = match.Index + match.Length;
            int next = -1;
            var h2 = H2Heading.Match(s, searchFrom);
            if (h2.Success) next = h2.Index;
            if (next < 0) next = s.Length;
            s = s.Remove(start, next - start);
            return s.Trim();
        }

        /// <summary>Editor Play Mode / domain reload: copy only when a Play page is newer.</summary>
        [InitializeOnLoadMethod]
        static void WatchPlayPages()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (!SourceNewerThanCopy()) return;
            CopyPages(log: true);
        }

        static bool SourceNewerThanCopy()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string source = Path.Combine(projectRoot, "Docs", "Play");
            string dest = Path.Combine(Application.dataPath, "StreamingAssets", "Compendium");
            if (!Directory.Exists(source)) return false;
            if (!Directory.Exists(dest)) return true;

            DateTime newestSrc = DateTime.MinValue;
            foreach (string f in Directory.GetFiles(source, "*.md"))
            {
                var t = File.GetLastWriteTimeUtc(f);
                if (t > newestSrc) newestSrc = t;
            }

            DateTime newestDest = DateTime.MinValue;
            foreach (string f in Directory.GetFiles(dest, "*.md"))
            {
                var t = File.GetLastWriteTimeUtc(f);
                if (t > newestDest) newestDest = t;
            }

            return newestSrc > newestDest;
        }
    }
}
