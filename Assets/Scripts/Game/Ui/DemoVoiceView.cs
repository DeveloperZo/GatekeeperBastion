using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the demo mission line surface — a Shift-family Lock box
    /// (speaker label, body copy, one Continue control, no reply row) and a bottom-centre
    /// subtitle lane. One authored instance per scene (Ship HUD and Bastion HUD canvases);
    /// it ticks the engine-free <see cref="DemoVoice"/> runner and owns the Lock-mode
    /// cursor (freed so the gun and Interactor refuse — that is the fire/interact eat).
    /// Never writes Time.timeScale. Unbound wires render mute; the runner still logs.</summary>
    public sealed class DemoVoiceView : MonoBehaviour
    {
        [SerializeField, OptionalBinding, Tooltip("Lock box root. Inactive while no Lock line is live.")]
        GameObject _boxRoot;

        [SerializeField, OptionalBinding, Tooltip("Speaker label inside the Lock box (GAME).")]
        TMP_Text _speakerText;

        [SerializeField, OptionalBinding, Tooltip("Body copy inside the Lock box.")]
        TMP_Text _bodyText;

        [SerializeField, OptionalBinding, Tooltip("Continue control — a button covering the box. Space/Enter also advance.")]
        Button _continueButton;

        [SerializeField, OptionalBinding, Tooltip("Subtitle lane root. Inactive while no Subtitle line is live.")]
        GameObject _subtitleRoot;

        [SerializeField, OptionalBinding, Tooltip("Subtitle text, bottom-centre.")]
        TMP_Text _subtitleText;

        string _shownId;
        bool _shownLock;
        bool _cursorWasLocked;
        bool _warnedUnbound;

        void Awake()
        {
            DemoVoice.ViewAttached();
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
                _continueButton.onClick.AddListener(OnContinueClicked);
            }
            UiBind.Show(_boxRoot, false);
            UiBind.Show(_subtitleRoot, false);
        }

        void OnDestroy() => DemoVoice.ViewDetached();

        void Update()
        {
            DemoVoice.Tick(Time.deltaTime);

            // Space/Enter advance a Lock line. Esc is NOT handled here — the scene's Esc
            // dispatcher owns it (Escape contract) and calls DemoVoice.TryHandleEscape first,
            // so one press cannot both end the line and open pause.
            if (!DemoVoice.LocksInput) return;
            var frame = Controls.Sample();
            if (frame.confirmDown && !frame.escapeDown)
                DemoVoice.Continue();
        }

        void LateUpdate()
        {
            bool live = DemoVoice.IsLive;
            bool lockLive = DemoVoice.LocksInput;

            // Lock mode frees the cursor every frame it is live: the box is clickable, and a
            // free cursor is exactly what makes the gun and Interactor refuse. ShipController
            // and FirstPersonController both yield their cursor writes while LocksInput.
            if (lockLive && Cursor.lockState == CursorLockMode.Locked)
            {
                _cursorWasLocked = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            // ...and give it back when the line ends. Without this the next click was spent
            // re-capturing instead of firing — "Fire refused: the cursor is not captured",
            // 11 times in the 2026-08-25 playtest, every one of them mid-fight.
            else if (!lockLive && _cursorWasLocked)
            {
                _cursorWasLocked = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            string id = DemoVoice.CurrentId;
            if (id == _shownId && lockLive == _shownLock) return;
            _shownId = id;
            _shownLock = lockLive;

            if (!live)
            {
                UiBind.Show(_boxRoot, false);
                UiBind.Show(_subtitleRoot, false);
                return;
            }

            if (_boxRoot == null && _subtitleRoot == null && !_warnedUnbound)
            {
                _warnedUnbound = true;
                Log.Warn("Demo",
                    "DemoVoiceView has no authored box or subtitle wire — lines run mute.");
            }

            if (lockLive)
            {
                UiBind.Show(_subtitleRoot, false);
                UiBind.Show(_boxRoot, true);
                string speaker = DemoVoice.CurrentSpeaker;
                UiBind.SetText(_speakerText, speaker != null ? speaker.ToUpperInvariant() : "");
                UiBind.SetText(_bodyText, DemoVoice.CurrentText);
            }
            else
            {
                UiBind.Show(_boxRoot, false);
                UiBind.Show(_subtitleRoot, true);
                UiBind.SetText(_subtitleText, DemoVoice.CurrentText);
            }
        }

        void OnContinueClicked() => DemoVoice.Continue();
    }
}
