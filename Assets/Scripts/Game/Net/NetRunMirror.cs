using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: host-written assault readout plus a one-shot end RPC.
    /// Spawned, not in-scene. A client clone registers with the scene NetSession if one exists
    /// (fixture tests have none — that Find is null-checked).</summary>
    public sealed class NetRunMirror : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        #endregion

        #region State
        GameController _game;
        EndSummary _lastEnd;
        bool _hasEnd;
        #endregion

        #region Properties
        public NetworkVariable<byte> Phase = new NetworkVariable<byte>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> Wave = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> Alive = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<long> Salvage = new NetworkVariable<long>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> HatchHp = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> HatchMax = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> ElapsedSeconds = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<long> Banked = new NetworkVariable<long>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<long> Unbanked = new NetworkVariable<long>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> Intensity = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Host <see cref="WaveMode"/>. Clients need it to tell a Hatch-bank
        /// Assault (buy-back) from a field-Terminal Assault (wait).</summary>
        public NetworkVariable<byte> Mode = new NetworkVariable<byte>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>In-wave respawn charges left on the host sim, so a waiting guest's
        /// RESPAWN label can refuse before the RPC does.</summary>
        public NetworkVariable<int> RespawnStock = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Respawns taken this run, pricing the next one (Respawns.CostFor).</summary>
        public NetworkVariable<int> RespawnsTaken = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Packed hatch assignment (ShipHatches wire form). Written by
        /// <see cref="ServerSetHatchAssign"/> on party change and embark — never from
        /// LateUpdate, which is gated on the Bastion GameController and no-ops on Ship,
        /// where the doors actually stand. 0 until the host first assigns.</summary>
        public NetworkVariable<int> HatchAssign = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Packed realm record (<see cref="RealmRecord"/> wire form) for the realm
        /// the party is playing in. Written by <see cref="ServerSetRealmRecord"/> on Ship load
        /// and at run end — never from LateUpdate, which is gated on the Bastion GameController
        /// and no-ops on the Ship, where the record is read. 0 until the host first publishes,
        /// which unpacks to a fresh realm rather than to a realm at wave zero.</summary>
        public NetworkVariable<long> RealmRecordPacked = new NetworkVariable<long>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool HasEnd => _hasEnd;
        public EndSummary LastEnd => _lastEnd;
        #endregion

        #region Unity Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            var session = FindAnyObjectByType<NetSession>();
            if (session != null)
                session.RegisterMirror(this);
            Wave.OnValueChanged += OnWaveChanged;
            // The mirror is the session's one replicated object that survives scene loads,
            // so it also carries the demo line broadcast. Host only: guests play from the
            // RPC and never re-broadcast.
            if (IsServer)
                DemoVoice.SetBroadcaster(ServerBroadcastDemoLine);
            Log.Event(Tag, "mirror spawned role=" + (IsServer ? "host" : "client"));
        }

        public override void OnNetworkDespawn()
        {
            Wave.OnValueChanged -= OnWaveChanged;
            if (IsServer)
                DemoVoice.ClearBroadcaster(ServerBroadcastDemoLine);
            _hasEnd = false;
            base.OnNetworkDespawn();
        }

        void LateUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            if (_game == null) return;
            var sim = _game.Sim;
            if (sim == null) return;

            byte phase = (byte)sim.Assault;
            int wave = sim.CurrentWaveIndex;
            int alive = _game.Spawner != null ? _game.Spawner.AliveCount : 0;
            long salvage = sim.Salvage != null ? sim.Salvage.RunSalvage : 0L;
            float hatchHp = 0f;
            float hatchMax = 0f;
            if (sim.Ship != null)
            {
                hatchHp = sim.Ship.Hatch.Hp;
                hatchMax = sim.Ship.Hatch.MaxHp;
            }
            int elapsed = (int)sim.RunElapsed;
            long banked = _game.Meta != null ? _game.Meta.BankedSalvage : 0L;
            long unbanked = sim.TotalUnbanked;
            int intensity = sim.Siphon != null ? sim.Siphon.Level : 0;
            byte mode = (byte)sim.Mode;

            if (Phase.Value != phase) Phase.Value = phase;
            if (Wave.Value != wave) Wave.Value = wave;
            if (Alive.Value != alive) Alive.Value = alive;
            if (Salvage.Value != salvage) Salvage.Value = salvage;
            if (HatchHp.Value != hatchHp) HatchHp.Value = hatchHp;
            if (HatchMax.Value != hatchMax) HatchMax.Value = hatchMax;
            if (ElapsedSeconds.Value != elapsed) ElapsedSeconds.Value = elapsed;
            if (Banked.Value != banked) Banked.Value = banked;
            if (Unbanked.Value != unbanked) Unbanked.Value = unbanked;
            if (Intensity.Value != intensity) Intensity.Value = intensity;
            if (Mode.Value != mode) Mode.Value = mode;
            if (RespawnStock.Value != sim.RespawnStock) RespawnStock.Value = sim.RespawnStock;
            if (RespawnsTaken.Value != sim.RespawnsTaken) RespawnsTaken.Value = sim.RespawnsTaken;
        }
        #endregion

        #region Public API
        /// <summary>Host bind from NetSession. Null in fixture tests; LateUpdate no-ops.</summary>
        public void Bind(GameController game)
        {
            _game = game;
        }

        /// <summary>Server: fire the end screen at every client. Host HUD still follows sim.Phase.</summary>
        public void ServerShowEnd(EndSummary summary)
        {
            if (!IsServer || !IsSpawned) return;
            ShowEndScreenClientRpc(summary);
        }

        /// <summary>Server: adopt a packed hatch assignment so every seat's doors read it.</summary>
        public void ServerSetHatchAssign(int packed)
        {
            if (!IsServer || !IsSpawned) return;
            if (HatchAssign.Value != packed) HatchAssign.Value = packed;
        }

        /// <summary>Server: adopt the record of the realm being played, so every seat reads
        /// the host's realm rather than the one in its own save.</summary>
        public void ServerSetRealmRecord(RealmRecord record)
        {
            if (!IsServer || !IsSpawned) return;
            long packed = record.Pack();
            if (RealmRecordPacked.Value != packed) RealmRecordPacked.Value = packed;
        }

        /// <summary>Server: start a Party-scope demo line on every connected seat. The host
        /// seat already played it locally (DemoVoice.PlayLocalAndBroadcast), so the RPC
        /// handler skips on the server. Registered with DemoVoice on host spawn.</summary>
        public void ServerBroadcastDemoLine(string id, DemoVoiceMode mode)
        {
            if (!IsServer || !IsSpawned) return;
            // Plain string param: NGO serializes it, ids are short, and lines fire per
            // beat, not per frame. Unity.Collections is not referenced by this assembly.
            DemoLineClientRpc(id, (byte)mode);
        }
        #endregion

        #region Events
        void OnWaveChanged(int previous, int next)
        {
            Log.Event(Tag, "mirror wave=" + next);
        }

        [ClientRpc]
        void DemoLineClientRpc(string id, byte mode)
        {
            if (IsServer) return; // the host seat played locally before broadcasting
            Log.Event("Demo", "line rpc id=" + id
                + " clientId=" + NetworkManager.LocalClientId);
            DemoVoice.Play(id, (DemoVoiceMode)mode);
        }

        [ClientRpc]
        void ShowEndScreenClientRpc(EndSummary summary)
        {
            if (NetShipWait.IsActive)
                NetShipWait.Exit();
            _lastEnd = summary;
            _hasEnd = true;
            Log.Event(Tag, "end kind=" + (byte)summary.Kind
                + " gross=" + summary.GrossEarned
                + " repair=" + summary.RepairSpent
                + " net=" + summary.Net);
        }
        #endregion
    }
}
