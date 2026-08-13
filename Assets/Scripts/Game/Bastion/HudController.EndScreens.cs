using GatekeeperBastion.Sim;
using UnityEngine;

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
                "converted this run: " + report.grossEarned,
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
                "converted this run: " + report.grossEarned,
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

        void PushFromSummary(EndSummary s)
        {
            string earned = "converted this run: " + s.GrossEarned;
            string repair = "repair spent: " + s.RepairSpent;
            string net = "net: " + s.Net;
            string banked = "banked total: " + s.Banked;
            string deepest = "DEEPEST WAVE " + (s.DeepestWave < 1 ? 1 : s.DeepestWave);

            if (s.Kind == RunEndKind.Cleared)
            {
                _view.SetEndScreen(
                    "BASTION STRIPPED",
                    ClearFraming + " — all " + WaveDirector.MaxWaves + " waves cleared",
                    earned, repair, net, banked, deepest);
            }
            else
            {
                string cause = s.Kind == RunEndKind.Hatch ? "HATCH DESTROYED" : "YOU DIED";
                _view.SetEndScreen(
                    cause, LoseFraming, earned, repair, net, banked, deepest);
            }

            PushEndScreenShortcut();
        }

        bool TryPushClientEnd()
        {
            var net = _game != null ? _game.Net : null;
            if (net == null || !net.IsClientOnly) return false;
            var mirror = net.Mirror;
            if (mirror == null || !mirror.HasEnd) return false;

            if (!_endPresented)
            {
                _endPresented = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (_view != null) _view.ShowEnd();
                PushFromSummary(mirror.LastEnd);
            }
            PushEndScreenShortcut();
            return true;
        }

        void TickClientEndDismiss()
        {
            if (!_endPresented) return;
            var net = _game != null ? _game.Net : null;
            if (net == null || !net.IsClientOnly) return;
            if (!Controls.Down(ControlId.Confirm)) return;
            if (_game != null) _game.ReturnToShip();
        }

        void PushEmergencyExit()
        {
            _view.SetEmergencyText("RUN LOST (HUD error — see the log)");
            _view.ShowEmergency();
            PushEndScreenShortcut();
        }
    }
}
