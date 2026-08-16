using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: shared-Ship ready gate and party EMBARK countdown.
    /// Host-spawned with the session. Distinct from <see cref="NetPlayerAvatar.RequestEmbark"/>
    /// (Hatch-bank get-up).</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetPartyLobby : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        public const float CountdownSeconds = 5f;
        #endregion

        #region State
        NetSession _session;
        #endregion

        #region Properties
        public NetworkVariable<bool> CountingDown = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> CountdownRemaining = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> PendingStartWave = new NetworkVariable<int>(
            1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>True while the party countdown is running.</summary>
        public bool IsCountingDown => IsSpawned && CountingDown.Value;
        #endregion

        #region Unity Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            var nm = NetworkManager;
            if (nm != null)
            {
                var session = nm.GetComponent<NetSession>();
                if (session != null)
                {
                    _session = session;
                    session.RegisterLobby(this);
                }
            }
            Log.Event(Tag, "party lobby spawned role=" + (IsServer ? "host" : "client"));
        }

        public override void OnNetworkDespawn()
        {
            if (_session != null)
                _session.UnregisterLobby(this);
            base.OnNetworkDespawn();
        }

        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            if (!CountingDown.Value) return;
            float next = CountdownRemaining.Value - Time.unscaledDeltaTime;
            if (next > 0f)
            {
                CountdownRemaining.Value = next;
                return;
            }
            CountdownRemaining.Value = 0f;
            CountingDown.Value = false;
            int wave = PendingStartWave.Value;
            ClearAllReady();
            if (_session != null)
                _session.ServerLoadPartyBastion(wave);
        }
        #endregion

        #region Public API
        /// <summary>Owner toggle for lobby Ready. Host does not need Ready.</summary>
        public void SetReady(bool ready)
        {
            SetReadyServerRpc(ready);
        }

        /// <summary>Party lead commit. Guests must refuse at the call site before this.</summary>
        public void RequestPartyEmbark(int startWave)
        {
            RequestPartyEmbarkServerRpc(startWave);
        }

        /// <summary>Any seat aborts countdown and clears Ready.</summary>
        public void CancelCountdown()
        {
            CancelCountdownServerRpc();
        }

        /// <summary>Guest (or any seat): ask the lead to party-load Ship after the end screen.</summary>
        public void RequestPartyReturnToShip()
        {
            RequestPartyReturnServerRpc();
        }

        /// <summary>Server: guest left — drop Ready quorum and tell the lead.</summary>
        public void ServerNotifyGuestLeft(ulong clientId)
        {
            if (!IsServer) return;
            if (CountingDown.Value)
                AbortCountdown("guest left clientId=" + clientId);
            Log.Event(Tag, "party guest left clientId=" + clientId);
        }
        #endregion

        #region Private
        bool GuestsAllReady()
        {
            var nm = NetworkManager;
            if (nm == null || !nm.IsServer) return false;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                if (client.ClientId == nm.LocalClientId) continue;
                var po = client.PlayerObject;
                if (po == null) return false;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar == null || !avatar.LobbyReady.Value) return false;
            }
            return true;
        }

        void ClearAllReady()
        {
            var nm = NetworkManager;
            if (nm == null || !nm.IsServer) return;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var po = clients[i].PlayerObject;
                if (po == null) continue;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar != null)
                    avatar.ServerSetLobbyReady(false);
            }
        }

        void AbortCountdown(string reason)
        {
            if (!CountingDown.Value) return;
            CountingDown.Value = false;
            CountdownRemaining.Value = 0f;
            ClearAllReady();
            Log.Event(Tag, "party embark cancelled — " + reason);
        }
        #endregion

        #region Rpcs
        [Rpc(SendTo.Server)]
        void SetReadyServerRpc(bool ready, RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            if (CountingDown.Value && !ready)
            {
                AbortCountdown("un-Ready clientId=" + sender);
                return;
            }
            if (CountingDown.Value)
            {
                Log.Event(Tag, "ready refused — countdown active clientId=" + sender);
                return;
            }
            var nm = NetworkManager;
            if (nm == null) return;
            if (!nm.ConnectedClients.TryGetValue(sender, out var client)) return;
            var po = client.PlayerObject;
            if (po == null) return;
            var avatar = po.GetComponent<NetPlayerAvatar>();
            if (avatar == null) return;
            avatar.ServerSetLobbyReady(ready);
            Log.Event(Tag, "ready=" + ready + " clientId=" + sender);
        }

        [Rpc(SendTo.Server)]
        void RequestPartyEmbarkServerRpc(int startWave, RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            var nm = NetworkManager;
            if (nm == null) return;
            if (sender != nm.LocalClientId)
            {
                Log.Event(Tag, "embark refused — not party lead clientId=" + sender);
                return;
            }
            if (CountingDown.Value)
            {
                Log.Event(Tag, "embark refused — countdown already running");
                return;
            }
            if (!GuestsAllReady())
            {
                Log.Event(Tag, "embark refused — guests not Ready");
                return;
            }
            if (startWave < 1) startWave = 1;
            PendingStartWave.Value = startWave;
            CountdownRemaining.Value = CountdownSeconds;
            CountingDown.Value = true;
            Log.Event(Tag, "party embark countdown " + CountdownSeconds.ToString("0.0")
                + "s startWave=" + startWave);
        }

        [Rpc(SendTo.Server)]
        void CancelCountdownServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (!CountingDown.Value)
            {
                Log.Event(Tag, "cancel refused — no countdown");
                return;
            }
            AbortCountdown("cancel clientId=" + rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server)]
        void RequestPartyReturnServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (_session == null)
            {
                var nm = NetworkManager;
                if (nm != null)
                    _session = nm.GetComponent<NetSession>();
            }
            if (_session == null)
            {
                Log.Warn(Tag, "party return refused — no session");
                return;
            }
            Log.Event(Tag, "party return requested clientId=" + rpcParams.Receive.SenderClientId);
            if (!_session.LoadPartyShip())
                Log.Warn(Tag, "party return LoadPartyShip failed");
        }
        #endregion
    }
}
