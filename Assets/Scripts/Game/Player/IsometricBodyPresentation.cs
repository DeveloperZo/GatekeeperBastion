using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: under the isometric camera the local Boarder body is visible, so
    /// drive its Animator Speed from horizontal velocity and turn the body child to face the
    /// travel direction (yaw only, no roll). Inert while the iso rig is absent or disabled —
    /// first-person scenes keep WeaponViewModel as the Speed owner. Sits on the player root.</summary>
    public sealed class IsometricBodyPresentation : MonoBehaviour
    {
        #region Constants

        // Must match NetPlayerAvatar.DriveRemoteAnimator's thresholds so the local body and a
        // remote avatar of the same player read the same gait at the same velocity.
        const float MoveEpsMps = 0.4f;
        const float SprintEpsMps = 6.25f;

        /// <summary>Body yaw rate toward the travel direction, degrees per second.</summary>
        const float FaceTurnDegreesPerSecond = 720f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");

        #endregion

        #region Inspector

        [SerializeField, Tooltip("Isometric rig; this component is inert while the rig is absent or disabled.")]
        IsometricCameraRig _isoRig;

        [SerializeField, Tooltip("Boarder body child rotated to face travel. Never the player " +
            "root — its yaw is input space and stays scene-frozen under the iso camera.")]
        Transform _body;

        [SerializeField, OptionalBinding, Tooltip("Body Animator; resolved from children when unset.")]
        Animator _animator;

        #endregion

        #region References

        CharacterController _cc;

        #endregion

        #region State

        Vector3 _lastPos;
        bool _haveSample;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            TryGetComponent(out _cc);
            if (_animator == null) _animator = GetComponentInChildren<Animator>(true);
        }

        void LateUpdate()
        {
            if (_isoRig == null || !_isoRig.isActiveAndEnabled)
            {
                _haveSample = false;
                return;
            }

            float dt = Time.deltaTime;
            Vector3 velocity = SampleHorizontalVelocity(dt);
            float mps = velocity.magnitude;

            if (_animator != null)
            {
                float speed = 0f;
                if (mps > MoveEpsMps)
                    speed = mps >= SprintEpsMps ? 1f : 0.5f;
                _animator.SetFloat(SpeedHash, speed);
            }

            if (_body != null && mps > MoveEpsMps)
            {
                Quaternion want = Quaternion.LookRotation(velocity / mps, Vector3.up);
                _body.rotation = Quaternion.RotateTowards(
                    _body.rotation, want, FaceTurnDegreesPerSecond * dt);
            }
        }

        #endregion

        #region Private

        /// <summary>Horizontal (XZ) velocity, m/s — CharacterController when live, else the
        /// transform delta (same sampling shape as NetPlayerAvatar's remote drive).</summary>
        Vector3 SampleHorizontalVelocity(float dt)
        {
            if (_cc != null && _cc.enabled)
            {
                Vector3 v = _cc.velocity;
                v.y = 0f;
                return v;
            }

            Vector3 pos = transform.position;
            if (!_haveSample || dt <= 0f)
            {
                _lastPos = pos;
                _haveSample = true;
                return Vector3.zero;
            }
            Vector3 delta = pos - _lastPos;
            _lastPos = pos;
            delta.y = 0f;
            return delta / dt;
        }

        #endregion
    }
}
