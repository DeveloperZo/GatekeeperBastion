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
        [SerializeField] float range = 3.2f;

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

            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range))
                _current = hit.collider.GetComponentInParent<IInteractable>();

            if (_current == null) return;
            CurrentPrompt = _current.Prompt;
            if (Controls.Held(ControlId.Interact))
                _current.Interact(dt);
        }
    }
}
