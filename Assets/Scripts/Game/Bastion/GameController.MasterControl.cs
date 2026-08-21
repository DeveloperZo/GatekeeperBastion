using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Master Control start.
    /// Split so Terminals.cs stays under the Game line cap.</summary>
    public sealed partial class GameController
    {
        /// <summary>Prep T at Master Control after E-select: start the two-phase win sequence.</summary>
        public bool StartMasterControl(BastionTerminal terminal)
        {
            if (terminal == null)
            {
                Log.Warn(Tag, "StartMasterControl refused — no terminal");
                return false;
            }

            if (_net != null && _net.IsClientOnly)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — client cannot start Master Control");
                return false;
            }

            if (terminal.Kind != BastionTerminalKind.Master)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — kind=" + terminal.Kind);
                return false;
            }

            if (Sim == null || RunIsOver)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — run not live");
                return false;
            }

            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — not in Prep");
                return false;
            }

            if (Sim.MasterSequenceLive)
            {
                Log.Event("Terminal", "master-control refused — sequence already live");
                return false;
            }

            if (!string.IsNullOrEmpty(_pendingTerminalId) || Sim.SequentialPending)
            {
                Log.Event("Terminal", "master-control refused — sequence already pending");
                return false;
            }

            if (_armedTerminalId != terminal.TerminalId)
            {
                Log.Event("Terminal", "master-control refused — select intensity first");
                return false;
            }

            ClearPendingTerminal();
            _armedTerminalId = null;
            _armedHatch = false;
            _masterTerminal = terminal;
            BastionLayout layout = terminal.Layout;
            if (layout == null)
                layout = _layout;
            if (layout != null)
                layout.SetControlRoomSealed(true);
            terminal.SetUnlockedSpawnsActive(true);

            int intensity = Sim.Siphon.Level;
            long juiced = Sim.JuiceSpentThisPull;
            if (!Sim.StartMasterControl())
            {
                if (layout != null)
                    layout.SetControlRoomSealed(false);
                terminal.SetUnlockedSpawnsActive(false);
                _masterTerminal = null;
                Log.Event("Terminal", "master-control refused — draw did not start");
                return false;
            }

            Log.Event("Demo", "master-control room fight");
            Log.Event("Terminal", "master-control started terminalWaves=" + MasterControl.TerminalWaves
                + " hatchWaves=" + MasterControl.HatchWaves
                + " intensity=" + intensity
                + " juiced=" + juiced);
            return true;
        }

        /// <summary>First Master look-at this Run: the Control Room line already played on
        /// D3, this one is the pull briefing. Once-per-Run because this component dies with
        /// the Bastion load. Party so a guest looking at the same terminal hears it.</summary>
        void NoticeMasterLook()
        {
            if (_masterLookPlayed) return;
            MetaState meta = Meta;
            if (meta == null || !meta.DemoActive) return;
            _masterLookPlayed = true;
            DemoVoice.PlayLocalAndBroadcast(
                DemoConversationIds.MasterControl, DemoVoiceMode.Lock);
        }
    }
}
