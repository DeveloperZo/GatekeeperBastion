using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): remote Fire/Reload triggers.
    /// Split so the avatar file stays under cap. Fire is cosmetic and high-rate, so
    /// that relay is unreliable; reload is one event with a duration, so it is reliable.</summary>
    public sealed partial class NetPlayerAvatar
    {
        #region Constants
        static readonly int FireHash = Animator.StringToHash("Fire");
        static readonly int ReloadHash = Animator.StringToHash("Reload");
        #endregion

        #region Public API
        /// <summary>Owner: a shot passed ammo/rate gating. Relays Fire to non-owner animators.</summary>
        public void NotifyFired()
        {
            if (!IsSpawned || !IsOwner) return;
            FiredServerRpc();
        }

        /// <summary>Owner: a reload actually started. Relays Reload to non-owner animators.</summary>
        public void NotifyReloadStarted()
        {
            if (!IsSpawned || !IsOwner) return;
            ReloadStartedServerRpc();
        }
        #endregion

        #region Private
        void ApplyRemoteTrigger(int hash, int other)
        {
            if (IsOwner) return;
            if (_animator == null) return;
            _animator.ResetTrigger(other);
            _animator.SetTrigger(hash);
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void FiredServerRpc()
        {
            if (!IsServer) return;
            FiredClientRpc();
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        void FiredClientRpc()
        {
            ApplyRemoteTrigger(FireHash, ReloadHash);
        }

        [Rpc(SendTo.Server)]
        void ReloadStartedServerRpc()
        {
            if (!IsServer) return;
            ReloadStartedClientRpc();
        }

        [Rpc(SendTo.NotOwner)]
        void ReloadStartedClientRpc()
        {
            ApplyRemoteTrigger(ReloadHash, FireHash);
        }
        #endregion
    }
}
