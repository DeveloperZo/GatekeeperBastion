using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: one hatch door on the hull. Carries its stable id,
    /// drives the open/sealed look and supplies the look-at string
    /// ("HATCH n — yours / SEAT n / sealed"). Reads the host-written assignment via
    /// ShipHatchAssignment; never writes it. There is no interact verb on a hatch —
    /// looking is the whole surface.</summary>
    public sealed class ShipHatchDoor : MonoBehaviour, IInteractable
    {
        #region Inspector
        [SerializeField, Tooltip("Stable hatch id, 1..3 bow to stern.")]
        int _hatchId = 1;

        [SerializeField, Tooltip("Door leaf renderer swapped between the state materials.")]
        Renderer _leaf;

        [SerializeField] Material _openMaterial;
        [SerializeField] Material _sealedMaterial;
        #endregion

        #region State
        int _appliedPacked = int.MinValue;
        int _appliedLocalSlot = int.MinValue;
        string _prompt = "";
        #endregion

        #region Properties
        /// <summary>Stable id, 1..3 bow to stern.</summary>
        public int HatchId
        {
            get { return _hatchId; }
        }

        /// <summary>True when this door is open under the current assignment.</summary>
        public bool IsOpen
        {
            get { return ShipHatches.IsOpen(ShipHatchAssignment.CurrentPacked, _hatchId - 1); }
        }

        /// <summary>Look-at line naming the owner or the sealed state.</summary>
        public string Prompt
        {
            get
            {
                Refresh();
                return _prompt;
            }
        }
        #endregion

        #region Public API
        /// <summary>No interact verb on a hatch; the Interactor still needs the member.</summary>
        public void Interact(float dt)
        {
        }
        #endregion

        #region Unity Lifecycle
        void Update()
        {
            Refresh();
        }
        #endregion

        #region Private
        /// <summary>Re-derive prompt and leaf material; allocates only when the
        /// assignment or the local seat actually changed.</summary>
        void Refresh()
        {
            int packed = ShipHatchAssignment.CurrentPacked;
            int localSlot = LocalSlot();
            if (packed == _appliedPacked && localSlot == _appliedLocalSlot) return;
            _appliedPacked = packed;
            _appliedLocalSlot = localSlot;

            int index = _hatchId - 1;
            bool open = ShipHatches.IsOpen(packed, index);
            int owner = ShipHatches.OwnerSlot(packed, index);

            if (_leaf != null)
            {
                var mat = open ? _openMaterial : _sealedMaterial;
                if (mat != null) _leaf.sharedMaterial = mat;
            }

            if (!open)
                _prompt = "HATCH " + _hatchId + " — sealed";
            else if (owner == localSlot)
                _prompt = "HATCH " + _hatchId + " — yours";
            else
                _prompt = "HATCH " + _hatchId + " — SEAT " + owner;
        }

        static int LocalSlot()
        {
            var avatar = NetPlayerAvatar.LocalOwner;
            if (avatar == null) return 0; // solo: the one seat is slot 0
            return avatar.Slot.Value;
        }
        #endregion
    }
}
