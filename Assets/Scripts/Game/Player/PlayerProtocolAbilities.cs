using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: Default Protocol actives on G/H. Ids pushed at Bind — never
    /// serialized (gotcha 1). Cooldowns run here; Barrage plants go server-side via NetVerbs
    /// when a session is listening.</summary>
    public sealed class PlayerProtocolAbilities : MonoBehaviour
    {
        const string Tag = "Protocol";

        GameController _game;
        FirstPersonController _fpc;
        PlayerHealth _health;
        BoarderProtocolId _protocol = BoarderProtocolId.None;
        BoarderAbilityId _passive = BoarderAbilityId.None;
        BoarderAbilityState _crew;
        BoarderAbilityState _personal;
        BarrageField _planted;
        bool _hosted;
        bool _loggedCrewExpiry;
        bool _loggedPersonalExpiry;

        public bool HasProtocol => _protocol != BoarderProtocolId.None;
        public string ProtocolName => BoarderProtocol.ProtocolName(_protocol);
        public bool BarrageWindowOpen => _crew.WindowOpen;
        public bool RushOpen => _personal.WindowOpen;
        public BoarderAbilityId CrewAbility => BoarderProtocol.CrewAbilityOf(_protocol);
        public BoarderAbilityId PersonalAbility => BoarderProtocol.PersonalAbilityOf(_protocol);
        public string CrewName => BoarderProtocol.DisplayName(CrewAbility);
        public string PersonalName => BoarderProtocol.DisplayName(PersonalAbility);
        public string PassiveName => BoarderProtocol.DisplayName(_passive);
        public float CrewWindow => _crew.WindowRemaining;
        public float CrewCooldown => _crew.CooldownRemaining;
        public float PersonalWindow => _personal.WindowRemaining;
        public float PersonalCooldown => _personal.CooldownRemaining;
        public bool CrewReady => _crew.IsReady;
        public bool PersonalReady => _personal.IsReady;

        public void Bind(GameController game, BoarderProtocolId protocol, BoarderAbilityId passive)
        {
            _game = game;
            _protocol = protocol;
            _passive = BoarderProtocol.OwnsPassive(_protocol, passive) ? passive : BoarderAbilityId.None;
            if (_fpc == null) _fpc = GetComponent<FirstPersonController>();
            if (_health == null) _health = GetComponent<PlayerHealth>();
            if (_health != null) _health.ConfigureAcquisition(1f);
        }

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return;
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            bool crewWasOpen = _crew.WindowOpen;
            bool personalWasOpen = _personal.WindowOpen;
            _crew.Tick(dt);
            _personal.Tick(dt);
            if (crewWasOpen && !_crew.WindowOpen)
            {
                Unplant();
                if (_loggedCrewExpiry)
                    Log.Event(Tag, "crew Barrage window ended");
                _loggedCrewExpiry = false;
            }
            if (personalWasOpen && !_personal.WindowOpen)
            {
                if (_loggedPersonalExpiry)
                    Log.Event(Tag, "self Rush window ended");
                _loggedPersonalExpiry = false;
            }

            if (OptionsScreen.IsOpen || HudController.IsPauseOpen) return;
            if (Controls.Down(ControlId.CrewAbility)) OnCrew();
            if (Controls.Down(ControlId.PersonalAbility)) OnPersonal();
        }

        public void NotifyDeath()
        {
            if (_crew.WindowOpen)
            {
                _loggedCrewExpiry = false;
                _crew.EndWindowEarly();
                Unplant();
                Log.Event(Tag, "crew Barrage window ended");
            }
            else
            {
                Unplant();
            }
            if (_personal.WindowOpen)
            {
                _loggedPersonalExpiry = false;
                _personal.EndWindowEarly();
                Log.Event(Tag, "self Rush window ended");
            }
        }

        public void NotifyBulkheadConsumed()
        {
            _loggedPersonalExpiry = false;
            _personal.EndWindowEarly();
        }

        void OnCrew()
        {
            if (!HasProtocol)
            {
                Log.Event(Tag, "crew refused — no protocol");
                return;
            }
            var id = BoarderProtocol.CrewAbilityOf(_protocol);
            if (_crew.WindowOpen)
            {
                float sat = _crew.SecondsSatReady;
                _loggedCrewExpiry = false;
                _crew.EndWindowEarly();
                Unplant();
                Log.Event(Tag, "crew Barrage unplant sat-ready=" + sat.ToString("0.##"));
                return;
            }
            if (!_crew.IsReady)
            {
                Log.Event(Tag, "crew refused — " + _crew.CooldownRemaining.ToString("0.#") + "s left");
                return;
            }
            float ready = _crew.SecondsSatReady;
            if (!_crew.TryActivate(BoarderProtocol.Window(id), BoarderProtocol.Cooldown(id)))
                return;
            _loggedCrewExpiry = true;
            Plant();
            Log.Event(Tag, "crew Barrage sat-ready=" + ready.ToString("0.##"));
        }

        void OnPersonal()
        {
            if (!HasProtocol)
            {
                Log.Event(Tag, "self refused — no protocol");
                return;
            }
            var id = BoarderProtocol.PersonalAbilityOf(_protocol);
            if (_personal.WindowOpen)
            {
                Log.Event(Tag, "self refused — window open");
                return;
            }
            if (!_personal.IsReady)
            {
                Log.Event(Tag, "self refused — " + _personal.CooldownRemaining.ToString("0.#") + "s left");
                return;
            }
            float ready = _personal.SecondsSatReady;
            if (!_personal.TryActivate(BoarderProtocol.Window(id), BoarderProtocol.Cooldown(id)))
                return;
            _loggedPersonalExpiry = true;
            Log.Event(Tag, "self Rush sat-ready=" + ready.ToString("0.##"));
            var verbs = LocalVerbs();
            if (verbs != null) verbs.RequestBulkhead();
        }

        void Plant()
        {
            Unplant();
            if (_fpc != null) _fpc.Rooted = true;
            var verbs = LocalVerbs();
            if (verbs != null)
                verbs.RequestAnchor(transform.position, true);
            else
                _planted = BarrageField.Plant(transform.position);
        }

        void Unplant()
        {
            if (_fpc != null) _fpc.Rooted = false;
            var verbs = LocalVerbs();
            if (verbs != null)
            {
                verbs.RequestAnchor(transform.position, false);
                return;
            }
            if (_planted == null) return;
            BarrageField.Uproot(_planted);
            _planted = null;
        }

        NetVerbs LocalVerbs()
        {
            var net = _game != null ? _game.Net : null;
            return net != null ? net.LocalVerbs : null;
        }
    }
}
