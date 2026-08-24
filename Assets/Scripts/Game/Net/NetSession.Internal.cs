using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST / CLIENT (partial of NetSession): start/join wiring, prefab register,
    /// connection hooks. Split so NetSession.cs stays under cap.</summary>
    public sealed partial class NetSession
    {
        #region Private
        bool CanStart(string op)
        {
            if (_nm == null)
            {
                Log.Warn(Tag, op + " refused — no NetworkManager.");
                return false;
            }
            if (_nm.IsListening)
            {
                Log.Warn(Tag, op + " refused — session already active.");
                return false;
            }
            return true;
        }

        void ArmNetwork()
        {
            if (!_nm.enabled) _nm.enabled = true;
            _nm.NetworkConfig.EnableSceneManagement = true;
            EnsurePrefabsRegistered();
        }

        bool FinishHostStart()
        {
            ArmNetwork();
            if (!_nm.StartHost())
            {
                Log.Warn(Tag, "StartHost refused — NetworkManager.StartHost returned false.");
                return false;
            }
            HookSceneEvents();
            return true;
        }

        bool FinishClientStart()
        {
            ArmNetwork();
            // Who is joining, for the shared-realm roster: the signed-in player id (empty on
            // LAN, where nobody signed in) and a display name. Data for the host's approval
            // hook — identity, never authorization; admission is decided host-side.
            _nm.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(
                JoinIdentityHeader + "\n" + RelayJoin.SignedInPlayerId + "\n" + LocalBoarderName());
            if (!_nm.StartClient())
            {
                Log.Warn(Tag, "StartClient refused — NetworkManager.StartClient returned false.");
                RefuseJoin("StartClient returned false");
                return false;
            }
            HookSceneEvents();
            return true;
        }

        const string JoinIdentityHeader = "gkb-join-1";

        static string LocalBoarderName()
        {
            var meta = GameSession.Meta;
            return meta != null && meta.Boarder != null ? meta.Boarder.Name : "";
        }

        void HookSceneEvents()
        {
            if (_nm == null || _nm.SceneManager == null) return;
            _nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            _nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }

        void OnLoadEventCompleted(
            string sceneName,
            LoadSceneMode mode,
            System.Collections.Generic.List<ulong> completed,
            System.Collections.Generic.List<ulong> timedOut)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (!SceneFlow.IsShipHub(sceneName)) return;
            _lobbyShipLogged = false;
            if (_nm != null)
                TryLogLobbyShip(_nm.LocalClientId);
        }

        void ClearLocalGatekeepers()
        {
            if (_game == null || _game.Spawner == null) return;
            int wiped = _game.Spawner.DespawnAllLive();
            if (wiped > 0)
                Log.Event(Tag, "client starting — cleared " + wiped + " local gatekeepers");
        }

        void BindRelayTransport()
        {
            if (_transport == null)
                _transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (_transport == null)
            {
                Log.Warn(Tag, "no UnityTransport — cannot bind Relay");
                return;
            }
            if (_nm != null && _nm.NetworkConfig.NetworkTransport == null)
                _nm.NetworkConfig.NetworkTransport = _transport;
            _transport.ConnectTimeoutMS = ConnectTimeoutMs;
            RelayJoin.Apply(_transport, _pendingRelay);
        }

        void ConfigureTransport(string address, ushort port, string listenAddress = null)
        {
            if (_transport == null)
                _transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (_transport == null)
            {
                Log.Warn(Tag, "no UnityTransport — cannot bind " + address + ":" + port);
                return;
            }
            if (_nm != null && _nm.NetworkConfig.NetworkTransport == null)
                _nm.NetworkConfig.NetworkTransport = _transport;
            _transport.ConnectTimeoutMS = ConnectTimeoutMs;
            if (string.IsNullOrEmpty(listenAddress))
                _transport.SetConnectionData(address, port);
            else
                _transport.SetConnectionData(address, port, listenAddress);
        }

        void RefuseJoin(string reason)
        {
            string address = string.IsNullOrEmpty(_connectAddress)
                ? "?"
                : _connectAddress + ":" + _connectPort;
            Log.Event(Tag, "join refused reason=" + reason + " address=" + address);
            NetJoinIntent.LastError = "Could not reach " + address + " — " + reason;
            if (!_menuBootstrap && _game == null) return;
            if (_returningToMenu) return;
            _returningToMenu = true;
            SceneFlow.LoadMainMenu();
        }

        void EnsurePrefabsRegistered()
        {
            RegisterPrefab(_avatarPrefab);
            RegisterPrefab(_mirrorPrefab);
            RegisterPrefab(_gatekeeperPrefab);
            RegisterPrefab(_hunterPrefab);
            RegisterPrefab(_thiefPrefab);
            RegisterPrefab(_cloakerPrefab);
            RegisterPrefab(_decoyPrefab);
        }

        void RegisterPrefab(GameObject prefab)
        {
            if (prefab == null || _nm == null) return;
            var prefabs = _nm.NetworkConfig.Prefabs;
            if (prefabs == null) return;
            if (prefabs.Contains(prefab)) return;
            prefabs.Add(new NetworkPrefab { Prefab = prefab });
        }

        void LogSessionSnapshot(string head)
        {
            var sim = _game != null ? _game.Sim : null;
            int wave = sim != null ? sim.CurrentWaveIndex : 0;
            string assault = sim != null ? sim.Assault.ToString() : "none";
            int alive = _game != null ? _game.EnemiesAlive : 0;
            Log.Event(Tag, head
                + " wave=" + wave
                + " assault=" + assault
                + " alive=" + alive
                + " netAlive=" + CountNetworkSpawned());
        }

        int CountNetworkSpawned()
        {
            if (_game == null || _game.Spawner == null) return 0;
            var live = _game.Spawner.Live;
            int n = 0;
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                var net = gk.GetComponent<NetworkObject>();
                if (net != null && net.IsSpawned) n++;
            }
            return n;
        }

        void SpawnSessionObjects()
        {
            SpawnMirror();
        }

        void SpawnMirror()
        {
            if (_mirrorPrefab == null)
            {
                Log.Warn(Tag, "StartHost — no NetRunMirror prefab assigned.");
                return;
            }

            if (SpawnUnparentedMirror(_mirrorPrefab) == null)
                Log.Warn(Tag, "StartHost — NetRunMirror prefab has no NetworkObject.");
        }

        /// <summary>Who each pending/connected client says it is, keyed by client id:
        /// [playerId, boarderName]. Written at approval, consumed at connect, dropped at
        /// disconnect. Identity only — admission decisions live in RealmShareService.</summary>
        readonly System.Collections.Generic.Dictionary<ulong, string[]> _joinIdentities =
            new System.Collections.Generic.Dictionary<ulong, string[]>();

        void OnConnectionApproval(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (_lobby != null && _lobby.IsCountingDown)
            {
                response.Approved = false;
                response.Reason = "party embark countdown";
                Log.Event(Tag, "join refused — party embark countdown");
                return;
            }
            RememberJoinIdentity(request.ClientNetworkId, request.Payload);
            response.Approved = true;
            response.CreatePlayerObject = true;
        }

        void RememberJoinIdentity(ulong clientId, byte[] payload)
        {
            _joinIdentities.Remove(clientId);
            if (payload == null || payload.Length == 0 || payload.Length > 512) return;
            string text;
            try { text = System.Text.Encoding.UTF8.GetString(payload); }
            catch (System.ArgumentException) { return; }
            string[] parts = text.Split('\n');
            if (parts.Length < 3 || parts[0] != JoinIdentityHeader) return;
            _joinIdentities[clientId] = new[] { parts[1], parts[2] };
        }

        void OnClientConnected(ulong clientId)
        {
            if (_nm != null && !_nm.IsServer && clientId == _nm.LocalClientId)
                _becamePeer = true;
            if (_nm != null && _nm.IsServer
                && _nm.ConnectedClients.TryGetValue(clientId, out var client)
                && client.PlayerObject != null)
            {
                var verbs = client.PlayerObject.GetComponent<NetVerbs>();
                if (verbs != null)
                    verbs.Bind(_game);
                var avatar = client.PlayerObject.GetComponent<NetPlayerAvatar>();
                if (avatar != null)
                    avatar.Bind(_game);
            }
            if (_nm == null) return;
            TryLogLobbyShip(clientId);
            if (_nm.IsServer && clientId == _nm.LocalClientId)
                return;
            if (_nm.IsServer)
            {
                LogSessionSnapshot("connected clientId=" + clientId
                    + " localId=" + _nm.LocalClientId
                    + " role=host");
                if (RunSeats.HasRoster)
                {
                    // The party is already out. This seat waits on the Ship and is worth no
                    // door and no share of the pot until it follows them in at a break, so
                    // the assignment is deliberately left where it is.
                    Log.Event(Tag, "joined mid-run — waiting on the Ship clientId=" + clientId);
                    AdmitIdentifiedGuest(clientId);
                    return;
                }
                ShipHatchAssignment.Recompute("guest joined");
                AdmitIdentifiedGuest(clientId);
                return;
            }
            Log.Event(Tag, "connected clientId=" + clientId
                + " localId=" + _nm.LocalClientId
                + " role=client");
        }

        void TryLogLobbyShip(ulong clientId)
        {
            if (_lobbyShipLogged) return;
            if (SceneFlow.ActiveSceneName != SceneFlow.ShipCoop) return;
            if (_nm == null) return;
            if (clientId != _nm.LocalClientId) return;
            _lobbyShipLogged = true;
            Log.Event(Tag, "lobby ship clientId=" + clientId);
        }

        /// <summary>A guest that presented a signed-in identity becomes a member of the
        /// session's shared realm. Quietly nothing for LAN guests and unshared sessions —
        /// there is no roster for them to be on.</summary>
        void AdmitIdentifiedGuest(ulong clientId)
        {
            if (!_joinIdentities.TryGetValue(clientId, out var who)) return;
            if (string.IsNullOrEmpty(who[0])) return;
            RealmShareService.AdmitGuest(who[0], who[1]);
        }

        void OnClientDisconnect(ulong clientId)
        {
            _joinIdentities.Remove(clientId);
            if (_nm != null && _nm.IsServer)
            {
                if (_lobby != null)
                    _lobby.ServerNotifyGuestLeft(clientId);
                if (RunSeats.HasRoster)
                {
                    // Sealing a hatch the moment somebody drops would be a free save in the
                    // middle of a wave. The seat is held and the break applies it.
                    RunSeats.NoteLeft(clientId);
                    return;
                }
                // The leaver can still sit in the connected list inside this callback,
                // so it is excluded by id rather than trusted to be gone.
                ShipHatchAssignment.Recompute("guest left", clientId);
                return;
            }
            if (_nm == null || _nm.IsServer) return;
            if (clientId != _nm.LocalClientId) return;
            if (_becamePeer) return;
            string reason = _nm.DisconnectReason;
            if (string.IsNullOrEmpty(reason)) reason = "timeout";
            RefuseJoin(reason);
        }

        void OnClientStopped(bool wasHost)
        {
            _mirror = null;
            _lobby = null;
            _lobbyShipLogged = false;
            Log.Event(Tag, "session stopped");
            if (wasHost) return;
            if (_returningToMenu) return;
            if (_becamePeer && !_leavingOnPurpose)
            {
                Log.Event(Tag, "host left");
                NetJoinIntent.LastError = "Host left the session.";
            }
            if (!_menuBootstrap && _game == null) return;
            _returningToMenu = true;
            SceneFlow.LoadMainMenu();
        }
        #endregion
    }
}
