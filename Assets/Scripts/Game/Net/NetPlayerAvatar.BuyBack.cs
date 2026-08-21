using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): paid Hatch-bank rejoin.
    /// Split so the avatar file stays under cap — this is the request and the rejoin,
    /// not a second type.</summary>
    public sealed partial class NetPlayerAvatar
    {
        #region Public API
        /// <summary>Owner: ask to pay <see cref="HatchBank.BuyBackCost"/> out of the realm's
        /// pot and rejoin a live Hatch-bank Assault. Host refuses named. No wallet figure
        /// travels: the pot belongs to the realm and the host can read it directly, so there
        /// is nothing here for a client to report or to get wrong.</summary>
        public void RequestBuyBack()
        {
            RequestBuyBackServerRpc();
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server)]
        void RequestBuyBackServerRpc()
        {
            if (!IsServer) return;
            if (_game == null || !_game.TryNetBuyBack(this)) return;
            ApplyRejoin();
            Log.Event("Demo", "buyback seat=" + Slot.Value + " cost=" + HatchBank.BuyBackCost);
        }
        #endregion
    }
}
