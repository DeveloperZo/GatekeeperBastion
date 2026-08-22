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
            {
                RunSeats.BeginRun(NetworkManager.Singleton);
            }
            else if (!tethered && RunSeats.IsLive)
            {
                // Clear the waiting flags BEFORE the roster closes. A seat that arrived mid-run
                // and never got a break carries AwaitingFollow and WaitingOnShip on the server;
                // nothing else ever clears them, so at the next embark it would be admitted by
                // BeginRun — counted for waves and paid a share — while its owner sat in the
                // Ship overlay unable to move, pressing a follow button that refused because it
                // was already seated.
                ReleaseWaitingSeats();
                RunSeats.EndRun();
            }

            // Any Prep is a boundary, not only the one an Assault just ended on: a seat that
            // leaves during Prep should go at that break rather than wait out a whole wave.
            if (!RunSeats.IsLive) return;
            if (Sim.Assault != AssaultState.Prep) return;
            if (!RunSeats.HasPending) return;
            if (!RunSeats.ApplyBoundary()) return;

            ShipHatchAssignment.Recompute("seat left at the break");
            RefreshPlayerCount();
        }

        /// <summary>Drop the mid-run wait from every seat still carrying it. The run is over, so
        /// there is nothing left to follow into — and a flag that outlives its run is worse than
        /// one that was never set.</summary>
        void ReleaseWaitingSeats()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var po = clients[i].PlayerObject;
                if (po == null) continue;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar != null) avatar.ServerReleaseFollowWait();
            }
        }
        #endregion
    }
}
