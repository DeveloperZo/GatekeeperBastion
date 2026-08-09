using System.Collections.Generic;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: listens to RunSim.WaveStarted (fired by TriggerDraw and deep-start
    /// chain follow-ups) and spawns each wave's gatekeepers in GROUPS at registered spawn points.
    /// Several WaveSpecs may run concurrently during a deep start — each keeps its own roster
    /// cursor and group timer; AliveCount is the aggregate, and the assault clears only when
    /// every chained wave has finished delivering AND every gatekeeper is dead (and the sim says
    /// ChainPending is false). Spawns ONLY while Tethered; on Lost it stops all spawn
    /// coroutines and despawns remaining gatekeepers.</summary>
    /// <remarks>
    /// WHY GROUPS: one-at-a-time arrivals meant the 12m shout radius only fired when the spacing
    /// happened to be kind, and a Ram would arrive alone and die politely. A group at one door is
    /// the unit of pressure — the shout fires by construction, and a Ram shows up escorted.
    /// The roster and the group emission itself live in WaveSpawner.Groups.cs; this file is the
    /// lifecycle, the alive count and the wave-cleared handshake.
    ///
    /// Teardown contract: _teardown latches on the first terminal phase, on OnDisable, and on
    /// application quit. Once latched, nothing Instantiates and no coroutine restarts — a
    /// straggler spawning into a scene that is unloading is exactly the kind of half-constructed
    /// object that throws during the lose screen.
    /// </remarks>
    public sealed partial class WaveSpawner : MonoBehaviour
    {
        const string Tag = "WaveSpawner";

        /// <summary>Metres from the spawn point to the ring the rest of a group lands on. Big
        /// enough that groupSize agents do not spawn inside one another (agent radius is 0.4);
        /// small enough that they still read as one arrival.</summary>
        const float GroupSpread = 1.1f;

        /// <summary>One in-flight wave's roster and spawn cursor. Deep start holds several at once.</summary>
        sealed class WaveEmit
        {
            public WaveSpec Spec;
            public readonly List<GatekeeperKind> Queue = new List<GatekeeperKind>();
            public int Pending;
            public int Spawned;
            public int Size;
        }

        [Tooltip("Default gatekeeper prefab (capsule + NavMeshAgent + EnemyGatekeeper + Damageable). Wired by SceneBuilder.")]
        public GameObject enemyPrefab;

        [Tooltip("Hunter prefab (HunterGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject hunterPrefab;

        [Tooltip("Ram prefab (RamGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject ramPrefab;

        [Tooltip("Explicit spawn points; when empty, falls back to the SpawnPoint registry.")]
        public SpawnPoint[] spawnPoints;

        /// <summary>Gatekeepers of the current assault spawned and still alive (HUD readout).</summary>
        public int AliveCount { get; private set; }

        readonly List<WaveEmit> _active = new List<WaveEmit>();

        RunSim _sim;
        int _spawnCursor;
        bool _teardown;         // latched: the run is over or the scene is going away
        bool _warnedPrefab;     // one line per run about a missing per-kind prefab, not one per spawn

        void Start()
        {
            var gc = GameController.Instance;
            _sim = gc != null ? gc.Sim : null;
            if (_sim == null)
            {
                Log.Warn(Tag, "No GameController/sim in scene; spawner idle.");
                return;
            }
            Subscribe();
        }

        void OnDisable()
        {
            // Teardown latch + unsubscribe: the sim can outlive this component's enabled state,
            // and a handler on a disabled spawner would try to StartCoroutine (which throws).
            _teardown = true;
            Unsubscribe();
            StopAllCoroutines();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void OnApplicationQuit()
        {
            _teardown = true;
        }

        void Subscribe()
        {
            if (_sim == null) return;
            Unsubscribe(); // idempotent
            _sim.WaveStarted += OnWaveStarted;
            _sim.PhaseChanged += OnPhaseChanged;
        }

        void Unsubscribe()
        {
            if (_sim == null) return;
            _sim.WaveStarted -= OnWaveStarted;
            _sim.PhaseChanged -= OnPhaseChanged;
        }

        void OnWaveStarted(WaveSpec spec)
        {
            if (_teardown || _sim == null || _sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"Wave start ignored (teardown={_teardown}, phase={(_sim == null ? "no-sim" : _sim.Phase.ToString())}).");
                return;
            }
            if (!isActiveAndEnabled)
            {
                // Guard trip: StartCoroutine on an inactive MonoBehaviour throws.
                Log.Warn(Tag, "Wave start ignored: spawner is not active.");
                return;
            }

            var emit = new WaveEmit { Spec = spec };
            BuildRoster(emit, spec);
            emit.Pending = emit.Queue.Count;
            emit.Size = emit.Queue.Count;
            emit.Spawned = 0;
            // Do NOT reset AliveCount — a deep-start chain overlaps waves in the room.
            _active.Add(emit);
            StartCoroutine(SpawnWaveRoutine(emit));
        }

        void OnPhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.Tethered) return;
            if (phase != RunPhase.Lost && phase != RunPhase.Escaped) return;

            // Clean stop on loss: no stragglers spawn into the lose screen, and no gatekeeper is
            // left running its AI over a scene that is about to unload.
            _teardown = true;
            StopAllCoroutines();
            int despawned = 0;
            AliveCount = 0;
            _active.Clear();
            foreach (var gatekeeper in FindObjectsByType<EnemyGatekeeper>(FindObjectsSortMode.None))
            {
                if (gatekeeper == null) continue; // guard: destroyed between the query and here
                gatekeeper.enabled = false;       // stop the AI THIS frame; Destroy is end-of-frame
                Destroy(gatekeeper.gameObject);
                despawned++;
            }
            // Info, not Event: this is teardown bookkeeping that happens AFTER the run summary has
            // been written (GameController subscribes to PhaseChanged first), and a stray line
            // trailing the summary block is exactly the structural noise we removed.
            Log.Info(Tag, $"Terminal phase {phase}: spawning stopped, {despawned} gatekeeper(s) despawned.");
        }

        /// <summary>A gatekeeper is down: decrement first so the kill line's "N remaining" is the
        /// count AFTER this death, then bank the kill and re-test the wave.</summary>
        /// <remarks>Convention R1: the award goes through GameController.GatekeeperKilled(), never
        /// straight into the sim. The value is READ BEFORE the award so the line reports what
        /// THIS kill paid (the sim owns the number; we only quote it).</remarks>
        void OnGatekeeperDied(GatekeeperKind kind, Damageable damageable)
        {
            AliveCount = Mathf.Max(0, AliveCount - 1);

            var gc = GameController.Instance;
            float award = gc != null && gc.Sim != null ? gc.Sim.CurrentKillValue : 0f;
            if (gc != null) gc.GatekeeperKilled();
            else Log.Warn(Tag, "Kill not awarded: no GameController in the scene.");

            string line = damageable != null && damageable.HasLastDamagingSlot
                ? KillLog.Format(KindName(kind), damageable.LastDamagingSlot, award, AliveCount)
                : KillLog.FormatUnknownSlot(KindName(kind), award, AliveCount);
            Log.Event("Combat", line);
            CheckAssaultCleared();
        }

        /// <summary>Assault dead = every active wave finished spawning, every spawned gatekeeper
        /// is down, and the sim is not still chaining more waves → tell the sim.</summary>
        void CheckAssaultCleared()
        {
            if (_teardown) return; // guard: never call into the sim from a terminal/teardown state
            if (_sim == null || _sim.Phase != RunPhase.Tethered) return;
            if (_sim.Assault != AssaultState.Assault) return;
            if (_sim.ChainPending) return;
            if (_active.Count > 0) return; // at least one wave still delivering
            if (AliveCount > 0) return;
            // The "wave N cleared" line is GameController's (on AssaultEnded)
            // — it has the duration, salvage and hatch HP this call site does not.
            _sim.NotifyAssaultCleared();
        }

        /// <summary>Dev/replay: stop spawning and destroy every live gatekeeper WITHOUT awarding
        /// kills or notifying assault-cleared. Caller decides whether to AbortAssault or
        /// NotifyAssaultCleared. Returns how many actors were destroyed.</summary>
        public int DespawnAllLive()
        {
            StopAllCoroutines();
            _active.Clear();
            AliveCount = 0;
            int despawned = 0;
            foreach (var gatekeeper in FindObjectsByType<EnemyGatekeeper>(FindObjectsSortMode.None))
            {
                if (gatekeeper == null) continue;
                gatekeeper.enabled = false;
                Destroy(gatekeeper.gameObject);
                despawned++;
            }
            return despawned;
        }

        /// <summary>The next spawn point in rotation. Called ONCE PER GROUP, not per gatekeeper:
        /// a group arriving together at one door is the point.</summary>
        Transform PickSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Skip destroyed/missing entries rather than returning a fake-null transform.
                for (int attempt = 0; attempt < spawnPoints.Length; attempt++)
                {
                    var bp = spawnPoints[_spawnCursor++ % spawnPoints.Length];
                    if (bp != null) return bp.transform;
                }
                Log.Warn(Tag, "All wired spawn points are missing; falling back to the registry.");
            }
            var all = SpawnPoint.All;
            if (all == null || all.Count == 0) return null;
            for (int attempt = 0; attempt < all.Count; attempt++)
            {
                var bp = all[_spawnCursor++ % all.Count];
                if (bp != null) return bp.transform;
            }
            return null;
        }
    }
}
