using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>
    /// Records the game view while you play, and stops on Insert. Editor-only: the capture component
    /// lives in this assembly, so nothing here can reach a player build.
    ///
    /// Capture is ScreenCapture.CaptureScreenshotIntoRenderTexture, which takes the composited
    /// backbuffer — the HUD and every overlay canvas are in the video, and the game view keeps
    /// rendering normally, so the recording does not change what you see while playing.
    /// Encoding is UnityEditor.Media.MediaEncoder (editor built-in, no package).
    ///
    /// Game time is left alone (no Time.captureFramerate): frames are emitted against the wall
    /// clock at <see cref="Fps"/>, duplicating the last frame through a hitch and dropping
    /// surplus frames above it, so the video runs at real speed rather than in slow motion.
    /// Output lands at the repository root as GameplayCapture_yyyyMMdd_HHmmss.mp4.
    ///
    /// The frame is encoded at the Game view's own size, never rescaled. PixelRender draws the
    /// game at 360 lines and point-samples it up, so any resample here turns authored pixels
    /// into uneven smears — the first version squeezed every view to 1600x900 through a
    /// bilinear blit and handed that to a Constrained Baseline encoder at ~5 Mbps, which is
    /// what "horrible quality" was. Set the Game view to 1920x1080 for a clip worth sharing.
    /// </summary>
    public static class GameplayRecorder
    {
        /// <summary>Insert: every F-key, Home and End belong to DevCommands (F9 is the gatekeeper
        /// readout since M13), and Insert is bound nowhere in Controls or the dev console, so
        /// stopping a capture cannot also fire a cheat.</summary>
        internal const KeyCode StopKey = KeyCode.Insert;
        internal const int Fps = 30;
        internal const float MaxMinutes = 20f;

        /// <summary>Bits per pixel per frame the encoder is asked for. 0.3 puts 1080p30 near
        /// 19 Mbps, which keeps flat dark rooms and hard pixel edges free of macroblocks;
        /// the floor covers small Game views, where the per-pixel figure gets stingy.</summary>
        internal const float BitsPerPixelPerFrame = 0.3f;
        internal const uint MinBitRate = 8_000_000;

        /// <summary>Bit rate for a frame of this size at <see cref="Fps"/>.</summary>
        internal static uint TargetBitRate(int width, int height)
        {
            double bits = (double)width * height * Fps * BitsPerPixelPerFrame;
            return (uint)Math.Max(MinBitRate, Math.Min(uint.MaxValue, bits));
        }

        /// <summary>H.264 wants even dimensions; an odd Game view loses its last row or column
        /// rather than being resampled.</summary>
        internal static int Even(int size) => Math.Max(2, size & ~1);

        const string ShipScene = "Assets/Scenes/Ship - CoOp.unity";
        const string BastionScene = "Assets/Scenes/Bastion_Coop.unity";
        const string PendingKey = "GKB.GameplayRecorder.Pending";

        /// <summary>ScreenCapture writes in native backbuffer order, which is upside down against
        /// Unity's texture convention on D3D. Flip it back; set false if a capture comes out
        /// inverted on another graphics API.</summary>
        internal const bool FlipVertically = true;

        internal static string NewOutputPath()
        {
            string name = "GameplayCapture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp4";
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", name));
        }

        /// <summary>Starts recording in the current Play session, or enters Play on a chosen
        /// scene when the editor is stopped.</summary>
        [MenuItem("GatekeepersProtocol/Record/Gameplay/This scene (Insert to stop)", false, 10)]
        public static void RecordThisScene()
        {
            Record(scenePath: null);
        }

        [MenuItem("GatekeepersProtocol/Record/Gameplay/Ship", false, 11)]
        public static void RecordShip()
        {
            Record(ShipScene);
        }

        [MenuItem("GatekeepersProtocol/Record/Gameplay/Bastion", false, 12)]
        public static void RecordBastion()
        {
            Record(BastionScene);
        }

        [MenuItem("GatekeepersProtocol/Record/Gameplay/This scene (Insert to stop)", true)]
        [MenuItem("GatekeepersProtocol/Record/Gameplay/Ship", true)]
        [MenuItem("GatekeepersProtocol/Record/Gameplay/Bastion", true)]
        static bool RecordValidate()
        {
            return GameplayRecorderDriver.Active == null;
        }

        static void Record(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                Attach();
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (scenePath != null)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogError("[GameplayRecorder] No loaded scene to record.");
                    return;
                }
            }

            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            // The driver is HideAndDontSave and the project enters Play without a domain
            // reload, so nothing destroys it on the way out and its OnDisable never ran: a
            // Stop-button exit left an mp4 with no moov atom (seen 2026-09-03). Close it here.
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                if (GameplayRecorderDriver.Active != null) GameplayRecorderDriver.Active.StopRecording();
                return;
            }
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            if (!SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            Attach();
        }

        static void Attach()
        {
            if (GameplayRecorderDriver.Active != null) return;
            var go = new GameObject("GKB_GameplayRecorder") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<GameplayRecorderDriver>();
        }
    }

    /// <summary>Runtime half of <see cref="GameplayRecorder"/>. Survives scene loads, so a
    /// recording started in one scene keeps running through later loads.</summary>
    public sealed class GameplayRecorderDriver : MonoBehaviour
    {
        public static GameplayRecorderDriver Active { get; private set; }

        RenderTexture _screen;   // backbuffer copy, screen sized
        RenderTexture _encode;   // even-sized, flipped, what the encoder reads
        Texture2D _frame;
        int _width;
        int _height;
        MediaEncoder _encoder;
        string _path;

        int _screenWidth;
        int _screenHeight;
        int _frames;
        double _startTime;
        double _nextFrameTime;
        bool _running;

        void Awake()
        {
            Active = this;
        }

        void Start()
        {
            _screenWidth = Mathf.Max(2, Screen.width);
            _screenHeight = Mathf.Max(2, Screen.height);

            _width = GameplayRecorder.Even(_screenWidth);
            _height = GameplayRecorder.Even(_screenHeight);

            _screen = new RenderTexture(_screenWidth, _screenHeight, 0, RenderTextureFormat.ARGB32);
            // Point: the blit is 1:1 (or one pixel short on an odd view), and if a driver ever
            // does resample, point keeps the pixel-art edges hard instead of averaging them.
            _screen.filterMode = FilterMode.Point;
            _encode = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32);
            _frame = new Texture2D(_width, _height, TextureFormat.RGBA32, false);

            _path = GameplayRecorder.NewOutputPath();
            uint bitRate = GameplayRecorder.TargetBitRate(_width, _height);
            var h264 = new H264EncoderAttributes
            {
                gopSize = GameplayRecorder.Fps,   // a keyframe a second: seekable, cheap
                numConsecutiveBFrames = 0,        // widest encoder support; the bit rate does the work
                profile = VideoEncodingProfile.H264High,
            };
            var attrs = new VideoTrackEncoderAttributes(h264)
            {
                frameRate = new MediaRational(GameplayRecorder.Fps),
                width = (uint)_width,
                height = (uint)_height,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High,
                targetBitRate = bitRate,
            };

            try
            {
                _encoder = new MediaEncoder(_path, attrs);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameplayRecorder] Could not open the encoder: " + e);
                Stop();
                return;
            }

            _startTime = Time.realtimeSinceStartupAsDouble;
            _nextFrameTime = _startTime;
            _running = true;
            Debug.Log($"[GameplayRecorder] Recording to {Path.GetFileName(_path)} at " +
                      $"{_width}x{_height}, H.264 High, {bitRate / 1_000_000f:0.#} Mbps — " +
                      $"{GameplayRecorder.StopKey} to stop.");
            StartCoroutine(CaptureLoop());
        }

        void Update()
        {
            if (!_running) return;

            // Editor-only tool, so this reads UnityEngine.Input directly rather than going through
            // Controls: the stop key is not a player binding and must work while the game has the
            // input frame locked (menus, panels, death screen).
            if (Input.GetKeyDown(GameplayRecorder.StopKey))
            {
                Stop();
                return;
            }

            if (Time.realtimeSinceStartupAsDouble - _startTime > GameplayRecorder.MaxMinutes * 60.0)
            {
                Debug.LogWarning($"[GameplayRecorder] Hit the {GameplayRecorder.MaxMinutes}-minute cap.");
                Stop();
            }
        }

        IEnumerator CaptureLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (_running)
            {
                yield return endOfFrame;
                if (!_running) break;

                if (Screen.width != _screenWidth || Screen.height != _screenHeight)
                {
                    Debug.LogWarning("[GameplayRecorder] Game view resized mid-recording; stopping " +
                                     "so the file stays valid.");
                    Stop();
                    yield break;
                }

                double now = Time.realtimeSinceStartupAsDouble;
                if (now + 0.0001 < _nextFrameTime) continue; // running above Fps: drop this one

                ScreenCapture.CaptureScreenshotIntoRenderTexture(_screen);
                Blit();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _encode;
                _frame.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
                _frame.Apply(false);
                RenderTexture.active = previous;

                // One emit per elapsed 1/Fps slot: a hitch repeats this frame rather than
                // compressing wall-clock time into fewer video frames.
                int emitted = 0;
                try
                {
                    while (now + 0.0001 >= _nextFrameTime && emitted < GameplayRecorder.Fps)
                    {
                        _encoder.AddFrame(_frame);
                        _frames++;
                        emitted++;
                        _nextFrameTime += 1.0 / GameplayRecorder.Fps;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[GameplayRecorder] Encode failed: " + e);
                    Stop();
                    yield break;
                }
            }
        }

        /// <summary>Screen copy into the fixed encode size, flipped if the backbuffer order needs it.</summary>
        void Blit()
        {
            if (GameplayRecorder.FlipVertically)
                Graphics.Blit(_screen, _encode, new Vector2(1f, -1f), new Vector2(0f, 1f));
            else
                Graphics.Blit(_screen, _encode);
        }

        /// <summary>Editor-side stop: the play-mode hook calls this on ExitingPlayMode.</summary>
        public void StopRecording() => Stop();

        void Stop()
        {
            if (!_running && _encoder == null)
            {
                Cleanup();
                return;
            }
            _running = false;

            if (_encoder != null)
            {
                _encoder.Dispose();
                _encoder = null;
                Debug.Log($"[GameplayRecorder] Wrote {_path} — {_frames} frames, " +
                          $"{_frames / (float)GameplayRecorder.Fps:0.0}s.");
            }
            Cleanup();
        }

        void Cleanup()
        {
            if (_frame != null) Destroy(_frame);
            if (_screen != null) { _screen.Release(); Destroy(_screen); }
            if (_encode != null) { _encode.Release(); Destroy(_encode); }
            _frame = null;
            _screen = null;
            _encode = null;

            if (this != null && gameObject != null) Destroy(gameObject);
        }

        void OnDisable()
        {
            // Leaving Play mode mid-recording still closes the file instead of truncating it.
            if (_running || _encoder != null) Stop();
            if (Active == this) Active = null;
        }
    }
}
