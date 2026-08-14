using System.Collections;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>
    /// In-editor Play-mode proof of the scene-flow loop against the REAL MonoBehaviour layer:
    /// entering the Bastion auto-boards (StartRun + EngageTether — the ship menu is its own
    /// scene now), and the CURRENCY MODEL holds end to end — an enemy dies, Unbanked rises,
    /// BankedSalvage does not. Player death ends the run (PlayerDied → Lost), wipes Unbanked,
    /// and keeps already-converted ship Salvage. GameSession supplies the
    /// meta via its direct-play fallback. Runs in an empty scene it populates itself; tolerant
    /// of a pre-existing save/session (asserts deltas, not absolutes). Does NOT press RETURN TO
    /// SHIP — that loads the Ship scene, which isn't available to the test harness.
    /// </summary>
    public class CoreLoopPlayModeTests
    {
        GameObject _gcObject;
        GameObject _hatchObject;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_hatchObject != null) Object.Destroy(_hatchObject);
            if (_gcObject != null) Object.Destroy(_gcObject);
            yield return null;
        }

        /// <summary>In the authored Bastion scene the hatch's GameController arrives through a
        /// serialized `_game` reference (M45's wiring rule: serialized for scene-stable objects).
        /// This test builds its objects in an empty scene, so it stands in for the scene author —
        /// by reflection, the same way WeaponAbilityPlayModeTests force-commits a gatekeeper.</summary>
        static void WireHatchToController(HatchStation hatch, GameController gc)
        {
            var field = typeof(HatchStation).GetField("_game",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                "HatchStation no longer has a serialized `_game` GameController reference — " +
                "rewire this test to whatever replaced it.");
            field.SetValue(hatch, gc);
        }

        [UnityTest]
        public IEnumerator CoreLoop_AutoBoard_KillsCreditUnbanked_DeathWipesUnbanked()
        {
            // --- Boot: bootstrap the real controller in an empty scene ---
            _gcObject = new GameObject("GameController");
            var gc = _gcObject.AddComponent<GameController>();
            _hatchObject = new GameObject("HatchStation");
            var hatch = _hatchObject.AddComponent<HatchStation>();
            WireHatchToController(hatch, gc);

            yield return null; // Awake + Start have run
            Assert.IsNotNull(gc.Sim, "GameController created a sim");
            Assert.IsNotNull(GameSession.Meta, "GameSession fallback loaded/created a MetaState");
            Assert.AreSame(GameSession.Meta, gc.Meta, "GameController.Meta is the session meta");
            Assert.AreEqual(RunPhase.Tethered, gc.Sim.Phase,
                "entering the Bastion auto-boards (Docked is transient)");
            Assert.AreEqual(AssaultState.Prep, gc.Sim.Assault, "boarding lands in Prep");

            long banked0 = gc.Meta.BankedSalvage;
            int runs0 = gc.Meta.TotalRuns;
            int pods0 = gc.Meta.PodEjections;
            Assert.GreaterOrEqual(runs0, 1, "StartRun counted this run");

            // --- Fight: crank the dial during Prep, pull the draw ---
            gc.Sim.SetSiphonLevel(2); // dial L2 = x1.15, set during Prep
            gc.Sim.TriggerDraw();     // the draw is the bell: assault starts, kills pay
            Assert.AreEqual(AssaultState.Assault, gc.Sim.Assault);

            // Kills credit Unbanked only. Time passing must not add salvage — the old
            // per-second siphon drip is gone, and Hatch-bank drip is off in Terminal mode.
            long idleBefore = gc.Sim.Salvage.RunSalvage;
            long idleBankedBefore = gc.Meta.BankedSalvage;
            long idleUnbankedBefore = gc.Sim.Unbanked;
            yield return new WaitForSeconds(0.75f);
            Assert.AreEqual(idleBefore, gc.Sim.Salvage.RunSalvage,
                "Salvage accrued while nothing was killed. Income is per-kill only: enemy dies, " +
                "currency added. Nothing may drip from the clock.");
            Assert.AreEqual(idleBankedBefore, gc.Meta.BankedSalvage,
                "Banked salvage grew while nothing was killed.");
            Assert.AreEqual(idleUnbankedBefore, gc.Sim.Unbanked,
                "Unbanked grew while nothing was killed.");

            float killValue = gc.Sim.CurrentKillValue;
            Assert.Greater(killValue, 0f,
                $"RunSim.CurrentKillValue is {killValue} at intensity {gc.Sim.Siphon.Level}. " +
                "A kill must be worth something or there is no income at all.");

            long bankedBeforeKill = gc.Meta.BankedSalvage;
            long runBeforeKill = gc.Sim.Salvage.RunSalvage;
            long unbankedBeforeKill = gc.Sim.Unbanked;

            gc.GatekeeperKilled();

            Assert.AreEqual(runBeforeKill, gc.Sim.Salvage.RunSalvage,
                "A kill must not add to converted RunSalvage — that tally is Hatch-bank drip/dump.");
            Assert.AreEqual(bankedBeforeKill, gc.Meta.BankedSalvage,
                "A kill must not credit MetaState.BankedSalvage. AwardKill fills Unbanked.");
            long unbankedDelta = gc.Sim.Unbanked - unbankedBeforeKill;
            Assert.Greater(unbankedDelta, 0L,
                $"A kill worth {killValue:0} did NOT raise Unbanked (still {gc.Sim.Unbanked}).");
            Assert.GreaterOrEqual(unbankedDelta, (long)Mathf.Floor(killValue),
                $"A kill worth {killValue:0} credited only {unbankedDelta} unbanked.");
            Assert.LessOrEqual(unbankedDelta, (long)Mathf.Ceil(killValue),
                $"A kill worth {killValue:0} credited {unbankedDelta} — more than one kill's value.");

            for (int i = 0; i < 3; i++)
            {
                long before = gc.Sim.Unbanked;
                gc.GatekeeperKilled();
                Assert.Greater(gc.Sim.Unbanked, before,
                    $"Kill #{i + 2} did not raise Unbanked (stuck at {before}).");
                Assert.AreEqual(bankedBeforeKill, gc.Meta.BankedSalvage,
                    $"Kill #{i + 2} credited the ship bank.");
            }

            Assert.AreEqual(WaveMode.Terminal, gc.Sim.Mode);
            float hpBefore = hatch.Hp;
            hatch.ApplyDamage(25f);
            Assert.AreEqual(hpBefore, hatch.Hp, 0.01f,
                "HatchStation routed ApplyDamage; Terminal mode left hatch HP unchanged");

            long bankedAtDeath = gc.Meta.BankedSalvage;
            long unbankedAtDeath = gc.Sim.Unbanked;
            Assert.Greater(unbankedAtDeath, 0L, "the run had unbanked before the player died");
            long flooredPerKill = (long)Mathf.Floor(killValue);
            Assert.AreEqual(unbankedBeforeKill + flooredPerKill * 4L, unbankedAtDeath,
                "Unbanked is missing floored kill payouts.");
            Assert.AreEqual(banked0, bankedAtDeath, "ship bank must not move on Terminal kills");

            gc.PlayerDied();
            Assert.AreEqual(RunPhase.Lost, gc.Sim.Phase, "player death ends the run");
            Assert.AreEqual(0L, gc.Sim.Unbanked, "dying must wipe Unbanked");
            Assert.AreEqual(bankedAtDeath, gc.Meta.BankedSalvage,
                $"DYING CHANGED THE SHIP BANK: {bankedAtDeath} -> {gc.Meta.BankedSalvage}. " +
                "Converted Salvage is kept; only Unbanked is wiped.");
            Assert.GreaterOrEqual(gc.Meta.BankedSalvage, banked0,
                "Banked salvage ended the run below where it started.");
            Assert.AreEqual(pods0 + 1, gc.Meta.PodEjections);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(gc.Meta),
                "first death unlocks the Armory upgrade rows");

            // Report shape (cause/grossEarned/repairSpent, and no stake members).
            RunReportAssertions.AssertShape(gc.Sim, "player", 0L);

            yield return null; // let phase-change side effects (save, locks) run a frame

            // A second death command is a no-op once the run is over.
            long bankedAfterLoss = gc.Meta.BankedSalvage;
            gc.PlayerDied();
            Assert.AreEqual(pods0 + 1, gc.Meta.PodEjections, "PlayerDied is idempotent after Lost");
            Assert.AreEqual(bankedAfterLoss, gc.Meta.BankedSalvage, "no double banking");
            Assert.AreEqual(runs0, gc.Meta.TotalRuns, "run count unchanged until the next embark");

            gc.GatekeeperKilled();
            Assert.AreEqual(bankedAfterLoss, gc.Meta.BankedSalvage,
                "A kill reported after the run ended still paid out.");
            Assert.AreEqual(0L, gc.Sim.Unbanked,
                "A kill reported after the run ended still raised Unbanked.");
        }
    }
}
