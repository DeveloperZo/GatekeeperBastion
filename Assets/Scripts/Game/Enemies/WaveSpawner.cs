using System.Collections.Generic;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: listens to RunSim.WaveStarted (fired by TriggerDraw and deep-start
    /// chain follow-ups) and spawns each wave's gatekeepers in GROUPS at registered spawn points.
    /// Several WaveSpecs may run concurrently during a deep start — each keeps its own roster
    /// cursor and group timer; AliveCount is the aggregate, and the assault clears only when
    /// every chained wave has finished delivering AND every gatekeeper is dead (and the sim says
    /// ChainPending is false). Spawns ONLY while Tethered; on Lost it stops delivery
    /// and despawns remaining gatekeepers.</summary>
    /// <remarks>
    /// WHY GROUPS: one-at-a-time arrivals meant the 12m shout radius only fired when the spacing
    /// happened to be kind, and a Ram would arrive alone and die politely. A group at one door is
    /// the unit of pressure — the shout fires by construction, and a Ram shows up escorted.
    /// The roster and the group emission itself live in WaveSpawner.Groups.cs; this file is the
    /// lifecycle, the alive count and the wave-cleared handshake.
    ///
    /// Teardown contract: _teardown latches on the first terminal phase, on OnDisable, and on
    /// application quit. Once latched, nothing Instantiates and delivery never advances again — a
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

        /// <summary>One in-flight wave's roster, spawn cursor and delivery state. Deep start
        /// holds several at once; each is advanced by <see cref="TickDelivery"/>.</summary>
        sealed class WaveEmit
        {
            public WaveSpec Spec;
            public readonly List<GatekeeperKind> Queue = new List<GatekeeperKind>();
            public int Spawned;
            public int Size;
            /// <summary>Seconds until the next group may emit. Runs down unconditionally
            /// (the cap hold only starts once it has expired).</summary>
            public float Countdown;
            /// <summary>Countdown expired but the group is held by the aggregate alive cap.</summary>
            public bool CapWaiting;
            public int GroupNumber;
            /// <summary>Delivery bailed (teardown / phase left Tethered). The wave stays in
            /// _active — so the assault cannot read as cleared — until teardown sweeps it.</summary>
            public bool Dead;
            /// <summary>Frame the wave started. Its first advance ran synchronously that frame,
            /// so TickDelivery skips it until the next one (no double-charged dt).</summary>
            public int BornFrame;
        }

        [Tooltip("Default gatekeeper prefab (capsule + NavMeshAgent + EnemyGatekeeper + Damageable). Wired by SceneBuilder.")]
        public GameObject enemyPrefab;

        [Tooltip("Hunter prefab (HunterGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject hunterPrefab;

        [Tooltip("Ram prefab (RamGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject ramPrefab;

        [Tooltip("Explicit spawn points; when empty, falls back to the SpawnPoint registry.")]
        public SpawnPoint[] spawnPoints;

        [SerializeField, Tooltip("Bastion GameController. Authored in the scene.")]
        GameController _game;

        /// <summary>Gatekeepers of the current assault spawned and still alive (HUD readout).</summary>
        public int AliveCount { get; private set; }

        readonly List<WaveEmit> _active = new List<WaveEmit>();

        /// <summary>Every gatekeeper this spawner Instantiated and has not yet seen die or
        /// despawn. Entries can be fake-null (destroyed elsewhere) — iterate with == null checks.</summary>
        readonly List<EnemyGatekeeper> _live = new List<EnemyGatekeeper>();

        /// <summary>The live ledger, read-only: ability cones and the provoke shout iterate this
        /// instead of a process-global actor list, so two worlds could never share it. Entries can
        /// be fake-null — callers keep their == null checks.</summary>
        public IReadOnlyList<EnemyGatekeeper> Live => _live;

        RunSim _sim;
        int _spawnCursor;
        bool _teardown;         // latched: the run is over or the scene is going away
        bool _warnedPrefab;     // one line per run about a missing per-kind prefab, not one per spawn
        bool _warnedNoRng;      // one line per run about a missing sim RNG, not one per wave

        void Start()
        {
            if (_game == null)
            {
                Log.Error(Tag, "No GameController wired (_game); spawner disabled.");
                enabled = false;
                return;
            }
            _sim = _game.Sim;
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
            // and TickDelivery refuses to advance anything once _teardown is latched.
            _teardown = true;
            Unsubscribe();
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
                // Guard trip: an inactive spawner never ticks delivery — the wave would stall
                // silently, so refuse it loudly instead.
                Log.Warn(Tag, "Wave start ignored: spawner is not active.");
                return;
            }

            var emit = new WaveEmit { Spec = spec, BornFrame = Time.frameCount };
            BuildRoster(emit, spec);
            emit.Size = emit.Queue.Count;
            emit.Spawned = 0;
            // Do NOT reset AliveCount — a deep-start chain overlaps waves in the room.
            _active.Add(emit);
            // First advance runs NOW: the first group emits on the wave-start frame (the old
            // coroutine ran to its first yield synchronously). BornFrame keeps TickDelivery
            // off this wave until next frame so the first interval is not charged this
            // frame's dt, which elapsed before the wave existed.
            if (AdvanceWave(emit, 0f))
            {
                _active.Remove(emit);
                CheckAssaultCleared();
            }
        }

        /// <summary>True once the Bastion's tick owner drives delivery (set by the
        /// GameController that owns this spawner); its own Update then yields so the written
        /// system order is the only order.</summary>
        bool _hosted;

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return; // the Bastion tick owner calls TickDelivery in the written order
            TickDelivery(Time.deltaTime);
        }

        void OnPhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.Tethered) return;
            if (phase != RunPhase.Lost && phase != RunPhase.Escaped) return;

            // Clean stop on loss: no stragglers spawn into the lose screen, and no gatekeeper is
            // left running its AI over a scene that is about to unload.
            _teardown = true;
            int despawned = DestroyTrackedGatekeepers();
            AliveCount = 0;
            _active.Clear();
            // Info, not Event: this is teardown bookkeeping that happens AFTER the run summary has
            // been written (GameController subscribes to PhaseChanged first), and a stray line
            // trailing the summary block is exactly the structural noise we removed.
            Log.Info(Tag, $"Terminal phase {phase}: spawning stopped, {despawned} gatekeeper(s) despawned.");
        }

        /// <summary>A gatekeeper is down: decrement first so the kill line's "N remaining" is the
        /// count AFTER this death, then bank the kill and re-test the wave.</summary>
        /// <remarks>Convention R1: the award goes through GameController.GatekeeperKilled(), never
        /// straight into the sim. The value is READ BEFORE the award so the line reports what
        /// THIS kill paid (the sim owns the number; we only quote it). The kill line carries the
        /// actor's stable spawn id — wave-report's field regexes tolerate the trailing token.</remarks>
        void OnGatekeeperDied(GatekeeperKind kind, Damageable damageable, EnemyGatekeeper actor)
        {
            AliveCount = Mathf.Max(0, AliveCount - 1);

            var gc = _game;
            long award = gc != null && gc.Sim != null ? gc.Sim.CurrentKillAward : 0L;
            if (gc != null) gc.GatekeeperKilled();
            else Log.Warn(Tag, "Kill not awarded: no GameController in the scene.");

            KillDistances(damageable, out float hatchDist, out float spawnDist);
            string line = damageable != null && damageable.HasLastDamagingSlot
                ? KillLog.Format(KindName(kind), damageable.LastDamagingSlot, award, AliveCount,
                    hatchDist, spawnDist)
                : KillLog.FormatUnknownSlot(KindName(kind), award, AliveCount, hatchDist, spawnDist);
            if (actor != null) line = line + " id " + actor.SpawnId.ToString();
            var netEnemy = actor != null ? actor.GetComponent<NetEnemy>() : null;
            if (netEnemy != null && netEnemy.IsSpawned)
                line = line + " netId=" + netEnemy.NetworkObjectId;
            Log.Event("Combat", line);
            CheckAssaultCleared();
        }

        /// <summary>Planar metres from the body to the Hatch surface and to SpawnedAt.
        /// spawnDist is -1 when SpawnedAt was never written (hand-placed / unset).</summary>
        static void KillDistances(Damageable damageable, out float hatchDist, out float spawnDist)
        {
            hatchDist = 0f;
            spawnDist = -1f;
            if (damageable == null) return;

            Vector3 pos = damageable.transform.position;
            var hatch = HatchStation.Instance;
            if (hatch != null)
            {
                Vector3 surface = hatch.ClosestSurfacePoint(pos);
                hatchDist = KillLog.PlanarMetres(
                    pos.x, pos.y, pos.z, surface.x, surface.y, surface.z);
            }
            else
                Log.Warn(Tag, "Kill hatchDist measured with no HatchStation in the scene.");

            var gatekeeper = damageable.GetComponent<EnemyGatekeeper>();
            if (gatekeeper != null && gatekeeper.SpawnedAt != default)
            {
                Vector3 at = gatekeeper.SpawnedAt;
                spawnDist = KillLog.PlanarMetres(pos.x, pos.y, pos.z, at.x, at.y, at.z);
            }
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
            _active.Clear();
            AliveCount = 0;
            return DestroyTrackedGatekeepers();
        }

        /// <summary>THE one destroy path for a spawned gatekeeper: drop it from the spawn ledger
        /// and destroy the actor. EnemyGatekeeper routes its death here (it holds this spawner
        /// from Configure), and the teardown/dev sweeps go through here too — nothing else
        /// Destroys a spawned gatekeeper.</summary>
        public void Despawn(EnemyGatekeeper gatekeeper)
        {
            if (gatekeeper == null) return; // guard: destroyed between bookkeeping and here
            _live.Remove(gatekeeper);
            var netObj = gatekeeper.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
                return;
            }
            Destroy(gatekeeper.gameObject);
        }

        /// <summary>Destroy every gatekeeper in the spawn ledger and clear it. Works on a copy:
        /// Destroy side-effects must not mutate the list mid-walk. Returns how many actors
        /// were actually destroyed (fake-null entries are skipped, not counted).</summary>
        int DestroyTrackedGatekeepers()
        {
            var live = _live.ToArray();
            _live.Clear();
            int despawned = 0;
            for (int i = 0; i < live.Length; i++)
            {
                var gatekeeper = live[i];
                if (gatekeeper == null) continue; // guard: destroyed between bookkeeping and here
                gatekeeper.enabled = false;       // stop the AI THIS frame; Destroy is end-of-frame
                Despawn(gatekeeper);
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
