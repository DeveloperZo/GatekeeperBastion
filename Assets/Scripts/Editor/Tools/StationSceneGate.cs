using System.Text;
using GatekeeperBastion.Game;
using Michsky.UI.Shift;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Hard postconditions for Ship station UI. A tool that mutates station panels
    /// may not write <c>OK</c> unless every check here passes. WARN lines in a log also
    /// forbid <c>OK</c>.</summary>
    public static class StationSceneGate
    {
        static readonly string[] Stations =
            { "ARMORY", "FABRICATION", "CREW QUARTERS", "NAV MAP" };

        /// <summary>Returns false and appends FAIL lines when the Ship station open path
        /// cannot paint. Call after scene mutations, before SaveScene.</summary>
        public static bool AssertOpenPath(Scene ship, StringBuilder log)
        {
            bool ok = true;
            var canvas = FindNamed(ship, "Canvas");
            if (canvas == null)
            {
                log.AppendLine("FAIL: Canvas missing");
                return false;
            }
            if (canvas.localScale.x < 0.5f || canvas.localScale.y < 0.5f)
            {
                log.AppendLine("FAIL: Canvas localScale < 0.5 (UI will paint blank)");
                ok = false;
            }
            if (DirectChild(canvas, "Background") == null)
            {
                log.AppendLine("FAIL: Canvas/Background missing (Main Menu Settings chrome)");
                ok = false;
            }
            var mainPanels = FindNamed(ship, "Main Panels");
            if (mainPanels == null || DirectChild(mainPanels, "Bottom Panel") == null)
            {
                log.AppendLine("FAIL: Main Panels/Bottom Panel missing (Main Menu Settings chrome)");
                ok = false;
            }
            if (mainPanels == null || DirectChild(mainPanels, "Top Panel") == null)
            {
                log.AppendLine("FAIL: Main Panels/Top Panel missing");
                ok = false;
            }

            var shipController = Object.FindFirstObjectByType<ShipController>(FindObjectsInactive.Include);
            if (shipController == null)
            {
                log.AppendLine("FAIL: ShipController missing");
                return false;
            }
            var soShip = new SerializedObject(shipController);
            var mpm = soShip.FindProperty("_panels")?.objectReferenceValue as MainPanelManager;
            var canvasRoot = soShip.FindProperty("_stationCanvas")?.objectReferenceValue as GameObject;
            if (mpm == null)
            {
                log.AppendLine("FAIL: ShipController._panels null");
                ok = false;
            }
            if (canvasRoot == null)
            {
                log.AppendLine("FAIL: ShipController._stationCanvas null");
                ok = false;
            }

            if (mpm != null)
            {
                var so = new SerializedObject(mpm);
                var panels = so.FindProperty("panels");
                if (panels == null || !panels.isArray)
                {
                    log.AppendLine("FAIL: MainPanelManager.panels missing");
                    ok = false;
                }
                else
                {
                    foreach (string name in Stations)
                    {
                        if (!MpmHasLivePanel(panels, name, log))
                            ok = false;
                    }
                }
            }

            foreach (string name in Stations)
            {
                if (!StationPanelReady(ship, name, log))
                    ok = false;
            }

            return ok;
        }

        /// <summary>OK is forbidden when the log already contains FAIL or WARN, or when
        /// <paramref name="assertOk"/> is false.</summary>
        public static string StatusLine(StringBuilder log, bool assertOk, string okMessage)
        {
            string text = log.ToString();
            bool poisoned = text.Contains("FAIL:") || text.Contains("WARN:");
            if (!assertOk || poisoned)
                return "FAIL: station scene gate — fix WARN/FAIL lines above; do not treat this run as done";
            return okMessage;
        }

        static bool MpmHasLivePanel(SerializedProperty panels, string name, StringBuilder log)
        {
            for (int i = 0; i < panels.arraySize; i++)
            {
                var item = panels.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("panelName").stringValue != name) continue;
                var go = item.FindPropertyRelative("panelObject").objectReferenceValue as GameObject;
                if (go == null)
                {
                    log.AppendLine("FAIL: MPM '" + name + "' panelObject is null");
                    return false;
                }
                return true;
            }
            log.AppendLine("FAIL: MPM missing entry '" + name + "'");
            return false;
        }

        static bool StationPanelReady(Scene ship, string name, StringBuilder log)
        {
            var panel = FindNamed(ship, name);
            if (panel == null)
            {
                log.AppendLine("FAIL: panel GO missing '" + name + "'");
                return false;
            }
            var view = panel.GetComponent<StationPanelView>();
            if (view == null)
            {
                log.AppendLine("FAIL: " + name + " missing StationPanelView");
                return false;
            }
            var so = new SerializedObject(view);
            bool ok = true;
            ok &= Require(so, "_bodyContent", name, log);
            ok &= Require(so, "_closeButton", name, log);
            ok &= Require(so, "_buttonPrefab", name, log);
            ok &= Require(so, "_labelPrefab", name, log);
            ok &= Require(so, "_rowPrefab", name, log);

            if (FindDeep(panel, "Background Blur") == null)
            {
                log.AppendLine("FAIL: " + name + " missing Background Blur (Settings chrome)");
                ok = false;
            }
            if (FindDeep(panel, "Background") == null)
            {
                log.AppendLine("FAIL: " + name + " missing Background (Settings chrome)");
                ok = false;
            }
            var list = FindDeep(panel, "List");
            if (list == null)
            {
                log.AppendLine("FAIL: " + name + " missing List (Settings body host)");
                ok = false;
            }
            else
            {
                var body = so.FindProperty("_bodyContent")?.objectReferenceValue as RectTransform;
                if (body == null || body.gameObject != list.gameObject)
                {
                    log.AppendLine("FAIL: " + name + " StationPanelView._bodyContent is not List");
                    ok = false;
                }
            }

            if (FindDeep(panel, "Panel Background") != null
                && FindDeep(panel, "Background Blur") == null)
            {
                log.AppendLine("FAIL: " + name + " has homemade Panel Background without Settings chrome");
                ok = false;
            }

            return ok;
        }

        static bool Require(SerializedObject so, string prop, string station, StringBuilder log)
        {
            var p = so.FindProperty(prop);
            if (p != null && p.objectReferenceValue != null) return true;
            log.AppendLine("FAIL: " + station + " StationPanelView." + prop + " is null");
            return false;
        }

        static Transform DirectChild(Transform parent, string name)
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
    }
}
