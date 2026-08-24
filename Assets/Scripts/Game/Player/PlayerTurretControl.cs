using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the Boarder's hold on FORGE's machine — where it stands, and what
    /// redlining it costs. Separate from <see cref="PlayerProtocolAbilities"/> because that
    /// owns windows, input and log lines while this owns a thing in the world with life of its
    /// own; they would pass as separate classes in any language that forced the question.</summary>
    public sealed class PlayerTurretControl : MonoBehaviour
    {
        const string Tag = "Protocol";

        /// <summary>How far ahead a Boarder may stand a turret. Beyond this they are looking at
        /// open air and it goes down at their feet instead.</summary>
        const float PlaceReachMetres = 12f;

        readonly TurretState _state = new TurretState();

        GameController _game;
        FirstPersonController _fpc;
        TurretObject _live;

        public TurretState State => _state;

        /// <summary>The session's verbs, or null solo. When present the SERVER owns the machine
        /// that shoots: a guest standing one locally would be a gun no gatekeeper can see and no
        /// teammate can stand in the aura of.</summary>
        NetVerbs Verbs
        {
            get
            {
                var net = _game != null ? _game.Net : null;
                return net != null ? net.LocalVerbs : null;
            }
        }

        /// <summary>The standing machine, or null. Target selection reads this for MAGNET HANDS.
        /// Null on a guest even while their turret stands, because the machine is the server's.</summary>
        public TurretObject Live => _live != null && _live.IsAlive ? _live : null;

        /// <summary>Whether a machine is standing for this Boarder anywhere — the server's in a
        /// session, the local one solo. Asking <see cref="Live"/> would tell a guest they have
        /// no turret while one is plainly standing in front of them.</summary>
        public bool HasTurret
        {
            get
            {
                var verbs = Verbs;
                return verbs != null ? verbs.ServerTurretStanding : Live != null;
            }
        }

        public void Bind(GameController game, FirstPersonController fpc)
        {
            _game = game;
            _fpc = fpc;
            _state.ResetForRun();
        }

        /// <summary>Stand a turret where the Boarder is looking. Replaces the one already
        /// standing — Field Deploy is one machine, and the cooldown is what rations it.</summary>
        public bool Deploy(float satReady)
        {
            Clear();

            Vector3 at = PlacementPoint();
            var verbs = Verbs;
            if (verbs != null)
            {
                // The server stands it, fires it and kills it; the scenery comes back by RPC.
                verbs.RequestTurret(at, true);
                Log.Event(Tag, "crew Field Deploy — turret requested at "
                    + at.ToString("0.#") + " sat-ready=" + satReady.ToString("0.##"));
                return true;
            }

            string reason;
            if (!_state.TryPlace(at.x, at.y, at.z, out reason))
            {
                Log.Event(Tag, "crew Field Deploy refused — " + reason);
                return false;
            }
            _live = TurretObject.Spawn(at, _state, _game, transform);
            Log.Event(Tag, "crew Field Deploy — turret standing at "
                + _state.MaxHp.ToString("0") + " HP, aura "
                + BoarderProtocol.TurretAuraRadius.ToString("0") + "m x"
                + BoarderProtocol.TurretAuraFireMul.ToString("0.##")
                + " sat-ready=" + satReady.ToString("0.##"));
            return true;
        }

        public void SetOvercharged(bool on)
        {
            var verbs = Verbs;
            if (verbs != null)
            {
                verbs.RequestOvercharge(on);
                return;
            }
            _state.SetOvercharged(on);
        }

        /// <summary>Spend the machine's life for the spike. True when the burn is what killed
        /// it — the caller's window then has nothing left to redline.</summary>
        public bool TickOvercharge(float dt)
        {
            // In a session the burn is spent on the server's machine by the server's clock.
            // Burning here too would charge the same life twice.
            if (Verbs != null) return false;
            float burned;
            if (!_state.BurnOvercharge(dt, out burned)) return false;
            Log.Event(Tag, "self Overcharge burned the turret out — "
                + _state.LifeBurned.ToString("0") + " life spent");
            Clear();
            return true;
        }

        /// <summary>How level a surface has to be to stand a turret on it. A raycast hits walls
        /// and ceilings just as readily as floors, and a turret bolted to a wall face reads as
        /// a bug rather than a placement.</summary>
        const float MinFloorNormalY = 0.7f;

        /// <summary>Where the turret goes: the floor the Boarder is looking at within reach, or
        /// a step in front of them when they are looking at a wall or at open air.</summary>
        Vector3 PlacementPoint()
        {
            Transform eye = _fpc != null && _fpc.cameraTransform != null
                ? _fpc.cameraTransform
                : transform;
            RaycastHit hit;
            if (Physics.Raycast(eye.position, eye.forward, out hit, PlaceReachMetres)
                && hit.normal.y >= MinFloorNormalY)
                return hit.point;
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            return transform.position + fwd.normalized * 3f;
        }

        public void Clear()
        {
            var verbs = Verbs;
            if (verbs != null && verbs.ServerTurretStanding)
                verbs.RequestTurret(Vector3.zero, false);
            if (_live != null) _live.Despawn();
            _live = null;
            _state.Clear();
        }

        void OnDisable()
        {
            // Never leave a machine standing on a torn-down rig: its aura is a static list
            // entry and would outlive the Boarder who placed it.
            Clear();
        }
    }
}
