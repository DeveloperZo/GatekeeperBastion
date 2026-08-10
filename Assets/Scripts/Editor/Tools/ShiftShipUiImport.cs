using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Michsky.UI.Shift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>
    /// M35: ensures station panels on Ship.unity only. Campaign Continue/Start/Options live on
    /// Splash (see <see cref="ShiftFrontEndMutate"/>). Re-running this import must not put the
    /// campaign menu back onto Ship.
    ///
    /// Run from <c>GatekeeperBastion/M35 Import Ship UI</c>, or write
    /// <c>gkb_ship_ui_import_request.txt</c>; result in <c>gkb_ship_ui_import_report.json</c>.
    ///
    /// If Ship already has a Canvas, it is kept and only missing station panels are added.
    /// If Canvas is missing, a Shift Main Menu Canvas is copied then stripped to stations only
    /// (Splash Screen + campaign Home removed). Source scene is never saved.
    /// </summary>
    /// <remarks>
    /// Lives in Editor/Tools rather than Editor/Ui because criterion 1 greps
    /// <c>new GameObject|AddComponent&lt;</c> under any Ui folder and this tool legitimately does both.
    /// </remarks>
    [InitializeOnLoad]
    public static class ShiftShipUiImport
    {
        const string SourceScenePath =
            "Assets/Shift - Complete Sci-Fi UI/Scenes/Main Menu/Main Menu (Desktop).unity";
        const string ShipScenePath = "Assets/Scenes/Ship.unity";

        const string CanvasName = "Canvas";
        const string MainPanelsPath = "Main Panels";
        const string ButtonListPath = "Main Panels/Top Panel/Button List";
        const string ModalWindowsPath = "Modal Windows";
        const string HomePanelName = "Home";
        const string ExitModalName = "Exit";

        /// <summary>Panel names are the station names — <c>StationTerminal.StationName</c> is passed
        /// straight to <c>OpenPanel</c>, so these two lists must not drift apart.</summary>
        static readonly string[] StationPanels = { "ARMORY", "FABRICATION", "CREW QUARTERS", "NAV MAP" };

        /// <summary>Shift demo panels with no game behind them. Their Panel Buttons go with them.</summary>
        static readonly string[] DemoPanels = { "Campaign", "Multiplayer" };

        static double _nextPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_ship_ui_import_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_ship_ui_import_report.json");

        static ShiftShipUiImport()
        {
            EditorApplication.update += PollSentinel;
        }

        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch (Exception e) { Debug.LogWarning("[ShipUiImport] sentinel: " + e.Message); return; }
            Run("sentinel file");
        }

        [MenuItem("GatekeeperBastion/M35 Import Ship UI")]
        public static void ImportFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run("menu");
        }

        /// <summary>Batch / <c>-executeMethod</c> entry.</summary>
        public static void ImportFromBatch() => Run("batch");

        static void Run(string trigger)
        {
            var report = new Report();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.Fail("Refusing '" + trigger + "' — the editor is in play mode.");
                WriteReport(report);
                return;
            }

            try
            {
                Import(report);
            }
            catch (Exception e)
            {
                report.Fail(e.GetType().Name + ": " + e.Message);
                Debug.LogError("[ShipUiImport] " + e);
            }

            WriteReport(report);
            Debug.Log("[ShipUiImport] " + report.Status + " (" + trigger + "): " + report.Message +
                      " — see gkb_ship_ui_import_report.json");
        }

        static void Import(Report report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
            {
                report.Fail("Shift source scene missing: " + SourceScenePath);
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShipScenePath) == null)
            {
                report.Fail("Ship scene missing: " + ShipScenePath +
                            " — run GatekeeperBastion/Build All Scenes first.");
                return;
            }

            Scene ship = EditorSceneManager.OpenScene(ShipScenePath, OpenSceneMode.Single);

            // Campaign FrontEnd host belongs on Splash, never Ship.
            DestroyRootsNamed(ship, "FrontEnd");

            GameObject canvas = FindRoot(ship, CanvasName);
            if (canvas == null)
            {
                canvas = CopyCanvasFromDemo(ship, report);
                if (canvas == null) return;
                ConfigureStationOnlyCanvas(canvas, report);
            }
            else
            {
                // Keep existing Canvas — do not replace with a fresh campaign Main Menu.
                // Stations first (need Home as template), then strip campaign chrome.
                report.RemovedEventSystems = RemoveEventSystemsOutside(ship, canvas);
                EnsureStationPanels(canvas, report);
                StripCampaignFromExisting(canvas, report);
                CheckModalWindows(canvas, report);
            }

            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm != null) EditorUtility.SetDirty(mpm);
            EditorSceneManager.MarkSceneDirty(ship);
            EditorSceneManager.SaveScene(ship, ShipScenePath);

            report.SplashBoot = "skipped — stations only on Ship; campaign menu is Splash";
            report.Succeed($"Ship station UI ensured in {ShipScenePath}; " +
                           $"{report.PanelsAdded.Count} station panel(s) added this run.");
        }

        // ---------- shared with ShiftFrontEndMutate ----------

        /// <summary>Opens the vendor scene additively, clones Canvas into <paramref name="ship"/>,
        /// closes vendor unsaved. Used by mutate rebuild and by import when Canvas is missing.</summary>
        public static GameObject CopyCanvasFromDemo(Scene ship)
        {
            var canvas = CopyCanvasFromDemo(ship, null);
            if (canvas != null) RemoveEventSystemsOutside(ship, canvas);
            return canvas;
        }

        static GameObject CopyCanvasFromDemo(Scene ship, Report report)
        {
            Scene src = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject source = null;
                foreach (var root in src.GetRootGameObjects())
                {
                    if (root.name != CanvasName) continue;
                    source = root;
                    break;
                }
                if (source == null)
                {
                    report?.Fail("No root named '" + CanvasName + "' in " + SourceScenePath);
                    return null;
                }

                var copy = UnityEngine.Object.Instantiate(source);
                copy.name = CanvasName;
                EditorSceneManager.MoveGameObjectToScene(copy, ship);
                copy.transform.SetAsLastSibling();

                ClearRefsIntoScene(copy, src, report);
                if (report != null)
                    report.CopiedObjectCount = copy.GetComponentsInChildren<Transform>(true).Length;
                ForceOverlay(copy, report);
                if (report != null)
                    report.RemovedEventSystems = RemoveEventSystemsOutside(ship, copy);
                return copy;
            }
            finally
            {
                EditorSceneManager.CloseScene(src, true);
            }
        }

        /// <summary>After a fresh copy: overlay, kill login/splash/demo, add stations, hide campaign Home.</summary>
        public static void ConfigureStationOnlyCanvas(GameObject canvas) =>
            ConfigureStationOnlyCanvas(canvas, null);

        static void ConfigureStationOnlyCanvas(GameObject canvas, Report report)
        {
            ForceOverlay(canvas, report);
            if (report != null) RecordShippedBranches(canvas, report);
            KillLogin(canvas);

            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm == null)
            {
                report?.Fail("Copied Canvas has no MainPanelManager — the Shift scene layout changed.");
                return;
            }

            RemoveDemoPanels(mpm, report);
            EnsureStationPanels(canvas, mpm, report);
            DestroySplashScreen(canvas, report);
            HideCampaignHome(mpm, report);
            CheckModalWindows(canvas, report);

            var mgr = canvas.GetComponentInChildren<SplashScreenManager>(true);
            if (mgr != null)
            {
                mgr.disableSplashScreen = true;
                mgr.enableLoginScreen = false;
                mgr.enablePressAnyKeyScreen = false;
                EditorUtility.SetDirty(mgr);
            }
        }

        /// <summary>Idempotent: duplicates Home once per missing station panel and registers it.</summary>
        public static void EnsureStationPanels(GameObject canvas) =>
            EnsureStationPanels(canvas, null);

        static void EnsureStationPanels(GameObject canvas, Report report)
        {
            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm == null)
            {
                report?.Fail("Canvas has no MainPanelManager.");
                return;
            }
            EnsureStationPanels(canvas, mpm, report);
        }

        static void StripCampaignFromExisting(GameObject canvas, Report report)
        {
            KillLogin(canvas);
            DestroySplashScreen(canvas, report);
            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm != null)
            {
                RemoveDemoPanels(mpm, report);
                HideCampaignHome(mpm, report);
            }
            var mgr = canvas.GetComponentInChildren<SplashScreenManager>(true);
            if (mgr != null)
            {
                mgr.disableSplashScreen = true;
                mgr.enableLoginScreen = false;
                mgr.enablePressAnyKeyScreen = false;
                EditorUtility.SetDirty(mgr);
            }
        }

        static void DestroySplashScreen(GameObject canvas, Report report)
        {
            var splash = canvas.transform.Find("Splash Screen");
            if (splash == null)
            {
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Splash Screen") { splash = t; break; }
                }
            }
            if (splash == null) return;
            UnityEngine.Object.DestroyImmediate(splash.gameObject);
            report?.PanelsRemoved.Add("Splash Screen");
        }

        /// <summary>Home (Continue/Play/Settings) is the Splash campaign menu — hide on Ship.</summary>
        static void HideCampaignHome(MainPanelManager mpm, Report report)
        {
            var keep = new HashSet<string>(StationPanels) { "Settings" };
            var next = new List<MainPanelManager.PanelItem>();
            foreach (var p in mpm.panels)
            {
                if (p == null || p.panelObject == null) continue;
                if (keep.Contains(p.panelName))
                {
                    next.Add(p);
                    continue;
                }
                if (p.panelObject != null) p.panelObject.SetActive(false);
                if (p.buttonObject != null) p.buttonObject.SetActive(false);
                report?.PanelsRemoved.Add(p.panelName + " (hidden on Ship)");
            }
            mpm.panels = next;
            mpm.currentPanelIndex = 0;
            EditorUtility.SetDirty(mpm);
        }

        static void KillLogin(GameObject canvas)
        {
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                string n = t.name;
                if (n == "Login" || n == "Sign Up" || n == "Login Stuff"
                    || n == "Register Stuff" || n == "Login Button")
                    t.gameObject.SetActive(false);
            }
        }

        static void ClearRefsIntoScene(GameObject root, Scene foreign, Report report)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var so = new SerializedObject(component);
                var prop = so.GetIterator();
                bool changed = false;
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = prop.objectReferenceValue;
                    if (value == null) continue;
                    GameObject owner = value as GameObject;
                    if (owner == null && value is Component c) owner = c.gameObject;
                    if (owner == null) continue;
                    if (owner.scene != foreign) continue;
                    report?.ClearedRefs.Add(
                        PathOf(component.transform) + " :: " + component.GetType().Name + "." +
                        prop.propertyPath + " -> " + owner.name);
                    prop.objectReferenceValue = null;
                    changed = true;
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Shift authors the menu at Screen Space – Camera against its own Main Camera,
        /// which we do not copy. Overlay needs no camera and keeps the 1920×1280 scaler intact.</summary>
        static void ForceOverlay(GameObject canvasRoot, Report report)
        {
            var canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                report?.Warnings.Add("Copied root has no Canvas component.");
                return;
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvasRoot.transform.localScale = Vector3.one;

            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            if (report != null)
            {
                report.CanvasReference = scaler != null
                    ? $"Overlay, reference {scaler.referenceResolution.x}x{scaler.referenceResolution.y}, match {scaler.matchWidthOrHeight}"
                    : "Overlay, no CanvasScaler";
            }
        }

        static int DestroyRootsNamed(Scene scene, string name)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != name) continue;
                UnityEngine.Object.DestroyImmediate(root);
                n++;
            }
            return n;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == name) return r;
            return null;
        }

        /// <summary>The Shift Canvas nests its own Event System; a second one spams uGUI warnings.</summary>
        static int RemoveEventSystemsOutside(Scene scene, GameObject keepUnder)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == keepUnder) continue;
                foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
                {
                    UnityEngine.Object.DestroyImmediate(es.gameObject);
                    n++;
                }
            }
            return n;
        }

        static void RecordShippedBranches(GameObject canvas, Report report)
        {
            Require(canvas, "Menu Manager", report, typeof(SplashScreenManager), typeof(MainPanelManager));
            Require(canvas, "Quality Manager", report, typeof(QualityManager));
            Require(canvas, "Background", report);
            Require(canvas, "Splash Screen", report, typeof(Animator));
            Require(canvas, MainPanelsPath, report, typeof(Animator), typeof(CanvasGroup));
            Require(canvas, ModalWindowsPath, report, typeof(BlurManager));
            Require(canvas, "Start Fade-in", report, typeof(Animator));
        }

        static void Require(GameObject canvas, string path, Report report, params Type[] components)
        {
            var t = canvas.transform.Find(path);
            if (t == null)
            {
                report.Warnings.Add("Missing after copy: " + path);
                report.Branches[path] = "MISSING";
                return;
            }
            var found = new List<string>();
            foreach (var type in components)
            {
                if (t.GetComponent(type) != null) found.Add(type.Name);
                else report.Warnings.Add(path + " has no " + type.Name + ".");
            }
            report.Branches[path] = found.Count == 0 ? "present" : "present (" + string.Join(", ", found) + ")";
        }

        static void RemoveDemoPanels(MainPanelManager mpm, Report report)
        {
            foreach (string name in DemoPanels)
            {
                for (int i = mpm.panels.Count - 1; i >= 0; i--)
                {
                    if (mpm.panels[i].panelName != name) continue;
                    if (mpm.panels[i].panelObject != null)
                        UnityEngine.Object.DestroyImmediate(mpm.panels[i].panelObject);
                    if (mpm.panels[i].buttonObject != null)
                        UnityEngine.Object.DestroyImmediate(mpm.panels[i].buttonObject);
                    mpm.panels.RemoveAt(i);
                    report?.PanelsRemoved.Add(name);
                }
            }
            mpm.currentPanelIndex = 0;
        }

        static void EnsureStationPanels(GameObject canvas, MainPanelManager mpm, Report report)
        {
            Transform mainPanels = canvas.transform.Find(MainPanelsPath);
            Transform buttonList = canvas.transform.Find(ButtonListPath);
            if (mainPanels == null || buttonList == null)
            {
                report?.Fail("Canvas is missing '" + MainPanelsPath + "' or '" + ButtonListPath + "'.");
                return;
            }

            Transform home = mainPanels.Find(HomePanelName);
            GameObject homeButton = ButtonFor(mpm, HomePanelName);
            // After HideCampaignHome, Home may be gone from mpm — still find the GO under Main Panels.
            if (homeButton == null && home != null)
            {
                foreach (Transform t in buttonList)
                {
                    if (t.name == HomePanelName) { homeButton = t.gameObject; break; }
                }
            }
            if (home == null || homeButton == null)
            {
                // Station panels already present and Home was stripped — nothing to duplicate from.
                bool anyStation = false;
                foreach (var name in StationPanels)
                    if (mainPanels.Find(name) != null) { anyStation = true; break; }
                if (anyStation)
                {
                    report?.Warnings.Add("Home template missing; existing station panels left alone.");
                    return;
                }
                report?.Fail("Home panel or its Panel Button is missing — nothing to duplicate.");
                return;
            }

            int listIndex = Mathf.Max(0, mpm.panels.FindIndex(p => p.panelName == HomePanelName)) + 1;
            if (listIndex <= 0) listIndex = mpm.panels.Count;
            int siblingIndex = homeButton.transform.GetSiblingIndex() + 1;

            for (int i = 0; i < StationPanels.Length; i++)
            {
                string name = StationPanels[i];
                if (mainPanels.Find(name) != null)
                {
                    report?.Warnings.Add("Panel '" + name + "' already exists — left alone.");
                    // Ensure registration if the GO exists but mpm lost it.
                    if (ButtonFor(mpm, name) == null)
                    {
                        var existing = mainPanels.Find(name).gameObject;
                        GameObject btn = null;
                        foreach (Transform t in buttonList)
                            if (t.name == name) { btn = t.gameObject; break; }
                        mpm.panels.Insert(Mathf.Min(listIndex + i, mpm.panels.Count),
                            new MainPanelManager.PanelItem
                            {
                                panelName = name,
                                panelObject = existing,
                                buttonObject = btn,
                            });
                    }
                    continue;
                }

                var panel = UnityEngine.Object.Instantiate(home.gameObject, mainPanels, false);
                panel.name = name;
                int cleared = ClearContent(panel, report);

                var group = panel.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 0f;
                panel.SetActive(false);

                var button = UnityEngine.Object.Instantiate(homeButton, buttonList, false);
                button.name = name;
                button.transform.SetSiblingIndex(siblingIndex + i);
                LabelPanelButton(button, name, report);
                RewireOpenPanel(button, mpm, name, report);

                mpm.panels.Insert(Mathf.Min(listIndex + i, mpm.panels.Count), new MainPanelManager.PanelItem
                {
                    panelName = name,
                    panelObject = panel,
                    buttonObject = button,
                });

                report?.PanelsAdded.Add(name + " (Content emptied of " + cleared + " Home child object(s))");
            }
        }

        static int ClearContent(GameObject panel, Report report)
        {
            var content = panel.transform.Find("Content");
            if (content == null)
            {
                report?.Warnings.Add(panel.name + " has no 'Content' child after duplication.");
                return 0;
            }
            int n = content.childCount;
            for (int i = n - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(content.GetChild(i).gameObject);
            return n;
        }

        static void LabelPanelButton(GameObject button, string label, Report report)
        {
            var mpb = button.GetComponent<MainPanelButton>();
            if (mpb == null)
            {
                report?.Warnings.Add("Panel Button copy for '" + label + "' has no MainPanelButton.");
                return;
            }
            mpb.useCustomText = false;
            mpb.buttonText = label;
            if (mpb.normalText != null) mpb.normalText.text = label;
            if (mpb.highlightedText != null) mpb.highlightedText.text = label;
            if (mpb.pressedText != null) mpb.pressedText.text = label;
            EditorUtility.SetDirty(mpb);
        }

        static void RewireOpenPanel(GameObject button, MainPanelManager mpm, string panelName, Report report)
        {
            var ui = button.GetComponent<Button>();
            if (ui == null)
            {
                report?.Warnings.Add("Panel Button copy for '" + panelName + "' has no Button.");
                return;
            }

            var so = new SerializedObject(ui);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null)
            {
                report?.Warnings.Add("Could not reach m_OnClick on the '" + panelName + "' button.");
                return;
            }
            calls.arraySize = 1;
            var call = calls.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue = mpm;
            call.FindPropertyRelative("m_MethodName").stringValue = "OpenPanel";
            call.FindPropertyRelative("m_Mode").enumValueIndex = 5; // PersistentListenerMode.String
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // UnityEventCallState.RuntimeOnly
            call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = panelName;
            var targetType = call.FindPropertyRelative("m_TargetAssemblyTypeName");
            if (targetType != null)
            {
                var t = typeof(MainPanelManager);
                targetType.stringValue = t.FullName + ", " + t.Assembly.GetName().Name;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            report?.ButtonsWired.Add(panelName + " -> MainPanelManager.OpenPanel(\"" + panelName + "\")");
        }

        static void CheckModalWindows(GameObject canvas, Report report)
        {
            if (report == null) return;
            var modals = canvas.transform.Find(ModalWindowsPath);
            if (modals == null)
            {
                report.Warnings.Add("No '" + ModalWindowsPath + "' under the copied Canvas.");
                return;
            }
            report.BlurManagerOnModalParent = modals.GetComponent<BlurManager>() != null;
            foreach (var m in modals.GetComponentsInChildren<ModalWindowManager>(true))
            {
                report.ModalWindows.Add(m.gameObject.name);
                if (m.gameObject.name == ExitModalName) report.ExitModalPresent = true;
            }
            if (!report.ExitModalPresent)
                report.Warnings.Add("No modal named '" + ExitModalName + "' — criterion 7 needs Exit.prefab.");
        }

        static GameObject ButtonFor(MainPanelManager mpm, string panelName)
        {
            foreach (var p in mpm.panels)
                if (p.panelName == panelName) return p.buttonObject;
            return null;
        }

        static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
                sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        // ---------- report ----------

        sealed class Report
        {
            public string Status = "error";
            public string Message = "Import did not run.";
            public int ReplacedExistingCanvas;
            public int CopiedObjectCount;
            public int RemovedEventSystems;
            public string CanvasReference = "";
            public string SplashBoot = "not touched";
            public bool BlurManagerOnModalParent;
            public bool ExitModalPresent;
            public readonly Dictionary<string, string> Branches = new Dictionary<string, string>();
            public readonly List<string> PanelsAdded = new List<string>();
            public readonly List<string> PanelsRemoved = new List<string>();
            public readonly List<string> ButtonsWired = new List<string>();
            public readonly List<string> ModalWindows = new List<string>();
            public readonly List<string> ClearedRefs = new List<string>();
            public readonly List<string> Warnings = new List<string>();

            public void Fail(string why)
            {
                Status = "error";
                Message = why;
            }

            public void Succeed(string what)
            {
                Status = Warnings.Count == 0 ? "complete" : "complete-with-warnings";
                Message = what;
            }
        }

        static void WriteReport(Report r)
        {
            var sb = new StringBuilder();
            sb.Append("{\"status\":").Append(Quote(r.Status))
              .Append(",\"message\":").Append(Quote(r.Message))
              .Append(",\"unity\":").Append(Quote(Application.unityVersion))
              .Append(",\"timestampUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")))
              .Append(",\"sourceScene\":").Append(Quote(SourceScenePath))
              .Append(",\"targetScene\":").Append(Quote(ShipScenePath))
              .Append(",\"replacedExistingCanvas\":").Append(r.ReplacedExistingCanvas)
              .Append(",\"copiedObjectCount\":").Append(r.CopiedObjectCount)
              .Append(",\"removedEventSystems\":").Append(r.RemovedEventSystems)
              .Append(",\"canvas\":").Append(Quote(r.CanvasReference))
              .Append(",\"splashBoot\":").Append(Quote(r.SplashBoot))
              .Append(",\"blurManagerOnModalParent\":").Append(r.BlurManagerOnModalParent ? "true" : "false")
              .Append(",\"exitModalPresent\":").Append(r.ExitModalPresent ? "true" : "false")
              .Append(",\"branches\":").Append(JsonMap(r.Branches))
              .Append(",\"panelsAdded\":").Append(JsonArray(r.PanelsAdded))
              .Append(",\"panelsRemoved\":").Append(JsonArray(r.PanelsRemoved))
              .Append(",\"buttonsWired\":").Append(JsonArray(r.ButtonsWired))
              .Append(",\"modalWindows\":").Append(JsonArray(r.ModalWindows))
              .Append(",\"clearedExternalRefs\":").Append(JsonArray(r.ClearedRefs))
              .Append(",\"warnings\":").Append(JsonArray(r.Warnings))
              .Append('}');
            try { File.WriteAllText(ReportPath, sb.ToString()); }
            catch (Exception e) { Debug.LogError("[ShipUiImport] Could not write report: " + e.Message); }
        }

        static string JsonArray(List<string> items)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Quote(items[i]));
            }
            return sb.Append(']').ToString();
        }

        static string JsonMap(Dictionary<string, string> items)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in items)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(Quote(kv.Key)).Append(':').Append(Quote(kv.Value));
            }
            return sb.Append('}').ToString();
        }

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
