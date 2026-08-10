using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GatekeeperBastion.Game;
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
    /// M35 B4–B5: copies the whole <c>Canvas</c> branch of Shift's Main Menu (Desktop) scene into
    /// Assets/Scenes/Ship.unity, duplicates <c>Home</c> once per station panel, and registers each
    /// duplicate in <see cref="MainPanelManager.panels"/> with its own Panel Button. Nothing is
    /// generated — every object is a copy of an object Shift shipped.
    ///
    /// Run it from <c>GatekeeperBastion/M35 Import Ship UI</c>, or by writing
    /// <c>gkb_ship_ui_import_request.txt</c> at the project root; the result lands in
    /// <c>gkb_ship_ui_import_report.json</c> either way.
    ///
    /// Idempotent: an existing <c>Canvas</c> root in Ship.unity is replaced, so re-running after a
    /// Shift update re-copies cleanly. Anything authored under a panel's <c>Content</c> is lost on
    /// re-run — that is the cost of the copy being a copy.
    ///
    /// The source scene is opened additively and closed WITHOUT saving. Nothing under
    /// Assets/Shift - Complete Sci-Fi UI/ is written.
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
        const string SplashScenePath = "Assets/Scenes/Splash.unity";

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
            report.ReplacedExistingCanvas = DestroyRootsNamed(ship, CanvasName);

            GameObject canvas = CopyCanvasFromSource(ship, report);
            if (canvas == null) return;

            ForceOverlay(canvas, report);
            report.RemovedEventSystems = RemoveEventSystemsOutside(ship, canvas);

            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm == null)
            {
                report.Fail("Copied Canvas has no MainPanelManager — the Shift scene layout changed.");
                return;
            }

            RecordShippedBranches(canvas, report);
            RemoveDemoPanels(mpm, report);
            AddStationPanels(canvas, mpm, report);
            CheckModalWindows(canvas, report);

            EditorUtility.SetDirty(mpm);
            EditorSceneManager.MarkSceneDirty(ship);
            EditorSceneManager.SaveScene(ship, ShipScenePath);

            WireSplashBoot(report);

            // Leave the editor on the scene the work landed in, not on Splash.
            EditorSceneManager.OpenScene(ShipScenePath, OpenSceneMode.Single);

            report.Succeed($"Copied the Shift Canvas into {ShipScenePath}; " +
                           $"{report.PanelsAdded.Count} station panel(s) registered.");
        }

        /// <summary>Opens the vendor scene additively, clones its Canvas into the Ship scene, then
        /// closes the vendor scene without saving.</summary>
        static GameObject CopyCanvasFromSource(Scene ship, Report report)
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
                    report.Fail("No root named '" + CanvasName + "' in " + SourceScenePath);
                    return null;
                }

                var copy = UnityEngine.Object.Instantiate(source);
                copy.name = CanvasName;
                EditorSceneManager.MoveGameObjectToScene(copy, ship);
                copy.transform.SetAsLastSibling();

                // Instantiate remaps references that point inside the branch and keeps the rest.
                // Anything still aimed at the vendor scene (its Main Camera) dangles the moment we
                // close that scene, so clear it here where we can still name what was cleared.
                ClearRefsIntoScene(copy, src, report);
                report.CopiedObjectCount = copy.GetComponentsInChildren<Transform>(true).Length;
                return copy;
            }
            finally
            {
                EditorSceneManager.CloseScene(src, true);
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
                    if (owner == null) continue;           // asset reference — keep
                    if (owner.scene != foreign) continue;  // in-branch reference — keep
                    report.ClearedRefs.Add(
                        PathOf(component.transform) + " :: " + component.GetType().Name + "." +
                        prop.propertyPath + " -> " + owner.name);
                    prop.objectReferenceValue = null;
                    changed = true;
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Shift authors the menu at Screen Space – Camera against its own Main Camera,
        /// which we do not copy. Overlay needs no camera and keeps the 1920×1280 scaler intact.
        /// Cost: Shift's UI blur samples a grabbed frame and will not blur under Overlay.</summary>
        static void ForceOverlay(GameObject canvasRoot, Report report)
        {
            var canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                report.Warnings.Add("Copied root has no Canvas component.");
                return;
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvasRoot.transform.localScale = Vector3.one;

            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            report.CanvasReference = scaler != null
                ? $"Overlay, reference {scaler.referenceResolution.x}x{scaler.referenceResolution.y}, match {scaler.matchWidthOrHeight}"
                : "Overlay, no CanvasScaler";
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

        /// <summary>Criterion 4's checklist, recorded as measurements rather than assumed.</summary>
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
                    report.PanelsRemoved.Add(name);
                }
            }
            // Awake indexes panels[currentPanelIndex] before anything else runs.
            mpm.currentPanelIndex = 0;
        }

        static void AddStationPanels(GameObject canvas, MainPanelManager mpm, Report report)
        {
            Transform mainPanels = canvas.transform.Find(MainPanelsPath);
            Transform buttonList = canvas.transform.Find(ButtonListPath);
            if (mainPanels == null || buttonList == null)
            {
                report.Fail("Copied Canvas is missing '" + MainPanelsPath + "' or '" + ButtonListPath + "'.");
                return;
            }

            Transform home = mainPanels.Find(HomePanelName);
            GameObject homeButton = ButtonFor(mpm, HomePanelName);
            if (home == null || homeButton == null)
            {
                report.Fail("Home panel or its Panel Button is missing — nothing to duplicate.");
                return;
            }

            int listIndex = Mathf.Max(0, mpm.panels.FindIndex(p => p.panelName == HomePanelName)) + 1;
            int siblingIndex = homeButton.transform.GetSiblingIndex() + 1;

            for (int i = 0; i < StationPanels.Length; i++)
            {
                string name = StationPanels[i];
                if (mainPanels.Find(name) != null)
                {
                    report.Warnings.Add("Panel '" + name + "' already exists — left alone.");
                    continue;
                }

                var panel = UnityEngine.Object.Instantiate(home.gameObject, mainPanels, false);
                panel.name = name;
                int cleared = ClearContent(panel, report);

                // Shift stores an inactive panel as alpha 0; MainPanelManager fades it back in.
                var group = panel.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 0f;
                panel.SetActive(false);

                var button = UnityEngine.Object.Instantiate(homeButton, buttonList, false);
                button.name = name;
                button.transform.SetSiblingIndex(siblingIndex + i);
                LabelPanelButton(button, name, report);
                RewireOpenPanel(button, mpm, name, report);

                mpm.panels.Insert(listIndex + i, new MainPanelManager.PanelItem
                {
                    panelName = name,
                    panelObject = panel,
                    buttonObject = button,
                });

                report.PanelsAdded.Add(name + " (Content emptied of " + cleared + " Home child object(s))");
            }
        }

        /// <summary>Keeps Shortcut Keys / Hotkeys / Content and empties Content — Home's profile card
        /// and placeholder model are Shift demo content, and Content is where ours goes.</summary>
        static int ClearContent(GameObject panel, Report report)
        {
            var content = panel.transform.Find("Content");
            if (content == null)
            {
                report.Warnings.Add(panel.name + " has no 'Content' child after duplication.");
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
                report.Warnings.Add("Panel Button copy for '" + label + "' has no MainPanelButton.");
                return;
            }
            mpb.useCustomText = false;
            mpb.buttonText = label;
            // OnEnable pushes buttonText onto these at runtime; set them now so the editor reads right.
            if (mpb.normalText != null) mpb.normalText.text = label;
            if (mpb.highlightedText != null) mpb.highlightedText.text = label;
            if (mpb.pressedText != null) mpb.pressedText.text = label;
            EditorUtility.SetDirty(mpb);
        }

        /// <summary>Retargets the copied button's persistent onClick to OpenPanel("&lt;name&gt;").
        /// Edited through SerializedProperty so the call keeps Shift's mode/target shape.</summary>
        static void RewireOpenPanel(GameObject button, MainPanelManager mpm, string panelName, Report report)
        {
            var ui = button.GetComponent<Button>();
            if (ui == null)
            {
                report.Warnings.Add("Panel Button copy for '" + panelName + "' has no Button.");
                return;
            }

            var so = new SerializedObject(ui);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null)
            {
                report.Warnings.Add("Could not reach m_OnClick on the '" + panelName + "' button.");
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
            report.ButtonsWired.Add(panelName + " -> MainPanelManager.OpenPanel(\"" + panelName + "\")");
        }

        static void CheckModalWindows(GameObject canvas, Report report)
        {
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

        /// <summary>Splash.unity is the boot scene and nothing else: SplashController async-loads Ship,
        /// and Shift's Splash Screen panel on Ship's Canvas is the splash the player actually sees.</summary>
        static void WireSplashBoot(Report report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SplashScenePath) == null)
            {
                report.Warnings.Add("Splash scene missing: " + SplashScenePath);
                return;
            }
            Scene splash = EditorSceneManager.OpenScene(SplashScenePath, OpenSceneMode.Single);
            var existing = UnityEngine.Object.FindFirstObjectByType<SplashController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                report.SplashBoot = "already wired on '" + existing.gameObject.name + "'";
                return;
            }
            var go = new GameObject("Boot");
            go.AddComponent<SplashController>();
            EditorSceneManager.MarkSceneDirty(splash);
            EditorSceneManager.SaveScene(splash, SplashScenePath);
            report.SplashBoot = "added 'Boot' with SplashController";
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
