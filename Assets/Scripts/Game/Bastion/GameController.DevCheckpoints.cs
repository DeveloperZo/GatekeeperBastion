using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Dev demo checkpoints — jump the session
    /// to a demo beat instead of replaying every loop to reach it (F12, then a digit). Always
    /// compiled, like the rest of the Dev command surface. Digits 1, 3, 5 adopt a constructed
    /// session-only MetaState and reload the Ship; digits 2, 4, 6, 7 load the Bastion at the
    /// fight with the authored Unbanked. Digit 8 resets the live Bastion's terminals and doors.
    /// Salvage numbers come from <see cref="HatchBank.BankCap"/> / 
    /// <see cref="HatchBank.MasterTerminalUnbanked"/>, not authored aims.</summary>
    public sealed partial class GameController
    {
        const string TerminalFourId = "T4";
        const string TerminalFiveId = "T5";
        const string DoorThreeId = "D3";

        /// <summary>Overlay rows for the F12 digit prompt — DevConsole draws exactly these.
        /// Last row is the live reset, not a constructed state.</summary>
        public static readonly string[] DemoCheckpointLabels = BuildDemoCheckpointLabels();

        static string[] BuildDemoCheckpointLabels()
        {
            long zoneOne = HatchBank.BankCap(2);
            long zoneTwo = HatchBank.BankCap(5) - HatchBank.BankCap(2);
            long master = HatchBank.MasterTerminalUnbanked();
            return new[]
            {
                "1  start — beat 0 done, first EMBARK next",
                "2  post zone-1 terminals — Hatch, " + zoneOne + " unbanked",
                "3  post first Hatch-bank — Ship, wheel opens",
                "4  post zone-2 terminals — Hatch, " + zoneTwo + " unbanked",
                "5  post second Hatch-bank — D2 open, Primary, Control Room next",
                "6  Master Control — Control Room, pull ready",
                "7  finale Hatch-bank — elite window, " + master + " unbanked",
                "8  reset terminals + doors (live Bastion run)",
            };
        }

        /// <summary>True for digits that load the Bastion and apply a live fight, not the Ship.</summary>
        public static bool IsDemoFightCheckpoint(int digit)
        {
            return digit == 2 || digit == 4 || digit == 6 || digit == 7;
        }

        /// <summary>F12 + digit 1–7: build a state and load Ship or Bastion. Digit 8 (the live
        /// reset) is DevConsole's — it holds the sanctioned GameController handle.</summary>
        public static void LoadDemoCheckpoint(int digit)
        {
            var live = NetSession.FindLive();
            if (live != null && live.Active)
            {
                Log.Event("Dev", "checkpoint refused — solo tool; leave the co-op session first");
                return;
            }

            GameSession.NotifyDevAction();
            MetaState state = BuildDemoCheckpoint(digit);
            if (state == null)
            {
                Log.Event("Dev", $"checkpoint refused — unknown digit {digit}");
                return;
            }
            GameSession.AdoptDevState(state);
            bool fight = IsDemoFightCheckpoint(digit);
            GameSession.PendingDemoFight = fight ? digit : 0;
            GameSession.PendingStartWave = 1;
            string dest = fight ? "Bastion" : "Ship";
            Log.Event("Dev", $"demo checkpoint {digit} — {DemoCheckpointLabels[digit - 1]}; loading the {dest}");
            bool loaded = fight ? SceneFlow.LoadBastion() : SceneFlow.LoadShip();
            if (!loaded)
                Log.Warn(Tag, "checkpoint adopted but the " + dest + " scene failed to load.");
        }

        /// <summary>A fresh MetaState at the named demo point. Public for the EditMode pins;
        /// null on an unknown digit. Salvage moves through Earn/Spend so the ledger balances,
        /// and every gate reads exactly what the beats say it reads. Digits 2/4/6/7 carry the
        /// completed-terminal snapshot the Bastion fight restore reads; Unbanked itself is
        /// seeded on the live Run, not in Meta.</summary>
        public static MetaState BuildDemoCheckpoint(int digit)
        {
            if (digit < 1 || digit > 7) return null;

            var m = MetaState.CreateNew();
            ApplyBeatZero(m);
            if (digit == 1) return m;

            if (digit == 2)
            {
                ApplyZoneOneTerminalsDone(m);
                return m;
            }

            ApplyFirstReturn(m);
            if (digit == 3) return m;

            ApplyDoorOneOpen(m);
            if (digit == 4)
            {
                ApplyZoneTwoTerminalsDone(m);
                return m;
            }

            ApplyDoorTwoOpen(m);
            if (digit == 5) return m;

            ApplyControlRoomReady(m);
            return m;
        }

        /// <summary>D1 open, shop spent, zone 2's terminal still live. PlayMode uses this
        /// to stand at the mouth of zone 2; it is not an F12 digit (that list is the seven
        /// demo beats).</summary>
        public static MetaState BuildZoneTwoReadyCheckpoint()
        {
            var m = MetaState.CreateNew();
            ApplyBeatZero(m);
            ApplyFirstReturn(m);
            ApplyDoorOneOpen(m);
            return m;
        }

        static void ApplyBeatZero(MetaState m)
        {
            m.DemoCapsuleOpened = true;
            m.DemoNavPowered = true;
            m.DemoArmoryPowered = true;
            m.DemoWeaponGranted = true;
            m.DemoShipFightDone = true;
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.SetEquipped(WeaponId.Mk1Sidearm);
        }

        static void ApplyZoneOneTerminalsDone(MetaState m)
        {
            m.DeepestWaveReached = FirstTerminal.ZoneOneEndWave;
            m.SetZoneDeepest(FirstTerminal.ZoneOneNumber, FirstTerminal.ZoneOneEndWave);
            m.SetCompletedTerminalIds(new[] { FirstTerminal.Id, FirstTerminal.SecondId });
        }

        static void ApplyFirstReturn(MetaState m)
        {
            m.TotalRuns = 1;
            m.PodEjections = 1;
            m.HatchConversions = 1;
            m.EarnSalvage(500);
            m.DeepestWaveReached = FirstTerminal.ZoneOneEndWave;
            m.SetZoneDeepest(FirstTerminal.ZoneOneNumber, FirstTerminal.ZoneOneEndWave);
        }

        static void ApplyDoorOneOpen(MetaState m)
        {
            m.DemoCrewMenuUnlocked = true;
            m.DemoWheelBuysLeft = 0;
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 2);
            long wheelSpend = WeaponTier.SpentToReach(2);
            m.EarnSalvage(wheelSpend - 450);
            m.SpendSalvage(wheelSpend);
            BoarderProtocol.TryClaim(m, BoarderProtocolId.Default);
            m.TotalRuns = 2;
            m.HatchConversions = 2;
            m.EarnSalvage(350);
            m.SetCompletedTerminalIds(new[] { FirstTerminal.Id, FirstTerminal.SecondId });
            m.SetOpenDoorIds(new[] { "D1" });
        }

        static void ApplyZoneTwoTerminalsDone(MetaState m)
        {
            int zoneTwo = FirstTerminal.ZoneOneNumber + 1;
            m.DeepestWaveReached = WaveDirector.MaxWaves;
            m.SetZoneDeepest(zoneTwo, WaveDirector.MaxWaves);
            m.SetCompletedTerminalIds(new[]
            {
                FirstTerminal.Id, FirstTerminal.SecondId, DemoZones.TerminalIdAt(zoneTwo, 0),
            });
        }

        static void ApplyDoorTwoOpen(MetaState m)
        {
            int zoneTwo = FirstTerminal.ZoneOneNumber + 1;
            m.TotalRuns = 3;
            m.PodEjections = 2;
            m.HatchConversions = 3;
            m.DeepestWaveReached = WaveDirector.MaxWaves;
            m.SetZoneDeepest(zoneTwo, WaveDirector.MaxWaves);
            m.DemoZone2BankLost = true;
            m.DemoRungTwoOpened = true;
            long kitCost = WeaponCatalog.Def(WeaponId.PriC).cost + WeaponTier.SpentToReach(2);
            m.EarnSalvage(kitCost);
            m.SpendSalvage(kitCost);
            m.SetOwned(WeaponId.PriC, true);
            m.ReplacePointsFromFlatTier(WeaponId.PriC, 2);
            m.SetEquipped(WeaponId.PriC);
            m.DemoRungTwoBuysLeft = 1;
            m.EarnSalvage(400);
            m.SetCompletedTerminalIds(new[]
            {
                FirstTerminal.Id, FirstTerminal.SecondId, DemoZones.TerminalIdAt(zoneTwo, 0),
            });
            m.SetOpenDoorIds(new[] { DemoZones.DoorId(FirstTerminal.ZoneOneNumber), DemoZones.DoorId(zoneTwo) });
        }

        static void ApplyControlRoomReady(MetaState m)
        {
            int zoneTwo = FirstTerminal.ZoneOneNumber + 1;
            m.SetCompletedTerminalIds(new[]
            {
                FirstTerminal.Id, FirstTerminal.SecondId, DemoZones.TerminalIdAt(zoneTwo, 0),
                TerminalFourId, TerminalFiveId,
            });
            m.SetOpenDoorIds(new[]
            {
                DemoZones.DoorId(FirstTerminal.ZoneOneNumber),
                DemoZones.DoorId(zoneTwo),
                DoorThreeId,
            });
        }

        /// <summary>Digit 8: replay the zone loop in place — every terminal incomplete, every
        /// locked door shut, snapshots cleared, the next draw pointed back at wave 1. The
        /// saved DemoZoneDeepestWave is deliberately kept, so the retrigger offer re-appears
        /// at the deepest terminal exactly as after a real loss. Nothing about the Hatch-bank
        /// needs restoring — banking carries no per-Run budget
        /// (Docs/Demo - Single/5-repeat-to-control-room.md, Delete).</summary>
        public void ResetZoneProgress()
        {
            GameSession.NotifyDevAction();
            if (Sim == null || Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
            {
                Log.Warn(Tag, "ResetZoneProgress ignored — needs a live run in Prep.");
                return;
            }
            if (_net != null && _net.IsClientOnly)
            {
                Log.Warn(Tag, "ResetZoneProgress ignored — host owns the run.");
                return;
            }
            if (_layout == null)
            {
                Log.Warn(Tag, "ResetZoneProgress ignored — no BastionLayout.");
                return;
            }

            DespawnLiveGatekeepers();
            ClearPendingTerminal();
            ClearPendingRetrigger();
            RestoreHatchBankSpawns();
            _layout.ResetProgress();
            if (Meta != null)
            {
                Meta.SetCompletedTerminalIds(null);
                Meta.SetOpenDoorIds(null);
            }
            Sim.SeekWave(1);
            Log.Event("Dev", "zone progress reset — terminals re-armed, doors re-locked, "
                + "next draw is wave 1");
        }
    }
}
