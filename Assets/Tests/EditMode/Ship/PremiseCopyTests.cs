using System.Text.RegularExpressions;
using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M27 — player-facing premise strings stay non-empty, under the word cap, and
    /// free of the retired "dormant Gate" framing. Exact wording is not pinned.</summary>
    public class PremiseCopyTests
    {
        static readonly Regex BuilderVocab = new Regex(
            @"gatekeeper-seconds|arrivals\s*x\s*dwell|\brealization\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static readonly string[] RetiredFragments =
        {
            "dormant",
            "Gate signature",
            "the run is over",
        };

        [Test]
        public void NavMapPremise_StatesGarrisonAndHatch_UnderFortyWords()
        {
            string line = MapStation.PremiseBrief;
            Assert.IsFalse(string.IsNullOrWhiteSpace(line));
            Assert.LessOrEqual(WordCount(line), 40, line);
            AssertContainsInsensitive(line, "Gatekeepers");
            AssertContainsInsensitive(line, "garrisoned");
            AssertContainsInsensitive(line, "Bastion");
            AssertContainsInsensitive(line, "Hatch");
            AssertContainsInsensitive(line, "Ship");
            AssertNoRetiredOrBuilder(line);
            Assert.IsFalse(Regex.IsMatch(line, @"(?<![A-Za-z])Gate(?![A-Za-z])"),
                "standalone 'Gate' was removed in M27 — " + line);
        }

        [Test]
        public void EndScreens_StateWhoWon_WithoutRetiredFraming()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(HudController.LoseFraming));
            Assert.IsFalse(string.IsNullOrWhiteSpace(HudController.ClearFraming));
            AssertContainsInsensitive(HudController.LoseFraming, "thrown off");
            AssertContainsInsensitive(HudController.LoseFraming, "Bastion");
            AssertContainsInsensitive(HudController.ClearFraming, "Hatch");
            AssertNoRetiredOrBuilder(HudController.LoseFraming);
            AssertNoRetiredOrBuilder(HudController.ClearFraming);
        }

        static int WordCount(string s)
        {
            int n = 0;
            bool inWord = false;
            for (int i = 0; i < s.Length; i++)
            {
                bool letter = char.IsLetterOrDigit(s[i]);
                if (letter && !inWord) { n++; inWord = true; }
                else if (!letter) inWord = false;
            }
            return n;
        }

        static void AssertContainsInsensitive(string hay, string needle)
        {
            Assert.IsTrue(hay.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0,
                $"expected '{needle}' in '{hay}'");
        }

        static void AssertNoRetiredOrBuilder(string line)
        {
            for (int i = 0; i < RetiredFragments.Length; i++)
            {
                Assert.IsFalse(
                    line.IndexOf(RetiredFragments[i], System.StringComparison.OrdinalIgnoreCase) >= 0,
                    $"retired fragment '{RetiredFragments[i]}' in '{line}'");
            }
            Assert.IsFalse(BuilderVocab.IsMatch(line), "builder vocabulary in '" + line + "'");
        }
    }
}
