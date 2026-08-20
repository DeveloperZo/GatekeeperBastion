using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Dev demo checkpoints — jump the session
    /// to a demo beat instead of replaying every loop to reach it (F12, then a digit). Always
    /// compiled, like the rest of the Dev command surface. Digits 1–4 adopt a constructed,
    /// session-only MetaState (the taint keeps it off the real save) and reload the Ship;
    /// digit 5 resets the live Bastion's terminals and doors so a zone loop replays in place.
    /// Numbers mirror the beat docs' authored aims (Docs/Demo), not live tuning.</summary>
    public sealed partial class GameController
    {
        /// <summary>Overlay rows for the F12 digit prompt — DevConsole draws exactly these.</summary>
        public static readonly string[] DemoCheckpointLabels =
        {
            "1  beat 0 done — first EMBARK next",
            "2  first return — loss 1 banked, wheel opens, D1 shut",
            "3  D1 open — buys spent, protocol claimed, zone 2 next",
            "4  D1+D2 open — Primary kit, Control Room next",
            "5  reset terminals + doors (live Bastion run)",
        };

        /// <summary>F12 + digit 1–4: build a state and go to the Ship. Digit 5 (the live
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
            Log.Event("Dev", $"demo checkpoint {digit} — {DemoCheckpointLabels[digit - 1]}; loading the Ship");
            if (!SceneFlow.LoadShip())
                Log.Warn(Tag, "checkpoint adopted but the Ship scene failed to load.");
        }

        /// <summary>A fresh MetaState at the named demo point. Public for the EditMode pins;
        /// null on an unknown digit. Salvage moves through Earn/Spend so the ledger balances,
        /// and every gate reads exactly what the beats say it reads.</summary>
        public static MetaState BuildDemoCheckpoint(int digit)
        {
            if (digit < 1 || digit > 4) return null;

            // Beat 0 complete on every checkpoint — the Ship intro is never the section
            // under test, and its gates are the EMBARK refuse table's.
            var m = MetaState.CreateNew();
            m.DemoCapsuleOpened = true;
            m.DemoNavPowered = true;
            m.DemoArmoryPowered = true;
            m.DemoWeaponGranted = true;
            m.DemoShipFightDone = true;
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.SetEquipped(WeaponId.Mk1Sidearm);
            if (digit == 1) return m;

            // Loss 1 taken, the authored ~500 banked through the Hatch (Demo beat 2 aims):
            // the wheel key is open, the first-return lines have not played, D1 is shut —
            // the first Docked resolve runs the whole beat from its top.
            m.TotalRuns = 1;
            m.PodEjections = 1;
            m.HatchConversions = 1;
            m.EarnSalvage(500);
            m.DeepestWaveReached = FirstTerminal.ZoneOneEndWave;
            m.SetZoneDeepest(FirstTerminal.ZoneOneNumber, FirstTerminal.ZoneOneEndWave);
            if (digit == 2) return m;

            // The shop pass is spent (both Secondary buys), the Default is claimed, and
            // zone 1's bank was survived — D1 stands open, zone 2 is the next fight.
            // The buys are funded at the reconstruction floor's rate: the load-time repair
            // prices every tier on the historical curve (WeaponTier.SpentToReach(int)), not
            // the Secondary lane the live shop charges, and a checkpoint below that floor
            // would be "repaired" on adoption and stop resembling a quiet save.
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
            if (digit == 3) return m;

            // Zone 2 taken (ahead of that beat's build): loss 2's granted Primary kit, D2
            // open — the Control Room approach is the next section. The kit is paid for
            // through the ledger, not granted: an unpaid weapon trips the load-time
            // spend-floor repair and the state stops resembling a played save.
            m.TotalRuns = 3;
            m.PodEjections = 2;
            m.HatchConversions = 3;
            m.DeepestWaveReached = WaveDirector.MaxWaves;
            m.SetZoneDeepest(FirstTerminal.ZoneOneNumber + 1, WaveDirector.MaxWaves);
            long kitCost = WeaponCatalog.Def(WeaponId.PriC).cost + WeaponTier.SpentToReach(2);
            m.EarnSalvage(kitCost);
            m.SpendSalvage(kitCost);
            m.SetOwned(WeaponId.PriC, true);
            m.ReplacePointsFromFlatTier(WeaponId.PriC, 2);
            m.SetEquipped(WeaponId.PriC);
            m.EarnSalvage(400);
            m.SetCompletedTerminalIds(new[] { FirstTerminal.Id, FirstTerminal.SecondId, "T3" });
            m.SetOpenDoorIds(new[] { "D1", "D2" });
            return m;
        }

        /// <summary>Digit 5: replay the zone loop in place — every terminal incomplete, every
        /// locked door shut, snapshots cleared, the next draw pointed back at wave 1. The
        /// saved DemoZoneDeepestWave is deliberately kept, so the retrigger offer re-appears
        /// at the deepest terminal exactly as after a real loss. Bank slots are per-Run and
        /// cannot be restored; the log names what is left.</summary>
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
                + "next draw is wave 1; bank slots left " + Sim.HatchBankSlotsLeft);
        }
    }
}
