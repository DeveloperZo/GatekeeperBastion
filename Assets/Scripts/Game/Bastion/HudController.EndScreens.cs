using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): terminal end screens and emergency exit.
    /// Called once on phase enter — not every frame.</summary>
    public sealed partial class HudController
    {
        public const string LoseFraming = "thrown off the Bastion";
        public const string ClearFraming = "out through the Hatch";

        void PushLose(GameController gc, RunSim sim)
        {
            var report = sim.LastRunReport;
            string cause = report.cause == "player" ? "YOU DIED"
                         : report.cause == "hatch" ? "HATCH DESTROYED"
                         : Player != null && Player.IsDead ? "YOU DIED" : "RUN ENDED";
            long net = report.grossEarned - report.repairSpent;

            _view.SetEndScreen(
                cause,
                LoseFraming,
                "earned this run: " + report.grossEarned + " — already banked",
                "repair spent: " + report.repairSpent,
                "net: " + net,
                "banked total: " + (gc.Meta != null ? gc.Meta.BankedSalvage : 0),
                "DEEPEST WAVE " + DeepestShown(gc));

            PushEndScreenShortcut();
        }

        void PushSuccess(GameController gc, RunSim sim)
        {
            var report = sim.LastRunReport;
            long net = report.grossEarned - report.repairSpent;

            _view.SetEndScreen(
                "BASTION STRIPPED",
                ClearFraming + " — all " + WaveDirector.MaxWaves + " waves cleared",
                "earned this run: " + report.grossEarned + " — already banked",
                "repair spent: " + report.repairSpent,
                "net: " + net,
                "banked total: " + (gc.Meta != null ? gc.Meta.BankedSalvage : 0),
                "DEEPEST WAVE " + DeepestShown(gc));

            PushEndScreenShortcut();
        }

        static int DeepestShown(GameController gc)
        {
            int d = gc.Meta != null ? gc.Meta.DeepestWaveReached : 1;
            return d < 1 ? 1 : d;
        }

        void PushEmergencyExit()
        {
            _view.SetEmergencyText("RUN LOST (HUD error — see the log)");
            _view.ShowEmergency();
            PushEndScreenShortcut();
        }
    }
}
