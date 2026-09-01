using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY handlers / LOCAL-OWNER senders: in-run verbs from a client avatar.
    /// Repair channel, decoy place, concussive cone and pin apply on the server sim only.
    /// Mk1 surge and railshot stay on the owner gun and never arrive here.
    /// This half also owns the spawn/despawn lifecycle and the server tick that drives both
    /// halves. The FORGE machines — turret and Outrider replicated state, their handlers and
    /// their request verbs — live in NetVerbs.Forge.cs, split out because that work took this
    /// file past the folder's line cap. It is <c>partial</c> rather than two types because
    /// NetVerbs is a NetworkBehaviour: Netcode binds RPCs and NetworkVariables to one
    /// component on the player object, so the halves cannot be separate classes.</summary>
    public sealed partial class NetVerbs : NetworkBehaviour
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
            TurretHp.OnValueChanged += OnTurretHpChanged;
            TurretCharge.OnValueChanged += OnTurretChargeChanged;
            TurretPhaseWire.OnValueChanged += OnTurretPhaseChanged;
            OutriderHp.OnValueChanged += OnOutriderHpChanged;
            OutriderCharge.OnValueChanged += OnOutriderChargeChanged;
            OutriderPhaseWire.OnValueChanged += OnOutriderPhaseChanged;
            if (!IsServer || _game != null) return;
            var session = NetworkManager.GetComponent<NetSession>();
            if (session != null)
                Bind(session.Game);
        }

        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            TickServerTurretMirror();
            TickServerOutriderMirror();
            WritePairingWire();
            if (!_repairing) return;
            TickRepair(Time.deltaTime);
        }

        public override void OnNetworkDespawn()
        {
            TurretHp.OnValueChanged -= OnTurretHpChanged;
            TurretCharge.OnValueChanged -= OnTurretChargeChanged;
            TurretPhaseWire.OnValueChanged -= OnTurretPhaseChanged;
            OutriderHp.OnValueChanged -= OnOutriderHpChanged;
            OutriderCharge.OnValueChanged -= OnOutriderChargeChanged;
            OutriderPhaseWire.OnValueChanged -= OnOutriderPhaseChanged;
            // A machine must not outlive the player who stood it up.
            if (IsServer)
            {
                ClearServerTurret();
                ClearServerOutrider();
            }
            if (_turretScenery != null) _turretScenery.Despawn();
            _turretScenery = null;
            if (_outriderScenery != null) _outriderScenery.Despawn();
            _outriderScenery = null;
            base.OnNetworkDespawn();
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

        /// <summary>VEIL Cloak. The flag has to live on the SERVER's copy of this player:
        /// gatekeepers are host-only and read <see cref="PlayerRegistry.NearestAlive"/>, which
        /// for a guest holds the avatar's PlayerHealth, not the guest's own rig. Set locally
        /// only, a guest's cloak is invisible to every enemy in the game.</summary>
        public void RequestCloak(bool cloaked)
        {
            RequestCloakServerRpc(cloaked);
        }

        /// <summary>VEIL Ping. The provoke has to run where the gatekeepers are.</summary>
        public void RequestPing()
        {
            RequestPingServerRpc();
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
            var seat = GetComponent<NetPlayerAvatar>();
            gc.RepairHatchFromNet(rate * dt, seat != null ? seat.Slot.Value : 0);
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

            // Shove returns walk-back seconds; metres are that product (criterion 6).
            float metresMoved = credited * WaveDirector.GatekeeperPatrolSpeed;
            Log.Event("Ability",
                "concussive shove — " + hit + " gatekeeper(s), " + metresMoved.ToString("0.##")
                + "m, " + credited.ToString("0.##") + " gatekeeper-seconds");
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

        BarrageField _barrage;

        [Rpc(SendTo.Server)]
        void RequestAnchorServerRpc(Vector3 position, bool planting)
        {
            if (!IsServer) return;
            ApplyBarragePlant(position, planting);
            ApplyBarrageClientRpc(position, planting);
        }

        [Rpc(SendTo.NotServer)]
        void ApplyBarrageClientRpc(Vector3 position, bool planting)
        {
            ApplyBarragePlant(position, planting);
        }

        void ApplyBarragePlant(Vector3 position, bool planting)
        {
            if (_barrage != null)
            {
                BarrageField.Uproot(_barrage);
                _barrage = null;
            }
            if (!planting) return;
            // Dump aura follows this avatar (NetVerbs lives on the player object).
            _barrage = BarrageField.Plant(transform);
        }

        [Rpc(SendTo.Server)]
        void RequestBulkheadServerRpc()
        {
            if (!IsServer) return;
        }

        // ---------- VEIL ----------

        [Rpc(SendTo.Server)]
        void RequestCloakServerRpc(bool cloaked)
        {
            if (!IsServer) return;
            var body = AvatarBody();
            if (body == null) return;
            body.SetCloaked(cloaked);
            Log.Event("Protocol", "cloak " + (cloaked ? "up" : "down")
                + " clientId=" + OwnerClientId);
        }

        [Rpc(SendTo.Server)]
        void RequestPingServerRpc()
        {
            if (!IsServer) return;
            var gc = _game;
            var spawner = gc != null ? gc.Spawner : null;
            if (spawner == null) return;
            var live = spawner.Live;
            int turned = 0;
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                if (gk.Provoke(transform, shout: false)) turned++;
            }
            Log.Event("Protocol", "crew Ping — " + turned
                + " gatekeeper(s) turned on the caster clientId=" + OwnerClientId);

            // The render is the crew's half of the ability, so it is broadcast rather than
            // shown where the caster stands. The server marks its own room directly; the
            // ClientRpc reaches everyone else.
            float window = BoarderProtocol.Window(BoarderAbilityId.Ping);
            PingMarker.ShowAll(live, window);
            ShowPingClientRpc(window);
        }

        [Rpc(SendTo.NotServer)]
        void ShowPingClientRpc(float seconds)
        {
            var gc = _game;
            var spawner = gc != null ? gc.Spawner : null;
            if (spawner == null) return;
            PingMarker.ShowAll(spawner.Live, seconds);
        }

        /// <summary>The body the server targets for this player. NetVerbs and the avatar's
        /// PlayerHealth share the PlayerObject, so this is the same component
        /// <see cref="PlayerRegistry"/> holds — which is the whole point: a cloak written
        /// anywhere else is invisible to every host-side gatekeeper.</summary>
        PlayerHealth AvatarBody()
        {
            return GetComponent<PlayerHealth>();
        }
        #endregion
    }
}
