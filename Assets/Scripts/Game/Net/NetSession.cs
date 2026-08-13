using GatekeeperBastion.Util;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST / CLIENT: Bastion-scene session. Starts a host or joins 127.0.0.1 over
    /// UnityTransport. Scene management stays off — both instances load Bastion themselves.
    /// No statics; consumers take this through a serialized GameController.Net reference.</summary>
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public sealed class NetSession : MonoBehaviour
    {
        #region Constants
        const string Tag = "Net";
        const string LoopbackAddress = "127.0.0.1";
        const ushort LoopbackPort = 7777;
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
            _nm.OnClientStopped += OnClientStopped;
        }

        void OnDestroy()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientStopped -= OnClientStopped;
            }
        }
        #endregion

        #region Public API
        /// <summary>Start a listen-server on loopback and spawn the run mirror.</summary>
        public void StartHost()
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

            ConfigureTransport();
            if (!_nm.enabled) _nm.enabled = true;
            EnsurePrefabsRegistered();
            if (!_nm.StartHost())
            {
                Log.Warn(Tag, "StartHost refused — NetworkManager.StartHost returned false.");
                return;
            }

            LogSessionSnapshot("host started localId=" + _nm.LocalClientId);
            SpawnMirror();
        }

        /// <summary>Join 127.0.0.1:7777. The host owns the run; this instance must not board.</summary>
        public void StartClient()
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

            ConfigureTransport();
            if (!_nm.enabled) _nm.enabled = true;
            EnsurePrefabsRegistered();
            if (!_nm.StartClient())
            {
                Log.Warn(Tag, "StartClient refused — NetworkManager.StartClient returned false.");
                return;
            }

            Log.Event(Tag, "client starting — host owns the run");
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
        void ConfigureTransport()
        {
            if (_transport == null) _transport = GetComponent<UnityTransport>();
            if (_transport == null)
            {
                Log.Warn(Tag, "no UnityTransport — cannot bind " + LoopbackAddress + ":" + LoopbackPort);
                return;
            }
            if (_nm != null && _nm.NetworkConfig.NetworkTransport == null)
                _nm.NetworkConfig.NetworkTransport = _transport;
            _transport.SetConnectionData(LoopbackAddress, LoopbackPort);
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
            if (_nm != null && _nm.IsServer
                && _nm.ConnectedClients.TryGetValue(clientId, out var client)
                && client.PlayerObject != null)
            {
                var verbs = client.PlayerObject.GetComponent<NetVerbs>();
                if (verbs != null)
                    verbs.Bind(_game);
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

        void OnClientStopped(bool wasHost)
        {
            _mirror = null;
            Log.Event(Tag, "session stopped");
            if (wasHost) return;
            SceneFlow.LoadMainMenu();
        }
        #endregion
    }
}
