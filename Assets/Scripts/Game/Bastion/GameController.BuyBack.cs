using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): guest buy-back gate.
    /// Split so GameController.cs stays under cap. The cost comes out of the realm's pot,
    /// which in co-op is the host's — a guest is playing in somebody else's world and it is
    /// that world's salvage that pays (Docs/Milestones — the realm and the Boarder).</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Server: may this waiting guest pay and rejoin. Checks the realm's pot
        /// and debits it on success, so the test and the spend cannot drift apart. Logs a
        /// named refuse. Does not mutate wallets or Unbanked.</summary>
        public bool TryNetBuyBack(NetPlayerAvatar from)
        {
            if (_net != null && _net.IsClientOnly) return false;
            if (from == null || Sim == null)
            {
                Log.Event("Demo", "buyback refused — not waiting");
                return false;
            }
            var meta = GameSession.Meta;
            long pot = meta != null ? meta.BankedSalvage : 0L;
            string reason = HatchBank.BuyBackRefuse(
                from.IsOwner,
                from.WaitingOnShip.Value,
                Sim.Mode,
                Sim.Assault,
                pot);
            if (reason != null)
            {
                Log.Event("Demo", "buyback refused — " + reason);
                return false;
            }
            if (meta == null || !meta.SpendSalvage(HatchBank.BuyBackCost))
            {
                Log.Event("Demo", "buyback refused — realm pot would not pay");
                return false;
            }
            return true;
        }
        #endregion
    }
}
