using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the Boarder's hold on FORGE's machine — where it stands, and
    /// whether Overcharge is running. Separate from <see cref="PlayerProtocolAbilities"/>
    /// because that owns windows, input and log lines while this owns a thing in the world;
    /// they would pass as separate classes in any language that forced the question.</summary>
    public sealed class PlayerTurretControl : MonoBehaviour
    {
        const string Tag = "Protocol";

        /// <summary>How far ahead a Boarder may stand a turret. Beyond this they are looking at
        /// open air and it goes down at their feet instead.</summary>
        const float PlaceReachMetres = 12f;

        readonly TurretState _state = new TurretState();

        GameController _game;
        FirstPersonController _fpc;
        PlayerProtocolAbilities _abilities;
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

        /// <summary>The standing machine, or null. Null on a guest even while their turret
        /// stands, because the machine is the server's.</summary>
        public TurretObject Live => _live != null && _live.IsAlive ? _live : null;

        /// <summary>Whether a machine is standing for this Boarder anywhere — the server's in a
        /// session, the local one solo. Asking <see cref="Live"/> would tell a guest they have
        /// no turret while one is plainly standing in front of them.</summary>
        public bool HasTurret
        {
            get
            {
                var verbs = Verbs;
                return verbs != null ? verbs.ServerTurretStanding : _state.IsLive;
            }
        }

        public bool IsExhausted
        {
            get
            {
                var verbs = Verbs;
                return verbs != null
                    ? verbs.ServerTurretPhase == TurretPhase.Exhausted
                    : _state.Phase == TurretPhase.Exhausted;
            }
        }

        public bool IsOvercharged
        {
            get
            {
                var verbs = Verbs;
                return verbs != null
                    ? verbs.ServerTurretPhase == TurretPhase.Special
                    : _state.Overcharged;
            }
        }

        public float Charge
        {
            get
            {
                var verbs = Verbs;
                return verbs != null ? verbs.ServerTurretCharge : _state.Charge;
            }
        }

        public float ChargeMax => BoarderProtocol.TurretChargeMax;

        bool _crewHolding;
        float _crewHold;
        bool _crewHoldFired;

        public void CancelCrewHold()
        {
            _crewHolding = false;
            _crewHoldFired = false;
            _crewHold = 0f;
        }

        /// <summary>Press versus hold on the crew binding. Tap deploys, redeploys, or despawns
        /// Exhausted. Hold past the threshold Overcharges if the machine has charge.</summary>
        public void TickCrewBinding(float dt, bool paused, ref BoarderAbilityState crew)
        {
            if (paused)
            {
                CancelCrewHold();
                return;
            }

            bool held = Controls.Held(ControlId.CrewAbility);
            if (held)
            {
                if (!_crewHolding)
                {
                    _crewHolding = true;
                    _crewHold = 0f;
                    _crewHoldFired = false;
                }
                _crewHold += dt;
                if (!_crewHoldFired && _crewHold >= BoarderProtocol.TurretHoldThresholdSeconds)
                {
                    _crewHoldFired = true;
                    if (!HasTurret)
                        Log.Event(Tag, "crew Overcharge refused — no turret standing");
                    else if (!IsExhausted)
                        TryOvercharge();
                }
                return;
            }

            if (!_crewHolding) return;
            _crewHolding = false;
            if (!_crewHoldFired) TapCrew(ref crew);
        }

        void TapCrew(ref BoarderAbilityState crew)
        {
            float ready = crew.SecondsSatReady;
            if (IsExhausted)
            {
                Clear();
                if (crew.IsReady)
                    crew.TrySpendInstant(BoarderProtocol.Cooldown(BoarderAbilityId.FieldDeploy));
                Log.Event(Tag, "crew Field Deploy — exhausted turret despawned sat-ready="
                    + ready.ToString("0.##"));
                return;
            }
            if (!crew.IsReady)
            {
                Log.Event(Tag, "crew refused — " + crew.CooldownRemaining.ToString("0.#") + "s left");
                return;
            }
            if (!crew.TrySpendInstant(BoarderProtocol.Cooldown(BoarderAbilityId.FieldDeploy))) return;
            Deploy(ready);
        }

        public void Bind(GameController game, FirstPersonController fpc)
        {
            _game = game;
            _fpc = fpc;
            _abilities = GetComponent<PlayerProtocolAbilities>();
            _state.ResetForRun();
        }

        /// <summary>Stand a turret where the Boarder is looking. Replaces a live one and keeps
        /// its charge. Refuses Exhausted — that press despawns instead.</summary>
        public bool Deploy(float satReady)
        {
            Vector3 at = PlacementPoint();
            var verbs = Verbs;
            if (verbs != null)
            {
                verbs.RequestTurret(at, true);
                NoteDeploy();
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
            if (_live == null)
                _live = TurretObject.Spawn(at, _state, _game, transform);
            else
                _live.transform.position = at;
            NoteDeploy();
            Log.Event(Tag, "crew Field Deploy — turret standing at "
                + _state.MaxHp.ToString("0") + " HP, aura "
                + BoarderProtocol.TurretAuraRadius.ToString("0") + "m x"
                + BoarderProtocol.TurretAuraFireMul.ToString("0.##")
                + " charge=" + _state.Charge.ToString("0")
                + "/" + _state.ChargeMax.ToString("0")
                + " sat-ready=" + satReady.ToString("0.##"));
            return true;
        }

        /// <summary>Hold: spend charge on Overcharge. Refuses by naming charge, not cooldown.
        /// </summary>
        public bool TryOvercharge()
        {
            var verbs = Verbs;
            if (verbs != null)
            {
                verbs.RequestOvercharge(true);
                NoteSpecial(Charge);
                return true;
            }
            string reason;
            float chargeAt = _state.Charge;
            if (!_state.TryBeginOvercharge(out reason))
            {
                Log.Event(Tag, "crew Overcharge refused — " + reason);
                return false;
            }
            NoteSpecial(chargeAt);
            Log.Event(Tag, "crew Overcharge — turret x"
                + BoarderProtocol.OverchargeTurretRateMul.ToString("0.##")
                + ", gun x" + BoarderProtocol.OverchargeSelfFireMul.ToString("0.##")
                + " charge=" + _state.Charge.ToString("0")
                + "/" + _state.ChargeMax.ToString("0"));
            return true;
        }

        void NoteDeploy()
        {
            if (_abilities != null) _abilities.NoteMachineDeploy("turret");
        }

        void NoteSpecial(float charge)
        {
            if (_abilities != null) _abilities.NoteTurretSpecial(charge);
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
