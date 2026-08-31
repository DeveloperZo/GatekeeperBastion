using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): the protocol ability strip. Charge for
    /// FORGE's turret lives here, not on <see cref="PlayerAbility"/>'s weapon cooldown.</summary>
    public sealed partial class HudController
    {
        float _cacheCrewFill = -1f;
        float _cachePersonalFill = -1f;
        string _cacheCrewCdLabel;
        string _cachePersonalCdLabel;
        int _cacheHasProtocol = -1;
        int _cachePaired = -1;
        // Protocol IDENTITY, not just its cooldown state. Without these the cache key was
        // (has, both fills, both countdown labels) — every one of which is identical between
        // two protocols that are simply both ready, so granting a new protocol from the range
        // console repainted nothing and the bar kept the old names until an ability was fired
        // and moved a fill. Enums, so the comparison is free.
        BoarderAbilityId _cacheCrewAbility = BoarderAbilityId.None;
        BoarderAbilityId _cachePersonalAbility = BoarderAbilityId.None;
        BoarderAbilityId _cachePassive = BoarderAbilityId.None;

        void PushProtocolReadout()
        {
            if (_protocol == null)
            {
                ResolveRefs();
                if (_protocol == null)
                {
                    if (_cacheHasProtocol != 0)
                    {
                        _cacheHasProtocol = 0;
                        PushProtocolChrome(false);
                    }
                    return;
                }
            }

            int has = _protocol.HasProtocol ? 1 : 0;
            BoarderAbilityId crewAbility = _protocol.CrewAbility;
            BoarderAbilityId personalAbility = _protocol.PersonalAbility;
            BoarderAbilityId passive = _protocol.Passive;
            float crewFill;
            string crewCd;
            bool turretCharge = _protocol.TryCrewChargeSquare(out crewFill, out crewCd);
            if (!turretCharge)
            {
                crewFill = BoarderProtocol.OverlayFill(_protocol.CrewWindow, _protocol.CrewCooldown,
                    BoarderProtocol.Window(_protocol.CrewAbility),
                    BoarderProtocol.Cooldown(_protocol.CrewAbility));
                crewCd = CdLabel(_protocol.CrewWindow, _protocol.CrewCooldown);
            }
            float persFill;
            string persCd;
            bool outriderCharge = _protocol.TryPersonalChargeSquare(out persFill, out persCd);
            if (!outriderCharge)
            {
                persFill = BoarderProtocol.OverlayFill(_protocol.PersonalWindow, _protocol.PersonalCooldown,
                    BoarderProtocol.Window(_protocol.PersonalAbility),
                    BoarderProtocol.Cooldown(_protocol.PersonalAbility));
                persCd = CdLabel(_protocol.PersonalWindow, _protocol.PersonalCooldown);
            }
            int paired = _protocol.MachinesPaired ? 1 : 0;
            if (has == _cacheHasProtocol && crewFill == _cacheCrewFill && persFill == _cachePersonalFill
                && crewCd == _cacheCrewCdLabel && persCd == _cachePersonalCdLabel
                && crewAbility == _cacheCrewAbility && personalAbility == _cachePersonalAbility
                && passive == _cachePassive && paired == _cachePaired)
                return;
            _cacheHasProtocol = has;
            _cacheCrewAbility = crewAbility;
            _cachePersonalAbility = personalAbility;
            _cachePassive = passive;
            _cacheCrewFill = crewFill;
            _cachePersonalFill = persFill;
            _cacheCrewCdLabel = crewCd;
            _cachePersonalCdLabel = persCd;
            _cachePaired = paired;
            PushProtocolChrome(has != 0);
        }

        void PushProtocolChrome(bool equipped)
        {
            if (!equipped)
            {
                _view.SetProtocolLine("NO PROTOCOL");
                _view.SetProtocolAbilities(
                    "--", 0f, "",
                    Controls.DisplayOf(ControlId.PersonalAbility), "--", 0f, "", false,
                    Controls.DisplayOf(ControlId.CrewAbility), "--", 0f, "", false);
                return;
            }

            string proto = _protocol.ProtocolName;
            if (_protocol.MachinesPaired && !string.IsNullOrEmpty(proto))
                proto = proto + " (paired)";
            _view.SetProtocolLine(string.IsNullOrEmpty(proto) ? "NO PROTOCOL" : proto);
            string passive = _protocol.PassiveName;
            float crewFill;
            string crewCd;
            bool turretCharge = _protocol.TryCrewChargeSquare(out crewFill, out crewCd);
            if (!turretCharge)
            {
                crewFill = BoarderProtocol.OverlayFill(_protocol.CrewWindow, _protocol.CrewCooldown,
                    BoarderProtocol.Window(_protocol.CrewAbility),
                    BoarderProtocol.Cooldown(_protocol.CrewAbility));
                crewCd = CdLabel(_protocol.CrewWindow, _protocol.CrewCooldown);
            }
            float persFill;
            string persCd;
            bool outriderCharge = _protocol.TryPersonalChargeSquare(out persFill, out persCd);
            if (!outriderCharge)
            {
                persFill = BoarderProtocol.OverlayFill(_protocol.PersonalWindow, _protocol.PersonalCooldown,
                    BoarderProtocol.Window(_protocol.PersonalAbility),
                    BoarderProtocol.Cooldown(_protocol.PersonalAbility));
                persCd = CdLabel(_protocol.PersonalWindow, _protocol.PersonalCooldown);
            }
            bool paired = _protocol.MachinesPaired;
            _view.SetProtocolAbilities(
                string.IsNullOrEmpty(passive) ? "--" : passive, 0f, "",
                Controls.DisplayOf(ControlId.PersonalAbility),
                _protocol.PersonalName,
                persFill,
                persCd,
                outriderCharge || _protocol.PersonalWindow > 0f || paired,
                Controls.DisplayOf(ControlId.CrewAbility),
                _protocol.CrewName,
                crewFill,
                crewCd,
                turretCharge || _protocol.CrewWindow > 0f || paired);
        }

        static readonly string[] SecLabel = BuildSecLabel();

        static string[] BuildSecLabel()
        {
            var a = new string[61];
            for (int i = 0; i < a.Length; i++) a[i] = i.ToString();
            return a;
        }

        static string CdLabel(float window, float cooldown)
        {
            const float eps = 0.05f;
            if (window > eps) return SecAt(window);
            if (cooldown > eps) return SecAt(cooldown);
            return "";
        }

        static string SecAt(float seconds)
        {
            int n = (int)Mathf.Ceil(seconds);
            if (n < 0) n = 0;
            if (n < SecLabel.Length) return SecLabel[n];
            return n.ToString();
        }
    }
}
