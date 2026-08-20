using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the wake teleporter — black hold, fade-up, the stir line, the
    /// first hold-interact of the game (release, 1.0 s), and the glass retract. The player
    /// materialises upright on the pad inside a glass cylinder, and the glass dropping is
    /// what makes Rooted legible. DemoShipDirector decides whether a load starts sealed,
    /// open-on-deck, or dormant; this component only plays the sequence it is told to. The
    /// fade image is authored on the Ship HUD canvas and driven here (no new light or
    /// camera API — A2/A6).</summary>
    public sealed class ShipTeleporter : MonoBehaviour, IInteractable
    {
        const string Tag = "Demo";
        const float BlackHoldSeconds = 0.4f;
        const float FadeSeconds = 1.5f;
        const float UnlockHoldSeconds = 1.0f;
        const float GlassRetractSeconds = 0.8f;
        // Full height of the graybox glass plus the pad, so the cylinder clears the deck
        // instead of stopping halfway. The lid only ever needed 1.1 because it was a slab.
        const float GlassRetractMetres = 2.5f;

        [SerializeField, OptionalBinding, Tooltip("Full-screen black Image on the Ship HUD canvas. Driven only while the wake sequence runs.")]
        Image _fadeImage;

        [SerializeField, OptionalBinding, Tooltip("Glass cylinder; retracts down into the pad on release. Carries NO collider — the player stands inside its footprint.")]
        Transform _glass;

        [SerializeField, OptionalBinding, Tooltip("Standing pose on the pad — the player is placed here while sealed.")]
        Transform _padPose;

        enum Phase { Dormant, Black, Fade, Stir, Sealed, Opening, Done }

        Phase _phase = Phase.Dormant;
        float _timer;
        float _hold;
        int _holdFrame = -1;
        Vector3 _glassClosedPos;
        bool _glassPosCaptured;
        FirstPersonController _fpc;

        public string Prompt
        {
            get
            {
                if (_phase != Phase.Sealed) return null;
                if (Time.frameCount - _holdFrame > 1) _hold = 0f;
                return _hold > 0f ? "[E] releasing" : "[E] release";
            }
        }

        /// <summary>Fresh wake: player onto the pad, look free, move rooted, screen black.
        /// Nothing is switched to trigger here. A pad below and an emitter above share no
        /// footprint with a standing rig, so their solid colliders cannot depenetrate the
        /// CharacterController — which is why the bed's session-long trigger flip (never
        /// restored) is gone rather than managed. The glass carries no collider at all: the
        /// player is Rooted for its entire closed life, so it has nothing to block.</summary>
        public void BeginSealed(FirstPersonController fpc)
        {
            _fpc = fpc;
            CaptureGlass();
            PlaceOnPad();
            if (_fpc != null) _fpc.Rooted = true;
            if (_fadeImage != null)
            {
                _fadeImage.gameObject.SetActive(true);
                SetFadeAlpha(1f);
            }
            _phase = Phase.Black;
            _timer = 0f;
            DemoShipFight.WakeHidesCrosshair = true;
            Log.Event(Tag, "spawn teleporter");
        }

        /// <summary>Reload after the release: glass down, nothing to play. The teleporter
        /// is furniture from here on (no reseal — beat 0 reload matrix).</summary>
        public void BeginOpen()
        {
            CaptureGlass();
            SetGlassOpen();
            _phase = Phase.Done;
            DemoShipFight.WakeHidesCrosshair = false;
        }

        /// <summary>Demo off / later beats: open furniture, no interactions, no fade.</summary>
        public void BeginDormant()
        {
            CaptureGlass();
            SetGlassOpen();
            _phase = Phase.Dormant;
            DemoShipFight.WakeHidesCrosshair = false;
        }

        /// <summary>Held E while sealed (via Interactor). Release resets the hold.</summary>
        public void Interact(float dt)
        {
            if (_phase != Phase.Sealed) return;
            if (Time.frameCount - _holdFrame > 1) _hold = 0f;
            _holdFrame = Time.frameCount;
            _hold += dt;
            if (_hold < UnlockHoldSeconds) return;

            _hold = 0f;
            var m = GameSession.Meta;
            if (m != null)
            {
                m.DemoCapsuleOpened = true;
                GameSession.Save();
            }
            Log.Event(Tag, "teleporter released");
            _phase = Phase.Opening;
            _timer = 0f;
        }

        void Update()
        {
            switch (_phase)
            {
                case Phase.Black:
                    _timer += Time.deltaTime;
                    if (_timer >= BlackHoldSeconds)
                    {
                        _phase = Phase.Fade;
                        _timer = 0f;
                    }
                    break;

                case Phase.Fade:
                    _timer += Time.deltaTime;
                    SetFadeAlpha(1f - Mathf.Clamp01(_timer / FadeSeconds));
                    if (_timer >= FadeSeconds)
                    {
                        SetFadeAlpha(0f);
                        if (_fadeImage != null) _fadeImage.gameObject.SetActive(false);
                        _phase = Phase.Stir;
                        DemoVoice.Play(DemoConversationIds.WakeStir, DemoVoiceMode.Lock);
                    }
                    break;

                case Phase.Stir:
                    // Prompt goes live when the stir line is dismissed — text before verb.
                    if (!DemoVoice.IsLive)
                    {
                        _phase = Phase.Sealed;
                        DemoShipFight.SetStanding("Release the teleporter");
                    }
                    break;

                case Phase.Opening:
                    _timer += Time.deltaTime;
                    if (_glass != null)
                        _glass.localPosition = _glassClosedPos
                            + Vector3.down * (GlassRetractMetres * Mathf.Clamp01(_timer / GlassRetractSeconds));
                    if (_timer >= GlassRetractSeconds)
                    {
                        _phase = Phase.Done;
                        DemoShipFight.WakeHidesCrosshair = false;
                        if (_fpc != null)
                        {
                            // Unroot only. The sealed pose starts level, so there is nothing
                            // to correct, and correcting anyway would yank the view of a
                            // player who looked around during the stir.
                            _fpc.Rooted = false;
                        }
                        DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
                        DemoShipFight.SetStanding("Repair the NAV MAP");
                    }
                    break;
            }
        }

        void CaptureGlass()
        {
            if (_glassPosCaptured || _glass == null) return;
            _glassPosCaptured = true;
            _glassClosedPos = _glass.localPosition;
        }

        void SetGlassOpen()
        {
            if (_glass == null) return;
            _glass.localPosition = _glassClosedPos + Vector3.down * GlassRetractMetres;
        }

        void PlaceOnPad()
        {
            var player = PlayerRegistry.Local;
            if (player == null || _padPose == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            // XZ from the pose; Y stays the rig's own. The authored rig height is the one
            // place its pivot-to-feet offset is correct — a marker Y below it starts the
            // CharacterController inside the deck and the player falls through the floor
            // on the first physics step (2026-08-18 defect).
            Vector3 at = _padPose.position;
            at.y = player.transform.position.y;
            // YAW ONLY on the body. FirstPersonController keeps yaw on the body and pitch
            // on the camera; a marker rotation with pitch tilts the body's yaw axis and
            // every later mouse look comes out skewed (the wake orientation defect,
            // 2026-08-18). Pitch is not written at all — see below.
            float yaw = _padPose.rotation.eulerAngles.y;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
            // No pitch write. FirstPersonController's _pitch starts at 0, which is the level
            // materialise pose; the bed needed 50 degrees down only because the eye sat
            // above a lid it had to look at.
        }

        void SetFadeAlpha(float a)
        {
            if (_fadeImage == null) return;
            var c = _fadeImage.color;
            if (Mathf.Approximately(c.a, a)) return;
            _fadeImage.color = new Color(c.r, c.g, c.b, a);
        }
    }
}
