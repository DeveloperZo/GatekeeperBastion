using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Splash presentation — setters only; hierarchy from authored Shift Canvas variant.</summary>
    public sealed class SplashView : MonoBehaviour
    {
        const string FailMessage =
            "MainMenu scene is not in the build settings — run GatekeeperBastion > Build All Scenes.";

        const string PressAnyKey = "press any key";

        [SerializeField] CanvasGroup _logoGroup;
        [SerializeField] TextMeshProUGUI _version;
        [SerializeField] TextMeshProUGUI _subtitle;
        [SerializeField] CanvasGroup _pressGroup;

        public CanvasGroup LogoGroup => _logoGroup;
        public CanvasGroup PressGroup => _pressGroup;

        /// <summary>authored prefab required — activates serialized children only.</summary>
        public void EnsureBuilt()
        {
            if (_version != null)
            {
                _version.gameObject.SetActive(true);
                _version.text = Application.version;
            }
            if (_subtitle != null)
            {
                _subtitle.gameObject.SetActive(true);
                if (string.IsNullOrEmpty(_subtitle.text))
                    _subtitle.text = PressAnyKey;
            }
            if (_pressGroup != null) _pressGroup.gameObject.SetActive(false);
        }

        public void SetPressVisible(bool visible)
        {
            if (_pressGroup != null) _pressGroup.gameObject.SetActive(visible);
            else if (_subtitle != null) _subtitle.gameObject.SetActive(visible);
        }

        public void SetSubtitle(string text)
        {
            if (_subtitle != null) _subtitle.text = text;
        }

        public void SetFailed(bool failed)
        {
            SetPressVisible(true);
            SetSubtitle(failed ? FailMessage : PressAnyKey);
        }
    }
}
