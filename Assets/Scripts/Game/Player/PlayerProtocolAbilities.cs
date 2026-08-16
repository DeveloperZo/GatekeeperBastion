using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Default Protocol actives on G/H. Ids pushed at Bind — never
    /// serialized (gotcha 1). Cooldowns run here; world effects go server-side via NetVerbs
    /// when hosted as a client.</summary>
    public sealed class PlayerProtocolAbilities : MonoBehaviour
    {
        const string Tag = "Protocol";

        GameController _game;
        FirstPersonController _fpc;
        PlayerHealth _health;
        BoarderProtocolId _protocol = BoarderProtocolId.Default;
        BoarderAbilityId _passive = BoarderAbilityId.Ghost;
        BoarderAbilityState _crew;
        BoarderAbilityState _personal;
        AnchorField _planted;
        bool _hosted;
        bool _loggedCrewExpiry;
        bool _loggedPersonalExpiry;

        public string CrewName => BoarderProtocol.DisplayName(BoarderProtocol.CrewAbilityOf(_protocol));
        public string PersonalName => BoarderProtocol.DisplayName(BoarderProtocol.PersonalAbilityOf(_protocol));
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
            _protocol = protocol == BoarderProtocolId.None ? BoarderProtocolId.Default : protocol;
            _passive = BoarderProtocol.OwnsPassive(_protocol, passive) ? passive : BoarderAbilityId.Ghost;
            if (_fpc == null) _fpc = GetComponent<FirstPersonController>();
            if (_health == null) _health = GetComponent<PlayerHealth>();
            float ghost = _passive == BoarderAbilityId.Ghost
                ? BoarderProtocol.Magnitude(BoarderAbilityId.Ghost) : 1f;
            if (_health != null) _health.ConfigureAcquisition(ghost);
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
                if (!_loggedCrewExpiry)
                    Log.Event(Tag, "Anchor window ended");
                _loggedCrewExpiry = false;
            }
            if (personalWasOpen && !_personal.WindowOpen)
            {
                if (_loggedPersonalExpiry)
                    Log.Event(Tag, "Bulkhead expired un-triggered");
                _loggedPersonalExpiry = false;
            }

            if (OptionsScreen.IsOpen || HudController.IsPauseOpen) return;
            if (Controls.Down(ControlId.CrewAbility)) OnCrew();
            if (Controls.Down(ControlId.PersonalAbility)) OnPersonal();
        }

        void OnCrew()
        {
            var id = BoarderProtocol.CrewAbilityOf(_protocol);
            if (_crew.WindowOpen)
            {
                float sat = 0f;
                _crew.EndWindowEarly();
                Unplant();
                Log.Event(Tag, "crew Anchor unplant sat-ready=" + sat.ToString("0.##"));
                SendAnchor(false);
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
            Log.Event(Tag, "crew Anchor sat-ready=" + ready.ToString("0.##"));
            SendAnchor(true);
        }

        void OnPersonal()
        {
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
            if (_health != null) _health.ArmBulkhead();
            Log.Event(Tag, "self Bulkhead sat-ready=" + ready.ToString("0.##"));
            SendBulkhead();
        }

        void Plant()
        {
            Unplant();
            if (_fpc != null) _fpc.Rooted = true;
            if (!ClientOnly())
                _planted = AnchorField.Plant(transform.position);
        }

        void Unplant()
        {
            if (_fpc != null) _fpc.Rooted = false;
            if (_planted == null) return;
            AnchorField.Uproot(_planted);
            _planted = null;
        }

        public void NotifyBulkheadConsumed()
        {
            _loggedPersonalExpiry = false;
            _personal.EndWindowEarly();
        }

        void SendAnchor(bool planting)
        {
            if (!ClientOnly()) return;
            var verbs = LocalVerbs();
            if (verbs == null) return;
            verbs.RequestAnchor(transform.position, planting);
        }

        void SendBulkhead()
        {
            if (!ClientOnly()) return;
            var verbs = LocalVerbs();
            if (verbs != null) verbs.RequestBulkhead();
        }

        bool ClientOnly()
        {
            var net = _game != null ? _game.Net : null;
            return net != null && net.IsClientOnly;
        }

        NetVerbs LocalVerbs()
        {
            var net = _game != null ? _game.Net : null;
            return net != null ? net.LocalVerbs : null;
        }
    }
}
