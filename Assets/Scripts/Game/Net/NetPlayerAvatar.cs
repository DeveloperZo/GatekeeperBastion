using GatekeeperBastion.Util;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (puppet): a visible Boarder for everyone except its owner.
    /// Owner-authoritative NetworkTransform. Capsule collider + PlayerHealth so the host
    /// targeting set can see a remote player. Copies the local rig's position and yaw each
    /// LateUpdate. Owner collider stays off so it does not double-physics with the FPS rig.</summary>
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
        #endregion

        #region State
        bool _registered;
        bool _hideApplied;
        bool _hideInit;
        #endregion

        #region Properties
        public NetworkVariable<float> Hp = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public PlayerHealth Body => _health;
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
            ApplyOwnerPresentation();

            if (IsOwner)
            {
                _rig = FindAnyObjectByType<FirstPersonController>();
                if (_rig != null)
                    _rigHealth = _rig.GetComponent<PlayerHealth>();
            }

            if (IsServer)
            {
                if (_health != null)
                    Hp.Value = _health.Hp;
                SyncRegistration();
            }
        }

        public override void OnNetworkDespawn()
        {
            Hp.OnValueChanged -= OnHpChanged;
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
            }
            ApplyOwnerPresentation();
            if (!IsOwner) return;
            if (_rig == null) return;
            Transform t = _rig.transform;
            transform.SetPositionAndRotation(
                t.position,
                Quaternion.Euler(0f, t.eulerAngles.y, 0f));
        }
        #endregion

        #region Private
        void SyncHp()
        {
            if (_health == null) return;
            float current = _health.Hp;
            if (Hp.Value == current) return;
            Hp.Value = current;
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
        #endregion
    }
}
