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
    /// scene now), and the CURRENCY MODEL holds end to end — an enemy dies, currency is added,
    /// and it is banked into MetaState.BankedSalvage the instant it is earned. Player death
    /// ends the run exactly like hatch loss (PlayerDied → Lost) and takes NOTHING back: a
    /// lost run keeps every point of salvage the player killed for. GameSession supplies the
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
        public IEnumerator CoreLoop_AutoBoard_KillsBankImmediately_DeathTakesNothingBack()
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

            // Kills are the ONLY income. Time passing must not add a single point of salvage —
            // that was the old per-second siphon drip and it is gone.
            long idleBefore = gc.Sim.Salvage.RunSalvage;
            long idleBankedBefore = gc.Meta.BankedSalvage;
            yield return new WaitForSeconds(0.75f);
            Assert.AreEqual(idleBefore, gc.Sim.Salvage.RunSalvage,
                "Salvage accrued while nothing was killed. Income is per-kill only: enemy dies, " +
                "currency added. Nothing may drip from the clock.");
            Assert.AreEqual(idleBankedBefore, gc.Meta.BankedSalvage,
                "Banked salvage grew while nothing was killed.");

            // --- ENEMY DIES → CURRENCY ADDED → BANKED, in that one motion ---
            float killValue = gc.Sim.CurrentKillValue;
            Assert.Greater(killValue, 0f,
                $"RunSim.CurrentKillValue is {killValue} at siphon L{gc.Sim.Siphon.Level}. " +
                "A kill must be worth something or there is no income at all.");

            long bankedBeforeKill = gc.Meta.BankedSalvage;
            long runBeforeKill = gc.Sim.Salvage.RunSalvage;

            gc.GatekeeperKilled(); // the one income command: → Sim.AwardKill()

            Assert.Greater(gc.Sim.Salvage.RunSalvage, runBeforeKill,
                "A kill did not add to the per-run tally (RunSim.Salvage.RunSalvage).");
            Assert.AreEqual(gc.Sim.Salvage.RunSalvage - runBeforeKill,
                gc.Meta.BankedSalvage - bankedBeforeKill,
                "Run tally and bank must receive the same floored award (M22).");

            long bankedDelta = gc.Meta.BankedSalvage - bankedBeforeKill;
            Assert.Greater(bankedDelta, 0L,
                $"A kill worth {killValue:0} did NOT reach MetaState.BankedSalvage " +
                $"(still {gc.Meta.BankedSalvage}). Currency is banked the moment it is earned — " +
                "there is no end-of-run settlement any more, so a kill that only touches the run " +
                "tally is money the player can never spend.");
            Assert.GreaterOrEqual(bankedDelta, (long)Mathf.Floor(killValue),
                $"A kill worth {killValue:0} banked only {bankedDelta}.");
            Assert.LessOrEqual(bankedDelta, (long)Mathf.Ceil(killValue),
                $"A kill worth {killValue:0} banked {bankedDelta} — more than one kill's value.");

            // Every subsequent kill pays too, and keeps paying into the same purse.
            for (int i = 0; i < 3; i++)
            {
                long before = gc.Meta.BankedSalvage;
                gc.GatekeeperKilled();
                Assert.Greater(gc.Meta.BankedSalvage, before,
                    $"Kill #{i + 2} did not bank (stuck at {before}).");
            }

            // Scene -> sim damage routing through the HatchStation proxy.
            float hpBefore = hatch.Hp;
            hatch.ApplyDamage(25f);
            Assert.AreEqual(hpBefore - 25f, hatch.Hp, 0.01f, "HatchStation routes damage into the sim hatch");

            // --- Player death: the ONLY other run ender. It stops the earning; it takes nothing ---
            long bankedAtDeath = gc.Meta.BankedSalvage;
            long earnedThisRun = gc.Sim.Salvage.RunSalvage;
            Assert.Greater(earnedThisRun, 0L, "the run earned something before the player died");
            long flooredPerKill = (long)Mathf.Floor(killValue);
            Assert.GreaterOrEqual(bankedAtDeath, banked0 + flooredPerKill * 4L,
                "The purse is missing floored kill payouts — some kills never reached the bank.");
            Assert.AreEqual(earnedThisRun, bankedAtDeath - banked0,
                "RunSalvage must equal the sum of banked awards this run (M22).");

            gc.PlayerDied(); // command → Sim.NotifyPlayerDown()
            Assert.AreEqual(RunPhase.Lost, gc.Sim.Phase, "player death ends the run");

            Assert.GreaterOrEqual(gc.Meta.BankedSalvage, bankedAtDeath,
                $"DYING TOOK MONEY BACK: banked went {bankedAtDeath} -> {gc.Meta.BankedSalvage}. " +
                "There is no loss stake and no forfeit of any kind — currency earned from kills is " +
                "the player's the instant it is earned, and a lost run may never reduce it.");
            Assert.GreaterOrEqual(gc.Meta.BankedSalvage, banked0,
                "Banked salvage ended the run below where it started.");
            Assert.AreEqual(pods0 + 1, gc.Meta.PodEjections);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(gc.Meta),
                "first death unlocks the Armory upgrade rows");

            // Report shape (cause/grossEarned/repairSpent, and no stake members).
            RunReportAssertions.AssertShape(gc.Sim, "player", earnedThisRun);

            yield return null; // let phase-change side effects (save, locks) run a frame

            // A second death command is a no-op once the run is over.
            long bankedAfterLoss = gc.Meta.BankedSalvage;
            gc.PlayerDied();
            Assert.AreEqual(pods0 + 1, gc.Meta.PodEjections, "PlayerDied is idempotent after Lost");
            Assert.AreEqual(bankedAfterLoss, gc.Meta.BankedSalvage, "no double banking");
            Assert.AreEqual(runs0, gc.Meta.TotalRuns, "run count unchanged until the next embark");

            // Kills after the run is over pay nothing — the tally is closed.
            gc.GatekeeperKilled();
            Assert.AreEqual(bankedAfterLoss, gc.Meta.BankedSalvage,
                "A kill reported after the run ended still paid out.");
        }
    }
}
