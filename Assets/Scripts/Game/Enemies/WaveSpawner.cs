using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace GatekeepersProtocol.Game
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
    /// happened to be kind, and a Thief would arrive alone and die politely. A group at one door is
    /// the unit of pressure — the shout fires by construction, and a Thief shows up escorted.
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
            /// <summary>The next Thief spawned from this wave wears <see cref="MasterControl.FinaleEliteHp"/>.
            /// Latched from <see cref="WaveSpec.finaleElite"/> at roster build.</summary>
            public bool FinaleElitePending;
        }

        sealed class Corpse
        {
            public EnemyGatekeeper Actor;
            public float Remaining;
        }

        [Tooltip("Default gatekeeper prefab (capsule + NavMeshAgent + EnemyGatekeeper + Damageable). Wired by SceneBuilder.")]
        public GameObject enemyPrefab;

        [Tooltip("Hunter prefab (HunterGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject hunterPrefab;

        // FormerlySerializedAs: this field was `ramPrefab` until the Thief rename. Every scene
        // that already wired it keeps its reference without being re-saved, which matters because
        // Bastion.unity is the only scene that wires it and re-saving a scene to migrate a field
        // name is a scene write nobody asked for.
        [FormerlySerializedAs("ramPrefab")]
        [Tooltip("Thief prefab (ThiefGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject thiefPrefab;

        [Tooltip("Cloaker prefab (CloakerGatekeeper). Falls back to enemyPrefab when unassigned.")]
        public GameObject cloakerPrefab;

        [Tooltip("Explicit spawn points; when empty, falls back to the SpawnPoint registry.")]
        public SpawnPoint[] spawnPoints;

        [SerializeField, Tooltip("Bastion GameController. Authored in the scene.")]
        GameController _game;

        /// <summary>Gatekeepers of the current assault spawned and still alive (HUD readout).</summary>
        public int AliveCount { get; private set; }

        readonly List<WaveEmit> _active = new List<WaveEmit>();

        /// <summary>Drip clock for the hold-until-shutoff trickle. See FieldTrickle.</summary>
        float _trickleAccrued;

        /// <summary>The spec a trickle body is built from — latched from the last field wave
        /// that launched, so the drip is that wave's Default arriving late rather than a second
        /// stat line that can drift away from it.</summary>
        WaveSpec _trickleSpec;

        /// <summary>Every gatekeeper this spawner Instantiated and has not yet seen die or
        /// despawn. Entries can be fake-null (destroyed elsewhere) — iterate with == null checks.</summary>
        readonly List<EnemyGatekeeper> _live = new List<EnemyGatekeeper>();

        /// <summary>Bodies already dropped from <see cref="_live"/> whose destroy tail has
        /// not fired. Teardown must still wipe these; a wave clear must not wait on them.</summary>
        readonly List<Corpse> _corpses = new List<Corpse>();

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

            // Latch the spec the drip will copy, and clear any drip left over from a previous
            // zone: a fresh wave owns the lane until ITS roster runs dry.
            _trickleSpec = spec;
            _trickleAccrued = 0f;
            if (_sim != null) _sim.SetFieldTrickle(false);

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
            if (phase != RunPhase.Lost && phase != RunPhase.Escaped
                && phase != RunPhase.Returned) return;

            // Clean stop on any run end: no stragglers spawn into the end screen, and no gatekeeper
            // is left running its AI over a scene that is about to unload.
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
        /// is down, and the sim is not still chaining more waves → tell the sim.
        ///
        /// <para>EXCEPT while the zone still owes a shutoff (Zo, 2026-08-26). A field wave ENDS
        /// AT A TERMINAL, so running the roster dry is not the end of it: the lane keeps
        /// dripping (<see cref="TickFieldTrickle"/>) and the Assault stays live until the
        /// player walks up and turns it off. Clearing here is what let a player hold the Hatch,
        /// kill all 48 authored bodies and then stroll an empty corridor to press a button —
        /// the objective was positional and the pressure was not.</para></summary>
        void CheckAssaultCleared()
        {
            if (_teardown) return; // guard: never call into the sim from a terminal/teardown state
            if (_sim == null) return;
            // The DECISION lives in FieldTrickle.Resolve — including the check order that
            // keeps the pay-zero flag off a field the player is still fighting. This shell
            // only gathers the values and applies the verdict; putting any condition back
            // here recreates the 2026-08-28 two-homes deadlock.
            switch (FieldTrickle.Resolve(_sim.Phase, _sim.Assault, _sim.ChainPending,
                _active.Count > 0, AliveCount, _game != null && _game.FieldWaveAwaitsShutoff))
            {
                case FieldWaveResolution.StartTrickle:
                    // Roster spent, field empty, shutoff owed: hand the rest of the wave to
                    // the drip and hold the Assault open.
                    _sim.SetFieldTrickle(true);
                    return;
                case FieldWaveResolution.Cleared:
                    // The "wave N cleared" line is GameController's (on AssaultEnded)
                    // — it has the duration, salvage and hatch HP this call site does not.
                    _sim.NotifyAssaultCleared();
                    return;
                default:
                    return;
            }
        }

        /// <summary>One tick of the drip that holds a field wave open until its terminal is shut
        /// off. Emits at most one body per tick, from the same weighted rotation the wave used,
        /// so the trickle arrives up the lane where the player is heading rather than behind
        /// them. Silent by design: <see cref="FieldTrickle"/> bodies pay nothing and a line per
        /// body would bury the wave's own log.</summary>
        void TickFieldTrickle(float dt)
        {
            if (_teardown) return;
            if (_sim == null || _sim.Phase != RunPhase.Tethered) return;
            if (_sim.Assault != AssaultState.Assault) return;
            if (_active.Count > 0) return;          // the authored roster still has bodies owed
            if (_game == null || !_game.FieldWaveAwaitsShutoff)
            {
                _trickleAccrued = 0f;
                return;
            }
            // THE FLAG IS THE GATE, NOT A LABEL ON IT (2026-08-28). This used to drip as soon
            // as the ROSTER was spent, while TickWaveCompletion only raises FieldTrickleLive
            // once the FIELD IS EMPTY. The two conditions disagreed, and the disagreement was
            // self-sustaining: the drip kept a body alive, so AliveCount never reached zero, so
            // the flag was never raised, so every dripped body billed the player the full award
            // and the wave could never clear. The 07:17 run is the evidence — wave 5 authored
            // 39 bodies and the player killed 93, all of them at +42, and had to abandon.
            //
            // Deriving the drip from the flag makes that impossible to reproduce: the one place
            // that decides the field is empty is the one place that starts the drip.
            if (!_sim.FieldTrickleLive)
            {
                _trickleAccrued = 0f;
                return;
            }
            if (_game.Net != null && _game.Net.IsClientOnly) return;
            if (!FieldTrickle.DueIn(dt, AliveCount, ref _trickleAccrued)) return;
            if (AliveCount >= WaveDirector.MaxEnemyCount) return;

            Transform point = PickSpawnPoint();
            if (point == null) return;
            SpawnTrickleBody(point);
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
        /// immediately, then destroy the actor. <paramref name="corpseSeconds"/> defers only the
        /// destroy tail (default 0, so every other caller is unchanged). A corpse must never
        /// delay a wave clear — AliveCount already dropped in <see cref="OnGatekeeperDied"/>.</summary>
        public void Despawn(EnemyGatekeeper gatekeeper, float corpseSeconds = 0f)
        {
            if (gatekeeper == null) return; // guard: destroyed between bookkeeping and here
            _live.Remove(gatekeeper);
            if (corpseSeconds > 0f)
            {
                _corpses.Add(new Corpse { Actor = gatekeeper, Remaining = corpseSeconds });
                return;
            }
            DestroyActor(gatekeeper);
        }

        void TickCorpses(float dt)
        {
            for (int i = _corpses.Count - 1; i >= 0; i--)
            {
                Corpse corpse = _corpses[i];
                if (corpse.Actor == null)
                {
                    _corpses.RemoveAt(i);
                    continue;
                }
                corpse.Remaining -= dt;
                if (corpse.Remaining > 0f) continue;
                _corpses.RemoveAt(i);
                DestroyActor(corpse.Actor);
            }
        }

        void DestroyActor(EnemyGatekeeper gatekeeper)
        {
            if (gatekeeper == null) return; // guard: destroyed between bookkeeping and here
            var netObj = gatekeeper.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
                return;
            }
            Destroy(gatekeeper.gameObject);
        }

        /// <summary>Destroy every gatekeeper in the spawn ledger and any pending corpses.
        /// Works on a copy: Destroy side-effects must not mutate the list mid-walk. Returns
        /// how many actors were actually destroyed (fake-null entries are skipped, not counted).</summary>
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
                DestroyActor(gatekeeper);
                despawned++;
            }
            for (int i = 0; i < _corpses.Count; i++)
            {
                var gatekeeper = _corpses[i].Actor;
                if (gatekeeper == null) continue;
                gatekeeper.enabled = false;
                DestroyActor(gatekeeper);
                despawned++;
            }
            _corpses.Clear();
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
                    if (bp != null && bp.gameObject.activeInHierarchy) return bp.transform;
                }
                Log.Warn(Tag, "All wired spawn points are missing; falling back to the registry.");
            }
            var all = SpawnPoint.All;
            if (all == null || all.Count == 0) return null;

            // WEIGHTED round-robin (Zo, 2026-08-25): a flat rotation put an equal share
            // through every door, so the closet at the far end of the lane — the one whose
            // enemies have to walk the whole approach the player is holding — delivered no
            // more than the one at his elbow. Each closet now takes Weight consecutive
            // slots in the rotation, so "most of the wave arrives from the far end" is a
            // number on the closet rather than a hand-placed extra door.
            int total = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var sp = all[i];
                if (sp != null && sp.gameObject.activeInHierarchy) total += sp.Weight;
            }
            if (total <= 0) return null;

            for (int attempt = 0; attempt < total; attempt++)
            {
                int slot = _spawnCursor++ % total;
                for (int i = 0; i < all.Count; i++)
                {
                    var sp = all[i];
                    if (sp == null || !sp.gameObject.activeInHierarchy) continue;
                    slot -= sp.Weight;
                    if (slot < 0) return sp.transform;
                }
            }
            return null;
        }
    }
}
