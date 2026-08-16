using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GatekeeperBastion.Game;
using NUnit.Framework;

namespace GatekeeperBastion.Tests
{
    /// <summary>M62: Compendium form — Game dimension, See also, StreamingAssets stem set.</summary>
    public class CompendiumFormTests
    {
        static readonly Regex SeeAlso = new Regex(
            @"^##\s+See also\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex GameHeading = new Regex(
            @"^##\s+Game\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex MilestoneId = new Regex(
            @"\bM[0-9]{1,3}\b", RegexOptions.Compiled);
        static readonly Regex PageAnswersTable = new Regex(
            @"\|\s*Page\s*\|\s*Answers\s*\|", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static string ProjectPlayFolder()
        {
            string fromData = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath, "..", "Docs", "Play"));
            Assert.IsTrue(Directory.Exists(fromData), "Docs/Play missing at " + fromData);
            return fromData;
        }

        static string ProjectStreamingFolder()
        {
            return Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath, "StreamingAssets", "Compendium"));
        }

        [Test]
        public void HandWrittenPages_HaveGameAndSeeAlso()
        {
            string folder = ProjectPlayFolder();
            var missingGame = new List<string>();
            var missingSee = new List<string>();
            foreach (string path in Directory.GetFiles(folder, "*.md"))
            {
                string name = Path.GetFileName(path);
                if (string.Equals(name, "README.md", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(name, "Field-Manual.md", StringComparison.OrdinalIgnoreCase)) continue;

                string body = File.ReadAllText(path);
                if (!GameHeading.IsMatch(body)) missingGame.Add(name);
                if (!SeeAlso.IsMatch(body)) missingSee.Add(name);
            }

            Assert.IsEmpty(missingGame, "Missing ## Game: " + string.Join(", ", missingGame));
            Assert.IsEmpty(missingSee, "Missing ## See also: " + string.Join(", ", missingSee));
        }

        [Test]
        public void Readme_IsVolumeFrontMatter_NotPageAnswersTable()
        {
            string path = Path.Combine(ProjectPlayFolder(), "README.md");
            Assert.IsTrue(File.Exists(path), "README.md missing");
            string body = File.ReadAllText(path);
            Assert.IsFalse(PageAnswersTable.IsMatch(body),
                "README must not contain a | Page | Answers | table.");
            StringAssert.Contains("## I. The Situation", body);
            StringAssert.Contains("## Appendix", body);
        }

        [Test]
        public void GameDimension_ContainsNoMilestoneIds()
        {
            string folder = ProjectPlayFolder();
            var hits = new List<string>();
            foreach (string path in Directory.GetFiles(folder, "*.md"))
            {
                string name = Path.GetFileName(path);
                if (string.Equals(name, "Field-Manual.md", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(name, "README.md", StringComparison.OrdinalIgnoreCase))
                    continue;

                string visible = CompendiumCatalog.StripForPlayer(File.ReadAllText(path));
                if (MilestoneId.IsMatch(visible))
                    hits.Add(name);
            }

            Assert.IsEmpty(hits, "Milestone IDs in Game view: " + string.Join(", ", hits));
        }

        [Test]
        public void CatalogVolumes_CoverEveryPlayPageExceptReadme()
        {
            string folder = ProjectPlayFolder();
            var volumeStems = CompendiumCatalog.VolumeStemSet();
            var missing = new List<string>();
            var extras = new List<string>();

            foreach (string path in Directory.GetFiles(folder, "*.md"))
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(stem, "README", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(stem, "Field-Manual", StringComparison.OrdinalIgnoreCase)) continue;
                if (!volumeStems.Contains(stem))
                    missing.Add(stem);
            }

            foreach (string stem in volumeStems)
            {
                string path = Path.Combine(folder, stem + ".md");
                if (!File.Exists(path))
                    extras.Add(stem);
            }

            Assert.IsEmpty(missing, "Play pages not in any volume: " + string.Join(", ", missing));
            Assert.IsEmpty(extras, "Volume stems with no Play page: " + string.Join(", ", extras));
        }

        [Test]
        public void PauseVolumes_OmitFieldManual()
        {
            Assert.IsFalse(
                CompendiumCatalog.VolumeStemSet().Contains("Field-Manual"),
                "Field Manual is Docs/Play only; pause has no table layout.");
        }

        [Test]
        public void StreamingAssets_StemSetMatchesPlay()
        {
            string play = ProjectPlayFolder();
            string stream = ProjectStreamingFolder();
            Assert.IsTrue(Directory.Exists(stream),
                "StreamingAssets/Compendium missing — run GatekeeperBastion/Build/Copy Compendium Pages");

            var playStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(play, "*.md"))
                playStems.Add(Path.GetFileNameWithoutExtension(path));

            var streamStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(stream, "*.md"))
                streamStems.Add(Path.GetFileNameWithoutExtension(path));

            var onlyPlay = new List<string>();
            foreach (string s in playStems)
                if (!streamStems.Contains(s)) onlyPlay.Add(s);
            var onlyStream = new List<string>();
            foreach (string s in streamStems)
                if (!playStems.Contains(s)) onlyStream.Add(s);

            Assert.IsEmpty(onlyPlay, "In Play but not StreamingAssets: " + string.Join(", ", onlyPlay));
            Assert.IsEmpty(onlyStream, "In StreamingAssets but not Play: " + string.Join(", ", onlyStream));
        }

        [Test]
        public void ExtractGameView_OmitsReferenceAndGaps()
        {
            const string sample =
                "<!-- builder -->\n# Ram\n\n## Game\n\nGame body.\n\n## Reference\n\nLonger.\n\n## See also\n\n[A](A.md).\n\n## Gaps\n\n- secret\n";
            string stripped = CompendiumCatalog.StripBuilderAudit(sample);
            Assert.IsFalse(stripped.Contains("builder"));
            Assert.IsFalse(stripped.Contains("## Gaps"));
            Assert.IsFalse(stripped.Contains("secret"));
            StringAssert.Contains("## Reference", stripped);

            string game = CompendiumCatalog.ExtractGameView(stripped, "Ram");
            StringAssert.Contains("Game body", game);
            StringAssert.Contains("## See also", game);
            Assert.IsFalse(game.Contains("## Reference"), game);
            Assert.IsFalse(game.Contains("Longer"), game);
        }
    }
}
