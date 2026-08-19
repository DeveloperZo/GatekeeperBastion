using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Anything the player can use with the E key: a HUD prompt plus a per-frame
    /// Interact(dt) called while the key is held and the object is in view.</summary>
    public interface IInteractable
    {
        /// <summary>Short prompt shown by the HUD while this interactable is targeted.</summary>
        string Prompt { get; }

        /// <summary>Called every frame the player holds E while looking at this interactable.</summary>
        void Interact(float dt);
    }

    /// <summary>LOCAL-OWNER: camera-forward raycaster — surfaces the targeted IInteractable's prompt and
    /// forwards E-key hold time into Interact(dt). Lives on the player camera.</summary>
    public sealed class Interactor : MonoBehaviour
    {
        // The cursor ray starts at the iso camera (FollowDistance back from the ship), so the
        // cast must reach well past the focus; the player-proximity contract still uses range.
        const float IsoCastDistance = ShipCamera.FollowDistance * 2f;

        [SerializeField] float range = 3.2f;

        [SerializeField, OptionalBinding, Tooltip("Isometric rig (Ship probe). While wired and " +
            "enabled, the target ray comes from the cursor and no cursor lock is required; " +
            "null or disabled means first-person behavior, unchanged.")]
        IsometricCameraRig _isoRig;

        /// <summary>Prompt of the currently targeted interactable, or null. Read by the HUD.</summary>
        public string CurrentPrompt { get; private set; }

        /// <summary>Target under the crosshair this frame, or null. Ship uses this for station open.</summary>
        public IInteractable Current => _current;

        IInteractable _current;

        /// <summary>True once the Bastion's tick owner drives this component; its own
        /// Update then yields so the written system order is the only order. The Ship scene
        /// has no tick owner and stays self-driven.</summary>
        bool _hosted;

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return; // the Bastion tick owner calls Tick in the written order
            Tick(Time.deltaTime);
        }

        /// <summary>Interact/repair: crosshair raycast and the held-interact drive.</summary>
        public void Tick(float dt)
        {
            _current = null;
            CurrentPrompt = null;

            bool iso = _isoRig != null && _isoRig.isActiveAndEnabled;
            if (!iso && Cursor.lockState != CursorLockMode.Locked) return;

            bool hitSomething;
            RaycastHit hit;
            if (iso)
            {
                // Cursor-driven: target what the cursor touches, but keep the proximity
                // contract — the player (this transform's rig) must still be within range.
                // Pointer comes from the input producer's frame, never an ambient engine poll.
                var frame = Controls.Sample();
                Ray ray = _isoRig.ScreenRay(new Vector3(frame.pointerX, frame.pointerY, 0f));
                hitSomething = Physics.Raycast(ray, out hit, IsoCastDistance)
                    && (hit.point - transform.position).sqrMagnitude <= range * range;
            }
            else
            {
                hitSomething = Physics.Raycast(
                    transform.position, transform.forward, out hit, range);
            }

            if (hitSomething)
                _current = hit.collider.GetComponentInParent<IInteractable>();

            if (_current == null) return;
            CurrentPrompt = _current.Prompt;
            if (Controls.Held(ControlId.Interact))
                _current.Interact(dt);
        }
    }
}
