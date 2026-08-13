using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: host-written assault phase, wave index, and live count.
    /// Spawned, not in-scene. A client clone registers with the scene NetSession if one exists
    /// (fixture tests have none — that Find is null-checked).</summary>
    public sealed class NetRunMirror : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        #endregion

        #region State
        GameController _game;
        #endregion

        #region Properties
        public NetworkVariable<byte> Phase = new NetworkVariable<byte>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> Wave = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> Alive = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        #region Unity Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            var session = FindAnyObjectByType<NetSession>();
            if (session != null)
                session.RegisterMirror(this);
            Wave.OnValueChanged += OnWaveChanged;
        }

        public override void OnNetworkDespawn()
        {
            Wave.OnValueChanged -= OnWaveChanged;
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
            if (Phase.Value != phase) Phase.Value = phase;
            if (Wave.Value != wave) Wave.Value = wave;
            if (Alive.Value != alive) Alive.Value = alive;
        }
        #endregion

        #region Public API
        /// <summary>Host bind from NetSession. Null in fixture tests; LateUpdate no-ops.</summary>
        public void Bind(GameController game)
        {
            _game = game;
        }
        #endregion

        #region Events
        void OnWaveChanged(int previous, int next)
        {
            Log.Event(Tag, "mirror wave=" + next);
        }
        #endregion
    }
}
