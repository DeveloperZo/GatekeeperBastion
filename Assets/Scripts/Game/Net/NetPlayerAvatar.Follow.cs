using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): a seat that arrived after the party
    /// embarked, and the break at which it may follow them in. Split so the avatar file stays
    /// under cap — this is the request and the admission, not a second type, and it reuses the
    /// same rejoin the Hatch-bank path does.</summary>
    public sealed partial class NetPlayerAvatar
    {
        #region State
        /// <summary>Server-written: this seat connected while a run was already under way and
        /// has not followed the party in yet. Distinct from <see cref="WaitingOnShip"/>, which
        /// a downed Boarder also raises — that one is bought back or embarked at a Hatch-bank
        /// window, this one only waits for the next break.</summary>
        public NetworkVariable<bool> AwaitingFollow = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        #region Public API
        /// <summary>Follow the party into the run they are already in. Refused outside a
        /// break — see <see cref="TryAcceptFollow"/>.</summary>
        public void RequestFollow()
        {
            RequestFollowServerRpc();
        }
        #endregion

        #region Private
        /// <summary>Take a late arrival into the run. Prep only: a wave already on the floor was
        /// composed for the party standing there when it was drawn, and letting a seat in under
        /// it would resize nothing and pay everything. The refusal is named rather than queued —
        /// a player holding a live button knows more than one watching a spinner, and pressing
        /// it again at the break is the whole interaction.</summary>
        void TryAcceptFollow()
        {
            if (!AwaitingFollow.Value) return;

            var sim = _game != null ? _game.Sim : null;
            if (sim == null) return;

            if (sim.Phase != RunPhase.Tethered)
            {
                Log.Event(Tag, "follow refused — the party is not in a run clientId="
                    + OwnerClientId);
                return;
            }

            if (sim.Assault != AssaultState.Prep)
            {
                Log.Event(Tag, "follow refused — a wave is on the floor clientId="
                    + OwnerClientId);
                return;
            }

            if (!RunSeats.Admit(OwnerClientId)) return;

            AwaitingFollow.Value = false;
            ApplyRejoin();
            // Both read the seat count, and the door opening is how arriving is legible from
            // inside the bastion rather than only in a log.
            ShipHatchAssignment.Recompute("seat followed the party");
            if (_game != null) _game.RefreshPlayerCount();
            Log.Event(Tag, "seat followed the party clientId=" + OwnerClientId);
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server)]
        void RequestFollowServerRpc()
        {
            if (!IsServer) return;
            TryAcceptFollow();
        }
        #endregion
    }
}
