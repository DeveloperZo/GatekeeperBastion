using Michsky.UI.Shift;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Binds Shift's <c>Splash Screen</c> branch. When a
    /// <see cref="SplashScreenManager"/> is present it owns the animation and this class only
    /// writes copy; the fades are the authored Animator's, not ours (M35 criterion 6).</summary>
    public sealed class SplashView : MonoBehaviour
    {
        const string FailMessage =
            "The Ship scene is not in the build settings — run GatekeeperBastion > Build All Scenes.";

        const string PressAnyKey = "press any key";

        const string SplashRoot = "Splash Screen";
        const string PressBlock = "Press Any Key";
        const string PressLabel = "Press Text";
        const string LogoName = "Logo";

        [SerializeField] SplashScreenManager _manager;
        [SerializeField] CanvasGroup _logoGroup;
        [SerializeField] TextMeshProUGUI _version;
        [SerializeField] TextMeshProUGUI _subtitle;
        [SerializeField] CanvasGroup _pressGroup;

        bool _resolved;

        public CanvasGroup LogoGroup => _logoGroup;
        public CanvasGroup PressGroup => _pressGroup;

        /// <summary>True when Shift's splash manager drives the sequence. On the boot Splash
        /// scene we strip that manager so this stays false and SplashController owns the prompt.</summary>
        public bool ShiftDriven => _manager != null;

        public void EnsureBuilt()
        {
            Resolve();
            if (_version != null)
            {
                _version.gameObject.SetActive(true);
                _version.text = Application.version;
            }

            UiBind.SetText(_subtitle, PressAnyKey);
            if (ShiftDriven) return;

            UiBind.Show(_subtitle, false);
            UiBind.Show(_pressGroup, false);
        }

        void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            if (_manager == null) _manager = UiBind.First<SplashScreenManager>();

            Transform splash = UiBind.Find(transform, SplashRoot);
            if (splash == null && _manager != null && _manager.splashScreen != null)
                splash = _manager.splashScreen.transform;
            if (splash == null) return;

            if (_logoGroup == null) _logoGroup = UiBind.Get<CanvasGroup>(splash, LogoName);
            if (_pressGroup == null) _pressGroup = UiBind.Get<CanvasGroup>(splash, PressBlock);
            if (_subtitle == null) _subtitle = UiBind.Get<TextMeshProUGUI>(splash, PressLabel);
        }

        public void SetPressVisible(bool visible)
        {
            if (ShiftDriven) return;
            UiBind.Show(_pressGroup, visible);
            UiBind.Show(_subtitle, visible);
        }

        public void SetSubtitle(string text) => UiBind.SetText(_subtitle, text);

        public void SetFailed(bool failed)
        {
            SetPressVisible(true);
            SetSubtitle(failed ? FailMessage : PressAnyKey);
        }
    }
}
