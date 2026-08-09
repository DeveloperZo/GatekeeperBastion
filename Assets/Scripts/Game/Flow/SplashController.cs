using GatekeeperBastion.Game.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: boot splash — logo fade, load-gated press-any-key, async MainMenu.</summary>
    public sealed class SplashController : MonoBehaviour
    {
        const float LogoFadeSeconds = 0.8f;
        const float BreathPeriod = 1.2f;

        SplashView _view;
        AsyncOperation _load;
        bool _failed;
        bool _skip;
        bool _activated;
        float _logoT;

        void Awake()
        {
            AudioBuses.ApplyFromSettings();
            _view = GetComponentInChildren<SplashView>(true);
            _view?.EnsureBuilt();
            _view?.SetPressVisible(false);
            if (_view != null && _view.LogoGroup != null)
                _view.LogoGroup.alpha = 0f;

            BeginAsyncMainMenu();
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void BeginAsyncMainMenu()
        {
            if (!SceneFlow.CanLoad(SceneFlow.MainMenu))
            {
                _failed = true;
                _view?.SetFailed(true);
                return;
            }

            _load = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            if (_load == null)
            {
                _failed = true;
                _view?.SetFailed(true);
                return;
            }
            _load.allowSceneActivation = false;
        }

        void Update()
        {
            if (_failed || _activated) return;

            if (Input.anyKeyDown) _skip = true;

            TickLogoFade();
            bool loadReady = _load != null && _load.progress >= 0.9f;
            if (loadReady) _view?.SetPressVisible(true);
            TickBreath(loadReady);

            if (!loadReady) return;
            if (!_skip && !Input.anyKeyDown) return;

            _activated = true;
            _load.allowSceneActivation = true;
        }

        void TickLogoFade()
        {
            if (_view == null || _view.LogoGroup == null) return;
            if (_skip && _view.LogoGroup.alpha < 1f)
            {
                _view.LogoGroup.alpha = 1f;
                return;
            }
            _logoT += Time.unscaledDeltaTime;
            _view.LogoGroup.alpha = Mathf.Clamp01(_logoT / LogoFadeSeconds);
        }

        void TickBreath(bool visible)
        {
            if (!visible || _view == null || _view.PressGroup == null) return;
            float wave = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / BreathPeriod)));
            _view.PressGroup.alpha = wave;
        }
    }
}
