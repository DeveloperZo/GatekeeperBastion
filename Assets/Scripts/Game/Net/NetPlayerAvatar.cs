using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (puppet): a visible Boarder for everyone except its owner.
    /// Owner-authoritative NetworkTransform. Capsule collider + PlayerHealth so the host
    /// targeting set can see a remote player. Copies the local rig's position and yaw each
    /// LateUpdate. Owner collider stays off so it does not double-physics with the FPS rig.
    /// A non-host owner at 0 HP waits on an additive Ship and embarks in Hatch-bank Prep.</summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class NetPlayerAvatar : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        #endregion

        #region References
        FirstPersonController _rig;
        PlayerHealth _rigHealth;
        PlayerHealth _health;
        Collider _collider;
        Renderer[] _renderers;
        GameController _game;
        #endregion

        #region State
        bool _registered;
        bool _hideApplied;
        bool _hideInit;
        bool _spawnOffsetApplied;
        #endregion

        #region Properties
        public NetworkVariable<float> Hp = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> WaitingOnShip = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> Slot = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<long> Unbanked = new NetworkVariable<long>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<long> Banked = new NetworkVariable<long>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public PlayerHealth Body => _health;

        /// <summary>This machine's player object avatar, or null until spawned.</summary>
        public static NetPlayerAvatar LocalOwner
        {
            get
            {
                var nm = NetworkManager.Singleton;
                if (nm == null || !nm.IsListening) return null;
                if (!nm.ConnectedClients.TryGetValue(nm.LocalClientId, out var client))
                    return null;
                var po = client.PlayerObject;
                return po != null ? po.GetComponent<NetPlayerAvatar>() : null;
            }
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _collider = GetComponent<Collider>();
            var nt = GetComponent<NetworkTransform>();
            if (nt != null)
                nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _renderers = GetComponentsInChildren<Renderer>(true);
            Hp.OnValueChanged += OnHpChanged;
            WaitingOnShip.OnValueChanged += OnWaitingChanged;
            Slot.OnValueChanged += OnSlotChanged;
            ApplyOwnerPresentation();

            if (IsOwner)
            {
                _rig = FindAnyObjectByType<FirstPersonController>();
                if (_rig != null)
                    _rigHealth = _rig.GetComponent<PlayerHealth>();
                OffsetOwnerRig();
                int deepest = GameSession.Meta != null ? GameSession.Meta.DeepestWaveReached : 1;
                ReportDeepestServerRpc(deepest);
            }

            if (IsServer)
            {
                AssignSlot();
                if (_health != null)
                    Hp.Value = _health.Hp;
                if (_game == null)
                {
                    var session = NetworkManager.GetComponent<NetSession>();
                    if (session != null)
                        Bind(session.Game);
                }
                SyncRegistration();
            }
        }

        public override void OnNetworkDespawn()
        {
            Hp.OnValueChanged -= OnHpChanged;
            WaitingOnShip.OnValueChanged -= OnWaitingChanged;
            Slot.OnValueChanged -= OnSlotChanged;
            if (_registered)
            {
                if (_health != null)
                    PlayerRegistry.Unregister(_health);
                _registered = false;
                Log.Event(Tag, "avatar unregistered clientId=" + OwnerClientId);
            }
            base.OnNetworkDespawn();
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer)
            {
                SyncRegistration();
                SyncHp();
                SyncWallet();
            }
            ApplyOwnerPresentation();
            if (!IsOwner) return;
            if (WaitingOnShip.Value) return;
            if (_rig == null) return;
            Transform t = _rig.transform;
            transform.SetPositionAndRotation(
                t.position,
                Quaternion.Euler(0f, t.eulerAngles.y, 0f));
        }
        #endregion

        #region Public API
        /// <summary>Fixture bind when no scene <see cref="NetSession"/> exists.</summary>
        public void Bind(GameController game)
        {
            _game = game;
        }

        /// <summary>Server: credit this owner's local Meta. Host slot 0 already wrote host Meta.</summary>
        public void CreditBanked(long credited)
        {
            if (!IsServer || credited <= 0L) return;
            CreditBankedClientRpc(credited);
        }

        /// <summary>Server: raise this owner's DeepestWaveReached from a co-op clear.</summary>
        public void PushProgress(int deepest)
        {
            if (!IsServer) return;
            if (_game != null && _game.Sim != null)
                _game.Sim.SetSlotDeepest(Slot.Value, deepest);
            PushProgressClientRpc(deepest);
        }

        /// <summary>Owner request: return from the Ship wait into the live Run.</summary>
        public void RequestEmbark()
        {
            RequestEmbarkServerRpc();
        }
        #endregion

        #region Private
        void AssignSlot()
        {
            var nm = NetworkManager;
            if (nm == null) return;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ClientId != OwnerClientId) continue;
                Slot.Value = i;
                return;
            }
        }

        void OffsetOwnerRig()
        {
            if (!IsOwner || _rig == null) return;
            int slot = Slot.Value;
            if (slot <= 0) return;
            if (_spawnOffsetApplied) return;
            _spawnOffsetApplied = true;
            Vector3 origin = _rig.transform.position;
            _rig.transform.position = origin
                + Quaternion.Euler(0f, slot * 120f, 0f) * Vector3.forward * 2.5f;
        }

        void OnSlotChanged(int previous, int next)
        {
            if (previous == next) return;
            if (previous > 0) return;
            OffsetOwnerRig();
        }

        void SyncWallet()
        {
            if (_game == null || _game.Sim == null) return;
            long u = _game.Sim.UnbankedOf(Slot.Value);
            if (Unbanked.Value != u) Unbanked.Value = u;
            if (Slot.Value != 0 || _game.Meta == null) return;
            long b = _game.Meta.BankedSalvage;
            if (Banked.Value != b) Banked.Value = b;
        }

        void SyncHp()
        {
            if (_health == null) return;
            float current = _health.Hp;
            if (Hp.Value != current)
                Hp.Value = current;
            TryMarkWaiting(current);
        }

        void TryMarkWaiting(float current)
        {
            if (current > 0f) return;
            if (WaitingOnShip.Value) return;
            if (IsOwner) return;
            WaitingOnShip.Value = true;
            if (_game != null && _game.Sim != null)
                _game.Sim.WipeUnbankedSlot(Slot.Value);
            Log.Event(Tag, "client down → ship clientId=" + OwnerClientId);
        }

        void SyncRegistration()
        {
            bool should = IsSpawned && IsServer && !IsOwner && _health != null;
            if (should)
            {
                if (_registered) return;
                PlayerRegistry.Register(_health);
                _registered = true;
                Log.Event(Tag, "avatar registered clientId=" + OwnerClientId);
                return;
            }

            if (!_registered) return;
            if (_health != null)
                PlayerRegistry.Unregister(_health);
            _registered = false;
            Log.Event(Tag, "avatar unregistered clientId=" + OwnerClientId);
        }

        void ApplyOwnerPresentation()
        {
            bool hide = IsOwner;
            if (_hideInit && hide == _hideApplied) return;
            _hideInit = true;
            _hideApplied = hide;

            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null)
                        _renderers[i].enabled = !hide;
                }
            }

            if (_collider != null)
                _collider.enabled = !hide;
        }

        void TryAcceptEmbark()
        {
            if (!WaitingOnShip.Value) return;

            var sim = _game != null ? _game.Sim : null;
            if (sim == null) return;

            if (sim.Phase != RunPhase.Tethered)
                return;

            if (sim.Assault != AssaultState.Prep)
            {
                Log.Event(Tag, "embark refused — Assault is live clientId=" + OwnerClientId);
                return;
            }

            if (sim.Mode != WaveMode.HatchBank)
            {
                Log.Event(Tag, "embark refused — not Hatch-bank Prep clientId=" + OwnerClientId);
                return;
            }

            if (_health != null)
            {
                _health.HealToFull();
                Hp.Value = _health.Hp;
            }
            WaitingOnShip.Value = false;
            Log.Event(Tag, "client embark hatch-bank clientId=" + OwnerClientId);
            EmbarkAcceptedClientRpc();
        }
        #endregion

        #region Events
        void OnHpChanged(float previous, float next)
        {
            if (!IsOwner) return;
            if (_rigHealth == null) return;
            _rigHealth.ApplyReplicatedHp(next);
            if (previous <= 0f && next > 0f)
                _rigHealth.ResetToSpawn();
        }

        void OnWaitingChanged(bool previous, bool next)
        {
            if (!IsOwner) return;
            if (next && !previous)
                NetShipWait.Enter();
            else if (!next && previous)
                NetShipWait.Exit();
        }

        [Rpc(SendTo.Server)]
        void RequestEmbarkServerRpc()
        {
            if (!IsServer) return;
            TryAcceptEmbark();
        }

        [Rpc(SendTo.Owner)]
        void EmbarkAcceptedClientRpc()
        {
            if (NetShipWait.IsActive)
                NetShipWait.Exit();
            if (_rigHealth != null)
                _rigHealth.ResetToSpawn();
        }

        [Rpc(SendTo.Server)]
        void ReportDeepestServerRpc(int deepest)
        {
            if (!IsServer) return;
            if (_game != null && _game.Sim != null)
                _game.Sim.SetSlotDeepest(Slot.Value, deepest);
        }

        [Rpc(SendTo.Owner)]
        void CreditBankedClientRpc(long credited)
        {
            if (credited <= 0L) return;
            if (GameSession.Meta != null)
                GameSession.Meta.EarnSalvage(credited);
        }

        [Rpc(SendTo.Owner)]
        void PushProgressClientRpc(int deepest)
        {
            var meta = GameSession.Meta;
            if (meta == null) return;
            if (deepest > meta.DeepestWaveReached)
                meta.DeepestWaveReached = deepest;
        }
        #endregion
    }
}
