using System;
using System.IO;
using System.Reflection;
using GatekeeperBastion.Game;
using GatekeeperBastion.Game.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>
    /// WP-5 play-path evidence (M35 criterion 41 + Verification §10): Splash → MainMenu →
    /// Ship → stations → EMBARK → Bastion → pause → QUIT TO MENU. Menu / file-bridge only —
    /// no InitializeOnLoad. Screenshots under Docs/GatekeeperZo Notes/m35-playpath/.
    /// </summary>
    public static class M35PlayPathCapture
    {
        const string OutRel = "Docs/GatekeeperZo Notes/m35-playpath";
        const string ReportName = "gkb_m35_playpath_report.json";

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string OutDir => Path.Combine(Root, OutRel);
        static string ReportPath => Path.Combine(Root, ReportName);

        static bool _running;
        static string _phase = "idle";
        static string _error;
        static int _shots;
        static double _waitUntil;
        static StationTerminal[] _terminals;
        static int _terminalIndex;
        static string _pendingShot;
        static double _startedAt;

        [MenuItem("GatekeeperBastion/M35 Play Path Capture")]
        public static void RunFromMenu()
        {
            if (_running)
            {
                Debug.LogWarning("[M35PlayPath] Already running.");
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[M35PlayPath] Stop Play Mode first.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(OutDir);
            _running = true;
            _phase = "enter-play";
            _error = null;
            _shots = 0;
            _terminals = null;
            _terminalIndex = 0;
            _pendingShot = null;
            _waitUntil = 0;
            _startedAt = EditorApplication.timeSinceStartup;
            WriteReport("running");

            EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            EditorApplication.update += Tick;
        }

        public static void RunFromRequest() => RunFromMenu();

        static void Tick()
        {
            if (!_running) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.isCompiling) return;

            // Hard cap — never strand the editor.
            if (EditorApplication.timeSinceStartup - _startedAt > 90.0)
            {
                Fail("timed out after 90s in phase " + _phase);
                return;
            }

            if (_waitUntil > 0 && EditorApplication.timeSinceStartup < _waitUntil) return;

            try
            {
                if (!EditorApplication.isPlaying)
                {
                    if (_phase == "enter-play") return;
                    if (_phase == "done" || _phase == "failed")
                    {
                        FinishTick();
                        return;
                    }
                    Fail("Play Mode stopped during phase " + _phase);
                    return;
                }

                // Flush a deferred screenshot one frame after the UI settles.
                if (_pendingShot != null)
                {
                    CaptureNow(_pendingShot);
                    _pendingShot = null;
                }

                string scene = SceneManager.GetActiveScene().name;
                switch (_phase)
                {
                    case "enter-play":
                        if (scene == "Splash")
                        {
                            ScheduleShot("01-splash");
                            _phase = "wait-menu";
                            Wait(0.2f);
                        }
                        break;

                    case "wait-menu":
                        if (scene == "MainMenu")
                        {
                            ScheduleShot("02-mainmenu");
                            _phase = "click-start";
                            Wait(0.35f);
                        }
                        break;

                    case "click-start":
                    {
                        var view = UnityEngine.Object.FindFirstObjectByType<MainMenuView>();
                        if (view == null) break;
                        view.onStartNew?.Invoke();
                        view.onOverwriteConfirm?.Invoke();
                        _phase = "wait-ship";
                        Wait(0.2f);
                        break;
                    }

                    case "wait-ship":
                        if (scene == "Ship")
                        {
                            ScheduleShot("03-ship");
                            _terminals = UnityEngine.Object.FindObjectsByType<StationTerminal>(
                                FindObjectsSortMode.None);
                            _terminalIndex = 0;
                            _phase = "open-station";
                            Wait(0.4f);
                        }
                        break;

                    case "open-station":
                    {
                        if (_terminals == null || _terminalIndex >= _terminals.Length)
                        {
                            _phase = "embark";
                            break;
                        }
                        var ship = UnityEngine.Object.FindFirstObjectByType<ShipController>();
                        var t = _terminals[_terminalIndex];
                        if (ship == null || t == null) { _terminalIndex++; break; }
                        ship.OpenStationPanel(t);
                        ScheduleShot($"04-station-{Sanitize(t.GetType().Name)}-{_terminalIndex}");
                        _phase = "close-station";
                        Wait(0.55f);
                        break;
                    }

                    case "close-station":
                    {
                        var ship = UnityEngine.Object.FindFirstObjectByType<ShipController>();
                        InvokePrivate(ship, "HandleEscape");
                        _terminalIndex++;
                        _phase = "open-station";
                        Wait(0.3f);
                        break;
                    }

                    case "embark":
                    {
                        var map = UnityEngine.Object.FindFirstObjectByType<MapStation>();
                        var ship = UnityEngine.Object.FindFirstObjectByType<ShipController>();
                        if (map != null && ship != null)
                        {
                            ship.OpenStationPanel(map);
                            ScheduleShot("05-nav-map");
                            _phase = "click-embark";
                            Wait(0.55f);
                        }
                        else
                        {
                            SceneFlow.LoadBastion();
                            _phase = "wait-bastion";
                        }
                        break;
                    }

                    case "click-embark":
                    {
                        var map = UnityEngine.Object.FindFirstObjectByType<MapStation>();
                        if (map != null && GameSession.Meta != null)
                        {
                            var m = typeof(MapStation).GetMethod("TryEmbark",
                                BindingFlags.Instance | BindingFlags.NonPublic);
                            m?.Invoke(map, new object[] { GameSession.Meta });
                        }
                        else SceneFlow.LoadBastion();
                        _phase = "wait-bastion";
                        Wait(0.2f);
                        break;
                    }

                    case "wait-bastion":
                        if (scene == "Bastion")
                        {
                            ScheduleShot("08-bastion-prep");
                            _phase = "bastion-pause";
                            Wait(0.6f);
                        }
                        break;

                    case "bastion-pause":
                    {
                        var hud = UnityEngine.Object.FindFirstObjectByType<HudController>();
                        if (hud == null) break;
                        InvokePrivate(hud, "OpenPause");
                        ScheduleShot("09-bastion-pause");
                        _phase = "quit-menu";
                        Wait(0.5f);
                        break;
                    }

                    case "quit-menu":
                    {
                        var gc = GameController.Instance;
                        if (gc == null) break;
                        gc.QuitToMenu();
                        _phase = "wait-menu-return";
                        Wait(0.2f);
                        break;
                    }

                    case "wait-menu-return":
                        if (scene == "MainMenu")
                        {
                            ScheduleShot("10-quit-to-menu");
                            _phase = "finish";
                            Wait(0.35f);
                        }
                        break;

                    case "finish":
                        EditorApplication.isPlaying = false;
                        _phase = "done";
                        break;
                }
            }
            catch (Exception e)
            {
                Fail(e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        static void ScheduleShot(string name)
        {
            _pendingShot = name;
        }

        static void CaptureNow(string name)
        {
            Directory.CreateDirectory(OutDir);
            string abs = Path.Combine(OutDir, name + ".png");
            // Absolute path — Unity writes there asynchronously; we also copy from Assets/Screenshots if needed.
            ScreenCapture.CaptureScreenshot(abs);
            _shots++;
            Debug.Log($"[M35PlayPath] shot {_shots}: {abs}");
            WriteReport("running");
        }

        static void InvokePrivate(object target, string method)
        {
            if (target == null) return;
            var m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            m?.Invoke(target, null);
        }

        static void Wait(float seconds) =>
            _waitUntil = EditorApplication.timeSinceStartup + seconds;

        static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }

        static void Fail(string msg)
        {
            _error = msg;
            _phase = "failed";
            Debug.LogError("[M35PlayPath] " + msg);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            FinishTick();
            WriteReport("error");
        }

        static void FinishTick()
        {
            EditorApplication.update -= Tick;
            _running = false;
            if (_phase == "done") WriteReport("complete");
        }

        static void WriteReport(string status)
        {
            string json =
                "{\"status\":\"" + status +
                "\",\"phase\":\"" + (_phase ?? "") +
                "\",\"shots\":" + _shots +
                ",\"error\":" + JsonEscape(_error) +
                ",\"timestampUtc\":\"" + DateTime.UtcNow.ToString("o") +
                "\",\"outDir\":\"" + OutRel.Replace("\\", "/") + "\"}";
            File.WriteAllText(ReportPath, json);
        }

        static string JsonEscape(string s)
        {
            if (s == null) return "null";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
