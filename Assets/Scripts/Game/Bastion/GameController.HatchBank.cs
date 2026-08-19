using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Hatch-bank start and spawn restore.
    /// Split so Terminals.cs stays under the Game line cap.</summary>
    public sealed partial class GameController
    {
        /// <summary>True while either zone-1 terminal exists and has not completed this
        /// Run. Off entirely once the demo is off (Docs/Demo/1-first-terminal.md).</summary>
        public bool HatchBankStartBlocked
        {
            get
            {
                if (_layout == null) return false;
                MetaState meta = Meta;
                return FirstTerminal.HatchBankBlocked(
                    meta != null && meta.DemoActive,
                    _layout.FindTerminal(FirstTerminal.Id) != null,
                    _layout.IsTerminalDone(FirstTerminal.Id),
                    _layout.FindTerminal(FirstTerminal.SecondId) != null,
                    _layout.IsTerminalDone(FirstTerminal.SecondId));
            }
        }

        bool HatchBankRefuse(string reason)
        {
            Log.Event("Hatch", "hatch-bank refused — " + reason);
            return false;
        }

        /// <summary>Prep T at the Hatch after E-select: start a Hatch-bank Wave.</summary>
        public bool StartHatchBankWave()
        {
            if (_net != null && _net.IsClientOnly)
                return HatchBankRefuse("client cannot start a Wave");
            if (Sim == null || RunIsOver)
                return HatchBankRefuse("run not live");
            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
                return HatchBankRefuse("not in Prep");
            if (!_armedHatch)
                return HatchBankRefuse("select intensity first");
            if (HatchBankStartBlocked)
            {
                Log.Event("Demo", "hatch-bank refused — finish the terminal");
                return false;
            }

            _armedHatch = false;
            if (Sim.TotalUnbanked <= 0L)
                return HatchBankRefuse("unbanked=0");
            if (Sim.HatchBankSlotsLeft <= 0)
                return HatchBankRefuse("no slots");
            if (Sim.AllWavesCleared)
                return HatchBankRefuse("all waves cleared");
            if (Sim.MasterSequenceLive)
                return HatchBankRefuse("Master Control sequence live");

            // Hatch is a scene root, so parent walk misses BastionLayout; use the authored ref.
            ClearPendingTerminal();
            BastionLayout layout = _layout;
            if (layout == null && _repair != null)
                layout = _repair.GetComponentInParent<BastionLayout>();
            if (layout != null)
                layout.ActivateHatchBankSpawns();
            else
                Log.Warn(Tag, "hatch-bank — no BastionLayout; spawn registry unchanged");
            _hatchBankLayout = layout;

            int intensity = Sim.Siphon.Level;
            long juiced = Sim.JuiceSpentThisPull;
            Sim.TriggerDraw(WaveMode.HatchBank);
            if (Sim.Assault != AssaultState.Assault)
            {
                RestoreHatchBankSpawns();
                return HatchBankRefuse("draw did not start");
            }

            Log.Event("Hatch", "hatch-bank started wave " + Sim.CurrentWaveIndex
                + " carry=" + Sim.HatchCarry
                + " convert=" + Sim.HatchConvertMultiplier.ToString("0.00")
                + " unbanked=" + Sim.TotalUnbanked
                + " slots=" + Sim.HatchBankSlotsLeft
                + " intensity=" + intensity
                + " juiced=" + juiced);
            return true;
        }

        void OnMasterHatchHalfStarted()
        {
            BastionLayout layout = _layout;
            if (layout == null && _masterTerminal != null)
                layout = _masterTerminal.Layout;
            if (layout != null)
            {
                if (_masterTerminal != null)
                    _masterTerminal.SetUnlockedSpawnsActive(false);
                layout.SetControlRoomSealed(false);
                layout.ActivateHatchBankSpawns();
                _hatchBankLayout = layout;
            }
            else
                Log.Warn(Tag, "master-control hatch half — no BastionLayout");
        }

        /// <summary>A Hatch-bank window was survived (RunSim.BankWindowSurvived). This is the
        /// only door-opening path in the game: the graph opens every eligible door, the ids are
        /// snapshotted so a Docked return finds them open, and the door line is spoken with the
        /// dock offer queued behind it. A breached window ends the Run before this fires.</summary>
        void OnBankWindowSurvived()
        {
            BastionLayout layout = _layout;
            if (layout == null && _masterTerminal != null)
                layout = _masterTerminal.Layout;
            if (layout == null) return;

            if (layout.RecordBankSurvived(out string[] opened))
            {
                for (int i = 0; i < opened.Length; i++)
                    Log.Event("Demo", "door open " + opened[i] + " cause=bank");
                if (Meta != null)
                    Meta.SetOpenDoorIds(layout.CopyOpenDoorIds());
                if (opened.Length > 0 && Meta != null && Meta.DemoActive)
                    DemoVoice.PlayLocalAndBroadcast(
                        DoorLineFor(opened[0]), DemoVoiceMode.Lock);
            }

            if (Meta != null && Meta.DemoActive)
                DemoVoice.PlayLocalAndBroadcast(
                    DemoConversationIds.DockOption, DemoVoiceMode.Subtitle);
        }

        /// <summary>The line for a door that just opened. D3 deliberately has no id of its own —
        /// the Control Room line covers it (DemoConversationIds).</summary>
        static string DoorLineFor(string doorId)
        {
            if (doorId == "D2") return DemoConversationIds.DoorD2;
            if (doorId == "D3") return DemoConversationIds.ControlRoom;
            return DemoConversationIds.DoorD1;
        }

        void SnapshotHatchBankGraph()
        {
            if (Meta == null) return;
            BastionLayout layout = _layout;
            if (layout == null && _masterTerminal != null)
                layout = _masterTerminal.Layout;
            if (layout == null) return;
            Meta.SetCompletedTerminalIds(layout.CopyCompletedTerminalIds());
        }

        void RestoreHatchBankSpawns()
        {
            if (_hatchBankLayout == null) return;
            _hatchBankLayout.RestoreFieldSpawns();
            _hatchBankLayout = null;
        }
    }
}
