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
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T1");
            Assert.IsFalse(layout.IsTerminalDone("T2"), "T2 reads done before it was played.");
            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the bank must refuse while T2 is unfinished — D1 requires BOTH terminals.");
            Assert.IsFalse(layout.IsDoorOpen("D1"), "D1 opened off a single terminal.");

            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T2");
            Assert.IsFalse(layout.IsDoorOpen("D1"),
                "D1 opened on terminal completion — only a survived bank may open a door.");
            yield return BastionRunDrive.SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D1"), "T1+T2 plus a survived bank must open D1.");

            // ---- zone 2: T3 is one composite trigger (3 Waves), then the second bank ----
            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the bank must refuse while zone 2's terminal T3 is unfinished.");
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 2);
            Assert.IsFalse(layout.IsDoorOpen("D2"), "D2 opened before its bank.");
            yield return BastionRunDrive.SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D2"), "T3 plus a survived bank must open D2.");

            // ---- zone 3: D3's multiplicity — T4 AND T5, then the third bank -------------
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T4");
            Assert.IsFalse(layout.IsDoorOpen("D3"), "D3 opened off a single terminal.");
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T5");
            Assert.IsFalse(layout.IsDoorOpen("D3"),
                "D3 opened on terminal completion — only a survived bank may open a door.");
            yield return BastionRunDrive.SurviveBank(gc, sim);
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

            yield return BastionRunDrive.WinThroughMasterControl(gc, sim, layout);

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

    }
}
