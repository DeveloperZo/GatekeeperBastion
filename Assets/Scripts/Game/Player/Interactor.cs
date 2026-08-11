using UnityEngine;

namespace GatekeeperBastion.Game
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

        void Update()
        {
            _current = null;
            CurrentPrompt = null;

            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range))
                _current = hit.collider.GetComponentInParent<IInteractable>();

            if (_current == null) return;
            CurrentPrompt = _current.Prompt;
            if (Controls.Held(ControlId.Interact))
                _current.Interact(Time.deltaTime);
        }
    }
}
