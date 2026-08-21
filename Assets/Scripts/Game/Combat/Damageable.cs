using System.Collections;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.Events;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: generic hit-point pool for scene actors (enemies in v0) —
    /// TakeDamage drains Hp, flashes the renderer so hits are visibly registered, and fires
    /// onDeath exactly once when Hp reaches zero.</summary>
    /// <remarks>onDeath fires EXACTLY once per death (_deathFired), because a listener commonly
    /// destroys the owning GameObject: a second invoke would run destroy logic on an object
    /// already queued for destruction. The flash coroutine is stopped and the listener list
    /// cleared on disable/destroy so nothing touches a destroyed Renderer.</remarks>
    public sealed class Damageable : MonoBehaviour
    {
        const string Tag = "Damageable";

        // 30 mirrors WaveDirector.StatsFor(Default).maxHp (the gatekeeper baseline) so an actor built
        // without a SceneBuilder pass still starts at a tuned value. WaveSpawner overrides it per
        // spawn with the actor's OWN kind's hp — a Hunter is 20 and a Thief is 120, and neither of
        // those numbers may ever be inferred from this one.
        [SerializeField] float maxHp = 30f;
        [SerializeField] Color hitFlashColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] float hitFlashSeconds = 0.09f;

        /// <summary>Invoked once when Hp reaches zero.</summary>
        public UnityEvent onDeath = new UnityEvent();

        /// <summary>Current hit points.</summary>
        public float Hp { get; private set; }

        /// <summary>Maximum hit points.</summary>
        public float MaxHp => maxHp;

        /// <summary>True once Hp has reached zero.</summary>
        public bool IsDead => Hp <= 0f && _initialized;

        /// <summary>True when a player weapon slot recorded the most recent damaging hit.</summary>
        public bool HasLastDamagingSlot { get; private set; }

        /// <summary>Slot that dealt the most recent damaging hit (killing blow attribution).</summary>
        public WeaponSlot LastDamagingSlot { get; private set; }

        bool _initialized;
        bool _deathFired;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        Color _baseColor;
        bool _hasBaseColor;
        // False when the material exposes no colour at all (a bought pack's Shader Graph is the
        // case that found this). Flashing such a renderer writes a property the shader ignores,
        // so the hit reads as no feedback at all rather than as a red flash.
        bool _hasTint;
        Coroutine _flash;

        void Awake()
        {
            Hp = maxHp;
            _initialized = true;
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _mpb = new MaterialPropertyBlock();
                var mat = _renderer.sharedMaterial;
                // HasProperty answers for a property of ANY type, so a shader that exposes
                // _BaseColor as a TEXTURE passed the guard and GetColor then threw once per
                // spawn (SG_SciFiRobotMat, 32 errors in one session, 2026-08-18). HasColor is
                // the type-checked question and the only one worth asking.
                if (mat != null && mat.HasColor("_BaseColor"))
                {
                    _baseColor = mat.GetColor("_BaseColor");
                    _hasBaseColor = true;
                    _hasTint = true;
                }
                else if (mat != null && mat.HasColor("_Color"))
                {
                    _baseColor = mat.GetColor("_Color");
                    _hasTint = true;
                }
            }
        }

        void OnDisable()
        {
            // Guard: a flash coroutine resuming after the renderer is gone would NRE in ClearColor.
            if (_flash != null)
            {
                StopCoroutine(_flash);
                _flash = null;
            }
        }

        void OnDestroy()
        {
            if (onDeath != null) onDeath.RemoveAllListeners(); // drop listeners so nothing calls into a dead object
            Damaged = null;
        }

        /// <summary>Rescales max hp (e.g. wave health multiplier); refills current Hp by default.</summary>
        public void SetMaxHp(float value, bool refill = true)
        {
            maxHp = Mathf.Max(1f, value);
            if (refill || Hp > maxHp) Hp = maxHp;
        }

        /// <summary>Raised on every hit with the attacker's transform (null if unknown). The AI
        /// listens so that being shot makes a gatekeeper turn on whoever shot it.</summary>
        public event System.Action<Transform> Damaged;

        /// <summary>Visual hit only. Does not change Hp, IsDead, or fire onDeath — client
        /// clones flash from replicated HP without applying local damage.</summary>
        public void NotifyHitFlash()
        {
            FlashHit();
        }

        /// <summary>Applies damage; flashes the hit, reports the attacker, fires onDeath when this
        /// kills the owner. <paramref name="source"/> is who dealt it — pass the shooter.</summary>
        public void TakeDamage(float amount, Transform source = null)
        {
            TakeDamage(amount, source, recordSlot: false, slot: default);
        }

        /// <summary>Applies damage and records the weapon slot for kill attribution.</summary>
        public void TakeDamage(float amount, Transform source, WeaponSlot slot)
        {
            TakeDamage(amount, source, recordSlot: true, slot);
        }

        void TakeDamage(float amount, Transform source, bool recordSlot, WeaponSlot slot)
        {
            if (IsDead || _deathFired || amount <= 0f) return;
            if (recordSlot)
            {
                HasLastDamagingSlot = true;
                LastDamagingSlot = slot;
            }
            Hp -= amount;
            FlashHit();
            var damaged = Damaged;
            if (damaged != null)
            {
                // Guard trip: one bad listener must not swallow the rest of the damage pipeline.
                try { damaged(source); }
                catch (System.Exception e) { Log.Error(Tag, $"Damaged listener threw {e.GetType().Name}: {e.Message}"); }
            }
            if (Hp > 0f) return;

            Hp = 0f;
            _deathFired = true; // guard: onDeath listeners destroy this object; never fire twice
            try
            {
                if (onDeath != null) onDeath.Invoke();
            }
            catch (System.Exception e)
            {
                // Guard trip: one bad listener must not abort the rest of the damage pipeline.
                Log.Error(Tag, $"onDeath listener threw {e.GetType().Name}: {e.Message}");
            }
        }

        void FlashHit()
        {
            if (_renderer == null || _mpb == null || !isActiveAndEnabled) return;
            if (!_hasTint) return; // no colour to drive; a write here would be silently discarded
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            SetColor(hitFlashColor);
            yield return new WaitForSeconds(hitFlashSeconds);
            ClearColor();
            _flash = null;
        }

        void SetColor(Color c)
        {
            if (_renderer == null || _mpb == null) return; // guard: renderer destroyed mid-flash
            string prop = _hasBaseColor ? "_BaseColor" : "_Color";
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, c);
            _renderer.SetPropertyBlock(_mpb);
        }

        void ClearColor()
        {
            if (_renderer == null || _mpb == null) return; // guard: renderer destroyed mid-flash
            // Clearing the property block restores the shared material's own color.
            _mpb.Clear();
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
