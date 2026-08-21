using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): guest buy-back gate.
    /// Split so GameController.cs stays under cap. Does not spend host Meta —
    /// the owner's <c>SpendBankedClientRpc</c> debits their own save.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Server: may this waiting guest pay and rejoin. Logs a named
        /// refuse. Does not mutate wallets or Unbanked.</summary>
        public bool TryNetBuyBack(NetPlayerAvatar from, long reportedBanked)
        {
            if (_net != null && _net.IsClientOnly) return false;
            if (from == null || Sim == null)
            {
                Log.Event("Demo", "buyback refused — not waiting");
                return false;
            }
            string reason = HatchBank.BuyBackRefuse(
                from.IsOwner,
                from.WaitingOnShip.Value,
                Sim.Mode,
                Sim.Assault,
                reportedBanked);
            if (reason == null) return true;
            Log.Event("Demo", "buyback refused — " + reason);
            return false;
        }
        #endregion
    }
}
