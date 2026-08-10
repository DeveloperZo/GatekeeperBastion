using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: boot splash — logo fade, load-gated press-any-key, async MainMenu.
    /// Any input arms skip; activation waits until the async load reaches 0.9 (Unity's
    /// allowSceneActivation gate). A soft timeout forces activation of the pending async op
    /// so a stalled gate cannot soft-lock boot; sync LoadMainMenu is only used when async
    /// never started.</summary>
    public sealed class SplashController : MonoBehaviour
    {
        const string Tag = "Splash";
        const float LogoFadeSeconds = 0.8f;
        const float BreathPeriod = 1.2f;
        /// <summary>Criterion 15: boot to menu under 10s.</summary>
        const float LoadTimeoutSeconds = 8f;
        /// <summary>Unity stops async progress at 0.9 while allowSceneActivation is false.</summary>
        const float AsyncReadyProgress = 0.9f;

        SplashView _view;
        AsyncOperation _load;
        bool _failed;
        bool _skip;
        bool _activated;
        float _logoT;
        float _elapsed;

        void Awake()
        {
            SceneFlow.EnsureEventSystem();
            SceneFlow.RepairCanvas(gameObject);
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
            // M35 moved the menu Canvas into Ship; FrontEndScene resolves to whichever of the
            // two is actually registered, so this scene cannot strand on a deregistered MainMenu.
            string target = SceneFlow.FrontEndScene;
            if (!SceneFlow.CanLoad(target))
            {
                FailLoad($"{target} is not in the build settings");
                return;
            }

            _load = SceneManager.LoadSceneAsync(target);
            if (_load == null)
            {
                FailLoad($"LoadSceneAsync({target}) returned null");
                return;
            }
            _load.allowSceneActivation = false;
        }

        void FailLoad(string reason)
        {
            _failed = true;
            Log.Error(Tag, reason + " — run GatekeeperBastion > Build All Scenes.");
            _view?.SetFailed(true);
        }

        void Update()
        {
            if (_activated) return;

            _elapsed += Time.unscaledDeltaTime;
            if (ReadAdvanceInput()) _skip = true;

            TickLogoFade();

            if (_failed)
            {
                // Failed begin (_load null): any input retries via SceneFlow sync load.
                if (_skip) ActivateSyncFallback();
                return;
            }

            bool loadReady = _load != null && _load.progress >= AsyncReadyProgress - 0.001f;
            if (loadReady)
            {
                _view?.SetSubtitle("press any key");
                _view?.SetPressVisible(true);
                TickBreath(true);
            }
            else if (_skip)
            {
                _view?.SetSubtitle("loading…");
                _view?.SetPressVisible(true);
            }

            if (loadReady && _skip)
            {
                ActivateAsync();
                return;
            }

            // Absolute boot budget — also covers "load ready but Input never fires"
            // (Editor Game-view focus / dual-EventSystem log storms).
            if (_elapsed >= LoadTimeoutSeconds)
            {
                Log.Warn(Tag,
                    $"Boot budget {_elapsed:0.0}s reached (loadReady={loadReady}, progress={(_load != null ? _load.progress : -1f):0.###}) — activating MainMenu.");
                if (_load != null)
                {
                    _activated = true;
                    _load.allowSceneActivation = true;
                }
                else
                    ActivateSyncFallback();
            }
        }

        static bool ReadAdvanceInput()
        {
            if (Input.anyKeyDown) return true;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
            return false;
        }

        void ActivateAsync()
        {
            if (_activated || _load == null) return;
            _activated = true;
            _load.allowSceneActivation = true;
        }

        void ActivateSyncFallback()
        {
            if (_activated) return;
            _activated = true;
            // A held LoadSceneAsync with allowSceneActivation=false stalls the load queue.
            // Prefer finishing that op over a second sync LoadScene of the same target.
            if (_load != null)
            {
                _load.allowSceneActivation = true;
                return;
            }
            if (!SceneFlow.LoadMainMenu())
            {
                _failed = true;
                _activated = false;
                _view?.SetFailed(true);
            }
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
