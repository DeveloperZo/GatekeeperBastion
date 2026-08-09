using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): terminal end screens and emergency exit.</summary>
    public sealed partial class HudController
    {
        /// <summary>M27 lose-screen framing: who won, not only that the Run ended.</summary>
        public const string LoseFraming = "thrown off the Bastion";

        /// <summary>M27 clear-screen framing: the escape half of a strip.</summary>
        public const string ClearFraming = "out through the Hatch";

        void PushLose(GameController gc, RunSim sim)
        {
            _view.HideAll();
            _view.SetEndVisible(true);
            _view.SetShortcutVisible(true);

            var report = sim.LastLossReport;
            string cause = report.cause == "player" ? "YOU DIED"
                         : report.cause == "hatch" ? "HATCH DESTROYED"
                         : Player != null && Player.IsDead ? "YOU DIED" : "RUN ENDED";
            long net = report.grossEarned - report.repairSpent;

            _view.SetEndScreen(
                cause,
                LoseFraming,
                $"earned this run: {report.grossEarned} — already banked",
                $"repair spent: {report.repairSpent}",
                $"net: {net}",
                $"banked total: {gc.Meta?.BankedSalvage ?? 0}",
                $"DEEPEST WAVE {DeepestShown(gc)}");

            PushEndScreenShortcut();
        }

        void PushSuccess(GameController gc, RunSim sim)
        {
            _view.HideAll();
            _view.SetEndVisible(true);
            _view.SetShortcutVisible(true);

            var report = sim.LastLossReport;
            long net = report.grossEarned - report.repairSpent;

            _view.SetEndScreen(
                "BASTION STRIPPED",
                $"{ClearFraming} — all {WaveDirector.MaxWaves} waves cleared",
                $"earned this run: {report.grossEarned} — already banked",
                $"repair spent: {report.repairSpent}",
                $"net: {net}",
                $"banked total: {gc.Meta?.BankedSalvage ?? 0}",
                $"DEEPEST WAVE {DeepestShown(gc)}");

            PushEndScreenShortcut();
        }

        static int DeepestShown(GameController gc)
        {
            int d = gc.Meta != null ? gc.Meta.DeepestWaveReached : 1;
            return d < 1 ? 1 : d;
        }

        void PushEmergencyExit()
        {
            _view.HideAll();
            _view.SetEmergencyText("RUN LOST (HUD error — see the log)");
            _view.SetEmergencyVisible(true);
            PushEndScreenShortcut();
        }
    }
}
