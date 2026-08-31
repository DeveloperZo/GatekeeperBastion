using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the Boarder's hold on FORGE's follower — deploy, redeploy,
    /// follow host, and the held-key EMP. Pairing reads live machines, not this type.</summary>
    public sealed class PlayerOutriderControl : MonoBehaviour
    {
        const string Tag = "Protocol";

        readonly OutriderState _state = new OutriderState();

        GameController _game;
        PlayerProtocolAbilities _abilities;
        OutriderObject _live;

        public OutriderState State => _state;

        NetVerbs Verbs
        {
            get
            {
                var net = _game != null ? _game.Net : null;
                return net != null ? net.LocalVerbs : null;
            }
        }

        public OutriderObject Live => _live != null && _live.IsAlive ? _live : null;

        public bool HasOutrider
        {
            get
            {
                var verbs = Verbs;
                return verbs != null ? verbs.ServerOutriderStanding : _state.IsLive;
            }
        }

        public bool IsExhausted
        {
            get
            {
                var verbs = Verbs;
                return verbs != null
                    ? verbs.ServerOutriderPhase == TurretPhase.Exhausted
                    : _state.Phase == TurretPhase.Exhausted;
            }
        }

        public bool IsEmpActive
        {
            get
            {
                var verbs = Verbs;
                return verbs != null
                    ? verbs.ServerOutriderPhase == TurretPhase.Special
                    : _state.EmpActive;
            }
        }

        public float Charge
        {
            get
            {
                var verbs = Verbs;
                return verbs != null ? verbs.ServerOutriderCharge : _state.Charge;
            }
        }

        public float ChargeMax => BoarderProtocol.OutriderChargeMax;

        bool _holding;
        float _hold;
        bool _holdFired;

        public void CancelHold()
        {
            _holding = false;
            _holdFired = false;
            _hold = 0f;
        }

        public void TickPersonalBinding(float dt, bool paused, ref BoarderAbilityState personal)
        {
            if (paused)
            {
                CancelHold();
                return;
            }

            bool held = Controls.Held(ControlId.PersonalAbility);
            if (held)
            {
                if (!_holding)
                {
                    _holding = true;
                    _hold = 0f;
                    _holdFired = false;
                }
                _hold += dt;
                if (!_holdFired && _hold >= BoarderProtocol.OutriderHoldThresholdSeconds)
                {
                    _holdFired = true;
                    if (!HasOutrider)
                        Log.Event(Tag, "self EMP refused — no outrider standing");
                    else if (!IsExhausted)
                        TryEmp();
                }
                return;
            }

            if (!_holding) return;
            _holding = false;
            if (!_holdFired) TapPersonal(ref personal);
        }

        void TapPersonal(ref BoarderAbilityState personal)
        {
            float ready = personal.SecondsSatReady;
            if (IsExhausted)
            {
                Clear();
                if (personal.IsReady)
                    personal.TrySpendInstant(BoarderProtocol.Cooldown(BoarderAbilityId.Emp));
                Log.Event(Tag, "self Outrider — exhausted outrider despawned sat-ready="
                    + ready.ToString("0.##"));
                return;
            }
            if (!personal.IsReady)
            {
                Log.Event(Tag, "self refused — " + personal.CooldownRemaining.ToString("0.#") + "s left");
                return;
            }
            if (!personal.TrySpendInstant(BoarderProtocol.Cooldown(BoarderAbilityId.Emp))) return;
            Deploy(ready);
        }

        public void Bind(GameController game)
        {
            _game = game;
            _abilities = GetComponent<PlayerProtocolAbilities>();
            _state.ResetForRun();
        }

        public bool Deploy(float satReady)
        {
            Vector3 at = transform.position;
            var verbs = Verbs;
            if (verbs != null)
            {
                verbs.RequestOutrider(at, true);
                NoteDeploy();
                Log.Event(Tag, "self Outrider — requested sat-ready=" + satReady.ToString("0.##"));
                return true;
            }

            string reason;
            if (!_state.TryPlace(at.x, at.y, at.z, out reason))
            {
                Log.Event(Tag, "self Outrider refused — " + reason);
                return false;
            }
            if (_live == null)
                _live = OutriderObject.Spawn(at, _state, _game, transform);
            else
                _live.transform.position = at;
            NoteDeploy();
            Log.Event(Tag, "self Outrider — standing charge="
                + _state.Charge.ToString("0") + "/" + _state.ChargeMax.ToString("0")
                + " sat-ready=" + satReady.ToString("0.##"));
            return true;
        }

        public bool TryEmp()
        {
            var verbs = Verbs;
            if (verbs != null)
            {
                verbs.RequestEmp(true);
                NoteSpecial(Charge);
                return true;
            }
            string reason;
            float chargeAt = _state.Charge;
            if (!_state.TryBeginEmp(out reason))
            {
                Log.Event(Tag, "self EMP refused — " + reason);
                return false;
            }
            NoteSpecial(chargeAt);
            int caught = _live != null ? _live.ApplyEmpBlast() : 0;
            Log.Event(Tag, "self EMP — " + caught + " gatekeeper(s) caught, root "
                + BoarderProtocol.EmpRootSeconds.ToString("0.##") + "s charge="
                + chargeAt.ToString("0") + "/" + _state.ChargeMax.ToString("0"));
            return true;
        }

        void NoteDeploy()
        {
            if (_abilities != null) _abilities.NoteMachineDeploy("outrider");
        }

        void NoteSpecial(float charge)
        {
            if (_abilities != null) _abilities.NoteOutriderSpecial(charge);
        }

        public void Clear()
        {
            var verbs = Verbs;
            if (verbs != null && verbs.ServerOutriderStanding)
                verbs.RequestOutrider(Vector3.zero, false);
            if (_live != null) _live.Despawn();
            _live = null;
            _state.Clear();
        }

        void OnDisable()
        {
            Clear();
        }
    }
}
