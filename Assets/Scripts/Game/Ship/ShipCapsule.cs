using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the wake capsule — black hold, fade-up, the stir line, the
    /// first hold-interact of the game (unlock, 1.0 s), and the lid lift. DemoShipDirector
    /// decides whether a load starts sealed, open-on-deck, or dormant; this component only
    /// plays the sequence it is told to. The fade image is authored on the Ship HUD canvas
    /// and driven here (no new light or camera API — A2/A6).</summary>
    public sealed class ShipCapsule : MonoBehaviour, IInteractable
    {
        const string Tag = "Demo";
        const float BlackHoldSeconds = 0.4f;
        const float FadeSeconds = 1.5f;
        const float UnlockHoldSeconds = 1.0f;
        const float LidLiftSeconds = 0.8f;
        const float LidLiftMetres = 1.1f;

        /// <summary>Sealed start pitch, degrees down: the standing rig's eye sits above the
        /// graybox lid, so the wake look (and the unlock raycast) point down at it.</summary>
        const float LidLookPitchDegrees = 50f;

        [SerializeField, OptionalBinding, Tooltip("Full-screen black Image on the Ship HUD canvas. Driven only while the wake sequence runs.")]
        Image _fadeImage;

        [SerializeField, OptionalBinding, Tooltip("Lid transform; lifts straight up on unlock. Carries the interact collider.")]
        Transform _lid;

        [SerializeField, OptionalBinding, Tooltip("Eye pose inside the bed — the player is placed here while sealed.")]
        Transform _bedPose;

        enum Phase { Dormant, Black, Fade, Stir, Sealed, Opening, Done }

        Phase _phase = Phase.Dormant;
        float _timer;
        float _hold;
        int _holdFrame = -1;
        Vector3 _lidClosedPos;
        bool _lidPosCaptured;
        FirstPersonController _fpc;

        public string Prompt
        {
            get
            {
                if (_phase != Phase.Sealed) return null;
                if (Time.frameCount - _holdFrame > 1) _hold = 0f;
                return _hold > 0f ? "[E] unlocking capsule" : "[E] unlock capsule";
            }
        }

        /// <summary>Fresh wake: player into the bed, look free, move rooted, screen black.
        /// The capsule's colliders go trigger for the whole waked session: the standing rig
        /// shares the capsule's footprint, and solid boxes there depenetrate the
        /// CharacterController (shoved sideways or through the deck). Raycasts still hit
        /// triggers in this project (DynamicsManager m_QueriesHitTriggers 1), so the lid
        /// unlock prompt keeps working. Never restored mid-session — going solid again
        /// while the player still stands in the footprint would shove them; reload paths
        /// (BeginOpen/BeginDormant) keep the authored solid capsule.</summary>
        public void BeginSealed(FirstPersonController fpc)
        {
            _fpc = fpc;
            CaptureLid();
            SetCapsuleTrigger(true);
            PlacePlayerInBed();
            if (_fpc != null) _fpc.Rooted = true;
            if (_fadeImage != null)
            {
                _fadeImage.gameObject.SetActive(true);
                SetFadeAlpha(1f);
            }
            _phase = Phase.Black;
            _timer = 0f;
            DemoShipFight.WakeHidesCrosshair = true;
            Log.Event(Tag, "spawn capsule");
        }

        /// <summary>Reload after the unlock: lid open, nothing to play. The capsule is
        /// furniture from here on (no reseal — beat 0 reload matrix).</summary>
        public void BeginOpen()
        {
            CaptureLid();
            SetLidOpen();
            _phase = Phase.Done;
            DemoShipFight.WakeHidesCrosshair = false;
        }

        /// <summary>Demo off / later beats: open furniture, no interactions, no fade.</summary>
        public void BeginDormant()
        {
            CaptureLid();
            SetLidOpen();
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
            Log.Event(Tag, "capsule unlocked");
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
                        DemoShipFight.SetStanding("Unlock the capsule");
                    }
                    break;

                case Phase.Opening:
                    _timer += Time.deltaTime;
                    if (_lid != null)
                        _lid.localPosition = _lidClosedPos
                            + Vector3.up * (LidLiftMetres * Mathf.Clamp01(_timer / LidLiftSeconds));
                    if (_timer >= LidLiftSeconds)
                    {
                        _phase = Phase.Done;
                        DemoShipFight.WakeHidesCrosshair = false;
                        if (_fpc != null)
                        {
                            _fpc.Rooted = false;
                            // Horizon: the sealed look is 50° down at the lid. Leaving that
                            // pitch on would start walking staring at the deck.
                            _fpc.SetLookPitch(0f);
                        }
                        DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
                        DemoShipFight.SetStanding("Repair the NAV MAP");
                    }
                    break;
            }
        }

        void SetCapsuleTrigger(bool asTrigger)
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].isTrigger = asTrigger;
            }
        }

        void CaptureLid()
        {
            if (_lidPosCaptured || _lid == null) return;
            _lidPosCaptured = true;
            _lidClosedPos = _lid.localPosition;
        }

        void SetLidOpen()
        {
            if (_lid == null) return;
            _lid.localPosition = _lidClosedPos + Vector3.up * LidLiftMetres;
        }

        void PlacePlayerInBed()
        {
            var player = PlayerRegistry.Local;
            if (player == null || _bedPose == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            // XZ from the pose; Y stays the rig's own. The authored rig height is the one
            // place its pivot-to-feet offset is correct — a marker Y below it starts the
            // CharacterController inside the deck and the player falls through the floor
            // on the first physics step (2026-08-18 defect).
            Vector3 at = _bedPose.position;
            at.y = player.transform.position.y;
            // YAW ONLY on the body. FirstPersonController keeps yaw on the body and pitch
            // on the camera; a marker rotation with pitch tilts the body's yaw axis and
            // every later mouse look comes out skewed (the wake orientation defect,
            // 2026-08-18). Pitch goes through the controller's own pipeline below.
            float yaw = _bedPose.rotation.eulerAngles.y;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
            if (_fpc != null) _fpc.SetLookPitch(LidLookPitchDegrees);
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
