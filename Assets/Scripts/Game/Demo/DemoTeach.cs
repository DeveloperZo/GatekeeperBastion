using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the in-Bastion teaching lines (beats 1, 2 and 5) — the seven
    /// ids that were authored in `Docs/Demo - Single/` and sat dark because nothing called
    /// them. Driven by look-at (Interactor.Current) and sim state, latched exactly as the
    /// beat tables author: per-Run for the orientation lines, per-save for the juice teach
    /// (RealmState.DemoJuiceTaught), per-window for the fight subtitle. GameController adds
    /// this on a demo-active run and never on anything else — every early-out below is
    /// belt-and-braces, not the gate.</summary>
    public sealed class DemoTeach : MonoBehaviour
    {
        /// <summary>Beat 1: quiet Prep this long with no field look plays demo.explore.</summary>
        const float ExploreSeconds = 45f;

        GameController _gc;
        Interactor _interactor;
        float _exploreQuiet;
        bool _exploreDone;       // per Run
        bool _terminalLookDone;  // per Run
        bool _afterTerminalDone; // per Run
        bool _hatchFightDone;    // per window
        bool _repeatDone;        // per Run

        public void Bind(GameController gc)
        {
            _gc = gc;
        }

        void Update()
        {
            var gc = _gc;
            if (gc == null || gc.Sim == null) return;
            var m = gc.Meta;
            if (m == null || !m.DemoActive) return;
            var sim = gc.Sim;
            if (sim.Phase != RunPhase.Tethered) return;

            ResolveInteractor();
            var target = _interactor != null ? _interactor.Current : null;
            bool atTerminal = target is BastionTerminal;
            bool atHatch = target is RepairInteractable;

            // Beat 5: the loop restated, once per Run after the first, first Prep frame.
            if (!_repeatDone && sim.Assault == AssaultState.Prep
                && (m.TotalRuns > 1 || m.PodEjections > 0))
            {
                _repeatDone = true;
                DemoVoice.Play(DemoConversationIds.RepeatUnit, DemoVoiceMode.Subtitle);
            }

            // Beat 1: a player lost in the room for 45 quiet seconds gets pointed at a T.
            if (!_exploreDone)
            {
                if (atTerminal || _terminalLookDone)
                    _exploreDone = true;
                else if (sim.Assault == AssaultState.Prep)
                {
                    _exploreQuiet += Time.deltaTime;
                    if (_exploreQuiet >= ExploreSeconds)
                    {
                        _exploreDone = true;
                        DemoVoice.Play(DemoConversationIds.Explore, DemoVoiceMode.Subtitle);
                    }
                }
            }

            // Beat 1: the first field look this Run teaches E/T and who the Wave hunts.
            if (atTerminal && !_terminalLookDone)
            {
                _terminalLookDone = true;
                _exploreDone = true;
                DemoVoice.Play(DemoConversationIds.TerminalLook, DemoVoiceMode.Subtitle);
            }

            // Beat 1: the first field look with Unbanked to spend teaches the Ante. Once per
            // save — a heard-once line must never replay verbatim.
            if (atTerminal && !m.DemoJuiceTaught && sim.UnbankedOf(0) > 0L
                && sim.Assault == AssaultState.Prep)
            {
                m.DemoJuiceTaught = true;
                GameSession.Save();
                DemoVoice.Play(DemoConversationIds.JuiceTeach, DemoVoiceMode.Subtitle);
            }

            // Beat 1→2: the Hatch look-at once the zone is cleared. It says the zone is done
            // and the next wave starts back here. The arm-side Convert teach that used to
            // ride this surface is gone with the Convert itself (Zo, 2026-08-25) — kills bank
            // as they land, so there is no pile waiting at the Hatch to explain.
            if (atHatch && sim.Assault == AssaultState.Prep && sim.WavesSinceBank > 0
                && !_afterTerminalDone)
            {
                _afterTerminalDone = true;
                DemoVoice.Play(DemoConversationIds.AfterTerminal, DemoVoiceMode.Subtitle);
            }

            // Beat 2: the window is live — subtitle, fight input stays free. Re-arms per
            // window so a second Convert this Run gets its warning too.
            if (sim.Mode == WaveMode.HatchBank && sim.Assault == AssaultState.Assault)
            {
                if (!_hatchFightDone)
                {
                    _hatchFightDone = true;
                    DemoVoice.Play(DemoConversationIds.HatchFight, DemoVoiceMode.Subtitle);
                }
            }
            else
            {
                _hatchFightDone = false;
            }
        }

        void ResolveInteractor()
        {
            if (_interactor != null) return;
            var local = PlayerRegistry.Local;
            if (local == null) return;
            _interactor = local.GetComponentInChildren<Interactor>(true);
        }
    }
}
