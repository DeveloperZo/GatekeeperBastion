using System.Collections.Generic;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: NavMesh gatekeeper — paths to the Hatch (HatchStation.Instance) and
    /// attacks it on an interval; harasses the NEAREST LIVE player (PlayerRegistry, re-picked
    /// every retarget) within aggro radius, hunting players outright once the hatch is gone.
    /// BASE CLASS for the three kinds: <see cref="HunterGatekeeper"/> and <see cref="RamGatekeeper"/>
    /// override only the targeting rule (see EnemyGatekeeper.Targeting.cs) and their crowd priority;
    /// every number they differ by is pushed from the sim through <see cref="Configure"/>.</summary>
    /// <remarks>
    /// The AI hard-stops the moment the run reaches a terminal phase. Nothing used to stop it:
    /// Update/Retarget/Attack kept running every frame after the player went down — still
    /// damaging the hatch, still spawning FX, still trusting cached targets that the WaveSpawner
    /// had just queued for destruction. Now Stop() disables the component once and for all, and
    /// every cached reference (_player, _hatchTarget, _agent) is re-checked Unity-null-safe at
    /// the point of use rather than trusted from the last retarget tick.
    ///
    /// SUBCLASSES CARRY NO BEHAVIOUR OF THEIR OWN BEYOND A REFUSAL. The two flags in
    /// GatekeeperStats (ignoresHatch / ignoresPlayer) drive the shared Retarget() directly, so a
    /// gatekeeper configured with Hunter stats behaves like a Hunter whatever prefab it came from.
    /// The subclasses exist to carry identity (Kind, and therefore colour/scale/prefab), to make
    /// the refusal unmissable at the call site, and to harden it: HunterGatekeeper cannot latch onto
    /// the hatch and RamGatekeeper cannot be provoked EVEN IF the flags were mis-pushed.
    /// </remarks>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Damageable))]
    public partial class EnemyGatekeeper : MonoBehaviour
    {
        protected const string Tag = "EnemyGatekeeper";

        // Combat tuning: NONE of the per-kind numbers below survive spawn — WaveSpawner pushes
        // WaveDirector.StatsFor(kind) onto every gatekeeper through Configure(). The serialized
        // values are only what a hand-placed gatekeeper in an unbuilt scene would fall back to.
        // Enemy HP is not set here either: WaveSpawner applies stats.maxHp times the wave's
        // multiplier, so a prefab's own value can never silently rescale a wave.
        [SerializeField] float attackRange = 2.4f;

        // The hatch is a 4m-wide box, not a person: a gatekeeper anywhere along its face is "close
        // enough" and starts wrecking it immediately. Player melee stays tight at attackRange.
        [SerializeField] float hatchAttackRange = 3.5f;

        /// <summary>Extra metres a target may drift before a gatekeeper counts as out of reach.</summary>
        const float ReachHysteresis = 1.2f;
        [SerializeField] float attackInterval = 0.7f;
        [SerializeField] float hatchDamage = 6f;
        [SerializeField] float playerDamage = 18f;
        [SerializeField] float aggroRadius = 8f;
        [SerializeField] float retargetInterval = 0.75f;

        // AGGRO. A gatekeeper's default job is the hatch, but the player must never be able to stand
        // out of reach and pick the queue apart. Two things flip a gatekeeper onto the player, and
        // once flipped it stays flipped for life:
        //   1. being shot (Damageable.Damaged carries the shooter) — you shot it, it is yours;
        //   2. seeing the player within noticeRange with clear line of sight.
        // A provoked gatekeeper also shouts: every other gatekeeper within alertRadius joins the hunt,
        // so sniping one out of a group pulls the group. Provoked movement is faster than the
        // player's 5 m/s walk (chaseSpeed) — otherwise kiting backwards is a free win.
        // HOME COMES FIRST: once a gatekeeper is this close to the hatch it commits to wrecking it,
        // even if the player shot it and even if the player is standing right there. Aggro decides
        // who they hunt across the bastion; proximity to the hatch overrides that entirely.
        [SerializeField] float hatchCommitRadius = 8f;
        [SerializeField] float noticeRange = 26f;
        [SerializeField] float alertRadius = 12f;
        [SerializeField] float chaseSpeed = 5.6f;

        // The two refusals, pushed from GatekeeperStats at spawn. Read through the virtual
        // MayTargetHatch / MayTargetPlayer / CanBeProvoked in EnemyGatekeeper.Targeting.cs.
        protected bool ignoresHatch;
        protected bool ignoresPlayer;

        bool _provoked;
        bool _committedToHatch;
        bool _wasInRange;
        bool _loggedHatchHit;
        float _committedAt; // diagnostic wall-clock (Time.time) for logs/readouts — not sim state
        Vector3? _standPoint;
        Vector3 _lastDestination = new Vector3(9999f, 9999f, 9999f);
        float _pinRemaining;

        NavMeshAgent _agent;
        Damageable _damageable;
        // Pushed by WaveSpawner at spawn (Configure). Null on a hand-placed actor, which then
        // treats the run as over and never acts — spawner-spawned is the supported path.
        protected GameController _game;
        // Pushed alongside _game at spawn: the one owner of this actor's destroy. Death routes
        // through _spawner.Despawn so the spawn ledger and the Destroy live in one place.
        WaveSpawner _spawner;
        PlayerHealth _player;
        HatchStation _hatchTarget;
        DecoyObject _decoyTarget;
        bool _chasingPlayer;
        bool _chasingDecoy;
        float _attackTimer;
        float _retargetTimer;

        /// <summary>World position written once at Instantiate (the GroupSlot sample). Left at
        /// default when never spawned through WaveSpawner — kill line then writes spawnDist=-1.</summary>
        public Vector3 SpawnedAt;

        /// <summary>Stable per-run spawn id (RunContext.TakeSpawnId), written once at spawn.
        /// 0 on a hand-placed actor. The kill line quotes it so a body can be traced back to
        /// its spawn across a whole run's log.</summary>
        public int SpawnId { get; set; }

        /// <summary>Which kind this actor is. Identity only — behaviour comes from the flags, so
        /// this is what the kill line and the prefab palette read, never the AI.</summary>
        public virtual GatekeeperKind Kind => GatekeeperKind.Default;

        /// <summary>True while a pin holds this gatekeeper in place. Commit latch is unchanged.</summary>
        public bool IsPinned => _pinRemaining > 0f;

        /// <summary>True once the hatch-commit latch has closed. Shove and pin do not clear it.</summary>
        public bool IsCommittedToHatch => _committedToHatch;

        /// <summary>Dev F9 readout: who this gatekeeper is hunting right now.</summary>
        public string DevTargetLabel =>
            _chasingDecoy ? "decoy" :
            _chasingPlayer ? "player" : (_committedToHatch ? "hatch" : (_hatchTarget != null ? "hatch?" : "idle"));

        /// <summary>Dev F9 readout: seconds since hatch commit (0 when not parked).</summary>
        public float DevTimeParked =>
            _committedToHatch ? Mathf.Max(0f, Time.time - _committedAt) : 0f;

        /// <summary>Crowd avoidance quality. 42 simultaneous agents is 3x the old load, so the
        /// chaff kinds get a cheaper solver; RamGatekeeper keeps a high-quality one because its whole
        /// job is shouldering through a crowd to reach the hatch.</summary>
        protected virtual ObstacleAvoidanceType AvoidanceQuality =>
            ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        /// <summary>Equal-priority agents push each other symmetrically and orbit a shared
        /// destination forever; staggered priorities let a crowd settle against the hatch. LOWER
        /// is stronger — RamGatekeeper overrides into a band that outranks every chaff gatekeeper.</summary>
        protected virtual int AvoidancePriority => 30 + (GetInstanceID() & 0x1F);

        /// <summary>Push tuning from the sim onto this gatekeeper at spawn. WaveSpawner calls this so
        /// the SIM is the single source of truth: a prefab's serialized fields are frozen at the
        /// moment the asset was created, so editing a [SerializeField] default in code does NOT
        /// reach an existing prefab. That silently cost a whole tuning pass — gatekeepers kept hitting
        /// for the original 4 damage every 1.2s while the code said 18 every 0.7s. With three kinds
        /// the same trap is three times as easy to fall into, so EVERY per-kind number (including
        /// the two refusal flags) arrives here and none of them live only in a prefab.</summary>
        public void Configure(GatekeeperStats stats)
        {
            Configure(stats.playerDamage, stats.hatchDamage, stats.attackInterval,
                      stats.patrolSpeed, stats.chaseSpeed, stats.ignoresHatch, stats.ignoresPlayer);
        }

        /// <summary>Stats push plus the controller handle — the controller is what this actor
        /// reads run state (RunIsOver, Decoys) from.</summary>
        public void Configure(GatekeeperStats stats, GameController game)
        {
            _game = game;
            Configure(stats);
        }

        /// <summary>WaveSpawner's spawn path: stats, controller, and the spawner that owns this
        /// actor's lifetime — death routes back through <see cref="WaveSpawner.Despawn"/>.</summary>
        public void Configure(GatekeeperStats stats, GameController game, WaveSpawner spawner)
        {
            _spawner = spawner;
            Configure(stats, game);
        }

        /// <summary>The primitive form of <see cref="Configure(GatekeeperStats)"/> — the only place
        /// that writes the tuning fields.</summary>
        public void Configure(float toPlayer, float toHatch, float interval, float patrol,
                              float chase, bool noHatch, bool noPlayer)
        {
            playerDamage = Mathf.Max(0f, toPlayer);
            hatchDamage = Mathf.Max(0f, toHatch);
            attackInterval = Mathf.Max(0.05f, interval);
            chaseSpeed = Mathf.Max(0.1f, chase);
            ignoresHatch = noHatch;
            ignoresPlayer = noPlayer;
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                // A kind whose patrol speed equals its chase speed (Hunter) is already at full
                // tilt from the breach; nothing needs to provoke it into moving properly.
                _agent.speed = _provoked ? chaseSpeed : Mathf.Max(0.1f, patrol);
                // The hatch destination is inside the box's collider, so the agent's path ends on
                // the navmesh edge beside it; a 1.6m stopping distance then parks it short of
                // melee range. Close all the way in and let attackRange decide.
                _agent.stoppingDistance = 0.4f;
                _agent.obstacleAvoidanceType = AvoidanceQuality;
                _agent.avoidancePriority = Mathf.Clamp(AvoidancePriority, 0, 99);
            }
            // A Ram must not be left mid-hunt by a late re-Configure, and a Hunter must not be
            // left latched on the hatch: re-derive the target under the new refusals right now.
            if (ignoresPlayer)
            {
                _provoked = false;
                _chasingPlayer = false;
                _chasingDecoy = false;
                _decoyTarget = null;
            }
            Retarget();
        }

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _damageable = GetComponent<Damageable>();
            if (_damageable != null)
            {
                _damageable.onDeath.AddListener(OnDeath);
                _damageable.Damaged += OnDamaged; // being shot is the strongest aggro signal there is
            }
        }

        void OnDestroy()
        {
            // Unsubscribe: a UnityEvent listener pointing at a destroyed component errors out if
            // the Damageable ever outlives us, and keeps this object alive to the GC otherwise.
            if (_damageable != null)
            {
                _damageable.onDeath.RemoveListener(OnDeath);
                _damageable.Damaged -= OnDamaged;
            }
        }

        void Start()
        {
            Retarget();
        }

        void Update()
        {
            // dt boundary: read once, thread into every sim-facing timer below. The Time.time
            // reads elsewhere in this class are diagnostics (logs/readouts), not sim state.
            float dt = Time.deltaTime;

            if (_damageable == null || _damageable.IsDead) return;

            // Terminal phase (player down / hatch lost) or scene teardown: stop, permanently.
            // Without this the AI kept attacking into the lose screen and through the scene load.
            if (RunIsOver())
            {
                Stop("run over");
                return;
            }

            if (_pinRemaining > 0f)
            {
                _pinRemaining -= dt;
                StopAgent();
                if (_pinRemaining > 0f) return;
                _pinRemaining = 0f;
                _retargetTimer = 0f;
            }

            _retargetTimer -= dt;
            if (_chasingDecoy && (_decoyTarget == null || !_decoyTarget.IsAlive))
                _retargetTimer = 0f;
            if (_retargetTimer <= 0f)
            {
                Retarget();
                _retargetTimer = retargetInterval;
            }

            if (!TryGetGoal(out Vector3 goal))
            {
                StopAgent();
                return;
            }

            // Measure melee range on the FLOOR PLANE and against the target's surface, not its
            // transform. Vertical offset (the hatch centre is 1.5m up; a gatekeeper's origin is at its
            // feet) was eating most of a 2.4m budget on its own.
            bool hittingHatch = !_chasingPlayer && _hatchTarget != null;
            float reach = hittingHatch ? hatchAttackRange : attackRange;
            float distance = PlanarDistanceToTarget(goal);

            // HYSTERESIS. Agents crowding one destination shove each other with avoidance, so a
            // gatekeeper pressed against the hatch drifts across a hard range line several times a
            // second. Enter at `reach`, leave only past `reach + ReachHysteresis` — otherwise the
            // swing cadence is destroyed by jitter it has no control over.
            bool inRange = _wasInRange ? distance <= reach + ReachHysteresis : distance <= reach;

            // The swing clock runs on WALL TIME, not on time-spent-in-range. Ticking it only while
            // in range meant a gatekeeper in range 36% of frames swung at 36% of its rate — measured
            // straight off a play log: 84 hatch damage took 13.5s instead of 4.9s.
            _attackTimer -= dt;

            // Arriving IS the attack: the first swing lands on contact, not an interval later.
            if (inRange && !_wasInRange) _attackTimer = 0f;
            _wasInRange = inRange;

            if (inRange)
            {
                StopAgent();
                if (_attackTimer <= 0f)
                {
                    Attack();
                    _attackTimer = attackInterval;
                }
            }
            else if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                // Re-issuing an identical destination every frame restarts the path each time,
                // which is what made them shuffle in place. Only re-path when the goal moves.
                if ((goal - _lastDestination).sqrMagnitude > 0.25f)
                {
                    _agent.SetDestination(goal);
                    _lastDestination = goal;
                }
            }
        }

        /// <summary>Push this gatekeeper along a planar delta without clearing the hatch-commit
        /// latch. Returns walk-back gatekeeper-seconds credited (distance / patrol speed).</summary>
        public float Shove(Vector3 planarDelta)
        {
            planarDelta.y = 0f;
            float metres = planarDelta.magnitude;
            if (metres < 0.05f) return 0f;

            Vector3 want = transform.position + planarDelta;
            if (!NavMesh.SamplePosition(want, out NavMeshHit hit, metres + 2f, NavMesh.AllAreas))
                return 0f;

            float moved = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(hit.position.x, 0f, hit.position.z));
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.Warp(hit.position);
                _agent.isStopped = false;
                _lastDestination = new Vector3(9999f, 9999f, 9999f);
                _wasInRange = false;
            }
            else
            {
                transform.position = hit.position;
            }

            // Walk-back time at patrol speed — the currency criterion 5 reads.
            float speed = Mathf.Max(0.1f, WaveDirector.GatekeeperPatrolSpeed);
            return moved / speed;
        }

        /// <summary>Hold this gatekeeper in place for <paramref name="seconds"/>. Refresh-only —
        /// re-applying to an already-pinned target does not stack. Does not un-commit.</summary>
        public void Pin(float seconds)
        {
            if (_pinRemaining <= 0f && seconds <= 0f) return;
            // Refresh to the new duration — never add (no stacking).
            _pinRemaining = Mathf.Max(0f, seconds);
            StopAgent();
        }

        /// <summary>Distance to whatever we are about to hit, ignoring height and using the hatch's
        /// surface rather than its centre.</summary>
        float PlanarDistanceToTarget(Vector3 goal)
        {
            Vector3 point = goal;
            if (!_chasingPlayer && _hatchTarget != null)
                point = _hatchTarget.ClosestSurfacePoint(transform.position);
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>True once the sim is in a terminal phase, or when no controller was bound
        /// at spawn (scene teardown / hand-placed actor outside the spawner path).</summary>
        bool RunIsOver()
        {
            var gc = _game;
            if (gc == null) return true; // no controller: nothing left to fight for
            return gc.RunIsOver;         // Lost/Escaped, or a null Sim
        }

        /// <summary>Halt and disable for good — cheaper and safer than re-testing every frame.</summary>
        void Stop(string reason)
        {
            StopAgent();
            enabled = false;
            Log.Info(Tag, $"Gatekeeper stopped: {reason}.");
        }

        void StopAgent()
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
        }

        void Attack()
        {
            if (RunIsOver()) return; // guard: the run can end between the range test and the swing

            if (_chasingPlayer && _player != null && !_player.IsDead)
            {
                _player.TakeDamage(playerDamage, transform);
                return;
            }

            // Re-test the hatch here, not just at retarget: it may have been destroyed since, and
            // hatch.transform on a destroyed HatchStation is a NullReferenceException.
            var hatch = _hatchTarget;
            if (hatch == null || hatch.IsDestroyed) return;

            // One line per gatekeeper, the first time it actually connects: arrival-to-damage latency
            // is the thing to watch, and it is measurable straight from the log.
            if (!_loggedHatchHit)
            {
                _loggedHatchHit = true;
                Log.Event("Gatekeepers", $"gatekeeper hitting the hatch — {Time.time - _committedAt:0.0}s after arriving");
            }
            hatch.ApplyDamage(hatchDamage);
            Effects.HatchDamageSparks(hatch.transform.position + Vector3.up * 1.2f);
        }

        void OnDeath()
        {
            // The kill LINE belongs to WaveSpawner: it owns the alive count this death changes,
            // and its listener closes over this gatekeeper's kind. So does the DESTROY —
            // Despawn is the one path that removes a spawned actor from the ledger and the
            // scene. A hand-placed actor has no spawner and cleans up after itself.
            StopAgent();
            SpawnDeathBurst();
            var spawner = _spawner;
            if (spawner != null) spawner.Despawn(this);
            else Destroy(gameObject);
        }

        /// <summary>Kill-confirm particle explosion so deaths read as an event, not a vanish. The
        /// tint comes off the renderer, so each kind's burst is its own colour for free.</summary>
        void SpawnDeathBurst()
        {
            var myRenderer = GetComponentInChildren<Renderer>();
            Color tint = myRenderer != null && myRenderer.sharedMaterial != null
                ? myRenderer.sharedMaterial.color
                : FxColors.EnemyAlert;
            Effects.DeathExplosion(transform.position, tint);
        }
    }
}
