using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: entry for the drone bay's decoy: owns <see cref="DecoyState"/>,
    /// spawns the live prop, and emits the per-Wave <c>Decoy</c> gatekeeper-seconds line.
    /// Placement confirm is HOST-ONLY (a client RPC calls <see cref="TryPlace"/> here);
    /// the ghost is local and predictive.</summary>
    public sealed class DecoyBay : MonoBehaviour
    {
        const string Tag = "Decoy";

        public DecoyState State { get; private set; } = new DecoyState();
        public DecoyObject Live { get; private set; }
        public bool HasPayload { get; private set; }

        DecoyPlacement _placement;
        // Pushed by GameController.ApplyMetaEffects (Bind); registration and the wave-tally
        // event subscriptions run against this handle.
        GameController _game;
        GameObject _spawnPrefab;
        bool _waveTallyOpen;
        bool _subscribed;

        public DecoyPlacement Placement => _placement;
        public GameController Game => _game;

        void Awake()
        {
            _placement = GetComponent<DecoyPlacement>();
            if (_placement == null) _placement = gameObject.AddComponent<DecoyPlacement>();
            var gun = GetComponent<PlayerGun>();
            if (gun == null) gun = GetComponentInParent<PlayerGun>();
            _placement.Bind(this, gun);
        }

        /// <summary>Adopt the scene's GameController (called from ApplyMetaEffects at run
        /// start): take the run's own DecoyState off its RunContext, register as its decoy
        /// bay and subscribe to its Sim's wave events. Optional spawn prefab is the networked
        /// decoy for host place; tests pass one when no NetSession exists.</summary>
        public void Bind(GameController game, GameObject spawnPrefab = null)
        {
            _game = game;
            if (spawnPrefab != null) _spawnPrefab = spawnPrefab;
            if (game != null && game.Run != null) State = game.Run.Decoy;
            Register();
            Subscribe();
        }

        void OnEnable()
        {
            // Re-register on a disable/enable cycle; no-op until the controller has bound us.
            Register();
            Subscribe();
        }

        void OnDisable()
        {
            Unregister();
            Unsubscribe();
        }

        void Register()
        {
            var gc = _game;
            if (gc == null) return;
            gc.RegisterDecoyBay(this);
        }

        void Unregister()
        {
            var gc = _game;
            if (gc != null) gc.UnregisterDecoyBay(this);
        }

        void Subscribe()
        {
            var gc = _game;
            if (gc == null || gc.Sim == null) return;
            if (_subscribed) return;
            gc.Sim.WaveStarted += OnWaveStarted;
            gc.Sim.AssaultEnded += OnAssaultEnded;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            var gc = _game;
            if (gc != null && gc.Sim != null && _subscribed)
            {
                gc.Sim.WaveStarted -= OnWaveStarted;
                gc.Sim.AssaultEnded -= OnAssaultEnded;
            }
            _subscribed = false;
        }

        /// <summary>Call from ApplyMetaEffects at Run start. Resets charges; clears any live decoy.</summary>
        public void ResetForRun(PayloadId equipped)
        {
            HasPayload = equipped == PayloadId.Decoy;
            if (Live != null)
            {
                // The prop's destroy belongs to DecoyObject (it creates itself in Spawn);
                // the bay only drops its handle.
                Live.Despawn();
                Live = null;
            }
            State.ResetForRun(equipped);
            _waveTallyOpen = false;
            if (HasPayload)
                Log.Event(Tag, $"bay loaded — {State.ChargesRemaining} charges, {State.MaxHp:0} HP");
            else
                Log.Event(Tag, "bay empty");
        }

        public bool TryPlace(Vector3 position)
        {
            if (!HasPayload) return false;
            if (!State.TryPlace(position.x, position.y, position.z, PlaceMode(), out string reason))
            {
                Log.Event(Tag, $"place refused — {reason}");
                return false;
            }

            Live = DecoyObject.Spawn(position, this, State.MaxHp, ResolveSpawnPrefab());
            if (Live != null) Live.Configure(this, State.MaxHp, State.Mode);
            Log.Event(Tag,
                $"placed — charges left {State.ChargesRemaining}, hp {State.MaxHp:0}");
            return true;
        }

        public void NotifyDestroyed(DecoyObject obj)
        {
            if (Live == obj) Live = null;
            State.ClearLive();
        }

        void OnWaveStarted(WaveSpec _)
        {
            if (_waveTallyOpen)
            {
                int completed = _game != null && _game.Sim != null
                    ? _game.Sim.CurrentWaveIndex - 1
                    : 0;
                LogWaveAbsorbed(completed);
            }
            _waveTallyOpen = true;
            State.BeginWaveTally();
        }

        void OnAssaultEnded()
        {
            if (!_waveTallyOpen) return;
            int wave = _game != null && _game.Sim != null
                ? _game.Sim.CurrentWaveIndex
                : 0;
            LogWaveAbsorbed(wave);
            _waveTallyOpen = false;
            State.BeginWaveTally();
        }

        void LogWaveAbsorbed(int wave)
        {
            Log.Event(Tag,
                $"wave {wave} gatekeeper-seconds absorbed: {State.WaveAbsorbedSeconds:0.##}");
        }

        GameObject ResolveSpawnPrefab()
        {
            if (_spawnPrefab != null) return _spawnPrefab;
            var gc = _game;
            if (gc == null || gc.Net == null) return null;
            return gc.Net.DecoyPrefab;
        }

        DoctrineMode PlaceMode()
        {
            var meta = _game != null ? _game.Meta : null;
            if (meta == null) return DoctrineMode.Stock;
            return BoarderProtocol.DoctrineOf(meta.EquippedProtocol);
        }
    }
}
