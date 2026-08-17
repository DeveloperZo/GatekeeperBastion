using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M59 chrome: station Settings leftovers inactive, scrim α, pause binding,
    /// end-screen scrim α, station header gutters.</summary>
    public class M59PolishChromeTests
    {
        static readonly Regex PauseBind = new Regex(
            @"HudController[\s\S]*?_pauseShortcuts: \{fileID: (\d+)\}",
            RegexOptions.Compiled);
        static readonly Regex ImageColorA = new Regex(
            @"m_Color: \{r: [-\d.]+, g: [-\d.]+, b: [-\d.]+, a: ([-\d.]+)\}",
            RegexOptions.Compiled);

        [Test]
        public void PauseShortcuts_AreBoundOnHudController()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion.unity"));
            Assert.IsTrue(yaml.Contains("m_Name: Pause Shortcuts"),
                "Pause Shortcuts GO missing under Bastion pause canvas");
            var m = PauseBind.Match(yaml);
            Assert.IsTrue(m.Success, "HudController._pauseShortcuts field missing");
            Assert.AreNotEqual("0", m.Groups[1].Value, "HudController._pauseShortcuts unbound");
        }

        [Test]
        public void StationCategoriesAndPreview_AreInactive()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship.unity"));
            AssertInactive(yaml, "Categories");
            AssertInactive(yaml, "Preview Image");
        }

        [Test]
        public void StationBackgroundBlur_AllAlphasAtLeast065()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship.unity"));
            var alphas = CollectNamedImageAlphas(yaml, "Background Blur");
            Assert.AreEqual(4, alphas.Count, "expected one Background Blur per station");
            for (int i = 0; i < alphas.Count; i++)
                Assert.GreaterOrEqual(alphas[i], 0.65f, "Background Blur[" + i + "] alpha=" + alphas[i]);
        }

        [Test]
        public void EndScreenScrim_ImageAlphaAtLeast065()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion.unity"));
            int nameAt = yaml.IndexOf("m_Name: Scrim\n", System.StringComparison.Ordinal);
            if (nameAt < 0)
                nameAt = yaml.IndexOf("m_Name: Scrim\r\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(nameAt, 0, "End Screen Scrim GO missing");
            int go = yaml.LastIndexOf("--- !u!1 &", nameAt, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(go, 0, "Scrim GameObject header missing");
            int end = yaml.IndexOf("\n--- !u!", go + 5, System.StringComparison.Ordinal);
            string goBlock = yaml.Substring(go, end - go);
            Assert.IsTrue(goBlock.Contains("m_Name: Scrim"), "Scrim GO block mismatch");
            float alpha = -1f;
            foreach (Match cm in Regex.Matches(goBlock, @"component: \{fileID: (\d+)\}"))
            {
                string cid = cm.Groups[1].Value;
                int s = yaml.IndexOf("--- !u!114 &" + cid + "\n", System.StringComparison.Ordinal);
                if (s < 0)
                    s = yaml.IndexOf("--- !u!114 &" + cid + "\r\n", System.StringComparison.Ordinal);
                if (s < 0) continue;
                int e = yaml.IndexOf("\n--- !u!", s + 5, System.StringComparison.Ordinal);
                string chunk = yaml.Substring(s, e - s);
                if (!chunk.Contains("m_Color:")) continue;
                var am = ImageColorA.Match(chunk);
                if (!am.Success) continue;
                alpha = float.Parse(am.Groups[1].Value, CultureInfo.InvariantCulture);
                break;
            }
            Assert.GreaterOrEqual(alpha, 0.65f, "End Screen Scrim alpha=" + alpha);
        }

        [Test]
        public void StationHosts_HeaderBodyCloseGuttersAtLeast16()
        {
            // Close is a stripped PrefabInstance. Authored gutters: top-anchored
            // Station Title → Purpose → Deal (16 each) and bottom-anchored Station Footer.
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship.unity"));
            string[] stations = { "ARMORY", "FABRICATION", "CREW QUARTERS", "NAV MAP" };
            for (int i = 0; i < stations.Length; i++)
            {
                string st = stations[i];
                var title = ChildAnchored(yaml, st, "Station Title");
                var purpose = ChildAnchored(yaml, st, "Station Purpose");
                var deal = ChildAnchored(yaml, st, "Station Deal");
                var footer = ChildAnchored(yaml, st, "Station Footer");
                float titleBottom = title.PosY - title.SizeY;
                float purposeBottom = purpose.PosY - purpose.SizeY;
                Assert.GreaterOrEqual(titleBottom - purpose.PosY, 16f,
                    st + " title→purpose gutter");
                Assert.GreaterOrEqual(purposeBottom - deal.PosY, 16f,
                    st + " purpose→deal gutter");
                Assert.GreaterOrEqual(footer.PosY, 16f,
                    st + " footer bottom clearance");
            }
        }

        struct Anchored
        {
            public float PosY;
            public float SizeY;
        }

        static Anchored ChildAnchored(string yaml, string station, string child)
        {
            int m = yaml.IndexOf("m_Name: " + station + "\n", System.StringComparison.Ordinal);
            if (m < 0)
                m = yaml.IndexOf("m_Name: " + station + "\r\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(m, 0, station + " missing");
            int go = yaml.LastIndexOf("--- !u!1 &", m, System.StringComparison.Ordinal);
            int end = yaml.IndexOf("\n--- !u!", go + 5, System.StringComparison.Ordinal);
            string goBlock = yaml.Substring(go, end - go);
            string rectId = null;
            foreach (Match cm in Regex.Matches(goBlock, @"component: \{fileID: (\d+)\}"))
            {
                string cid = cm.Groups[1].Value;
                if (yaml.IndexOf("--- !u!224 &" + cid + "\n", System.StringComparison.Ordinal) >= 0
                    || yaml.IndexOf("--- !u!224 &" + cid + "\r\n", System.StringComparison.Ordinal) >= 0)
                {
                    rectId = cid;
                    break;
                }
            }
            Assert.IsNotNull(rectId, station + " RectTransform missing");
            int rs = yaml.IndexOf("--- !u!224 &" + rectId + "\n", System.StringComparison.Ordinal);
            if (rs < 0)
                rs = yaml.IndexOf("--- !u!224 &" + rectId + "\r\n", System.StringComparison.Ordinal);
            int re = yaml.IndexOf("\n--- !u!", rs + 5, System.StringComparison.Ordinal);
            string kidsBlock = yaml.Substring(rs, re - rs);
            int kidsAt = kidsBlock.IndexOf("m_Children:", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(kidsAt, 0, station + " has no children");
            int fatherAt = kidsBlock.IndexOf("m_Father:", kidsAt, System.StringComparison.Ordinal);
            foreach (Match km in Regex.Matches(kidsBlock.Substring(kidsAt, fatherAt - kidsAt),
                         @"\{fileID: (\d+)\}"))
            {
                string kid = km.Groups[1].Value;
                int hit = yaml.IndexOf("- component: {fileID: " + kid + "}",
                    System.StringComparison.Ordinal);
                if (hit < 0) continue;
                int kg = yaml.LastIndexOf("--- !u!1 &", hit, System.StringComparison.Ordinal);
                int ke = yaml.IndexOf("\n--- !u!", kg + 5, System.StringComparison.Ordinal);
                string kblock = yaml.Substring(kg, ke - kg);
                if (!kblock.Contains("m_Name: " + child + "\n")
                    && !kblock.Contains("m_Name: " + child + "\r\n"))
                    continue;
                int krs = yaml.IndexOf("--- !u!224 &" + kid + "\n", System.StringComparison.Ordinal);
                if (krs < 0)
                    krs = yaml.IndexOf("--- !u!224 &" + kid + "\r\n", System.StringComparison.Ordinal);
                int kre = yaml.IndexOf("\n--- !u!", krs + 5, System.StringComparison.Ordinal);
                string chunk = yaml.Substring(krs, kre - krs);
                var pos = Regex.Match(chunk, @"m_AnchoredPosition: \{x: ([-\d.]+), y: ([-\d.]+)\}");
                var size = Regex.Match(chunk, @"m_SizeDelta: \{x: ([-\d.]+), y: ([-\d.]+)\}");
                Assert.IsTrue(pos.Success && size.Success, child + " anchors missing under " + station);
                return new Anchored
                {
                    PosY = float.Parse(pos.Groups[2].Value, CultureInfo.InvariantCulture),
                    SizeY = float.Parse(size.Groups[2].Value, CultureInfo.InvariantCulture)
                };
            }
            Assert.Fail(child + " not found under " + station);
            return default;
        }

        static void AssertInactive(string yaml, string name)
        {
            var m = Regex.Match(yaml,
                @"m_Name: " + Regex.Escape(name) + @"\r?\n(?:.*\r?\n){0,16}\s*m_IsActive: (\d)");
            Assert.IsTrue(m.Success, name + " GO not found");
            Assert.AreEqual("0", m.Groups[1].Value, name + " still active");
        }

        static System.Collections.Generic.List<float> CollectNamedImageAlphas(string yaml, string name)
        {
            var list = new System.Collections.Generic.List<float>(8);
            int idx = 0;
            while (true)
            {
                int m = yaml.IndexOf("m_Name: " + name + "\n", idx, System.StringComparison.Ordinal);
                if (m < 0) break;
                int go = yaml.LastIndexOf("--- !u!1 &", m, System.StringComparison.Ordinal);
                int end = yaml.IndexOf("\n--- !u!", go + 5, System.StringComparison.Ordinal);
                string goBlock = yaml.Substring(go, end - go);
                foreach (Match cm in Regex.Matches(goBlock, @"component: \{fileID: (\d+)\}"))
                {
                    string cid = cm.Groups[1].Value;
                    int s = yaml.IndexOf("--- !u!114 &" + cid + "\n", System.StringComparison.Ordinal);
                    if (s < 0) continue;
                    int e = yaml.IndexOf("\n--- !u!", s + 5, System.StringComparison.Ordinal);
                    string chunk = yaml.Substring(s, e - s);
                    if (!chunk.Contains("m_Sprite:") || !chunk.Contains("m_Color:")) continue;
                    var am = ImageColorA.Match(chunk);
                    if (!am.Success) continue;
                    list.Add(float.Parse(am.Groups[1].Value, CultureInfo.InvariantCulture));
                    break;
                }
                idx = m + 1;
            }
            return list;
        }
    }
}
