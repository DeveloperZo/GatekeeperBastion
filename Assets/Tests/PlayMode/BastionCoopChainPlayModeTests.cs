using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// END-TO-END EVIDENCE FOR THE CO-OP BASTION'S PROGRESSION CHAIN, against the real
    /// Bastion_Coop scene: terminal Waves complete terminals, a survived Hatch-bank is the
    /// only thing that opens a door, and the chain runs T1+T2 → D1, T3 (composite) → D2,
    /// T4+T5 → D3 all the way to the Control Room. Door MULTIPLICITY is asserted at both
    /// ends: D1 stays shut (and the bank stays refused) while only one of its two terminals
    /// is done, and D3 needs both T4 and T5.
    /// Drives the same host-side verbs and helpers as ZoneTwoLoopPlayModeTests; like that
    /// fixture it despawns rather than fights, so it proves wiring, not difficulty.
    /// </summary>
    public class BastionCoopChainPlayModeTests
    {
        const float ReachMetres = 1000f;
        const int KillsPerWave = 8;

        string _campaignJson;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Snapshot only a session that already exists (the ZoneTwoLoop lesson: never
            // EnsureLoaded here, or the developer's campaign leaks into every later fixture).
            _campaignJson = GameSession.Meta == null ? null : GameSession.Meta.ToSnapshotJson();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameSession.AdoptDevState(_campaignJson == null
                ? InertSession()
                : MetaState.FromSnapshotJson(_campaignJson));
            _campaignJson = null;
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        static MetaState InertSession()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            return m;
        }

        [UnityTest]
        public IEnumerator CoopChain_TwoTerminalsOpenD1_ThenD2_ThenTwoMoreOpenD3()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);

            // A fresh demo session — the same start a new realm plays on this hull.
            MetaState checkpoint = GameController.BuildDemoCheckpoint(1);
            Assert.IsNotNull(checkpoint, "demo checkpoint 1 no longer exists.");
            GameSession.AdoptDevState(checkpoint);
            GameSession.PendingStartWave = 1;

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    GameController g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsNotNull(gc, "Bastion_Coop has no GameController.");
            Assert.IsNotNull(layout, "Bastion_Coop has no BastionLayout.");
            RunSim sim = gc.Sim;
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "entering the Bastion auto-boards.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "boarding lands in Prep.");

            // ---- zone 1: D1's multiplicity — one terminal is not enough -----------------
            yield return RunSingleTerminal(gc, sim, layout, "T1");
            Assert.IsFalse(layout.IsTerminalDone("T2"), "T2 reads done before it was played.");
            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the bank must refuse while T2 is unfinished — D1 requires BOTH terminals.");
            Assert.IsFalse(layout.IsDoorOpen("D1"), "D1 opened off a single terminal.");

            yield return RunSingleTerminal(gc, sim, layout, "T2");
            Assert.IsFalse(layout.IsDoorOpen("D1"),
                "D1 opened on terminal completion — only a survived bank may open a door.");
            yield return SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D1"), "T1+T2 plus a survived bank must open D1.");

            // ---- zone 2: T3 is one composite trigger (3 Waves), then the second bank ----
            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the bank must refuse while zone 2's terminal T3 is unfinished.");
            yield return RunZoneComposite(gc, sim, layout, 2);
            Assert.IsFalse(layout.IsDoorOpen("D2"), "D2 opened before its bank.");
            yield return SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D2"), "T3 plus a survived bank must open D2.");

            // ---- zone 3: D3's multiplicity — T4 AND T5, then the third bank -------------
            yield return RunSingleTerminal(gc, sim, layout, "T4");
            Assert.IsFalse(layout.IsDoorOpen("D3"), "D3 opened off a single terminal.");
            yield return RunSingleTerminal(gc, sim, layout, "T5");
            Assert.IsFalse(layout.IsDoorOpen("D3"),
                "D3 opened on terminal completion — only a survived bank may open a door.");
            yield return SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D3"),
                "T4+T5 plus a survived bank must open the Control Room door.");
            Assert.IsNotNull(layout.FindMaster(), "the Control Room has no Master terminal.");
        }

        /// <summary>THE WIN LEG, from the Master pull to the ship: the sequence runs its five
        /// Terminal Waves, rolls into the Hatch half on its own timer, the survived finale
        /// window banks the whole pile and completes the Run as Escaped with the clean exit
        /// recorded, and RETURN lands in the Ship with the pot intact. The lose leg of this
        /// same journey is BastionCombatAndDeathTests' death test; between the two, both
        /// exits of a Run are walked end to end through the real scene.</summary>
        [UnityTest]
        public IEnumerator MasterControl_WinsTheRun_BanksThePot_AndShipsHome()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            Assert.IsTrue(PlayTestUtil.IsSceneInBuild(SceneFlow.ShipCoop),
                "DEAD END: '" + SceneFlow.ShipCoop + "' is not in Build Settings, so the win "
                + "screen's only exit cannot land anywhere.");

            MetaState checkpoint = GameController.BuildDemoCheckpoint(1);
            Assert.IsNotNull(checkpoint, "demo checkpoint 1 no longer exists.");
            GameSession.AdoptDevState(checkpoint);
            GameSession.PendingStartWave = 1;

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    GameController g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsNotNull(gc, "Bastion_Coop has no GameController.");
            Assert.IsNotNull(layout, "Bastion_Coop has no BastionLayout.");
            RunSim sim = gc.Sim;
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "entering the Bastion auto-boards.");

            long bankedBefore = GameSession.Meta.BankedSalvage;
            int cleanBefore = GameSession.Meta.CleanExits;

            BastionTerminal master = layout.FindMaster();
            Assert.IsNotNull(master, "the Control Room has no Master terminal.");
            Assert.IsTrue(
                gc.TryNetStartMaster(master.TerminalId, master.transform.position, ReachMetres),
                "the Master pull refused.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the pull did not start a Wave.");
            Assert.IsTrue(sim.MasterSequenceLive, "the pull did not arm the sequence.");

            // The Terminal half: each clear re-draws the next Wave off the follow-up timer.
            for (int wave = 0; wave < MasterControl.TerminalWaves; wave++)
            {
                yield return null; // one scene frame: the spawner emits into the real rooms
                AwardKills(gc);
                StripAndDeliver(sim);
                sim.NotifyAssaultCleared();
                sim.Tick(WaveDirector.InterWaveGap + 0.1f);
            }

            // The fifth clear rolls into the Hatch half on the same timer — no second pull.
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode,
                "the Terminal half did not roll into the Hatch half.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the finale window did not draw.");
            Assert.Greater(sim.TotalUnbanked, 0L,
                "the Terminal half banked nothing — the finale would dump an empty pile.");

            // A Hatch window is SURVIVED, not kill-cleared: NotifyAssaultCleared is refused
            // while its timer runs, so strip the bodies and let the window expire.
            yield return null;
            StripAndDeliver(sim);
            sim.Tick(HatchBank.DurationSeconds + 0.1f);
            yield return null;

            Assert.AreEqual(RunPhase.Escaped, sim.Phase,
                "surviving the finale Hatch wave must complete the Run.");
            Assert.IsTrue(gc.RunIsOver, "the controller does not read the Run as over.");
            Assert.AreEqual(0L, sim.TotalUnbanked, "the dump left salvage stranded unbanked.");
            long bankedAfter = GameSession.Meta.BankedSalvage;
            Assert.Greater(bankedAfter, bankedBefore, "the finale dump banked nothing.");
            Assert.AreEqual(cleanBefore + 1, GameSession.Meta.CleanExits,
                "the clean exit was not recorded.");

            gc.ReturnToShip();
            yield return PlayTestUtil.WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneFlow.ShipCoop, 20f);
            Assert.AreEqual(SceneFlow.ShipCoop, SceneManager.GetActiveScene().name,
                "RETURN did not land in the Ship within 20 s (active scene: '"
                + SceneManager.GetActiveScene().name + "').");
            Assert.AreEqual(bankedAfter, GameSession.Meta.BankedSalvage,
                "the pot changed on the trip home.");
        }

        // ------------------------------------------------------------------ drive

        /// <summary>One field terminal, one Wave, cleared without fighting.</summary>
        static IEnumerator RunSingleTerminal(
            GameController gc, RunSim sim, BastionLayout layout, string terminalId)
        {
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "Bastion_Coop has no terminal " + terminalId + ".");
            Assert.IsTrue(
                gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
                terminalId + " refused its trigger.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault,
                terminalId + "'s trigger did not start a Wave.");

            yield return null; // one scene frame: the spawner emits into the real rooms
            AwardKills(gc);
            StripAndDeliver(sim);
            sim.NotifyAssaultCleared();

            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                terminalId + "'s Wave did not hand the Run back to Prep.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                terminalId + " did not complete on its clear.");
        }

        /// <summary>The zone's whole Wave span on one trigger (the T3 composite).</summary>
        static IEnumerator RunZoneComposite(
            GameController gc, RunSim sim, BastionLayout layout, int zone)
        {
            string terminalId = DemoZones.TerminalIdAt(zone, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "Bastion_Coop has no terminal " + terminalId + ".");
            Assert.IsTrue(
                gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
                terminalId + " refused its trigger.");

            int endWave = DemoZones.EndWave(zone);
            for (int wave = DemoZones.FirstWave(zone); wave < endWave; wave++)
            {
                yield return null;
                AwardKills(gc);
                StripAndDeliver(sim);
                sim.NotifyAssaultCleared();
                sim.Tick(WaveDirector.InterWaveGap);
            }

            yield return null;
            AwardKills(gc);
            StripAndDeliver(sim);
            sim.NotifyAssaultCleared();

            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the composite did not clear.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                terminalId + " did not complete.");
        }

        /// <summary>Open a bank window through the host-side verb and survive its whole
        /// timer in one sim advance, so nothing in the scene can breach the Hatch.</summary>
        static IEnumerator SurviveBank(GameController gc, RunSim sim)
        {
            Assert.Greater(sim.TotalUnbanked, 0L,
                "the field Waves banked nothing — the window would refuse on unbanked=0.");
            HatchStation hatch = HatchStation.Instance;
            Vector3 at = hatch == null ? Vector3.zero : hatch.transform.position;
            Assert.IsTrue(gc.TryNetStartHatchBank(at, ReachMetres), "the bank refused.");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            sim.Tick(HatchBank.DurationSeconds);
            yield return null;

            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "the survived window ended the Run.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the window did not close.");
        }

        static void StripAndDeliver(RunSim sim)
        {
            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            if (spawner != null) spawner.DespawnAllLive();
            sim.NotifyDeliveryComplete();
        }

        static void AwardKills(GameController gc)
        {
            for (int i = 0; i < KillsPerWave; i++) gc.GatekeeperKilled();
        }
    }
}
