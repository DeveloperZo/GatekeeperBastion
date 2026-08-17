using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): connected-player count into the sim,
    /// guest convert RPCs, guest DeepestWaveReached. Split so GameController.cs does not grow.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Host: NGO connected count into <see cref="RunSim.SetPlayerCount"/>. Solo is 1.</summary>
        public void RefreshPlayerCount()
        {
            if (Sim == null) return;
            if (_net != null && _net.IsClientOnly) return;
            int n = 1;
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer && nm.IsListening)
            {
                n = nm.ConnectedClientsList.Count;
                if (n < 1) n = 1;
                if (n > WaveDirector.PlayerCapacity) n = WaveDirector.PlayerCapacity;
            }
            if (n == Sim.PlayerCount) return;
            Sim.SetPlayerCount(n);
            Log.Event("Net", "playerCount=" + n);
        }
        #endregion

        #region Private
        void OnGuestConverted(int slot, long credited)
        {
            if (credited <= 0L) return;
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var po = clients[i].PlayerObject;
                if (po == null) continue;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar == null || avatar.Slot.Value != slot) continue;
                avatar.CreditBanked(credited);
                return;
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
