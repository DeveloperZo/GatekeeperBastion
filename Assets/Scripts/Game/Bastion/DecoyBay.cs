using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL + HOST entry for the drone bay's decoy: owns <see cref="DecoyState"/>,
    /// spawns the live prop, and emits the per-Wave <c>Decoy</c> gatekeeper-seconds line.
    /// Placement confirm is HOST-ONLY; the ghost is local and predictive.</summary>
    public sealed class DecoyBay : MonoBehaviour
    {
        const string Tag = "Decoy";

        public DecoyState State { get; private set; } = new DecoyState();
        public DecoyObject Live { get; private set; }
        public bool HasPayload { get; private set; }

        DecoyPlacement _placement;
        bool _waveTallyOpen;
        bool _subscribed;
        bool _registered;

        public DecoyPlacement Placement => _placement;

        void Awake()
        {
            _placement = GetComponent<DecoyPlacement>();
            if (_placement == null) _placement = gameObject.AddComponent<DecoyPlacement>();
            var gun = GetComponent<PlayerGun>();
            if (gun == null) gun = GetComponentInParent<PlayerGun>();
            _placement.Bind(this, gun);
        }

        void OnEnable()
        {
            TryRegister();
            TrySubscribe();
        }

        void OnDisable()
        {
            Unregister();
            Unsubscribe();
        }

        void Update()
        {
            // The controller can enable after the bay; both hooks retry until it exists.
            if (!_registered) TryRegister();
            if (!_subscribed) TrySubscribe();
        }

        void TryRegister()
        {
            var gc = GameController.Instance;
            if (gc == null) return;
            gc.RegisterDecoyBay(this);
            _registered = true;
        }

        void Unregister()
        {
            _registered = false;
            var gc = GameController.Instance;
            if (gc != null) gc.UnregisterDecoyBay(this);
        }

        void TrySubscribe()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.Sim == null) return;
            if (_subscribed) return;
            gc.Sim.WaveStarted += OnWaveStarted;
            gc.Sim.AssaultEnded += OnAssaultEnded;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            var gc = GameController.Instance;
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
                Destroy(Live.gameObject);
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
            if (!State.TryPlace(position.x, position.y, position.z, out string reason))
            {
                Log.Event(Tag, $"place refused — {reason}");
                return false;
            }

            Live = DecoyObject.Spawn(position, this, State.MaxHp);
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
                int completed = GameController.Instance != null && GameController.Instance.Sim != null
                    ? GameController.Instance.Sim.CurrentWaveIndex - 1
                    : 0;
                LogWaveAbsorbed(completed);
            }
            _waveTallyOpen = true;
            State.BeginWaveTally();
        }

        void OnAssaultEnded()
        {
            if (!_waveTallyOpen) return;
            int wave = GameController.Instance != null && GameController.Instance.Sim != null
                ? GameController.Instance.Sim.CurrentWaveIndex
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
    }
}
