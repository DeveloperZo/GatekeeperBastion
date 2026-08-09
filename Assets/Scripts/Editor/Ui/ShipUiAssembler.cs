using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GatekeeperBastion.Game;
using GatekeeperBastion.Game.Ui;
using Michsky.UI.Shift;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>
    /// Assembles ShipHud / StationPanel / ShortcutPanel as Shift Canvas (or Panel Button) variants
    /// with game vocabulary, L-bracket Hatch amber motif, and SerializeField wiring on views.
    /// Menu: GatekeeperBastion/Assemble Ship UI. Batch: AssembleFromBatch.
    /// </summary>
    public static class ShipUiAssembler
    {
        const string PrefabFolder = "Assets/Prefabs/Ui";
        const string ShiftCanvas =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Other/Canvas.prefab";
        const string ShiftPanelButton =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Panel Button.prefab";
        const string ShiftMainButton =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Main Button.prefab";
        const string ShiftScrollbar =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Scrollbar/Scrollbar.prefab";
        const string BodyFontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string DisplayFontPath =
            "Assets/Shift - Complete Sci-Fi UI/Fonts/Rajdhani-Bold SDF.asset";

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string ReportPath => Path.Combine(Root, "gkb_ship_ui_report.json");

        [MenuItem("GatekeeperBastion/Assemble Ship UI")]
        public static void AssembleFromMenu() => AssembleAll("menu");

        /// <summary>Batchmode entry — assemble ship screens then exit with report.</summary>
        public static void AssembleFromBatch()
        {
            AssembleAll("batch");
            bool ok = File.Exists(ReportPath) &&
                      File.ReadAllText(ReportPath).Contains("\"status\":\"ok\"");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Alias for batch scripts that look for BuildFromBatch.</summary>
        public static void BuildFromBatch() => AssembleFromBatch();

        public static void AssembleAll(string trigger)
        {
            EnsurePrefabFolder();
            var built = new List<string>();
            var errors = new List<string>();

            try
            {
                built.Add(BuildShortcutPanel());
            }
            catch (Exception e)
            {
                errors.Add("ShortcutPanel: " + FormatEx(e));
            }

            try
            {
                built.Add(BuildShipHud());
            }
            catch (Exception e)
            {
                errors.Add("ShipHud: " + FormatEx(e));
            }

            try
            {
                built.Add(BuildStationPanel());
            }
            catch (Exception e)
            {
                errors.Add("StationPanel: " + FormatEx(e));
            }

            string status = errors.Count == 0 ? "ok" : "error";
            string message = errors.Count == 0
                ? $"Assembled {built.Count} ship UI prefab(s) ({trigger})."
                : string.Join("; ", errors);
            WriteReport(status, message, built, errors);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (errors.Count == 0) Debug.Log("[ShipUiAssembler] " + message);
            else Debug.LogWarning("[ShipUiAssembler] " + message);
        }

        /// <summary>Called from UiPrefabBuilder so skeleton pass does not wipe ship screens.</summary>
        public static bool TryAssemble(string prefabName, out string error)
        {
            error = null;
            try
            {
                switch (prefabName)
                {
                    case "ShortcutPanel":
                        BuildShortcutPanel();
                        return true;
                    case "ShipHud":
                        BuildShipHud();
                        return true;
                    case "StationPanel":
                        BuildStationPanel();
                        return true;
                    default:
                        error = "not a ship screen";
                        return false;
                }
            }
            catch (Exception e)
            {
                error = FormatEx(e);
                return false;
            }
        }

        static string BuildShipHud()
        {
            var canvasSource = LoadRequired(ShiftCanvas);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(canvasSource);
            root.name = "ShipHud";
            var canvasRt = root.GetComponent<RectTransform>();

            // Strip Event System from HUD canvas — scene already has one.
            var es = root.transform.Find("Event System");
            if (es != null) UnityEngine.Object.DestroyImmediate(es.gameObject);

            EnsureCanvasScaler(root);

            var view = root.GetComponent<ShipHudView>() ?? root.AddComponent<ShipHudView>();
            if (root.GetComponent<ShipHud>() == null) root.AddComponent<ShipHud>();

            var content = CreateRt(canvasRt, "Content");
            Stretch(content);

            var header = CreatePanel(content, "Header", Theme.Panel);
            SetAnchored(header, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(25f + 350f, -25f - 70f), new Vector2(700f, 140f));
            AddLBrackets(header, Theme.Accent);

            var headerStack = CreateVertical(header, "Stack", 0f);
            Stretch(headerStack, new Vector2(25f, 25f), new Vector2(-30f, -25f));

            var title = CreateText(headerStack, "Title", "ABOARD YOUR SHIP", Theme.Title, Theme.Ink,
                FontStyles.Bold, TextAlignmentOptions.Left, display: true);
            var salvage = CreateText(headerStack, "Salvage", "Banked salvage: 0", Theme.Body, Theme.InkDim);
            var objective = CreateText(headerStack, "Objective", "OBJECTIVE", Theme.Head, Theme.Warn,
                FontStyles.Bold);
            var directory = CreateText(headerStack, "Directory",
                "ARMORY: west   |   FABRICATION: east   |   NAV MAP: north   |   CREW QUARTERS: south",
                Theme.Small, Theme.InkDim);

            var crosshair = CreateText(content, "Crosshair", "+", Theme.Display, Theme.Ink,
                FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchored(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(50f, 50f));

            var prompt = CreateText(content, "Prompt", "", Theme.Head, Theme.Ink,
                FontStyles.Bold, TextAlignmentOptions.Center);
            var promptRt = prompt.rectTransform;
            promptRt.anchorMin = new Vector2(0f, 0f);
            promptRt.anchorMax = new Vector2(1f, 0f);
            promptRt.pivot = new Vector2(0.5f, 0f);
            promptRt.anchoredPosition = new Vector2(0f, 85f);
            promptRt.sizeDelta = new Vector2(-470f, 35f);
            prompt.gameObject.SetActive(false);

            // Nested ShortcutPanel variant instance
            var shortcutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/ShortcutPanel.prefab");
            ShortcutPanelView shortcuts = null;
            if (shortcutPrefab != null)
            {
                var shortcutGo = (GameObject)PrefabUtility.InstantiatePrefab(shortcutPrefab, content);
                shortcutGo.name = "ShortcutPanel";
                var srt = shortcutGo.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(0f, 0f);
                srt.pivot = new Vector2(0f, 0f);
                srt.anchoredPosition = new Vector2(25f, 25f);
                srt.sizeDelta = new Vector2(420f, 280f);
                shortcuts = shortcutGo.GetComponent<ShortcutPanelView>();
            }

            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = content;
            so.FindProperty("_headerPanel").objectReferenceValue = header.gameObject;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_salvageLine").objectReferenceValue = salvage;
            so.FindProperty("_objective").objectReferenceValue = objective;
            so.FindProperty("_directory").objectReferenceValue = directory;
            so.FindProperty("_crosshair").objectReferenceValue = crosshair;
            so.FindProperty("_prompt").objectReferenceValue = prompt;
            so.FindProperty("_shortcuts").objectReferenceValue = shortcuts;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveVariant(root, $"{PrefabFolder}/ShipHud.prefab");
        }

        static string BuildStationPanel()
        {
            var canvasSource = LoadRequired(ShiftCanvas);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(canvasSource);
            root.name = "StationPanel";
            var es = root.transform.Find("Event System");
            if (es != null) UnityEngine.Object.DestroyImmediate(es.gameObject);
            EnsureCanvasScaler(root);

            var view = root.GetComponent<StationPanelView>() ?? root.AddComponent<StationPanelView>();
            var canvasRt = root.GetComponent<RectTransform>();

            var rootRt = CreateRt(canvasRt, "Root");
            Stretch(rootRt);
            rootRt.gameObject.SetActive(false);

            var scrim = CreatePanel(rootRt, "Scrim", new Color(0f, 0f, 0f, 0.49f));
            Stretch(scrim);

            var panel = CreatePanel(rootRt, "Panel", new Color(0f, 0f, 0f, 0.88f));
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 700f));
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = Theme.Accent;
            border.effectDistance = new Vector2(1f, -1f);
            AddLBrackets(panel, Theme.Accent);

            var stack = CreateVertical(panel, "Stack", 25f);
            Stretch(stack, new Vector2(25f, 25f), new Vector2(-35f, -25f));
            stack.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            stack.GetComponent<VerticalLayoutGroup>().childControlHeight = false;
            stack.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

            var title = CreateText(stack, "Title", "ARMORY", Theme.Title, Theme.Ink,
                FontStyles.Bold, TextAlignmentOptions.Center, display: true);
            LayoutPreferred(title.gameObject, -1f, 45f);
            var banked = CreateText(stack, "Banked", "Banked salvage: 0", Theme.Head, Theme.Ink,
                FontStyles.Bold, TextAlignmentOptions.Center);
            LayoutPreferred(banked.gameObject, -1f, 35f);
            var lifetime = CreateText(stack, "Lifetime", "Lifetime: 0 earned  —  0 spent", Theme.Body,
                Theme.InkDim, FontStyles.Normal, TextAlignmentOptions.Center);
            LayoutPreferred(lifetime.gameObject, -1f, 30f);

            var wheelSlot = CreateRt(stack, "WheelSlot");
            LayoutPreferred(wheelSlot.gameObject, -1f, 220f);
            var wheelLe = wheelSlot.gameObject.GetComponent<LayoutElement>() ??
                          wheelSlot.gameObject.AddComponent<LayoutElement>();
            wheelLe.minHeight = 220f;
            wheelLe.preferredHeight = 220f;

            // Scroll body
            var scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(ScrollRect),
                typeof(Image), typeof(LayoutElement));
            scrollGo.transform.SetParent(stack, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(0f, 320f);
            var scrollLe = scrollGo.GetComponent<LayoutElement>();
            scrollLe.minHeight = 320f;
            scrollLe.preferredHeight = 320f;
            scrollLe.flexibleHeight = 1f;
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.39f);
            scrollImg.sprite = WhiteSprite();
            scrollImg.type = Image.Type.Simple;

            var viewport = CreateRt(scrollRt, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.sprite = WhiteSprite();

            var bodyContent = CreateRt(viewport, "Content");
            bodyContent.anchorMin = new Vector2(0f, 1f);
            bodyContent.anchorMax = new Vector2(1f, 1f);
            bodyContent.pivot = new Vector2(0.5f, 1f);
            bodyContent.anchoredPosition = Vector2.zero;
            bodyContent.sizeDelta = new Vector2(0f, 0f);
            var bodyVlg = bodyContent.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyVlg.spacing = 25f;
            bodyVlg.padding = new RectOffset(25, 30, 25, 25);
            bodyVlg.childAlignment = TextAnchor.UpperCenter;
            bodyVlg.childControlWidth = true;
            bodyVlg.childControlHeight = true;
            bodyVlg.childForceExpandWidth = true;
            bodyVlg.childForceExpandHeight = false;
            var bodyFitter = bodyContent.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var stagger = bodyContent.gameObject.AddComponent<StaggerEntrance>();

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = bodyContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var scrollbarSource = AssetDatabase.LoadAssetAtPath<GameObject>(ShiftScrollbar);
            if (scrollbarSource != null)
            {
                var sbGo = (GameObject)PrefabUtility.InstantiatePrefab(scrollbarSource, scrollRt);
                sbGo.name = "Scrollbar";
                var sbRt = sbGo.GetComponent<RectTransform>();
                sbRt.anchorMin = new Vector2(1f, 0f);
                sbRt.anchorMax = new Vector2(1f, 1f);
                sbRt.pivot = new Vector2(1f, 0.5f);
                sbRt.sizeDelta = new Vector2(25f, 0f);
                sbRt.anchoredPosition = Vector2.zero;
                var sb = sbGo.GetComponent<Scrollbar>();
                if (sb != null)
                {
                    scroll.verticalScrollbar = sb;
                    scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                }
            }

            // CLOSE — Shift Panel Button nested instance
            var panelBtnSource = LoadRequired(ShiftPanelButton);
            var closeGo = (GameObject)PrefabUtility.InstantiatePrefab(panelBtnSource, stack);
            closeGo.name = "Close";
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.sizeDelta = new Vector2(280f, 50f);
            LayoutPreferred(closeGo, 280f, 50f);
            var closeBtn = closeGo.GetComponent<Button>();
            var panelBtn = closeGo.GetComponent<MainPanelButton>();
            if (panelBtn != null)
            {
                panelBtn.buttonText = "CLOSE";
                panelBtn.useCustomText = false;
            }
            if (closeGo.GetComponent<UIElementSound>() == null)
                closeGo.AddComponent<UIElementSound>();

            // Templates (inactive) — Instantiated at runtime by StationPanelView
            var templates = CreateRt(rootRt, "Templates");
            templates.gameObject.SetActive(false);

            var labelPrefab = CreateText(templates, "LabelTemplate", "Label", Theme.Body, Theme.InkDim);
            var labelLe = labelPrefab.gameObject.AddComponent<LayoutElement>();
            labelLe.minHeight = 25f;
            labelLe.preferredHeight = 28f;
            labelPrefab.gameObject.SetActive(false);

            var spacerPrefab = CreateRt(templates, "SpacerTemplate");
            var spacerLe = spacerPrefab.gameObject.AddComponent<LayoutElement>();
            spacerLe.minHeight = 25f;
            spacerLe.preferredHeight = 25f;
            spacerPrefab.gameObject.SetActive(false);

            var rowHost = CreateRt(templates, "RowHostTemplate");
            var rowHlg = rowHost.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 25f;
            rowHlg.childAlignment = TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;
            var rowHostLe = rowHost.gameObject.AddComponent<LayoutElement>();
            rowHostLe.minHeight = 50f;
            rowHostLe.preferredHeight = 50f;
            rowHost.gameObject.SetActive(false);

            var btnSource = LoadRequired(ShiftMainButton);
            var buttonTemplateGo = (GameObject)PrefabUtility.InstantiatePrefab(btnSource, templates);
            buttonTemplateGo.name = "ButtonTemplate";
            buttonTemplateGo.SetActive(false);
            var buttonTemplate = buttonTemplateGo.GetComponent<Button>();
            if (buttonTemplateGo.GetComponent<LayoutElement>() == null)
                buttonTemplateGo.AddComponent<LayoutElement>();
            if (buttonTemplateGo.GetComponent<UIElementSound>() == null)
                buttonTemplateGo.AddComponent<UIElementSound>();
            var mainBtn = buttonTemplateGo.GetComponent<MainButton>();
            if (mainBtn != null)
            {
                mainBtn.buttonText = "BUY";
                mainBtn.useCustomText = false;
            }

            var rowPrefab = BuildPricedRowTemplate(templates, btnSource);

            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = rootRt;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_banked").objectReferenceValue = banked;
            so.FindProperty("_lifetime").objectReferenceValue = lifetime;
            so.FindProperty("_bodyContent").objectReferenceValue = bodyContent;
            so.FindProperty("_wheelSlot").objectReferenceValue = wheelSlot;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("_rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("_labelPrefab").objectReferenceValue = labelPrefab;
            so.FindProperty("_buttonPrefab").objectReferenceValue = buttonTemplate;
            so.FindProperty("_spacerPrefab").objectReferenceValue = spacerPrefab;
            so.FindProperty("_rowHostPrefab").objectReferenceValue = rowHost;
            so.FindProperty("_bodyStagger").objectReferenceValue = stagger;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveVariant(root, $"{PrefabFolder}/StationPanel.prefab");
        }

        static StationRowView BuildPricedRowTemplate(Transform templates, GameObject btnSource)
        {
            var rowRt = CreateRt(templates, "PricedRowTemplate");
            var hlg = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 25f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var rowLe = rowRt.gameObject.AddComponent<LayoutElement>();
            rowLe.minHeight = 50f;
            rowLe.preferredHeight = 50f;

            var name = CreateText(rowRt, "Name", "Name", Theme.Body, Theme.Ink);
            LayoutPreferred(name.gameObject, 160f, 50f);
            var cost = CreateText(rowRt, "Cost", "cost 0", Theme.Body, Theme.InkDim);
            LayoutPreferred(cost.gameObject, 100f, 50f);
            var level = CreateText(rowRt, "Level", "0/5", Theme.Body, Theme.InkDim);
            LayoutPreferred(level.gameObject, 70f, 50f);
            var effect = CreateText(rowRt, "Effect", "effect", Theme.Small, Theme.InkDim);
            LayoutPreferred(effect.gameObject, 220f, 50f);
            var reason = CreateText(rowRt, "Reason", "", Theme.Small, Theme.InkFaint, FontStyles.Italic);
            LayoutPreferred(reason.gameObject, 160f, 50f);
            reason.gameObject.SetActive(false);

            var actionGo = (GameObject)PrefabUtility.InstantiatePrefab(btnSource, rowRt);
            actionGo.name = "Action";
            LayoutPreferred(actionGo, 150f, 50f);
            if (actionGo.GetComponent<LayoutElement>() == null)
                actionGo.AddComponent<LayoutElement>();
            if (actionGo.GetComponent<UIElementSound>() == null)
                actionGo.AddComponent<UIElementSound>();
            var actionMain = actionGo.GetComponent<MainButton>();
            if (actionMain != null)
            {
                actionMain.buttonText = "BUY";
                actionMain.useCustomText = false;
            }

            var rowView = rowRt.gameObject.AddComponent<StationRowView>();
            var so = new SerializedObject(rowView);
            so.FindProperty("_name").objectReferenceValue = name;
            so.FindProperty("_cost").objectReferenceValue = cost;
            so.FindProperty("_level").objectReferenceValue = level;
            so.FindProperty("_effect").objectReferenceValue = effect;
            so.FindProperty("_reason").objectReferenceValue = reason;
            so.FindProperty("_action").objectReferenceValue = actionGo.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            rowRt.gameObject.SetActive(false);
            return rowView;
        }

        static string BuildShortcutPanel()
        {
            // Variant of Panel Button gives Shift ancestry; strip button chrome into a strip panel.
            var source = LoadRequired(ShiftPanelButton);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            root.name = "ShortcutPanel";

            // Disable button interaction — this is a legend strip, not a clickable Panel Button.
            var btn = root.GetComponent<Button>();
            if (btn != null) btn.enabled = false;
            var animator = root.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            var view = root.GetComponent<ShortcutPanelView>() ?? root.AddComponent<ShortcutPanelView>();
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(420f, 280f);

            // Clear default children labels that say LOGIN / placeholder — keep shell Image if present.
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            var panelImg = root.GetComponent<Image>();
            if (panelImg == null) panelImg = root.AddComponent<Image>();
            panelImg.color = Theme.Panel;
            panelImg.sprite = WhiteSprite();
            panelImg.raycastTarget = false;
            AddLBrackets(rootRt, Theme.Accent);

            var collapsed = CreateText(rootRt, "CollapsedTip", "BKSP — show keys", Theme.Body, Theme.InkDim,
                FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(collapsed.rectTransform, new Vector2(25f, 10f), new Vector2(-25f, -10f));

            var linesRoot = CreateRt(rootRt, "Lines");
            Stretch(linesRoot, new Vector2(25f, 25f), new Vector2(-25f, -25f));
            var vlg = linesRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0f;
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var pool = new TextMeshProUGUI[16];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = CreateText(linesRoot, $"Line{i}", "", Theme.Body, Theme.InkDim);
                LayoutPreferred(pool[i].gameObject, -1f, 25f);
                pool[i].gameObject.SetActive(false);
            }

            var so = new SerializedObject(view);
            so.FindProperty("_panel").objectReferenceValue = root;
            so.FindProperty("_collapsedTip").objectReferenceValue = collapsed;
            so.FindProperty("_linesRoot").objectReferenceValue = linesRoot;
            var poolProp = so.FindProperty("_linePool");
            poolProp.arraySize = pool.Length;
            for (int i = 0; i < pool.Length; i++)
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveVariant(root, $"{PrefabFolder}/ShortcutPanel.prefab");
        }

        // --- helpers ---

        static void AddLBrackets(RectTransform parent, Color color)
        {
            // Single motif: L-bracket at each corner (two bars each), Hatch amber.
            const float thick = 3f;
            const float len = 25f;
            AddBracketArm(parent, "BracketTL_H", color, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(len * 0.5f, -thick * 0.5f), new Vector2(len, thick));
            AddBracketArm(parent, "BracketTL_V", color, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(thick * 0.5f, -len * 0.5f), new Vector2(thick, len));

            AddBracketArm(parent, "BracketTR_H", color, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-len * 0.5f, -thick * 0.5f), new Vector2(len, thick));
            AddBracketArm(parent, "BracketTR_V", color, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-thick * 0.5f, -len * 0.5f), new Vector2(thick, len));

            AddBracketArm(parent, "BracketBL_H", color, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(len * 0.5f, thick * 0.5f), new Vector2(len, thick));
            AddBracketArm(parent, "BracketBL_V", color, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(thick * 0.5f, len * 0.5f), new Vector2(thick, len));

            AddBracketArm(parent, "BracketBR_H", color, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-len * 0.5f, thick * 0.5f), new Vector2(len, thick));
            AddBracketArm(parent, "BracketBR_V", color, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-thick * 0.5f, len * 0.5f), new Vector2(thick, len));
        }

        static void AddBracketArm(RectTransform parent, string name, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var rt = CreateRt(parent, name);
            SetAnchored(rt, aMin, aMax, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = color;
            img.raycastTarget = false;
        }

        static void EnsureCanvasScaler(GameObject canvasRoot)
            => UiPrefabVariant.EnsureCanvasScaler(canvasRoot);

        static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
            Color color, FontStyles style = FontStyles.Normal,
            TextAlignmentOptions align = TextAlignmentOptions.Left, bool display = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text ?? "";
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.characterSpacing = 0f;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(display ? DisplayFontPath : BodyFontPath);
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
            if (font != null) tmp.font = font;
            return tmp;
        }

        static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = color;
            img.raycastTarget = true;
            return go.GetComponent<RectTransform>();
        }

        static RectTransform CreateVertical(Transform parent, string name, float spacing)
        {
            var rt = CreateRt(parent, name);
            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return rt;
        }

        static RectTransform CreateRt(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rt)
        {
            Stretch(rt, Vector2.zero, Vector2.zero);
        }

        static void Stretch(RectTransform rt, Vector2 minPad, Vector2 maxPad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = minPad;
            rt.offsetMax = maxPad;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetAnchored(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void LayoutPreferred(GameObject go, float width, float height)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                le.preferredWidth = width;
                le.minWidth = width;
            }
            if (height >= 0f)
            {
                le.preferredHeight = height;
                le.minHeight = height;
            }
        }

        static Sprite WhiteSprite()
        {
            var builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (builtin != null) return builtin;
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        static GameObject LoadRequired(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) throw new InvalidOperationException("Missing asset: " + path);
            return go;
        }

        static string SaveVariant(GameObject instance, string destPath)
            => UiPrefabVariant.Save(instance, destPath);

        static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Ui");
        }

        static string FormatEx(Exception e)
        {
            if (e.InnerException != null) e = e.InnerException;
            return e.GetType().Name + ": " + e.Message;
        }

        static void WriteReport(string status, string message, List<string> prefabs, List<string> errors)
        {
            var sb = new StringBuilder();
            sb.Append("{\"status\":").Append(Quote(status))
              .Append(",\"message\":").Append(Quote(Truncate(message ?? "", 800)))
              .Append(",\"unity\":").Append(Quote(Application.unityVersion))
              .Append(",\"timestampUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")))
              .Append(",\"prefabs\":").Append(StringList(prefabs))
              .Append(",\"errors\":").Append(StringList(errors))
              .Append('}');
            try { File.WriteAllText(ReportPath, sb.ToString()); }
            catch (Exception e) { Debug.LogError("[ShipUiAssembler] report: " + e.Message); }
        }

        static string StringList(List<string> items)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Quote(items[i]));
            }
            return sb.Append(']').ToString();
        }

        static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') { }
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }
    }
}
