using System.Collections;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY (see WaveSpawner.cs for the ownership contract): how a wave becomes
    /// actors — the shuffled per-kind roster, and the group emission that puts groupSize of them
    /// through ONE door at a time. Deep-start chains run several of these routines at once.</summary>
    public sealed partial class WaveSpawner
    {
        /// <summary>Fill a wave emit's roster from the spec's three counts and shuffle it.</summary>
        /// <remarks>SHUFFLED, NOT BLOCKED: appending the kinds in order would send every Hunter
        /// first and every Ram last, so the mix a player has to triage would only ever exist for
        /// the few seconds the blocks overlap. Mixing is the whole reason there are three kinds.
        ///
        /// The shuffle draws from RunSim.Rng — the sim's seeded stream — so the same seed replays
        /// the same roster; it is host-side wave construction, not player input, so it belongs on
        /// that stream (the reason PlayerGun's crits deliberately do not). UnityEngine.Random is
        /// the fallback only when there is no sim, which is the same case in which nothing spawns.
        /// </remarks>
        void BuildRoster(WaveEmit emit, WaveSpec spec)
        {
            emit.Queue.Clear();
            AddKind(emit, GatekeeperKind.Default, spec.defaultCount);
            AddKind(emit, GatekeeperKind.Hunter, spec.hunterCount);
            AddKind(emit, GatekeeperKind.Ram, spec.ramCount);

            // The sim's contract is that the three counts sum to enemyCount. If a tuning pass ever
            // breaks that, spawn what the spec actually asked for rather than an empty wave, and
            // say so — a silently short wave clears itself and looks like a win.
            if (emit.Queue.Count != spec.enemyCount)
            {
                Log.Warn(Tag, $"Wave roster {emit.Queue.Count} != spec.enemyCount {spec.enemyCount} " +
                              $"({spec.defaultCount}/{spec.hunterCount}/{spec.ramCount}); using the roster.");
                if (emit.Queue.Count == 0) AddKind(emit, GatekeeperKind.Default, spec.enemyCount);
            }

            var rng = _sim != null ? _sim.Rng : null;
            for (int i = emit.Queue.Count - 1; i > 0; i--)
            {
                int j = rng != null ? rng.Next(i + 1) : Random.Range(0, i + 1);
                GatekeeperKind swap = emit.Queue[i];
                emit.Queue[i] = emit.Queue[j];
                emit.Queue[j] = swap;
            }
        }

        void AddKind(WaveEmit emit, GatekeeperKind kind, int count)
        {
            for (int i = 0; i < count; i++) emit.Queue.Add(kind);
        }

        /// <summary>One group per spawnInterval until the roster is empty. spawnInterval is the gap
        /// between GROUPS now, so the wave's total arrival time is unchanged in shape but the
        /// pressure arrives in clumps. Respects MaxEnemyCount in aggregate across concurrent waves.</summary>
        IEnumerator SpawnWaveRoutine(WaveEmit emit)
        {
            WaveSpec spec = emit.Spec;
            int groupSize = Mathf.Max(1, spec.groupSize);
            int groupNumber = 0;
            // spawnInterval is fixed for the wave, so the yield object is built once out here
            // instead of once per group inside the loop.
            WaitForSeconds groupGap = spec.spawnInterval > 0f
                ? new WaitForSeconds(spec.spawnInterval)
                : null;
            while (emit.Queue.Count > 0)
            {
                // Re-tested every iteration: the run can end (or the scene start unloading)
                // during any of the WaitForSeconds below.
                if (_teardown || _sim == null || _sim.Phase != RunPhase.Tethered) yield break;

                // Aggregate live-gatekeeper cap across a deep-start pile-up.
                int want = Mathf.Min(groupSize, emit.Queue.Count);
                while (!_teardown && AliveCount + want > WaveDirector.MaxEnemyCount)
                {
                    yield return null;
                    if (_teardown || _sim == null || _sim.Phase != RunPhase.Tethered) yield break;
                }

                groupNumber++;
                EmitGroup(emit, groupSize, groupNumber);
                CheckAssaultCleared();
                if (groupGap != null && emit.Queue.Count > 0)
                    yield return groupGap;
            }

            // Delivery finished for THIS wave — may unlock the next chained wave.
            if (!_teardown && _sim != null && _sim.Phase == RunPhase.Tethered)
                _sim.NotifyDeliveryComplete();

            _active.Remove(emit);
            CheckAssaultCleared();
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
                // Decrement whether or not the spawn lands: a roster entry that can never be
                // spawned must still stop counting against the wave, or the wave never clears
                // and the run softlocks in Assault.
                emit.Pending--;
                if (point == null) continue;
                if (SpawnOne(emit.Spec, kind, point, i, size, emit)) tally[(int)kind]++;
            }

            if (point == null)
            {
                Log.Warn(Tag, $"No spawn points registered; dropped group {groupNumber} ({size} gatekeepers).");
                return;
            }
            // AliveCount suffix is existing live state, not a new counter.
            Log.Event("Gatekeepers", $"group {groupNumber}: {Describe(tally)} breached at {point.name} " +
                                  $"({emit.Spawned}/{emit.Size}) — {AliveCount} live");
        }

        /// <summary>"2 Gatekeepers, 1 Hunter, 2 Rams" — the group's composition, kinds omitted when
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
                case GatekeeperKind.Ram: return "Ram";
                default: return "Gatekeeper";
            }
        }

        /// <summary>Instantiate one actor of <paramref name="kind"/> at slot
        /// <paramref name="index"/> of a <paramref name="groupCount"/>-strong group.</summary>
        bool SpawnOne(WaveSpec spec, GatekeeperKind kind, Transform point, int index, int groupCount,
                      WaveEmit emit)
        {
            if (_teardown) return false; // guard: never Instantiate into a scene that is unloading
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

            Vector3 at = GroupSlot(point.position, index, groupCount);
            GameObject enemy = Instantiate(prefab, at, point.rotation);
            var damageable = enemy.GetComponent<Damageable>();
            if (damageable != null)
            {
                // Absolute, not relative: scaling whatever the prefab happened to carry made the
                // tuned baseline depend on a serialized asset that can drift (and a re-scaled
                // pooled instance would compound). The kind's own maxHp is the one source.
                float multiplier = spec.enemyHealthMultiplier > 0f ? spec.enemyHealthMultiplier : 1f;
                damageable.SetMaxHp(Mathf.Max(1f, stats.maxHp) * multiplier);
            }

            var gatekeeper = enemy.GetComponent<EnemyGatekeeper>();
            if (gatekeeper != null)
            {
                gatekeeper.Configure(stats);
                gatekeeper.SpawnedAt = at;
            }

            AliveCount++;
            emit.Spawned++;

            // The listener captures ITS gatekeeper's KIND so the kill line can name what died.
            // Damageable drops all listeners in OnDestroy, so the closure cannot outlive the
            // object it closed over.
            if (damageable != null)
            {
                GatekeeperKind died = kind;
                Damageable body = damageable;
                damageable.onDeath.AddListener(() => OnGatekeeperDied(died, body));
            }
            return true;
        }

        /// <summary>Where slot <paramref name="index"/> of a group stands. Slot 0 takes the breach
        /// point itself; the rest ring it, snapped back onto the navmesh so nobody is born inside
        /// a wall (a spawn point sits at the far end of a chamber, not always in open floor).</summary>
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
                case GatekeeperKind.Ram:
                    if (ramPrefab != null) return ramPrefab;
                    break;
                default:
                    return enemyPrefab;
            }
            if (!_warnedPrefab)
            {
                _warnedPrefab = true;
                Log.Warn(Tag, $"No {KindName(kind)} prefab wired; using the default prefab " +
                              "(behaviour is correct, colour is not). Re-run Build All Scenes.");
            }
            return enemyPrefab;
        }
    }
}
