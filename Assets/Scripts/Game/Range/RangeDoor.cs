using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the door between the party ship and the practice range, in both
    /// directions. One component: the range is a round trip, and a second type for the way
    /// back would be the same twelve lines with the destination swapped.
    ///
    /// It holds rather than taps. The teleporter's first interact is a 1.0 s hold
    /// (<c>ShipTeleporter.UnlockHoldSeconds</c>) and a scene change is at least as committal as
    /// that, so a brush past E while running for the hatch does not take you out of the ship.
    ///
    /// The refusal for walking into the range mid-Run is NOT here. It lives in
    /// <see cref="SceneFlow.LoadRange"/>, so a dev command or a checkpoint that loads the range
    /// is refused on the same terms as the door.</summary>
    public sealed class RangeDoor : MonoBehaviour, IInteractable
    {
        const string Tag = "Range";

        /// <summary>Matches the teleporter's unlock hold, so the two doors in the game feel the
        /// same in the hand.</summary>
        const float HoldSeconds = 1.0f;

        [SerializeField, Tooltip("On in the range, off on the ship. On sends the player back to " +
            "Ship - CoOp; off sends them to the range.")]
        bool _returnsToShip;

        [SerializeField, Tooltip("Shown on the HUD while the player looks at the door.")]
        string _label = "";

        float _hold;
        int _holdFrame = -1;
        bool _loading;

        public string Prompt
        {
            get
            {
                if (_loading) return "";
                if (!string.IsNullOrEmpty(_label)) return _label;
                return _returnsToShip ? "Hold E — back to the ship" : "Hold E — practice range";
            }
        }

        public void Interact(float dt)
        {
            if (_loading) return;

            // Interact is called every frame the key is held. A frame where it is NOT called
            // means the player let go, so the hold restarts — the same shape the teleporter's
            // release check uses, without needing an input query of its own.
            if (_holdFrame != Time.frameCount - 1) _hold = 0f;
            _holdFrame = Time.frameCount;

            _hold += dt;
            if (_hold < HoldSeconds) return;

            _hold = 0f;
            _loading = true;
            bool ok = _returnsToShip ? SceneFlow.LoadShipCoop() : SceneFlow.LoadRange();
            if (ok) return;

            // A refused load leaves the player standing at the door. Re-arm, or the door is
            // dead for the rest of the session and the reason is only in the log.
            _loading = false;
            Log.Event(Tag, _returnsToShip
                ? "return refused — see the load line above"
                : "range refused — see the load line above");
        }
    }
}
