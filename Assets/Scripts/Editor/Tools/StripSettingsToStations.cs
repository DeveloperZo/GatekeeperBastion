using System.IO;
using System.Text;
using GatekeeperBastion.Game;
using Michsky.UI.Shift;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Rebuilds Ship station panels from <b>Main Menu (Desktop)</b> Settings — the
    /// SHIFT UI Settings screenshot — plus Background / Top / Bottom chrome. Strips tabs,
    /// preview, and top Button List; keeps Gameplay list shell + Hotkeys/Close.
    /// Menu: <c>Gatekeeper/HUD/Strip Settings Shell Onto Stations</c>.
    /// File bridge: <c>gkb_station_settings_request.txt</c>.</summary>
    [InitializeOnLoad]
    public static class StripSettingsToStations
    {
        const string ShipPath = "Assets/Scenes/Ship.unity";
        const string MainMenuPath =
            "Assets/Shift - Complete Sci-Fi UI/Scenes/Main Menu/Main Menu (Desktop).unity";
        const string MainButtonPath =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Main Button.prefab";

        static readonly (string Panel, string ConsoleType)[] Stations =
        {
            ("ARMORY", nameof(ArmoryStation)),
            ("FABRICATION", nameof(EngineeringStation)),
            ("CREW QUARTERS", nameof(CrewQuartersStation)),
            ("NAV MAP", nameof(MapStation)),
        };

        static double _nextPoll;

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_station_settings_request.txt");
        static string LogPath => Path.Combine(Root, "gkb_station_settings_log.txt");

        static StripSettingsToStations()
        {
            EditorApplication.update += PollSentinel;
        }

        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling) return;
            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch { return; }
            EditorApplication.delayCall += RunFromMenu;
        }

        [MenuItem("Gatekeeper/HUD/Strip Settings Shell Onto Stations")]
        public static void RunFromMenu() => Run();

        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("[StripSettingsToStations]");
            log.AppendLine("source: Main Menu (Desktop) Settings");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish(log, "REFUSED: Play Mode is active");
                return;
            }
            if (EditorApplication.isCompiling)
            {
                Finish(log, "REFUSED: scripts are compiling");
                return;
            }

            Scene mainMenu = default;
            try
            {
                Scene ship = default;
                bool shipWasOpen = false;
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    Scene s = EditorSceneManager.GetSceneAt(i);
                    if (s.path != ShipPath) continue;
                    ship = s;
                    shipWasOpen = true;
                    break;
                }
                // Always reload Ship from disk so a prior failed strip cannot leave empty chrome.
                ship = EditorSceneManager.OpenScene(ShipPath, OpenSceneMode.Single);
                if (!ship.IsValid() || !ship.isLoaded)
                {
                    Finish(log, "FAIL: could not load Ship.unity");
                    return;
                }
                log.AppendLine(shipWasOpen ? "Reloaded Ship.unity from disk" : "Opened Ship.unity");

                mainMenu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Additive);
                if (!mainMenu.IsValid() || !mainMenu.isLoaded)
                {
                    Finish(log, "FAIL: could not open Main Menu (Desktop).unity");
                    return;
                }
                log.AppendLine("Opened Main Menu (Desktop) additive");

                var shipCanvas = FindNamed(ship, "Canvas");
                var mainPanels = FindNamed(ship, "Main Panels");
                var mmSettings = FindSettingsPanel(mainMenu);
                if (shipCanvas == null || mainPanels == null || mmSettings == null)
                {
                    Finish(log, "FAIL: Ship Canvas / Main Panels or Main Menu Settings missing");
                    return;
                }

                if (!EnsureMainMenuChrome(ship, mainMenu, shipCanvas, mainPanels, log))
                {
                    Finish(log, StationSceneGate.StatusLine(log, false, ""));
                    return;
                }

                // Esc Settings panel from Main Menu. Station shells clone from the live MM
                // Settings (not the Ship copy) so nested Content cannot be dropped twice.
                var settings = ReplaceSettingsFromMainMenu(ship, mainPanels, mmSettings, log);
                if (settings == null)
                {
                    Finish(log, StationSceneGate.StatusLine(log, false, ""));
                    return;
                }

                var shipController = Object.FindFirstObjectByType<ShipController>(FindObjectsInactive.Include);
                MainPanelManager mpm = null;
                if (shipController != null)
                {
                    var soShip = new SerializedObject(shipController);
                    mpm = soShip.FindProperty("_panels")?.objectReferenceValue as MainPanelManager;
                }
                if (mpm == null)
                    mpm = FindMenuManagerPanels(ship);
                if (mpm == null)
                {
                    Finish(log, "FAIL: Menu Manager MainPanelManager missing");
                    return;
                }
                log.AppendLine("Using MPM on " + mpm.gameObject.name);

                var mainButton = AssetDatabase.LoadAssetAtPath<GameObject>(MainButtonPath);
                if (mainButton == null)
                {
                    Finish(log, "FAIL: Main Button prefab missing at " + MainButtonPath);
                    return;
                }

                // Canvas must paint.
                shipCanvas.localScale = Vector3.one;
                if (shipCanvas.gameObject.activeSelf)
                    shipCanvas.gameObject.SetActive(false);

                foreach (var (panelName, _) in Stations)
                {
                    if (!RebuildStation(ship, mmSettings, mainPanels, mpm, mainButton, panelName, log))
                    {
                        Finish(log, StationSceneGate.StatusLine(log, false, ""));
                        return;
                    }
                }

                bool gate = StationSceneGate.AssertOpenPath(ship, log);
                if (!gate)
                {
                    Finish(log, StationSceneGate.StatusLine(log, false, ""));
                    return;
                }

                EditorSceneManager.MarkSceneDirty(ship);
                EditorSceneManager.SaveScene(ship);
                Finish(log, StationSceneGate.StatusLine(log, true,
                    "OK — stations from Main Menu (Desktop) Settings; Background/Top/Bottom imported"));
            }
            catch (System.Exception e)
            {
                // An uncaught throw here previously died silently: sentinel consumed, no log.
                Finish(log, "FAIL: exception — " + e.GetType().Name + ": " + e.Message +
                    "\n" + e.StackTrace);
            }
            finally
            {
                if (mainMenu.IsValid() && mainMenu.isLoaded)
                    EditorSceneManager.CloseScene(mainMenu, true);
            }
        }

        static bool EnsureMainMenuChrome(Scene ship, Scene mainMenu, RectTransform shipCanvas,
            RectTransform mainPanels, StringBuilder log)
        {
            var mmCanvas = FindNamed(mainMenu, "Canvas");
            var mmMainPanels = FindNamed(mainMenu, "Main Panels");
            if (mmCanvas == null || mmMainPanels == null)
            {
                log.AppendLine("FAIL: Main Menu Canvas / Main Panels missing");
                return false;
            }

            // Background (blue haze / Glow Motes) — sibling of Main Panels.
            var existingBg = FindChild(shipCanvas, "Background");
            if (existingBg != null && existingBg.childCount == 0)
            {
                Object.DestroyImmediate(existingBg.gameObject);
                log.AppendLine("removed empty Canvas/Background");
                existingBg = null;
            }
            if (existingBg == null)
            {
                var mmBg = FindChild(mmCanvas, "Background");
                if (mmBg == null || mmBg.childCount == 0)
                {
                    log.AppendLine("FAIL: Main Menu Background missing or empty");
                    return false;
                }
                var bg = CloneHierarchy(mmBg.gameObject, shipCanvas, "Background", log);
                bg.transform.SetAsFirstSibling();
                bg.SetActive(true);
                if (bg.transform.childCount == 0)
                {
                    log.AppendLine("FAIL: Background clone has no children");
                    return false;
                }
                log.AppendLine("imported Canvas/Background from Main Menu (Desktop) (" +
                               bg.transform.childCount + " children)");
            }

            // Top Panel — Blur/Shadow/Logo; strip HOME/CAMPAIGN/… Button List.
            var mmTop = FindChild(mmMainPanels, "Top Panel");
            if (mmTop == null)
            {
                log.AppendLine("FAIL: Main Menu Top Panel missing");
                return false;
            }
            var oldTop = FindChild(mainPanels, "Top Panel");
            int topIdx = oldTop != null ? oldTop.GetSiblingIndex() : 0;
            if (oldTop != null)
                Object.DestroyImmediate(oldTop.gameObject);
            var top = CloneHierarchy(mmTop.gameObject, mainPanels, "Top Panel", log);
            top.transform.SetSiblingIndex(topIdx);
            top.SetActive(true);
            DestroyChildNamed(top.transform, "Button List", log);
            log.AppendLine("imported Top Panel from Main Menu (Desktop); Button List stripped");

            // Bottom Panel — profile strip + [ESC] EXIT.
            if (FindChild(mainPanels, "Bottom Panel") == null)
            {
                var mmBottom = FindChild(mmMainPanels, "Bottom Panel");
                if (mmBottom == null)
                {
                    log.AppendLine("FAIL: Main Menu Bottom Panel missing");
                    return false;
                }
                var bottom = CloneHierarchy(mmBottom.gameObject, mainPanels, "Bottom Panel", log);
                bottom.SetActive(true);
                log.AppendLine("imported Bottom Panel from Main Menu (Desktop)");
            }

            return true;
        }

        static RectTransform ReplaceSettingsFromMainMenu(Scene ship, RectTransform mainPanels,
            RectTransform mmSettings, StringBuilder log)
        {
            var old = FindNamed(ship, "Settings");
            int sibling = old != null ? old.GetSiblingIndex() : mainPanels.childCount;
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var clone = CloneHierarchy(mmSettings.gameObject, mainPanels, "Settings", log);
            clone.SetActive(false);
            var rt = clone.GetComponent<RectTransform>();
            CopyRect(mmSettings, rt);
            rt.SetSiblingIndex(sibling);
            if (FindChild(rt, "Content") == null)
            {
                log.AppendLine("FAIL: replaced Settings has no Content child; children=" +
                               ChildNames(rt));
                return null;
            }
            log.AppendLine("replaced Ship Settings with Main Menu (Desktop) Settings");
            return rt;
        }

        const string TempPrefabPath = "Assets/__StripSettingsTemp.prefab";

        /// <summary>Clone via a temp prefab asset. The asset round-trip guarantees children
        /// survive (cross-scene Instantiate dropped Content), and unpacking leaves plain
        /// objects — a straight Instantiate keeps the source's prefab links, and
        /// DestroyImmediate on a child inside a prefab instance throws.</summary>
        static GameObject CloneHierarchy(GameObject source, Transform parent, string name,
            StringBuilder log)
        {
            // Same-scene staging copy, unpacked, so SaveAsPrefabAsset is legal whatever
            // prefab linkage the source carries. Throws here reach Run's catch and get logged.
            var staging = Object.Instantiate(source);
            GameObject clone = null;
            try
            {
                staging.name = name;
                UnpackInstancesUnder(staging);
                var asset = PrefabUtility.SaveAsPrefabAsset(staging, TempPrefabPath);
                if (asset != null)
                    clone = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
                if (clone != null && PrefabUtility.IsAnyPrefabInstanceRoot(clone))
                    PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
            }
            finally
            {
                Object.DestroyImmediate(staging);
                AssetDatabase.DeleteAsset(TempPrefabPath);
            }

            if (clone == null)
            {
                log.AppendLine("note: prefab round-trip returned null for " + name +
                               " — direct clone + unpack");
                clone = Object.Instantiate(source, parent, false);
                UnpackInstancesUnder(clone);
            }
            clone.name = name;
            if (clone.transform.childCount == 0 && source.transform.childCount > 0)
            {
                log.AppendLine("WARN: clone dropped children for " + name +
                               " — deep-copying " + source.transform.childCount);
                for (int i = 0; i < source.transform.childCount; i++)
                {
                    var srcChild = source.transform.GetChild(i);
                    var childClone = Object.Instantiate(srcChild.gameObject, clone.transform, false);
                    UnpackInstancesUnder(childClone);
                    childClone.name = srcChild.name;
                }
            }
            return clone;
        }

        /// <summary>Break every prefab link under root so DestroyImmediate on any child is
        /// legal. Guard bound only stops a pathological loop; one pass per instance root.</summary>
        static void UnpackInstancesUnder(GameObject root)
        {
            for (int guard = 0; guard < 256; guard++)
            {
                GameObject target = null;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                    var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    target = outer != null ? outer : go;
                    break;
                }
                if (target == null) return;
                PrefabUtility.UnpackPrefabInstance(target, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
        }

        static string ChildNames(Transform t)
        {
            if (t == null) return "(null)";
            var sb = new StringBuilder();
            for (int i = 0; i < t.childCount; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(t.GetChild(i).name);
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        static bool RebuildStation(Scene ship, RectTransform settings, RectTransform mainPanels,
            MainPanelManager mpm, GameObject mainButton, string panelName, StringBuilder log)
        {
            var oldRt = FindNamed(ship, panelName);
            if (oldRt == null)
            {
                log.AppendLine("FAIL: missing old panel " + panelName);
                return false;
            }

            var oldView = oldRt.GetComponent<StationPanelView>();
            if (oldView == null)
            {
                log.AppendLine("FAIL: " + panelName + " has no StationPanelView");
                return false;
            }

            var soOld = new SerializedObject(oldView);
            var oldTemplates = FindDeep(oldRt, "Templates");
            var oldWheel = FindDeep(oldRt, "Wheel Slot");
            var oldTitle = soOld.FindProperty("_title").objectReferenceValue as TMP_Text;
            var oldBanked = soOld.FindProperty("_banked").objectReferenceValue as TMP_Text;
            var oldLifetime = soOld.FindProperty("_lifetime").objectReferenceValue as TMP_Text;
            var oldRow = soOld.FindProperty("_rowPrefab").objectReferenceValue as Component;
            var oldLabel = soOld.FindProperty("_labelPrefab").objectReferenceValue as Component;
            var oldHeading = soOld.FindProperty("_headingPrefab").objectReferenceValue as Component;
            var oldButton = soOld.FindProperty("_buttonPrefab").objectReferenceValue as Component;
            var oldSpacer = soOld.FindProperty("_spacerPrefab").objectReferenceValue as Component;
            var oldRowHost = soOld.FindProperty("_rowHostPrefab").objectReferenceValue as Component;

            int sibling = oldRt.GetSiblingIndex();

            var cloneGo = CloneHierarchy(settings.gameObject, mainPanels, panelName, log);
            cloneGo.SetActive(false);
            var cloneRt = cloneGo.GetComponent<RectTransform>();
            CopyRect(settings, cloneRt);
            cloneRt.SetSiblingIndex(sibling);

            DestroyChildNamed(cloneRt, "Shortcut Keys", log);
            foreach (var nested in cloneGo.GetComponents<MainPanelManager>())
            {
                if (nested != null) Object.DestroyImmediate(nested);
            }

            var content = FindChild(cloneRt, "Content") ?? FindDeep(cloneRt, "Content");
            if (content == null)
            {
                log.AppendLine("FAIL: Settings clone missing Content; children=" + ChildNames(cloneRt));
                Object.DestroyImmediate(cloneGo);
                return false;
            }

            // Tabs + right-hand Preview.
            DestroyChildNamed(content, "Categories", log);

            var panelsHost = FindChild(content, "Panels");
            if (panelsHost == null)
            {
                log.AppendLine("FAIL: Settings clone missing Content/Panels");
                Object.DestroyImmediate(cloneGo);
                return false;
            }

            for (int i = panelsHost.childCount - 1; i >= 0; i--)
            {
                var child = panelsHost.GetChild(i);
                if (child.name == "Gameplay") continue;
                log.AppendLine("stripped: Panels/" + child.name);
                Object.DestroyImmediate(child.gameObject);
            }

            DestroyChildNamed(content, "Options Actions", log);

            var gameplay = FindChild(panelsHost, "Gameplay");
            if (gameplay == null)
            {
                log.AppendLine("FAIL: Settings clone missing Panels/Gameplay");
                Object.DestroyImmediate(cloneGo);
                return false;
            }

            var gameplayContent = FindChild(gameplay, "Content");
            var list = gameplayContent != null ? FindChild(gameplayContent, "List") : null;
            var blur = gameplayContent != null ? FindChild(gameplayContent, "Background Blur") : null;
            var background = gameplayContent != null ? FindChild(gameplayContent, "Background") : null;
            if (list == null || blur == null || background == null)
            {
                log.AppendLine("FAIL: Gameplay Content missing Background Blur/Background/List");
                Object.DestroyImmediate(cloneGo);
                return false;
            }

            for (int i = list.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(list.GetChild(i).gameObject);

            // Preview column gone — Gameplay list fills the Settings content area.
            StretchShell((RectTransform)panelsHost);
            StretchShell((RectTransform)gameplay);
            StretchShell((RectTransform)gameplayContent);
            StretchList((RectTransform)list);
            log.AppendLine(panelName + ": Main Menu Gameplay shell kept (Blur/Background/List)");

            if (oldTemplates != null)
            {
                oldTemplates.SetParent(list, false);
                oldTemplates.gameObject.SetActive(false);
                oldTemplates.SetAsFirstSibling();
                log.AppendLine(panelName + ": moved Templates under List");
            }
            else
            {
                var tGo = new GameObject("Templates", typeof(RectTransform));
                oldTemplates = tGo.GetComponent<RectTransform>();
                oldTemplates.SetParent(list, false);
                tGo.SetActive(false);
                log.AppendLine(panelName + ": created Templates under List");
            }

            if (oldWheel != null && panelName == "ARMORY")
            {
                oldWheel.SetParent(list, false);
                oldWheel.gameObject.SetActive(false);
                log.AppendLine(panelName + ": moved Wheel Slot under List");
            }

            MoveNamed(oldRt, cloneRt, "Station Title", log);
            MoveNamed(oldRt, cloneRt, "Banked Salvage", log);
            MoveNamed(oldRt, cloneRt, "Lifetime", log);

            // Close must live under the CLONE. A button borrowed from the old panel is
            // destroyed with it below, and the gate then reads _closeButton as null —
            // that dangling fallback nulled all four stations on 2026-08-11.
            Button close = null;
            var exitGo = FindDeep(cloneRt, "Exit");
            if (exitGo != null)
                close = exitGo.GetComponentInChildren<Button>(true);
            if (close == null)
            {
                var closeGo = FindDeep(cloneRt, "Close");
                if (closeGo != null)
                    close = closeGo.GetComponentInChildren<Button>(true);
            }
            if (exitGo != null && close != null)
            {
                exitGo.name = "Close";
                var tmp = exitGo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null && tmp.text.IndexOf("EXIT", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    tmp.text = "CLOSE";
                log.AppendLine(panelName + ": Hotkeys/Exit → Close");
            }
            if (close == null)
                close = CreateClose(cloneRt, mainButton, log);

            var buttonTemplate = EnsureButtonTemplate(oldTemplates as RectTransform, mainButton, oldButton, log);

            Object.DestroyImmediate(oldView);
            var newView = cloneGo.AddComponent<StationPanelView>();

            var title = FindDeep(cloneRt, "Station Title")?.GetComponentInChildren<TMP_Text>(true) ?? oldTitle;
            var banked = FindDeep(cloneRt, "Banked Salvage")?.GetComponentInChildren<TMP_Text>(true) ?? oldBanked;
            var lifetime = FindDeep(cloneRt, "Lifetime")?.GetComponentInChildren<TMP_Text>(true) ?? oldLifetime;
            if (title != null) title.text = panelName;

            var templates = oldTemplates;
            var so = new SerializedObject(newView);
            SetRef(so, "_root", cloneRt);
            SetRef(so, "_title", title);
            SetRef(so, "_banked", banked);
            SetRef(so, "_lifetime", lifetime);
            SetRef(so, "_bodyContent", list as RectTransform);
            SetRef(so, "_closeButton", close);
            SetRef(so, "_rowPrefab",
                oldRow ?? ComponentNamed(templates, "Row Template", typeof(StationRowView)));
            SetRef(so, "_labelPrefab",
                oldLabel ?? ComponentNamed(templates, "Label Template", typeof(TextMeshProUGUI)));
            SetRef(so, "_headingPrefab",
                oldHeading ?? ComponentNamed(templates, "Heading Template", typeof(TextMeshProUGUI)));
            SetRef(so, "_buttonPrefab", buttonTemplate);
            SetRef(so, "_spacerPrefab",
                oldSpacer ?? ComponentNamed(templates, "Spacer Template", typeof(RectTransform)));
            SetRef(so, "_rowHostPrefab",
                oldRowHost ?? ComponentNamed(templates, "Row Host Template", typeof(RectTransform)));
            if (panelName == "ARMORY")
            {
                var wheelSlot = FindDeep(cloneRt, "Wheel Slot") as RectTransform;
                SetRef(so, "_wheelSlot", wheelSlot);
                SetRef(so, "_wheel",
                    wheelSlot != null ? wheelSlot.GetComponentInChildren<ArmoryWheelView>(true) : null);
            }
            else
            {
                SetRef(so, "_wheelSlot", null);
                SetRef(so, "_wheel", null);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(newView);

            var anim = cloneGo.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            var group = cloneGo.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            if (!RetargetConsole(ship, panelName, newView, log)
                || !RetargetMpm(mpm, panelName, cloneGo, log)
                || close == null)
            {
                Object.DestroyImmediate(cloneGo);
                log.AppendLine("FAIL: " + panelName + " retarget/Close failed — kept old panel");
                return false;
            }

            Object.DestroyImmediate(oldRt.gameObject);
            log.AppendLine(panelName + ": Main Menu Settings shell applied");
            return true;
        }

        static Button CreateClose(Transform panel, GameObject mainButton, StringBuilder log)
        {
            var clone = (GameObject)PrefabUtility.InstantiatePrefab(mainButton, panel);
            clone.name = "Close";
            var rt = clone.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-48f, 48f);
            rt.sizeDelta = new Vector2(240f, 56f);
            var main = clone.GetComponent<MainButton>();
            if (main != null)
            {
                main.buttonText = "CLOSE";
                main.useCustomText = false;
            }
            var tmp = clone.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = "CLOSE";
            log.AppendLine(panel.name + ": created Close");
            return clone.GetComponent<Button>();
        }

        static Button EnsureButtonTemplate(RectTransform templates, GameObject mainButton,
            Component oldButton, StringBuilder log)
        {
            if (oldButton is Button existingBtn && existingBtn != null)
                return existingBtn;
            if (templates == null) return null;

            var existing = FindDeep(templates, "Button Template");
            if (existing != null)
            {
                var b = existing.GetComponent<Button>();
                if (b != null)
                {
                    existing.gameObject.SetActive(false);
                    return b;
                }
            }

            var clone = (GameObject)PrefabUtility.InstantiatePrefab(mainButton, templates);
            clone.name = "Button Template";
            clone.SetActive(false);
            log.AppendLine("created Button Template under Templates");
            return clone.GetComponent<Button>();
        }

        static void StretchShell(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(48f, 120f);
            rt.offsetMax = new Vector2(-48f, -48f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static void StretchList(RectTransform list)
        {
            if (list == null) return;
            list.anchorMin = Vector2.zero;
            list.anchorMax = Vector2.one;
            list.pivot = new Vector2(0.5f, 0.5f);
            list.offsetMin = new Vector2(24f, 24f);
            list.offsetMax = new Vector2(-24f, -24f);
            list.localScale = Vector3.one;
        }

        static bool RetargetConsole(Scene ship, string panelName, StationPanelView view, StringBuilder log)
        {
            foreach (var terminal in Object.FindObjectsByType<StationTerminal>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (terminal == null) continue;
                bool match = terminal.name.IndexOf(panelName.Replace(" ", ""), System.StringComparison.OrdinalIgnoreCase) >= 0
                    || (panelName == "ARMORY" && terminal is ArmoryStation)
                    || (panelName == "FABRICATION" && terminal is EngineeringStation)
                    || (panelName == "CREW QUARTERS" && terminal is CrewQuartersStation)
                    || (panelName == "NAV MAP" && terminal is MapStation);
                if (!match) continue;

                var so = new SerializedObject(terminal);
                SetRef(so, "_view", view);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(terminal);
                log.AppendLine(panelName + ": " + terminal.name + "._view → new StationPanelView");
                return true;
            }
            log.AppendLine("FAIL: no StationTerminal retargeted for " + panelName);
            return false;
        }

        static bool RetargetMpm(MainPanelManager mpm, string panelName, GameObject panel, StringBuilder log)
        {
            var so = new SerializedObject(mpm);
            var panels = so.FindProperty("panels");
            if (panels == null || !panels.isArray)
            {
                log.AppendLine("FAIL: MPM.panels missing on " + mpm.name);
                return false;
            }
            for (int i = 0; i < panels.arraySize; i++)
            {
                var item = panels.GetArrayElementAtIndex(i);
                var nameProp = item.FindPropertyRelative("panelName");
                if (nameProp == null || nameProp.stringValue != panelName) continue;
                item.FindPropertyRelative("panelObject").objectReferenceValue = panel;
                item.FindPropertyRelative("buttonObject").objectReferenceValue = panel;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mpm);
                log.AppendLine(panelName + ": MainPanelManager panelObject → " + panel.name);
                return true;
            }
            log.AppendLine("FAIL: MPM entry missing for " + panelName + " on " + mpm.name);
            return false;
        }

        static MainPanelManager FindMenuManagerPanels(Scene ship)
        {
            var menu = FindNamed(ship, "Menu Manager");
            return menu != null ? menu.GetComponent<MainPanelManager>() : null;
        }

        static Component ComponentNamed(Transform root, string name, System.Type type)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent(type) : null;
        }

        static void MoveNamed(Transform from, Transform to, string name, StringBuilder log)
        {
            var t = FindDeep(from, name);
            if (t == null) return;
            t.SetParent(to, false);
            log.AppendLine("moved " + name);
        }

        static void DestroyChildNamed(Transform parent, string name, StringBuilder log)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c == null || c.name != name) continue;
                log.AppendLine("stripped: " + name);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        static void CopyRect(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.sizeDelta = src.sizeDelta;
            dst.offsetMin = src.offsetMin;
            dst.offsetMax = src.offsetMax;
            dst.localScale = Vector3.one;
            dst.localRotation = src.localRotation;
        }

        static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        static Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
            }
            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        /// <summary>The Settings PANEL, not the SETTINGS nav button — the scene holds both
        /// names. Only the panel has a direct Content child; a button's children are its
        /// Underline/Normal/Highlighted/Pressed states.</summary>
        static RectTransform FindSettingsPanel(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "Settings") continue;
                    if (FindChild(t, "Content") == null) continue;
                    return t as RectTransform;
                }
            }
            return null;
        }

        static RectTransform FindNamed(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name) return t as RectTransform;
                }
            }
            return null;
        }

        static void Finish(StringBuilder log, string status)
        {
            log.AppendLine(status);
            File.WriteAllText(LogPath, log.ToString());
            Debug.Log(log.ToString());
        }
    }
}
