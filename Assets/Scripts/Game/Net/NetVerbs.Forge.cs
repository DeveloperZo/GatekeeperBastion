using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY handlers / LOCAL-OWNER senders (partial of NetVerbs): everything the
    /// FORGE machines need to cross the wire — the turret and Outrider replicated state, their
    /// client intake handlers, the shadow states the scenery copies read, the pairing flag and
    /// the request verbs that stand a machine up or spend its charge. Split out because the
    /// FORGE work took NetVerbs.cs past the folder's line cap; the other half keeps the repair
    /// channel, decoy, concussive, pin and VEIL verbs. This is <c>partial</c> rather than a
    /// second type because NetVerbs is a NetworkBehaviour: Netcode binds RPCs and
    /// NetworkVariables to one component on the player object, so these members cannot live on
    /// a class of their own.</summary>
    public sealed partial class NetVerbs
    {
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
        #endregion

        #region Public API
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

        #region Events
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
        #endregion
    }
}
