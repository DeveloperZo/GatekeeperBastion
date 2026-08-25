using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The death presentation — everything between the killing blow and the deferred
    /// destroy. Split from the AI the way Targeting is: this file answers "what does a corpse
    /// look like", never "who do I hunt".
    /// <para>THE LIVE PATH IS THE AUTHORED CLIP: BeginDeathPresentation fires the Die trigger
    /// into the controller's Death state (no exit transition, loop off, so the last frame
    /// holds). Every root channel of the falling-back take is baked into pose in the fbx
    /// import — orientation, Y and XZ, all Based Upon Original — so applyRootMotion stays
    /// false and the whole fall, tip-over rotation included, plays in body space retargeted
    /// onto each kind's avatar. Earlier imports left channels as root motion, and with root
    /// motion off they were discarded: the hips dropped while the body stayed upright, the
    /// sink seen in play. applyRootMotion=true on the child Animator (Hips sit metres off its
    /// origin) buried the mesh and kicked it a storey up — never re-enable it.</para>
    /// <para>A body with no controller (fixtures) falls back to a procedural board-tip around
    /// the feet. Both paths run under the LateUpdate floor clamp, which can only RAISE the
    /// corpse so it rests on the walkable plane instead of inside the floor slab.</para></summary>
    public partial class EnemyGatekeeper
    {
        /// <summary>How long the corpse stays. Ledger removal is immediate; the clip is
        /// 3.63 s, so the deferral outlives the fall with a beat to spare.</summary>
        const float CorpseSeconds = 3.7f;
        /// <summary>Fallback board-tip duration. Longer and the vanish reads as the death.</summary>
        const float CollapseSeconds = 0.9f;

        static readonly int AnimDie = Animator.StringToHash("Die");

        bool _collapsing;
        bool _presentationDead;
        bool _collapseProcedural;
        float _collapseT;
        Vector3 _collapsePos;
        Vector3 _collapsePivot;
        Vector3 _collapseAxis;
        Quaternion _collapseFrom;
        float _collapseFloorY;
        Renderer[] _collapseRenderers;

        /// <summary>Latched forever by <see cref="BeginDeathPresentation"/>, and the FIRST thing
        /// Update returns on. The IsDead guard alone is not enough: on a CLONE the local
        /// Damageable never takes damage (hits apply host-side), so IsDead stays false there and
        /// full AI would resume — on a corpse with a disabled agent — the moment the collapse
        /// ended. Subclass presentation reads it too, so a dead Cloaker cannot re-cloak.</summary>
        protected bool IsPresentationDead => _presentationDead;

        /// <summary>Inert corpse: fire the authored Death clip (or board-tip a controller-less
        /// body), agent and colliders off. Host fires this from onDeath; clones fire it from
        /// NetEnemy's death Rpc. Burst stays host-side.</summary>
        public void BeginDeathPresentation()
        {
            // Before enabled=true below: setting enabled fires OnEnable/Start on a clone whose
            // component was off since spawn, and those observers must already see a corpse.
            _presentationDead = true;
            StopAgent();
            if (_agent != null) _agent.enabled = false;
            var cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != null) cols[i].enabled = false;

            // Clones disable this component on spawn so AI does not run. LateUpdate has to
            // keep ticking for the floor clamp; Update bails on the _presentationDead latch
            // (IsDead is no guard here — a clone's local Damageable never took the damage).
            enabled = true;
            _collapsing = true;
            _collapseT = 0f;
            _collapsePos = transform.position;
            // Walkable plane the agent was holding, not mesh min.y: the standing rig's skinned
            // bounds already pad below the sole, and the fall puts body thickness below the
            // feet — clamping to THIS plane is what keeps the corpse out of the floor slab.
            float lift = _agent != null ? _agent.baseOffset : 0f;
            _collapseFloorY = _collapsePos.y - lift;
            _collapseRenderers = GetComponentsInChildren<Renderer>(true);

            bool clipDriven = _animator != null && _animator.isActiveAndEnabled
                              && _animator.runtimeAnimatorController != null;
            _collapseProcedural = !clipDriven;
            if (clipDriven)
            {
                _animator.applyRootMotion = false; // root channels are baked into pose (fbx meta)
                _animator.SetTrigger(AnimDie);
                return;
            }
            // No controller to play: freeze whatever pose the body holds and board-tip it.
            if (_animator != null) _animator.enabled = false;
            _collapseFrom = transform.rotation;
            _collapseAxis = transform.right;
            _collapsePivot = new Vector3(_collapsePos.x, _collapseFloorY, _collapsePos.z);
        }

        void LateUpdate()
        {
            if (!_collapsing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) dt = 0.02f;
            _collapseT += dt;

            if (!_collapseProcedural)
            {
                // Clip mode: the Animator wrote this frame's pose (bones only — zero root
                // motion), so the root is re-planted at the death spot and the clamp lift is
                // recomputed ABSOLUTELY from there. Additive lift would compound across
                // frames; it was only correct procedurally because the pose reset came first.
                transform.position = _collapsePos;
                float clipMinY = EncapsulatedMinY(_collapseRenderers);
                if (!float.IsPositiveInfinity(clipMinY) && clipMinY < _collapseFloorY)
                    transform.position = _collapsePos + Vector3.up * (_collapseFloorY - clipMinY);
                return; // clamp holds until despawn — the held last frame must stay planted
            }

            float u = Mathf.Clamp01(_collapseT / CollapseSeconds);
            u = u * u * (3f - 2f * u);
            transform.SetPositionAndRotation(_collapsePos, _collapseFrom);
            transform.RotateAround(_collapsePivot, _collapseAxis, -90f * u);
            float minY = EncapsulatedMinY(_collapseRenderers);
            // No live renderer sampled = no lift. A 0f fallback would read as "mesh at world
            // y 0", which on any storey above ground (floor plane at +5, +10 …) would lift
            // the corpse by the whole storey height.
            if (!float.IsPositiveInfinity(minY) && minY < _collapseFloorY)
                transform.position += Vector3.up * (_collapseFloorY - minY);
            if (u >= 1f) _collapsing = false;
        }

        /// <summary>Lowest world-space bounds min.y across the live renderers, or +infinity
        /// when none contributed — the caller skips the floor lift then.</summary>
        static float EncapsulatedMinY(Renderer[] rends)
        {
            float minY = float.PositiveInfinity;
            if (rends == null) return minY;
            for (int i = 0; i < rends.Length; i++)
            {
                Renderer r = rends[i];
                if (r == null || !r.enabled) continue;
                float y = r.bounds.min.y;
                if (y < minY) minY = y;
            }
            return minY;
        }

        void OnDeath()
        {
            // The kill LINE belongs to WaveSpawner: it owns the alive count this death changes,
            // and its listener closes over this gatekeeper's kind. So does the DESTROY —
            // Despawn is the one path that removes a spawned actor from the ledger and the
            // scene. A hand-placed actor has no spawner and cleans up after itself.
            BeginDeathPresentation();
            SpawnDeathBurst();
            var spawner = _spawner;
            if (spawner != null) spawner.Despawn(this, CorpseSeconds);
            else Destroy(gameObject);
        }

        /// <summary>Kill-confirm particle explosion so deaths read as an event, not a vanish. The
        /// tint comes off the renderer, so each kind's burst is its own colour for free.</summary>
        void SpawnDeathBurst()
        {
            var myRenderer = GetComponentInChildren<Renderer>();
            Color tint = FxColors.EnemyAlert;
            var mat = myRenderer != null ? myRenderer.sharedMaterial : null;
            if (mat != null)
            {
                if (mat.HasProperty("_Color")) tint = mat.color;
                else if (mat.HasProperty("_BaseColor")) tint = mat.GetColor("_BaseColor");
            }
            Effects.DeathExplosion(transform.position, tint);
        }
    }
}
