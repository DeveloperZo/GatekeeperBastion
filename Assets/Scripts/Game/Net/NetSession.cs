using GatekeeperBastion.Util;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST / CLIENT: Bastion-scene session. Starts a host or joins over
    /// UnityTransport. Scene management stays off — both instances load Bastion themselves.
    /// No statics; consumers take this through a serialized GameController.Net reference.
    /// Menu host/join arrives via <see cref="NetJoinIntent"/> consumed in Awake.</summary>
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    [DefaultExecutionOrder(-100)]
    public sealed class NetSession : MonoBehaviour
    {
        #region Constants
        const string Tag = "Net";
        const string BindAllAddress = "0.0.0.0";
        const string LoopbackAddress = "127.0.0.1";
        const ushort DefaultPort = 7777;
        const int ConnectTimeoutMs = 4000;
        #endregion

        #region Inspector
        [SerializeField, Tooltip("Bastion GameController. Authored in the scene.")]
        GameController _game;

        [SerializeField, Tooltip("Spawned on host start; replicates phase/wave/alive.")]
        GameObject _mirrorPrefab;

        [SerializeField, Tooltip("NGO default player prefab — puppet capsule, not the local rig.")]
        GameObject _avatarPrefab;

        [SerializeField] GameObject _gatekeeperPrefab;
        [SerializeField] GameObject _hunterPrefab;
        [SerializeField] GameObject _ramPrefab;

        [SerializeField, Tooltip("Networked decoy prop. Spawned by the host bay.")]
        GameObject _decoyPrefab;
        #endregion

        #region References
        NetworkManager _nm;
        UnityTransport _transport;
        NetRunMirror _mirror;
        string _connectAddress;
        ushort _connectPort;
        bool _becamePeer;
        bool _returningToMenu;
        NetJoinIntent.Kind _pendingKind;
        string _pendingAddress;
        ushort _pendingPort;
        #endregion

        #region Properties
        /// <summary>The wired GameController. Null in fixture tests that spawn no scene session.</summary>
        public GameController Game => _game;

        /// <summary>The live mirror after host spawn / client clone. Null until spawned.</summary>
        public NetRunMirror Mirror => _mirror;

        /// <summary>True while this instance's NetworkManager is listening.</summary>
        public bool Active => _nm != null && _nm.IsListening;

        /// <summary>True on a connected client that is not also the server.</summary>
        public bool IsClientOnly => Active && _nm != null && _nm.IsClient && !_nm.IsServer;

        /// <summary>Host-spawned decoy prefab. Null in solo when unwired.</summary>
        public GameObject DecoyPrefab => _decoyPrefab;

        /// <summary>Owner avatar verbs, or null until the player object exists.</summary>
        public NetVerbs LocalVerbs
        {
            get
            {
                if (_nm == null || !_nm.IsListening) return null;
                if (!_nm.ConnectedClients.TryGetValue(_nm.LocalClientId, out var client))
                    return null;
                var po = client.PlayerObject;
                if (po == null) return null;
                var verbs = po.GetComponent<NetVerbs>();
                if (verbs != null)
                    verbs.Bind(_game);
                return verbs;
            }
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _nm = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
            if (_nm == null)
            {
                Log.Error(Tag, "NetworkManager missing on Net; session disabled.");
                return;
            }

            _nm.NetworkConfig.EnableSceneManagement = false;
            if (_nm.NetworkConfig.NetworkTransport == null && _transport != null)
                _nm.NetworkConfig.NetworkTransport = _transport;
            if (_avatarPrefab != null)
                _nm.NetworkConfig.PlayerPrefab = _avatarPrefab;

            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnect;
            _nm.OnClientStopped += OnClientStopped;

            // Consume here so a second Bastion load is solo. StartHost/StartClient wait for
            // Start — NetworkManager.Awake has not run yet when this type is order -100.
            _pendingKind = NetJoinIntent.Consume();
            _pendingAddress = NetJoinIntent.Address;
            _pendingPort = NetJoinIntent.Port;
        }

        void Start()
        {
            if (_pendingKind == NetJoinIntent.Kind.Host)
                StartHost();
            else if (_pendingKind == NetJoinIntent.Kind.Join)
                StartClient(_pendingAddress, _pendingPort);
            _pendingKind = NetJoinIntent.Kind.None;
        }

        void OnDestroy()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientDisconnectCallback -= OnClientDisconnect;
                _nm.OnClientStopped -= OnClientStopped;
            }
        }
        #endregion

        #region Public API
        /// <summary>Start a listen-server on all interfaces (default 0.0.0.0:7777) and spawn
        /// the run mirror. 0.0.0.0 still accepts loopback, so MPPM F11 keeps working.</summary>
        public void StartHost(string bindAddress = BindAllAddress, ushort port = DefaultPort)
        {
            if (_nm == null)
            {
                Log.Warn(Tag, "StartHost refused — no NetworkManager.");
                return;
            }
            if (_nm.IsListening)
            {
                Log.Warn(Tag, "StartHost refused — session already active.");
                return;
            }

            // Listen on bindAddress (0.0.0.0 = all NICs). Advertise loopback so
            // SetConnectionData is not given 0.0.0.0 as a connect target — that address
            // is what the failed same-machine join typed (log 2026-08-15 09:01:41).
            string advertise = bindAddress == BindAllAddress ? LoopbackAddress : bindAddress;
            ConfigureTransport(advertise, port, bindAddress);
            if (!_nm.enabled) _nm.enabled = true;
            EnsurePrefabsRegistered();
            if (!_nm.StartHost())
            {
                Log.Warn(Tag, "StartHost refused — NetworkManager.StartHost returned false.");
                return;
            }

            LogSessionSnapshot("host started localId=" + _nm.LocalClientId
                + " bind=" + bindAddress + ":" + port
                + " join=" + advertise + ":" + port);
            SpawnMirror();
        }

        /// <summary>Join address:port. The host owns the run; this instance must not board.
        /// F11 uses 127.0.0.1:7777 as the editor/dev fallback.</summary>
        public void StartClient(string address, ushort port = DefaultPort)
        {
            if (_nm == null)
            {
                Log.Warn(Tag, "StartClient refused — no NetworkManager.");
                return;
            }
            if (_nm.IsListening)
            {
                Log.Warn(Tag, "StartClient refused — session already active.");
                return;
            }

            if (!NetJoinIntent.IsJoinableAddress(address))
            {
                _connectAddress = address;
                _connectPort = port;
                RefuseJoin("0.0.0.0 is a listen address — use 127.0.0.1 on this machine or the host LAN IPv4");
                return;
            }

            _connectAddress = address;
            _connectPort = port;
            _becamePeer = false;
            ConfigureTransport(address, port);
            if (!_nm.enabled) _nm.enabled = true;
            EnsurePrefabsRegistered();
            if (!_nm.StartClient())
            {
                Log.Warn(Tag, "StartClient refused — NetworkManager.StartClient returned false.");
                RefuseJoin("StartClient returned false");
                return;
            }

            Log.Event(Tag, "client starting — host owns the run address=" + address + ":" + port);
            if (_game != null && _game.Spawner != null)
            {
                int wiped = _game.Spawner.DespawnAllLive();
                if (wiped > 0)
                    Log.Event(Tag, "client starting — cleared " + wiped + " local gatekeepers");
            }
        }

        /// <summary>Client clones register here from OnNetworkSpawn. Null-safe for fixture tests
        /// that spawn a mirror with no scene session.</summary>
        public void RegisterMirror(NetRunMirror mirror)
        {
            _mirror = mirror;
            if (mirror != null)
                mirror.Bind(_game);
        }

        /// <summary>Drop the session. A non-host then loads MainMenu from OnClientStopped.
        /// Host ReturnToShip loads Ship itself after this returns.</summary>
        public void ShutdownSession()
        {
            _mirror = null;
            if (_nm == null) return;
            if (!_nm.IsListening) return;
            Log.Event(Tag, "session shutdown");
            _nm.Shutdown();
        }
        #endregion

        #region Private
        void ConfigureTransport(string address, ushort port, string listenAddress = null)
        {
            if (_transport == null) _transport = GetComponent<UnityTransport>();
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
            // Fixtures spawn NetSession with no GameController; do not dump the test into MainMenu.
            if (_game == null) return;
            NetJoinIntent.LastError = "Could not reach " + address + " — " + reason;
            if (_returningToMenu) return;
            _returningToMenu = true;
            SceneFlow.LoadMainMenu();
        }

        /// <summary>After NetworkManager.Awake has built the authored list, add any missing
        /// prefab. Calling this from Awake duplicates GlobalObjectIdHash against the list.</summary>
        void EnsurePrefabsRegistered()
        {
            RegisterPrefab(_avatarPrefab);
            RegisterPrefab(_mirrorPrefab);
            RegisterPrefab(_gatekeeperPrefab);
            RegisterPrefab(_hunterPrefab);
            RegisterPrefab(_ramPrefab);
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

        /// <summary>Wave / assault / alive at this instant. netAlive is how many live
        /// bodies are actually Spawn()'d — 0 with alive &gt; 0 means the session started
        /// after the wave, so the client will not see those actors.</summary>
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

        void SpawnMirror()
        {
            if (_mirrorPrefab == null)
            {
                Log.Warn(Tag, "StartHost — no NetRunMirror prefab assigned.");
                return;
            }

            var go = Instantiate(_mirrorPrefab);
            var netObj = go.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Log.Warn(Tag, "StartHost — NetRunMirror prefab has no NetworkObject.");
                Destroy(go);
                return;
            }
            netObj.Spawn(true);
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
            if (_nm.IsServer && clientId == _nm.LocalClientId)
                return;
            if (_nm.IsServer)
            {
                LogSessionSnapshot("connected clientId=" + clientId
                    + " localId=" + _nm.LocalClientId
                    + " role=host");
                return;
            }
            Log.Event(Tag, "connected clientId=" + clientId
                + " localId=" + _nm.LocalClientId
                + " role=client");
        }

        void OnClientDisconnect(ulong clientId)
        {
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
            Log.Event(Tag, "session stopped");
            if (wasHost) return;
            if (_returningToMenu) return;
            if (_game == null) return;
            SceneFlow.LoadMainMenu();
        }
        #endregion
    }
}
