using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): paid Hatch-bank rejoin.
    /// Split so the avatar file stays under cap — this is the report-and-ask wallet
    /// plus the rejoin, not a second type.</summary>
    public sealed partial class NetPlayerAvatar
    {
        #region Public API
        /// <summary>Owner: pay <see cref="HatchBank.BuyBackCost"/> from this machine's
        /// banked Salvage and rejoin a live Hatch-bank Assault. Host refuses named.</summary>
        public void RequestBuyBack()
        {
            long banked = GameSession.Meta != null ? GameSession.Meta.BankedSalvage : 0L;
            RequestBuyBackServerRpc(banked);
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server)]
        void RequestBuyBackServerRpc(long reportedBanked)
        {
            if (!IsServer) return;
            if (_game == null || !_game.TryNetBuyBack(this, reportedBanked)) return;
            SpendBankedClientRpc(HatchBank.BuyBackCost);
            ApplyRejoin();
            Log.Event("Demo", "buyback seat=" + Slot.Value + " cost=" + HatchBank.BuyBackCost);
        }

        [Rpc(SendTo.Owner)]
        void SpendBankedClientRpc(long amount)
        {
            if (amount <= 0L) return;
            var meta = GameSession.Meta;
            if (meta == null) return;
            if (meta.SpendSalvage(amount)) return;
            Log.Warn(Tag, "buy-back spend failed on owner Meta amount=" + amount);
        }
        #endregion
    }
}
