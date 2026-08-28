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

            // ---- zone 1: start at the Hatch, walk it down, and the shutoff opens D1 ----
            string zoneOneId = DemoZones.TerminalIdAt(1, 0);
            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the bank must refuse while zone 1's terminal is unfinished.");
            Assert.IsFalse(layout.IsDoorOpen("D1"), "D1 is open before zone 1 was played.");
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 1);
            Assert.IsTrue(layout.IsTerminalDone(zoneOneId),
                zoneOneId + " did not read done after its composite and shutoff.");
            Assert.IsTrue(layout.IsDoorOpen("D1"),
                "closing zone 1 at its terminal must open D1. It used to take a survived "
                + "Convert, and when the Convert left the demo that made D1 unopenable and "
                + "the spine impassable (Docs/Design/0-Gameplay.md).");

            // ---- zone 2: the same loop again, and its shutoff opens the control door ----
            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the bank must refuse while zone 2's terminal is unfinished.");
            Assert.IsFalse(layout.IsDoorOpen("D3"), "the control door opened before its zone.");
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 2);
            Assert.IsTrue(layout.IsDoorOpen("D3"),
                "closing zone 2 at its terminal must open the control door.");

            // There is no third zone on the spine — zone 2's bank IS the control door.
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
