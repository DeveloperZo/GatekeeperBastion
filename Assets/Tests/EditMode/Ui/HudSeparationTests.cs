using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeeperBastion.Tests
{
    /// <summary>Fight and Ship HUD hosts: pairwise AABB ≥24 canvas units at 1920×1080 and
    /// 1280×720, overflow TMP not truncated, Profile Exp inactive.</summary>
    public class HudSeparationTests
    {
        const float MinGap = 24f;
        static readonly int[] Widths = { 1920, 1280 };
        static readonly int[] Heights = { 1080, 720 };

        [Test]
        public void FightHosts_PairwiseGapAtBothResolutions()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion.unity");
            string[] hosts =
            {
                "Hud Profile", "Hud Score", "Hud Hatch", "Hud Vitals", "Hud Weapon"
            };
            for (int r = 0; r < Widths.Length; r++)
            {
                var boxes = new Aabb[hosts.Length];
                for (int i = 0; i < hosts.Length; i++)
                    boxes[i] = scene.CanvasAabb(hosts[i], "Canvas - Dynamic", Widths[r], Heights[r]);
                AssertPairwise(hosts, boxes, Widths[r], Heights[r]);
            }
        }

        [Test]
        public void FightShortcutPrepAndPrompt_ClearLockedCards()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion.unity");
            for (int r = 0; r < Widths.Length; r++)
            {
                int w = Widths[r], h = Heights[r];
                var vitals = scene.CanvasAabb("Hud Vitals", "Canvas - Dynamic", w, h);
                var weapon = scene.CanvasAabb("Hud Weapon", "Canvas - Dynamic", w, h);
                var hatch = scene.CanvasAabb("Hud Hatch", "Canvas - Dynamic", w, h);
                var shortcut = scene.CanvasAabb("Shortcut Keys", "Hud Drawer Left", w, h);
                var prompt = scene.CanvasAabb("Interact Prompt", "Canvas - Dynamic", w, h);
                var prep = scene.PrepHeadingAabb(w, h);
                AssertGap("Shortcut Keys", shortcut, "Hud Vitals", vitals, w, h);
                AssertGap("Shortcut Keys", shortcut, "Hud Weapon", weapon, w, h);
                AssertGap("Prep heading", prep, "Hud Hatch", hatch, w, h);
                AssertGap("Interact Prompt", prompt, "Hud Vitals", vitals, w, h);
            }
        }

        [Test]
        public void ShipHosts_PairwiseGapAndPromptClearsWeapon()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship.unity");
            string[] hosts = { "Hud Profile", "Hud Score", "Hud Weapon" };
            for (int r = 0; r < Widths.Length; r++)
            {
                int w = Widths[r], h = Heights[r];
                var boxes = new Aabb[hosts.Length];
                for (int i = 0; i < hosts.Length; i++)
                    boxes[i] = scene.CanvasAabb(hosts[i], "Ship HUD", w, h);
                AssertPairwise(hosts, boxes, w, h);
                var prompt = scene.ShipPromptAabb(w, h);
                AssertGap("Ship prompt", prompt, "Hud Weapon", boxes[2], w, h);
            }
        }

        [Test]
        public void OverflowTmp_WrapsOrAutoSizes_NotTruncated()
        {
            AssertOverflow("Assets/Scenes/Bastion.unity", "Canvas - Dynamic");
            AssertOverflow("Assets/Scenes/Ship.unity", "Ship HUD");
        }

        [Test]
        public void ProfileExp_IsInactive()
        {
            AssertProfileExpInactive("Assets/Scenes/Bastion.unity", "Canvas - Dynamic");
            AssertProfileExpInactive("Assets/Scenes/Ship.unity", "Ship HUD");
        }

        static void AssertOverflow(string scenePath, string ancestor)
        {
            var scene = SceneHudYaml.Load(scenePath);
            string[] names = { "Score Salvage", "Wave Counter", "Weapon Name" };
            for (int i = 0; i < names.Length; i++)
            {
                var tmp = scene.TmpOf(names[i], ancestor);
                Assert.IsTrue(tmp.AutoSize || tmp.WrapMode != 0,
                    names[i] + " in " + scenePath + " must wrap or auto-size");
                Assert.IsFalse(tmp.OverflowMode == 1 || tmp.OverflowMode == 3,
                    names[i] + " in " + scenePath + " truncates (overflowMode=" + tmp.OverflowMode + ")");
            }
        }

        [Test]
        public void FightDrawerAndMessages_DoNotTruncate()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion.unity");
            string[] names =
            {
                "Drawer Left Header", "Drawer Right Header",
                "Msg General Text", "Msg Player Text"
            };
            for (int i = 0; i < names.Length; i++)
            {
                var tmp = scene.TmpOf(names[i], "Canvas - Dynamic");
                Assert.IsTrue(tmp.AutoSize || tmp.WrapMode != 0,
                    names[i] + " must wrap or auto-size");
                Assert.IsFalse(tmp.OverflowMode == 1 || tmp.OverflowMode == 3,
                    names[i] + " truncates (overflowMode=" + tmp.OverflowMode + ")");
            }
        }

        [Test]
        public void ArmoryCatalogCostAndEffect_DoNotWrap()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship.unity");
            for (int i = 1; i <= 3; i++)
            {
                string row = "Catalog Row " + i;
                var cost = scene.TmpOf("Cost", row);
                Assert.AreEqual(0, cost.WrapMode, row + " Cost wraps");
                Assert.GreaterOrEqual(scene.LayoutMinWidth("Cost", row), 160f,
                    row + " Cost has no LayoutElement minWidth — ChildControlWidth 1 collapses it");
                var effect = scene.TmpOf("Effect", row);
                Assert.AreEqual(0, effect.WrapMode, row + " Effect wraps");
                Assert.GreaterOrEqual(scene.LayoutMinWidth("Effect", row), 260f,
                    row + " Effect has no LayoutElement minWidth");
            }
        }

        static void AssertProfileExpInactive(string scenePath, string ancestor)
        {
            var scene = SceneHudYaml.Load(scenePath);
            Assert.IsFalse(scene.IsActive("Profile Exp", ancestor),
                "Profile Exp still active in " + scenePath);
        }

        static void AssertPairwise(string[] names, Aabb[] boxes, int w, int h)
        {
            for (int i = 0; i < names.Length; i++)
            {
                for (int j = i + 1; j < names.Length; j++)
                    AssertGap(names[i], boxes[i], names[j], boxes[j], w, h);
            }
        }

        static void AssertGap(string aName, Aabb a, string bName, Aabb b, int w, int h)
        {
            float gap = Aabb.Gap(a, b);
            Assert.GreaterOrEqual(gap, MinGap,
                aName + " vs " + bName + " at " + w + "x" + h + " gap=" + gap
                + " a=" + a + " b=" + b);
        }
    }

    readonly struct Aabb
    {
        public readonly float X0, Y0, X1, Y1;

        public Aabb(float x0, float y0, float x1, float y1)
        {
            X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
        }

        public override string ToString() =>
            "(" + X0.ToString("0.#") + "," + Y0.ToString("0.#") + ")-("
            + X1.ToString("0.#") + "," + Y1.ToString("0.#") + ")";

        public static float Gap(Aabb a, Aabb b)
        {
            float ox = Math.Min(a.X1, b.X1) - Math.Max(a.X0, b.X0);
            float oy = Math.Min(a.Y1, b.Y1) - Math.Max(a.Y0, b.Y0);
            if (ox > 0f && oy > 0f) return -Math.Min(ox, oy);
            if (ox > 0f) return -oy;
            if (oy > 0f) return -ox;
            return (float)Math.Sqrt((double)ox * ox + (double)oy * oy);
        }
    }

    readonly struct TmpFlags
    {
        public readonly int WrapMode;
        public readonly int OverflowMode;
        public readonly bool AutoSize;

        public TmpFlags(int wrap, int overflow, bool autoSize)
        {
            WrapMode = wrap;
            OverflowMode = overflow;
            AutoSize = autoSize;
        }
    }

    sealed class SceneHudYaml
    {
        struct Go
        {
            public string Name;
            public long RectId;
            public bool Active;
        }

        struct Rt
        {
            public long Id;
            public long GoId;
            public long Father;
            public float AMinX, AMinY, AMaxX, AMaxY;
            public float PosX, PosY, SizeX, SizeY, PivotX, PivotY;
        }

        readonly Dictionary<long, Go> _gos = new Dictionary<long, Go>();
        readonly Dictionary<long, Rt> _rects = new Dictionary<long, Rt>();
        readonly Dictionary<long, TmpFlags> _tmpByGo = new Dictionary<long, TmpFlags>();
        readonly Dictionary<long, float> _leMinByGo = new Dictionary<long, float>();
        readonly Dictionary<long, long> _mbGo = new Dictionary<long, long>();
        long _prepHeadingMb;
        long _shipPromptMb;

        static readonly Regex Header = new Regex(@"^--- !u!(\d+) &(\d+)", RegexOptions.Compiled);
        static readonly Regex FileId = new Regex(@"\{fileID: (\d+)\}", RegexOptions.Compiled);
        static readonly Regex Vec2 = new Regex(
            @"\{x: ([-\d.]+), y: ([-\d.]+)\}", RegexOptions.Compiled);

        public static SceneHudYaml Load(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            var scene = new SceneHudYaml();
            scene.Parse(path);
            return scene;
        }

        public Aabb CanvasAabb(string name, string ancestor, float screenW, float screenH)
        {
            long goId = FindGo(name, ancestor);
            Assert.AreNotEqual(0L, goId, "no '" + name + "' under " + ancestor);
            return RectAabb(_gos[goId].RectId, screenW, screenH);
        }

        public Aabb PrepHeadingAabb(float screenW, float screenH)
        {
            Assert.AreNotEqual(0L, _prepHeadingMb, "BastionHudView._prepHeading missing");
            long goId = _mbGo[_prepHeadingMb];
            return RectAabb(_gos[goId].RectId, screenW, screenH);
        }

        public Aabb ShipPromptAabb(float screenW, float screenH)
        {
            Assert.AreNotEqual(0L, _shipPromptMb, "ShipHudView._prompt missing");
            long goId = _mbGo[_shipPromptMb];
            return RectAabb(_gos[goId].RectId, screenW, screenH);
        }

        public TmpFlags TmpOf(string name, string ancestor)
        {
            long goId = FindGo(name, ancestor);
            Assert.IsTrue(_tmpByGo.ContainsKey(goId), "no TMP on " + name);
            return _tmpByGo[goId];
        }

        public float LayoutMinWidth(string name, string ancestor)
        {
            long goId = FindGo(name, ancestor);
            Assert.AreNotEqual(0L, goId, "no '" + name + "' under " + ancestor);
            float min;
            Assert.IsTrue(_leMinByGo.TryGetValue(goId, out min), "no LayoutElement on " + name);
            return min;
        }

        public bool IsActive(string name, string ancestor)
        {
            long goId = FindGo(name, ancestor);
            return _gos[goId].Active;
        }

        long FindGo(string name, string ancestor)
        {
            foreach (var kv in _gos)
            {
                if (kv.Value.Name != name) continue;
                if (HasAncestor(kv.Key, ancestor)) return kv.Key;
            }
            return 0;
        }

        bool HasAncestor(long goId, string ancestor)
        {
            int guard = 0;
            while (goId != 0 && guard++ < 64)
            {
                if (!_gos.TryGetValue(goId, out Go go)) return false;
                if (go.Name == ancestor) return true;
                if (go.RectId == 0 || !_rects.TryGetValue(go.RectId, out Rt rt)) return false;
                if (rt.Father == 0) return ancestor == go.Name;
                goId = GoOfRect(rt.Father);
            }
            return false;
        }

        long GoOfRect(long rectId)
        {
            if (!_rects.TryGetValue(rectId, out Rt rt)) return 0;
            return rt.GoId;
        }

        Aabb RectAabb(long rectId, float screenW, float screenH)
        {
            var chain = new List<Rt>(8);
            long id = rectId;
            int guard = 0;
            while (id != 0 && guard++ < 64)
            {
                if (!_rects.TryGetValue(id, out Rt rt)) break;
                chain.Add(rt);
                string name = _gos.TryGetValue(rt.GoId, out Go g) ? g.Name : "";
                if (name == "Canvas - Dynamic" || name == "Ship HUD" || name == "Canvas - In Game"
                    || name == "Ship HUD Canvas" || name == "Canvas")
                    break;
                id = rt.Father;
            }

            float parentW = screenW;
            float parentH = screenH;
            Aabb parent = new Aabb(0f, 0f, screenW, screenH);
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                Rt rt = chain[i];
                string name = _gos.TryGetValue(rt.GoId, out Go g) ? g.Name : "";
                bool isRoot = name == "Canvas - Dynamic" || name == "Ship HUD"
                    || name == "Canvas - In Game" || name == "Ship HUD Canvas" || name == "Canvas";
                if (isRoot)
                {
                    parent = new Aabb(0f, 0f, screenW, screenH);
                    parentW = screenW;
                    parentH = screenH;
                    continue;
                }

                float ax0 = parentW * rt.AMinX;
                float ay0 = parentH * rt.AMinY;
                float ax1 = parentW * rt.AMaxX;
                float ay1 = parentH * rt.AMaxY;
                float w = ax1 - ax0 + rt.SizeX;
                float h = ay1 - ay0 + rt.SizeY;
                float px = ax0 + (ax1 - ax0) * rt.PivotX + rt.PosX;
                float py = ay0 + (ay1 - ay0) * rt.PivotY + rt.PosY;
                float x0 = parent.X0 + px - rt.PivotX * w;
                float y0 = parent.Y0 + py - rt.PivotY * h;
                parent = new Aabb(x0, y0, x0 + w, y0 + h);
                parentW = w;
                parentH = h;
            }
            return parent;
        }

        void Parse(string path)
        {
            string kind = null;
            long id = 0;
            Go go = default;
            Rt rt = default;
            long mbGo = 0;
            int wrap = 0, overflow = 0;
            bool autoSize = false;
            bool inHudController = false;
            bool inShipHud = false;
            bool inTmp = false;
            bool inLe = false;
            float leMin = -1f;

            foreach (string raw in File.ReadLines(path))
            {
                var m = Header.Match(raw);
                if (m.Success)
                {
                    Flush(kind, id, go, rt, mbGo, wrap, overflow, autoSize, inTmp, inLe, leMin);
                    kind = m.Groups[1].Value;
                    id = long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    go = default;
                    rt = new Rt { Id = id };
                    mbGo = 0;
                    wrap = overflow = 0;
                    autoSize = false;
                    inHudController = false;
                    inShipHud = false;
                    inTmp = false;
                    inLe = false;
                    leMin = -1f;
                    continue;
                }

                if (kind == "1")
                {
                    if (raw.StartsWith("  m_Name: ", StringComparison.Ordinal))
                        go.Name = Unquote(raw.Substring(10).Trim());
                    else if (raw.StartsWith("  m_IsActive: ", StringComparison.Ordinal))
                        go.Active = raw.IndexOf('1') >= 0;
                    else if (raw.Contains("component: {fileID:") && go.RectId == 0)
                    {
                        var fm = FileId.Match(raw);
                        if (fm.Success)
                            go.RectId = long.Parse(fm.Groups[1].Value, CultureInfo.InvariantCulture);
                    }
                }
                else if (kind == "224")
                {
                    if (raw.StartsWith("  m_GameObject:", StringComparison.Ordinal))
                        rt.GoId = ParseFileId(raw);
                    else if (raw.StartsWith("  m_Father:", StringComparison.Ordinal))
                        rt.Father = ParseFileId(raw);
                    else if (raw.StartsWith("  m_AnchorMin:", StringComparison.Ordinal))
                        ParseVec2(raw, out rt.AMinX, out rt.AMinY);
                    else if (raw.StartsWith("  m_AnchorMax:", StringComparison.Ordinal))
                        ParseVec2(raw, out rt.AMaxX, out rt.AMaxY);
                    else if (raw.StartsWith("  m_AnchoredPosition:", StringComparison.Ordinal))
                        ParseVec2(raw, out rt.PosX, out rt.PosY);
                    else if (raw.StartsWith("  m_SizeDelta:", StringComparison.Ordinal))
                        ParseVec2(raw, out rt.SizeX, out rt.SizeY);
                    else if (raw.StartsWith("  m_Pivot:", StringComparison.Ordinal))
                        ParseVec2(raw, out rt.PivotX, out rt.PivotY);
                }
                else if (kind == "114")
                {
                    if (raw.StartsWith("  m_GameObject:", StringComparison.Ordinal))
                        mbGo = ParseFileId(raw);
                    else if (raw.Contains("BastionHudView"))
                        inHudController = true;
                    else if (raw.Contains("ShipHudView"))
                        inShipHud = true;
                    else if (raw.Contains("TextMeshProUGUI"))
                        inTmp = true;
                    else if (raw.Contains("UnityEngine.UI.LayoutElement"))
                        inLe = true;
                    else if (inHudController && raw.StartsWith("  _prepHeading:", StringComparison.Ordinal))
                        _prepHeadingMb = ParseFileId(raw);
                    else if (inShipHud && raw.StartsWith("  _prompt:", StringComparison.Ordinal))
                        _shipPromptMb = ParseFileId(raw);
                    else if (inTmp && raw.StartsWith("  m_TextWrappingMode:", StringComparison.Ordinal))
                        wrap = ParseInt(raw);
                    else if (inTmp && raw.StartsWith("  m_overflowMode:", StringComparison.Ordinal))
                        overflow = ParseInt(raw);
                    else if (inTmp && raw.StartsWith("  m_enableAutoSizing:", StringComparison.Ordinal))
                        autoSize = raw.IndexOf('1') >= 0;
                    else if (inLe && raw.StartsWith("  m_MinWidth:", StringComparison.Ordinal))
                        leMin = ParseFloat(raw);
                }
            }
            Flush(kind, id, go, rt, mbGo, wrap, overflow, autoSize, inTmp, inLe, leMin);
        }

        void Flush(string kind, long id, Go go, Rt rt, long mbGo, int wrap, int overflow,
            bool autoSize, bool inTmp, bool inLe, float leMin)
        {
            if (kind == "1")
                _gos[id] = go;
            else if (kind == "224")
            {
                _rects[id] = rt;
                if (rt.GoId != 0 && _gos.TryGetValue(rt.GoId, out Go g))
                {
                    g.RectId = id;
                    _gos[rt.GoId] = g;
                }
            }
            else if (kind == "114")
            {
                if (mbGo != 0) _mbGo[id] = mbGo;
                if (inTmp && mbGo != 0)
                    _tmpByGo[mbGo] = new TmpFlags(wrap, overflow, autoSize);
                if (inLe && mbGo != 0)
                    _leMinByGo[mbGo] = leMin;
            }
        }

        static long ParseFileId(string raw)
        {
            var m = FileId.Match(raw);
            return m.Success ? long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        }

        static void ParseVec2(string raw, out float x, out float y)
        {
            var m = Vec2.Match(raw);
            x = y = 0f;
            if (!m.Success) return;
            x = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            y = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        }

        static int ParseInt(string raw)
        {
            int colon = raw.LastIndexOf(':');
            if (colon < 0) return 0;
            int.TryParse(raw.Substring(colon + 1).Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int v);
            return v;
        }

        static float ParseFloat(string raw)
        {
            int colon = raw.LastIndexOf(':');
            if (colon < 0) return 0f;
            float.TryParse(raw.Substring(colon + 1).Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out float v);
            return v;
        }

        static string Unquote(string s)
        {
            if (s.Length >= 2 && s[0] == '\'' && s[s.Length - 1] == '\'')
                return s.Substring(1, s.Length - 2);
            return s;
        }
    }
}
