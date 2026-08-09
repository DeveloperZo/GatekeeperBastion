using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
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

        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float sprintSpeed = 7.5f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float pitchLimit = 85f;

        [Tooltip("Max stamina. Overwritten at run start by GameController.ApplyMetaEffects " +
                 "with MetaUpgrades.StaminaMax(meta.StaminaLevel).")]
        [SerializeField] float staminaMax = 100f;

        [Tooltip("Camera child used for pitch. Wired by SceneBuilder; falls back to Camera.main.")]
        public Transform cameraTransform;

        CharacterController _cc;
        float _pitch;
        float _verticalVelocity;
        float _stamina;
        float _regenDelay;   // > 0 while the post-sprint pause is still running
        bool _exhausted;     // latched at zero; cleared once ReengageFraction is back in the tank

        /// <summary>After bottoming out, sprint stays locked until the pool is back above this
        /// fraction of max. Guard, not a design knob: without it a held Shift re-engages sprint on
        /// the first regen frame, drains the ~0.3 stamina it just got back inside one frame, and
        /// the player gets a flickering IsSprinting/HUD bar instead of "you are out of breath".</summary>
        const float ReengageFraction = 0.2f;

        /// <summary>When true (menu/lose screen, dead/respawning) look and movement input are ignored.</summary>
        public bool MovementLocked { get; set; }

        /// <summary>Current sprint stamina, 0..StaminaMax. HUD reads this.</summary>
        public float Stamina => _stamina;

        /// <summary>Max sprint stamina (MetaUpgrades.StaminaMax at the Stamina track level).</summary>
        public float StaminaMax => staminaMax;

        /// <summary>True on the frames the player is actually sprinting (Shift held, moving,
        /// stamina left). HUD reads this.</summary>
        public bool IsSprinting { get; private set; }

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

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            staminaMax = Mathf.Max(1f, staminaMax);
            _stamina = staminaMax;
        }

        void Update()
        {
            if (Controls.Down(ControlId.FreeCursor)) SetCursorLocked(false);
            // Click-to-relock only during the fight; menu clicks must stay free for IMGUI buttons.
            if (!MovementLocked && Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
                SetCursorLocked(true);

            bool canControl = Cursor.lockState == CursorLockMode.Locked && !MovementLocked;
            if (canControl)
            {
                float sens = Settings.GetFloat(SettingId.MouseSensitivity);
                transform.Rotate(0f, Input.GetAxis("Mouse X") * sens, 0f);
                _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * sens, -pitchLimit, pitchLimit);
                if (cameraTransform != null)
                    cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            Vector3 planar = Vector3.zero;
            if (canControl)
            {
                var input = Vector3.ClampMagnitude(
                    new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")), 1f);
                planar = transform.TransformDirection(input) * TickSprint(input.sqrMagnitude > 0.01f);
            }
            else
            {
                TickSprint(false); // locked out (panel/menu/down): no sprint, but stamina still returns
            }

            _verticalVelocity = _cc.isGrounded ? -1f : _verticalVelocity + gravity * Time.deltaTime;
            _cc.Move((planar + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Resolve sprint for this frame and return the planar speed to move at.
        /// Sprint requires Left Shift AND actual movement input AND stamina — holding Shift while
        /// standing still must not drain the pool, or "sprint" becomes a tax on standing.</summary>
        float TickSprint(bool moving)
        {
            float dt = Time.deltaTime;
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
