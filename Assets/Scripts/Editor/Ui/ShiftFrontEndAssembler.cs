using System;
using System.Collections.Generic;
using System.IO;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using Michsky.UI.Shift;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Builds Splash / MainMenu / Options / ConfirmModal as Shift prefab variants
    /// under Assets/Prefabs/Ui/. Editor-only assembly — runtime views bind SerializeFields.</summary>
    public static class ShiftFrontEndAssembler
    {
        const string PrefabFolder = "Assets/Prefabs/Ui";
        const string ResourcesUi = "Assets/Resources/Ui";
        const string ShiftRoot = "Assets/Shift - Complete Sci-Fi UI/Prefabs";
        const string CanvasPath = ShiftRoot + "/Other/Canvas.prefab";
        const string MainButtonPath = ShiftRoot + "/Button/Main Button.prefab";
        const string PanelButtonPath = ShiftRoot + "/Button/Panel Button.prefab";
        const string SliderPath = ShiftRoot + "/Slider/Slider.prefab";
        const string SwitchPath = ShiftRoot + "/Switch/Switch.prefab";
        const string ModalPath = ShiftRoot + "/Modal Window/Exit.prefab";
        const string SelectorPath = ShiftRoot + "/Horizontal Selector/Horizontal Selector.prefab";
        const string UiManagerPath = "Assets/Shift - Complete Sci-Fi UI/Resources/Shift UI Manager.asset";

        static readonly string[] TabNames =
            { "Graphics", "Audio", "Controls", "Gameplay", "Accessibility" };

        // Reachable via GatekeeperBastion/Build UI Prefabs (UiPrefabBuilder) — no separate menu.
        public static void BuildFromMenu() => BuildAll("menu");

        public static void BuildFromBatch()
        {
            BuildAll("batch");
            EditorApplication.Exit(0);
        }

        public static void BuildAll(string trigger)
        {
            EnsureFolders();
            var built = new List<string>();
            try
            {
                built.Add(BuildConfirmModal());
                built.Add(BuildSplash());
                built.Add(BuildMainMenu());
                built.Add(BuildOptions());
                Debug.Log($"[ShiftFrontEndAssembler] Built {built.Count} prefab(s) ({trigger}).");
            }
            catch (Exception e)
            {
                Debug.LogError("[ShiftFrontEndAssembler] " + e);
                throw;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Ui");
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ResourcesUi))
                AssetDatabase.CreateFolder("Assets/Resources", "Ui");
        }

        static GameObject LoadShift(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Missing Shift prefab", path);
            return prefab;
        }

        static GameObject InstantiateCanvas(string name)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(CanvasPath));
            root.name = name;
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
            }
            UiPrefabVariant.EnsureCanvasScaler(root);
            // Shift Canvas ships a nested Event System — scene/runtime Ensure owns the singleton.
            // Leaving both creates dual EventSystems (console spam + dead uGUI).
            StripNestedEventSystems(root.transform);
            EnsureUiAudio(root.transform);
            return root;
        }

        static void StripNestedEventSystems(Transform root)
        {
            var systems = root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                    UnityEngine.Object.DestroyImmediate(systems[i].gameObject);
            }
        }

        static void EnsureUiAudio(Transform canvas)
        {
            var existing = canvas.Find("UI Audio");
            if (existing != null) return;
            var go = new GameObject("UI Audio");
            go.transform.SetParent(canvas, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
        }

        static string SaveVariant(GameObject root, string path)
            => UiPrefabVariant.Save(root, path);

        static void CopyIntoResources(string prefabPath, string fileName)
        {
            string dest = $"{ResourcesUi}/{fileName}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dest) != null)
                AssetDatabase.DeleteAsset(dest);
            if (!AssetDatabase.CopyAsset(prefabPath, dest))
                throw new IOException("CopyAsset failed: " + prefabPath + " → " + dest);
        }

        static string BuildConfirmModal()
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(ModalPath));
            root.name = "ConfirmModal";
            var modal = root.GetComponent<ModalWindowManager>();
            if (modal != null)
            {
                modal.useCustomTexts = true;
                modal.titleText = "Confirm";
                modal.descriptionText = "";
            }
            ApplyVerbLabels(root, "Confirm", "Cancel");
            string path = SaveVariant(root, $"{PrefabFolder}/ConfirmModal.prefab");
            CopyIntoResources(path, "ConfirmModal.prefab");
            return path;
        }

        static string BuildSplash()
        {
            var root = InstantiateCanvas("Splash");
            var content = CreateRect("SplashContent", root.transform, stretch: true);
            content.gameObject.AddComponent<StaggerEntrance>();

            var logoGo = CreateRect("Logo", content, new Vector2(0.5f, 0.58f), new Vector2(420, 120));
            var logoImage = logoGo.gameObject.AddComponent<Image>();
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            var uiManager = AssetDatabase.LoadAssetAtPath<UIManager>(UiManagerPath);
            if (uiManager != null && uiManager.gameLogo != null)
            {
                logoImage.sprite = uiManager.gameLogo;
                logoImage.color = Theme.Ink;
            }
            else
                logoImage.color = Theme.Accent;
            var logoCg = logoGo.gameObject.AddComponent<CanvasGroup>();

            var title = CreateTmp(content, "Title", "GATEKEEPER BASTION", 40f, Theme.Ink,
                new Vector2(0.5f, 0.48f), new Vector2(900, 50));
            var version = CreateTmp(content, "Version", Application.version, 28f, Theme.InkDim,
                new Vector2(0.5f, 0.42f), new Vector2(900, 36));

            var pressRoot = CreateRect("PressAnyKey", content, new Vector2(0.5f, 0.22f), new Vector2(600, 40));
            var pressCg = pressRoot.gameObject.AddComponent<CanvasGroup>();
            var press = CreateTmp(pressRoot, "Label", "press any key", 28f, Theme.InkDim,
                new Vector2(0.5f, 0.5f), new Vector2(600, 40));
            pressCg.gameObject.SetActive(false);

            AddLBrackets(content);

            // SplashScreenManager needs demo Animator clips on splash/mainPanels; wiring it
            // without those clips NREs in OnEnable. SplashController owns the load-gate flow.
            // Controllers live ON the prefab — scene-added components break when nested Shift
            // fileIDs change (Splash.unity once had m_AddedComponents: [] and no input).
            root.AddComponent<GatekeeperBastion.Game.SplashController>();

            var view = root.AddComponent<SplashView>();
            SetPrivate(view, "_logoGroup", logoCg);
            SetPrivate(view, "_version", version);
            SetPrivate(view, "_subtitle", press);
            SetPrivate(view, "_pressGroup", pressCg);

            return SaveVariant(root, $"{PrefabFolder}/Splash.prefab");
        }

        static string BuildMainMenu()
        {
            var root = InstantiateCanvas("MainMenu");

            var backdrop = CreateRect("ArenaScrim", root.transform, stretch: true);
            var bg = backdrop.gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.045f, 0.06f, 1f);
            bg.raycastTarget = false;
            var drift = CreateRect("DriftAccent", backdrop, stretch: true);
            var driftImg = drift.gameObject.AddComponent<Image>();
            MenuArenaBackdrop.ApplyRestrainedLook(driftImg);
            var backdropComp = backdrop.gameObject.AddComponent<MenuArenaBackdrop>();
            SetPrivate(backdropComp, "_drift", drift);

            var panel = CreateRect("MainPanel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(520, 560));
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.69f);
            AddAccentBorder(panel);
            AddLBrackets(panel);
            panel.gameObject.AddComponent<StaggerEntrance>();

            var title = CreateTmp(panel, "Title", "GATEKEEPER BASTION", 36f, Theme.Ink,
                new Vector2(0.5f, 0.92f), new Vector2(460, 40));

            var strip = CreateRect("CampaignStrip", panel, new Vector2(0.5f, 0.82f), new Vector2(460, 50));
            var banked = CreateTmp(strip, "Banked", "0 banked", 25f, Theme.InkDim,
                new Vector2(0.2f, 0.5f), new Vector2(140, 30));
            var runs = CreateTmp(strip, "Runs", "0 run(s)", 25f, Theme.InkDim,
                new Vector2(0.5f, 0.5f), new Vector2(140, 30));
            var deepest = CreateTmp(strip, "Deepest", "deepest Wave 1", 25f, Theme.InkDim,
                new Vector2(0.82f, 0.5f), new Vector2(180, 30));
            var campaignLine = CreateTmp(panel, "CampaignLine", "", 25f, Theme.InkDim,
                new Vector2(0.5f, 0.76f), new Vector2(460, 30));
            campaignLine.gameObject.SetActive(false);

            var list = CreateRect("Buttons", panel, new Vector2(0.5f, 0.42f), new Vector2(400, 320));
            var accent = CreateRect("AccentBar", list, new Vector2(0f, 0.85f), new Vector2(4, 36));
            accent.anchoredPosition = new Vector2(-12f, 0f);
            var accentImg = accent.gameObject.AddComponent<Image>();
            accentImg.color = Theme.Accent;

            var continueGo = SpawnMainButton(list, "CONTINUE", new Vector2(0f, 120f));
            var startGo = SpawnMainButton(list, "START NEW", new Vector2(0f, 50f));
            var optionsGo = SpawnMainButton(list, "OPTIONS", new Vector2(0f, -20f));
            var quitGo = SpawnMainButton(list, "QUIT", new Vector2(0f, -90f));

            var modal = SpawnNestedModal(root.transform, "OverwriteModal");
            var yesBtn = FindDeepButton(modal.transform, "Yes");
            var cancelBtn = FindDeepButton(modal.transform, "Cancel");
            ApplyVerbLabels(modal, "Overwrite and start new", "Keep my save");

            var view = root.AddComponent<MainMenuView>();
            SetPrivate(view, "_mainRoot", panel.gameObject);
            SetPrivate(view, "_continueButton", continueGo);
            SetPrivate(view, "_campaignBanked", banked);
            SetPrivate(view, "_campaignRuns", runs);
            SetPrivate(view, "_campaignDeepest", deepest);
            SetPrivate(view, "_campaignLine", campaignLine);
            SetPrivate(view, "_accentBar", accent);
            SetPrivate(view, "_continueBtn", continueGo.GetComponent<Button>());
            SetPrivate(view, "_startNewBtn", startGo.GetComponent<Button>());
            SetPrivate(view, "_optionsBtn", optionsGo.GetComponent<Button>());
            SetPrivate(view, "_quitBtn", quitGo.GetComponent<Button>());
            SetPrivate(view, "_overwriteModal", modal.GetComponent<ModalWindowManager>());
            SetPrivate(view, "_overwriteConfirmBtn", yesBtn);
            SetPrivate(view, "_overwriteCancelBtn", cancelBtn);
            SetPrivate(view, "_overwriteConfirmLabel", FindLabel(yesBtn));
            SetPrivate(view, "_overwriteCancelLabel", FindLabel(cancelBtn));

            // Controllers on the prefab — scene-added MainMenuController breaks when Shift
            // nested fileIDs churn (same failure mode as Splash).
            root.AddComponent<GatekeeperBastion.Game.MainMenuController>();

            // silence unused
            _ = title;

            return SaveVariant(root, $"{PrefabFolder}/MainMenu.prefab");
        }

        static string BuildOptions()
        {
            var root = InstantiateCanvas("Options");
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 200;

            var scrim = CreateRect("Scrim", root.transform, stretch: true);
            var scrimImg = scrim.gameObject.AddComponent<Image>();
            scrimImg.color = Theme.ModalDim;

            var panel = CreateRect("OptionsPanel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(860, 620));
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.88f);
            AddAccentBorder(panel);
            AddLBrackets(panel);
            panel.gameObject.AddComponent<StaggerEntrance>();

            CreateTmp(panel, "Title", "OPTIONS", 36f, Theme.Ink,
                new Vector2(0.5f, 0.93f), new Vector2(700, 40));

            var tabsRow = CreateRect("Tabs", panel, new Vector2(0.5f, 0.84f), new Vector2(800, 50));
            var tabButtons = new Button[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                float x = (i - 2) * 155f;
                var tab = SpawnPanelButton(tabsRow, TabNames[i], new Vector2(x, 0f));
                tabButtons[i] = tab.GetComponent<Button>();
            }

            var pagesRoot = CreateRect("Pages", panel, new Vector2(0.5f, 0.48f), new Vector2(780, 380));
            var pages = new GameObject[TabNames.Length];
            var bindings = new List<OptionsView.OptionsRowBinding>();

            for (int i = 0; i < TabNames.Length; i++)
            {
                var page = CreateRect("Page_" + TabNames[i], pagesRoot, stretch: true);
                pages[i] = page.gameObject;
                page.gameObject.SetActive(i == 0);
                var vlg = page.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(25, 45, 25, 25);
                vlg.spacing = 25f;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            foreach (var d in Settings.Declarations)
            {
                int pageIndex = (int)d.Category;
                var page = pages[pageIndex].transform;
                var row = CreateRowHost(page, d.Label);
                var binding = new OptionsView.OptionsRowBinding
                {
                    RowIndex = (int)d.Id,
                    Category = pageIndex,
                    NameLabel = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                };

                if (d.Type == SettingType.Float)
                {
                    var sliderGo = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(SliderPath), row.transform);
                    sliderGo.name = "Slider_" + d.Id;
                    var rt = sliderGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(520, 40);
                    WireSounds(sliderGo);
                    binding.Slider = sliderGo.GetComponent<Slider>();
                    binding.ValueLabel = sliderGo.GetComponentInChildren<TextMeshProUGUI>(true);
                    var sm = sliderGo.GetComponent<SliderManager>();
                    if (sm != null) sm.enableSaving = false;
                }
                else if (d.Type == SettingType.Bool)
                {
                    var sw = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(SwitchPath), row.transform);
                    sw.name = "Switch_" + d.Id;
                    var sm = sw.GetComponent<SwitchManager>();
                    if (sm != null)
                    {
                        sm.saveValue = false;
                        sm.invokeAtStart = false;
                        sm.switchTag = "gkb_" + d.Id;
                    }
                    WireSounds(sw);
                    binding.Switch = sm;
                }
                else if (d.Type == SettingType.Int && d.Id == SettingId.DisplayMode)
                {
                    var sel = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(SelectorPath), row.transform);
                    sel.name = "Selector_DisplayMode";
                    var hs = sel.GetComponent<HorizontalSelector>();
                    if (hs != null)
                    {
                        hs.saveValue = false;
                        hs.invokeAtStart = false;
                        hs.itemList = new List<HorizontalSelector.Item>
                        {
                            new HorizontalSelector.Item { itemTitle = "Windowed" },
                            new HorizontalSelector.Item { itemTitle = "Fullscreen" },
                        };
                    }
                    WireSounds(sel);
                    binding.IntSelector = hs;
                }

                bindings.Add(binding);
            }

            // Zo 1A: Accessibility (and any empty tab) — Reset on the panel + one sentence, no fake rows.
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i].transform.childCount > 0) continue;
                AddEmptyPageCopy(pages[i].transform,
                    $"No {TabNames[i].ToLowerInvariant()} settings yet. Reset restores this page when options are added.");
            }

            var revert = CreateTmp(panel, "DisplayRevert", "", 25f, Theme.Warn,
                new Vector2(0.5f, 0.22f), new Vector2(700, 30));
            revert.gameObject.SetActive(false);

            var keepGo = SpawnMainButton(panel, "KEEP THIS MODE", new Vector2(0f, -200f));
            keepGo.SetActive(false);
            var resetGo = SpawnMainButton(panel, "RESET TO DEFAULTS", new Vector2(-160f, -260f));
            var backGo = SpawnMainButton(panel, "BACK", new Vector2(160f, -260f));

            var modal = SpawnNestedModal(root.transform, "ResetConfirmModal");
            var yesBtn = FindDeepButton(modal.transform, "Yes");
            var cancelBtn = FindDeepButton(modal.transform, "Cancel");
            ApplyVerbLabels(modal, "Reset to defaults", "Keep current");

            var view = root.AddComponent<OptionsView>();
            SetPrivate(view, "_root", root);
            SetPrivate(view, "_tabButtons", tabButtons);
            SetPrivate(view, "_pages", pages);
            SetPrivate(view, "_resetButton", resetGo.GetComponent<Button>());
            SetPrivate(view, "_backButton", backGo.GetComponent<Button>());
            SetPrivate(view, "_rowBindings", bindings.ToArray());
            SetPrivate(view, "_confirmModal", modal.GetComponent<ModalWindowManager>());
            SetPrivate(view, "_confirmYesBtn", yesBtn);
            SetPrivate(view, "_confirmNoBtn", cancelBtn);
            SetPrivate(view, "_confirmYesLabel", FindLabel(yesBtn));
            SetPrivate(view, "_confirmNoLabel", FindLabel(cancelBtn));
            SetPrivate(view, "_displayRevertLabel", revert);
            SetPrivate(view, "_keepDisplayButton", keepGo.GetComponent<Button>());

            root.SetActive(false);
            string path = SaveVariant(root, $"{PrefabFolder}/Options.prefab");
            CopyIntoResources(path, "Options.prefab");
            return path;
        }

        static RectTransform CreateRowHost(Transform page, string label)
        {
            var row = new GameObject("Row_" + label, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(page, false);
            var le = row.GetComponent<LayoutElement>();
            le.minHeight = 70f;
            le.preferredHeight = 70f;
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 25f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.padding = new RectOffset(0, 0, 0, 0);
            CreateTmp(row.transform, "Label", label, 28f, Theme.Ink,
                new Vector2(0f, 0.5f), new Vector2(280, 36));
            return row.GetComponent<RectTransform>();
        }

        static void AddEmptyPageCopy(Transform page, string text)
        {
            var go = new GameObject("EmptyCopy", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(page, false);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 100f;
            le.preferredHeight = 100f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28f;
            tmp.color = Theme.InkDim;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.characterSpacing = 0f;
            tmp.raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font != null) tmp.font = font;
        }

        static GameObject SpawnMainButton(Transform parent, string text, Vector2 anchored)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(MainButtonPath), parent);
            go.name = text.Replace(' ', '_');
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(360f, 50f);
            var mb = go.GetComponent<MainButton>();
            if (mb != null)
            {
                mb.buttonText = text;
                if (mb.normalText != null) mb.normalText.text = text;
                if (mb.highlightedText != null) mb.highlightedText.text = text;
                if (mb.pressedText != null) mb.pressedText.text = text;
            }
            WireSounds(go);
            return go;
        }

        static GameObject SpawnPanelButton(Transform parent, string text, Vector2 anchored)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(PanelButtonPath), parent);
            go.name = "Tab_" + text;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(140f, 45f);
            var pb = go.GetComponent<MainPanelButton>();
            if (pb != null)
            {
                pb.hasIcon = false;
                pb.buttonText = text;
                if (pb.normalText != null) pb.normalText.text = text;
                if (pb.highlightedText != null) pb.highlightedText.text = text;
                if (pb.pressedText != null) pb.pressedText.text = text;
            }
            WireSounds(go);
            return go;
        }

        static GameObject SpawnNestedModal(Transform canvas, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadShift(ModalPath), canvas);
            go.name = name;
            go.SetActive(false);
            return go;
        }

        static void WireSounds(GameObject go)
        {
            if (go.GetComponent<UIElementSound>() == null)
                go.AddComponent<UIElementSound>();
            if (go.GetComponent<UiPitchRandomizer>() == null)
                go.AddComponent<UiPitchRandomizer>();
        }

        static void ApplyVerbLabels(GameObject modalRoot, string yes, string no)
        {
            var yesBtn = FindDeepButton(modalRoot.transform, "Yes");
            var cancelBtn = FindDeepButton(modalRoot.transform, "Cancel");
            SetButtonTreeText(yesBtn, yes);
            SetButtonTreeText(cancelBtn, no);
            var modal = modalRoot.GetComponent<ModalWindowManager>();
            if (modal != null) modal.useCustomTexts = true;
        }

        static void SetButtonTreeText(Button btn, string text)
        {
            if (btn == null) return;
            var mb = btn.GetComponent<MainButton>();
            if (mb != null)
            {
                mb.buttonText = text;
                if (mb.normalText != null) mb.normalText.text = text;
                if (mb.highlightedText != null) mb.highlightedText.text = text;
                if (mb.pressedText != null) mb.pressedText.text = text;
            }
            foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.text = text;
        }

        static Button FindDeepButton(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                var b = t.GetComponent<Button>();
                if (b != null) return b;
            }
            return null;
        }

        static TextMeshProUGUI FindLabel(Button btn)
        {
            if (btn == null) return null;
            return btn.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static RectTransform CreateRect(string name, Transform parent, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            return rt;
        }

        static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float size,
            Color color, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 0f;
            tmp.raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font != null) tmp.font = font;
            return tmp;
        }

        static void AddAccentBorder(RectTransform panel)
        {
            var border = CreateRect("AccentBorder", panel, stretch: true);
            var img = border.gameObject.AddComponent<Image>();
            img.color = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.55f);
            img.raycastTarget = false;
            border.offsetMin = new Vector2(-1f, -1f);
            border.offsetMax = new Vector2(1f, 1f);
            border.SetAsFirstSibling();
            var fill = CreateRect("PanelFill", panel, stretch: true);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0f, 0f, 0f, 0.69f);
            fillImg.raycastTarget = true;
            fill.offsetMin = new Vector2(1f, 1f);
            fill.offsetMax = new Vector2(-1f, -1f);
            fill.SetSiblingIndex(1);
        }

        static void AddLBrackets(RectTransform panel)
        {
            SpawnBracket(panel, "BracketTL", new Vector2(0f, 1f), new Vector2(0f, 1f), 0f);
            SpawnBracket(panel, "BracketBR", new Vector2(1f, 0f), new Vector2(1f, 0f), 180f);
        }

        static void SpawnBracket(RectTransform panel, string name, Vector2 anchor, Vector2 pivot, float zRot)
        {
            var root = CreateRect(name, panel, anchor, new Vector2(30f, 30f));
            root.pivot = pivot;
            root.anchoredPosition = Vector2.zero;
            root.localEulerAngles = new Vector3(0f, 0f, zRot);
            var h = CreateRect("H", root, new Vector2(0f, 1f), new Vector2(30f, 3f));
            h.pivot = new Vector2(0f, 1f);
            h.anchoredPosition = Vector2.zero;
            var hImg = h.gameObject.AddComponent<Image>();
            hImg.color = Theme.Accent;
            hImg.raycastTarget = false;
            var v = CreateRect("V", root, new Vector2(0f, 1f), new Vector2(3f, 30f));
            v.pivot = new Vector2(0f, 1f);
            v.anchoredPosition = Vector2.zero;
            var vImg = v.gameObject.AddComponent<Image>();
            vImg.color = Theme.Accent;
            vImg.raycastTarget = false;
        }

        static void SetPrivate(object target, string field, object value)
        {
            var flags = System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Public;
            var f = target.GetType().GetField(field, flags);
            if (f == null)
                throw new MissingFieldException(target.GetType().Name, field);
            f.SetValue(target, value);
        }
    }
}
