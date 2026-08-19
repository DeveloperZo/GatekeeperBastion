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
        #endregion

        #region Properties
        public NetworkVariable<float> Hp = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _ai = GetComponent<EnemyGatekeeper>();
            _agent = GetComponent<NavMeshAgent>();
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
            if (IsServer)
            {
                WriteReplicatedHp();
                return;
            }
            if (_ai != null) _ai.enabled = false;
            if (_agent != null) _agent.enabled = false;
            Log.Event(Tag, "clone enemy=" + NetworkObjectId);
        }

        public override void OnNetworkDespawn()
        {
            Hp.OnValueChanged -= OnReplicatedHpChanged;
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
            var netObj = NetworkObject;
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
        }
        #endregion
    }
}
