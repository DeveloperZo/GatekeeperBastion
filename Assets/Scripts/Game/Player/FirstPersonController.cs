using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: minimal first-person controller — CharacterController WASD movement
    /// plus mouse look with cursor lock (Escape releases, left-click re-locks), and a
    /// stamina-limited sprint on Left Shift. No jump in v0.
    /// Mouse sensitivity is read from <see cref="Settings"/> every frame so options take effect live.</summary>
    /// <remarks>
    /// WHY SPRINT IS RATIONED — this is the load-bearing part, do not "simplify" it away:
    /// a provoked gatekeeper chases at 5.6 m/s (EnemyGatekeeper.chaseSpeed), which is deliberately
    /// FASTER than the 5 m/s walk so that backing away while shooting is not a free win. Sprint
    /// at 7.5 m/s is faster than the chase, so an UNLIMITED sprint would hand the player exactly
    /// the kiting exploit the enemy-AI work just closed: outrun the pack forever, plink at it,
    /// never take a swing, never risk the hatch. Stamina is the price. The pool exists so sprint
    /// is a BURST for repositioning — break contact, cross the bore, get back to the hatch —
    /// and never sustained flight: 100 stamina at 25/s drains in exactly FOUR SECONDS of sprint,
    /// about 30 m of ground, and it costs a 1s pause plus ~5.5s of standing regen to buy back.
    /// Over any window longer than a few seconds the player's average speed is still below the
    /// gatekeepers'. Change these five numbers together or not at all.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        /// <summary>Sprint stamina burned per second while actually sprinting. Public so the
        /// Crew Quarters panel can quote the pool in SECONDS of sprint without duplicating it.</summary>
        public const float SprintDrainPerSecond = 25f;

        /// <summary>Stamina regained per second once the regen delay has elapsed.</summary>
        public const float StaminaRegenPerSecond = 18f;

        /// <summary>Seconds after sprinting stops before stamina starts coming back.</summary>
        public const float StaminaRegenDelaySeconds = 1f;

        /// <summary>Movement basis under the locked isometric camera: W is screen-up (the
        /// camera's ground-plane forward), A/D screen left/right. The camera yaw is a
        /// compile-time constant (rotation locked), so this is built once — no per-frame
        /// camera read. The root's frozen authored yaw must never steer iso movement.</summary>
        static readonly Quaternion IsoMoveBasis =
            Quaternion.Euler(0f, ShipCamera.YawDegrees, 0f);

        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float sprintSpeed = 7.5f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float pitchLimit = 85f;

        [Tooltip("Max stamina. Overwritten at run start by GameController.ApplyMetaEffects " +
                 "with MetaUpgrades.StaminaMax(meta.StaminaLevel).")]
        [SerializeField] float staminaMax = 100f;

        [Tooltip("Camera child used for pitch. Wired by SceneBuilder; look is disabled when missing. " +
                 "Optional only because isometric scenes author it null on purpose.")]
        [OptionalBinding]
        public Transform cameraTransform;

        [Tooltip("Isometric rig (Ship probe). While wired and enabled, walking leaves the cursor " +
                 "free and movement no longer requires cursor lock; null or disabled means " +
                 "first-person behavior, unchanged.")]
        [SerializeField, OptionalBinding]
        IsometricCameraRig _isoRig;

        CharacterController _cc;
        Camera _cam;
        DecoyPlacement _decoy;
        AudioSource _footSfx;
        AudioClip _footClip;
        float _pitch;
        float _verticalVelocity;
        float _stamina;
        float _regenDelay;   // > 0 while the post-sprint pause is still running
        bool _exhausted;     // latched at zero; cleared once ReengageFraction is back in the tank
        float _footClock;

        /// <summary>After bottoming out, sprint stays locked until the pool is back above this
        /// fraction of max. Guard, not a design knob: without it a held Shift re-engages sprint on
        /// the first regen frame, drains the ~0.3 stamina it just got back inside one frame, and
        /// the player gets a flickering IsSprinting/HUD bar instead of "you are out of breath".</summary>
        const float ReengageFraction = 0.2f;

        /// <summary>When true (menu/lose screen, dead/respawning) look and movement input are ignored.</summary>
        /// <summary>Anchor plant: WASD off, look and fire stay. Distinct from
        /// <see cref="MovementLocked"/> (pause / death / fighting gate).</summary>
        public bool Rooted { get; set; }

        public bool MovementLocked { get; set; }

        /// <summary>Current sprint stamina, 0..StaminaMax. HUD reads this.</summary>
        public float Stamina => _stamina;

        /// <summary>Max sprint stamina (MetaUpgrades.StaminaMax at the Stamina track level).</summary>
        public float StaminaMax => staminaMax;

        /// <summary>True on the frames the player is actually sprinting (Shift held, moving,
        /// stamina left). HUD reads this.</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>True when WASD is producing planar motion this tick.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>True while RMB zoom is live (FOV 40). WeaponViewModel tucks long guns.</summary>
        public bool IsZooming { get; private set; }

        /// <summary>Apply the Crew Quarters stamina pool (GameController pushes MetaUpgrades values
        /// here). Fills the tank — called at run start, never mid-fight, so the player never
        /// starts a run winded by a purchase.</summary>
        public void ConfigureStamina(float newStaminaMax)
        {
            staminaMax = Mathf.Max(1f, newStaminaMax);
            _stamina = staminaMax;
            _regenDelay = 0f;
            _exhausted = false;
        }

        /// <summary>Lock/free the cursor (GameController drives this from the flow state).</summary>
        public void SetCursorLock(bool locked)
        {
            SetCursorLocked(locked);
        }

        /// <summary>Set the look pitch through the controller's own pipeline. THE ONLY legal
        /// way to point the camera up/down from outside: pitch lives in _pitch and is
        /// re-applied to the camera every look tick, so writing camera.localRotation
        /// directly is stomped a frame later, and putting pitch on the BODY transform
        /// tilts the yaw axis and skews every subsequent mouse look (the capsule wake
        /// orientation defect, 2026-08-18). Positive looks down.</summary>
        public void SetLookPitch(float degrees)
        {
            _pitch = Mathf.Clamp(degrees, -pitchLimit, pitchLimit);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (cameraTransform == null && _isoRig == null)
            {
                // Authoring-error-only path: an isometric scene authors no camera wire on
                // purpose (cursor-driven aim, no mouse look); anywhere else a missing wire
                // is a defect — look stays disabled.
                Log.Error("FirstPersonController",
                    "cameraTransform is not wired — mouse look is disabled (author the binding).");
            }
            staminaMax = Mathf.Max(1f, staminaMax);
            _stamina = staminaMax;
            if (cameraTransform != null)
            {
                _cam = cameraTransform.GetComponent<Camera>();
                _decoy = cameraTransform.GetComponent<DecoyPlacement>();
            }
            if (_decoy == null)
                _decoy = GetComponentInChildren<DecoyPlacement>(true);
            var footGo = transform.Find("SfxFootsteps");
            if (footGo != null) _footSfx = footGo.GetComponent<AudioSource>();
            if (_footSfx == null)
            {
                var go = new GameObject("SfxFootsteps");
                go.transform.SetParent(transform, false);
                _footSfx = go.AddComponent<AudioSource>();
                _footSfx.playOnAwake = false;
                _footSfx.spatialBlend = 0f;
            }
            _footClip = Resources.Load<AudioClip>(WeaponView.FootstepWalk);
            AudioBuses.ApplyFromSettings();
        }

        /// <summary>True once the Bastion's tick owner drives this component; its own
        /// Update then yields so the written system order is the only order. Ship and menu
        /// scenes have no tick owner and stay self-driven.</summary>
        bool _hosted;

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return; // the Bastion tick owner calls Tick in the written order
            Tick(Time.deltaTime);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SetCursorLocked(false);
        }

        /// <summary>Locomotion: cursor lock, look, sprint and the CharacterController move.</summary>
        public void Tick(float dt)
        {
            if (!isActiveAndEnabled) return;
            if (_cc == null || !_cc.enabled) return;

            // Defensive sample: free when the scene's tick owner already sampled this frame.
            var frame = Controls.Sample();

            // Iso mode aims with the OS cursor: never re-lock it, and movement must not
            // require the lock (canLook stays false there — no camera wire, no mouse look).
            bool isoDriven = _isoRig != null && _isoRig.isActiveAndEnabled;

            if (Controls.Down(ControlId.FreeCursor)) SetCursorLocked(false);
            // A live Lock line uses the same move lock as an open station, and clicking its
            // Continue control must not re-lock the cursor mid-line.
            bool voiceLocked = DemoVoice.LocksInput;
            // Click-to-relock only during the fight; menu clicks must stay free for IMGUI buttons.
            if (!MovementLocked && !voiceLocked && !isoDriven && frame.click0Down
                && Cursor.lockState != CursorLockMode.Locked)
                SetCursorLocked(true);

            bool canLook = Cursor.lockState == CursorLockMode.Locked && !MovementLocked
                && !voiceLocked;
            bool canMove = (canLook || (isoDriven && !MovementLocked && !voiceLocked)) && !Rooted;
            TickZoom();
            // Look requires the authored camera wire: yaw without pitch is a half-broken camera,
            // so a missing cameraTransform (already Log.Error'd in Awake) disables look entirely.
            if (canLook && cameraTransform != null)
            {
                float sens = Settings.GetFloat(SettingId.MouseSensitivity);
                if (_cam != null)
                    sens *= _cam.fieldOfView / WeaponView.HipFov;
                transform.Rotate(0f, frame.lookX * sens, 0f);
                _pitch = Mathf.Clamp(_pitch - frame.lookY * sens, -pitchLimit, pitchLimit);
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            Vector3 planar = Vector3.zero;
            if (canMove)
            {
                var input = Vector3.ClampMagnitude(
                    new Vector3(frame.moveX, 0f, frame.moveY), 1f);
                IsMoving = input.sqrMagnitude > 0.01f;
                // Iso: camera-relative movement (IsoMoveBasis), because the root's yaw is
                // frozen and meaningless to the player there. First person: the transform
                // basis, exactly as before the iso path existed.
                Vector3 world = isoDriven
                    ? IsoMoveBasis * input
                    : transform.TransformDirection(input);
                planar = world * TickSprint(IsMoving, dt);
            }
            else
            {
                IsMoving = false;
                TickSprint(false, dt); // locked out (panel/menu/down): no sprint, but stamina still returns
            }

            // Rooted and a live Lock line must not tick gravity or CharacterController.Move.
            // Move(0) still depenetrates, and gravity while the wake capsule's boxes are
            // triggers drops the rig through the mattress onto the deck (Ship.unity: Bed
            // occupies y 0–0.7, Floor_-1_1 top is y 0, CC bottom is pivot−0.9). The wake
            // Lock after the lid would sink the same way if gravity ran under the box.
            // Look stays live above (sealed, Rooted, no Lock) or is already eaten by Lock.
            if (Rooted || voiceLocked)
            {
                _verticalVelocity = 0f;
                TickFootsteps(dt);
                return;
            }

            _verticalVelocity = _cc.isGrounded ? -1f : _verticalVelocity + gravity * dt;
            _cc.Move((planar + Vector3.up * _verticalVelocity) * dt);
            TickFootsteps(dt);
        }

        /// <summary>Hold RMB. Hip 60 / zoom 40. Decoy aim keeps RMB as cancel.</summary>
        void TickZoom()
        {
            if (_cam == null)
            {
                IsZooming = false;
                return;
            }
            bool decoyAiming = _decoy != null && _decoy.IsAiming;
            bool zoom = !MovementLocked && !DemoVoice.LocksInput && !decoyAiming
                && Controls.Held(ControlId.AltFire);
            IsZooming = zoom;
            float want = zoom ? WeaponView.ZoomFov : WeaponView.HipFov;
            if (Mathf.Abs(_cam.fieldOfView - want) > 0.01f)
                _cam.fieldOfView = want;
        }

        void TickFootsteps(float dt)
        {
            if (!IsMoving || MovementLocked || _cc == null || !_cc.isGrounded)
            {
                _footClock = 0f;
                return;
            }
            float period = IsSprinting ? WeaponView.SprintFootstepSeconds : WeaponView.WalkFootstepSeconds;
            _footClock += dt;
            if (_footClock < period) return;
            _footClock -= period;
            if (_footSfx != null && _footClip != null)
                _footSfx.PlayOneShot(_footClip, WeaponView.SfxOneShotScale);
        }

        /// <summary>Resolve sprint for this frame and return the planar speed to move at.
        /// Sprint requires Left Shift AND actual movement input AND stamina — holding Shift while
        /// standing still must not drain the pool, or "sprint" becomes a tax on standing.</summary>
        float TickSprint(bool moving, float dt)
        {
            bool wants = moving && Controls.Held(ControlId.Sprint);

            if (_exhausted && _stamina >= staminaMax * ReengageFraction) _exhausted = false;

            IsSprinting = wants && !_exhausted && _stamina > 0f;

            if (IsSprinting)
            {
                _stamina -= SprintDrainPerSecond * dt;
                if (_stamina <= 0f)
                {
                    _stamina = 0f;
                    _exhausted = true;   // cannot sprint at zero
                    IsSprinting = false; // this frame's move already spent the last of it
                }
                _regenDelay = StaminaRegenDelaySeconds;
                return sprintSpeed;
            }

            // Not sprinting: serve out the delay, then refill.
            if (_regenDelay > 0f) _regenDelay -= dt;
            else if (_stamina < staminaMax)
                _stamina = Mathf.Min(staminaMax, _stamina + StaminaRegenPerSecond * dt);

            return moveSpeed;
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
