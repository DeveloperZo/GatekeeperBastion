using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: FORGE's standing turret. The only player-owned thing that deals
    /// damage, so it goes through <see cref="Damageable.TakeDamage"/> exactly as player fire
    /// does — kill credit then reaches Unbanked through the spawner's own death path and this
    /// file never touches the wallet. Life and the overcharge burn live in
    /// <see cref="TurretState"/>; this is presentation plus the firing clock.</summary>
    public sealed class TurretObject : MonoBehaviour
    {
        const string Tag = "Turret";

        static readonly List<TurretObject> Live = new List<TurretObject>();

        TurretState _state;
        GameController _game;
        Transform _owner;
        Renderer _bodyRenderer;
        float _shotClock;
        int _shotsFired;

        public TurretState State => _state;
        public bool IsAlive => _state != null && _state.IsLive;

        /// <summary>Fire-rate multiplier a Boarder standing here gets from a live turret. Zone
        /// sources SET rather than stack, so overlapping turrets stay at one aura.</summary>
        public static float FireMulAt(Vector3 position)
        {
            float mul = 1f;
            float r = BoarderProtocol.TurretAuraRadius;
            float rSq = r * r;
            for (int i = 0; i < Live.Count; i++)
            {
                var t = Live[i];
                if (t == null || !t.IsAlive) continue;
                Vector3 d = position - t.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > rSq) continue;
                if (BoarderProtocol.TurretAuraFireMul > mul) mul = BoarderProtocol.TurretAuraFireMul;
            }
            return mul;
        }

        /// <summary>Nearest live turret to a position, or null. Target selection uses this to
        /// prefer hardware over the Boarder who placed it (MAGNET HANDS).</summary>
        public static TurretObject NearestAlive(Vector3 position, float maxDistance)
        {
            TurretObject best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < Live.Count; i++)
            {
                var t = Live[i];
                if (t == null || !t.IsAlive) continue;
                float sq = (t.transform.position - position).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq = sq;
                best = t;
            }
            return best;
        }

        public void Configure(TurretState state, GameController game, Transform owner)
        {
            _state = state;
            _game = game;
            _owner = owner;
            _shotClock = 0f;
            _shotsFired = 0;
        }

        void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
        }

        void OnDisable()
        {
            Live.Remove(this);
        }

        void Update()
        {
            if (_state == null) return;
            if (!_state.IsLive)
            {
                Despawn();
                return;
            }
            PushTint();
            TickFiring(Time.deltaTime);
        }

        void TickFiring(float dt)
        {
            float interval = _state.ShotInterval();
            if (interval <= 0f) return;
            _shotClock += dt;
            if (_shotClock < interval) return;
            _shotClock = 0f;

            var target = AcquireTarget();
            if (target == null) return;

            // Face the target so the muzzle flash reads as coming from the barrel.
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);

            var body = target.GetComponent<Damageable>();
            if (body == null || body.IsDead) return;

            // Source is the owning Boarder: provoke, kill credit and the hit-flash path all
            // read the source, and a machine with no owner would break every one of them.
            body.TakeDamage(BoarderProtocol.TurretDamagePerShot,
                _owner != null ? _owner : transform);
            _shotsFired++;
            Effects.MuzzleFlash(transform.position + transform.forward * 0.6f + Vector3.up * 0.7f);
        }

        /// <summary>Nearest live gatekeeper in range, off the spawner's ledger — the one list.
        /// No allocation: the ledger is walked in place.</summary>
        EnemyGatekeeper AcquireTarget()
        {
            var gc = _game;
            if (gc == null || gc.Spawner == null) return null;
            var live = gc.Spawner.Live;
            float bestSq = BoarderProtocol.TurretRangeMetres * BoarderProtocol.TurretRangeMetres;
            EnemyGatekeeper best = null;
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                float sq = (gk.transform.position - transform.position).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq = sq;
                best = gk;
            }
            return best;
        }

        /// <summary>Gatekeeper damage intake. True when this hit destroys the turret.</summary>
        public bool ApplyDamage(float amount)
        {
            if (_state == null || !_state.IsLive || amount <= 0f) return false;
            if (!_state.TakeDamage(amount)) return false;
            Log.Event(Tag, "turret destroyed — took " + _state.DamageTaken.ToString("0")
                + " damage, fired " + _shotsFired + " shot(s)");
            Despawn();
            return true;
        }

        /// <summary>Overcharged turrets run hot, and the tint is the only thing that says so
        /// from across the room.</summary>
        void PushTint()
        {
            if (_bodyRenderer == null) return;
            var mat = _bodyRenderer.sharedMaterial;
            if (mat == null) return;
            Color want = _state.Overcharged
                ? new Color(1f, 0.35f, 0.1f, 1f)
                : new Color(0.35f, 0.75f, 1f, 1f);
            if (mat.color != want) mat.color = want;
        }

        public void Despawn()
        {
            Live.Remove(this);
            Destroy(gameObject);
        }

        /// <summary>Build a turret at a world position. Graybox primitive — a base and a barrel
        /// so facing reads. Networked spawn is not built; FIELD DEPLOY is host-side.</summary>
        public static TurretObject Spawn(Vector3 position, TurretState state,
            GameController game, Transform owner)
        {
            var go = new GameObject("Turret");
            go.transform.position = position + Vector3.up * 0.5f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "TurretBody";
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
            StripCollider(body);

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "TurretBarrel";
            barrel.transform.SetParent(go.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.25f, 0.55f);
            barrel.transform.localScale = new Vector3(0.18f, 0.18f, 1.1f);
            StripCollider(barrel);

            var turret = go.AddComponent<TurretObject>();
            turret.Configure(state, game, owner);
            turret._bodyRenderer = Tint(body, new Color(0.35f, 0.75f, 1f, 1f));
            Tint(barrel, new Color(0.22f, 0.5f, 0.7f, 1f));
            return turret;
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) go.layer = ignore;
        }

        static Renderer Tint(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return null;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return rend;
            rend.sharedMaterial = new Material(shader) { color = color };
            return rend;
        }
    }
}
