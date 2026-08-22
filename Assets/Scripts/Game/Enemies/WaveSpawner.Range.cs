using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of WaveSpawner): putting one Gatekeeper on a practice lane,
    /// with no Wave behind it.
    ///
    /// Split because spawning for a Wave and spawning for a range are different callers with
    /// different rules — a Wave spawn is a slot in a group with a spec's health multiplier and a
    /// tally to report, a range spawn is one body at one marker — but they must produce the SAME
    /// actor, so both go through <c>CreateActor</c>. The range does not get its own kind of
    /// Gatekeeper (Docs/Milestones — a range you can fight in, "Do not build").</summary>
    public sealed partial class WaveSpawner
    {
        #region Public API
        /// <summary>One actor of <paramref name="kind"/> standing at <paramref name="point"/>,
        /// with that kind's own stats and no wave scaling. Null when it could not be made.
        ///
        /// The caller owns nothing: death routes through <see cref="Despawn"/> and the spawn
        /// ledger is this spawner's, exactly as for a Wave.</summary>
        public EnemyGatekeeper SpawnForRange(GatekeeperKind kind, Transform point)
        {
            if (point == null)
            {
                Log.Warn(Tag, "range spawn refused — no marker to stand on");
                return null;
            }
            return SpawnForRange(kind, point.position, point.rotation);
        }

        /// <summary>As above, at a position rather than a marker.</summary>
        public EnemyGatekeeper SpawnForRange(GatekeeperKind kind, Vector3 at, Quaternion rotation)
        {
            if (_teardown)
            {
                // Same guard the Wave path carries: never Instantiate into a scene that is on
                // its way out.
                Log.Warn(Tag, "range spawn refused — spawner is tearing down");
                return null;
            }
            if (_game != null && _game.Net != null && _game.Net.IsClientOnly)
            {
                Log.Warn(Tag, "range spawn refused — a guest does not own actors");
                return null;
            }

            GameObject prefab = PrefabFor(kind);
            if (prefab == null)
            {
                Log.Warn(Tag, "range spawn refused — no prefab for " + kind);
                return null;
            }

            // The kind's own numbers, unscaled. A range that quietly buffed what it spawned
            // would answer questions about a Gatekeeper nobody ever meets.
            GatekeeperStats stats = WaveDirector.StatsFor(kind);
            var actor = CreateActor(kind, stats, prefab, at, rotation, 1f, false);
            if (actor == null)
            {
                Log.Warn(Tag, "range spawn failed — " + kind + " prefab carries no EnemyGatekeeper");
                return null;
            }
            Log.Event(Tag, "range spawn " + kind + " hp=" + stats.maxHp.ToString("0.#")
                + " at " + at.ToString("0.#"));
            return actor;
        }
        #endregion
    }
}
