using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GatekeepersProtocol.Game
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
    public sealed partial class NetPlayerAvatar : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        /// <summary>Matches WeaponViewModel / Boarder.controller blend thresholds.</summary>
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        /// <summary>Planar m/s below this is idle — absorbs NetworkTransform jitter.</summary>
        const float MoveEpsMps = 0.4f;
        /// <summary>At/above this planar m/s → run (1); else walk (0.5). Midway walk 5 / sprint 7.5.</summary>
        const float SprintEpsMps = 6.25f;
        #endregion

        #region References
        FirstPersonController _rig;
        PlayerHealth _rigHealth;
        PlayerHealth _health;
        Collider _collider;
        Renderer[] _renderers;
        Renderer _seatMarker;
        Animator _animator;
        GameController _game;
        #endregion

        #region State
        bool _registered;
        bool _hideApplied;
        bool _hideInit;
        bool _spawnOffsetApplied;
        bool _haveAnimSample;
        Vector3 _lastAnimPos;
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

        public NetworkVariable<int> ProtocolId = new NetworkVariable<int>(
            (int)BoarderProtocolId.Default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> PassiveId = new NetworkVariable<int>(
            (int)BoarderAbilityId.Ghost,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> LobbyReady = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public PlayerHealth Body => _health;

        static readonly Color[] SeatTints =
        {
            new Color(0.85f, 0.85f, 0.9f),
            new Color(0.35f, 0.75f, 1f),
            new Color(1f, 0.55f, 0.2f),
            new Color(0.7f, 0.4f, 1f),
            new Color(0.4f, 0.9f, 0.45f),
            new Color(1f, 0.85f, 0.3f)
        };

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
            _animator = GetComponentInChildren<Animator>(true);
            _haveAnimSample = false;
            Hp.OnValueChanged += OnHpChanged;
            WaitingOnShip.OnValueChanged += OnWaitingChanged;
            Slot.OnValueChanged += OnSlotChanged;
            ApplyOwnerPresentation();
            ApplySeatTint();
            // Presence line on every seat (host id 0 included). Targeting registration is
            // separate and server/remote-only — see SyncRegistration.
            Log.Event(Tag, "avatar registered clientId=" + OwnerClientId);

            if (IsOwner)
            {
                BindOwnerRig();
                ReportOwnerMeta();
            }

            if (IsServer)
            {
                AssignSlot();
                if (_health != null)
                    Hp.Value = _health.Hp;
                // Ship lobby has no GameController — Bind must tolerate null.
                if (_game == null)
                {
                    var session = NetworkManager.GetComponent<NetSession>();
                    if (session != null)
                        Bind(session.Game);
                }
                SyncRegistration();
            }
        }

        void BindOwnerRig()
        {
            var found = FindAnyObjectByType<FirstPersonController>();
            // Host avatar often spawns on MainMenu before an FPC exists; Ship/Bastion
            // loads replace the rig. Rebind when missing, destroyed, or a different instance.
            // Do not treat "both null" as a successful bind — that left the host puppet at
            // origin until a later path happened to call again (2026-08-16 dual-box).
            if (found != null && found == _rig) return;
            if (found == null && _rig == null) return;
            _spawnOffsetApplied = false;
            _rig = found;
            _rigHealth = _rig != null ? _rig.GetComponent<PlayerHealth>() : null;
            OffsetOwnerRig();
            if (_rig != null)
                Log.Event(Tag, "owner rig bound clientId=" + OwnerClientId);
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
            if (IsOwner)
            {
                if (WaitingOnShip.Value) return;
                if (_rig == null)
                    BindOwnerRig();
                if (_rig == null) return;
                Transform t = _rig.transform;
                transform.SetPositionAndRotation(
                    t.position,
                    Quaternion.Euler(0f, t.eulerAngles.y, 0f));
                return;
            }

            DriveRemoteAnimator();
        }
        #endregion

        #region Public API
        /// <summary>Fixture / session bind. Null game is valid on the shared Ship lobby.</summary>
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

        /// <summary>Owner request: return from the Ship wait into the live Run (Hatch-bank).
        /// Not the party lobby EMBARK — that is <see cref="NetPartyLobby.RequestPartyEmbark"/>.</summary>
        public void RequestEmbark()
        {
            RequestEmbarkServerRpc();
        }

        /// <summary>Server: lobby Ready flag for party embark quorum.</summary>
        public void ServerSetLobbyReady(bool ready)
        {
            if (!IsServer) return;
            LobbyReady.Value = ready;
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
            ApplySeatTint();
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
            // Server registers remote puppets for targeting only — never the owner's body
            // (owner collider is off; local FPC is the scene rig in PlayerRegistry).
            bool should = IsSpawned && IsServer && !IsOwner && _health != null;
            if (should)
            {
                if (_registered) return;
                PlayerRegistry.Register(_health);
                _registered = true;
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

            if (_seatMarker != null)
                _seatMarker.enabled = !hide;

            if (_collider != null)
                _collider.enabled = !hide;
        }

        /// <summary>Remote clients: Boarder.controller Speed from replicated transform delta.
        /// Owner mesh is hidden; local FPC + WeaponViewModel own first-person anim.</summary>
        void DriveRemoteAnimator()
        {
            if (_animator == null) return;
            Vector3 pos = transform.position;
            if (!_haveAnimSample)
            {
                _lastAnimPos = pos;
                _haveAnimSample = true;
                _animator.SetFloat(SpeedHash, 0f);
                return;
            }

            float dt = Time.deltaTime;
            Vector3 delta = pos - _lastAnimPos;
            _lastAnimPos = pos;
            if (dt <= 0f) return;
            delta.y = 0f;
            float mps = delta.magnitude / dt;
            float speed = 0f;
            if (mps > MoveEpsMps)
                speed = mps >= SprintEpsMps ? 1f : 0.5f;
            _animator.SetFloat(SpeedHash, speed);
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

            ApplyRejoin();
            Log.Event(Tag, "client embark hatch-bank clientId=" + OwnerClientId);
        }

        /// <summary>Heal, clear the wait flag, tell the owner to reset to Hatch spawn.
        /// Shared by free Hatch-bank Prep embark and paid buy-back.</summary>
        void ApplyRejoin()
        {
            if (_health != null)
            {
                _health.HealToFull();
                Hp.Value = _health.Hp;
            }
            WaitingOnShip.Value = false;
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
            var meta = GameSession.Meta;
            if (meta == null) return;
            meta.EarnSalvage(credited);
            // Salvage came through the Hatch for THIS seat: the guest's own wheel /
            // equipment-slot unlock (MetaUpgrades.UpgradesUnlocked). Drip credits arrive
            // per tick, so this floors at 1 rather than counting windows — the gate only
            // ever reads >= 1.
            if (meta.HatchConversions < 1)
                meta.HatchConversions = 1;
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
