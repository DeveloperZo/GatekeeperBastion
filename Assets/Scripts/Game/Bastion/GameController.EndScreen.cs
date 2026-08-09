using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY (partial of GameController): everything that happens once a run is
    /// over. Split out of GameController.cs when the success path landed and the file passed the
    /// 400-line cap — same class, same state, one concern per file (the repo already splits
    /// SceneBuilder this way).</summary>
    /// <remarks>
    /// A run ends in one of TWO terminal phases and this file refuses to care which:
    ///   * Lost  — death (player down or hatch destroyed).
    ///   * Escaped — all WaveDirector.MaxWaves waves cleared.
    /// Neither settles any currency: kills bank as they happen, so ending a run — well or
    /// badly — only stops the earning. All that is left here is the summary line and the exit.
    /// Both draw an end screen whose ONLY exit is ReturnToShip, so both need identical
    /// anti-soft-lock treatment. That symmetry is the whole point of IsTerminal: a win that
    /// leaves the cursor captured is exactly as unplayable as a loss that does, and it is worse
    /// in practice because the player has no dead body on screen to explain why nothing responds.
    /// </remarks>
    public sealed partial class GameController
    {
        /// <summary>Seconds on a terminal end screen with no input before we log a stuck-player warning.</summary>
        const float LoseScreenWatchdogSeconds = 30f;

        bool _leavingScene;
        float _loseScreenAge; // seconds spent on a terminal end screen (lose OR success) with no input
        bool _watchdogFired;

        /// <summary>Terminal = the run is over and the only way on is RETURN TO SHIP. Lost
        /// (died) and Escaped (all waves cleared) are treated identically by every exit path.</summary>
        static bool IsTerminal(RunPhase phase) => phase == RunPhase.Lost || phase == RunPhase.Escaped;

        /// <summary>Called from OnPhaseChanged the moment either terminal phase lands: free the
        /// cursor, arm the watchdog, write the run summary, persist.</summary>
        void EnterTerminalPhase(RunPhase phase)
        {
            _loseScreenAge = 0f;
            _watchdogFired = false;

            // THE soft-lock guard. Escaped is a WIN, and a win with a captured cursor is a player
            // who cannot click RETURN TO SHIP and has no fight left to explain why. Freed here for
            // both terminal phases (and re-asserted every frame in TickLoseScreen), and logged so
            // the transcript proves which state the cursor was actually left in.
            EnsureCursorFree();
            Log.Info(Tag, $"Terminal phase {phase}: cursor freed " +
                          $"(lock={Cursor.lockState}, visible={Cursor.visible}).");

            var r = Sim.LastLossReport;
            long net = r.grossEarned - r.repairSpent;
            if (phase == RunPhase.Escaped)
            {
                Log.Event("Run", $"BASTION STRIPPED — all {WaveDirector.MaxWaves} waves cleared, " +
                    $"gross {r.grossEarned}, repair {r.repairSpent}, net {net}, banked now {Meta.BankedSalvage}");
            }
            else
            {
                Log.Event("Run", $"RUN OVER after {Sim.RunElapsed:0}s on wave {Sim.CurrentWaveIndex} — " +
                    $"gross {r.grossEarned}, repair {r.repairSpent}, net {net}, banked now {Meta.BankedSalvage}");
            }
            GameSession.Save();
        }

        /// <summary>Either terminal phase (Lost lose screen or Escaped success screen): keep the
        /// cursor usable, accept the keyboard fallback, and warn if the player appears stuck.
        /// This is the only screen with no other way out — including for a WINNING player.</summary>
        void TickLoseScreen()
        {
            if (_leavingScene) return;

            // Every frame, not just on the transition: a station panel, a stray click, or another
            // controller re-locking the cursor would otherwise make the button unclickable.
            EnsureCursorFree();

            if (Controls.Down(ControlId.Confirm))
            {
                ReturnToShip(); // logs the single "returning to ship" line for both entry points
                return;
            }

            _loseScreenAge += Time.unscaledDeltaTime;
            if (Input.anyKeyDown || Input.GetAxisRaw("Mouse X") != 0f || Input.GetAxisRaw("Mouse Y") != 0f)
                _loseScreenAge = 0f;

            if (!_watchdogFired && _loseScreenAge >= LoseScreenWatchdogSeconds)
            {
                _watchdogFired = true;
                Log.Warn(Tag, $"End-screen watchdog ({Sim.Phase}): {LoseScreenWatchdogSeconds:0}s with no input. " +
                              $"cursor={Cursor.lockState}/visible={Cursor.visible}, " +
                              $"shipLoadable={SceneFlow.CanLoad(SceneFlow.Ship)}. " +
                              "Press ENTER to return to ship.");
            }
        }

        /// <summary>End-screen command: the purse was settled kill by kill, so there is nothing
        /// to reconcile here — persist and go home. Accepts
        /// BOTH terminal phases; gating it on Lost alone would strand a winning player on the
        /// success screen with a button that logs a warning and does nothing.</summary>
        public void ReturnToShip()
        {
            if (_leavingScene) return;
            if (Sim == null || !IsTerminal(Sim.Phase))
            {
                Log.Warn(Tag, $"ReturnToShip ignored (phase {(Sim == null ? "no-sim" : Sim.Phase.ToString())}).");
                return;
            }

            _leavingScene = true;
            if (Meta != null) Meta.PayloadLoadoutLocked = false;
            Log.Event("Ship", $"returning to the ship — {Meta?.BankedSalvage ?? 0} banked");
            GameSession.Save();

            if (SceneFlow.LoadShip()) return;

            // Guard trip: the load failed (scene not in build settings). Re-arm so the button and
            // the keyboard fallback keep working instead of soft-locking on a dead screen.
            _leavingScene = false;
            _loseScreenAge = 0f;
            _watchdogFired = false;
            Log.Error(Tag, "RETURN TO SHIP failed to load the Ship scene; staying on the end screen. " +
                           "Rebuild scenes from the GatekeeperBastion menu.");
        }

        /// <summary>Cursor free + visible. Idempotent and cheap; safe to call every frame.</summary>
        static void EnsureCursorFree()
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }
    }
}
