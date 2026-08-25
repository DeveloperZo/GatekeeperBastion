using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for the rule the visibility matrix proves at the view layer:
    /// the protocol chrome is ALWAYS visible while the fight HUD is up — an empty loadout
    /// changes what the bar and line SAY, never whether they show. These read the source so
    /// the offline harness keeps the rule when the Unity-only matrix cannot run.</summary>
    public class ProtocolHudSourceTests
    {
        const string ViewPath = "Assets/Scripts/Game/Ui/BastionHudView.cs";
        const string FightPath = "Assets/Scripts/Game/Bastion/HudController.Fight.cs";

        [Test]
        public void TheView_TogglesTheBarOnlyWithTheFightChrome()
        {
            string src = Read(ViewPath);
            MatchCollection calls = Regex.Matches(src, @"_protocolBar\.SetVisible\(");
            Assert.AreEqual(1, calls.Count,
                "_protocolBar.SetVisible must have exactly one call site (ShowCornerCards) — "
                + "a second one gives the bar a visibility rule of its own");
            int cards = src.IndexOf("void ShowCornerCards", System.StringComparison.Ordinal);
            int next = src.IndexOf("public void", cards, System.StringComparison.Ordinal);
            Assert.IsTrue(cards >= 0 && calls[0].Index > cards && calls[0].Index < next,
                "_protocolBar.SetVisible must be called from ShowCornerCards");
            StringAssert.Contains("UiBind.Show(_protocolLine, true)", src,
                "SetProtocolLine must always show the line — content changes, visibility does not");
        }

        [Test]
        public void TheController_PushesPlaceholders_WhenNoProtocolIsEquipped()
        {
            string src = Read(FightPath);
            int branch = src.IndexOf("if (!equipped)", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(branch, 0,
                "PushProtocolChrome has lost its unequipped branch");
            int close = src.IndexOf("return;", branch, System.StringComparison.Ordinal);
            string block = src.Substring(branch, close - branch);
            StringAssert.Contains("SetProtocolLine(\"NO PROTOCOL\")", block,
                "the empty loadout must read NO PROTOCOL");
            StringAssert.Contains("SetProtocolAbilities(", block,
                "the empty loadout must push placeholder slots, not skip the bar");
            Assert.IsFalse(src.Contains("_protocolBar"),
                "HudController must not reach for the bar's visibility — that rule lives in "
                + "the view's ShowCornerCards alone");
        }

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }
    }
}
