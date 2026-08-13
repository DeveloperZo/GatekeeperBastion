using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (puppet): a visible capsule for everyone except its owner.
    /// Owner-authoritative NetworkTransform. No collider — nothing in this slice targets
    /// or shoots players. Copies the local rig's position and yaw each LateUpdate.</summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public sealed class NetPlayerAvatar : NetworkBehaviour
    {
        #region References
        FirstPersonController _rig;
        Renderer[] _renderers;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var nt = GetComponent<NetworkTransform>();
            if (nt != null)
                nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (!IsOwner) return;

            HideOwnRenderers();
            _rig = FindAnyObjectByType<FirstPersonController>();
        }

        void LateUpdate()
        {
            if (!IsSpawned || !IsOwner) return;
            if (_rig == null) return;
            Transform t = _rig.transform;
            transform.SetPositionAndRotation(
                t.position,
                Quaternion.Euler(0f, t.eulerAngles.y, 0f));
        }
        #endregion

        #region Private
        void HideOwnRenderers()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].enabled = false;
            }
        }
        #endregion
    }
}
