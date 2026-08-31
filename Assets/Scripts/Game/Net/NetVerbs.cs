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
        /// <summary>Server-written: is this player's turret standing? See
        /// <see cref="ServerTurretStanding"/> for why it has to cross the wire.</summary>
        public readonly NetworkVariable<bool> TurretStanding = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Server-written: the standing machine's remaining life. Without it every
        /// scenery copy's bar reads full until the machine vanishes.</summary>
        public readonly NetworkVariable<float> TurretHp = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Server-written: earned charge, so the owner's crew square can read it
        /// without routing through <c>PlayerAbility</c>'s weapon cooldown.</summary>
        public readonly NetworkVariable<float> TurretCharge = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Server-written: <see cref="TurretPhase"/> as a byte. Exhausted must cross
        /// the wire or a guest's tap refreshes a spent machine.</summary>
        public readonly NetworkVariable<byte> TurretPhaseWire = new NetworkVariable<byte>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> OutriderStanding = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> OutriderHp = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> OutriderCharge = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<byte> OutriderPhaseWire = new NetworkVariable<byte>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> MachinesPairedWire = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

        /// <summary>FORGE Field Deploy. The server stands the machine that actually shoots;
        /// every client gets scenery at the same spot.</summary>
        public void RequestTurret(Vector3 position, bool deploying)
        {
            RequestTurretServerRpc(position, deploying);
        }

        /// <summary>FORGE Overcharge. Paid from earned charge on the server's machine.</summary>
        public void RequestOvercharge(bool on)
        {
            RequestOverchargeServerRpc(on);
        }

        public void RequestOutrider(Vector3 position, bool deploying)
        {
            RequestOutriderServerRpc(position, deploying);
        }

        public void RequestEmp(bool on)
        {
            RequestEmpServerRpc(on);
        }

        /// <summary>True while the server has a machine standing for this player. Replicated,
        /// not read off the server's own state: a guest asking its local copy would be asking an
        /// object the server never writes, and would be told "no turret" with one standing in
        /// front of it.</summary>
        public bool ServerTurretStanding => TurretStanding.Value;
        public float ServerTurretCharge => TurretCharge.Value;
        public TurretPhase ServerTurretPhase => (TurretPhase)TurretPhaseWire.Value;
        public bool ServerOutriderStanding => OutriderStanding.Value;
        public float ServerOutriderCharge => OutriderCharge.Value;
        public TurretPhase ServerOutriderPhase => (TurretPhase)OutriderPhaseWire.Value;
        public bool ServerMachinesPaired => MachinesPairedWire.Value;
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

        // ---------- FORGE ----------

        readonly TurretState _turretState = new TurretState();
        TurretObject _turretLive;
        TurretState _turretShadow;
        TurretObject _turretScenery;
        readonly OutriderState _outriderState = new OutriderState();
        OutriderObject _outriderLive;
        OutriderState _outriderShadow;
        OutriderObject _outriderScenery;

        [Rpc(SendTo.Server)]
        void RequestTurretServerRpc(Vector3 position, bool deploying)
        {
            if (!IsServer) return;
            if (!deploying)
            {
                ClearServerTurret();
                ApplyTurretClientRpc(position, false);
                return;
            }
            if (_turretState.Phase == TurretPhase.Exhausted)
            {
                Log.Event("Protocol", "crew Field Deploy refused — exhausted turret cannot be refreshed");
                return;
            }
            bool hadBody = _turretLive != null;
            string reason;
            if (!_turretState.TryPlace(position.x, position.y, position.z, out reason))
            {
                Log.Event("Protocol", "crew Field Deploy refused — " + reason);
                return;
            }
            if (!hadBody)
                _turretLive = TurretObject.Spawn(position, _turretState, _game, transform);
            else
                _turretLive.transform.position = position;
            WriteTurretWire();
            ApplyTurretClientRpc(position, true);
        }

        /// <summary>Scenery only. The server's machine is the one that shoots and the one that
        /// dies; this copy exists so the room is not empty where it stands.</summary>
        [Rpc(SendTo.NotServer)]
        void ApplyTurretClientRpc(Vector3 position, bool deploying)
        {
            if (_turretScenery != null) _turretScenery.Despawn();
            _turretScenery = null;
            if (!deploying) return;
            if (_turretShadow == null) _turretShadow = new TurretState();
            _turretShadow.ResetForRun();
            _turretShadow.TryPlace(position.x, position.y, position.z, out _);
            _turretShadow.MirrorCharge(TurretCharge.Value);
            _turretShadow.MirrorPhase((TurretPhase)TurretPhaseWire.Value);
            _turretScenery = TurretObject.Spawn(position, _turretShadow, _game, null, true);
        }

        [Rpc(SendTo.Server)]
        void RequestOverchargeServerRpc(bool on)
        {
            if (!IsServer || !on) return;
            string reason;
            if (!_turretState.TryBeginOvercharge(out reason))
            {
                Log.Event("Protocol", "crew Overcharge refused — " + reason);
                return;
            }
            WriteTurretWire();
            ApplyOverchargeClientRpc(true);
        }

        [Rpc(SendTo.NotServer)]
        void ApplyOverchargeClientRpc(bool on)
        {
            if (_turretShadow == null || !on) return;
            _turretShadow.MirrorPhase(TurretPhase.Special);
        }

        /// <summary>Keep the wire's view of the machine honest. A gatekeeper kills the turret
        /// inside <see cref="TurretObject"/>, which knows nothing of the session — without
        /// this watch the standing flag stays true and every client keeps a ghost machine.
        /// Charge and phase ride with Hp so the crew square and Exhausted tint stay true.</summary>
        void TickServerTurretMirror()
        {
            if (TurretStanding.Value && !_turretState.IsLive)
            {
                ClearServerTurret();
                ApplyTurretClientRpc(Vector3.zero, false);
                return;
            }
            if (_turretState.IsLive) WriteTurretWire();
        }

        void WriteTurretWire()
        {
            if (!IsServer || !IsSpawned) return;
            TurretStanding.Value = _turretState.IsLive;
            TurretHp.Value = _turretState.Hp;
            TurretCharge.Value = _turretState.Charge;
            TurretPhaseWire.Value = (byte)_turretState.Phase;
        }

        /// <summary>Client intake for the replicated life — onto the scenery copy's state,
        /// where the bar reads it.</summary>
        void OnTurretHpChanged(float previous, float current)
        {
            if (IsServer) return;
            if (_turretShadow != null) _turretShadow.MirrorHp(current);
        }

        void OnTurretChargeChanged(float previous, float current)
        {
            if (IsServer) return;
            if (_turretShadow != null) _turretShadow.MirrorCharge(current);
        }

        void OnTurretPhaseChanged(byte previous, byte current)
        {
            if (IsServer) return;
            if (_turretShadow != null) _turretShadow.MirrorPhase((TurretPhase)current);
        }

        void ClearServerTurret()
        {
            if (_turretLive != null) _turretLive.Despawn();
            _turretLive = null;
            _turretState.Clear();
            if (IsServer && IsSpawned)
            {
                TurretStanding.Value = false;
                TurretHp.Value = 0f;
                TurretCharge.Value = 0f;
                TurretPhaseWire.Value = (byte)TurretPhase.Available;
            }
        }

        [Rpc(SendTo.Server)]
        void RequestOutriderServerRpc(Vector3 position, bool deploying)
        {
            if (!IsServer) return;
            if (!deploying)
            {
                ClearServerOutrider();
                ApplyOutriderClientRpc(false);
                return;
            }
            if (_outriderState.Phase == TurretPhase.Exhausted)
            {
                Log.Event("Protocol", "self Outrider refused — exhausted outrider cannot be refreshed");
                return;
            }
            bool hadBody = _outriderLive != null;
            string reason;
            if (!_outriderState.TryPlace(position.x, position.y, position.z, out reason))
            {
                Log.Event("Protocol", "self Outrider refused — " + reason);
                return;
            }
            if (!hadBody)
                _outriderLive = OutriderObject.Spawn(position, _outriderState, _game, transform);
            else
                _outriderLive.transform.position = position;
            WriteOutriderWire();
            ApplyOutriderClientRpc(true);
        }

        [Rpc(SendTo.NotServer)]
        void ApplyOutriderClientRpc(bool deploying)
        {
            if (_outriderScenery != null) _outriderScenery.Despawn();
            _outriderScenery = null;
            if (!deploying) return;
            if (_outriderShadow == null) _outriderShadow = new OutriderState();
            _outriderShadow.ResetForRun();
            _outriderShadow.TryPlace(transform.position.x, transform.position.y, transform.position.z, out _);
            _outriderShadow.MirrorCharge(OutriderCharge.Value);
            _outriderShadow.MirrorPhase((TurretPhase)OutriderPhaseWire.Value);
            _outriderScenery = OutriderObject.Spawn(transform.position, _outriderShadow, _game, transform, true);
        }

        [Rpc(SendTo.Server)]
        void RequestEmpServerRpc(bool on)
        {
            if (!IsServer || !on) return;
            string reason;
            if (!_outriderState.TryBeginEmp(out reason))
            {
                Log.Event("Protocol", "self EMP refused — " + reason);
                return;
            }
            int caught = _outriderLive != null ? _outriderLive.ApplyEmpBlast() : 0;
            Log.Event("Protocol", "self EMP — " + caught + " gatekeeper(s) caught clientId="
                + OwnerClientId);
            WriteOutriderWire();
            ApplyEmpClientRpc(true);
        }

        [Rpc(SendTo.NotServer)]
        void ApplyEmpClientRpc(bool on)
        {
            if (_outriderShadow == null || !on) return;
            _outriderShadow.MirrorPhase(TurretPhase.Special);
        }

        void TickServerOutriderMirror()
        {
            if (OutriderStanding.Value && !_outriderState.IsLive)
            {
                ClearServerOutrider();
                ApplyOutriderClientRpc(false);
                return;
            }
            if (_outriderState.IsLive) WriteOutriderWire();
        }

        void WriteOutriderWire()
        {
            if (!IsServer || !IsSpawned) return;
            OutriderStanding.Value = _outriderState.IsLive;
            OutriderHp.Value = _outriderState.Hp;
            OutriderCharge.Value = _outriderState.Charge;
            OutriderPhaseWire.Value = (byte)_outriderState.Phase;
        }

        void WritePairingWire()
        {
            if (!IsServer || !IsSpawned) return;
            MachinesPairedWire.Value = TurretObject.PairingActive(transform);
        }

        void OnOutriderHpChanged(float previous, float current)
        {
            if (IsServer) return;
            if (_outriderShadow != null) _outriderShadow.MirrorHp(current);
        }

        void OnOutriderChargeChanged(float previous, float current)
        {
            if (IsServer) return;
            if (_outriderShadow != null) _outriderShadow.MirrorCharge(current);
        }

        void OnOutriderPhaseChanged(byte previous, byte current)
        {
            if (IsServer) return;
            if (_outriderShadow != null) _outriderShadow.MirrorPhase((TurretPhase)current);
        }

        void ClearServerOutrider()
        {
            if (_outriderLive != null) _outriderLive.Despawn();
            _outriderLive = null;
            _outriderState.Clear();
            if (IsServer && IsSpawned)
            {
                OutriderStanding.Value = false;
                OutriderHp.Value = 0f;
                OutriderCharge.Value = 0f;
                OutriderPhaseWire.Value = (byte)TurretPhase.Available;
                MachinesPairedWire.Value = false;
            }
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
