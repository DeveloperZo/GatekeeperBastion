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
        }

        void OnDestroy()
        {
            if (_nm != null)
                _nm.OnClientConnectedCallback -= OnClientConnected;
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

            Log.Event(Tag, "host started");
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

            Log.Event(Tag, "client cannot board — the host owns the run");
            if (_game != null && _game.Spawner != null)
                _game.Spawner.DespawnAllLive();
        }

        /// <summary>Client clones register here from OnNetworkSpawn. Null-safe for fixture tests
        /// that spawn a mirror with no scene session.</summary>
        public void RegisterMirror(NetRunMirror mirror)
        {
            _mirror = mirror;
            if (mirror != null)
                mirror.Bind(_game);
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
        }

        void RegisterPrefab(GameObject prefab)
        {
            if (prefab == null || _nm == null) return;
            var prefabs = _nm.NetworkConfig.Prefabs;
            if (prefabs == null) return;
            if (prefabs.Contains(prefab)) return;
            prefabs.Add(new NetworkPrefab { Prefab = prefab });
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
            Log.Event(Tag, "connected clientId=" + clientId);
        }
        #endregion
    }
}
