using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: server-spawned gatekeeper. Client clones disable AI and
    /// NavMeshAgent and never write Damageable — hits go through SubmitHitServerRpc so
    /// damage applies once, on the host. Null-safe when no NetSession exists (fixture tests).</summary>
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

        #region Unity Lifecycle
        void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _ai = GetComponent<EnemyGatekeeper>();
            _agent = GetComponent<NavMeshAgent>();
            if (_damageable != null)
                _damageable.onDeath.AddListener(OnHostDeath);
        }

        void OnDestroy()
        {
            if (_damageable != null)
                _damageable.onDeath.RemoveListener(OnHostDeath);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) return;
            if (_ai != null) _ai.enabled = false;
            if (_agent != null) _agent.enabled = false;
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
        void OnHostDeath()
        {
            if (!IsServer) return;
            var netObj = NetworkObject;
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
        }
        #endregion
    }
}
