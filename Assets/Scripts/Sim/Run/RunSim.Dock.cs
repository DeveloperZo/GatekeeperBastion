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
            // while the Master Hatch half is live, so EndDemo never has to filter it.
            if (_masterPhase == MasterControlPhase.Hatch) return;
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
