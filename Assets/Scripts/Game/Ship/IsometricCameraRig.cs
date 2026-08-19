using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Isometric follow camera. Presence AND enabled state of this component is the
    /// isometric-mode flag: every mode-aware consumer holds a serialized reference and treats
    /// a null or disabled rig as first-person. All rig numbers come from
    /// <see cref="ShipCamera"/> so docgen and the scene cannot disagree. The camera holds
    /// still while the target stays inside the ground-plane dead zone, then follows with
    /// exponential smoothing; rotation is locked (see <see cref="ShipCamera"/>).</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        #region Inspector

        [SerializeField, OptionalBinding, Tooltip("Player root the camera follows. Falls back " +
            "to PlayerRegistry.Local when unset (net-spawned rigs register there).")]
        Transform _target;

        #endregion

        #region References

        Camera _camera;

        #endregion

        #region State

        Vector3 _focus;
        bool _focusInit;

        #endregion

        #region Properties

        /// <summary>The rig's own camera (cached in Awake).</summary>
        public Camera Camera => _camera;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            _camera = GetComponent<Camera>();
            // Perspective with a narrow FOV, pushed back — not orthographic; FollowDistance
            // is derived from FovDegrees to keep the framing (see ShipCamera).
            _camera.orthographic = false;
            _camera.fieldOfView = ShipCamera.FovDegrees;
            transform.rotation =
                Quaternion.Euler(ShipCamera.PitchDegrees, ShipCamera.YawDegrees, 0f);
            // The local body sits on LocalHead so the first-person camera can cull it;
            // this camera must draw it. There is no separate view-model layer to exclude —
            // with no first-person camera wire the hand gun stays parented to the body.
            int headLayer = LayerMask.NameToLayer(WeaponView.LocalHeadLayer);
            if (headLayer >= 0)
                _camera.cullingMask |= 1 << headLayer;
        }

        void OnEnable()
        {
            // Re-acquire the focus on re-enable so a teleport while disabled cannot make the
            // camera sweep across the ship.
            _focusInit = false;
        }

        void LateUpdate()
        {
            if (_target == null)
            {
                var local = PlayerRegistry.Local;
                if (local == null) return;
                _target = local.transform;
            }

            Vector3 targetPos = _target.position;
            if (!_focusInit)
            {
                _focusInit = true;
                _focus = targetPos;
            }
            else
            {
                float dx = targetPos.x - _focus.x;
                float dz = targetPos.z - _focus.z;
                bool outside = dx > ShipCamera.DeadZoneHalfX || dx < -ShipCamera.DeadZoneHalfX
                    || dz > ShipCamera.DeadZoneHalfZ || dz < -ShipCamera.DeadZoneHalfZ;
                if (outside)
                {
                    // Frame-rate-independent exponential smoothing toward the target.
                    float k = 1f - Mathf.Exp(-ShipCamera.FollowLerpPerSecond * Time.deltaTime);
                    _focus.x += dx * k;
                    _focus.z += dz * k;
                }
                // Single floor: height tracks directly, the dead zone is ground-plane only.
                _focus.y = targetPos.y;
            }

            transform.position = _focus - transform.forward * ShipCamera.FollowDistance;
        }

        #endregion

        #region Public API

        /// <summary>World ray under a screen position (cursor aim / cursor interact).</summary>
        public Ray ScreenRay(Vector3 screenPos)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            return _camera.ScreenPointToRay(screenPos);
        }

        #endregion
    }
}
