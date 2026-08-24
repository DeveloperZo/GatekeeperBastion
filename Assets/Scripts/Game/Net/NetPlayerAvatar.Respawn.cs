using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): paid respawn for a downed seat.
    /// Split so the avatar file stays under cap — this is the request and the rejoin,
    /// not a second type. One button covers both tiers: mid-Assault it draws on the
    /// respawn pool, in Prep it does not, and the sim refuses named either way.</summary>
    public sealed partial class NetPlayerAvatar
    {
        #region Public API
        /// <summary>Owner: ask to pay the run's next respawn cost out of the realm's pot
        /// and rejoin at the Hatch. No wallet figure travels: the pot belongs to the realm
        /// and the host reads it directly, so there is nothing here for a client to report
        /// or to get wrong.</summary>
        public void RequestRespawn()
        {
            RequestRespawnServerRpc();
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server)]
        void RequestRespawnServerRpc()
        {
            if (!IsServer) return;
            if (_game == null || !_game.TryNetRespawn(this, out long cost)) return;
            ApplyRejoin();
            Log.Event("Demo", "respawn seat=" + Slot.Value + " cost=" + cost);
        }
        #endregion
    }
}
