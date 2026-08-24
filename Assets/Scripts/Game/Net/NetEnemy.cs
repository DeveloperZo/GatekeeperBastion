using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: server-spawned gatekeeper. Client clones disable AI and
    /// NavMeshAgent and never write Damageable — hits go through SubmitHitServerRpc so
    /// damage applies once, on the host. Replicated Hp drives clone hit-flash only; lethal
    /// state and onDeath stay on the server, and despawn still comes from the host.
    /// Null-safe when no NetSession exists (fixture tests).</summary>
    public sealed class NetEnemy : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        #endregion

        #region References
        Damageable _damageable;
        EnemyGatekeeper _ai;
        NavMeshAgent _agent;
        CloakerPresentation _cloak;
        #endregion

        #region Properties
        public NetworkVariable<float> Hp = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Cloaker only (default false = solid, right for every other kind): whether
        /// the body renders cloaked right now. Global, not per-viewer — one player looking
        /// stops it for everybody, so there is nothing per-viewer to replicate. Host-side
        /// CloakerGatekeeper writes it; clones drive CloakerPresentation from it. No RPC.</summary>
        public NetworkVariable<bool> Cloaked = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _ai = GetComponent<EnemyGatekeeper>();
            _agent = GetComponent<NavMeshAgent>();
            _cloak = GetComponent<CloakerPresentation>();
            if (_damageable != null)
            {
                _damageable.onDeath.AddListener(OnHostDeath);
                _damageable.Damaged += OnHostDamaged;
            }
        }

        void OnDestroy()
        {
            if (_damageable != null)
            {
                _damageable.onDeath.RemoveListener(OnHostDeath);
                _damageable.Damaged -= OnHostDamaged;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Hp.OnValueChanged += OnReplicatedHpChanged;
            Cloaked.OnValueChanged += OnReplicatedCloakChanged;
            if (IsServer)
            {
                WriteReplicatedHp();
                return;
            }
            if (_ai != null) _ai.enabled = false;
            if (_agent != null) _agent.enabled = false;
            // Late joiner: the variable arrives already true for a cloaked body, and no
            // OnValueChanged fires for state that never changes — apply it once here.
            if (_cloak != null) _cloak.SetCloaked(Cloaked.Value);
            Log.Event(Tag, "clone enemy=" + NetworkObjectId);
        }

        public override void OnNetworkDespawn()
        {
            Hp.OnValueChanged -= OnReplicatedHpChanged;
            Cloaked.OnValueChanged -= OnReplicatedCloakChanged;
            base.OnNetworkDespawn();
        }
        #endregion

        #region Public API
        /// <summary>Client-authoritative hit: falloff and crit are already in <paramref name="dmg"/>.
        /// Require-everyone so a client that does not own the enemy can still submit.</summary>
        [Rpc(SendTo.Server)]
        public void SubmitHitServerRpc(float dmg, byte slot, RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (_damageable == null) _damageable = GetComponent<Damageable>();
            if (_damageable == null || _damageable.IsDead) return;

            Transform source = null;
            var nm = NetworkManager;
            if (nm != null)
            {
                ulong sender = rpcParams.Receive.SenderClientId;
                if (nm.ConnectedClients.TryGetValue(sender, out var client)
                    && client.PlayerObject != null)
                    source = client.PlayerObject.transform;
            }

            _damageable.TakeDamage(dmg, source, (WeaponSlot)slot);
            Log.Event(Tag, "hit enemy=" + NetworkObjectId + " dmg=" + dmg + " slot=" + slot);
        }
        #endregion

        /// <summary>Host-side CloakerGatekeeper's one write path for the replicated cloak.
        /// Safe to call unspawned or in fixtures — it simply does nothing there, and the
        /// local CloakerPresentation call beside it carries solo play.</summary>
        public void SetCloaked(bool cloaked)
        {
            if (!IsSpawned || !IsServer) return;
            if (Cloaked.Value != cloaked) Cloaked.Value = cloaked;
        }

        #region Private
        void WriteReplicatedHp()
        {
            if (!IsSpawned || !IsServer) return;
            if (_damageable == null) return;
            float hp = _damageable.Hp;
            if (hp < 0f) hp = 0f;
            if (Hp.Value != hp) Hp.Value = hp;
        }
        #endregion

        #region Events
        void OnHostDamaged(Transform _)
        {
            WriteReplicatedHp();
        }

        void OnReplicatedCloakChanged(bool previous, bool next)
        {
            // The host's presentation was already set directly by CloakerGatekeeper.
            if (IsServer) return;
            if (_cloak == null) _cloak = GetComponent<CloakerPresentation>();
            if (_cloak != null) _cloak.SetCloaked(next);
        }

        void OnReplicatedHpChanged(float previous, float next)
        {
            if (IsServer) return;
            if (next >= previous) return;
            if (_damageable == null) _damageable = GetComponent<Damageable>();
            if (_damageable == null) return;
            _damageable.NotifyHitFlash();
        }

        void OnHostDeath()
        {
            if (!IsServer) return;
            WriteReplicatedHp();
            PlayDeathRpc();
            // WaveSpawner.Despawn owns the deferred destroy. Despawning here would
            // delete the clone before the death clip can play on either side.
            if (_ai != null && _ai.HasLifetimeOwner) return;
            var netObj = NetworkObject;
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
        }

        [Rpc(SendTo.NotServer)]
        void PlayDeathRpc()
        {
            if (_ai != null) _ai.BeginDeathPresentation();
        }
        #endregion
    }
}
