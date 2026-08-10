using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: the boot splash (M35 criteria 1-2). Fades the title card in,
    /// holds, fades out, and lands in MainMenu with no input required — total under 4 s.
    /// Any key or click after the first displayed frame skips straight to MainMenu.
    /// MainMenu is async-loaded behind the card so the skip is instant.</summary>
    public sealed class SplashController : MonoBehaviour
    {
        const string Tag = "Splash";
        // 0.4 in + 1.6 hold + 0.5 out = 2.5 s hands-off, inside criterion 1's 4 s budget
        // (fade-in also inside its 0.5 s cap).
        const float FadeInSeconds = 0.4f;
        const float HoldSeconds = 1.6f;
        const float FadeOutSeconds = 0.5f;
        /// <summary>Unity parks async progress at 0.9 while allowSceneActivation is false.</summary>
        const float AsyncReadyProgress = 0.9f;

        [SerializeField] CanvasGroup _fadeGroup;

        AsyncOperation _load;
        bool _activated;
        bool _failed;
        float _elapsed;
        int _framesSeen;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_fadeGroup == null)
                _fadeGroup = GetComponentInChildren<CanvasGroup>(true);
            if (_fadeGroup != null) _fadeGroup.alpha = 0f;

            if (!SceneFlow.CanLoad(SceneFlow.MainMenu))
            {
                _failed = true;
                Log.Error(Tag, $"{SceneFlow.MainMenu} is not in the build settings — " +
                               "run GatekeeperBastion > Build All Scenes.");
                return;
            }

            _load = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            if (_load == null)
            {
                _failed = true;
                Log.Error(Tag, $"LoadSceneAsync({SceneFlow.MainMenu}) returned null.");
                return;
            }
            _load.allowSceneActivation = false;
        }

        void Update()
        {
            if (_activated) return;

            _elapsed += Time.unscaledDeltaTime;
            bool skip = _framesSeen > 0 && ReadAnyInput();
            _framesSeen++;

            if (_fadeGroup != null)
                _fadeGroup.alpha = AlphaAt(_elapsed);

            if (skip || _elapsed >= FadeInSeconds + HoldSeconds + FadeOutSeconds)
                Advance();
        }

        /// <summary>Fade in, hold, fade out — alpha over unscaled seconds since boot.</summary>
        static float AlphaAt(float t)
        {
            if (t < FadeInSeconds) return Mathf.Clamp01(t / FadeInSeconds);
            t -= FadeInSeconds;
            if (t < HoldSeconds) return 1f;
            t -= HoldSeconds;
            return Mathf.Clamp01(1f - t / FadeOutSeconds);
        }

        static bool ReadAnyInput() =>
            Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

        /// <summary>The any-key skip path, callable by tests (Input cannot be injected):
        /// activates the parked MainMenu load immediately.</summary>
        public void Skip() => Advance();

        void Advance()
        {
            if (_activated) return;
            if (_load != null)
            {
                // allowSceneActivation completes the load whenever it is ready; no wait here.
                _activated = true;
                _load.allowSceneActivation = true;
                return;
            }
            // Failed Awake path: retry the sync load so a fixed build-settings state recovers.
            if (SceneFlow.LoadMainMenu())
                _activated = true;
            else if (!_failed)
            {
                _failed = true;
                Log.Error(Tag, "Could not load MainMenu from the splash.");
            }
        }
    }
}
