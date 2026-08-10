using GatekeeperBastion.Util;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: boot splash. Fades the authored canvas, CaptureOrJoin
    /// music, waits for any key/click, loads MainMenu. No auto-advance.
    /// Scene owns look (backdrop, motes, title, prompt) — this script only drives flow.</summary>
    public sealed class SplashController : MonoBehaviour
    {
        const string Tag = "Splash";
        const float FadeInSeconds = 0.4f;
        const float InputArmSeconds = 0.35f;
        const float MaxStepSeconds = 1f / 30f;

        [SerializeField] CanvasGroup _fadeGroup;
        [SerializeField] AudioSource _music;
        [SerializeField] TextMeshProUGUI _prompt;

        bool _activated;
        bool _failed;
        float _elapsed;
        Color _promptBase;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_fadeGroup != null)
                _fadeGroup.alpha = 0f;

            if (_prompt != null)
            {
                _promptBase = _prompt.color;
                var c = _promptBase;
                c.a = 0f;
                _prompt.color = c;
            }

            FrontEndMusic.CaptureOrJoin(_music);
            Log.Event(Tag, "waiting for any key/click");
        }

        void Update()
        {
            if (_activated) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
            _elapsed += dt;
            float alpha = Mathf.Clamp01(_elapsed / FadeInSeconds);
            if (_fadeGroup != null)
                _fadeGroup.alpha = alpha;

            if (_prompt != null && _elapsed >= FadeInSeconds)
            {
                float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
                var c = _promptBase;
                c.a = _promptBase.a * pulse;
                if (_promptBase.a < 0.01f) c.a = pulse;
                _prompt.color = c;
            }

            if (_elapsed >= InputArmSeconds && ReadAnyInput())
            {
                Log.Event(Tag, "continued by input");
                Advance();
            }
        }

        static bool ReadAnyInput() =>
            Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

        /// <summary>PlayMode tests — same path as press-any-key.</summary>
        public void Skip() => Advance();

        void Advance()
        {
            if (_activated) return;
            _activated = true;
            // Music keeps playing via FrontEndMusic (DontDestroyOnLoad).
            Log.Event(Tag, "advancing to MainMenu");
            if (SceneFlow.LoadMainMenu()) return;
            if (SceneFlow.CanLoad(SceneFlow.MainMenu))
            {
                SceneManager.LoadScene(SceneFlow.MainMenu);
                return;
            }
            if (!_failed)
            {
                _failed = true;
                Log.Error(Tag, "Could not load MainMenu from the splash.");
            }
        }
    }
}
