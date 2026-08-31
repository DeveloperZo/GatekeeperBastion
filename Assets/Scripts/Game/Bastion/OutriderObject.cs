using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>FORGE's follower. Charge and the phase chain live in <see cref="OutriderState"/>;
    /// this is presentation, follow, and the firing clock. Targets the Gatekeeper nearest to
    /// the Boarder, not to itself. Same server-owns-gun / scenery-copy split as
    /// <see cref="TurretObject"/>.</summary>
    public sealed class OutriderObject : MonoBehaviour
    {
        const string Tag = "Outrider";
        const float FollowSpeedMetres = 16f;
        /// <summary>Chest height so a first-person camera sees the body when the lap
        /// crosses in front. Foot height hid it under the near clip.</summary>
        const float OrbitHeightMetres = 1.35f;
        /// <summary>Pushes the orbit centre forward of the Boarder so more of the lap is
        /// in the look direction (and the EMP origin is not parked 4 m behind them).</summary>
        const float OrbitForwardMetres = 1.6f;
        /// <summary>~5 s per lap. Presentation only — pairing reads world position,
        /// not this angle.</summary>
        const float OrbitRadiansPerSecond = 1.25f;

        static readonly List<OutriderObject> Live = new List<OutriderObject>();

        OutriderState _state;
        GameController _game;
        Transform _owner;
        bool _presentationOnly;
        Renderer _bodyRenderer;
        TurretLifeBar _lifeBar;
        float _shotClock;
        int _shotsFired;
        float _orbitAngle;
        TurretPhase _fxPhase;
        GameObject _specialAura;

        public OutriderState State => _state;
        public bool IsAlive => _state != null && _state.IsLive;
        public Transform Owner => _owner;

        public static OutriderObject NearestAlive(Vector3 position, float maxDistance)
        {
            OutriderObject best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < Live.Count; i++)
            {
                var o = Live[i];
                if (o == null || !o.IsAlive) continue;
                float sq = (o.transform.position - position).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq = sq;
                best = o;
            }
            return best;
        }

        public static OutriderObject ForOwner(Transform owner)
        {
            if (owner == null) return null;
            for (int i = 0; i < Live.Count; i++)
            {
                var o = Live[i];
                if (o == null || o._presentationOnly || o._owner != owner) continue;
                if (o._state == null || !o._state.MayFire) continue;
                return o;
            }
            return null;
        }

        public void Configure(OutriderState state, GameController game, Transform owner)
        {
            Configure(state, game, owner, false);
        }

        public void Configure(OutriderState state, GameController game, Transform owner,
            bool presentationOnly)
        {
            _state = state;
            _game = game;
            _owner = owner;
            _presentationOnly = presentationOnly;
            _shotClock = 0f;
            _shotsFired = 0;
            _orbitAngle = Mathf.PI * 0.5f;
            _fxPhase = TurretPhase.Available;
            ClearSpecialAura();
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
            TickFollow(Time.deltaTime);
            PushTint();
            PushLife();
            TickSpecialFx();
            if (_presentationOnly) return;

            if (_state.TickExhausted(Time.deltaTime))
            {
                Log.Event(Tag, "outrider exhausted window ended — " + _state.ShotsFired
                    + " rounds fired, " + _state.DamageTaken.ToString("0") + " damage absorbed");
                Despawn();
                return;
            }
            if (_state.MayFire) TickFiring(Time.deltaTime);
        }

        void TickFollow(float dt)
        {
            if (_owner == null) return;
            _orbitAngle += OrbitRadiansPerSecond * dt;
            if (_orbitAngle > Mathf.PI * 2f) _orbitAngle -= Mathf.PI * 2f;
            Vector3 fwd = _owner.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            float r = BoarderProtocol.OutriderFollowDistance;
            float c = Mathf.Cos(_orbitAngle);
            float s = Mathf.Sin(_orbitAngle);
            Vector3 want = _owner.position
                + Vector3.up * OrbitHeightMetres
                + fwd * OrbitForwardMetres
                + right * (c * r)
                + fwd * (s * r);
            transform.position = Vector3.MoveTowards(transform.position, want, FollowSpeedMetres * dt);
            if (!_presentationOnly)
                _state.SetPosition(transform.position.x, transform.position.y, transform.position.z);
        }

        void TickFiring(float dt)
        {
            bool paired = TurretObject.PairingActive(_owner);
            var turret = TurretObject.ForOwner(_owner);
            bool turretOvercharged = turret != null && turret.State != null && turret.State.Overcharged;
            float rateMul = BoarderProtocol.PairedOutriderRateMul(turretOvercharged, paired);
            float interval = _state.ShotInterval(rateMul);
            if (interval <= 0f) return;
            _shotClock += dt;
            if (_shotClock < interval) return;
            _shotClock = 0f;

            var target = AcquireTarget();
            if (target == null) return;

            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);

            var body = target.GetComponent<Damageable>();
            if (body == null || body.IsDead) return;

            body.TakeDamage(BoarderProtocol.OutriderDamagePerShot,
                _owner != null ? _owner : transform);
            _shotsFired++;
            Vector3 muzzle = transform.position + transform.forward * 0.5f + Vector3.up * 0.2f;
            if (_state.Phase == TurretPhase.Special)
                Effects.SpecialMuzzle(muzzle, FxColors.FxEmp);
            else
                Effects.MuzzleFlash(muzzle);

            if (_state.SpendShot())
            {
                Log.Event(Tag, "outrider EMP spent — exhausted after "
                    + _state.ShotsFired + " rounds, "
                    + _state.DamageTaken.ToString("0") + " damage absorbed");
            }
        }

        /// <summary>Nearest live gatekeeper to the Boarder that the Outrider can still reach.
        /// Walks the spawner ledger in place — no allocation, and not every frame: the shot
        /// clock gates this.</summary>
        EnemyGatekeeper AcquireTarget()
        {
            var gc = _game;
            if (gc == null || gc.Spawner == null) return null;
            Transform from = _owner != null ? _owner : transform;
            var live = gc.Spawner.Live;
            float rangeSq = BoarderProtocol.OutriderRangeMetres * BoarderProtocol.OutriderRangeMetres;
            float bestToBoarder = float.MaxValue;
            EnemyGatekeeper best = null;
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                float toGun = (gk.transform.position - transform.position).sqrMagnitude;
                if (toGun > rangeSq) continue;
                float toBoarder = (gk.transform.position - from.position).sqrMagnitude;
                if (toBoarder >= bestToBoarder) continue;
                bestToBoarder = toBoarder;
                best = gk;
            }
            return best;
        }

        public bool ApplyDamage(float amount)
        {
            if (_state == null || !_state.IsLive || amount <= 0f) return false;
            if (!_state.TakeDamage(amount)) return false;
            Log.Event(Tag, "outrider destroyed — took " + _state.DamageTaken.ToString("0")
                + " damage, fired " + _shotsFired + " shot(s)");
            Despawn();
            return true;
        }

        /// <summary>Blast from this machine: light damage and a root on every body in
        /// <see cref="BoarderProtocol.EmpRadiusMetres"/>.</summary>
        public int ApplyEmpBlast()
        {
            var gc = _game;
            if (gc == null || gc.Spawner == null) return 0;
            var live = gc.Spawner.Live;
            Vector3 origin = transform.position;
            Transform source = _owner != null ? _owner : transform;
            int caught = 0;
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                Vector3 p = gk.transform.position;
                if (!BoarderProtocol.EmpCatches(origin.x, origin.y, origin.z, p.x, p.y, p.z))
                    continue;
                var body = gk.GetComponent<Damageable>();
                if (body == null || body.IsDead) continue;
                body.TakeDamage(BoarderProtocol.EmpDamage, source);
                gk.Root(BoarderProtocol.EmpRootSeconds);
                Effects.ImpactSparks(p + Vector3.up);
                caught++;
            }
            return caught;
        }

        /// <summary>Blast disc and looping ember for the EMP window. Scenery copies run
        /// this too so guests see the special without owning the gun.</summary>
        void TickSpecialFx()
        {
            var phase = _state.Phase;
            if (phase == TurretPhase.Special && _fxPhase != TurretPhase.Special)
            {
                Effects.EmpBlast(transform.position, BoarderProtocol.EmpRadiusMetres);
                ClearSpecialAura();
                _specialAura = Effects.AttachSpecialAura(transform, FxColors.FxEmp);
            }
            else if (phase != TurretPhase.Special && _fxPhase == TurretPhase.Special)
            {
                ClearSpecialAura();
            }
            _fxPhase = phase;
        }

        void ClearSpecialAura()
        {
            if (_specialAura == null) return;
            Destroy(_specialAura);
            _specialAura = null;
        }

        void PushLife()
        {
            if (_lifeBar == null) return;
            float max = _state.MaxHp;
            _lifeBar.SetFraction(max > 0f ? _state.Hp / max : 0f);
        }

        void PushTint()
        {
            if (_bodyRenderer == null) return;
            var mat = _bodyRenderer.sharedMaterial;
            if (mat == null) return;
            Color want = new Color(0.45f, 0.85f, 0.4f, 1f);
            if (_state.Phase == TurretPhase.Special)
                want = new Color(0.85f, 0.75f, 0.15f, 1f);
            else if (_state.Phase == TurretPhase.Exhausted)
                want = new Color(0.35f, 0.35f, 0.38f, 1f);
            if (mat.color != want) mat.color = want;
        }

        public void Despawn()
        {
            ClearSpecialAura();
            Live.Remove(this);
            Destroy(gameObject);
        }

        public static OutriderObject Spawn(Vector3 position, OutriderState state,
            GameController game, Transform owner)
        {
            return Spawn(position, state, game, owner, false);
        }

        public static OutriderObject Spawn(Vector3 position, OutriderState state,
            GameController game, Transform owner, bool presentationOnly)
        {
            var go = new GameObject("Outrider");
            go.transform.position = position + Vector3.up * 0.4f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "OutriderBody";
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            StripCollider(body);

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "OutriderBarrel";
            barrel.transform.SetParent(go.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            barrel.transform.localScale = new Vector3(0.14f, 0.14f, 0.7f);
            StripCollider(barrel);

            var rider = go.AddComponent<OutriderObject>();
            rider.Configure(state, game, owner, presentationOnly);
            rider._bodyRenderer = Tint(body, new Color(0.45f, 0.85f, 0.4f, 1f));
            Tint(barrel, new Color(0.28f, 0.55f, 0.25f, 1f));
            rider._lifeBar = TurretLifeBar.Attach(go.transform);
            return rider;
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
