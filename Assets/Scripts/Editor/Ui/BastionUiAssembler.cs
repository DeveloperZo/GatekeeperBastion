using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GatekeeperBastion.Game;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Assembles BastionHud, EndScreens and ArmoryWheel as Shift prefab variants.
    /// Invoked from UiPrefabBuilder (one menu: Build UI Prefabs). Does not edit vendor Shift prefabs.</summary>
    public static class BastionUiAssembler
    {
        const string PrefabFolder = "Assets/Prefabs/Ui";
        const string ShiftCanvas = "Assets/Shift - Complete Sci-Fi UI/Prefabs/Other/Canvas.prefab";
        const string ShiftPanelBtn = "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Panel Button.prefab";
        const string ShiftMainBtn = "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Main Button.prefab";
        const string ShiftPanelAlt = "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Panel Button Alternative.prefab";
        const string StationPanelPath = PrefabFolder + "/StationPanel.prefab";
        const string BastionHudPath = PrefabFolder + "/BastionHud.prefab";
        const string ArmoryWheelPath = PrefabFolder + "/ArmoryWheel.prefab";
        const string EndScreensPath = PrefabFolder + "/EndScreens.prefab";

        // 5% title-safe on 1920×1280 → 96×64.
        const float SafeX = 96f;
        const float SafeY = 64f;

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string ReportPath => Path.Combine(Root, "gkb_bastion_ui_report.json");

        // Reachable via GatekeeperBastion/Build UI Prefabs (UiPrefabBuilder) — no separate menu.
        public static void AssembleFromMenu() => AssembleAll("menu");

        /// <summary>Batchmode: -executeMethod GatekeeperBastion.EditorTools.BastionUiAssembler.AssembleFromBatch</summary>
        public static void AssembleFromBatch()
        {
            AssembleAll("batch");
            bool ok = File.Exists(ReportPath) && File.ReadAllText(ReportPath).Contains("\"status\":\"ok\"");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Alias for batch scripts that look for BuildFromBatch.</summary>
        public static void BuildFromBatch() => AssembleFromBatch();

        public static void AssembleAll(string trigger)
        {
            var built = new List<string>();
            var errors = new List<string>();

            try
            {
                EnsureFolder();
                built.Add(BuildBastionHud());
                built.Add(BuildEndScreens());
                built.Add(BuildArmoryWheel());
                TryHostWheelInStationPanel(built, errors);
            }
            catch (Exception e)
            {
                errors.Add(e.GetType().Name + ": " + e.Message);
            }

            string status = errors.Count == 0 ? "ok" : "error";
            string message = errors.Count == 0
                ? $"Assembled Bastion UI ({trigger}): {string.Join(", ", built)}"
                : string.Join("; ", errors);
            WriteReport(status, message, built, errors);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (errors.Count == 0) Debug.Log("[BastionUiAssembler] " + message);
            else Debug.LogWarning("[BastionUiAssembler] " + message);
        }

        static string BuildBastionHud()
        {
            var canvasSrc = LoadRequired(ShiftCanvas);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(canvasSrc);
            root.name = "BastionHud";
            ConfigureCanvas(root);

            var view = root.GetComponent<BastionHudView>() ?? root.AddComponent<BastionHudView>();
            var so = new SerializedObject(view);

            var safe = CreateRect(root.transform, "SafeArea");
            Stretch(safe, Vector2.zero, Vector2.one, new Vector2(SafeX, SafeY), new Vector2(-SafeX, -SafeY));
            so.FindProperty("_root").objectReferenceValue = root.GetComponent<RectTransform>();
            so.FindProperty("_safeArea").objectReferenceValue = safe;

            // Vignette
            var vig = CreatePanel(safe, "Vignette", new Color(0.4f, 0f, 0f, 0f));
            Stretch(vig, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            vig.gameObject.SetActive(false);
            var vigImg = vig.GetComponent<Image>();
            vigImg.raycastTarget = false;
            var down = CreateTmp(vig, "DownLabel", Theme.Title, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetCentered(down.rectTransform, 800f, 40f);
            so.FindProperty("_vignetteRoot").objectReferenceValue = vig.gameObject;
            so.FindProperty("_vignetteImage").objectReferenceValue = vigImg;
            so.FindProperty("_downLabel").objectReferenceValue = down;

            // Fight panel — restrained flat fill (criterion 29)
            var fight = CreatePanel(safe, "FightPanel", new Color(0f, 0f, 0f, 0.55f));
            fight.anchorMin = fight.anchorMax = new Vector2(0f, 1f);
            fight.pivot = new Vector2(0f, 1f);
            fight.anchoredPosition = new Vector2(0f, 0f);
            fight.sizeDelta = new Vector2(420f, 560f);
            AddLBracket(fight);
            var fightCg = fight.gameObject.AddComponent<CanvasGroup>();
            fight.gameObject.SetActive(false);
            so.FindProperty("_fightRoot").objectReferenceValue = fight.gameObject;
            so.FindProperty("_fightGroup").objectReferenceValue = fightCg;

            float y = -25f;
            const float left = 25f;
            const float width = 370f;
            var timeWave = AddFightLabel(fight, "TimeWave", left, ref y, width, Theme.Small);
            var siphon = AddFightLabel(fight, "Siphon", left, ref y, width, Theme.Small);
            var salvage = AddFightLabel(fight, "Salvage", left, ref y, width, Theme.Body);
            salvage.fontStyle = FontStyles.Bold;
            var salvagePunch = salvage.gameObject.AddComponent<ValueAnimator>();
            AssignAnimator(salvagePunch, salvage, null);

            var hatchLabel = AddFightLabel(fight, "HatchLabel", left, ref y, width, Theme.Small);
            var hatchBar = CreateBar(fight, "HatchBar", left, ref y, width, 18f);
            var playerLabel = AddFightLabel(fight, "PlayerLabel", left, ref y, width, Theme.Small);
            var playerBar = CreateBar(fight, "PlayerBar", left, ref y, width, 18f);

            var stamina = CreateBlock(fight, "Stamina", left, ref y, width, 50f, out var staminaCg,
                out var staminaLabel, out var staminaBar);
            var reload = CreateBlock(fight, "Reload", left, ref y, width, 50f, out var reloadCg,
                out var reloadLabel, out var reloadBar);
            var weapon = CreateBlock(fight, "Weapon", left, ref y, width, 140f, out var weaponCg,
                out var weaponText, out _);
            weaponText.textWrappingMode = TextWrappingModes.Normal;
            weaponText.alignment = TextAlignmentOptions.TopLeft;
            var ability = CreateBlock(fight, "Ability", left, ref y, width, 50f, out var abilityCg,
                out var abilityLabel, out var abilityBar);

            so.FindProperty("_timeWave").objectReferenceValue = timeWave;
            so.FindProperty("_siphonLine").objectReferenceValue = siphon;
            so.FindProperty("_salvageLine").objectReferenceValue = salvage;
            so.FindProperty("_salvagePunch").objectReferenceValue = salvagePunch;
            so.FindProperty("_hatchLabel").objectReferenceValue = hatchLabel;
            so.FindProperty("_hatchBar").objectReferenceValue = hatchBar;
            so.FindProperty("_playerLabel").objectReferenceValue = playerLabel;
            so.FindProperty("_playerBar").objectReferenceValue = playerBar;
            so.FindProperty("_staminaBlock").objectReferenceValue = stamina;
            so.FindProperty("_staminaGroup").objectReferenceValue = staminaCg;
            so.FindProperty("_staminaLabel").objectReferenceValue = staminaLabel;
            so.FindProperty("_staminaBar").objectReferenceValue = staminaBar;
            so.FindProperty("_reloadBlock").objectReferenceValue = reload;
            so.FindProperty("_reloadGroup").objectReferenceValue = reloadCg;
            so.FindProperty("_reloadLabel").objectReferenceValue = reloadLabel;
            so.FindProperty("_reloadBar").objectReferenceValue = reloadBar;
            so.FindProperty("_weaponStack").objectReferenceValue = weapon;
            so.FindProperty("_weaponGroup").objectReferenceValue = weaponCg;
            so.FindProperty("_weaponBlock").objectReferenceValue = weaponText;
            so.FindProperty("_abilityBlock").objectReferenceValue = ability;
            so.FindProperty("_abilityGroup").objectReferenceValue = abilityCg;
            so.FindProperty("_abilityLabel").objectReferenceValue = abilityLabel;
            so.FindProperty("_abilityBar").objectReferenceValue = abilityBar;

            // Crosshair
            var cross = CreateRect(safe, "Crosshair");
            SetCentered(cross, 80f, 40f);
            cross.gameObject.SetActive(false);
            var cCenter = CreateTmp(cross, "Center", Theme.Body, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            cCenter.text = "+";
            SetCentered(cCenter.rectTransform, 20f, 28f);
            var cLeft = CreateTmp(cross, "Left", Theme.Body, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            cLeft.text = "[";
            Place(cLeft.rectTransform, -28f, 0f, 24f, 28f);
            var cRight = CreateTmp(cross, "Right", Theme.Body, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            cRight.text = "]";
            Place(cRight.rectTransform, 28f, 0f, 24f, 28f);
            so.FindProperty("_crosshairRoot").objectReferenceValue = cross.gameObject;
            so.FindProperty("_crosshairCenter").objectReferenceValue = cCenter;
            so.FindProperty("_crosshairLeft").objectReferenceValue = cLeft;
            so.FindProperty("_crosshairRight").objectReferenceValue = cRight;

            // Prompt / Prep / Assault / Arriving
            var prompt = CreatePanel(safe, "Prompt", Color.clear);
            Stretch(prompt, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(432f, 80f), new Vector2(-12f, 110f));
            prompt.gameObject.SetActive(false);
            var promptTxt = CreateTmp(prompt, "Text", Theme.Body, Theme.Ink, FontStyles.Normal,
                TextAlignmentOptions.Center);
            Stretch(promptTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            so.FindProperty("_promptRoot").objectReferenceValue = prompt.gameObject;
            so.FindProperty("_promptText").objectReferenceValue = promptTxt;

            BuildStrip(safe, "PrepStrip", out var prepRoot, out var prepHead,
                out var p1, out var p2, out var p3, out var p4);
            so.FindProperty("_prepRoot").objectReferenceValue = prepRoot;
            so.FindProperty("_prepHeading").objectReferenceValue = prepHead;
            so.FindProperty("_prepLine1").objectReferenceValue = p1;
            so.FindProperty("_prepLine2").objectReferenceValue = p2;
            so.FindProperty("_prepLine3").objectReferenceValue = p3;
            so.FindProperty("_prepLine4").objectReferenceValue = p4;

            var assault = CreatePanel(safe, "AssaultStrip", new Color(0f, 0f, 0f, 0.55f));
            Stretch(assault, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(440f, 80f), new Vector2(-12f, 126f));
            assault.gameObject.SetActive(false);
            AddLBracket(assault);
            var assaultHead = CreateTmp(assault, "Heading", Theme.Head, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            PlaceTop(assaultHead.rectTransform, 0f, -8f, 30f);
            so.FindProperty("_assaultRoot").objectReferenceValue = assault.gameObject;
            so.FindProperty("_assaultHeading").objectReferenceValue = assaultHead;

            var alarm = CreatePanel(safe, "HatchAlarm", Color.clear);
            Stretch(alarm, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(440f, 170f), new Vector2(-12f, 200f));
            alarm.gameObject.SetActive(false);
            var alarmTxt = CreateTmp(alarm, "Text", Theme.Head, Theme.AlarmText, FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(alarmTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            so.FindProperty("_hatchAlarmRoot").objectReferenceValue = alarm.gameObject;
            so.FindProperty("_hatchAlarmText").objectReferenceValue = alarmTxt;

            var shortcutHost = CreateRect(safe, "ShortcutPanelHost");
            shortcutHost.anchorMin = shortcutHost.anchorMax = new Vector2(0f, 0f);
            shortcutHost.pivot = new Vector2(0f, 0f);
            shortcutHost.anchoredPosition = new Vector2(0f, 0f);
            shortcutHost.sizeDelta = new Vector2(BastionHudView.ShortcutBandWidth, 280f);
            var shortcuts = shortcutHost.gameObject.AddComponent<ShortcutPanelView>();
            WireMinimalShortcut(shortcuts, shortcutHost);
            so.FindProperty("_shortcutRoot").objectReferenceValue = shortcutHost.gameObject;
            so.FindProperty("_shortcuts").objectReferenceValue = shortcuts;

            var arriving = CreatePanel(safe, "Arriving", Color.clear);
            Stretch(arriving, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, -20f), new Vector2(0f, 20f));
            arriving.gameObject.SetActive(false);
            var arrTxt = CreateTmp(arriving, "Text", Theme.Display, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(arrTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            so.FindProperty("_arrivingRoot").objectReferenceValue = arriving.gameObject;
            so.FindProperty("_arrivingText").objectReferenceValue = arrTxt;

            // Nested end screen (also saved standalone)
            var end = BuildEndScreenHierarchy(safe);
            so.FindProperty("_endScreen").objectReferenceValue = end;

            // Emergency
            var emerg = CreatePanel(safe, "EmergencyExit", new Color(0f, 0f, 0f, 0.7f));
            SetCentered(emerg, 580f, 160f);
            emerg.gameObject.SetActive(false);
            AddLBracket(emerg);
            var emergTxt = CreateTmp(emerg, "Text", Theme.Head, Theme.Ink, FontStyles.Normal,
                TextAlignmentOptions.Center);
            Stretch(emergTxt.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f),
                new Vector2(25f, 0f), new Vector2(-25f, -10f));
            var emergBtnGo = InstantiateButton(ShiftMainBtn, emerg, "ReturnButton", "RETURN TO SHIP");
            var emergBtnRt = emergBtnGo.GetComponent<RectTransform>();
            emergBtnRt.anchorMin = new Vector2(0.2f, 0f);
            emergBtnRt.anchorMax = new Vector2(0.8f, 0f);
            emergBtnRt.pivot = new Vector2(0.5f, 0f);
            emergBtnRt.anchoredPosition = new Vector2(0f, 25f);
            emergBtnRt.sizeDelta = new Vector2(0f, 50f);
            so.FindProperty("_emergencyRoot").objectReferenceValue = emerg.gameObject;
            so.FindProperty("_emergencyText").objectReferenceValue = emergTxt;
            so.FindProperty("_emergencyButton").objectReferenceValue = emergBtnGo.GetComponent<Button>();

            so.ApplyModifiedPropertiesWithoutUndo();
            return SaveVariant(root, BastionHudPath);
        }

        static string BuildEndScreens()
        {
            var canvasSrc = LoadRequired(ShiftCanvas);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(canvasSrc);
            root.name = "EndScreens";
            ConfigureCanvas(root);
            BuildEndScreenHierarchy(root.transform);
            return SaveVariant(root, EndScreensPath);
        }

        static EndScreenView BuildEndScreenHierarchy(Transform parent)
        {
            var root = CreatePanel(parent, "EndScreen", new Color(0f, 0f, 0f, 0.45f));
            Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);
            var scrim = root.GetComponent<Image>();
            scrim.raycastTarget = true;
            var scrimCg = root.gameObject.AddComponent<CanvasGroup>();

            var panel = CreatePanel(root, "Panel", new Color(0.08f, 0.08f, 0.09f, 0.92f));
            SetCentered(panel, 700f, 620f);
            AddLBracket(panel);
            TryAddGradient(panel.GetComponent<Image>());

            var view = root.gameObject.AddComponent<EndScreenView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_scrim").objectReferenceValue = scrimCg;
            so.FindProperty("_panel").objectReferenceValue = panel.gameObject;

            var verdict = CreateTmp(panel, "Verdict", Theme.Display, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            PlaceTop(verdict.rectTransform, 25f, -35f, 50f);
            var headline = CreateTmp(panel, "Headline", Theme.Title, Theme.Warn, FontStyles.Bold,
                TextAlignmentOptions.Center);
            PlaceTop(headline.rectTransform, 25f, -95f, 40f);

            var statsHost = CreateRect(panel, "Stats");
            Stretch(statsHost, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(50f, 180f), new Vector2(-50f, -150f));
            var stagger = statsHost.gameObject.AddComponent<StaggerEntrance>();
            var statRows = new GameObject[4];
            var statLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var row = CreateRect(statsHost, "Stat" + i);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -i * 35f);
                row.sizeDelta = new Vector2(0f, 30f);
                var cg = row.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
                var label = CreateTmp(row, "Label", Theme.Body, Theme.InkDim, FontStyles.Normal,
                    TextAlignmentOptions.Center);
                Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                statRows[i] = row.gameObject;
                statLabels[i] = label;
            }

            var salvageCaption = CreateTmp(panel, "SalvageCaption", Theme.Body, Theme.InkDim,
                FontStyles.Normal, TextAlignmentOptions.Center);
            PlaceTop(salvageCaption.rectTransform, 25f, -360f, 30f);
            var salvageGo = CreateRect(panel, "SalvageCount");
            PlaceTop(salvageGo, 25f, -395f, 45f);
            var salvageTmp = CreateTmp(salvageGo, "Value", Theme.Display, Theme.Accent, FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(salvageTmp.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var salvageAnim = salvageGo.gameObject.AddComponent<ValueAnimator>();
            AssignAnimator(salvageAnim, salvageTmp, null);

            var actions = CreateRect(panel, "Actions");
            Stretch(actions, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f),
                new Vector2(0f, 25f), new Vector2(0f, 120f));
            var returnBtn = InstantiateButton(ShiftMainBtn, actions, "Return", "RETURN TO SHIP");
            var returnRt = returnBtn.GetComponent<RectTransform>();
            Stretch(returnRt, new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var detailsBtn = InstantiateButton(ShiftPanelAlt, actions, "Details", "DETAILS");
            var detailsRt = detailsBtn.GetComponent<RectTransform>();
            Stretch(detailsRt, new Vector2(0.25f, 0f), new Vector2(0.75f, 0.4f), Vector2.zero, Vector2.zero);
            detailsBtn.SetActive(false);

            var detailsRoot = CreateRect(panel, "Details");
            Stretch(detailsRoot, new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.35f),
                Vector2.zero, Vector2.zero);
            detailsRoot.gameObject.SetActive(false);

            so.FindProperty("_verdict").objectReferenceValue = verdict;
            so.FindProperty("_headline").objectReferenceValue = headline;
            var rowsProp = so.FindProperty("_statRows");
            rowsProp.arraySize = statRows.Length;
            for (int i = 0; i < statRows.Length; i++)
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = statRows[i];
            var labelsProp = so.FindProperty("_statLabels");
            labelsProp.arraySize = statLabels.Length;
            for (int i = 0; i < statLabels.Length; i++)
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = statLabels[i];
            so.FindProperty("_salvageCount").objectReferenceValue = salvageAnim;
            so.FindProperty("_salvageCaption").objectReferenceValue = salvageCaption;
            so.FindProperty("_actionsRoot").objectReferenceValue = actions.gameObject;
            so.FindProperty("_returnButton").objectReferenceValue = returnBtn.GetComponent<Button>();
            so.FindProperty("_detailsButton").objectReferenceValue = detailsBtn.GetComponent<Button>();
            so.FindProperty("_detailsRoot").objectReferenceValue = detailsRoot.gameObject;
            so.FindProperty("_statsStagger").objectReferenceValue = stagger;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static string BuildArmoryWheel()
        {
            var canvasSrc = LoadRequired(ShiftCanvas);
            // Wheel is a nested piece — build under a plain RectTransform rooted as Shift Canvas variant
            // so m_SourcePrefab points at Shift, then strip canvas for embedding.
            var root = (GameObject)PrefabUtility.InstantiatePrefab(canvasSrc);
            root.name = "ArmoryWheel";
            ConfigureCanvas(root);

            var host = CreateRect(root.transform, "WheelHost");
            Stretch(host, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var le = host.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 370f;
            le.preferredHeight = 370f;

            var wheelView = host.gameObject.AddComponent<ArmoryWheelView>();
            var so = new SerializedObject(wheelView);

            var wheel = CreateRect(host, "Wheel");
            wheel.sizeDelta = new Vector2(248f, 248f);
            wheel.anchorMin = wheel.anchorMax = new Vector2(0.5f, 1f);
            wheel.pivot = new Vector2(0.5f, 1f);
            wheel.anchoredPosition = new Vector2(0f, -10f);
            so.FindProperty("_wheelRoot").objectReferenceValue = wheel;

            var hub = CreatePanel(wheel, "Hub", Theme.WheelHub);
            hub.anchorMin = hub.anchorMax = new Vector2(0.5f, 0.5f);
            hub.sizeDelta = new Vector2(78f, 78f);
            hub.anchoredPosition = Vector2.zero;
            var hubTitle = CreateTmp(hub, "HubTitle", Theme.Body, Theme.WheelCentre, FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(hubTitle.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var hubSub = CreateTmp(hub, "HubSub", Theme.Small, Theme.InkFaint, FontStyles.Normal,
                TextAlignmentOptions.Center);
            hubSub.text = "FOCUS";
            Stretch(hubSub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f), Vector2.zero, Vector2.zero);
            so.FindProperty("_hubTitle").objectReferenceValue = hubTitle;
            so.FindProperty("_hubSub").objectReferenceValue = hubSub;

            var spokeRoots = so.FindProperty("_spokeRoots");
            var spokes = so.FindProperty("_spokes");
            var fills = so.FindProperty("_spokeFills");
            var labels = so.FindProperty("_spokeLabels");
            var rays = so.FindProperty("_spokeRays");
            spokeRoots.arraySize = ArmoryWheelView.SpokeCount;
            spokes.arraySize = ArmoryWheelView.SpokeCount;
            fills.arraySize = ArmoryWheelView.SpokeCount;
            labels.arraySize = ArmoryWheelView.SpokeCount;
            rays.arraySize = ArmoryWheelView.SpokeCount;

            for (int i = 0; i < ArmoryWheelView.SpokeCount; i++)
            {
                var rayGo = new GameObject("Ray" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rayGo.transform.SetParent(wheel, false);
                var rayImg = rayGo.GetComponent<Image>();
                rayImg.sprite = UiSprite();
                rayImg.color = Theme.WheelSpokeLit;
                rayImg.raycastTarget = false;
                rays.GetArrayElementAtIndex(i).objectReferenceValue = rayImg;

                var spokeGo = InstantiateButton(ShiftPanelBtn, wheel, "Spoke" + i, "SPOKE");
                var spokeRt = spokeGo.GetComponent<RectTransform>();
                spokeRt.sizeDelta = new Vector2(90f, 70f);
                var btn = spokeGo.GetComponent<Button>();
                var fill = spokeGo.GetComponent<Image>() ?? spokeGo.GetComponentInChildren<Image>();
                var label = spokeGo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.fontSize = Theme.Small;
                    label.alignment = TextAlignmentOptions.Center;
                    label.textWrappingMode = TextWrappingModes.Normal;
                }

                spokeRoots.GetArrayElementAtIndex(i).objectReferenceValue = spokeRt;
                spokes.GetArrayElementAtIndex(i).objectReferenceValue = btn;
                fills.GetArrayElementAtIndex(i).objectReferenceValue = fill;
                labels.GetArrayElementAtIndex(i).objectReferenceValue = label;
            }

            var projFire = CreateTmp(host, "ProjFire", Theme.Body, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            PlaceBelowWheel(projFire.rectTransform, -270f);
            var projReload = CreateTmp(host, "ProjReload", Theme.Body, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            PlaceBelowWheel(projReload.rectTransform, -300f);
            so.FindProperty("_projFire").objectReferenceValue = projFire;
            so.FindProperty("_projReload").objectReferenceValue = projReload;

            var buy = InstantiateButton(ShiftMainBtn, host, "Buy", "BUY");
            var buyRt = buy.GetComponent<RectTransform>();
            buyRt.anchorMin = buyRt.anchorMax = new Vector2(0.5f, 1f);
            buyRt.pivot = new Vector2(0.5f, 1f);
            buyRt.anchoredPosition = new Vector2(0f, -330f);
            buyRt.sizeDelta = new Vector2(360f, 50f);
            so.FindProperty("_buyButton").objectReferenceValue = buy.GetComponent<Button>();
            so.FindProperty("_buyLabel").objectReferenceValue = buy.GetComponentInChildren<TextMeshProUGUI>(true);

            var reason = CreateTmp(host, "Reason", Theme.Body, Theme.InkFaint, FontStyles.Italic,
                TextAlignmentOptions.Center);
            PlaceBelowWheel(reason.rectTransform, -330f);
            reason.gameObject.SetActive(false);
            so.FindProperty("_reasonLabel").objectReferenceValue = reason;

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save nested host as the wheel prefab content with Shift canvas ancestry on root.
            return SaveVariant(root, ArmoryWheelPath);
        }

        static void TryHostWheelInStationPanel(List<string> built, List<string> errors)
        {
            var station = AssetDatabase.LoadAssetAtPath<GameObject>(StationPanelPath);
            if (station == null)
            {
                errors.Add("StationPanel.prefab not found — wheel prefab built; host after Ship UI assemble");
                return;
            }

            var wheelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoryWheelPath);
            if (wheelPrefab == null)
            {
                errors.Add("ArmoryWheel.prefab missing after build");
                return;
            }

            string path = StationPanelPath;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var panelView = root.GetComponentInChildren<StationPanelView>(true);
                if (panelView == null)
                {
                    errors.Add("StationPanel has no StationPanelView");
                    return;
                }

                var so = new SerializedObject(panelView);
                var slotProp = so.FindProperty("_wheelSlot");
                RectTransform slot = slotProp.objectReferenceValue as RectTransform;
                if (slot == null)
                {
                    // Create a slot under the panel root if Ship agent left it empty.
                    slot = CreateRect(panelView.transform, "WheelSlot");
                    slot.sizeDelta = new Vector2(400f, 380f);
                    var le = slot.gameObject.AddComponent<LayoutElement>();
                    le.minHeight = 380f;
                    le.preferredHeight = 380f;
                    slotProp.objectReferenceValue = slot;
                }

                // Clear previous hosted wheel instances (keep slot).
                for (int i = slot.childCount - 1; i >= 0; i--)
                {
                    var child = slot.GetChild(i);
                    if (child.GetComponent<ArmoryWheelView>() != null
                        || child.name.Contains("ArmoryWheel")
                        || child.name.Contains("WheelHost"))
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(wheelPrefab, slot);
                instance.name = "ArmoryWheel";
                var wheelView = instance.GetComponentInChildren<ArmoryWheelView>(true);
                so.FindProperty("_wheel").objectReferenceValue = wheelView;
                so.ApplyModifiedPropertiesWithoutUndo();
                instance.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                built.Add(path + " (wheel hosted)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ----- helpers -----

        static void ConfigureCanvas(GameObject root)
        {
            // Shift Canvas nests an Event System — scene/runtime Ensure owns the singleton.
            var es = root.transform.Find("Event System");
            if (es != null) UnityEngine.Object.DestroyImmediate(es.gameObject);

            UiPrefabVariant.EnsureCanvasScaler(root);
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                // Shift Canvas boots with zero scale; overlay needs 1.
                root.transform.localScale = Vector3.one;
            }
        }

        static string SaveVariant(GameObject root, string path)
            => UiPrefabVariant.Save(root, path);

        static GameObject LoadRequired(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) throw new InvalidOperationException("Missing Shift prefab: " + path);
            return go;
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Ui");
        }

        static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiSprite();
            img.color = color;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        static TMP_Text CreateTmp(Transform parent, string name, float size, Color color,
            FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.characterSpacing = 0f;
            return tmp;
        }

        static TMP_Text AddFightLabel(Transform parent, string name, float left, ref float y,
            float width, float size)
        {
            var label = CreateTmp(parent, name, size, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(left, y);
            rt.sizeDelta = new Vector2(width, size + 4f);
            y -= size + 6f;
            return label;
        }

        static ValueAnimator CreateBar(Transform parent, string name, float left, ref float y,
            float width, float height)
        {
            var root = CreateRect(parent, name);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(left, y);
            root.sizeDelta = new Vector2(width, height);
            y -= height + 8f;

            var track = CreatePanel(root, "Track", Theme.BarTrack);
            Stretch(track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var fillRt = CreatePanel(root, "Fill", Theme.Good);
            var fill = fillRt.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            Stretch(fillRt, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));

            var text = CreateTmp(root, "Text", 12f, Theme.Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var anim = root.gameObject.AddComponent<ValueAnimator>();
            AssignAnimator(anim, text, fill);
            return anim;
        }

        static GameObject CreateBlock(Transform parent, string name, float left, ref float y,
            float width, float height, out CanvasGroup cg, out TMP_Text label, out ValueAnimator bar)
        {
            var root = CreateRect(parent, name);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(left, y);
            root.sizeDelta = new Vector2(width, height);
            y -= height + 4f;
            cg = root.gameObject.AddComponent<CanvasGroup>();
            label = CreateTmp(root, "Label", Theme.Small, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            Stretch(label.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            if (height > 60f)
            {
                bar = null;
                Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                var barRoot = CreateRect(root, "Bar");
                barRoot.anchorMin = new Vector2(0f, 0f);
                barRoot.anchorMax = new Vector2(1f, 0.5f);
                barRoot.offsetMin = Vector2.zero;
                barRoot.offsetMax = Vector2.zero;
                var track = CreatePanel(barRoot, "Track", Theme.BarTrack);
                Stretch(track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var fillRt = CreatePanel(barRoot, "Fill", Theme.Good);
                var fill = fillRt.GetComponent<Image>();
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                Stretch(fillRt, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));
                bar = barRoot.gameObject.AddComponent<ValueAnimator>();
                AssignAnimator(bar, null, fill);
            }
            return root.gameObject;
        }

        static void BuildStrip(Transform parent, string name, out GameObject root, out TMP_Text head,
            out TMP_Text l1, out TMP_Text l2, out TMP_Text l3, out TMP_Text l4)
        {
            var panel = CreatePanel(parent, name, new Color(0f, 0f, 0f, 0.55f));
            Stretch(panel, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(440f, 80f), new Vector2(-12f, 250f));
            panel.gameObject.SetActive(false);
            AddLBracket(panel);
            root = panel.gameObject;
            head = CreateTmp(panel, "Heading", Theme.Head, Theme.Ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            PlaceTop(head.rectTransform, 0f, -8f, 30f);
            l1 = CreateTmp(panel, "Line1", Theme.Body, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.Center);
            PlaceTop(l1.rectTransform, 0f, -42f, 28f);
            l2 = CreateTmp(panel, "Line2", Theme.Body, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.Center);
            PlaceTop(l2.rectTransform, 0f, -74f, 24f);
            l3 = CreateTmp(panel, "Line3", Theme.Body, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.Center);
            PlaceTop(l3.rectTransform, 0f, -102f, 24f);
            l4 = CreateTmp(panel, "Line4", Theme.Body, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.Center);
            PlaceTop(l4.rectTransform, 0f, -130f, 24f);
        }

        static void WireMinimalShortcut(ShortcutPanelView view, RectTransform host)
        {
            var panel = CreatePanel(host, "Panel", new Color(0f, 0f, 0f, 0.55f));
            Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddLBracket(panel);
            var tip = CreateTmp(panel, "CollapsedTip", Theme.Body, Theme.InkDim, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            Stretch(tip.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f));
            tip.text = "controls";
            var lines = CreateRect(panel, "Lines");
            Stretch(lines, Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-10f, -8f));
            var pool = new TMP_Text[12];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = CreateTmp(lines, "Line" + i, Theme.Small, Theme.InkDim, FontStyles.Normal,
                    TextAlignmentOptions.MidlineLeft);
                var rt = pool[i].rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * 20f);
                rt.sizeDelta = new Vector2(0f, 20f);
                pool[i].gameObject.SetActive(false);
            }

            var so = new SerializedObject(view);
            so.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            so.FindProperty("_collapsedTip").objectReferenceValue = tip;
            so.FindProperty("_linesRoot").objectReferenceValue = lines;
            var poolProp = so.FindProperty("_linePool");
            poolProp.arraySize = pool.Length;
            for (int i = 0; i < pool.Length; i++)
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignAnimator(ValueAnimator anim, TMP_Text label, Image fill)
        {
            var so = new SerializedObject(anim);
            if (label != null) so.FindProperty("_label").objectReferenceValue = label;
            if (fill != null) so.FindProperty("_fill").objectReferenceValue = fill;
            so.FindProperty("_punchTarget").objectReferenceValue = anim.transform as RectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject InstantiateButton(string prefabPath, Transform parent, string name, string label)
        {
            var src = LoadRequired(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            go.name = name;
            var main = go.GetComponent<Michsky.UI.Shift.MainButton>();
            if (main != null)
            {
                main.buttonText = label;
                main.useCustomText = false;
            }
            var panel = go.GetComponent<Michsky.UI.Shift.MainPanelButton>();
            if (panel != null)
            {
                panel.buttonText = label;
                panel.useCustomText = false;
            }
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = label;
            return go;
        }

        static void AddLBracket(Transform panel)
        {
            // Motif: single L-bracket corner (top-left), Hatch amber, restrained.
            var bracket = CreatePanel(panel, "LBracket", Theme.Accent);
            bracket.anchorMin = bracket.anchorMax = new Vector2(0f, 1f);
            bracket.pivot = new Vector2(0f, 1f);
            bracket.anchoredPosition = new Vector2(0f, 0f);
            bracket.sizeDelta = new Vector2(25f, 3f);
            var vert = CreatePanel(panel, "LBracketVert", Theme.Accent);
            vert.anchorMin = vert.anchorMax = new Vector2(0f, 1f);
            vert.pivot = new Vector2(0f, 1f);
            vert.anchoredPosition = new Vector2(0f, 0f);
            vert.sizeDelta = new Vector2(3f, 25f);
        }

        static void TryAddGradient(Image panel)
        {
            var type = Type.GetType("Michsky.UI.Shift.GradientEffect, Michsky.UI.Shift")
                       ?? Type.GetType("Michsky.UI.Shift.GradientEffect, Assembly-CSharp");
            if (type == null) return;
            if (panel.GetComponent(type) != null) return;
            panel.gameObject.AddComponent(type);
        }

        static Sprite UiSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
                   ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        static void SetCentered(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void PlaceTop(RectTransform rt, float padX, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(padX, top - height);
            rt.offsetMax = new Vector2(-padX, top);
        }

        static void PlaceBelowWheel(RectTransform rt, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(400f, 28f);
        }

        static void WriteReport(string status, string message, List<string> prefabs, List<string> errors)
        {
            var sb = new StringBuilder();
            sb.Append("{\"status\":\"").Append(status)
              .Append("\",\"message\":").Append(JsonString(message))
              .Append(",\"prefabs\":[");
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonString(prefabs[i]));
            }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonString(errors[i]));
            }
            sb.Append("]}");
            File.WriteAllText(ReportPath, sb.ToString());
        }

        static string JsonString(string s)
        {
            if (s == null) return "\"\"";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }
    }
}
