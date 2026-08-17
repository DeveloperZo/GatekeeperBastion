using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY handlers / LOCAL-OWNER senders: in-run verbs from a client avatar.
    /// Repair channel, decoy place, concussive cone and pin apply on the server sim only.
    /// Mk1 surge and railshot stay on the owner gun and never arrive here.</summary>
    public sealed class NetVerbs : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        /// <summary>Must match <see cref="Interactor"/>'s serialized default range.</summary>
        const float RepairRange = 3.2f;
        #endregion

        #region State
        GameController _game;
        bool _repairing;
        bool _loggedRange;
        #endregion

        #region Unity Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer || _game != null) return;
            var session = NetworkManager.GetComponent<NetSession>();
            if (session != null)
                Bind(session.Game);
        }

        void Update()
        {
            if (!IsSpawned || !IsServer || !_repairing) return;
            TickRepair(Time.deltaTime);
        }
        #endregion

        #region Public API
        /// <summary>Fixture bind when no scene <see cref="NetSession"/> exists.</summary>
        public void Bind(GameController game)
        {
            _game = game;
        }

        public void SetRepairing(bool repairing)
        {
            SetRepairingServerRpc(repairing);
        }

        public void RequestPlaceDecoy(Vector3 position)
        {
            PlaceDecoyServerRpc(position);
        }

        public void RequestConcussive(Vector3 origin, Vector3 dir, float metres, float range)
        {
            ActivateConcussiveServerRpc(origin, dir, metres, range);
        }

        public void RequestPin(ulong targetNetId, float seconds)
        {
            RequestPinServerRpc(targetNetId, seconds);
        }

        public void RequestAnchor(Vector3 position, bool planting)
        {
            RequestAnchorServerRpc(position, planting);
        }

        public void RequestBulkhead()
        {
            RequestBulkheadServerRpc();
        }
        #endregion

        #region Private
        void TickRepair(float dt)
        {
            if (dt <= 0f) return;
            var gc = _game;
            if (gc == null) return;
            if (!InRepairRange())
            {
                if (!_loggedRange)
                {
                    Log.Event(Tag, "repair refused — out of range");
                    _loggedRange = true;
                }
                return;
            }
            _loggedRange = false;
            float rate = EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond;
            gc.RepairHatchFromNet(rate * dt);
        }

        bool InRepairRange()
        {
            var hatch = HatchStation.Instance;
            if (hatch == null) return true;
            Vector3 from = transform.position;
            Vector3 surface = hatch.ClosestSurfacePoint(from);
            return Vector3.Distance(from, surface) <= RepairRange + 0.05f;
        }

        void ApplyConcussive(Vector3 origin, Vector3 forward, float metres, float range)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            float cosMin = Mathf.Cos(WeaponAbility.Br12ConeHalfAngleDegrees * Mathf.Deg2Rad);
            var hatch = HatchStation.Instance;
            Vector3 awayFromHatch = hatch != null
                ? (origin - hatch.transform.position)
                : -forward;
            awayFromHatch.y = 0f;
            if (awayFromHatch.sqrMagnitude < 0.01f) awayFromHatch = -forward;
            awayFromHatch.Normalize();

            int hit = 0;
            float credited = 0f;
            var nm = NetworkManager;
            if (nm == null || nm.SpawnManager == null) return;
            foreach (var kv in nm.SpawnManager.SpawnedObjects)
            {
                var b = kv.Value.GetComponent<EnemyGatekeeper>();
                if (b == null) continue;
                Vector3 to = b.transform.position - origin;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > range || dist < 0.05f) continue;
                if (Vector3.Dot(forward, to / dist) < cosMin) continue;
                credited += b.Shove(awayFromHatch * metres);
                hit++;
            }

            Log.Event("Ability",
                "concussive shove — " + hit + " gatekeeper(s), " + credited.ToString("0.##")
                + " gatekeeper-seconds");
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server)]
        void SetRepairingServerRpc(bool repairing)
        {
            if (!IsServer) return;
            _repairing = repairing;
            if (!repairing) _loggedRange = false;
        }

        [Rpc(SendTo.Server)]
        void PlaceDecoyServerRpc(Vector3 position)
        {
            if (!IsServer) return;
            var gc = _game;
            if (gc == null || gc.Decoys == null)
            {
                Log.Event("Decoy", "place refused — no host bay");
                return;
            }
            gc.Decoys.TryPlace(position);
        }

        [Rpc(SendTo.Server)]
        void ActivateConcussiveServerRpc(Vector3 origin, Vector3 dir, float metres, float range)
        {
            if (!IsServer) return;
            if (metres <= 0f || range <= 0f) return;
            float maxMetres = WeaponAbility.Magnitude(AbilityId.Br12Concussive, WeaponAbility.MaxDialPoints);
            if (metres > maxMetres) metres = maxMetres;
            ApplyConcussive(origin, dir, metres, range);
        }

        [Rpc(SendTo.Server)]
        void RequestPinServerRpc(ulong targetNetId, float seconds)
        {
            if (!IsServer) return;
            if (seconds <= 0f) return;
            float max = WeaponAbility.Magnitude(AbilityId.Mr35Pin, WeaponAbility.MaxDialPoints);
            if (seconds > max) seconds = max;

            var nm = NetworkManager;
            if (nm == null || nm.SpawnManager == null) return;
            if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var netObj))
            {
                Log.Event(Tag, "pin refused — unknown target " + targetNetId);
                return;
            }
            var gk = netObj.GetComponent<EnemyGatekeeper>();
            if (gk == null)
            {
                Log.Event(Tag, "pin refused — not a gatekeeper");
                return;
            }
            gk.Pin(seconds);
            Log.Event("Ability",
                "pin held " + seconds.ToString("0.##") + "s on " + gk.Kind);
        }

        AnchorField _anchor;

        [Rpc(SendTo.Server)]
        void RequestAnchorServerRpc(Vector3 position, bool planting)
        {
            if (!IsServer) return;
            if (_anchor != null)
            {
                AnchorField.Uproot(_anchor);
                _anchor = null;
            }
            if (!planting) return;
            _anchor = AnchorField.Plant(position);
        }

        [Rpc(SendTo.Server)]
        void RequestBulkheadServerRpc()
        {
            if (!IsServer) return;
            var health = GetComponent<PlayerHealth>();
            if (health != null) health.ArmBulkhead();
        }
        #endregion
    }
}
