using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Demo routing in front of the Ship stations: hidden stations (no prompt,
    /// no panel — 2026-08-18 revision), the NAV hold-repair, and the CREW tap-heal —
    /// everything that must run BEFORE OpenStationPanel while the demo gates hold. Pure
    /// allow/deny rules live in <see cref="DemoProgress"/>; this class is the Unity
    /// side. With the demo off every call falls through and the live stations are
    /// untouched. The MK-1 grant moved to <see cref="ShipTerminal"/>'s start.</summary>
    public static class DemoStationGate
    {
        const string Tag = "Demo";
        const float NavRepairHoldSeconds = 1.5f;

        static float _navHold;
        static int _navHoldFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _navHold = 0f;
            _navHoldFrame = -1;
        }

        /// <summary>Interact Down on a targeted station. True = consumed (no panel opens).
        /// ShipController calls this before TryOpenStationFromInteract.</summary>
        public static bool TryHandleInteractDown(StationTerminal station)
        {
            if (station == null) return false;
            var m = GameSession.Meta;
            if (m == null || !m.DemoActive) return false;

            if (station is MapStation)
            {
                // Dark NAV is the hold-repair target (TickHold); powered NAV opens no
                // picker until the ship is clear. The prompt is the answer — no log.
                return DemoProgress.NavVerdict(m) != DemoStationVerdict.Open;
            }

            if (station is ArmoryStation)
            {
                if (DemoProgress.ArmoryVerdict(m) == DemoStationVerdict.Open) return false;
                Log.Event(Tag, "station hidden ARMORY");
                return true; // hidden: silent to the player, never a panel
            }

            if (station is CrewQuartersStation)
            {
                var v = DemoProgress.CrewVerdict(m);
                if (v == DemoStationVerdict.Open) return false;
                if (v == DemoStationVerdict.Hidden)
                {
                    Log.Event(Tag, "station hidden CREW QUARTERS");
                    return true;
                }
                Heal(m);
                return true;
            }

            if (station is EngineeringStation)
            {
                Log.Event(Tag, "station hidden FABRICATION");
                return true;
            }

            return false;
        }

        /// <summary>Demo prompt for a targeted station: null for the live prompt, ""
        /// for a hidden station (no name on the crosshair — the HUD treats an empty
        /// prompt as nothing targeted). Strings are the beat 0 play-through table's,
        /// verbatim.</summary>
        public static string PromptOverride(StationTerminal station)
        {
            if (station == null) return null;
            var m = GameSession.Meta;
            if (m == null || !m.DemoActive) return null;

            if (station is MapStation)
            {
                var v = DemoProgress.NavVerdict(m);
                if (v == DemoStationVerdict.Dark)
                {
                    if (Time.frameCount - _navHoldFrame > 1) _navHold = 0f; // release resets
                    return _navHold > 0f
                        ? "[E] repairing NAV MAP"
                        : "[E] repair NAV MAP";
                }
                if (v == DemoStationVerdict.NoPanelYet)
                    return "[E] NAV MAP — clear the ship first";
                return null;
            }

            if (station is ArmoryStation)
            {
                if (DemoProgress.ArmoryVerdict(m) == DemoStationVerdict.Hidden) return "";
                return null;
            }

            if (station is CrewQuartersStation)
            {
                var v = DemoProgress.CrewVerdict(m);
                if (v == DemoStationVerdict.Hidden) return "";
                if (v == DemoStationVerdict.HealOnly)
                {
                    var player = PlayerRegistry.Local;
                    bool hurt = player != null && player.Hp < player.MaxHp - 0.5f;
                    return hurt
                        ? "[E] CREW QUARTERS — restore health"
                        : "[E] CREW QUARTERS — crew is rested";
                }
                return null;
            }

            if (station is EngineeringStation)
                return "";

            return null;
        }

        /// <summary>Held E on a station (StationTerminal.Interact forwards here). Only the
        /// dark NAV takes a hold: 1.5 s to power it, release resets.</summary>
        public static void TickHold(StationTerminal station, float dt)
        {
            if (!(station is MapStation)) return;
            var m = GameSession.Meta;
            if (m == null || !m.DemoActive) return;
            if (DemoProgress.NavVerdict(m) != DemoStationVerdict.Dark) return;

            if (Time.frameCount - _navHoldFrame > 1) _navHold = 0f; // release resets
            _navHoldFrame = Time.frameCount;
            _navHold += dt;
            if (_navHold < NavRepairHoldSeconds) return;

            _navHold = 0f;
            m.DemoNavPowered = true;
            GameSession.Save();
            Log.Event(Tag, "NAV powered");
            DemoVoice.Play(DemoConversationIds.NavRepair, DemoVoiceMode.Lock);
            DemoShipFight.SetStanding("Start the ship terminal");
        }

        /// <summary>CREW restores health on a visit for the rest of the demo — the panel
        /// unlocking folds the heal in, never drops it (beat 4). CrewQuartersStation calls
        /// this on open once the menu is live; the heal-only tap above calls it too.</summary>
        public static void HealOnVisit(MetaState m)
        {
            Heal(m);
        }

        static void Heal(MetaState m)
        {
            var player = PlayerRegistry.Local;
            if (player == null) return;
            if (player.Hp >= player.MaxHp - 0.01f) return; // rested — prompt says so
            float before = player.Hp;
            player.HealToFull();
            Log.Event(Tag, "CREW heal hp=" + before.ToString("0") + "→"
                + player.MaxHp.ToString("0"));
            // Post-first-bank the standing is state-keyed (DemoShipStanding); only the
            // beat 0 visit answers a heal with the EMBARK standing directly.
            if (m.DemoShipFightDone && m.HatchConversions < 1)
                DemoShipFight.SetStanding("EMBARK from the NAV MAP");
        }
    }
}
