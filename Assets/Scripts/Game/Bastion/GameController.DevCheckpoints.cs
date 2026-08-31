using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Dev demo checkpoints — jump the session
    /// to a demo beat instead of replaying every loop to reach it (F12, then a letter; rows
    /// are A..J so the beats never collide with the weapon-slot digits). Always compiled,
    /// like the rest of the Dev command surface. Beats A, C, E adopt a constructed
    /// session-only MetaState and reload the Ship; B, D, F, G, H load the Bastion at the
    /// fight with the authored Unbanked. I stocks a realm record and a Boarder kit in
    /// place, and J resets the live Bastion's terminals and doors. Internally a beat is
    /// still its 1-based row index — the letter is the key cap, not the identity.
    /// Salvage numbers come from <see cref="HatchBank.BankCap"/> /
    /// <see cref="HatchBank.MasterTerminalUnbanked"/>, not authored aims.</summary>
    public sealed partial class GameController
    {
        const string TerminalFiveId = "T5";

        /// <summary>Every authored zone terminal up to and including <paramref name="throughZone"/>,
        /// read from <see cref="DemoZones"/>. Checkpoints used to name ids literally, which is
        /// how "T4" survived the spine restructure that deleted it (2026-08-25).</summary>
        static string[] ZoneTerminalsThrough(int throughZone)
        {
            var ids = new System.Collections.Generic.List<string>();
            for (int zone = 1; zone <= throughZone && zone <= DemoZones.ZoneCount; zone++)
            {
                for (int i = 0; i < DemoZones.TerminalCount(zone); i++)
                {
                    string id = DemoZones.TerminalIdAt(zone, i);
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }
            return ids.ToArray();
        }

        /// <summary>Overlay rows for the F12 letter prompt — DevConsole draws exactly these
        /// and derives the key range (A..) from the array length. Last row is the live
        /// reset, not a constructed state.</summary>
        public static readonly string[] DemoCheckpointLabels = BuildDemoCheckpointLabels();

        static string[] BuildDemoCheckpointLabels()
        {
            long zoneOne = HatchBank.BankCap(2);
            long zoneTwo = HatchBank.BankCap(5) - HatchBank.BankCap(2);
            long master = HatchBank.MasterTerminalUnbanked();
            return new[]
            {
                "A  start — beat 0 done, first EMBARK next",
                "B  post zone-1 terminals — Hatch, " + zoneOne + " unbanked",
                "C  post first Convert — Ship, wheel opens",
                "D  post zone-2 terminals — Hatch, " + zoneTwo + " unbanked",
                "E  post second Convert — D3 open, Primary, Master next",
                "F  post zone-2 Convert — Hatch, both doors open, Master next",
                "G  Master Control — Hatch, pull ready (start at Hatch)",
                "H  finale Convert — elite window, " + master + " unbanked",
                "I  stock realm record + Boarder kit (in place, no reload)",
                "J  reset terminals + doors (live Bastion run)",
            };
        }

        /// <summary>The beat that fills a realm's record and a Boarder's kit in place.</summary>
        public const int StockDigit = 9;

        /// <summary>True for beats that load the Bastion and apply a live fight, not the Ship.</summary>
        public static bool IsDemoFightCheckpoint(int digit)
        {
            return digit == 2 || digit == 4 || digit == 6 || digit == 7 || digit == 8;
        }

        /// <summary>F12 + letter A–H: build a state and load Ship or Bastion. I stocks the
        /// standing session in place. The last row (the live reset) is DevConsole's — it
        /// holds the sanctioned GameController handle.</summary>
        public static void LoadDemoCheckpoint(int digit)
        {
            LoadDemoCheckpoint(digit, 0, coop: false);
        }

        /// <summary>Beat plus a loadout configuration (0 = the beat as authored; rows in
        /// <see cref="CheckpointConfigs"/>) plus the hull: F12 forces the solo pair
        /// (Bastion / Ship), F11 the co-op pair (Bastion_Coop / Ship - CoOp) — never
        /// resolved from the active scene. The overlay lands before the scene loads, so
        /// ApplyMetaEffects pushes the configured kit like any other save.</summary>
        public static void LoadDemoCheckpoint(int digit, int config, bool coop = false)
        {
            var live = NetSession.FindLive();
            if (live != null && live.Active)
            {
                Log.Event("Dev", "checkpoint refused — solo tool; leave the co-op session first");
                return;
            }

            // Stocking mutates the session you are standing in rather than building a new
            // one and reloading — the point is to watch a bare hull fill in, and a reload
            // would throw away the thing being looked at.
            if (digit == StockDigit)
            {
                StockRealmAndBoarder();
                return;
            }

            GameSession.NotifyDevAction();
            MetaState state = BuildDemoCheckpoint(digit);
            if (state == null)
            {
                Log.Event("Dev", $"checkpoint refused — unknown beat {digit}");
                return;
            }
            // The taint verdict is judged against the beat AS AUTHORED — before the overlay,
            // since the overlay is exactly the thing being judged (it mints its own kit into
            // LifetimeSalvageEarned, which would feed the affordability test its answer).
            bool configTaint = CheckpointConfigTainted(state, config);
            ApplyCheckpointConfig(state, config);
            GameSession.AdoptDevState(state);
            bool fight = IsDemoFightCheckpoint(digit);
            GameSession.PendingDemoFight = fight ? digit : 0;
            GameSession.PendingStartWave = 1;
            // Remembered for the run-summary Metric line: which beat and configuration this
            // session's state came from, so the dashboard can tell checkpoint play apart.
            GameSession.CheckpointBeat = digit;
            GameSession.CheckpointConfig = config;
            GameSession.CheckpointConfigTaint = configTaint;
            // One scene pair since the consolidation: solo and co-op checkpoints land the
            // same hull; the flag only records which surface asked.
            string dest = fight ? "Bastion" : "Ship";
            Log.Event("Dev", $"demo checkpoint {digit} config {config} "
                + (coop ? "coop — " : "solo — ")
                + $"{DemoCheckpointLabels[digit - 1]}; loading the {dest}");
            bool loaded = fight
                ? SceneFlow.LoadBastionCoop()
                : SceneFlow.LoadShipCoop();
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
            if (digit < 1 || digit > 8) return null;

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
            return m; // 7 and 8 share meta; the finale's elite pile is Unbanked on the Run
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
            // One definition of "the ship tutorial is behind you", shared with the skip a
            // player takes. A second copy here is exactly how the two would drift apart.
            DemoProgress.SkipShipTutorial(m);
        }

        static void ApplyZoneOneTerminalsDone(MetaState m)
        {
            m.DemoJuiceTaught = true; // beat 1's teach played before its terminals were done
            m.DeepestWaveReached = FirstTerminal.ZoneOneEndWave;
            m.SetZoneDeepest(FirstTerminal.ZoneOneNumber, FirstTerminal.ZoneOneEndWave);
            m.SetCompletedTerminalIds(ZoneTerminalsThrough(FirstTerminal.ZoneOneNumber));
        }

        static void ApplyFirstReturn(MetaState m)
        {
            m.TotalRuns = 1;
            m.PodEjections = 1;
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
            BoarderProtocol.TryClaim(m, BoarderProtocolId.Depot);
            m.TotalRuns = 2;
            m.EarnSalvage(350);
            m.SetCompletedTerminalIds(ZoneTerminalsThrough(FirstTerminal.ZoneOneNumber));
            m.SetOpenDoorIds(new[] { "D1" });
        }

        static void ApplyZoneTwoTerminalsDone(MetaState m)
        {
            int zoneTwo = FirstTerminal.ZoneOneNumber + 1;
            m.DeepestWaveReached = WaveDirector.MaxWaves;
            m.SetZoneDeepest(zoneTwo, WaveDirector.MaxWaves);
            m.SetCompletedTerminalIds(ZoneTerminalsThrough(zoneTwo));
        }

        static void ApplyDoorTwoOpen(MetaState m)
        {
            int zoneTwo = FirstTerminal.ZoneOneNumber + 1;
            m.TotalRuns = 3;
            m.PodEjections = 2;
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
            m.SetCompletedTerminalIds(ZoneTerminalsThrough(zoneTwo));
            m.SetOpenDoorIds(new[] { DemoZones.DoorId(FirstTerminal.ZoneOneNumber), DemoZones.DoorId(zoneTwo) });
        }

        static void ApplyControlRoomReady(MetaState m)
        {
            int zoneTwo = FirstTerminal.ZoneOneNumber + 1;
            m.SetCompletedTerminalIds(ZoneTerminalsThrough(zoneTwo));
            // Zone 2's door IS the control door on the spine — no third id to add.
            m.SetOpenDoorIds(new[]
            {
                DemoZones.DoorId(FirstTerminal.ZoneOneNumber),
                DemoZones.DoorId(zoneTwo),
            });
        }

        /// <summary>Digit 9: replay the zone loop in place — every terminal incomplete, every
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
