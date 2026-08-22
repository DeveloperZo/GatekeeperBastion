using GatekeepersProtocol.Sim;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): keeps <see cref="RunSeats"/> in step
    /// with the run, and applies held seat departures at a Prep boundary. Split so
    /// GameController.cs does not grow; it is a separate concern from the wallet split that
    /// reads the same count.</summary>
    public sealed partial class GameController
    {
        #region Private
        /// <summary>Open and close the roster with the run, then flush anything held.
        /// Called once per host tick, before the count is read.</summary>
        void TickSeatRoster()
        {
            if (Sim == null) return;
            if (_net != null && _net.IsClientOnly) return;

            bool tethered = Sim.Phase == RunPhase.Tethered;
            if (tethered && !RunSeats.IsLive)
                RunSeats.BeginRun(NetworkManager.Singleton);
            else if (!tethered && RunSeats.IsLive)
                RunSeats.EndRun();

            // Any Prep is a boundary, not only the one an Assault just ended on: a seat that
            // leaves during Prep should go at that break rather than wait out a whole wave.
            if (!RunSeats.IsLive) return;
            if (Sim.Assault != AssaultState.Prep) return;
            if (!RunSeats.HasPending) return;
            if (!RunSeats.ApplyBoundary()) return;

            ShipHatchAssignment.Recompute("seat left at the break");
            RefreshPlayerCount();
        }
        #endregion
    }
}
