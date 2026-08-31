using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
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
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion_Coop.unity");
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
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion_Coop.unity");
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
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship - CoOp.unity");
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
            AssertOverflow("Assets/Scenes/Bastion_Coop.unity", "Canvas - Dynamic");
            AssertOverflow("Assets/Scenes/Ship - CoOp.unity", "Ship HUD");
        }

        [Test]
        public void ProfileExp_IsInactive()
        {
            AssertProfileExpInactive("Assets/Scenes/Bastion_Coop.unity", "Canvas - Dynamic");
            AssertProfileExpInactive("Assets/Scenes/Ship - CoOp.unity", "Ship HUD");
        }

        [Test]
        public void FightScore_PushesAliveCountOnTheWaveLine()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Bastion/HudController.Fight.cs")).Replace("\r\n", "\n");
            int set = src.IndexOf("_view.SetScore(salvage, _lineBuf.ToString())", StringComparison.Ordinal);
            Assert.GreaterOrEqual(set, 0, "SetScore(salvage, ...) missing");
            int start = src.LastIndexOf("_lineBuf.Clear();", set, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "wave-line buffer clear missing");
            string block = src.Substring(start, set - start);
            StringAssert.Contains(".Append(alive)", block,
                "Wave Counter must carry remaining enemies");
            StringAssert.Contains("alive != _cacheScoreAlive", src,
                "Score must repaint when remaining enemies change");
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
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion_Coop.unity");
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
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship - CoOp.unity");
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

        [Test]
        public void FightHosts_LockedInsetsAndTypeFloors()
        {
            const string canvas = "Canvas - Dynamic";
            var scene = SceneHudYaml.Load("Assets/Scenes/Bastion_Coop.unity");
            AssertPosSize(scene, "Hud Vitals", canvas, 135f, 24f, 400f, 150f);
            AssertPosSize(scene, "Hud Profile", canvas, 40f, -40f, 360f, 96f);
            float px, py, sx, sy;
            scene.RectOf("Hud Score", canvas, out px, out py, out sx, out sy);
            Assert.AreEqual(-24f, px, 0.01, "Hud Score pos.x");
            Assert.AreEqual(-40f, py, 0.01, "Hud Score pos.y");
            Assert.AreEqual(400f, sx, 0.01, "Hud Score size.x");
            Assert.AreEqual(96f, sy, 0.01, "Hud Score size.y");

            Assert.IsTrue(scene.Has("Profile Level", "Hud Profile"),
                "Profile Level must live under Hud Profile");
            Assert.IsTrue(scene.Has("Protocol Line", "Hud Profile"),
                "Protocol Line must live under Hud Profile");
            var level = scene.TmpOf("Profile Level", canvas);
            Assert.GreaterOrEqual(level.FontSizeBase, 28f, "Profile Level fontSizeBase");
            Assert.AreNotEqual(3, level.OverflowMode, "Profile Level overflow is Truncate");
            Assert.GreaterOrEqual(scene.TmpOf("Score Salvage", canvas).FontSizeMin, 20f,
                "Score Salvage fontSizeMin");
            Assert.GreaterOrEqual(scene.TmpOf("Wave Counter", canvas).FontSizeMin, 18f,
                "Wave Counter fontSizeMin");
        }

        [Test]
        public void FightMissionHeader_ClearsPrepBody()
        {
            AssertMissionHeader("Assets/Scenes/Bastion_Coop.unity");
            AssertMissionHeader("Assets/Scenes/Range.unity");
        }

        [Test]
        public void FightAndShipHosts_EdgeGapAtLeast24()
        {
            AssertEdgeInset("Assets/Scenes/Bastion_Coop.unity", "Canvas - Dynamic",
                new[] { "Hud Profile", "Hud Score", "Hud Hatch", "Hud Vitals", "Hud Weapon" });
            AssertEdgeInset("Assets/Scenes/Ship - CoOp.unity", "Ship HUD",
                new[] { "Hud Profile", "Hud Score", "Hud Weapon" });
        }

        [Test]
        public void ShipHosts_LockedInsetsAndTypeFloors()
        {
            const string hud = "Ship HUD";
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship - CoOp.unity");
            AssertPosSize(scene, "Hud Profile", hud, 40f, -40f, 360f, 96f);
            AssertPosSize(scene, "Hud Score", hud, -24f, -40f, 360f, 96f);
            Assert.IsTrue(scene.Has("Profile Level", "Hud Profile"),
                "Ship Profile Level must live under Hud Profile");
            Assert.IsTrue(scene.Has("Protocol Line", "Hud Profile"),
                "Ship Protocol Line must live under Hud Profile");
            Assert.GreaterOrEqual(scene.TmpOf("Profile Level", hud).FontSizeBase, 28f,
                "Ship Profile Level fontSizeBase");
            Assert.GreaterOrEqual(scene.TmpOf("Score Salvage", hud).FontSizeMin, 18f,
                "Ship Score Salvage fontSizeMin");
            Assert.GreaterOrEqual(scene.TmpOf("Wave Counter", hud).FontSizeMin, 18f,
                "Ship Wave Counter fontSizeMin");
            Assert.GreaterOrEqual(scene.TmpOf("Weapon Name", hud).FontSizeMin, 16f,
                "Ship Weapon Name fontSizeMin");
        }

        [Test]
        public void RangeIdentity_SitsBesideIconOnProfile()
        {
            const string canvas = "Canvas - Dynamic";
            var scene = SceneHudYaml.Load("Assets/Scenes/Range.unity");
            AssertPosSize(scene, "Hud Profile", canvas, 40f, -40f, 360f, 96f);
            Assert.IsTrue(scene.Has("Profile Level", "Hud Profile"),
                "Range Profile Level must live under Hud Profile");
            Assert.IsTrue(scene.Has("Protocol Line", "Hud Profile"),
                "Range Protocol Line must live under Hud Profile");
            Assert.GreaterOrEqual(scene.TmpOf("Profile Level", canvas).FontSizeBase, 28f,
                "Range Profile Level fontSizeBase");
            Assert.AreNotEqual(3, scene.TmpOf("Profile Level", canvas).OverflowMode,
                "Range Profile Level overflow is Truncate");
        }

        [Test]
        public void ShipPause_CompendiumHasItsOwnRow()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship - CoOp.unity");
            Assert.IsTrue(scene.Has("Compendium", "Button List"),
                "Ship Pause Menu Button List has no Compendium row");
            Assert.IsTrue(scene.IsActive("Compendium", "Button List"),
                "Ship Pause Menu Compendium row is inactive");
            float spacing = scene.VlgSpacing("Button List", "Pause Menu");
            float cx, cy, cw, ch, sx, sy, sw, sh, qx, qy, qw, qh, rx, ry, rw, rh;
            scene.RectOf("Resume", "Button List", out rx, out ry, out rw, out rh);
            scene.RectOf("Compendium", "Button List", out cx, out cy, out cw, out ch);
            scene.RectOf("Settings", "Button List", out sx, out sy, out sw, out sh);
            scene.RectOf("QUIT TO MENU", "Button List", out qx, out qy, out qw, out qh);
            float slot = rh + spacing;
            Assert.GreaterOrEqual(Math.Abs(cy - ry), slot - 0.01f,
                "Compendium overlaps Resume on Ship pause (cy=" + cy + " ry=" + ry + ")");
            Assert.GreaterOrEqual(Math.Abs(sy - cy), slot - 0.01f,
                "Compendium shares Settings' Ship pause row (cy=" + cy + " sy=" + sy + ")");
            Assert.GreaterOrEqual(Math.Abs(qy - sy), slot - 0.01f,
                "QUIT TO MENU overlaps Settings on Ship pause (qy=" + qy + " sy=" + sy + ")");
        }

        [Test]
        public void CrewQuartersCatalog_NameAndLevelFit()
        {
            var scene = SceneHudYaml.Load("Assets/Scenes/Ship - CoOp.unity");
            string[] rows = { "Vitality Row", "Stamina Row", "Scavenging Row", "Luck Row" };
            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                float px, py, sx, sy;
                scene.RectOf("Name", row, out px, out py, out sx, out sy);
                Assert.GreaterOrEqual(sx, 340f, row + " Name size.x");
                scene.RectOf("Level", row, out px, out py, out sx, out sy);
                Assert.GreaterOrEqual(sx, 160f, row + " Level size.x");
                Assert.AreEqual(0, scene.TmpOf("Level", row).WrapMode, row + " Level wraps");
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

        static void AssertPosSize(SceneHudYaml scene, string name, string ancestor,
            float posX, float posY, float sizeX, float sizeY)
        {
            float x, y, w, h;
            scene.RectOf(name, ancestor, out x, out y, out w, out h);
            Assert.AreEqual(posX, x, 0.01, name + " pos.x");
            Assert.AreEqual(posY, y, 0.01, name + " pos.y");
            Assert.AreEqual(sizeX, w, 0.01, name + " size.x");
            Assert.AreEqual(sizeY, h, 0.01, name + " size.y");
        }

        static void AssertMissionHeader(string scenePath)
        {
            var scene = SceneHudYaml.Load(scenePath);
            float px, py, sx, sy;
            scene.RectOf("Drawer Right Header", "Canvas - Dynamic", out px, out py, out sx, out sy);
            Assert.GreaterOrEqual(sy, 52f, scenePath + " Drawer Right Header size.y");
            Assert.GreaterOrEqual(scene.LayoutMinHeight("Drawer Right Header", "Canvas - Dynamic"),
                52f, scenePath + " Drawer Right Header LayoutElement minHeight");
            Assert.GreaterOrEqual(
                scene.TmpOf("Drawer Right Header", "Canvas - Dynamic").FontSizeBase, 24f,
                scenePath + " Drawer Right Header fontSizeBase");
            scene.RectOf("Active Mission", "Hud Drawer Right", out px, out py, out sx, out sy);
            Assert.LessOrEqual(py, -100f, scenePath + " Active Mission pos.y");
            Assert.GreaterOrEqual(scene.VlgSpacing("Panel", "Hud Drawer Right"), 24f,
                scenePath + " Hud Drawer Right Panel VLG spacing");
        }

        static void AssertEdgeInset(string scenePath, string ancestor, string[] hosts)
        {
            var scene = SceneHudYaml.Load(scenePath);
            for (int r = 0; r < Widths.Length; r++)
            {
                int w = Widths[r], h = Heights[r];
                for (int i = 0; i < hosts.Length; i++)
                {
                    var box = scene.CanvasAabb(hosts[i], ancestor, w, h);
                    string at = hosts[i] + " at " + w + "x" + h + " " + box;
                    Assert.GreaterOrEqual(box.X0, MinGap, at + " left inset");
                    Assert.GreaterOrEqual(box.Y0, MinGap, at + " bottom inset");
                    Assert.GreaterOrEqual(w - box.X1, MinGap, at + " right inset");
                    Assert.GreaterOrEqual(h - box.Y1, MinGap, at + " top inset");
                }
            }
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
        public readonly float FontSizeBase;
        public readonly float FontSizeMin;

        public TmpFlags(int wrap, int overflow, bool autoSize, float fontSizeBase, float fontSizeMin)
        {
            WrapMode = wrap;
            OverflowMode = overflow;
            AutoSize = autoSize;
            FontSizeBase = fontSizeBase;
            FontSizeMin = fontSizeMin;
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
        readonly Dictionary<long, float> _leMinHByGo = new Dictionary<long, float>();
        readonly Dictionary<long, float> _vlgSpacingByGo = new Dictionary<long, float>();
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

        public float LayoutMinHeight(string name, string ancestor)
        {
            long goId = FindGo(name, ancestor);
            Assert.AreNotEqual(0L, goId, "no '" + name + "' under " + ancestor);
            float min;
            Assert.IsTrue(_leMinHByGo.TryGetValue(goId, out min), "no LayoutElement height on " + name);
            return min;
        }

        public void RectOf(string name, string ancestor,
            out float posX, out float posY, out float sizeX, out float sizeY)
        {
            long goId = FindGo(name, ancestor);
            Assert.AreNotEqual(0L, goId, "no '" + name + "' under " + ancestor);
            Assert.IsTrue(_rects.TryGetValue(_gos[goId].RectId, out Rt rt),
                "no RectTransform on " + name);
            posX = rt.PosX;
            posY = rt.PosY;
            sizeX = rt.SizeX;
            sizeY = rt.SizeY;
        }

        public float VlgSpacing(string name, string ancestor)
        {
            long goId = FindGo(name, ancestor);
            Assert.AreNotEqual(0L, goId, "no '" + name + "' under " + ancestor);
            float spacing;
            Assert.IsTrue(_vlgSpacingByGo.TryGetValue(goId, out spacing),
                "no VerticalLayoutGroup on " + name);
            return spacing;
        }

        /// <summary>Whether a named object exists under an ancestor. Lets a test assert a
        /// widget is ABSENT, which name-based lookup otherwise cannot express.</summary>
        public bool Has(string name, string ancestor)
        {
            return FindGo(name, ancestor) != 0;
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
            float fontBase = 0f, fontMin = 0f;
            bool inHudController = false;
            bool inShipHud = false;
            bool inTmp = false;
            bool inLe = false;
            bool inVlg = false;
            float leMin = -1f;
            float leMinH = -1f;
            float vlgSpacing = 0f;

            foreach (string raw in File.ReadLines(path))
            {
                var m = Header.Match(raw);
                if (m.Success)
                {
                    Flush(kind, id, go, rt, mbGo, wrap, overflow, autoSize, fontBase, fontMin,
                        inTmp, inLe, inVlg, leMin, leMinH, vlgSpacing);
                    kind = m.Groups[1].Value;
                    id = long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    go = default;
                    rt = new Rt { Id = id };
                    mbGo = 0;
                    wrap = overflow = 0;
                    autoSize = false;
                    fontBase = fontMin = 0f;
                    inHudController = false;
                    inShipHud = false;
                    inTmp = false;
                    inLe = false;
                    inVlg = false;
                    leMin = -1f;
                    leMinH = -1f;
                    vlgSpacing = 0f;
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
                    else if (raw.Contains("VerticalLayoutGroup"))
                        inVlg = true;
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
                    else if (inTmp && raw.StartsWith("  m_fontSizeBase:", StringComparison.Ordinal))
                        fontBase = ParseFloat(raw);
                    else if (inTmp && raw.StartsWith("  m_fontSizeMin:", StringComparison.Ordinal))
                        fontMin = ParseFloat(raw);
                    else if (inLe && raw.StartsWith("  m_MinWidth:", StringComparison.Ordinal))
                        leMin = ParseFloat(raw);
                    else if (inLe && raw.StartsWith("  m_MinHeight:", StringComparison.Ordinal))
                        leMinH = ParseFloat(raw);
                    else if (inVlg && raw.StartsWith("  m_Spacing:", StringComparison.Ordinal))
                        vlgSpacing = ParseFloat(raw);
                }
            }
            Flush(kind, id, go, rt, mbGo, wrap, overflow, autoSize, fontBase, fontMin,
                inTmp, inLe, inVlg, leMin, leMinH, vlgSpacing);
        }

        void Flush(string kind, long id, Go go, Rt rt, long mbGo, int wrap, int overflow,
            bool autoSize, float fontBase, float fontMin, bool inTmp, bool inLe, bool inVlg,
            float leMin, float leMinH, float vlgSpacing)
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
                    _tmpByGo[mbGo] = new TmpFlags(wrap, overflow, autoSize, fontBase, fontMin);
                if (inLe && mbGo != 0)
                {
                    _leMinByGo[mbGo] = leMin;
                    _leMinHByGo[mbGo] = leMinH;
                }
                if (inVlg && mbGo != 0)
                    _vlgSpacingByGo[mbGo] = vlgSpacing;
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
