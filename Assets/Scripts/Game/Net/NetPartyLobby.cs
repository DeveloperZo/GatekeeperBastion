using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: shared-Ship ready gate and party EMBARK countdown.
    /// Host-spawned with the session. Distinct from <see cref="NetPlayerAvatar.RequestRespawn"/>
    /// (a downed seat's paid return).</summary>
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

        /// <summary>The shared realm this session is standing in, for a guest's chooser to
        /// remember: realm id, the OWNER's store player id, and the display name. Empty until
        /// the host's share upload lands, and forever on LAN — a guest treats empty as
        /// "nothing to remember" (Docs/Milestones — a realm you can open while its owner is
        /// offline).</summary>
        public NetworkVariable<Unity.Collections.FixedString64Bytes> SharedRealmId =
            new NetworkVariable<Unity.Collections.FixedString64Bytes>(
                default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<Unity.Collections.FixedString64Bytes> SharedRealmOwner =
            new NetworkVariable<Unity.Collections.FixedString64Bytes>(
                default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<Unity.Collections.FixedString64Bytes> SharedRealmName =
            new NetworkVariable<Unity.Collections.FixedString64Bytes>(
                default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>True while the party countdown is running.</summary>
        public bool IsCountingDown => IsSpawned && CountingDown.Value;
        #endregion

        #region Unity Lifecycle
        public override void OnNetworkDespawn()
        {
            if (!IsServer)
                SharedRealmOwner.OnValueChanged -= OnSharedRealmChanged;
            if (_session != null)
                _session.UnregisterLobby(this);
            base.OnNetworkDespawn();
        }

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
            if (!IsServer)
            {
                // A guest remembers the realm the moment the host's share lands — which may
                // be after this spawn, so the change is watched, not just the spawn value.
                SharedRealmOwner.OnValueChanged += OnSharedRealmChanged;
                RememberIfShared();
            }
            Log.Event(Tag, "party lobby spawned role=" + (IsServer ? "host" : "client"));
        }

        void OnSharedRealmChanged(
            Unity.Collections.FixedString64Bytes before, Unity.Collections.FixedString64Bytes after)
        {
            RememberIfShared();
        }

        void RememberIfShared()
        {
            if (IsServer) return;
            RealmShareService.RememberJoinedRealm(
                SharedRealmId.Value.ToString(),
                SharedRealmOwner.Value.ToString(),
                SharedRealmName.Value.ToString());
        }

        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            PublishSharedRealm();
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
        /// <summary>Host: put the shared realm's identity on the wire once the share upload
        /// has landed. Cheap once set; stays empty for LAN and unshared sessions.</summary>
        void PublishSharedRealm()
        {
            if (!SharedRealmId.Value.IsEmpty) return;
            if (!RealmShareService.Holding) return;
            var meta = GameSession.Meta;
            if (meta == null || meta.Realm == null) return;
            if (RealmShareService.HeldRealmId != meta.Realm.Id) return;
            // The realm's OWNER, not this machine — a member can host a borrowed realm, and
            // a guest's later reads seed from the owner's store slot.
            string owner = RealmShareService.HeldOwnerId;
            if (string.IsNullOrEmpty(owner)) return;
            SharedRealmId.Value = meta.Realm.Id;
            SharedRealmOwner.Value = owner;
            SharedRealmName.Value = string.IsNullOrEmpty(meta.Realm.Name) ? "" : meta.Realm.Name;
            Log.Event(Tag, "shared realm on the wire — " + meta.Realm.Id + " of " + owner);
        }

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
            // Joins are refused while the countdown runs, so the seat count the party
            // embarks with is the one assigned here.
            ShipHatchAssignment.Recompute("embark countdown");
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
