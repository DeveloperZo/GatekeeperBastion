using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY (see EnemyGatekeeper.cs for the ownership contract): the targeting rule —
    /// who a gatekeeper is willing to go after, what provokes it, and where it stands to swing.
    /// THIS FILE IS THE SEAM BETWEEN THE THREE KINDS. Everything else about a gatekeeper is shared;
    /// a kind is exactly the set of these three questions it answers differently.</summary>
    public partial class EnemyGatekeeper
    {
        // ---------- the rule: three virtuals, one refusal each ----------

        /// <summary>May this gatekeeper march on the hatch? False for Hunters, which are a threat to
        /// the player and to nothing else.</summary>
        protected virtual bool MayTargetHatch => !ignoresHatch;

        /// <summary>May this gatekeeper go after a player at all — by aggro, by sight, or by being
        /// shot? False for Rams, which walk past you to get to the door.</summary>
        protected virtual bool MayTargetPlayer => !ignoresPlayer;

        /// <summary>Can being shot (or a neighbour's shout) flip this gatekeeper onto the player?
        /// A gatekeeper that will never target a player can never be provoked into doing so —
        /// shooting a Ram changes nothing at all about it, which is the whole design.</summary>
        protected virtual bool CanBeProvoked => MayTargetPlayer;

        // ---------- provocation ----------

        /// <summary>Shot by someone: hunt them, and pull the neighbours in with us.</summary>
        void OnDamaged(Transform source)
        {
            // A Ram absorbs this and does not change: not its target, not its speed, and it does
            // not shout. Provoke() is the single gate, so there is no second path to miss.
            Provoke(source, shout: true);
        }

        /// <summary>Commit to hunting the player for the rest of this gatekeeper's life. Idempotent,
        /// and refused outright by any kind that cannot target players. Returns true only when
        /// this call is what flipped the gatekeeper, so a shout can count real joiners.
        /// <paramref name="shout"/> alerts nearby gatekeepers (they do not shout again, so one shot
        /// pulls one group, not the whole bastion by relay).</summary>
        public bool Provoke(Transform source, bool shout)
        {
            if (!CanBeProvoked) return false;
            if (_provoked || _damageable == null || _damageable.IsDead) return false;
            _provoked = true;
            _chasingPlayer = true;
            if (_agent != null && _agent.isOnNavMesh) _agent.speed = chaseSpeed;
            _retargetTimer = 0f; // re-path this frame, not up to 0.75s later
            Retarget();

            if (!shout) return true;
            int joined = 0;
            for (int i = 0; i < Gatekeepers.Count; i++)
            {
                var other = Gatekeepers[i];
                if (other == null || other == this) continue;
                if (Vector3.Distance(transform.position, other.transform.position) > alertRadius) continue;
                // Rams in the group silently decline; only gatekeepers that actually joined count.
                if (other.Provoke(source, shout: false)) joined++;
            }
            Log.Event("Gatekeepers", joined > 0
                ? $"gatekeeper provoked — {joined} nearby joined the hunt"
                : "gatekeeper provoked — hunting the player");
            return true;
        }

        /// <summary>Clear line of sight to the player within noticeRange? Cheap: only runs on the
        /// retarget tick, and only until the gatekeeper is provoked.</summary>
        bool CanSeePlayer()
        {
            if (_player == null) return false;
            Vector3 eye = transform.position + Vector3.up * 1.2f;
            Vector3 target = _player.transform.position + Vector3.up * 1.0f;
            Vector3 to = target - eye;
            if (to.magnitude > noticeRange) return false;
            // Anything solid between us blocks the sighting; the player's own collider does not.
            if (Physics.Raycast(eye, to.normalized, out RaycastHit hit, to.magnitude,
                                ~0, QueryTriggerInteraction.Ignore))
                return hit.collider != null && hit.collider.transform.IsChildOf(_player.transform);
            return true;
        }

        // ---------- the retarget tick ----------

        void Retarget()
        {
            // Co-op ready: re-pick the closest LIVE player every retarget tick. NearestAlive means
            // a downed player stops drawing aggro the instant the run ends, and the registry
            // prunes destroyed entries first (dereferencing one used to be an NRE right here).
            _player = PlayerRegistry.NearestAlive(transform.position);

            var hatch = HatchStation.Instance;
            // MayTargetHatch folds straight into "is there a hatch for me": a Hunter simply lives
            // in a world with no hatch in it, so every hatch branch below is closed to it without
            // a single kind test in the flow.
            bool hatchAvailable = MayTargetHatch && hatch != null && !hatch.IsDestroyed;
            bool playerNear = MayTargetPlayer && _player != null &&
                              Vector3.Distance(transform.position, _player.transform.position) <= aggroRadius;

            // Seeing the player down a sightline counts as encountering them.
            if (!_provoked && MayTargetPlayer && _player != null && !playerNear && CanSeePlayer())
                Provoke(_player.transform, shout: false);

            // Player is the target when they get in the way — or when the hatch is gone.
            // Proximity to the hatch beats every other consideration: a gatekeeper in the bay is
            // there to break your way home, not to chase whoever annoyed it on the way in.
            // A Hunter never reaches this branch (hatchAvailable is false for it even while it is
            // standing on the hatch), so the commit latch can never close on one.
            if (hatchAvailable && !_committedToHatch &&
                Vector3.Distance(transform.position, hatch.transform.position) <= hatchCommitRadius)
            {
                // LATCHED, not re-evaluated. Recomputing this every retarget made a gatekeeper sitting
                // near the 8m line flip between "hunt the player" and "wreck the hatch" twice a
                // second — each flip threw away its path and restarted it, so it hovered instead of
                // closing. Once a gatekeeper is home, it is a hatch problem until the hatch is gone.
                _committedToHatch = true;
                _committedAt = Time.time;
                Log.Event("Gatekeepers", "gatekeeper reached the hatch — attacking home");
            }
            if (!hatchAvailable) _committedToHatch = false;

            // The one line that decides everything downstream. Read per kind:
            //   Default — as it always was.
            //   Hunter  — hatchAvailable is always false, so `!hatchAvailable` holds and it chases
            //             the player from the breach, for life.
            //   Ram     — MayTargetPlayer is false, so this is always false and the hatch is the
            //             only goal it can ever have.
            _chasingPlayer = MayTargetPlayer && !_committedToHatch &&
                             (_provoked || playerNear || !hatchAvailable);
            _hatchTarget = hatchAvailable ? hatch : null;
            _standPoint = _committedToHatch && hatchAvailable ? HatchStandPoint(hatch) : (Vector3?)null;

            // Decoy pull: ahead of Hatch and player when in radius. Rams never; committed Defaults never.
            _decoyTarget = null;
            _chasingDecoy = false;
            var gc = GameController.Instance;
            var bay = gc != null ? gc.Decoys : null;
            if (bay != null && bay.State.IsLive && bay.Live != null && bay.Live.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, bay.Live.transform.position);
                if (GatekeeperBastion.Sim.DecoyState.ShouldPull(Kind, _committedToHatch, true, dist, bay.State.PullRadius))
                {
                    _decoyTarget = bay.Live;
                    _chasingDecoy = true;
                    _chasingPlayer = false;
                    _standPoint = null;
                }
            }
        }

        /// <summary>A navmesh point just off the hatch's face. Pathing at the hatch's transform
        /// aims INSIDE its collider, where no navmesh exists: the agent gets a partial path, stalls
        /// at the edge and never registers as "arrived". This gives it somewhere it can stand.</summary>
        Vector3 HatchStandPoint(HatchStation hatch)
        {
            Vector3 surface = hatch.ClosestSurfacePoint(transform.position);
            Vector3 out2 = transform.position - surface;
            out2.y = 0f;
            if (out2.sqrMagnitude < 0.01f) out2 = Vector3.forward;
            Vector3 want = surface + out2.normalized * 0.9f;
            return NavMesh.SamplePosition(want, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)
                ? hit.position
                : want;
        }

        bool TryGetGoal(out Vector3 goal)
        {
            // Unity-null-safe on every access: both targets can be destroyed between retargets.
            if (_chasingDecoy && _decoyTarget != null && _decoyTarget.IsAlive)
            {
                goal = _decoyTarget.transform.position;
                return true;
            }
            if (_chasingPlayer && _player != null && !_player.IsDead)
            {
                goal = _player.transform.position;
                return true;
            }
            if (_hatchTarget != null && !_hatchTarget.IsDestroyed)
            {
                goal = _standPoint != null ? _standPoint.Value : _hatchTarget.transform.position;
                return true;
            }
            goal = default;
            return false;
        }
    }
}
