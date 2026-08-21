using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GatekeepersProtocol.Game;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>
    /// One-click walkthrough video of the Ship scene, recorded from the player rig in Play mode.
    /// The route is walked by the real <see cref="FirstPersonController"/> object through its own
    /// CharacterController, so walls stop the camera instead of being flown through, and the
    /// footage is the player's own camera (and any weapon/overlay camera) rather than a free cam.
    ///
    /// Capture is UnityEditor.Media.MediaEncoder — editor built-in, no package. Every enabled
    /// camera is retargeted at one RenderTexture for the duration, so Screen Space - Overlay UI
    /// does not appear in the video. Time.captureFramerate pins the step at <see cref="Fps"/>,
    /// which makes playback speed independent of how slow encoding runs.
    ///
    /// Output lands at the repository root as ShipFlythrough.mp4, then Play mode exits.
    /// </summary>
    public static class ShipFlythrough
    {
        const string ShipScene = "Assets/Scenes/Ship - Demo.unity";
        const string OutputName = "ShipFlythrough.mp4";
        const string PendingKey = "GKB.ShipFlythrough.Pending";

        internal const float Grid = 5f;         // SceneBuilder.Ship G
        internal const float WalkSpeed = 2.6f;  // metres per second, below the 5 m/s walk
        internal const float TurnRate = 220f;   // degrees per second of yaw
        internal const float ArriveRadius = 0.9f;
        internal const float StuckSeconds = 3f; // no progress for this long, skip the waypoint
        internal const int Width = 1600;
        internal const int Height = 900;
        internal const int Fps = 30;
        internal const float MaxSeconds = 240f; // hard stop, so a stuck run still writes a file

        /// <summary>
        /// Route through occupied cells, bow is +Z. Every consecutive pair must be orthogonally
        /// adjacent and occupied; <see cref="RouteIsValid"/> checks that before Play mode starts.
        /// Engineering (iz -3) forward through the hold, the wide midship row, the corridor, then
        /// the bridge (iz 4..5). Backtracks are deliberate: the side cells are dead ends.
        /// </summary>
        internal static readonly Vector2Int[] Route =
        {
            new Vector2Int(-1, -3), new Vector2Int(0, -3), new Vector2Int(1, -3),
            new Vector2Int(1, -2), new Vector2Int(0, -2), new Vector2Int(-1, -2),
            new Vector2Int(0, -2), new Vector2Int(0, -1), new Vector2Int(0, 0),
            new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3),
            new Vector2Int(-1, 3), new Vector2Int(0, 3), new Vector2Int(1, 3),
            new Vector2Int(0, 3), new Vector2Int(0, 4), new Vector2Int(-1, 4),
            new Vector2Int(-1, 5), new Vector2Int(0, 5), new Vector2Int(1, 5),
        };

        /// <summary>Occupied cells, copied from SceneBuilder.Ship's OccupiedIx / OccupiedIz.</summary>
        static readonly Vector2Int[] Occupied =
        {
            new Vector2Int(-1, 5), new Vector2Int(0, 5), new Vector2Int(1, 5),
            new Vector2Int(-1, 4), new Vector2Int(0, 4), new Vector2Int(1, 4),
            new Vector2Int(0, 3), new Vector2Int(-1, 3), new Vector2Int(1, 3),
            new Vector2Int(0, 2),
            new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(1, 1),
            new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-2, 0), new Vector2Int(2, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, -2), new Vector2Int(-1, -2), new Vector2Int(1, -2),
            new Vector2Int(-1, -3), new Vector2Int(0, -3), new Vector2Int(1, -3),
        };

        internal static string OutputPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputName));

        internal static Vector3 CellCentre(Vector2Int cell, float y)
        {
            return new Vector3((cell.x + 0.5f) * Grid, y, (cell.y + 0.5f) * Grid);
        }

        [MenuItem("GatekeepersProtocol/Record/Ship Flythrough", false, 22)]
        public static void Record()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[ShipFlythrough] Exit Play mode first.");
                return;
            }
            if (!RouteIsValid()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(ShipScene, OpenSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Play mode restarts the domain, so the driver is attached on the far side of
        /// the reload from a SessionState flag rather than held in a static field.</summary>
        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            if (!SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);

            var go = new GameObject("GKB_FlythroughDriver") { hideFlags = HideFlags.HideAndDontSave };
            go.AddComponent<ShipFlythroughDriver>();
        }

        /// <summary>The route is hand-written, so it is checked rather than trusted: every step
        /// must land on an occupied cell one grid square away.</summary>
        static bool RouteIsValid()
        {
            var occupied = new HashSet<Vector2Int>(Occupied);
            bool ok = true;

            for (int i = 0; i < Route.Length; i++)
            {
                if (!occupied.Contains(Route[i]))
                {
                    Debug.LogError($"[ShipFlythrough] Route step {i} {Route[i]} is not an occupied cell.");
                    ok = false;
                }
                if (i == 0) continue;
                Vector2Int step = Route[i] - Route[i - 1];
                if (Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1) continue;
                Debug.LogError($"[ShipFlythrough] Route step {i} {Route[i - 1]}->{Route[i]} is not orthogonally adjacent.");
                ok = false;
            }
            return ok;
        }
    }

    /// <summary>
    /// Runtime half of <see cref="ShipFlythrough"/>: walks the player rig along the route and
    /// encodes one video frame per rendered frame. Lives in the editor assembly, so it never
    /// ships in a player build and can only be attached by the menu row.
    /// </summary>
    public sealed class ShipFlythroughDriver : MonoBehaviour
    {
        FirstPersonController _fpc;
        CharacterController _cc;
        Camera[] _cameras;
        RenderTexture[] _previousTargets;
        RenderTexture _rt;
        Texture2D _frame;
        MediaEncoder _encoder;

        int _waypoint;
        int _frames;
        float _elapsed;
        float _stuckClock;
        float _lastDistance = float.MaxValue;
        bool _running;

        void Start()
        {
            _fpc = FindAnyObjectByType<FirstPersonController>();
            if (_fpc == null)
            {
                Debug.LogError("[ShipFlythrough] No FirstPersonController in the scene; nothing to walk.");
                Finish();
                return;
            }
            _cc = _fpc.GetComponent<CharacterController>();

            // The player keeps its own gravity and collision; only intent is taken over.
            _fpc.MovementLocked = true;
            _fpc.SetCursorLock(false);

            float y = _fpc.transform.position.y;
            Teleport(ShipFlythrough.CellCentre(ShipFlythrough.Route[0], y));
            _waypoint = 1;

            if (_fpc.cameraTransform != null)
                _fpc.cameraTransform.localRotation = Quaternion.identity;

            _rt = new RenderTexture(ShipFlythrough.Width, ShipFlythrough.Height, 24,
                RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _frame = new Texture2D(ShipFlythrough.Width, ShipFlythrough.Height, TextureFormat.RGBA32, false);
            RetargetCameras();

            var attrs = new VideoTrackAttributes
            {
                frameRate = new MediaRational(ShipFlythrough.Fps),
                width = ShipFlythrough.Width,
                height = ShipFlythrough.Height,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High,
            };
            _encoder = new MediaEncoder(ShipFlythrough.OutputPath, attrs);

            // Fixed capture step: every frame advances exactly 1/Fps of game time, however long
            // the encode takes, so the video plays back at the intended walking speed.
            Time.captureFramerate = ShipFlythrough.Fps;

            _running = true;
            StartCoroutine(CaptureLoop());
        }

        /// <summary>Every enabled camera renders into the shared target for the run and is put
        /// back afterwards, so weapon/overlay cameras land in the video in their own depth order.</summary>
        void RetargetCameras()
        {
            var live = new List<Camera>();
            foreach (Camera cam in Camera.allCameras)
                if (cam != null && cam.isActiveAndEnabled) live.Add(cam);

            _cameras = live.ToArray();
            _previousTargets = new RenderTexture[_cameras.Length];
            for (int i = 0; i < _cameras.Length; i++)
            {
                _previousTargets[i] = _cameras[i].targetTexture;
                _cameras[i].targetTexture = _rt;
            }
        }

        void RestoreCameras()
        {
            if (_cameras == null) return;
            for (int i = 0; i < _cameras.Length; i++)
                if (_cameras[i] != null) _cameras[i].targetTexture = _previousTargets[i];
            _cameras = null;
        }

        void Teleport(Vector3 position)
        {
            if (_cc != null) _cc.enabled = false;
            _fpc.transform.position = position;
            if (_cc != null) _cc.enabled = true;
        }

        void Update()
        {
            if (!_running) return;

            _elapsed += Time.deltaTime;
            if (_elapsed > ShipFlythrough.MaxSeconds)
            {
                Debug.LogWarning($"[ShipFlythrough] Hit the {ShipFlythrough.MaxSeconds}s cap at " +
                                 $"waypoint {_waypoint} of {ShipFlythrough.Route.Length}.");
                Finish();
                return;
            }

            if (_waypoint >= ShipFlythrough.Route.Length)
            {
                Finish();
                return;
            }

            Vector3 position = _fpc.transform.position;
            Vector3 target = ShipFlythrough.CellCentre(ShipFlythrough.Route[_waypoint], position.y);
            Vector3 delta = target - position;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (distance <= ShipFlythrough.ArriveRadius)
            {
                _waypoint++;
                _stuckClock = 0f;
                _lastDistance = float.MaxValue;
                return;
            }

            // Blocked by geometry rather than clipping through it: give it StuckSeconds, then
            // move on so one bad corner cannot eat the whole recording.
            if (distance < _lastDistance - 0.02f)
            {
                _lastDistance = distance;
                _stuckClock = 0f;
            }
            else
            {
                _stuckClock += Time.deltaTime;
                if (_stuckClock > ShipFlythrough.StuckSeconds)
                {
                    Debug.LogWarning($"[ShipFlythrough] Stuck short of waypoint {_waypoint} " +
                                     $"({distance:0.0}m out); skipping it.");
                    _waypoint++;
                    _stuckClock = 0f;
                    _lastDistance = float.MaxValue;
                    return;
                }
            }

            Vector3 direction = delta / distance;
            Quaternion want = Quaternion.LookRotation(direction, Vector3.up);
            _fpc.transform.rotation = Quaternion.RotateTowards(
                _fpc.transform.rotation, want, ShipFlythrough.TurnRate * Time.deltaTime);

            // Walk along the body's facing, not straight at the target, so the turn reads as a
            // turn instead of a strafe. FirstPersonController still applies gravity this frame.
            Vector3 forward = _fpc.transform.forward;
            forward.y = 0f;
            float alignment = Mathf.Clamp01(Vector3.Dot(forward.normalized, direction));
            if (_cc != null)
                _cc.Move(forward.normalized * (ShipFlythrough.WalkSpeed * alignment * Time.deltaTime));
        }

        IEnumerator CaptureLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (_running)
            {
                yield return endOfFrame;
                if (!_running) break;

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _rt;
                _frame.ReadPixels(new Rect(0, 0, ShipFlythrough.Width, ShipFlythrough.Height), 0, 0, false);
                _frame.Apply(false);
                RenderTexture.active = previous;

                try
                {
                    _encoder.AddFrame(_frame);
                    _frames++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[ShipFlythrough] Encode failed: " + e);
                    Finish();
                    yield break;
                }
            }
        }

        void Finish()
        {
            if (!_running && _encoder == null) return;
            _running = false;

            Time.captureFramerate = 0;
            RestoreCameras();

            if (_encoder != null)
            {
                _encoder.Dispose();
                _encoder = null;
                Debug.Log($"[ShipFlythrough] Wrote {ShipFlythrough.OutputPath} — {_frames} frames, " +
                          $"{_frames / (float)ShipFlythrough.Fps:0.0}s, reached waypoint " +
                          $"{_waypoint} of {ShipFlythrough.Route.Length}.");
            }

            if (_frame != null) Destroy(_frame);
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }
            _frame = null;
            _rt = null;

            EditorApplication.isPlaying = false;
        }

        void OnDisable()
        {
            // Manual exit from Play mode mid-run still closes the file rather than truncating it.
            if (_running || _encoder != null) Finish();
        }
    }
}
