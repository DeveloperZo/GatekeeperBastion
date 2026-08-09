using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>REPLICATED-STATE: player hit points. Death is a RUN ENDER — no in-run respawn:
    /// on death the player is locked down and GameController.PlayerDied() ends the run.</summary>
    /// <remarks>Self-registers in PlayerRegistry (OnEnable/OnDisable) — the sanctioned way for
    /// other systems to find players. ResetToSpawn remains for scene starts only.
    /// Die() is idempotent and never leaves the player in a captured-cursor dead body: if no
    /// GameController is present to raise the lose screen it frees the cursor itself and logs
    /// an error, so "dead, mouse locked, no UI" cannot happen.</remarks>
    public sealed class PlayerHealth : MonoBehaviour
    {
        const string Tag = "PlayerHealth";

        [SerializeField] float maxHp = 100f;

        [Tooltip("Optional respawn transform; defaults to the player's starting pose.")]
        public Transform spawnPoint;

        /// <summary>Current hit points.</summary>
        public float Hp { get; private set; }

        /// <summary>Maximum hit points.</summary>
        public float MaxHp => maxHp;

        /// <summary>True once dead. Permanent for the run — only ResetToSpawn clears it.</summary>
        public bool IsDead { get; private set; }

        /// <summary>Dev F6: when true, TakeDamage is a no-op. Cleared on ResetToSpawn.</summary>
        public bool Invulnerable { get; set; }

        Vector3 _initialPosition;
        Quaternion _initialRotation;
        CharacterController _cc;
        FirstPersonController _fpc;
        Transform _lastAttacker; // death log only: who was on us, and how close
        float _lastAttackerTime;

        void Awake()
        {
            Hp = maxHp;
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _cc = GetComponent<CharacterController>();
            _fpc = GetComponent<FirstPersonController>();
        }

        void OnEnable()
        {
            PlayerRegistry.Register(this);
        }

        void OnDisable()
        {
            PlayerRegistry.Unregister(this);
        }

        /// <summary>Applies damage; a killing blow ends the run via GameController.PlayerDied().
        /// <paramref name="source"/> is optional and only enriches the death log.</summary>
        public void TakeDamage(float amount, Transform source = null)
        {
            if (IsDead || amount <= 0f || Invulnerable) return;
            if (source != null)
            {
                _lastAttacker = source;
                _lastAttackerTime = Time.time;
            }
            float taken = Mathf.Min(amount, Hp); // overkill is not damage taken
            Hp -= amount;
            bool fatal = Hp <= 0f;
            if (fatal) Hp = 0f;

            // Rate-limited to ~1 line/second, aggregating every swing inside that window. Recorded
            // before Die() so the killing blow is in the transcript above the death line (which
            // flushes this window on its way out).
            Log.Event("Player", $"took {taken:0} damage — HP {Hp:0}/{maxHp:0}");
            if (fatal) Die();
        }

        /// <summary>Apply the Crew Quarters VITALITY pool (GameController pushes
        /// MetaUpgrades.PlayerMaxHp here from ApplyMetaEffects).</summary>
        /// <remarks>HEALS TO FULL, unconditionally. This is only ever called at run start, where
        /// "full" is the only correct answer — and the alternatives are all wrong in a way the
        /// player would feel: keeping raw Hp leaves a freshly-bought Vitality level showing
        /// 100/140 on the first frame of the run (the upgrade reads as broken), while scaling Hp
        /// by the ratio would silently carry damage across a run boundary that already heals.
        /// If this ever gets called mid-fight it must be revisited: a free full heal from a shop
        /// purchase is not a thing this game sells.</remarks>
        public void ConfigureMaxHp(float newMaxHp)
        {
            maxHp = Mathf.Max(1f, newMaxHp);
            Hp = maxHp;
            IsDead = false;
            Log.Info(Tag, $"Configured: max HP {maxHp:0} (healed to full).");
        }

        /// <summary>Instant reset used at scene starts: back to spawn at full HP.</summary>
        public void ResetToSpawn()
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : _initialPosition;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : _initialRotation;
            if (_cc != null) _cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            if (_cc != null) _cc.enabled = true;
            Hp = maxHp;
            IsDead = false;
            Invulnerable = false;
            _lastAttacker = null;
            Log.Info(Tag, "Player reset to spawn at full HP.");
        }

        /// <summary>Dev F5: refill to max HP and clear IsDead without moving the body.</summary>
        public void HealToFull()
        {
            Hp = maxHp;
            IsDead = false;
            if (_fpc == null) _fpc = GetComponent<FirstPersonController>();
            if (_fpc != null) _fpc.MovementLocked = false;
            Log.Info(Tag, $"Healed to full — HP {Hp:0}/{maxHp:0}.");
        }

        /// <summary>Death is the run ender: lock the body and report up. The HUD draws the
        /// DOWN overlay; GameController routes the command into the sim (NotifyPlayerDown).</summary>
        void Die()
        {
            if (IsDead) return; // guard: re-entrancy (a second killing hit inside the same frame)
            IsDead = true;

            if (_fpc == null) _fpc = GetComponent<FirstPersonController>(); // re-resolve if fake-null
            if (_fpc != null) _fpc.MovementLocked = true;

            string killer = "unknown";
            if (_lastAttacker != null && Time.time - _lastAttackerTime < 5f)
                killer = $"gatekeeper at {Vector3.Distance(transform.position, _lastAttacker.position):0.0}m";

            var gc = GameController.Instance;
            // Logged before the sim is told: PlayerDied ends the run synchronously.
            Log.Event("Player", $"YOU DIED — killed by {killer}, {(gc != null ? gc.EnemiesAlive : 0)} gatekeepers still alive");

            if (gc != null)
            {
                gc.PlayerDied();
                return;
            }

            // Guard trip: no controller means no sim, no transcript line and no lose screen. Free
            // the cursor so the player can still reach a menu instead of being stuck in a
            // locked-mouse dead body.
            Log.Error(Tag, $"No GameController in the scene: player down (HP {Hp:0}/{maxHp:0}, killer {killer}, " +
                           $"scene '{SceneFlow.ActiveSceneName}') but the run cannot be ended and no lose " +
                           "screen will appear. Freeing the cursor as a last resort.");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
