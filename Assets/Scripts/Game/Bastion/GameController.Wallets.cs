using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): connected-player count into the sim,
    /// and each seat's depth for the per-seat bank cap. Split so GameController.cs does not
    /// grow.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Host: seats playing into <see cref="RunSim.SetPlayerCount"/>. Solo is 1.
        /// While a run is under way the seat count is <see cref="RunSeats"/>, not the connected
        /// count — a late arrival is connected while it waits on the Ship, and a seat that has
        /// gone is still counted until the break. Before a run the two are the same thing and
        /// the connected list is read directly, which is what the lobby has always done.</summary>
        public void RefreshPlayerCount()
        {
            if (Sim == null) return;
            if (_net != null && _net.IsClientOnly) return;
            int n = 1;
            var nm = NetworkManager.Singleton;
            if (RunSeats.HasRoster)
            {
                n = RunSeats.Count;
            }
            else if (nm != null && nm.IsServer && nm.IsListening)
            {
                n = nm.ConnectedClientsList.Count;
                if (n < 1) n = 1;
                if (n > WaveDirector.PlayerCapacity) n = WaveDirector.PlayerCapacity;
            }
            if (n != Sim.PlayerCount)
            {
                Sim.SetPlayerCount(n);
                Log.Event("Net", "playerCount=" + n);
            }

            MuteSeatSpawns();
        }
        #endregion

        #region Private
        /// <summary>Two-seat mute: minSeats=3 closets (S7–S12) go inactive. Hatch sealed
        /// state stays <see cref="ShipHatches"/> / <see cref="ShipHatchDoor"/>.</summary>
        void MuteSeatSpawns()
        {
            int seats = Sim.PlayerCount;
            SpawnRoom[] closets = UnityEngine.Object.FindObjectsByType<SpawnRoom>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < closets.Length; i++)
            {
                SpawnRoom closet = closets[i];
                if (closet == null)
                    continue;
                bool live = BastionSeatSpawns.IsLive(closet.MinSeats, seats);
                if (closet.gameObject.activeSelf == live)
                    continue;
                closet.gameObject.SetActive(live);
            }
        }

        void PushGuestProgress()
        {
            if (Sim == null) return;
            int deepest = Sim.CurrentWaveIndex;
            if (deepest < 1) return;
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var po = clients[i].PlayerObject;
                if (po == null) continue;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar == null) continue;
                avatar.PushProgress(deepest);
            }
        }
        #endregion
    }
}
