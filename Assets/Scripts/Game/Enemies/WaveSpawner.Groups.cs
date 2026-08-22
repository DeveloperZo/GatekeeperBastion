using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (see WaveSpawner.cs for the ownership contract): how a wave becomes
    /// actors — the shuffled per-kind roster, and the tick-driven group delivery that puts
    /// groupSize of them through ONE door at a time. Deep-start chains advance several waves
    /// per tick.</summary>
    public sealed partial class WaveSpawner
    {
        /// <summary>Fill a wave emit's roster from the spec's three counts and shuffle it.</summary>
        /// <remarks>SHUFFLED, NOT BLOCKED: appending the kinds in order would send every Hunter
        /// first and every Thief last, so the mix a player has to triage would only ever exist for
        /// the few seconds the blocks overlap. Mixing is the whole reason there are three kinds.
        ///
        /// The shuffle draws from RunSim.Rng — the sim's seeded stream, and ONLY that stream — so
        /// the same seed replays the same roster; it is host-side wave construction, not player
        /// input, so it belongs on that stream (the reason PlayerGun's crits deliberately do not).
        /// No sim means no seeded stream: the roster is left in build order (deterministic), with
        /// one warning — the same no-sim case in which nothing spawns anyway.
        /// </remarks>
        void BuildRoster(WaveEmit emit, WaveSpec spec)
        {
            emit.Queue.Clear();
            emit.FinaleElitePending = spec.finaleElite;
            AddKind(emit, GatekeeperKind.Default, spec.defaultCount);
            AddKind(emit, GatekeeperKind.Hunter, spec.hunterCount);
            AddKind(emit, GatekeeperKind.Thief, spec.thiefCount);

            // The sim's contract is that the three counts sum to enemyCount. If a tuning pass ever
            // breaks that, spawn what the spec actually asked for rather than an empty wave, and
            // say so — a silently short wave clears itself and looks like a win.
            if (emit.Queue.Count != spec.enemyCount)
            {
                Log.Warn(Tag, $"Wave roster {emit.Queue.Count} != spec.enemyCount {spec.enemyCount} " +
                              $"({spec.defaultCount}/{spec.hunterCount}/{spec.thiefCount}); using the roster.");
                if (emit.Queue.Count == 0) AddKind(emit, GatekeeperKind.Default, spec.enemyCount);
            }

            var rng = _sim != null ? _sim.Rng : null;
            if (rng == null)
            {
                if (!_warnedNoRng)
                {
                    _warnedNoRng = true;
                    Log.Warn(Tag, "No sim RNG; roster left unshuffled (deterministic build order).");
                }
                return;
            }
            for (int i = emit.Queue.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                GatekeeperKind swap = emit.Queue[i];
                emit.Queue[i] = emit.Queue[j];
                emit.Queue[j] = swap;
            }
        }

        void AddKind(WaveEmit emit, GatekeeperKind kind, int count)
        {
            for (int i = 0; i < count; i++) emit.Queue.Add(kind);
        }

        /// <summary>Advance every in-flight wave's group delivery by <paramref name="dt"/>.
        /// One group per spawnInterval until each roster is empty. spawnInterval is the gap
        /// between GROUPS, so the wave's total arrival time is unchanged in shape but the
        /// pressure arrives in clumps. Respects MaxEnemyCount in aggregate across concurrent
        /// waves. Allocates nothing: all state lives on the WaveEmit entries in _active.</summary>
        public void TickDelivery(float dt)
        {
            if (_teardown) return; // latched: delivery never advances again
            for (int i = 0; i < _active.Count; i++)
            {
                var emit = _active[i];
                if (emit.Dead) continue; // bailed; waits in _active for the teardown sweep
                if (emit.BornFrame == Time.frameCount) continue; // first advance ran at wave start
                if (!AdvanceWave(emit, dt)) continue;
                // Delivery finished for THIS wave: drop it, then re-test the assault.
                _active.RemoveAt(i);
                i--;
                CheckAssaultCleared();
            }
        }

        /// <summary>One tick of one wave's delivery. Returns true when the roster has emptied
        /// and the delivery-complete notification has fired — the caller removes the wave from
        /// _active and probes the assault-clear.
        /// Ordering contract: the inter-group countdown runs down first and unconditionally;
        /// only once it has expired do the teardown/phase bail-out and the aggregate alive-cap
        /// hold apply, re-tested every tick until the group emits. The countdown does not
        /// advance while cap-held. A positive spawnInterval emits at most ONE group per tick
        /// however large dt is, and the next interval starts fresh at emission (overshoot
        /// discarded); spawnInterval &lt;= 0 delivers group after group within one tick.</summary>
        bool AdvanceWave(WaveEmit emit, float dt)
        {
            WaveSpec spec = emit.Spec;
            int groupSize = Mathf.Max(1, spec.groupSize);

            if (!emit.CapWaiting && emit.Countdown > 0f)
            {
                emit.Countdown -= dt;
                if (emit.Countdown > 0f) return false;
            }

            while (emit.Queue.Count > 0)
            {
                // Re-tested every tick: the run can end (or the scene start unloading) while
                // a group is counting down or cap-held. A bailed wave is marked Dead and stays
                // in _active so the assault cannot read as cleared before teardown sweeps it.
                if (_teardown || _sim == null || _sim.Phase != RunPhase.Tethered)
                {
                    emit.Dead = true;
                    return false;
                }

                // Aggregate live-gatekeeper cap across a deep-start pile-up.
                int want = Mathf.Min(groupSize, emit.Queue.Count);
                if (AliveCount + want > WaveDirector.MaxEnemyCount)
                {
                    emit.CapWaiting = true; // hold here; the countdown stays expired
                    return false;
                }
                emit.CapWaiting = false;

                emit.GroupNumber++;
                EmitGroup(emit, groupSize, emit.GroupNumber);
                CheckAssaultCleared();

                if (spec.spawnInterval > 0f && emit.Queue.Count > 0)
                {
                    emit.Countdown = spec.spawnInterval;
                    return false; // one group per tick when an interval paces the wave
                }
                // spawnInterval <= 0: no gap between groups — keep emitting this tick.
            }

            // Delivery finished for THIS wave — may unlock the next chained wave.
            if (!_teardown && _sim != null && _sim.Phase == RunPhase.Tethered)
                _sim.NotifyDeliveryComplete();
            return true;
        }

        /// <summary>Emit up to <paramref name="groupSize"/> actors from the roster AT ONE BREACH
        /// POINT, arranged on a small ring around it so they do not spawn inside each other. The
        /// point is picked once per group — that rotation is what spreads groups around the map
        /// while keeping each group together.</summary>
        void EmitGroup(WaveEmit emit, int groupSize, int groupNumber)
        {
            int size = Mathf.Min(groupSize, emit.Queue.Count);
            Transform point = PickSpawnPoint();
            var tally = new int[3];

            for (int i = 0; i < size; i++)
            {
                GatekeeperKind kind = emit.Queue[emit.Queue.Count - 1];
                emit.Queue.RemoveAt(emit.Queue.Count - 1);
                // Queue.RemoveAt plus AliveCount/_active clearing prevent a failed spawn from
                // softlocking the assault.
                if (point == null) continue;
                if (SpawnOne(emit.Spec, kind, point, i, size, emit)) tally[(int)kind]++;
            }

            if (point == null)
            {
                Log.Warn(Tag, $"No spawn points registered; dropped group {groupNumber} ({size} gatekeepers).");
                return;
            }
            // AliveCount suffix is existing live state, not a new counter.
            // `net` / `local` only while a session is listening — solo lines stay byte-identical
            // so wave-report's `live\b` still matches and old logs stay comparable.
            string netTag = "";
            if (_game != null && _game.Net != null && _game.Net.Active)
                netTag = _game.Net.IsClientOnly ? " local" : " net";
            Log.Event("Gatekeepers", $"group {groupNumber}: {Describe(tally)} breached at {point.name} " +
                                  $"({emit.Spawned}/{emit.Size}) — {AliveCount} live" + netTag);
        }

        /// <summary>"2 Gatekeepers, 1 Hunter, 2 Thieves" — the group's composition, kinds omitted when
        /// absent. This is the one place per-kind detail is worth log space: it is what makes a
        /// group's threat readable afterwards.</summary>
        static string Describe(int[] tally)
        {
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < tally.Length; k++)
            {
                if (tally[k] <= 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(tally[k]).Append(' ').Append(KindName((GatekeeperKind)k));
                if (tally[k] > 1) sb.Append('s');
            }
            return sb.Length > 0 ? sb.ToString() : "0 gatekeepers";
        }

        static string KindName(GatekeeperKind kind)
        {
            switch (kind)
            {
                case GatekeeperKind.Hunter: return "Hunter";
                case GatekeeperKind.Thief: return "Thief";
                default: return "Gatekeeper";
            }
        }

        /// <summary>Instantiate one actor of <paramref name="kind"/> at slot
        /// <paramref name="index"/> of a <paramref name="groupCount"/>-strong group.</summary>
        bool SpawnOne(WaveSpec spec, GatekeeperKind kind, Transform point, int index, int groupCount,
                      WaveEmit emit)
        {
            if (_teardown) return false; // guard: never Instantiate into a scene that is unloading
            if (_game != null && _game.Net != null && _game.Net.IsClientOnly)
                return false;

            GameObject prefab = PrefabFor(kind);
            if (prefab == null)
            {
                Log.Warn(Tag, "No enemy prefab assigned; cannot spawn.");
                return false;
            }

            // ONE call to the sim per actor: every per-kind number this gatekeeper will ever use
            // comes from here, because a prefab's serialized fields are frozen at asset-creation
            // time and silently outrank any later code change (Docs/Architecture.md GOTCHA 1).
            GatekeeperStats stats = WaveDirector.StatsFor(kind);
            bool isElite = kind == GatekeeperKind.Thief && emit.FinaleElitePending;
            if (isElite)
            {
                emit.FinaleElitePending = false;
                stats.maxHp = MasterControl.FinaleEliteHp;
                Log.Event(Tag, "finale elite hp=" + stats.maxHp
                               + " bodyScale=" + MasterControl.FinaleEliteScale);
            }

            Vector3 at = GroupSlot(point.position, index, groupCount);
            float multiplier = spec.enemyHealthMultiplier > 0f ? spec.enemyHealthMultiplier : 1f;
            if (CreateActor(kind, stats, prefab, at, point.rotation, multiplier, isElite) == null)
                return false;

            emit.Spawned++;
            return true;
        }

        /// <summary>Instantiate one configured actor and take ownership of it. Extracted from
        /// <see cref="SpawnOne"/> so the practice range can put the SAME actor on its lane —
        /// same prefab, same stats push, same spawn ledger, same death routing. A range that
        /// spawned its own flavour of Gatekeeper would not be measuring the game
        /// (Docs/Milestones — a range you can fight in, "Do not build").
        ///
        /// <paramref name="at"/> is navmesh height; the vertical lift is applied here.</summary>
        EnemyGatekeeper CreateActor(GatekeeperKind kind, GatekeeperStats stats, GameObject prefab,
            Vector3 at, Quaternion rotation, float healthMultiplier, bool isElite)
        {
            // The agent's baseOffset lifts the transform on its FIRST update, so instantiating at
            // navmesh height buries the body and pops it up a metre on frame one (1.0 Default,
            // 0.85 Hunter, 1.4 Thief — same class as the Ship's bed pose, 2026-08-18 defect).
            // Spawn at the height the agent is going to hold anyway.
            Vector3 spawnAt = at + Vector3.up * PrefabBaseOffset(prefab);
            GameObject enemy = Instantiate(prefab, spawnAt, rotation);
            if (isElite) GrowEliteBody(enemy);
            var damageable = enemy.GetComponent<Damageable>();
            if (damageable != null)
            {
                // Absolute, not relative: scaling whatever the prefab happened to carry made the
                // tuned baseline depend on a serialized asset that can drift (and a re-scaled
                // pooled instance would compound). The kind's own maxHp is the one source.
                float multiplier = healthMultiplier > 0f ? healthMultiplier : 1f;
                damageable.SetMaxHp(Mathf.Max(1f, stats.maxHp) * multiplier);
            }

            var gatekeeper = enemy.GetComponent<EnemyGatekeeper>();
            if (gatekeeper != null)
            {
                // The controller rides along with the stats push — spawned actors read run
                // state (RunIsOver, Decoys) from it, never from a global — and so does this
                // spawner, the one owner of the actor's destroy (Despawn).
                gatekeeper.Configure(stats, _game, this);
                if (_game != null && _game.Run != null)
                    gatekeeper.SpawnId = _game.Run.TakeSpawnId();
                gatekeeper.SpawnedAt = spawnAt;
                _live.Add(gatekeeper); // spawn ledger: removed by Despawn; swept on teardown
            }

            if (_game != null && _game.Net != null && _game.Net.Active && !_game.Net.IsClientOnly)
            {
                var netObj = enemy.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn(true);
                else
                    Log.Warn("Net", "spawn skipped — no NetworkObject kind=" + kind);
            }

            AliveCount++;

            // The listener captures ITS gatekeeper's KIND and ACTOR so the kill line can name
            // what died (and its spawn id). Ledger removal is Despawn's job — the actor's own
            // OnDeath routes there before this listener runs (it subscribed first, in Awake).
            // Damageable drops all listeners in OnDestroy, so the closure cannot outlive the
            // object it closed over.
            if (damageable != null)
            {
                GatekeeperKind died = kind;
                Damageable body = damageable;
                EnemyGatekeeper actor = gatekeeper;
                damageable.onDeath.AddListener(() => OnGatekeeperDied(died, body, actor));
            }
            return gatekeeper;
        }

        /// <summary>Where slot <paramref name="index"/> of a group stands. Slot 0 takes the breach
        /// point itself; the rest ring it, snapped back onto the navmesh so nobody is born inside
        /// a wall (a spawn point sits at the far end of a chamber, not always in open floor).</summary>
        /// <summary>The vertical lift the prefab's agent will apply on its first update. Read from
        /// the PREFAB, not the instance: it has to be known before Instantiate.</summary>
        static float PrefabBaseOffset(GameObject prefab)
        {
            if (prefab == null) return 0f;
            var agent = prefab.GetComponent<NavMeshAgent>();
            return agent != null ? agent.baseOffset : 0f;
        }

        /// <summary>Grow the finale elite's BODY and nothing else — the visual child under the
        /// enemy root, never the root, so the NavMeshAgent, the capsule and the spawn height all
        /// stay exactly the Thief's (<see cref="MasterControl.FinaleEliteScale"/> says why).
        /// The visual's pivot sits at the model's feet, so this grows upward off the floor
        /// instead of sinking the body into it.
        /// <para>Found by renderer rather than by name: the builder names that child per kind,
        /// and a rename there should not quietly stop the boss looking like one.</para></summary>
        static void GrowEliteBody(GameObject enemy)
        {
            if (enemy == null) return;
            var skin = enemy.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null)
            {
                Log.Warn(Tag, "finale elite has no skinned body — size tell skipped.");
                return;
            }
            Transform visual = skin.transform;
            while (visual.parent != null && visual.parent != enemy.transform)
                visual = visual.parent;
            if (visual == enemy.transform) return; // body is on the root; growing it would move the agent
            visual.localScale *= MasterControl.FinaleEliteScale;
        }

        static Vector3 GroupSlot(Vector3 origin, int index, int groupCount)
        {
            if (index <= 0 || groupCount <= 1) return origin;
            float angle = Mathf.PI * 2f * index / groupCount;
            Vector3 want = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * GroupSpread;
            return NavMesh.SamplePosition(want, out NavMeshHit hit, GroupSpread * 2f, NavMesh.AllAreas)
                ? hit.position
                : origin;
        }

        /// <summary>The prefab for a kind, falling back to the default one when a per-kind prefab
        /// is missing. A fallback actor still BEHAVES correctly — EnemyGatekeeper derives its whole
        /// targeting rule from the two flags in GatekeeperStats — so the only thing lost is the
        /// colour that tells the player which one it is. Worth one warning, not a spawn failure.
        /// </summary>
        GameObject PrefabFor(GatekeeperKind kind)
        {
            switch (kind)
            {
                case GatekeeperKind.Hunter:
                    if (hunterPrefab != null) return hunterPrefab;
                    break;
                case GatekeeperKind.Thief:
                    if (thiefPrefab != null) return thiefPrefab;
                    break;
                default:
                    return enemyPrefab;
            }
            if (!_warnedPrefab)
            {
                _warnedPrefab = true;
                Log.Warn(Tag, $"No {KindName(kind)} prefab wired; using the default prefab " +
                              "(behaviour is correct, colour is not). Re-run GatekeepersProtocol/Build/All Scenes.");
            }
            return enemyPrefab;
        }
    }
}
