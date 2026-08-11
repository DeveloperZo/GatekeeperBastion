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
    /// <summary>Repairs Ship station panels after a bad Settings-strip pass: canvas scale,
    /// full-screen dim Background, Close button, Button Template, MainPanelManager refs.
    /// Menu: <c>Gatekeeper/HUD/Repair Station Panels</c>.
    /// File bridge: <c>gkb_repair_stations_request.txt</c>.</summary>
    [InitializeOnLoad]
    public static class RepairStationPanels
    {
        const string ShipPath = "Assets/Scenes/Ship.unity";
        const string MainButtonPath =
            "Assets/Shift - Complete Sci-Fi UI/Prefabs/Button/Main Button.prefab";

        static readonly string[] StationNames =
            { "ARMORY", "FABRICATION", "CREW QUARTERS", "NAV MAP" };

        static double _nextPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_repair_stations_request.txt");
        static string LogPath => Path.Combine(Root, "gkb_repair_stations_log.txt");

        static RepairStationPanels() => EditorApplication.update += Poll;

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch { return; }
            EditorApplication.delayCall += RunFromMenu;
        }

        [MenuItem("Gatekeeper/HUD/Repair Station Panels")]
        public static void RunFromMenu() => Run();

        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("[RepairStationPanels]");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish(log, "REFUSED: Play Mode");
                return;
            }

            Scene ship = default;
            bool open = false;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var s = EditorSceneManager.GetSceneAt(i);
                if (s.path != ShipPath) continue;
                ship = s;
                open = true;
                break;
            }
            if (!open) ship = EditorSceneManager.OpenScene(ShipPath, OpenSceneMode.Single);
            if (!ship.IsValid())
            {
                Finish(log, "FAIL: Ship.unity");
                return;
            }

            var canvas = FindNamed(ship, "Canvas");
            if (canvas == null)
            {
                Finish(log, "FAIL: Canvas");
                return;
            }
            if (canvas.localScale.x < 0.5f || canvas.localScale.y < 0.5f)
            {
                canvas.localScale = Vector3.one;
                log.AppendLine("Canvas localScale → 1");
            }

            var mainButton = AssetDatabase.LoadAssetAtPath<GameObject>(MainButtonPath);
            if (mainButton == null)
            {
                Finish(log, "FAIL: Main Button prefab missing at " + MainButtonPath);
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
            {
                var menu = FindNamed(ship, "Menu Manager");
                if (menu != null) mpm = menu.GetComponent<MainPanelManager>();
            }

            foreach (string stationName in StationNames)
            {
                var panel = FindNamed(ship, stationName);
                if (panel == null)
                {
                    log.AppendLine("WARN: missing " + stationName);
                    continue;
                }

                EnsureBackground(panel, log);
                var content = FindDeep(panel, "Content") as RectTransform;
                var templates = FindDeep(panel, "Templates") as RectTransform;
                if (templates == null && content != null)
                {
                    var tGo = new GameObject("Templates", typeof(RectTransform));
                    templates = tGo.GetComponent<RectTransform>();
                    templates.SetParent(content, false);
                    tGo.SetActive(false);
                    log.AppendLine(stationName + ": created Templates");
                }

                Button close = EnsureClose(panel, mainButton, log);
                Button buttonTemplate = EnsureButtonTemplate(templates, mainButton, log);

                var view = panel.GetComponent<StationPanelView>();
                if (view == null) view = panel.gameObject.AddComponent<StationPanelView>();
                WireView(view, panel, content, templates, close, buttonTemplate, log);

                var group = panel.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    // Authored alpha 0 is fine (Panel In); leave it. Ensure interactable.
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }

                if (mpm != null) RetargetMpm(mpm, stationName, panel.gameObject, log);
                RetargetConsole(stationName, view, log);
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
                "OK — station panels repaired (scale, background, Close, Button Template, MPM)"));
        }

        static void EnsureBackground(RectTransform panel, StringBuilder log)
        {
            Transform existing = FindDeep(panel, "Panel Background");
            if (existing != null) return;

            var go = new GameObject("Panel Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
            img.raycastTarget = true;
            log.AppendLine(panel.name + ": added Panel Background");
        }

        static Button EnsureClose(RectTransform panel, GameObject mainButton, StringBuilder log)
        {
            var existing = FindDeep(panel, "Close");
            if (existing != null)
            {
                var b = existing.GetComponent<Button>();
                if (b != null) return b;
            }

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

        static Button EnsureButtonTemplate(RectTransform templates, GameObject mainButton, StringBuilder log)
        {
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
            var main = clone.GetComponent<MainButton>();
            if (main != null)
            {
                main.buttonText = "ACTION";
                main.useCustomText = false;
            }
            log.AppendLine(templates.parent != null
                ? templates.parent.parent.name + ": created Button Template"
                : "created Button Template");
            return clone.GetComponent<Button>();
        }

        static void WireView(StationPanelView view, RectTransform panel, RectTransform content,
            RectTransform templates, Button close, Button buttonTemplate, StringBuilder log)
        {
            var so = new SerializedObject(view);
            SetRef(so, "_root", panel);
            SetRef(so, "_bodyContent", content);
            SetRef(so, "_closeButton", close);
            if (buttonTemplate != null) SetRef(so, "_buttonPrefab", buttonTemplate);

            SetRef(so, "_title", TmpNamed(panel, "Station Title"));
            SetRef(so, "_banked", TmpNamed(panel, "Banked Salvage"));
            SetRef(so, "_lifetime", TmpNamed(panel, "Lifetime"));

            // Keep existing template refs when still valid; only fill gaps from Templates folder.
            FillTemplateIfNull(so, "_labelPrefab", templates, "Label Template", typeof(TextMeshProUGUI));
            FillTemplateIfNull(so, "_headingPrefab", templates, "Heading Template", typeof(TextMeshProUGUI));
            FillTemplateIfNull(so, "_rowPrefab", templates, "Row Template", typeof(StationRowView));
            FillTemplateIfNull(so, "_spacerPrefab", templates, "Spacer Template", typeof(RectTransform));
            FillTemplateIfNull(so, "_rowHostPrefab", templates, "Row Host Template", typeof(RectTransform));

            if (panel.name == "ARMORY")
            {
                var wheelSlot = FindDeep(panel, "Wheel Slot") as RectTransform;
                SetRef(so, "_wheelSlot", wheelSlot);
                SetRef(so, "_wheel",
                    wheelSlot != null ? wheelSlot.GetComponentInChildren<ArmoryWheelView>(true) : null);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            log.AppendLine(panel.name + ": StationPanelView rewired");
        }

        static void FillTemplateIfNull(SerializedObject so, string prop, Transform templates, string name,
            System.Type type)
        {
            var p = so.FindProperty(prop);
            if (p == null || p.objectReferenceValue != null || templates == null) return;
            var t = FindDeep(templates, name);
            if (t == null) return;
            if (type == typeof(RectTransform))
                p.objectReferenceValue = t as RectTransform ?? t.GetComponent<RectTransform>();
            else
                p.objectReferenceValue = t.GetComponent(type);
        }

        static void RetargetMpm(MainPanelManager mpm, string panelName, GameObject panel, StringBuilder log)
        {
            var so = new SerializedObject(mpm);
            var panels = so.FindProperty("panels");
            if (panels == null || !panels.isArray) return;
            for (int i = 0; i < panels.arraySize; i++)
            {
                var item = panels.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("panelName").stringValue != panelName) continue;
                item.FindPropertyRelative("panelObject").objectReferenceValue = panel;
                item.FindPropertyRelative("buttonObject").objectReferenceValue = panel;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mpm);
                log.AppendLine(panelName + ": MPM rebound");
                return;
            }
        }

        static void RetargetConsole(string panelName, StationPanelView view, StringBuilder log)
        {
            foreach (var terminal in Object.FindObjectsByType<StationTerminal>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool match = (panelName == "ARMORY" && terminal is ArmoryStation)
                    || (panelName == "FABRICATION" && terminal is EngineeringStation)
                    || (panelName == "CREW QUARTERS" && terminal is CrewQuartersStation)
                    || (panelName == "NAV MAP" && terminal is MapStation);
                if (!match) continue;
                var so = new SerializedObject(terminal);
                SetRef(so, "_view", view);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(terminal);
                log.AppendLine(panelName + ": console._view rebound");
                return;
            }
        }

        static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        static TMP_Text TmpNamed(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static RectTransform FindNamed(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t as RectTransform;
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
