using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Hatch-bank start and spawn restore.
    /// Split so Terminals.cs stays under the Game line cap.</summary>
    public sealed partial class GameController
    {
        /// <summary>The zone the Run is working right now: the first authored zone whose
        /// door is still shut. Zone N sits behind zone N-1's door, so an open door means
        /// that zone is behind the party. Derived rather than stored — door state already
        /// persists across Runs through the successful-bank snapshot, and a second copy of
        /// the same fact would be one more thing to keep true. Past the last authored zone
        /// this answers one above it, which the table reads as "no terminal gate": the
        /// finale is its own beat (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        int CurrentDemoZone
        {
            get
            {
                if (_layout == null) return 1;
                int zone = 1;
                while (zone <= DemoZones.ZoneCount && _layout.IsDoorOpen(DemoZones.DoorId(zone)))
                    zone++;
                return zone;
            }
        }

        /// <summary>True while the CURRENT zone still has a terminal in the layout that has
        /// not completed this Run — the bank waits on the zone in front of the player, not on
        /// zone 1 forever. Off entirely once the demo is off
        /// (Docs/Demo - Single/1-first-terminal.md, Docs/Demo - Single/5-repeat-to-control-room.md).
        /// Counted rather than collected: this getter is read from the Hatch prompt and the
        /// prep panel every frame and must not allocate.</summary>
        public bool HatchBankStartBlocked
        {
            get
            {
                if (_layout == null) return false;
                MetaState meta = Meta;
                int zone = CurrentDemoZone;
                int present = 0;
                int done = 0;
                int count = DemoZones.TerminalCount(zone);
                for (int i = 0; i < count; i++)
                {
                    string id = DemoZones.TerminalIdAt(zone, i);
                    if (string.IsNullOrEmpty(id)) continue;
                    // Unity fake-null: a destroyed terminal must read as absent, so ==, not is.
                    if (_layout.FindTerminal(id) == null) continue;
                    present++;
                    if (_layout.IsTerminalDone(id)) done++;
                }
                return DemoZones.HatchBankBlocked(
                    meta != null && meta.DemoActive, zone, present, done);
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
                return HatchBankRefuse("no boarding live");
            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
                return HatchBankRefuse("not in Prep");
            if (Ante.Enabled && !_armedHatch)
                return HatchBankRefuse("select the Ante first");
            if (!Ante.Enabled)
                _armedHatch = true;
            if (HatchBankStartBlocked)
            {
                Log.Event("Demo", "bank refused — zone incomplete");
                return false;
            }

            _armedHatch = false;
            if (Sim.TotalUnbanked <= 0L)
                return HatchBankRefuse("unbanked=0");
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
                + " intensity=" + intensity
                + " juiced=" + juiced);
            return true;
        }

        /// <summary>The finale's second half began — THE ELITE, in the control room.
        /// <para>This used to send the fight to the Hatch, because the elite used to be a
        /// Hatch-bank window there: it switched the control-room closets OFF and the Hatch
        /// closets ON. The elite became a control-room wave on 2026-08-28 and this handler was
        /// not moved with it, which would have delivered the boss 156 m from the room the
        /// player is standing in — or not at all. The room's closets stay armed; the Hatch's
        /// are not touched.</para>
        /// <para>The unseal stays: it is idempotent, and the room must be open whatever else
        /// happens, because the player has to walk out of it to the terminal.</para></summary>
        void OnMasterHatchHalfStarted()
        {
            BastionLayout layout = _layout;
            if (layout == null && _masterTerminal != null)
                layout = _masterTerminal.Layout;
            if (layout != null)
            {
                // The control-room closets keep delivering — the elite arrives where the fight
                // already is. Deliberately NOT ActivateHatchBankSpawns(): there is no Hatch
                // half any more, and arming those would open a second front behind the player.
                layout.SetControlRoomSealed(false);
            }
            else
                Log.Warn(Tag, "master-control elite — no BastionLayout");
            // Fires at the START of the beat: the elite is called, its escort lands first,
            // and the room has FinaleBeatSeconds of quiet before either does.
            Log.Event("Demo", "the elite is called — its escort lands first");
            DemoVoice.PlayLocalAndBroadcast(
                DemoConversationIds.HatchFinale, DemoVoiceMode.Subtitle);
        }

        /// <summary>A Hatch-bank window was survived (RunSim.BankWindowSurvived). One of TWO
        /// door-opening paths now — the terminal shutoff is the other, and the one the demo
        /// actually walks (Zo, 2026-08-25). Both go through OpenDoorsFor so they cannot drift.
        /// A breached window ends the Run before this fires.</summary>
        void OnBankWindowSurvived()
        {
            Log.Event("Demo", "bank window survived zone="
                + FirstTerminal.ZoneOfWave(Sim != null ? Sim.CurrentWaveIndex : 1));
            OpenDoorsFor("bank");

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
