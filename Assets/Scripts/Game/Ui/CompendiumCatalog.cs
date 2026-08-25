using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: volumes and entry stems for the pause Compendium reader.
    /// Runtime loads pages from StreamingAssets; EditMode tests read Docs/Play directly.</summary>
    public static class CompendiumCatalog
    {
        public const string FieldManualStem = "Field-Manual";
        public const string StreamingRelative = "Compendium";
        public const string DocsRelative = "Docs/Play";

        public struct Volume
        {
            public string Title;
            public string[] EntryStems;
        }

        public struct Entry
        {
            public string Stem;
            public string Title;
            public string BodyMarkdown;
        }

        /// <summary>The pause volumes, in the order Docs/Play/README.md states them — that
        /// file is the structure of record and this array mirrors it. Field Manual stays in
        /// Docs/Play; the reader has no table layout and does not open it. Coming Soon pages
        /// carry the marker in their own titles and ship like any other entry.</summary>
        public static readonly Volume[] Volumes =
        {
            new Volume
            {
                Title = "I. The Situation",
                EntryStems = new[] { "The-Game", "Getting-Started" }
            },
            new Volume
            {
                Title = "II. A Boarding",
                EntryStems = new[]
                {
                    "A-Boarding", "Waves", "Ante", "Salvage", "Experience-And-Cores",
                    "Going-Down", "Staying-Alive"
                }
            },
            new Volume
            {
                Title = "III. The Bastion",
                EntryStems = new[] { "The-Hatch", "Destinations" }
            },
            new Volume
            {
                Title = "IV. The Garrison",
                EntryStems = new[]
                {
                    "Gatekeepers", "Gatekeeper-Default", "Gatekeeper-Hunter", "Gatekeeper-Thief",
                    "Gatekeeper-Cloaker", "Gatekeeper-Source", "Gatekeeper-Enabler",
                    "Gatekeeper-Alarmer", "Gatekeeper-Bypasser", "Gatekeeper-Reclaimer",
                    "Gatekeeper-Exploder", "Gatekeeper-Standoff-Shooter",
                    "Gatekeeper-Ailment-Shooter"
                }
            },
            new Volume
            {
                Title = "V. Doctrine",
                EntryStems = new[]
                {
                    "Protocols", "Protocol-Default", "Protocol-VEIL", "Protocol-FORGE",
                    "Protocol-AEGIS", "Protocol-LEDGER"
                }
            },
            new Volume
            {
                Title = "VI. Arms",
                EntryStems = new[]
                {
                    "Weapons", "Weapon-MK-1-Sidearm", "Weapon-SEC-D", "Weapon-BR-12-Shotgun",
                    "Weapon-MR-35-Rifle", "Weapon-PRI-C", "The-Drone", "Drone-Payloads"
                }
            },
            new Volume
            {
                Title = "VII. The Hull",
                EntryStems = new[] { "The-Ship", "Upgrades", "The-Safehouse" }
            },
            new Volume
            {
                Title = "VIII. The Realm",
                EntryStems = new[] { "Realms", "The-Crew", "Shared-Realms" }
            }
        };

        static readonly Regex HtmlComment = new Regex(
            @"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex InBuildFlag = new Regex(
            @"\bin_build:\s*(true|false)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex ComingSoonFlag = new Regex(
            @"\bcoming_soon:\s*(true|false)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex MdLink = new Regex(
            @"\[([^\]]+)\]\(([^)]+\.md)\)", RegexOptions.Compiled);
        static readonly Regex Bold = new Regex(
            @"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        static readonly Regex GapsHeading = new Regex(
            @"^##\s+Gaps\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex GameHeading = new Regex(
            @"^##\s+Game\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex SeeAlsoHeading = new Regex(
            @"^##\s+See also\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex H2Heading = new Regex(
            @"^##\s+", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>Folder the reader opens at runtime (StreamingAssets/Compendium).</summary>
        public static string RuntimeFolder =>
            Path.Combine(Application.streamingAssetsPath, StreamingRelative);

        /// <summary>Docs/Play at the project root — editor and EditMode only.</summary>
        public static string DocsFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", DocsRelative));

        public static bool TryLoadEntry(string stem, out Entry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(stem)) return false;
            if (string.Equals(stem, FieldManualStem, StringComparison.OrdinalIgnoreCase))
                return false;

            string path = ResolvePagePath(stem);
            if (path == null || !File.Exists(path)) return false;

            string raw;
            try { raw = File.ReadAllText(path); }
            catch (Exception) { return false; }

            string cleaned = StripBuilderAudit(raw);
            string title = ExtractTitle(cleaned, stem);
            string gameView = ExtractGameView(cleaned, title);
            entry = new Entry
            {
                Stem = stem,
                Title = title,
                BodyMarkdown = gameView
            };
            return true;
        }

        /// <summary>Nav label for a stem — page title when loadable, else stem with dashes spaced.</summary>
        public static string DisplayTitle(string stem)
        {
            if (TryLoadEntry(stem, out var entry) && !string.IsNullOrEmpty(entry.Title))
                return entry.Title;
            return string.IsNullOrEmpty(stem) ? "" : stem.Replace('-', ' ');
        }

        /// <summary>Every Play page stem that belongs in a volume (excludes README).</summary>
        public static HashSet<string> VolumeStemSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int v = 0; v < Volumes.Length; v++)
            {
                var stems = Volumes[v].EntryStems;
                for (int i = 0; i < stems.Length; i++)
                    set.Add(stems[i]);
            }
            return set;
        }

        public static string ResolvePagePath(string stem)
        {
            string file = stem.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                ? stem
                : stem + ".md";

            string runtime = Path.Combine(RuntimeFolder, file);
            if (File.Exists(runtime)) return runtime;

#if UNITY_EDITOR
            string docs = Path.Combine(DocsFolder, file);
            if (File.Exists(docs)) return docs;
#endif
            return File.Exists(runtime) ? runtime : null;
        }

        /// <summary>The `in_build` flag from a page's opening HTML comment; MISSING MEANS TRUE —
        /// every page written before the flag existed ships. The one parser for the flag: the
        /// build copier, the pause reader and the form tests all call here, so they cannot
        /// drift on what "in build" means.</summary>
        public static bool MarkdownIsInBuild(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return true;
            var comment = HtmlComment.Match(markdown);
            if (!comment.Success) return true;
            var flag = InBuildFlag.Match(comment.Value);
            if (!flag.Success) return true;
            return !string.Equals(flag.Groups[1].Value, "false", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Whether the page behind a stem is in the build. An unresolvable page reads
        /// true — absence is a different failure with its own reporting, not a filter.</summary>
        public static bool IsInBuild(string stem)
        {
            string path = ResolvePagePath(stem);
            if (path == null || !File.Exists(path)) return true;
            try { return MarkdownIsInBuild(File.ReadAllText(path)); }
            catch (Exception) { return true; }
        }

        /// <summary>The `coming_soon` flag from a page's opening HTML comment; MISSING MEANS
        /// FALSE — an ordinary page is not a teaser. The one parser for the flag: the pause
        /// nav's optional filter and the form tests both call here, so the marker in a page's
        /// title and the flag cannot drift apart unnoticed.</summary>
        public static bool MarkdownIsComingSoon(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return false;
            var comment = HtmlComment.Match(markdown);
            if (!comment.Success) return false;
            var flag = ComingSoonFlag.Match(comment.Value);
            if (!flag.Success) return false;
            return string.Equals(flag.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Whether the page behind a stem is a Coming Soon teaser. An unresolvable
        /// page reads false — absence is its own failure, not a teaser.</summary>
        public static bool IsComingSoon(string stem)
        {
            string path = ResolvePagePath(stem);
            if (path == null || !File.Exists(path)) return false;
            try { return MarkdownIsComingSoon(File.ReadAllText(path)); }
            catch (Exception) { return false; }
        }

        /// <summary>Strip HTML comments and ## Gaps. Keeps Game, Reference, See also.</summary>
        public static string StripBuilderAudit(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return "";
            string s = HtmlComment.Replace(markdown, "");
            var match = GapsHeading.Match(s);
            if (!match.Success) return s.Trim();

            int start = match.Index;
            int searchFrom = match.Index + match.Length;
            int next = IndexOfNextH2(s, searchFrom);
            if (next < 0) next = s.Length;
            s = s.Remove(start, next - start);
            return s.Trim();
        }

        /// <summary>Deprecated name — prefer <see cref="StripBuilderAudit"/> then
        /// <see cref="ExtractGameView"/>.</summary>
        public static string StripForPlayer(string markdown) =>
            ExtractGameView(StripBuilderAudit(markdown), ExtractTitle(markdown, "Entry"));

        /// <summary>In-game dimension: title + ## Game body + ## See also.
        /// Pages without ## Game return the full stripped markdown.</summary>
        public static string ExtractGameView(string markdown, string title)
        {
            if (string.IsNullOrEmpty(markdown)) return "";

            var game = GameHeading.Match(markdown);
            if (!game.Success)
                return markdown.Trim();

            int gameBodyStart = game.Index + game.Length;
            while (gameBodyStart < markdown.Length
                   && (markdown[gameBodyStart] == '\r' || markdown[gameBodyStart] == '\n'))
                gameBodyStart++;

            int gameBodyEnd = IndexOfNextH2(markdown, gameBodyStart);
            if (gameBodyEnd < 0) gameBodyEnd = markdown.Length;
            string gameBody = markdown.Substring(gameBodyStart, gameBodyEnd - gameBodyStart).Trim();

            string seeAlso = "";
            var see = SeeAlsoHeading.Match(markdown);
            if (see.Success)
            {
                int seeStart = see.Index;
                int seeEnd = IndexOfNextH2(markdown, see.Index + see.Length);
                if (seeEnd < 0) seeEnd = markdown.Length;
                seeAlso = markdown.Substring(seeStart, seeEnd - seeStart).Trim();
            }

            var sb = new StringBuilder(gameBody.Length + seeAlso.Length + 64);
            if (!string.IsNullOrEmpty(title))
            {
                sb.Append("# ");
                sb.Append(title);
                sb.Append("\n\n");
            }
            sb.Append(gameBody);
            if (!string.IsNullOrEmpty(seeAlso))
            {
                sb.Append("\n\n");
                sb.Append(seeAlso);
            }
            return sb.ToString().Trim();
        }

        static int IndexOfNextH2(string s, int from)
        {
            if (from < 0 || from >= s.Length) return -1;
            var m = H2Heading.Match(s, from);
            return m.Success ? m.Index : -1;
        }

        /// <summary>Convert the supported markdown subset to TMP rich text.</summary>
        public static string ToTmpRichText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return "";

            var sb = new StringBuilder(markdown.Length + 64);
            using (var reader = new StringReader(markdown))
            {
                string line;
                bool first = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!first) sb.Append('\n');
                    first = false;
                    AppendLineAsTmp(sb, line);
                }
            }
            return sb.ToString();
        }

        /// <summary>Stem from a markdown link target like Gatekeeper-Thief.md.</summary>
        public static string StemFromLinkTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) return null;
            string name = Path.GetFileName(target.Replace('\\', '/'));
            if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - 3);
            return name;
        }

        public static IEnumerable<string> AllShippedStems()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int v = 0; v < Volumes.Length; v++)
            {
                var stems = Volumes[v].EntryStems;
                for (int i = 0; i < stems.Length; i++)
                {
                    if (seen.Add(stems[i]))
                        yield return stems[i];
                }
            }
            if (seen.Add("README"))
                yield return "README";
        }

        static void AppendLineAsTmp(StringBuilder sb, string line)
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                sb.Append("<size=136%><b>");
                sb.Append(Inline(line.Substring(2).Trim()));
                sb.Append("</b></size>");
                return;
            }
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                sb.Append("<size=118%><b>");
                sb.Append(Inline(line.Substring(3).Trim()));
                sb.Append("</b></size>");
                return;
            }
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                sb.Append("<b>");
                sb.Append(Inline(line.Substring(4).Trim()));
                sb.Append("</b>");
                return;
            }
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                sb.Append("• ");
                sb.Append(Inline(line.Substring(2)));
                return;
            }
            sb.Append(Inline(line));
        }

        static string Inline(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string s = Bold.Replace(text, "<b>$1</b>");
            s = MdLink.Replace(s, m =>
            {
                string label = m.Groups[1].Value;
                string stem = StemFromLinkTarget(m.Groups[2].Value);
                if (string.IsNullOrEmpty(stem)) return label;
                if (string.Equals(stem, FieldManualStem, StringComparison.OrdinalIgnoreCase))
                    return label;
                return "<link=\"" + stem + "\"><u>" + label + "</u></link>";
            });
            return s;
        }

        static string ExtractTitle(string markdown, string fallbackStem)
        {
            using (var reader = new StringReader(markdown))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("# ", StringComparison.Ordinal))
                        return line.Substring(2).Trim();
                }
            }
            return fallbackStem.Replace('-', ' ');
        }
    }
}
