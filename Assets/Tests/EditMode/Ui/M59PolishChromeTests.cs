using System;
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
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity"));
            Assert.IsTrue(yaml.Contains("m_Name: Pause Shortcuts"),
                "Pause Shortcuts GO missing under Bastion pause canvas");
            var m = PauseBind.Match(yaml);
            Assert.IsTrue(m.Success, "HudController._pauseShortcuts field missing");
            Assert.AreNotEqual("0", m.Groups[1].Value, "HudController._pauseShortcuts unbound");
        }

        [Test]
        public void SettingsListContent_TopPaddingIsNotNegative()
        {
            foreach (string scene in new[]
            {
                "Assets/Scenes/Bastion_Coop.unity",
                "Assets/Scenes/Range.unity"
            })
            {
                string yaml = File.ReadAllText(Path.GetFullPath(scene));
                Assert.IsFalse(yaml.Contains("m_Top: -26"),
                    scene + " Settings List Content top padding is -26 (first row clips)");
            }
        }

        [Test]
        public void PauseCanvas_SortsAboveFightHud()
        {
            foreach (string scene in new[]
            {
                "Assets/Scenes/Bastion_Coop.unity",
                "Assets/Scenes/Range.unity"
            })
            {
                string yaml = File.ReadAllText(Path.GetFullPath(scene));
                int pause = CanvasSortingOrder(yaml, "Canvas - Pause Menu");
                int hud = CanvasSortingOrder(yaml, "Canvas - Dynamic");
                Assert.Greater(pause, hud,
                    scene + " pause order " + pause + " must beat fight HUD " + hud);
            }
        }

        [Test]
        public void BastionPause_HasCompendiumRow()
        {
            foreach (string path in new[]
            {
                "Assets/Scenes/Bastion_Coop.unity",
                "Assets/Scenes/Range.unity"
            })
            {
                var scene = SceneHudYaml.Load(path);
                Assert.IsTrue(scene.Has("Compendium", "Button List"),
                    path + " Pause Menu Button List has no Compendium row");
                Assert.IsTrue(scene.IsActive("Compendium", "Button List"),
                    path + " Pause Menu Compendium row is inactive");
            }
        }

        [Test]
        public void BastionPause_SettingsOpensOptionsScreen()
        {
            foreach (string path in new[]
            {
                "Assets/Scenes/Bastion_Coop.unity",
                "Assets/Scenes/Range.unity"
            })
            {
                string yaml = File.ReadAllText(Path.GetFullPath(path));
                Assert.IsTrue(yaml.Contains("OpenOptionsFromPauseMenu"),
                    path + " Settings button does not call OpenOptionsFromPauseMenu");
            }
        }

        static int CanvasSortingOrder(string yaml, string canvasName)
        {
            int nameAt = yaml.IndexOf("m_Name: " + canvasName + "\n", StringComparison.Ordinal);
            if (nameAt < 0)
                nameAt = yaml.IndexOf("m_Name: " + canvasName + "\r\n", StringComparison.Ordinal);
            Assert.GreaterOrEqual(nameAt, 0, canvasName + " missing");
            int go = yaml.LastIndexOf("--- !u!1 &", nameAt, StringComparison.Ordinal);
            int canvasComp = -1;
            foreach (Match cm in Regex.Matches(yaml.Substring(go, 400), @"component: \{fileID: (\d+)\}"))
            {
                string cid = cm.Groups[1].Value;
                int s = yaml.IndexOf("--- !u!223 &" + cid + "\n", StringComparison.Ordinal);
                if (s < 0)
                    s = yaml.IndexOf("--- !u!223 &" + cid + "\r\n", StringComparison.Ordinal);
                if (s < 0) continue;
                canvasComp = s;
                break;
            }
            Assert.GreaterOrEqual(canvasComp, 0, canvasName + " Canvas component missing");
            int end = yaml.IndexOf("\n--- !u!", canvasComp + 5, StringComparison.Ordinal);
            string block = yaml.Substring(canvasComp, end - canvasComp);
            var om = Regex.Match(block, @"m_SortingOrder: (-?\d+)");
            Assert.IsTrue(om.Success, canvasName + " sortingOrder missing");
            return int.Parse(om.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Test]
        public void StationCategoriesAndPreview_AreInactive()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity"));
            AssertInactive(yaml, "Categories");
            AssertInactive(yaml, "Preview Image");
        }

        [Test]
        public void ShiftUiWordmark_IsOff_AndGameTitleIsTheProtocol()
        {
            string ship = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity"));
            int titles = 0;
            int idx = 0;
            while (true)
            {
                int at = ship.IndexOf("m_text: GATEKEEPER'S PROTOCOL", idx, System.StringComparison.Ordinal);
                if (at < 0) break;
                titles++;
                idx = at + 1;
            }
            Assert.GreaterOrEqual(titles, 2, "Top Panel and Pause Game Title");

            int logos = 0;
            int off = 0;
            idx = 0;
            while (true)
            {
                int nameAt = ship.IndexOf("m_Name: Logo\n", idx, System.StringComparison.Ordinal);
                if (nameAt < 0)
                    nameAt = ship.IndexOf("m_Name: Logo\r\n", idx, System.StringComparison.Ordinal);
                if (nameAt < 0) break;
                logos++;
                int go = ship.LastIndexOf("--- !u!1 &", nameAt, System.StringComparison.Ordinal);
                int end = ship.IndexOf("\n--- !u!", go + 5, System.StringComparison.Ordinal);
                string goBlock = ship.Substring(go, end - go);
                if (goBlock.Contains("m_IsActive: 0")) off++;
                idx = nameAt + 1;
            }
            Assert.Greater(logos, 0, "Logo GOs missing");
            Assert.AreEqual(logos, off, "every SHIFT UI Logo GO is inactive");

            string menu = File.ReadAllText(Path.GetFullPath("Assets/Scenes/MainMenu.unity"));
            Assert.IsTrue(menu.Contains("m_text: GATEKEEPER'S PROTOCOL"),
                "Main Menu Title is GATEKEEPER'S PROTOCOL");
            AssertInactive(menu, "Preview Image");

            foreach (string scene in new[]
            {
                "Assets/Scenes/Bastion_Coop.unity",
                "Assets/Scenes/Range.unity"
            })
            {
                string yaml = File.ReadAllText(Path.GetFullPath(scene));
                Assert.IsTrue(yaml.Contains("m_text: GATEKEEPER'S PROTOCOL"),
                    scene + " Pause Game Title");
                AssertInactive(yaml, "Logo");
                AssertInactive(yaml, "Preview Image");
            }
        }

        [Test]
        public void Compendium_HidesGameTitleWhileOpen()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity"));
            int view = yaml.IndexOf(
                "GatekeepersProtocol::GatekeepersProtocol.Game.CompendiumView",
                StringComparison.Ordinal);
            Assert.GreaterOrEqual(view, 0, "CompendiumView missing");
            int end = yaml.IndexOf("\n--- !u!", view, StringComparison.Ordinal);
            string block = yaml.Substring(view, end - view);
            Assert.IsFalse(block.Contains("_hideWhileOpen: []"),
                "CompendiumView._hideWhileOpen is empty");
            int hide = block.IndexOf("_hideWhileOpen:", StringComparison.Ordinal);
            int next = block.IndexOf("\n  _volumeListRoot", hide, StringComparison.Ordinal);
            Assert.GreaterOrEqual(next, 0, "_hideWhileOpen block");
            string arr = block.Substring(hide, next - hide);
            var ids = Regex.Matches(arr, @"fileID: (\d+)");
            Assert.GreaterOrEqual(ids.Count, 2, "hide Pause Game Title and Top Panel Game Title");
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i].Groups[1].Value;
                int go = yaml.IndexOf("--- !u!1 &" + id + "\n", StringComparison.Ordinal);
                if (go < 0)
                    go = yaml.IndexOf("--- !u!1 &" + id + "\r\n", StringComparison.Ordinal);
                Assert.GreaterOrEqual(go, 0, "hideWhileOpen[" + i + "] GO " + id);
                int goEnd = yaml.IndexOf("\n--- !u!", go + 5, StringComparison.Ordinal);
                string goBlock = yaml.Substring(go, goEnd - go);
                Assert.IsTrue(goBlock.Contains("m_Name: Game Title"),
                    "hideWhileOpen[" + i + "] is not Game Title");
            }
        }

        [Test]
        public void StationBackgroundBlur_AllAlphasAtLeast065()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity"));
            var alphas = CollectNamedImageAlphas(yaml, "Background Blur");
            // Five since M117: the PRACTICE RANGE door is a station with a panel cloned from the ARMORY's.
            Assert.AreEqual(5, alphas.Count, "expected one Background Blur per station");
            for (int i = 0; i < alphas.Count; i++)
                Assert.GreaterOrEqual(alphas[i], 0.65f, "Background Blur[" + i + "] alpha=" + alphas[i]);
        }

        [Test]
        public void EndScreenScrim_ImageAlphaAtLeast065()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity"));
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
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity"));
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

        [Test]
        public void ShortcutTemplates_HaveFilledCooldownFill()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity"));
            int templates = 0;
            int bar = 0;
            int idx = 0;
            while (true)
            {
                int m = yaml.IndexOf("m_Name: CooldownFill\n", idx, System.StringComparison.Ordinal);
                if (m < 0)
                    m = yaml.IndexOf("m_Name: CooldownFill\r\n", idx, System.StringComparison.Ordinal);
                if (m < 0) break;
                int go = yaml.LastIndexOf("--- !u!1 &", m, System.StringComparison.Ordinal);
                int end = yaml.IndexOf("\n--- !u!", go + 5, System.StringComparison.Ordinal);
                string goBlock = yaml.Substring(go, end - go);
                bool ok = false;
                string fillChunk = null;
                foreach (Match cm in Regex.Matches(goBlock, @"component: \{fileID: (\d+)\}"))
                {
                    string cid = cm.Groups[1].Value;
                    int s = yaml.IndexOf("--- !u!114 &" + cid + "\n", System.StringComparison.Ordinal);
                    if (s < 0)
                        s = yaml.IndexOf("--- !u!114 &" + cid + "\r\n", System.StringComparison.Ordinal);
                    if (s < 0) continue;
                    int e = yaml.IndexOf("\n--- !u!", s + 5, System.StringComparison.Ordinal);
                    string chunk = yaml.Substring(s, e - s);
                    if (!chunk.Contains("m_Type: 3") || !chunk.Contains("m_FillMethod: 4"))
                        continue;
                    ok = true;
                    fillChunk = chunk;
                    break;
                }
                Assert.IsTrue(ok, "CooldownFill at " + m + " is not Filled Radial360");
                int rtAt = yaml.IndexOf("--- !u!224 &", go, System.StringComparison.Ordinal);
                int rtEnd = yaml.IndexOf("\n--- !u!", rtAt + 5, System.StringComparison.Ordinal);
                string rt = yaml.Substring(rtAt, rtEnd - rtAt);
                if (rt.Contains("m_Father: {fileID: 287379034}")
                    || rt.Contains("m_Father: {fileID: 197274594}"))
                    templates++;
                else
                {
                    bar++;
                    Assert.IsFalse(fillChunk.Contains("m_Sprite: {fileID: 0}"),
                        "Protocol Bar CooldownFill has no sprite — Radial360 cannot paint");
                    Assert.IsTrue(fillChunk.Contains("guid: b1e2bb5a32fee96498075a860def4696"),
                        "Protocol Bar CooldownFill must use Rounded Circle");
                    Assert.IsTrue(Regex.IsMatch(fillChunk, @"m_FillAmount: 0\r?\n"),
                        "Protocol Bar CooldownFill must author fillAmount 0 (READY)");
                    Assert.IsTrue(fillChunk.Contains("m_FillClockwise: 0"),
                        "Protocol Bar CooldownFill must wipe counterclockwise from 12");
                }
                idx = m + 1;
            }
            Assert.AreEqual(2, templates, "Fight and Pause templates each need one CooldownFill");
            Assert.AreEqual(3, bar, "Protocol Bar needs Passive/Personal/Crew CooldownFill");
        }

        [Test]
        public void CrewQuarters_ProtocolSheet_IsBound()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity"));
            int sheetAt = yaml.IndexOf("m_Name: Protocol Sheet\n", System.StringComparison.Ordinal);
            if (sheetAt < 0)
                sheetAt = yaml.IndexOf("m_Name: Protocol Sheet\r\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(sheetAt, 0, "Protocol Sheet GO missing under CREW List Content");
            string[] fields =
            {
                "_sheet"
            };
            for (int i = 0; i < fields.Length; i++)
            {
                var m = Regex.Match(yaml, fields[i] + @": \{fileID: (\d+)\}");
                Assert.IsTrue(m.Success, "CrewQuartersPanelView." + fields[i] + " missing");
                Assert.AreNotEqual("0", m.Groups[1].Value,
                    "CrewQuartersPanelView." + fields[i] + " is unbound");
            }
            string[] sheetFields =
            {
                "_titleLabel", "_title", "_descriptionLabel", "_doctrine",
                "_crewLabel", "_crew", "_personalLabel", "_personal",
                "_passiveLabel", "_passive", "_claim", "_back"
            };
            int viewAt = yaml.IndexOf("GatekeepersProtocol.Game.ProtocolSheetView", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(viewAt, 0, "ProtocolSheetView missing on Protocol Sheet");
            string viewBlock = yaml.Substring(viewAt, Math.Min(2000, yaml.Length - viewAt));
            for (int i = 0; i < sheetFields.Length; i++)
            {
                var m = Regex.Match(viewBlock, sheetFields[i] + @": \{fileID: (\d+)\}");
                Assert.IsTrue(m.Success, "ProtocolSheetView." + sheetFields[i] + " missing");
                Assert.AreNotEqual("0", m.Groups[1].Value,
                    "ProtocolSheetView." + sheetFields[i] + " is unbound");
            }
            Assert.GreaterOrEqual(
                yaml.IndexOf("m_Name: Claim Row\n", System.StringComparison.Ordinal), 0,
                "Claim Row missing under Protocol Sheet");
            int claimAt = yaml.IndexOf("m_Name: Claim\n", System.StringComparison.Ordinal);
            if (claimAt < 0)
                claimAt = yaml.IndexOf("m_Name: Claim\r\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(claimAt, 0, "Claim button missing");
            int mainButtonAt = yaml.IndexOf("guid: 7f6465851424ac84fa7e7c1d80acd1b7", claimAt,
                System.StringComparison.Ordinal);
            Assert.Greater(mainButtonAt, claimAt, "Claim is a Shift MainButton, not a plain Image strip");
            Assert.Less(mainButtonAt - claimAt, 12000, "Claim MainButton is on the Claim GO");
        }

        [Test]
        public void ProtocolLine_IsBoundOnBastionHud()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity"));
            Assert.GreaterOrEqual(
                yaml.IndexOf("m_Name: Protocol Line\n", System.StringComparison.Ordinal), 0,
                "Protocol Line GO missing");
            Assert.IsFalse(Regex.IsMatch(yaml, @"_protocolLine: \{fileID: 0\}"),
                "BastionHudView._protocolLine is unbound");
        }

        /// <summary>Profile, Score and Hatch became OptionalBinding so the practice range can
        /// omit its run-economy cards — the range is Tethered from its first frame, so hiding
        /// those panels in the scene did nothing: ShowCornerCards turned them back on. Making
        /// them optional means the binding validator no longer speaks for them, and an accidental
        /// unbinding in a scene that DOES run an economy would now be silent. This is that
        /// guard.</summary>
        [Test]
        public void RunEconomyCards_StayBoundOnPlayableBastions()
        {
            string[] scenes = { "Assets/Scenes/Bastion_Coop.unity", "Assets/Scenes/Bastion_Coop.unity" };
            string[] fields = { "_profileRoot", "_scoreRoot", "_hatchRoot" };
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!File.Exists(Path.GetFullPath(scenes[i]))) continue;
                string yaml = File.ReadAllText(Path.GetFullPath(scenes[i]));
                for (int f = 0; f < fields.Length; f++)
                {
                    Assert.IsTrue(Regex.IsMatch(yaml, fields[f] + @": \{fileID: \d+"),
                        scenes[i] + " has no " + fields[f] + " at all");
                    Assert.IsFalse(Regex.IsMatch(yaml, fields[f] + @": \{fileID: 0\}"),
                        scenes[i] + ": BastionHudView." + fields[f]
                        + " is unbound — that card is only optional for the range");
                }
            }
        }

        [Test]
        public void ProtocolBar_IsBoundOnBastionHud()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity"));
            Assert.GreaterOrEqual(
                yaml.IndexOf("m_Name: Protocol Bar\n", System.StringComparison.Ordinal), 0,
                "Protocol Bar GO missing");
            Assert.IsFalse(Regex.IsMatch(yaml, @"_protocolBar: \{fileID: 0\}"),
                "BastionHudView._protocolBar is unbound");
            Assert.GreaterOrEqual(
                yaml.IndexOf("m_Name: Passive Slot\n", System.StringComparison.Ordinal), 0,
                "Passive Slot missing");
            Assert.GreaterOrEqual(
                yaml.IndexOf("m_Name: Personal Slot\n", System.StringComparison.Ordinal), 0,
                "Personal Slot missing");
            Assert.GreaterOrEqual(
                yaml.IndexOf("m_Name: Crew Slot\n", System.StringComparison.Ordinal), 0,
                "Crew Slot missing");
            int viewAt = yaml.IndexOf(
                "GatekeepersProtocol.Game.ProtocolAbilityBarView", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(viewAt, 0, "ProtocolAbilityBarView missing on Protocol Bar");
            string viewBlock = yaml.Substring(viewAt, Math.Min(1200, yaml.Length - viewAt));
            string[] slots = { "_passive", "_personal", "_crew" };
            string[] fields = { "root:", "key:", "name:", "cooldownFill:", "countdown:" };
            for (int s = 0; s < slots.Length; s++)
            {
                Assert.IsTrue(viewBlock.Contains(slots[s] + ":"),
                    "ProtocolAbilityBarView." + slots[s] + " missing");
            }
            for (int f = 0; f < fields.Length; f++)
            {
                var matches = Regex.Matches(viewBlock, fields[f] + @" \{fileID: (\d+)\}");
                Assert.AreEqual(3, matches.Count, "ProtocolAbilityBarView." + fields[f] + " count");
                for (int i = 0; i < matches.Count; i++)
                    Assert.AreNotEqual("0", matches[i].Groups[1].Value,
                        "ProtocolAbilityBarView." + fields[f] + " unbound");
            }
            Assert.AreEqual(3, Regex.Matches(yaml, @"m_SizeDelta: \{x: 24, y: 20\}").Count,
                "Protocol Bar Key rects must be 24x20 (font 16)");
            // Three slot frames, each Cut Frame Filled and Sliced. This pinned three HARDCODED
            // fileIDs until 2026-08-22 — 1786971113/33/53 — which `git log -S` shows never
            // existed in Bastion.unity in this project's history: the art was specified and never
            // authored, so the test had been red since the day it was written rather than since
            // anything broke. Unity assigns fileIDs, so no builder can produce those numbers, and
            // pinning them tests serialization identity rather than what reaches the screen.
            // What the frames ARE is pinned instead, so re-authoring cannot break it wrongly.
            Assert.AreEqual(3, Regex.Matches(yaml, @"m_Name: Frame\r?\n").Count,
                "Protocol Bar needs exactly three slot frames");
            Assert.GreaterOrEqual(
                Regex.Matches(yaml, "guid: dc2366542d8657842a8bf5378c77bee0").Count, 3,
                "Protocol Bar frames must use Cut Frame Filled");
        }

        [Test]
        public void HudVitals_HostStays400x150()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity"));
            int nameAt = yaml.IndexOf("m_Name: Hud Vitals\n", System.StringComparison.Ordinal);
            if (nameAt < 0)
                nameAt = yaml.IndexOf("m_Name: Hud Vitals\r\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(nameAt, 0, "Hud Vitals GO missing");
            int go = yaml.LastIndexOf("--- !u!1 &", nameAt, System.StringComparison.Ordinal);
            int end = yaml.IndexOf("\n--- !u!", go + 5, System.StringComparison.Ordinal);
            string goBlock = yaml.Substring(go, end - go);
            var cm = Regex.Match(goBlock, @"component: \{fileID: (\d+)\}");
            Assert.IsTrue(cm.Success, "Hud Vitals has no first component");
            string rid = cm.Groups[1].Value;
            int s = yaml.IndexOf("--- !u!224 &" + rid + "\n", System.StringComparison.Ordinal);
            if (s < 0)
                s = yaml.IndexOf("--- !u!224 &" + rid + "\r\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(s, 0, "Hud Vitals RectTransform missing");
            int e = yaml.IndexOf("\n--- !u!", s + 5, System.StringComparison.Ordinal);
            string rt = yaml.Substring(s, e - s);
            Assert.IsTrue(rt.Contains("m_SizeDelta: {x: 400, y: 150}"),
                "Hud Vitals sizeDelta must stay 400 x 150\n" + rt);
        }
    }
}
