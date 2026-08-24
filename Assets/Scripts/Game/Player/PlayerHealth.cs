using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: player hit points. On the local rig, death is a RUN ENDER:
    /// the body locks and GameController.PlayerDied() ends the run. A networked puppet sets
    /// endsTheRunOnDeath false so down does not take the host with it.</summary>
    /// <remarks>Self-registers in PlayerRegistry (OnEnable/OnDisable) when contributeToRegistry
    /// is true — the sanctioned way for other systems to find players. Networked avatars leave
    /// that off and the server registers the remote body itself.
    /// Die() is idempotent and never leaves the player in a captured-cursor dead body: if no
    /// GameController is present to raise the lose screen it frees the cursor itself and logs
    /// an error, so "dead, mouse locked, no UI" cannot happen.</remarks>
    public sealed class PlayerHealth : MonoBehaviour
    {
        const string Tag = "PlayerHealth";

        [SerializeField] float maxHp = 100f;

        [Tooltip("Optional respawn transform; defaults to the player's starting pose.")]
        [OptionalBinding] public Transform spawnPoint;

        [SerializeField, Tooltip("Off on networked avatars — the server registers the remote body.")]
        bool contributeToRegistry = true;

        [SerializeField, Tooltip("Off on networked avatars — puppet down must not end the host run.")]
        bool endsTheRunOnDeath = true;

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
        // Pushed by GameController.ApplyMetaEffects; Die() reports the run end through it.
        GameController _game;
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
            if (contributeToRegistry)
                PlayerRegistry.Register(this);
        }

        void OnDisable()
        {
            PlayerRegistry.Unregister(this);
        }

        /// <summary>Ghost acquisition multiplier. 1 = unbiased. Pushed at spawn, never serialized.</summary>
        public float AcquisitionMul { get; private set; } = 1f;

        bool _bulkheadArmed;

        /// <summary>Cloak: off every targeting list while true. This is a removal, not a bias —
        /// <see cref="PlayerRegistry.NearestAlive"/> skips the player outright, so no
        /// multiplier on <see cref="AcquisitionMul"/> could express it.</summary>
        public bool Cloaked { get; private set; }

        public void SetCloaked(bool cloaked)
        {
            Cloaked = cloaked;
        }

        public void ConfigureAcquisition(float mul)
        {
            AcquisitionMul = mul > 0f ? mul : 1f;
        }

        public void ArmBulkhead() { _bulkheadArmed = true; }

        /// <summary>Applies damage; a killing blow ends the run via GameController.PlayerDied().
        /// <paramref name="source"/> is optional and only enriches the death log.</summary>
        public void TakeDamage(float amount, Transform source = null)
        {
            if (IsDead || amount <= 0f || Invulnerable) return;
            if (_bulkheadArmed && source != null)
            {
                var gk = source.GetComponent<EnemyGatekeeper>();
                if (gk != null)
                {
                    _bulkheadArmed = false;
                    Log.Event("Protocol", "Bulkhead negated " + amount.ToString("0") + " from " + gk.name);
                    var proto = GetComponent<PlayerProtocolAbilities>();
                    if (proto != null) proto.NotifyBulkheadConsumed();
                    return;
                }
            }
            if (source != null)
            {
                _lastAttacker = source;
                _lastAttackerTime = Time.time;
            }
            float taken = Mathf.Min(amount, Hp); // overkill is not damage taken
            Hp -= amount;
            bool fatal = Hp <= 0f;
            if (fatal) Hp = 0f;

            // Demo floor: on the Ship while the demo is active, no damage source may kill —
            // the wake fight teaches getting hit, not dying. Covers melee, falls, self.
            if (fatal && DemoShipFight.FloorsPlayerHp)
            {
                Hp = 1f;
                fatal = false;
            }

            // One line per damaging hit, recorded before Die() so the killing blow precedes death.
            Log.Event("Player", $"took {taken:0} damage — HP {Hp:0}/{maxHp:0}");
            if (fatal) Die();
        }

        /// <summary>Off on networked avatars so OnEnable does not pollute PlayerRegistry.</summary>
        public bool ContributeToRegistry
        {
            get { return contributeToRegistry; }
            set
            {
                contributeToRegistry = value;
                if (!value)
                    PlayerRegistry.Unregister(this);
            }
        }

        /// <summary>Off on networked avatars so puppet Die() does not call PlayerDied.</summary>
        public bool EndsTheRunOnDeath
        {
            get { return endsTheRunOnDeath; }
            set { endsTheRunOnDeath = value; }
        }

        /// <summary>Adopt the scene's GameController (called from ApplyMetaEffects at run
        /// start). Death routes into the sim through this handle.</summary>
        public void Bind(GameController game)
        {
            _game = game;
        }

        /// <summary>Owner-side view of replicated HP. Does not write back, does not log a hit.
        /// Lethal still runs Die(); a restore from 0 clears IsDead without moving the body.
        /// Replicated death has no attacker, so the death line says gatekeeper and reads
        /// live count from NetRunMirror when this peer is client-only.</summary>
        public void ApplyReplicatedHp(float hp)
        {
            hp = Mathf.Clamp(hp, 0f, maxHp);
            if (hp <= 0f)
            {
                if (DemoShipFight.FloorsPlayerHp)
                {
                    Hp = 1f;
                    return;
                }
                Hp = 0f;
                if (!IsDead) Die(replicatedHp: true);
                return;
            }

            Hp = hp;
            if (!IsDead) return;
            IsDead = false;
            if (_fpc == null) _fpc = GetComponent<FirstPersonController>();
            if (_fpc != null) _fpc.MovementLocked = false;
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
        void Die(bool replicatedHp = false)
        {
            if (IsDead) return; // guard: re-entrancy (a second killing hit inside the same frame)
            IsDead = true;

            var proto = GetComponent<PlayerProtocolAbilities>();
            if (proto != null) proto.NotifyDeath();

            if (_fpc == null) _fpc = GetComponent<FirstPersonController>(); // re-resolve if fake-null
            if (_fpc != null) _fpc.MovementLocked = true;

            if (!endsTheRunOnDeath)
            {
                Log.Event("Player", "down — host run continues");
                return;
            }

            string killer = "unknown";
            if (_lastAttacker != null && Time.time - _lastAttackerTime < 5f)
                killer = $"gatekeeper at {Vector3.Distance(transform.position, _lastAttacker.position):0.0}m";
            else if (replicatedHp)
                killer = "gatekeeper";

            var gc = _game;
            // Logged before the sim is told: PlayerDied ends the run synchronously.
            Log.Event("Player", $"YOU DIED — killed by {killer}, {AliveForDeathLine()} gatekeepers still alive");

            if (gc != null)
            {
                if (gc.Net != null && gc.Net.IsClientOnly)
                {
                    Log.Event("Player", "down — client does not end the host run");
                    return;
                }
                gc.PlayerDied(this);
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

        int AliveForDeathLine()
        {
            var gc = _game;
            if (gc == null) return 0;
            var net = gc.Net;
            if (net != null && net.IsClientOnly)
            {
                var mirror = net.Mirror;
                if (mirror != null) return mirror.Alive.Value;
                return 0;
            }
            return gc.EnemiesAlive;
        }
    }
}
