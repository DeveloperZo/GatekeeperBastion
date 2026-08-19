using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the beat-0 ship intruder driver — walks the Bastion gatekeeper
    /// body at the local player and swings on an interval. Exists because the Bastion brain
    /// (EnemyGatekeeper) needs a GameController and a WaveSpawner, and the solo Ship has
    /// neither; that component is disabled on the instance and this one drives the same
    /// NavMeshAgent and Animator. Damage/HP come from DemoShipFight's constants at spawn —
    /// nothing here survives spawn (push-at-spawn, Architecture gotcha 1).</summary>
    public sealed class ShipIntruder : MonoBehaviour
    {
        const string Tag = "Demo";
        const float RepathSeconds = 0.25f;

        static readonly int AnimSpeed = Animator.StringToHash("Speed");
        static readonly int AnimAttack = Animator.StringToHash("Attack");

        float _damage;
        float _swingSeconds;
        float _moveSpeed;
        float _attackRange;
        float _attackTimer;
        float _repathTimer;
        NavMeshAgent _agent;
        Animator _animator;
        Damageable _damageable;
        bool _agentDriven;

        public void Configure(float damage, float swingSeconds, float moveSpeed,
            float attackRange)
        {
            _damage = damage;
            _swingSeconds = swingSeconds;
            _moveSpeed = moveSpeed;
            _attackRange = attackRange;
        }

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponentInChildren<Animator>();
            _damageable = GetComponent<Damageable>();
        }

        void Start()
        {
            // The ship deck has no baked NavMesh (A10, 2026-08-17 review). Straight
            // planar walk is the live path; an on-mesh agent is used if one exists.
            _agentDriven = _agent != null && _agent.enabled && _agent.isOnNavMesh;
            if (_agentDriven)
            {
                _agent.speed = _moveSpeed;
                _agent.stoppingDistance = _attackRange * 0.8f;
            }
            else if (_agent != null)
            {
                _agent.enabled = false;
            }
        }

        void Update()
        {
            if (_damageable != null && _damageable.IsDead) return;

            var player = PlayerRegistry.Local;
            if (player == null || player.IsDead) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f; // planar — melee range must not be eaten by eye height
            float dist = toPlayer.magnitude;

            _attackTimer -= Time.deltaTime;
            if (dist <= _attackRange)
            {
                Face(toPlayer);
                if (_agentDriven && _agent.hasPath) _agent.ResetPath();
                if (_attackTimer <= 0f)
                {
                    _attackTimer = _swingSeconds;
                    if (_animator != null) _animator.SetTrigger(AnimAttack);
                    player.TakeDamage(_damage, transform);
                }
                SetAnimSpeed(0f);
                return;
            }
            if (_agentDriven)
            {
                _repathTimer -= Time.deltaTime;
                if (_repathTimer <= 0f)
                {
                    _repathTimer = RepathSeconds;
                    _agent.SetDestination(player.transform.position);
                }
                SetAnimSpeed(_agent.velocity.magnitude);
                return;
            }

            Vector3 step = toPlayer.normalized * (_moveSpeed * Time.deltaTime);
            transform.position += step;
            Face(toPlayer);
            SetAnimSpeed(_moveSpeed);
        }

        void Face(Vector3 planarDir)
        {
            if (planarDir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(planarDir, Vector3.up);
        }

        void SetAnimSpeed(float v)
        {
            if (_animator != null) _animator.SetFloat(AnimSpeed, v);
        }
    }
}
