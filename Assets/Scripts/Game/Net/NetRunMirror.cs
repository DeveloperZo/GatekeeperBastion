using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
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
            Log.Event(Tag, "mirror spawned role=" + (IsServer ? "host" : "client"));
        }

        public override void OnNetworkDespawn()
        {
            Wave.OnValueChanged -= OnWaveChanged;
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
        #endregion

        #region Events
        void OnWaveChanged(int previous, int next)
        {
            Log.Event(Tag, "mirror wave=" + next);
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
