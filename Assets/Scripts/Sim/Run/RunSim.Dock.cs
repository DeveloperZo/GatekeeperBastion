using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Ending a Run at the Hatch by choice. A survived Hatch-bank window opens a
    /// window in which held Interact ends the Run Docked instead of Lost; triggering the next
    /// Wave closes it. A breached window ends the Run before any of this fires, which is what
    /// makes "a failed bank never unlocks and never lets you dock" true by construction rather
    /// than by a check.</summary>
    public sealed partial class RunSim
    {
        /// <summary>The run-end cause written into RunReport when the player walks home.</summary>
        public const string DockedCause = "docked";

        /// <summary>True from a survived Hatch-bank window until the next Wave is triggered:
        /// the window in which DockRun is legal. Cleared by LaunchWave and by any run end.</summary>
        public bool DockOffered { get; private set; }

        /// <summary>Raised once per Hatch-bank window the Run survived, immediately after
        /// AssaultEnded. This is what opens doors. Not raised for Master Control's Hatch half,
        /// which ends the Run Escaped instead.</summary>
        public event Action BankWindowSurvived;

        /// <summary>Called from NotifyAssaultCleared once the assault is back in Prep.</summary>
        void OfferDockIfBankSurvived(bool hatchBank)
        {
            if (!hatchBank) return;
            if (Phase != RunPhase.Tethered) return;
            // Finale access is refused here, not unguarded: a Docked end is unreachable
            // for as long as ANY part of Master Control is live, so EndDemo never has to
            // filter it. Widened from the old Hatch-half test when the finale stopped having
            // halves — walking home mid-finale must not be a way to dodge the elite.
            if (MasterSequenceLive) return;
            DockOffered = true;
            // Zone 2 RESOLVED opens the demo's Primary rack — survived here, or lost
            // in EndRun's twin write. One flag, either outcome (Zo, 2026-08-25): the
            // rack rewards having ANSWERED zone 2, not specifically having bled there.
            if (_meta != null && _meta.DemoActive
                && DemoZones.WaveIsIn(Zone2, CurrentWaveIndex))
                _meta.DemoZone2BankLost = true;
            var survived = BankWindowSurvived;
            if (survived != null) survived();
        }

        /// <summary>A zone's terminal was shut off: the player may walk home from the Hatch.
        /// <para>THE DEMO HAD NO WAY HOME (Zo, 2026-08-28: "I didnt notice any call to action at
        /// hatch to come back to ship after 1st terminal is completed"). There was no call to
        /// action because there was no action. <see cref="OfferDockIfBankSurvived"/> opens the
        /// dock only after a surviving HATCH-BANK window, and Convert was struck from the demo
        /// on 2026-08-25 — so <see cref="DockOffered"/> could never become true and
        /// RepairInteractable's "[E] Dock — return to Ship" prompt could never be reached. The
        /// only exits left were dying, abandoning, or running the whole spine to Master Control.
        /// This is the same shape as the door bug the Convert removal caused: one beat was the
        /// sole gate on something the beat did not own.</para>
        /// <para>Completing a zone is the honest trigger. It is the moment the loop already
        /// treats as a resting point — the carry has just banked in ShutOffFieldWave, the door
        /// has opened, and the choice is genuinely "press on or go home". LaunchWave still
        /// clears the offer, so starting the next zone is the push-on answer exactly as it was
        /// for the bank window.</para></summary>
        void OfferDockAfterZoneShutoff()
        {
            if (Phase != RunPhase.Tethered) return;
            if (MasterSequenceLive) return;   // the finale ends Escaped, at its own terminal
            if (DockOffered) return;
            DockOffered = true;
            Log.Event(Tag, "dock offered — the zone is closed, the Hatch will take you home");
        }

        /// <summary>Triggering a Wave is the push-on answer: the offer is gone from here.</summary>
        void ClearDockOffer() => DockOffered = false;

        /// <summary>
        /// The player walked home: held Interact at the Hatch inside a survived bank window.
        /// Terminal Returned, NOT Lost — PodEjections does not move, so this never pays the
        /// first-loss unlock. Leftover Unbanked is wiped exactly as on any other run end; what
        /// survives is what was already banked. Refused once the next Wave is triggered.
        /// </summary>
        public bool DockRun()
        {
            if (Phase != RunPhase.Tethered)
            {
                Log.Info(Tag, "DockRun ignored in phase " + Phase);
                return false;
            }
            if (!DockOffered)
            {
                Log.Info(Tag, "DockRun refused — no survived bank window is open");
                return false;
            }
            EndRun(DockedCause);
            return true;
        }
    }
}
