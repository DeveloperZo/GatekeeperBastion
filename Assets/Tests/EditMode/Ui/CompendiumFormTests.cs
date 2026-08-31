using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
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
        public void StreamingAssets_StemSetMatchesPlaysInBuildSet()
        {
            // Not the raw Play set: a page marked `in_build: false` is authored-but-not-shipped,
            // so it must be ABSENT from StreamingAssets while its source stays in Docs/Play.
            // (Was StreamingAssets_StemSetMatchesPlay until the in_build flag existed.)
            string play = ProjectPlayFolder();
            string stream = ProjectStreamingFolder();
            Assert.IsTrue(Directory.Exists(stream),
                "StreamingAssets/Compendium missing — run GatekeepersProtocol/Build/Copy Compendium Pages");

            var playStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pulledStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(play, "*.md"))
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                if (CompendiumCatalog.MarkdownIsInBuild(File.ReadAllText(path)))
                    playStems.Add(stem);
                else
                    pulledStems.Add(stem);
            }

            var streamStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(stream, "*.md"))
                streamStems.Add(Path.GetFileNameWithoutExtension(path));

            var onlyPlay = new List<string>();
            foreach (string s in playStems)
                if (!streamStems.Contains(s)) onlyPlay.Add(s);
            var onlyStream = new List<string>();
            foreach (string s in streamStems)
                if (!playStems.Contains(s)) onlyStream.Add(s);
            var shippedPulled = new List<string>();
            foreach (string s in pulledStems)
                if (streamStems.Contains(s)) shippedPulled.Add(s);

            Assert.IsEmpty(onlyPlay, "In Play but not StreamingAssets: " + string.Join(", ", onlyPlay));
            Assert.IsEmpty(onlyStream, "In StreamingAssets but not Play: " + string.Join(", ", onlyStream));
            Assert.IsEmpty(shippedPulled,
                "Marked in_build: false but still shipped: " + string.Join(", ", shippedPulled));
        }

        [Test]
        public void MarkdownIsInBuild_MissingMeansTrue_FalseMeansPulled()
        {
            // The one parser (CompendiumCatalog.MarkdownIsInBuild) the copier, the pause nav
            // and the stem-set test all share. Missing means true: every page written before
            // the flag existed ships.
            Assert.IsTrue(CompendiumCatalog.MarkdownIsInBuild("# Page\n\nNo comment at all.\n"));
            Assert.IsTrue(CompendiumCatalog.MarkdownIsInBuild(
                "<!-- Purpose: x / Status: working -->\n# Page\n"), "comment without the flag");
            Assert.IsTrue(CompendiumCatalog.MarkdownIsInBuild(
                "<!-- Purpose: x / in_build: true -->\n# Page\n"));
            Assert.IsFalse(CompendiumCatalog.MarkdownIsInBuild(
                "<!-- Purpose: x / in_build: false -->\n# Page\n"));
            Assert.IsFalse(CompendiumCatalog.MarkdownIsInBuild(
                "<!-- in_build: FALSE -->\n# Page\n"), "case-insensitive");
            Assert.IsTrue(CompendiumCatalog.MarkdownIsInBuild(
                "# Page\n\nBody text saying in_build: false outside any comment.\n<!-- late comment -->"),
                "only the opening comment carries the flag");
            Assert.IsTrue(CompendiumCatalog.MarkdownIsInBuild(""), "empty page defaults in");
        }

        [Test]
        public void MarkdownIsComingSoon_MissingMeansFalse_AndTitleMarkerAgreesWithFlag()
        {
            // The one parser (CompendiumCatalog.MarkdownIsComingSoon) the pause nav's filter
            // and this test share. Missing means false: an ordinary page is not a teaser.
            Assert.IsFalse(CompendiumCatalog.MarkdownIsComingSoon("# Page\n\nNo comment.\n"));
            Assert.IsFalse(CompendiumCatalog.MarkdownIsComingSoon(
                "<!-- Purpose: x / Status: working -->\n# Page\n"), "comment without the flag");
            Assert.IsTrue(CompendiumCatalog.MarkdownIsComingSoon(
                "<!-- Purpose: x / coming_soon: true -->\n# Page\n"));
            Assert.IsFalse(CompendiumCatalog.MarkdownIsComingSoon(
                "<!-- Purpose: x / coming_soon: false -->\n# Page\n"));
            Assert.IsTrue(CompendiumCatalog.MarkdownIsComingSoon(
                "<!-- coming_soon: TRUE -->\n# Page\n"), "case-insensitive");

            // The human marker (— Coming Soon in the title) and the machine flag must not
            // drift: a page with one and not the other either hides from the filter or
            // reads as shipped when it is not.
            var firstH1 = new Regex(@"^#\s+(.+)$", RegexOptions.Multiline);
            string folder = ProjectPlayFolder();
            var mismatched = new List<string>();
            foreach (string path in Directory.GetFiles(folder, "*.md"))
            {
                string name = Path.GetFileName(path);
                if (string.Equals(name, "README.md", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(name, "Field-Manual.md", StringComparison.OrdinalIgnoreCase)) continue;

                string body = File.ReadAllText(path);
                bool flagged = CompendiumCatalog.MarkdownIsComingSoon(body);
                var h1 = firstH1.Match(body);
                bool titled = h1.Success && h1.Groups[1].Value.IndexOf(
                    "Coming Soon", StringComparison.OrdinalIgnoreCase) >= 0;
                if (flagged != titled)
                    mismatched.Add(name + (flagged ? " (flag, no title marker)" : " (title marker, no flag)"));
            }
            Assert.IsEmpty(mismatched,
                "coming_soon flag and title marker disagree: " + string.Join(", ", mismatched));
        }

        [Test]
        public void ExtractGameView_OmitsReferenceAndGaps()
        {
            const string sample =
                "<!-- builder -->\n# Thief\n\n## Game\n\nGame body.\n\n## Reference\n\nLonger.\n\n## See also\n\n[A](A.md).\n\n## Gaps\n\n- secret\n";
            string stripped = CompendiumCatalog.StripBuilderAudit(sample);
            Assert.IsFalse(stripped.Contains("builder"));
            Assert.IsFalse(stripped.Contains("## Gaps"));
            Assert.IsFalse(stripped.Contains("secret"));
            StringAssert.Contains("## Reference", stripped);

            string game = CompendiumCatalog.ExtractGameView(stripped, "Thief");
            StringAssert.Contains("Game body", game);
            StringAssert.Contains("## See also", game);
            Assert.IsFalse(game.Contains("## Reference"), game);
            Assert.IsFalse(game.Contains("Longer"), game);
        }

        [Test]
        public void ExtractWalkthroughView_IncludesReferenceBody_DropsReferenceHeading()
        {
            const string sample =
                "# First Time Out\n\n## Game\n\nI start at the Hatch.\n\n## Reference\n\n### On the Ship\n\nBuy the MK-1.\n\n## See also\n\n[The Game](The-Game.md).\n\n## Gaps\n\n- secret\n";
            string stripped = CompendiumCatalog.StripBuilderAudit(sample);
            string walk = CompendiumCatalog.ExtractWalkthroughView(stripped, "First Time Out");
            StringAssert.Contains("I start at the Hatch.", walk);
            StringAssert.Contains("### On the Ship", walk);
            StringAssert.Contains("Buy the MK-1.", walk);
            StringAssert.Contains("## See also", walk);
            Assert.IsFalse(walk.Contains("## Reference"), walk);
            Assert.IsFalse(walk.Contains("secret"), walk);
            Assert.Greater(walk.IndexOf("On the Ship", StringComparison.Ordinal),
                walk.IndexOf("I start at the Hatch.", StringComparison.Ordinal));
            Assert.Greater(walk.IndexOf("See also", StringComparison.Ordinal),
                walk.IndexOf("On the Ship", StringComparison.Ordinal));
        }

        [Test]
        public void GettingStarted_PlayerView_IsTheWalkthroughAndTheDefaultOpen()
        {
            Assert.AreEqual("Getting-Started", CompendiumCatalog.DefaultOpenStem);
            Assert.AreEqual("Getting-Started", CompendiumCatalog.Volumes[0].EntryStems[0]);
            Assert.IsTrue(CompendiumCatalog.IsWalkthrough("Getting-Started"));
            Assert.IsFalse(CompendiumCatalog.IsWalkthrough("The-Game"));

            Assert.IsTrue(CompendiumCatalog.TryLoadEntry("Getting-Started", out var entry),
                "Getting-Started must load from StreamingAssets (or Docs/Play).");
            StringAssert.Contains("On the Ship", entry.BodyMarkdown);
            StringAssert.Contains("Master Control", entry.BodyMarkdown);
            StringAssert.Contains("BASTION STRIPPED", entry.BodyMarkdown);
            Assert.IsFalse(entry.BodyMarkdown.Contains("## Reference"), entry.BodyMarkdown);
            Assert.IsFalse(entry.BodyMarkdown.Contains("## Gaps"), entry.BodyMarkdown);
        }

        [Test]
        public void ToTmpRichText_RendersCodeSpansAsBold()
        {
            string tmp = CompendiumCatalog.ToTmpRichText("Hold `E`, then `T`.");
            StringAssert.Contains("<b>E</b>", tmp);
            StringAssert.Contains("<b>T</b>", tmp);
            Assert.IsFalse(tmp.Contains("`"), tmp);
        }
    }
}
