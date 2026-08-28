using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): guest respawn gate. Split so
    /// GameController.cs stays under cap. The rule and the debit live sim-side
    /// (RunSim.TryRespawn) — a seat pays out of its OWN Unbanked, so the guest's slot is
    /// the purse and the realm's bank is never touched; this file is only the seat checks
    /// no sim type can see.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Server: may this waiting guest pay and rejoin. True debits the REALM'S
        /// BANK (Zo, 2026-08-25 — one pot, and a seat that never staked anything must still
        /// be able to come back) and counts the respawn; false leaves everything unchanged
        /// with the refusal logged. The host's own seat never comes through here — it
        /// respawns at the DOWN overlay — and a late arrival follows the party for
        /// free instead.</summary>
        public bool TryNetRespawn(NetPlayerAvatar from, out long cost)
        {
            cost = 0L;
            if (_net != null && _net.IsClientOnly) return false;
            if (from == null || Sim == null)
            {
                Log.Event("Demo", "respawn refused — not waiting");
                return false;
            }
            if (from.IsOwner)
            {
                Log.Event("Demo", "respawn refused — the host respawns at the DOWN overlay");
                return false;
            }
            if (!from.WaitingOnShip.Value)
            {
                Log.Event("Demo", "respawn refused — not waiting");
                return false;
            }
            if (from.AwaitingFollow.Value)
            {
                Log.Event("Demo", "respawn refused — follow the party instead");
                return false;
            }
            return Sim.TryRespawn(from.Slot.Value, out cost);
        }
        #endregion
    }
}
