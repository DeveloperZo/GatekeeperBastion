using System;
using System.Collections.Generic;
using System.IO;
using GatekeeperBastion.Game;
using GatekeeperBastion.Game.Ui;
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
    /// Scene roles:
    ///   Splash  — campaign front-end (Continue / Start / Options / Quit).
    ///   Ship    — hub after Continue/Start (station panels only).
    ///   Bastion — combat after Embark.
    /// Menu: GatekeeperBastion/M35 Mutate Front End, or sentinel
    /// gkb_frontend_mutate_request.txt → gkb_frontend_mutate_report.json.
    /// </summary>
    [InitializeOnLoad]
    public static class ShiftFrontEndMutate
    {
        const string DemoPath =
            "Assets/Shift - Complete Sci-Fi UI/Scenes/Main Menu/Main Menu (Desktop).unity";
        const string SplashPath = "Assets/Scenes/Splash.unity";
        const string ShipPath = "Assets/Scenes/Ship.unity";
        const string BastionPath = "Assets/Scenes/Bastion.unity";

        static readonly string[] DemoPanels = { "Campaign", "Multiplayer" };
        static readonly string[] StationPanels = { "ARMORY", "FABRICATION", "CREW QUARTERS", "NAV MAP" };

        static double _nextPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_frontend_mutate_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_frontend_mutate_report.json");

        static ShiftFrontEndMutate() => EditorApplication.update += Poll;

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(SentinelPath)) return;
            try { File.Delete(SentinelPath); }
            catch { return; }
            Run("sentinel");
        }

        [MenuItem("GatekeeperBastion/M35 Mutate Front End")]
        public static void FromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run("menu");
        }

        [MenuItem("GatekeeperBastion/M35 Smoke Scene Flow")]
        public static void SmokeFromMenu() => Smoke("menu");

        public static void FromBatch() => Run("batch");

        static void Run(string trigger)
        {
            string msg;
            string status = "ok";
            try { msg = Mutate(); }
            catch (Exception e)
            {
                status = "fail";
                msg = e.GetType().Name + ": " + e.Message;
                Debug.LogError("[FrontEndMutate] " + e);
            }
            File.WriteAllText(ReportPath,
                "{\"status\":\"" + status + "\",\"message\":\"" +
                msg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}\n");
            Debug.Log("[FrontEndMutate] " + status + " (" + trigger + "): " + msg);
            if (status == "ok") Smoke("after-mutate");
        }

        static string SmokePath => Path.Combine(Root, "gkb_flow_smoke_report.json");

        /// <summary>Edit-mode ownership checks — not a playthrough. Zo still clicks Play for criterion 22.</summary>
        static void Smoke(string trigger)
        {
            var fails = new List<string>();
            try
            {
                var splash = EditorSceneManager.OpenScene(SplashPath, OpenSceneMode.Single);
                if (UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) == null)
                    fails.Add("Splash: missing MainMenuController");
                var splashCanvas = FindRoot(splash, "Canvas");
                if (splashCanvas == null) fails.Add("Splash: missing Canvas");
                else
                {
                    var mgr = splashCanvas.GetComponentInChildren<SplashScreenManager>(true);
                    if (mgr != null && mgr.enableLoginScreen) fails.Add("Splash: enableLoginScreen still true");
                    if (FindDeep(splashCanvas.transform, "Home") == null) fails.Add("Splash: missing Home");
                    if (FindDeep(splashCanvas.transform, "Continue") == null) fails.Add("Splash: missing Continue");
                }

                var ship = EditorSceneManager.OpenScene(ShipPath, OpenSceneMode.Single);
                if (UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) != null)
                    fails.Add("Ship: still has MainMenuController");
                var shipCanvas = FindRoot(ship, "Canvas");
                if (shipCanvas == null) fails.Add("Ship: missing Canvas");
                else
                {
                    foreach (var n in StationPanels)
                        if (FindDeep(shipCanvas.transform, n) == null) fails.Add("Ship: missing " + n);
                    if (FindDeep(shipCanvas.transform, "Splash Screen") != null)
                        fails.Add("Ship: Splash Screen still present");
                }

                var bastion = EditorSceneManager.OpenScene(BastionPath, OpenSceneMode.Single);
                if (FindRoot(bastion, "Canvas - In Game") == null)
                    fails.Add("Bastion: missing Canvas - In Game");

                var build = EditorBuildSettings.scenes;
                if (build == null || build.Length != 3
                    || build[0].path != SplashPath || build[1].path != ShipPath || build[2].path != BastionPath)
                    fails.Add("BuildSettings: expected Splash, Ship, Bastion");
            }
            catch (Exception e)
            {
                fails.Add(e.GetType().Name + ": " + e.Message);
            }

            string status = fails.Count == 0 ? "ok" : "fail";
            string message = fails.Count == 0
                ? "Splash=menu, Ship=stations, Bastion=HUD, build=Splash>Ship>Bastion"
                : string.Join("; ", fails);
            File.WriteAllText(SmokePath,
                "{\"status\":\"" + status + "\",\"trigger\":\"" + trigger + "\",\"message\":\"" +
                message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}\n");
            Debug.Log("[FlowSmoke] " + status + " (" + trigger + "): " + message);
        }

        static string Mutate()
        {
            var parts = new List<string>
            {
                BuildSplashMenu(),
                StripShipCampaignMenu(),
                FixBuildOrder(),
            };
            return string.Join("; ", parts);
        }

        // ---------- A) Splash = full campaign menu ----------

        static string BuildSplashMenu()
        {
            var splash = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(new Color(0.02f, 0.02f, 0.04f));
            EnsureEventSystem();

            var demo = EditorSceneManager.OpenScene(DemoPath, OpenSceneMode.Additive);
            var src = FindRoot(demo, "Canvas");
            if (src == null)
            {
                EditorSceneManager.CloseScene(demo, true);
                EditorSceneManager.SaveScene(splash, SplashPath);
                return "Splash: demo Canvas missing";
            }

            var canvas = UnityEngine.Object.Instantiate(src);
            canvas.name = "Canvas";
            ClearCameraRef(canvas, demo);
            EditorSceneManager.MoveGameObjectToScene(canvas, splash);
            EditorSceneManager.CloseScene(demo, true);

            ForceOverlay(canvas);
            KillLogin(canvas);
            RemoveDemoPanels(canvas);
            ShowMainPanelsImmediately(canvas);
            RelabelHomeButtons(canvas);

            // No SplashController — Splash IS Continue/Start/Options, not a loader to Ship.
            DestroyAllOfTypeInScene<SplashController>(splash);

            var boot = FindRoot(splash, "FrontEnd") ?? FindRoot(splash, "Boot");
            if (boot == null) boot = new GameObject("FrontEnd");
            else boot.name = "FrontEnd";
            EditorSceneManager.MoveGameObjectToScene(boot, splash);
            if (boot.GetComponent<MainMenuController>() == null) boot.AddComponent<MainMenuController>();
            if (boot.GetComponent<MainMenuView>() == null) boot.AddComponent<MainMenuView>();
            if (boot.GetComponent<OptionsView>() == null) boot.AddComponent<OptionsView>();

            EditorSceneManager.MarkSceneDirty(splash);
            EditorSceneManager.SaveScene(splash, SplashPath);
            return "Splash: campaign menu Canvas + FrontEnd host, no SplashController";
        }

        // ---------- B) Ship = stations only ----------

        static string StripShipCampaignMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShipPath) == null)
                return "Ship: missing";

            var ship = EditorSceneManager.OpenScene(ShipPath, OpenSceneMode.Single);
            int removed = 0;

            foreach (var root in ship.GetRootGameObjects())
            {
                if (root.name != "FrontEnd") continue;
                UnityEngine.Object.DestroyImmediate(root);
                removed++;
            }

            var canvas = FindRoot(ship, "Canvas");
            bool rebuild = canvas == null || NeedsStationRebuild(canvas);
            if (rebuild)
            {
                if (canvas != null)
                {
                    UnityEngine.Object.DestroyImmediate(canvas);
                    removed++;
                }
                canvas = ShiftShipUiImport.CopyCanvasFromDemo(ship);
                if (canvas == null)
                    return "Ship: could not copy demo Canvas for station rebuild";
                ShiftShipUiImport.ConfigureStationOnlyCanvas(canvas);
            }
            else
            {
                StripCampaignChrome(canvas, ref removed);
                ShiftShipUiImport.EnsureStationPanels(canvas);
            }

            DestroyAllOfTypeInScene<MainMenuController>(ship);
            DestroyAllOfTypeInScene<MainMenuView>(ship);
            EnsureShipHud(ship);

            EditorSceneManager.MarkSceneDirty(ship);
            EditorSceneManager.SaveScene(ship, ShipPath);
            return rebuild
                ? "Ship: Canvas rebuilt station-only (" + removed + " removed)"
                : "Ship: campaign chrome stripped (" + removed + " removed), stations kept";
        }

        static bool NeedsStationRebuild(GameObject canvas)
        {
            if (FindDeep(canvas.transform, "Splash Screen") != null) return true;
            if (!HasAllStationPanels(canvas)) return true;
            // Full Main Menu copy still exposing campaign Home verbs.
            var home = FindDeep(canvas.transform, "Home");
            if (home != null && home.gameObject.activeSelf)
            {
                if (FindDeep(home, "Continue") != null || FindDeep(home, "Play") != null)
                    return true;
            }
            return false;
        }

        static bool HasAllStationPanels(GameObject canvas)
        {
            var main = canvas.transform.Find("Main Panels");
            if (main == null) return false;
            foreach (var name in StationPanels)
                if (main.Find(name) == null) return false;
            return true;
        }

        static void StripCampaignChrome(GameObject canvas, ref int removed)
        {
            KillLogin(canvas);

            var mgr = canvas.GetComponentInChildren<SplashScreenManager>(true);
            if (mgr != null)
            {
                mgr.disableSplashScreen = true;
                mgr.enableLoginScreen = false;
                mgr.enablePressAnyKeyScreen = false;
                EditorUtility.SetDirty(mgr);
            }

            var splashScreen = FindDeep(canvas.transform, "Splash Screen");
            if (splashScreen != null)
            {
                UnityEngine.Object.DestroyImmediate(splashScreen.gameObject);
                removed++;
            }

            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm == null) return;

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
            }
            mpm.panels = next;
            mpm.currentPanelIndex = 0;
            EditorUtility.SetDirty(mpm);
        }

        static void EnsureShipHud(Scene ship)
        {
            ShipController control = null;
            foreach (var root in ship.GetRootGameObjects())
            {
                control = root.GetComponentInChildren<ShipController>(true);
                if (control != null) break;
            }
            if (control == null) return;
            if (control.GetComponent<ShipHud>() == null)
                control.gameObject.AddComponent<ShipHud>();
        }

        // ---------- C) Build settings ----------

        static string FixBuildOrder()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SplashPath, true),
                new EditorBuildSettingsScene(ShipPath, true),
                new EditorBuildSettingsScene(BastionPath, true),
            };
            return "BuildSettings: Splash → Ship → Bastion";
        }

        // ---------- Splash helpers ----------

        /// <summary>disableSplashScreen so Main Panels (Home) show immediately — not a press-any-key loader.</summary>
        static void ShowMainPanelsImmediately(GameObject canvas)
        {
            var mgr = canvas.GetComponentInChildren<SplashScreenManager>(true);
            if (mgr != null)
            {
                mgr.disableSplashScreen = true;
                mgr.enableLoginScreen = false;
                mgr.enablePressAnyKeyScreen = false;
                EditorUtility.SetDirty(mgr);
            }
            var splash = FindDeep(canvas.transform, "Splash Screen");
            if (splash != null) splash.gameObject.SetActive(false);
            var main = FindDeep(canvas.transform, "Main Panels");
            if (main != null)
            {
                main.gameObject.SetActive(true);
                var cg = main.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
            }
            var home = FindDeep(canvas.transform, "Home");
            if (home != null)
            {
                home.gameObject.SetActive(true);
                var cg = home.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
            }
        }

        static void RelabelHomeButtons(GameObject canvas)
        {
            var home = FindDeep(canvas.transform, "Home");
            if (home == null) return;
            SetButtonText(FindDeep(home, "Play"), "START NEW");
            SetButtonText(FindDeep(home, "Start New"), "START NEW");
            SetButtonText(FindDeep(home, "Continue"), "CONTINUE");
            SetButtonText(FindDeep(home, "Settings"), "OPTIONS");
            SetButtonText(FindDeep(home, "Options"), "OPTIONS");
            SetButtonText(FindDeep(home, "Quit"), "QUIT");
        }

        static void SetButtonText(Transform btn, string text)
        {
            if (btn == null) return;
            var main = btn.GetComponent<MainButton>();
            if (main != null) { main.buttonText = text; main.useCustomText = false; }
            var panel = btn.GetComponent<MainPanelButton>();
            if (panel != null) { panel.buttonText = text; panel.useCustomText = false; }
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = text;
        }

        static void RemoveDemoPanels(GameObject canvas)
        {
            var mpm = canvas.GetComponentInChildren<MainPanelManager>(true);
            if (mpm == null) return;
            var next = new List<MainPanelManager.PanelItem>();
            foreach (var p in mpm.panels)
            {
                if (p == null) continue;
                bool drop = false;
                foreach (var d in DemoPanels)
                    if (p.panelName == d) { drop = true; break; }
                if (drop)
                {
                    if (p.panelObject != null) UnityEngine.Object.DestroyImmediate(p.panelObject);
                    if (p.buttonObject != null) UnityEngine.Object.DestroyImmediate(p.buttonObject);
                    continue;
                }
                next.Add(p);
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

        static void ForceOverlay(GameObject canvasGo)
        {
            var c = canvasGo.GetComponent<Canvas>();
            if (c != null)
            {
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.worldCamera = null;
            }
            canvasGo.transform.localScale = Vector3.one;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1280f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        static void ClearCameraRef(GameObject canvas, Scene demo)
        {
            var c = canvas.GetComponent<Canvas>();
            if (c != null && c.worldCamera != null && c.worldCamera.gameObject.scene == demo)
                c.worldCamera = null;
        }

        static void MakeCamera(Color bg)
        {
            var go = new GameObject("Main Camera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static void DestroyAllOfTypeInScene<T>(Scene scene) where T : Component
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<T>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c != null && c.gameObject.scene == scene)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == name) return r;
            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
