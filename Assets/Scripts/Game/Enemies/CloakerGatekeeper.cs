using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: the gatekeeper that advances only while no Boarder is looking at it.
    /// Watched — inside a 30° half-angle cone of a live player's planar forward, within
    /// watchRange, with clear line of sight — it refuses every target and holds position;
    /// unobserved it targets exactly as a Default does. The freeze is global: one watcher
    /// stops it for everybody.</summary>
    /// <remarks>
    /// The refusal is a STATE, not a pushed flag: it flips several times a second, so it lives
    /// here and not in <see cref="GatekeeperStats"/>. Both refusal virtuals gate on
    /// <c>_observed</c>; <see cref="CanBeProvoked"/> deliberately does NOT — being shot while
    /// held still latches the grudge, so a wounded Cloaker comes for you the moment you look
    /// away, which is what "targets exactly as a Default" requires.
    ///
    /// 30° IS CHOSEN AGAINST THE SCREEN, NOT BY TASTE. Hip FOV is 60° vertical
    /// (WeaponView.HipFov), ≈91° horizontal at 16:9 — ≈45.7° each side. The cone therefore
    /// leaves a band at each screen edge where a Cloaker still walks ON SCREEN, and that band
    /// is the entire tell; widening the cone to the screen edge deletes it. ADS narrows the
    /// camera FOV while the cone stays fixed — harmless direction, and why the cone is not
    /// derived from the live FOV.
    ///
    /// The edge guard is a Schmitt band, not a delay: held at ≤30°, released only past 36°
    /// (and past 28 m when range is what freed it), polled every 0.1 s. Mouse jitter of a few
    /// degrees can never cross the band, while a deliberate 45° turn crosses it in one poll —
    /// both directions land inside the 0.25 s the milestone's criterion 3 demands.
    ///
    /// Cloak presentation lives in <see cref="CloakerPresentation"/>, NOT here: NetEnemy
    /// disables this component on clones, so anything visual attached to the AI would be dead
    /// on a guest's screen. This class only decides; it pushes the decision to the
    /// presentation and to <see cref="NetEnemy"/>'s replicated bool.
    /// </remarks>
    public sealed class CloakerGatekeeper : EnemyGatekeeper
    {
        // Serialized so the saved prefab carries them (prefab freeze); Reset() keeps the code
        // defaults from diverging silently. watchRange mirrors EnemyGatekeeper.noticeRange.
        [SerializeField] float watchRange = 26f;
        [SerializeField] float watchHalfAngle = 30f;

        /// <summary>Gaze poll cadence. The 0.25 s stop/resume criterion needs better than the
        /// 0.75 s retarget tick; 0.1 s gives two polls of margin at players-count raycasts each.</summary>
        const float WatchRefresh = 0.1f;

        /// <summary>Being damaged forces the solid material this long regardless of gaze, so a
        /// blind hit that connects tells the player where it is.</summary>
        const float RevealOnHitSeconds = 1f;

        /// <summary>Watcher's eye height above their root (the FPS camera sits about here).</summary>
        const float PlayerEyeHeight = 1.6f;

        /// <summary>Point on this body the gaze is measured against — chest, same as the
        /// CanSeePlayer eye on the way out.</summary>
        const float BodyHeight = 1.2f;

        bool _observed;
        float _watchCountdown;
        float _revealRemaining;
        CloakerPresentation _presentation;
        NetEnemy _net;
        Damageable _body;

        public override GatekeeperKind Kind => GatekeeperKind.Cloaker;

        /// <summary>Watched: nothing at all. Unobserved: exactly a Default.</summary>
        protected override bool MayTargetHatch => !_observed && base.MayTargetHatch;

        /// <summary>Watched: nothing at all. Unobserved: exactly a Default.</summary>
        protected override bool MayTargetPlayer => !_observed && base.MayTargetPlayer;

        /// <summary>NOT gated on the gaze: shooting a held Cloaker still latches the grudge
        /// (Retarget refuses to chase while observed, so the freeze holds; the latch simply
        /// survives it). Gating this on _observed would silently swallow the provocation and
        /// break "targets exactly as a Default does" the moment you look away.</summary>
        protected override bool CanBeProvoked => !ignoresPlayer;

        void Reset()
        {
            watchRange = 26f;
            watchHalfAngle = 30f;
        }

        void OnEnable()
        {
            _presentation = GetComponent<CloakerPresentation>();
            _net = GetComponent<NetEnemy>();
            _body = GetComponent<Damageable>();
            if (_body != null) _body.Damaged += OnRevealHit;
            PushCloak();
        }

        void OnDisable()
        {
            if (_body != null) _body.Damaged -= OnRevealHit;
        }

        protected override void KindTick(float dt)
        {
            if (_revealRemaining > 0f)
            {
                _revealRemaining -= dt;
                if (_revealRemaining <= 0f)
                {
                    _revealRemaining = 0f;
                    PushCloak();
                }
            }

            _watchCountdown -= dt;
            if (_watchCountdown > 0f) return;
            _watchCountdown = WatchRefresh;

            bool observed = AnyoneWatching();
            if (observed == _observed) return;
            _observed = observed;
            // Re-derive the target THIS frame, not up to 0.75 s later: the stop must land
            // while the player's turn is still the cause they can read it against.
            Retarget();
            PushCloak();
        }

        /// <summary>The whole rule: any live player whose planar gaze holds this body inside
        /// the cone, in range, with clear line of sight. Iterates PlayerRegistry.All (remote
        /// seats included — NetPlayerAvatar's owner-authoritative NetworkTransform puts a
        /// guest's yaw on the host); allocates nothing.</summary>
        bool AnyoneWatching()
        {
            var players = PlayerRegistry.All;
            Vector3 body = transform.position + Vector3.up * BodyHeight;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.IsDead) continue; // guard: destroyed mid-iteration
                Vector3 eye = player.transform.position + Vector3.up * PlayerEyeHeight;
                if (!CloakerWatch.Watched(player.transform.forward, body - eye, _observed,
                                          watchHalfAngle, watchRange)) continue;
                if (!ClearLineFrom(eye, body)) continue;
                return true;
            }
            return false;
        }

        /// <summary>Same raycast shape as CanSeePlayer, cast the other way: anything solid
        /// between the watcher's eye and this body blocks the sighting; this body's own
        /// colliders do not.</summary>
        bool ClearLineFrom(Vector3 eye, Vector3 body)
        {
            Vector3 to = body - eye;
            float dist = to.magnitude;
            if (dist < 0.05f) return true;
            if (Physics.Raycast(eye, to / dist, out RaycastHit hit, dist,
                                ~0, QueryTriggerInteraction.Ignore))
                return hit.collider != null && hit.collider.transform.IsChildOf(transform);
            return true;
        }

        /// <summary>Being hit forces the solid body for a second, gaze or no gaze. Fires on the
        /// killing blow too — Update stops on IsDead, so the reveal never decays and the corpse
        /// stays visible, which is the point of shooting blind.</summary>
        void OnRevealHit(Transform source)
        {
            _revealRemaining = RevealOnHitSeconds;
            PushCloak();
        }

        /// <summary>One writer for the visible state: local presentation directly (solo and
        /// host), and the replicated bool for clones. Null-safe on both — fixtures have
        /// neither component.</summary>
        void PushCloak()
        {
            // IsPresentationDead as well as IsDead: a clone's Damageable never takes damage, and
            // BeginDeathPresentation re-enables this component (LateUpdate must tick the
            // tip-over), which fires OnEnable → PushCloak — without the latch that call
            // re-cloaked the corpse on a guest's screen.
            bool cloaked = !_observed && _revealRemaining <= 0f && !IsPresentationDead
                           && (_body == null || !_body.IsDead);
            if (_presentation != null) _presentation.SetCloaked(cloaked);
            if (_net != null) _net.SetCloaked(cloaked);
        }
    }
}
