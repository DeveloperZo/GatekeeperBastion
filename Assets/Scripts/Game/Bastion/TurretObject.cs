using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>FORGE's standing turret, and the only player-owned thing that deals damage —
    /// so it goes through <see cref="Damageable.TakeDamage"/> exactly as player fire does, and
    /// kill credit reaches Unbanked through the spawner's own death path without this file
    /// touching the wallet. Charge and the phase chain live in <see cref="TurretState"/>;
    /// this is presentation plus the firing clock.
    /// <para>In a session the server owns the one that shoots and every client carries a
    /// presentation-only copy — see <see cref="Configure(TurretState, GameController,
    /// Transform, bool)"/>. Solo, the local one is both.</para></summary>
    public sealed class TurretObject : MonoBehaviour
    {
        const string Tag = "Turret";

        static readonly List<TurretObject> Live = new List<TurretObject>();

        TurretState _state;
        GameController _game;
        Transform _owner;
        bool _presentationOnly;
        Renderer _bodyRenderer;
        TurretLifeBar _lifeBar;
        float _shotClock;
        int _shotsFired;
        TurretPhase _fxPhase;
        GameObject _specialAura;

        public TurretState State => _state;
        public bool IsAlive => _state != null && _state.IsLive;
        public Transform Owner => _owner;

        /// <summary>Fire-rate multiplier a Boarder standing here gets from a firing turret.
        /// Exhausted machines stand but no longer grant the aura. Zone sources SET rather than
        /// stack, so overlapping turrets stay at one aura.</summary>
        public static float FireMulAt(Vector3 position)
        {
            float mul = 1f;
            float r = BoarderProtocol.TurretAuraRadius;
            float rSq = r * r;
            for (int i = 0; i < Live.Count; i++)
            {
                var t = Live[i];
                if (t == null || t.State == null || !t.State.MayFire) continue;
                Vector3 d = position - t.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > rSq) continue;
                if (BoarderProtocol.TurretAuraFireMul > mul) mul = BoarderProtocol.TurretAuraFireMul;
            }
            return mul;
        }

        /// <summary>Nearest standing turret to a position, or null. Ordinary targeting uses
        /// this when a machine is already in melee of a body chasing the Boarder.</summary>
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

        public static TurretObject ForOwner(Transform owner)
        {
            if (owner == null) return null;
            for (int i = 0; i < Live.Count; i++)
            {
                var t = Live[i];
                if (t == null || t._presentationOnly || t._owner != owner) continue;
                if (t._state == null || !t._state.MayFire) continue;
                return t;
            }
            return null;
        }

        /// <summary>True when this Boarder's turret and Outrider both may fire and sit inside
        /// the pairing radius. Presentation copies are skipped so a session does not double-count.
        /// </summary>
        public static bool PairingActive(Transform owner)
        {
            var turret = ForOwner(owner);
            var rider = OutriderObject.ForOwner(owner);
            if (turret == null || rider == null) return false;
            Vector3 a = turret.transform.position;
            Vector3 b = rider.transform.position;
            return BoarderProtocol.MachinesPaired(a.x, a.y, a.z, b.x, b.y, b.z);
        }

        public void Configure(TurretState state, GameController game, Transform owner)
        {
            Configure(state, game, owner, false);
        }

        /// <summary>A turret exists twice in a session: once on the server, which owns its life
        /// and its gun, and once on every client as scenery. The client copy must never fire —
        /// two guns for one machine would double its output and desync every Damageable it
        /// touched.</summary>
        public void Configure(TurretState state, GameController game, Transform owner,
            bool presentationOnly)
        {
            _state = state;
            _game = game;
            _owner = owner;
            _presentationOnly = presentationOnly;
            _shotClock = 0f;
            _shotsFired = 0;
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
            PushTint();
            PushLife();
            TickSpecialFx();
            if (_presentationOnly) return;

            if (_state.TickExhausted(Time.deltaTime))
            {
                Log.Event(Tag, "turret exhausted window ended — " + _state.ShotsFired
                    + " rounds fired, " + _state.DamageTaken.ToString("0") + " damage absorbed");
                Despawn();
                return;
            }
            if (_state.MayFire) TickFiring(Time.deltaTime);
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
            var rider = OutriderObject.ForOwner(_owner);
            bool empRounds = BoarderProtocol.PairedTurretFiresEmp(
                rider != null && rider.State != null && rider.State.EmpActive,
                PairingActive(_owner));
            if (empRounds) target.Root(BoarderProtocol.EmpRootSeconds);
            _shotsFired++;
            Vector3 muzzle = transform.position + transform.forward * 0.6f + Vector3.up * 0.7f;
            if (_state.Phase == TurretPhase.Special)
                Effects.SpecialMuzzle(muzzle, FxColors.FxOvercharge);
            else
                Effects.MuzzleFlash(muzzle);

            if (_state.SpendShot())
            {
                Log.Event(Tag, "turret Overcharge spent — exhausted after "
                    + _state.ShotsFired + " rounds, "
                    + _state.DamageTaken.ToString("0") + " damage absorbed");
            }
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

        /// <summary>The life bar over the machine. In a session the scenery copy's state is
        /// fed by <see cref="NetVerbs"/>'s replicated Hp, so this one read covers every
        /// seat.</summary>
        void PushLife()
        {
            if (_lifeBar == null) return;
            float max = _state.MaxHp;
            _lifeBar.SetFraction(max > 0f ? _state.Hp / max : 0f);
        }

        /// <summary>Ignite burst and looping ember for the Overcharge window. Scenery copies
        /// run this too, so guests see the special without owning the gun.</summary>
        void TickSpecialFx()
        {
            var phase = _state.Phase;
            if (phase == TurretPhase.Special && _fxPhase != TurretPhase.Special)
            {
                Effects.OverchargeIgnite(transform.position + Vector3.up * 0.4f);
                ClearSpecialAura();
                _specialAura = Effects.AttachSpecialAura(transform, FxColors.FxOvercharge);
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

        /// <summary>Overcharged turrets run hot. Tint is the standing tell; ignition is
        /// <see cref="TickSpecialFx"/>.</summary>
        void PushTint()
        {
            if (_bodyRenderer == null) return;
            var mat = _bodyRenderer.sharedMaterial;
            if (mat == null) return;
            Color want = new Color(0.35f, 0.75f, 1f, 1f);
            if (_state.Phase == TurretPhase.Special)
                want = new Color(1f, 0.35f, 0.1f, 1f);
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

        /// <summary>Build a turret at a world position. Graybox primitive — a base and a barrel
        /// so facing reads. In a session the server calls this for the live machine and each
        /// client calls it again with presentationOnly for scenery.</summary>
        public static TurretObject Spawn(Vector3 position, TurretState state,
            GameController game, Transform owner)
        {
            return Spawn(position, state, game, owner, false);
        }

        public static TurretObject Spawn(Vector3 position, TurretState state,
            GameController game, Transform owner, bool presentationOnly)
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
            turret.Configure(state, game, owner, presentationOnly);
            turret._bodyRenderer = Tint(body, new Color(0.35f, 0.75f, 1f, 1f));
            Tint(barrel, new Color(0.22f, 0.5f, 0.7f, 1f));
            turret._lifeBar = TurretLifeBar.Attach(go.transform);
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
