using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The state-keyed Ship standing after the first bank: **Spend Salvage at the
    /// ARMORY** while a wheel upgrade is still buyable, **EMBARK from the NAV MAP** once
    /// none remains (beat 3's re-key, Zo 2026-08-17 — replaces the third-refuse producer).
    /// Evaluated on Docked and after every buy / refuse / spend; the pure rule is
    /// <see cref="DemoProgress.NoBuyableUpgradeRemains"/>. Pre-first-bank standings stay
    /// DemoShipFight's and DemoShipDirector's.</summary>
    public static class DemoShipStanding
    {
        const string Tag = "Demo";
        public const string SpendLine = "Spend Salvage at the ARMORY";
        public const string EmbarkLine = "EMBARK from the NAV MAP";

        static bool _flipLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _flipLogged = false;
        }

        /// <summary>Re-read the state key and set the standing. No-op off the Ship, off the
        /// demo, or before the first bank. The flip to EMBARK is logged once per flip; the
        /// key is state, so a new buyable upgrade (an owned SEC-D's wheel coming into
        /// budget) honestly re-opens the ARMORY standing and re-arms the flip log.</summary>
        public static void Reevaluate()
        {
            var m = GameSession.Meta;
            if (m == null || !m.DemoActive) return;
            if (m.HatchConversions < 1) return;
            // Either hub — the tutorial runs on the co-op hull for a fresh realm, and the
            // post-first-bank objective never appeared there.
            if (!SceneFlow.IsShipHub(SceneFlow.ActiveSceneName)) return;

            if (DemoProgress.NoBuyableUpgradeRemains(m))
            {
                if (!_flipLogged)
                {
                    _flipLogged = true;
                    Log.Event(Tag, "exit standing set — no buyable upgrades remain");
                }
                DemoShipFight.SetStanding(EmbarkLine);
            }
            else
            {
                _flipLogged = false;
                DemoShipFight.SetStanding(SpendLine);
            }
        }
    }
}
