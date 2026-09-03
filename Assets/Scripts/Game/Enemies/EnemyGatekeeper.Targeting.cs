using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
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
        /// shot? False for Thieves, which walk past you to get to the door.</summary>
        protected virtual bool MayTargetPlayer => !ignoresPlayer;

        /// <summary>Can being shot (or a neighbour's shout) flip this gatekeeper onto the player?
        /// A gatekeeper that will never target a player can never be provoked into doing so —
        /// shooting a Thief changes nothing at all about it, which is the whole design.</summary>
        protected virtual bool CanBeProvoked => MayTargetPlayer;

        // ---------- provocation ----------

        /// <summary>Shot by someone: hunt them, and pull the neighbours in with us.</summary>
        void OnDamaged(Transform source)
        {
            // Presentation: flinch even when Provoke refuses (a Thief still staggers).
            if (_animator != null) _animator.SetTrigger(AnimHitReact);
            RefuseIfShielded();
            // A Thief absorbs this and does not change: not its target, not its speed, and it does
            // not shout. Provoke() is the single gate, so there is no second path to miss.
            Provoke(source, shout: true);
        }

        /// <summary>The finale elite refuses damage while its escort stands. The rule is the
        /// sim's (MasterControl.EliteShielded); the spawner's ledger is the count. Damageable
        /// has already taken the hit off Hp by the time this listener runs and it fires
        /// onDeath only after the listeners return, so refilling to max here IS the refusal —
        /// and the flash and the flinch above have already played, so the player reads "no
        /// damage" rather than "no hit". A state like the Cloaker's gaze, not a pushed flag:
        /// it flips the moment the last escort falls.</summary>
        void RefuseIfShielded()
        {
            if (!FinaleElite || _spawner == null || _damageable == null) return;
            int escorts = _spawner.FinaleEscortAlive;
            if (!MasterControl.EliteShielded(escorts)) return;
            _damageable.SetMaxHp(_damageable.MaxHp);
            if (_loggedShield) return;
            _loggedShield = true;
            Log.Event("Gatekeepers", "finale elite shielded — " + escorts + " escorts standing");
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
            RecomputeSpeed();
            _retargetTimer = 0f; // re-path this frame, not up to 0.75s later
            Retarget();

            if (!shout) return true;
            int joined = 0;
            // The spawner's ledger is the one list of live gatekeepers. A hand-placed actor has
            // no spawner and shouts to nobody — it never acts anyway (see _game remarks).
            var live = _spawner != null
                ? _spawner.Live
                : (System.Collections.Generic.IReadOnlyList<EnemyGatekeeper>)System.Array.Empty<EnemyGatekeeper>();
            for (int i = 0; i < live.Count; i++)
            {
                var other = live[i];
                if (other == null || other == this) continue;
                if (Vector3.Distance(transform.position, other.transform.position) > alertRadius) continue;
                // Thieves in the group silently decline; only gatekeepers that actually joined count.
                if (other.Provoke(source, shout: false)) joined++;
            }
            Log.Event("Gatekeepers", joined > 0
                ? $"gatekeeper provoked — {joined} nearby joined the hunt"
                : "gatekeeper provoked — hunting the player");
            return true;
        }

        /// <summary>Clear line of sight to the player within noticeRange? Cheap: only runs on the
        /// retarget tick, and only until the gatekeeper is provoked.</summary>
        /// <summary>PHANTOM: hand this gatekeeper's interest to a decoy it did not pull.
        /// True only when this call is what moved it, so the caller can count real transfers.
        /// Refused by anything that would not chase a decoy anyway — a Thief walks on, and a
        /// gatekeeper already committed to the Hatch keeps its commitment. The doctrine changes
        /// what a decoy INHERITS; it never overrules what a kind refuses.</summary>
        public bool TransferInterestTo(DecoyObject decoy)
        {
            if (decoy == null || !decoy.IsAlive) return false;
            if (Kind == GatekeeperKind.Thief) return false;
            if (_committedToHatch || !_chasingPlayer) return false;

            _decoyTarget = decoy;
            _chasingDecoy = true;
            _chasingPlayer = false;
            _chasingTurret = false;
            _chasingOutrider = false;
            _turretTarget = null;
            _outriderTarget = null;
            _standPoint = null;
            _retargetTimer = 0f;
            Retarget();
            return true;
        }

        bool CanSeePlayer()
        {
            if (_player == null) return false;
            Vector3 eye = transform.position + Vector3.up * 1.2f;
            Vector3 target = _player.transform.position + Vector3.up * 1.0f;
            Vector3 to = target - eye;
            if (to.magnitude > NoticeRangeFor(_player)) return false;
            // Anything solid between us blocks the sighting; the player's own collider does not.
            if (Physics.Raycast(eye, to.normalized, out RaycastHit hit, to.magnitude,
                                ~0, QueryTriggerInteraction.Ignore))
                return hit.collider != null && hit.collider.transform.IsChildOf(_player.transform);
            return true;
        }

        // ---------- the retarget tick ----------

        /// <summary>Protected, not private: a kind whose targeting rule flips between retargets
        /// (the Cloaker's gaze state) re-derives its target the frame the rule changes instead
        /// of waiting out the 0.75 s tick. Kinds do not otherwise call this.</summary>
        protected void Retarget()
        {
            // Co-op ready: re-pick the closest LIVE player every retarget tick. NearestAlive means
            // a downed player stops drawing aggro the instant the run ends, and the registry
            // prunes destroyed entries first (dereferencing one used to be an NRE right here).
            _player = PlayerRegistry.NearestAlive(transform.position);

            var hatch = HatchStation.Instance;
            // MayTargetHatch folds straight into "is there a hatch for me": a Hunter simply lives
            // in a world with no hatch in it, so every hatch branch below is closed to it without
            // a single kind test in the flow.
            //
            // THE HATCH IS IN THE FIGHT ON BOTH MODES (Zo, 2026-08-27). Field-terminal Waves used
            // to remove it from targeting entirely, and that is a far bigger rule than it looks:
            // `!hatchAvailable` is a term in the _chasingPlayer line below, so removing the Hatch
            // did not merely leave bodies without a second option — it forced EVERY body onto the
            // player unconditionally, for life, from anywhere on the spine. A Wave 5 field Wave is
            // 66 bodies, and a logged run died to that six times. What differs between the modes
            // is now only which end of the spine the player is standing at.
            //
            // Faucet field Waves (the dev-gated faucet graybox) still suppress sight-provoke
            // below: the Wave hunts the Hatch, and only damage or proximity puts a body on a
            // Boarder.
            bool faucetField = _game != null && _game.Sim != null && _game.Sim.FaucetHatchHunt;
            bool hatchAvailable = MayTargetHatch && hatch != null && !hatch.IsDestroyed;
            bool playerNear = MayTargetPlayer && _player != null &&
                              Vector3.Distance(transform.position, _player.transform.position) <= AggroRadiusFor(_player);

            // Seeing the player down a sightline counts as encountering them. Not on a faucet
            // Wave: sight-provoke would stickily pull the stream off the Hatch, which is the
            // hunt-you grammar that mode retires. Proximity (playerNear) stays — it is the
            // transient peel — and being shot still provokes through OnDamaged.
            if (!_provoked && MayTargetPlayer && !faucetField && _player != null
                && !playerNear && CanSeePlayer())
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
            //   Thief     — MayTargetPlayer is true on field-terminal Waves (2026-08-13), so it
            //             hunts the Boarder while the Hatch is out of the fight. Hatch-bank
            //             restores MayTargetPlayer false.
            _chasingPlayer = ChasesPlayer(MayTargetPlayer, _committedToHatch, _provoked,
                                          playerNear, hatchAvailable);
            _hatchTarget = hatchAvailable ? hatch : null;
            _standPoint = _committedToHatch && hatchAvailable ? HatchStandPoint(hatch) : (Vector3?)null;

            // Decoy pull: ahead of Hatch and player when in radius. Thieves never; committed
            // Defaults never. MayTargetPlayer gates it because this branch bypasses the three
            // targeting virtuals: a watched Cloaker must not walk to a decoy while it refuses
            // everything else. Behaviour-preserving for the other kinds — ShouldPull already
            // refuses Thieves by kind, and Default/Hunter answer MayTargetPlayer true always.
            _decoyTarget = null;
            _chasingDecoy = false;
            var gc = _game;
            var bay = gc != null ? gc.Decoys : null;
            if (MayTargetPlayer && bay != null && bay.State.IsLive && bay.Live != null && bay.Live.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, bay.Live.transform.position);
                if (GatekeepersProtocol.Sim.DecoyState.ShouldPull(Kind, _committedToHatch, true, dist, bay.State.PullRadius))
                {
                    _decoyTarget = bay.Live;
                    _chasingDecoy = true;
                    _chasingPlayer = false;
                    _standPoint = null;
                }
            }

            // A standing machine is a target by the ordinary rule: already in melee of a body
            // that was chasing the Boarder, so it is in the way and it gets hit. Reach is
            // hatch melee. Nothing about who placed the machine is read. When both a turret
            // and an Outrider sit in reach, the nearer one takes the swing.
            _turretTarget = null;
            _outriderTarget = null;
            _chasingTurret = false;
            _chasingOutrider = false;
            if (_chasingPlayer && _player != null)
            {
                var turret = TurretObject.NearestAlive(transform.position, hatchAttackRange);
                var rider = OutriderObject.NearestAlive(transform.position, hatchAttackRange);
                float tDist = turret != null
                    ? Vector3.Distance(transform.position, turret.transform.position)
                    : float.MaxValue;
                float oDist = rider != null
                    ? Vector3.Distance(transform.position, rider.transform.position)
                    : float.MaxValue;
                if (turret != null && tDist <= oDist)
                {
                    _turretTarget = turret;
                    _chasingTurret = true;
                    _chasingPlayer = false;
                    _standPoint = null;
                }
                else if (rider != null)
                {
                    _outriderTarget = rider;
                    _chasingOutrider = true;
                    _chasingPlayer = false;
                    _standPoint = null;
                }
            }
        }

        /// <summary>WILL THIS GATEKEEPER HUNT THE BOARDER? Static and pure so the decision can
        /// be executed by a test — the class around it is a NavMesh MonoBehaviour, and this one
        /// line is the whole of what went wrong on 2026-08-28.
        ///
        /// <para><paramref name="hatchAvailable"/> is the trap. It is not merely "one fewer
        /// option": it is a TERM IN THIS EXPRESSION, so removing the Hatch from targeting
        /// forces every body onto the player unconditionally and for life, from anywhere on the
        /// spine. Field waves used to do exactly that, which is 66 bodies hunting one player at
        /// Wave 5 with nothing else in the world to do — six deaths in one logged run.</para>
        ///
        /// <para>Read the clauses as: a kind that may hunt (Hunters and Defaults, never a
        /// Thief), that has not committed to the Hatch, and that has EITHER been given a reason
        /// (shot, or the player walked into its aggro radius) OR has nowhere else to go.</para>
        /// </summary>
        public static bool ChasesPlayer(bool mayTargetPlayer, bool committedToHatch,
                                        bool provoked, bool playerNear, bool hatchAvailable)
        {
            return mayTargetPlayer && !committedToHatch
                   && (provoked || playerNear || !hatchAvailable);
        }

        // Resolved ONCE per gatekeeper, from wherever it first wanted the hatch.
        Vector3 _hatchStand;
        bool _hasHatchStand;

        /// <summary>A navmesh point just off the hatch's face. Pathing at the hatch's transform
        /// aims INSIDE its collider, where no navmesh exists: the agent gets a partial path, stalls
        /// at the edge and never registers as "arrived". This gives it somewhere it can stand.
        /// <para>RESOLVED ONCE, deliberately. It used to be recomputed from
        /// <c>transform.position</c> on every retarget (0.75s), which meant crowd avoidance moved
        /// the body, the body moved its own destination, and the re-path guard fired again — a
        /// scrum of twenty at one hatch had no fixed point to settle on and visibly oscillated
        /// (2026-08-18 defect). A frozen point ends the feedback; the hatch does not move, so
        /// there is nothing for it to go stale against.</para></summary>
        Vector3 HatchStandPoint(HatchStation hatch)
        {
            if (_hasHatchStand) return _hatchStand;

            Vector3 surface = hatch.ClosestSurfacePoint(transform.position);
            Vector3 out2 = transform.position - surface;
            out2.y = 0f;
            if (out2.sqrMagnitude < 0.01f) out2 = Vector3.forward;
            Vector3 want = surface + out2.normalized * 0.9f;
            _hatchStand = NavMesh.SamplePosition(want, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)
                ? hit.position
                : want;
            _hasHatchStand = true;
            return _hatchStand;
        }

        bool TryGetGoal(out Vector3 goal)
        {
            // Unity-null-safe on every access: both targets can be destroyed between retargets.
            if (_chasingTurret && _turretTarget != null && _turretTarget.IsAlive)
            {
                goal = _turretTarget.transform.position;
                return true;
            }
            if (_chasingOutrider && _outriderTarget != null && _outriderTarget.IsAlive)
            {
                goal = _outriderTarget.transform.position;
                return true;
            }
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

        float NoticeRangeFor(PlayerHealth player)
        {
            float mul = player != null ? player.AcquisitionMul : 1f;
            return noticeRange * mul;
        }

        float AggroRadiusFor(PlayerHealth player)
        {
            float mul = player != null ? player.AcquisitionMul : 1f;
            return aggroRadius * mul;
        }
    }
}
