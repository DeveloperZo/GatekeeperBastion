using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST / CLIENT: DDOL session. Main Menu Host/Join starts this before Ship.
    /// <see cref="NetworkConfig.EnableSceneManagement"/> is true for friends sessions;
    /// solo never creates this object. Additive wait never uses NetworkSceneManager.
    /// Prefab fixtures keep forcing scene management off on their own managers.</summary>
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(Unity.Netcode.Transports.UTP.UnityTransport))]
    [DefaultExecutionOrder(-100)]
    public sealed partial class NetSession : MonoBehaviour
    {
        #region Constants
        const string Tag = "Net";
        const string BindAllAddress = "0.0.0.0";
        const string LoopbackAddress = "127.0.0.1";
        const ushort DefaultPort = 7777;
        const int ConnectTimeoutMs = 10000;
        const string BootstrapResource = "Net/NetSession";
        #endregion

        #region Inspector
        [SerializeField, Tooltip("Bastion GameController. Bound when Bastion loads; null on Ship.")]
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
        Unity.Netcode.Transports.UTP.UnityTransport _transport;
        NetRunMirror _mirror;
        NetPartyLobby _lobby;
        string _connectAddress;
        ushort _connectPort;
        bool _becamePeer;
        bool _returningToMenu;
        bool _leavingOnPurpose;
        bool _lobbyShipLogged;
        bool _menuBootstrap;
        NetJoinIntent.Kind _pendingKind;
        string _pendingAddress;
        ushort _pendingPort;
        RelayJoin.Payload _pendingRelay;
        string _joinCode;
        #endregion

        #region Properties
        /// <summary>The wired GameController. Null on Ship / fixtures without a Bastion host.</summary>
        public GameController Game => _game;

        /// <summary>The live mirror after host spawn / client clone. Null until spawned.</summary>
        public NetRunMirror Mirror => _mirror;

        /// <summary>Party lobby after host spawn. Null until spawned / fixtures without one.</summary>
        public NetPartyLobby Lobby => _lobby;

        /// <summary>True while this instance's NetworkManager is listening.</summary>
        public bool Active => _nm != null && _nm.IsListening;

        /// <summary>True on a connected client that is not also the server.</summary>
        public bool IsClientOnly => Active && _nm != null && _nm.IsClient && !_nm.IsServer;

        /// <summary>True when this seat is the party lead (listen-server host).</summary>
        public bool IsPartyLead => Active && _nm != null && _nm.IsServer;

        /// <summary>Relay join code for this session. Null on LAN / solo.</summary>
        public string JoinCode => _joinCode;

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

        #region Static
        /// <summary>DDOL session on the live NetworkManager, or null when solo / none.</summary>
        public static NetSession FindLive()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return null;
            return nm.GetComponent<NetSession>();
        }

        /// <summary>Instantiate the Resources bootstrap, mark DDOL, return the session.
        /// Refuses a second live session.</summary>
        public static NetSession CreateBootstrap()
        {
            var live = FindLive();
            if (live != null && live.Active)
            {
                Log.Warn(Tag, "CreateBootstrap refused — session already active.");
                return live;
            }
            if (live != null)
                return live;

            var prefab = Resources.Load<GameObject>(BootstrapResource);
            if (prefab == null)
            {
                Log.Error(Tag, "CreateBootstrap refused — Resources/" + BootstrapResource
                    + " missing. Create Prefabs/Net/NetSession and copy under Resources/Net/.");
                return null;
            }
            var go = Instantiate(prefab);
            go.name = "Net";
            DontDestroyOnLoad(go);
            var session = go.GetComponent<NetSession>();
            if (session != null)
                session._menuBootstrap = true;
            return session;
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _nm = GetComponent<NetworkManager>();
            _transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (_nm == null)
            {
                Log.Error(Tag, "NetworkManager missing on Net; session disabled.");
                return;
            }
            if (_nm.NetworkConfig == null)
                _nm.NetworkConfig = new NetworkConfig();

            // Friends session always. PlayMode Net* fixtures use their own managers and
            // force EnableSceneManagement = false there — not this object.
            _nm.NetworkConfig.EnableSceneManagement = true;
            _nm.NetworkConfig.ConnectionApproval = true;
            if (_nm.NetworkConfig.NetworkTransport == null && _transport != null)
                _nm.NetworkConfig.NetworkTransport = _transport;
            if (_avatarPrefab != null)
                _nm.NetworkConfig.PlayerPrefab = _avatarPrefab;

            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnect;
            _nm.OnClientStopped += OnClientStopped;
            _nm.ConnectionApprovalCallback = OnConnectionApproval;

            // CLI / Splash smoke may still set intent; menu Host/Join usually starts directly.
            // Consume so a second load is solo.
            _pendingKind = NetJoinIntent.Consume();
            _pendingAddress = NetJoinIntent.Address;
            _pendingPort = NetJoinIntent.Port;
            _joinCode = _pendingKind != NetJoinIntent.Kind.None ? NetJoinIntent.JoinCode : null;
            _pendingRelay = _pendingKind != NetJoinIntent.Kind.None && NetJoinIntent.HasRelay
                ? NetJoinIntent.Relay
                : default;
        }

        void Start()
        {
            ApplyPendingIntent();
        }

        void OnDestroy()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientDisconnectCallback -= OnClientDisconnect;
                _nm.OnClientStopped -= OnClientStopped;
                if (_nm.ConnectionApprovalCallback == OnConnectionApproval)
                    _nm.ConnectionApprovalCallback = null;
                if (_nm.SceneManager != null)
                    _nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }
        }
        #endregion

        #region Public API
        /// <summary>Rebind Bastion GameController after a networked Bastion load.</summary>
        public void BindGame(GameController game)
        {
            _game = game;
            if (_mirror != null)
                _mirror.Bind(game);
            if (_nm == null || !_nm.IsServer) return;
            foreach (var pair in _nm.ConnectedClients)
            {
                var po = pair.Value.PlayerObject;
                if (po == null) continue;
                var verbs = po.GetComponent<NetVerbs>();
                if (verbs != null) verbs.Bind(game);
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar != null) avatar.Bind(game);
            }
        }

        /// <summary>Consume pending CLI intent into StartHost / StartClient. Idempotent.</summary>
        public void ApplyPendingIntent()
        {
            if (_pendingKind == NetJoinIntent.Kind.Host)
            {
                if (RelayJoin.HasPayload(_pendingRelay))
                    StartHostRelay();
                else
                    StartHost();
            }
            else if (_pendingKind == NetJoinIntent.Kind.Join)
            {
                if (RelayJoin.HasPayload(_pendingRelay))
                    StartClientRelay();
                else
                    StartClient(_pendingAddress, _pendingPort);
            }
            _pendingKind = NetJoinIntent.Kind.None;
        }

        /// <summary>Start a listen-server on all interfaces (default 0.0.0.0:7777) and spawn
        /// the run mirror. Always LAN — Relay host goes through StartHostRelay.</summary>
        public void StartHost(string bindAddress = BindAllAddress, ushort port = DefaultPort)
        {
            if (!CanStart("StartHost")) return;
            string advertise = bindAddress == BindAllAddress ? LoopbackAddress : bindAddress;
            ConfigureTransport(advertise, port, bindAddress);
            if (!FinishHostStart()) return;
            LogSessionSnapshot("host started localId=" + _nm.LocalClientId
                + " bind=" + bindAddress + ":" + port
                + " join=" + advertise + ":" + port);
            SpawnSessionObjects();
        }

        /// <summary>Join address:port. Always LAN — Relay join goes through StartClientRelay.</summary>
        public void StartClient(string address, ushort port = DefaultPort)
        {
            if (!CanStart("StartClient")) return;
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
            if (!FinishClientStart()) return;
            Log.Event(Tag, "client starting — host owns the run address=" + address + ":" + port);
            ClearLocalGatekeepers();
        }

        /// <summary>Relay listen-server. Menu binds allocation before Ship.</summary>
        public void StartHostRelay(RelayJoin.Payload payload, string joinCode)
        {
            _pendingRelay = payload;
            _joinCode = joinCode;
            StartHostRelay();
        }

        /// <summary>Relay client. Menu binds allocation before Ship.</summary>
        public void StartClientRelay(RelayJoin.Payload payload, string joinCode)
        {
            _pendingRelay = payload;
            _joinCode = joinCode;
            StartClientRelay();
        }

        void StartHostRelay()
        {
            if (!CanStart("StartHost")) return;
            BindRelayTransport();
            if (!FinishHostStart()) return;
            LogSessionSnapshot("host started localId=" + _nm.LocalClientId
                + " relay joinCode=" + _joinCode);
            SpawnSessionObjects();
        }

        void StartClientRelay()
        {
            if (!CanStart("StartClient")) return;
            _connectAddress = _joinCode;
            _connectPort = DefaultPort;
            _becamePeer = false;
            BindRelayTransport();
            if (!FinishClientStart()) return;
            Log.Event(Tag, "client starting — host owns the run joinCode=" + _joinCode);
            ClearLocalGatekeepers();
        }

        /// <summary>Host: NetworkSceneManager loads the shared Ship lobby.</summary>
        public bool LoadLobbyShip()
        {
            if (_nm == null || !_nm.IsServer)
            {
                Log.Warn(Tag, "LoadLobbyShip refused — not party lead.");
                return false;
            }
            if (!_nm.IsListening)
            {
                Log.Warn(Tag, "LoadLobbyShip refused — session not listening.");
                return false;
            }
            if (!SceneFlow.CanLoad(SceneFlow.Ship))
            {
                Log.Error(Tag, "LoadLobbyShip refused — Ship not in Build Settings.");
                return false;
            }
            _nm.SceneManager.LoadScene(SceneFlow.Ship, LoadSceneMode.Single);
            Log.Event(Tag, "party load Ship");
            return true;
        }

        /// <summary>Host: NetworkSceneManager loads Bastion for every seat after countdown.</summary>
        public void ServerLoadPartyBastion(int startWave)
        {
            if (_nm == null || !_nm.IsServer) return;
            GameSession.PendingStartWave = startWave;
            if (!SceneFlow.CanLoad(SceneFlow.Bastion))
            {
                Log.Error(Tag, "party Bastion load refused — Bastion not in Build Settings.");
                return;
            }
            _nm.SceneManager.LoadScene(SceneFlow.Bastion, LoadSceneMode.Single);
            Log.Event(Tag, "party load Bastion startWave=" + startWave);
        }

        /// <summary>Host: NetworkSceneManager returns the party to Ship without shutdown.</summary>
        public bool LoadPartyShip()
        {
            if (_nm == null || !_nm.IsServer)
            {
                Log.Warn(Tag, "LoadPartyShip refused — not party lead.");
                return false;
            }
            if (!_nm.IsListening)
            {
                Log.Warn(Tag, "LoadPartyShip refused — session not listening.");
                return false;
            }
            if (!SceneFlow.CanLoad(SceneFlow.Ship))
            {
                Log.Error(Tag, "LoadPartyShip refused — Ship not in Build Settings.");
                return false;
            }
            _nm.SceneManager.LoadScene(SceneFlow.Ship, LoadSceneMode.Single);
            Log.Event(Tag, "party return Ship");
            return true;
        }

        /// <summary>Client clones register here from OnNetworkSpawn. Null-safe for fixture tests
        /// that spawn a mirror with no scene session.</summary>
        public void RegisterMirror(NetRunMirror mirror)
        {
            _mirror = mirror;
            if (mirror != null)
                mirror.Bind(_game);
        }

        public void RegisterLobby(NetPartyLobby lobby) => _lobby = lobby;

        public void UnregisterLobby(NetPartyLobby lobby)
        {
            if (_lobby == lobby) _lobby = null;
        }

        /// <summary>Drop the session. A non-host then loads MainMenu from OnClientStopped.</summary>
        public void ShutdownSession()
        {
            _mirror = null;
            _lobby = null;
            _leavingOnPurpose = true;
            if (_nm == null) return;
            if (!_nm.IsListening) return;
            Log.Event(Tag, "session shutdown");
            _nm.Shutdown();
        }
        #endregion
    }
}
