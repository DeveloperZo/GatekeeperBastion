using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// In-editor Play-mode proof of the scene-flow loop against the REAL MonoBehaviour layer:
    /// entering the Bastion auto-boards (StartRun + EngageTether — the ship menu is its own
    /// scene now), and the CURRENCY MODEL holds end to end.
    ///
    /// THAT MODEL INVERTED ON 2026-08-25 (Zo — "it should be impossible for a player to die
    /// with no progress to show for it"). It used to be "an enemy dies, Unbanked rises,
    /// BankedSalvage does not"; it is now "an enemy dies, BankedSalvage rises at face value,
    /// and the carried pile does not move at all". A pile exists only where the player
    /// deliberately Withdrew one back out at the Hatch, and that pile — and only that pile —
    /// is what a death takes. Player death still ends the run (PlayerDied → Lost) and still
    /// leaves the bank alone. GameSession supplies the meta via its direct-play fallback.
    /// Runs in an empty scene it populates itself; tolerant of a pre-existing save/session
    /// (asserts deltas, not absolutes). Does NOT press RETURN TO SHIP — that loads the Ship
    /// scene, which isn't available to the test harness.
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
        public IEnumerator CoreLoop_AutoBoard_KillsBankOnLanding_DeathWipesOnlyTheStake()
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

            // Income is per-kill and nothing else. Time passing must not add salvage — the old
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

            // INVERTED BY DESIGN (Zo, 2026-08-25 — "it should be impossible for a player to die
            // with no progress to show for it"). Both of the first two assertions used to read
            // the other way round: RunSalvage was a converted-only tally and the bank was not
            // allowed to move on a Terminal kill. Neither survives the ruling. RunSalvage is now
            // the Run's BANKED tally and a kill Earns into it as the body falls; the realm's pot
            // takes the same award at face value in the same breath. The carried pile is what
            // must not move — it fills only on a deliberate Withdraw at the Hatch.
            long runDelta = gc.Sim.Salvage.RunSalvage - runBeforeKill;
            long bankedDelta = gc.Meta.BankedSalvage - bankedBeforeKill;
            Assert.AreEqual(bankedDelta, runDelta,
                $"A kill moved the run tally by {runDelta} and the pot by {bankedDelta}. " +
                "BankKill writes both, with the same units, in one call.");
            Assert.Greater(bankedDelta, 0L,
                $"A kill worth {killValue:0} did NOT credit MetaState.BankedSalvage " +
                $"(still {gc.Meta.BankedSalvage}).");
            Assert.GreaterOrEqual(bankedDelta, (long)Mathf.Floor(killValue),
                $"A kill worth {killValue:0} banked only {bankedDelta}.");
            Assert.LessOrEqual(bankedDelta, (long)Mathf.Ceil(killValue),
                $"A kill worth {killValue:0} banked {bankedDelta} — more than one kill's value.");
            Assert.AreEqual(unbankedBeforeKill, gc.Sim.Unbanked,
                $"A kill raised Unbanked to {gc.Sim.Unbanked}. Nothing may be put at risk that " +
                "the player did not choose to risk.");

            for (int i = 0; i < 3; i++)
            {
                long before = gc.Meta.BankedSalvage;
                gc.GatekeeperKilled();
                Assert.Greater(gc.Meta.BankedSalvage, before,
                    $"Kill #{i + 2} did not raise the ship bank (stuck at {before}).");
                Assert.AreEqual(unbankedBeforeKill, gc.Sim.Unbanked,
                    $"Kill #{i + 2} credited the carried pile.");
            }

            Assert.AreEqual(WaveMode.Terminal, gc.Sim.Mode);
            float hpBefore = hatch.Hp;
            hatch.ApplyDamage(25f);
            Assert.AreEqual(hpBefore, hatch.Hp, 0.01f,
                "HatchStation routed ApplyDamage; Terminal mode left hatch HP unchanged");

            long flooredPerKill = (long)Mathf.Floor(killValue);
            long bankedAfterKills = gc.Meta.BankedSalvage;
            Assert.AreEqual(banked0 + flooredPerKill * 4L, bankedAfterKills,
                "The pot is missing floored kill payouts.");

            // --- Stake: the only way a pile exists to lose ---
            // The wave has to end first, because Withdraw is Prep-only and a full Hatch is the
            // other half of its gate — which is exactly the shape of the bet the ruling built:
            // the money is safe until the player walks to the Hatch in a quiet moment and takes
            // it back out on purpose. Half of what the kills paid goes out; half stays behind,
            // so the death below can tell "the stake was wiped" apart from "the pot was raided".
            gc.Sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, gc.Sim.Assault,
                "stripping the wave must hand the Run back to Prep, or the Hatch cannot pay out.");
            long stakeAsk = (bankedAfterKills - banked0) / 2L;
            Assert.Greater(stakeAsk, 0L, "four kills paid too little to split into a stake.");
            long staked = BastionRunDrive.Stake(gc.Sim, stakeAsk);
            Assert.GreaterOrEqual(staked, stakeAsk, "the Hatch paid out short of the ask.");
            Assert.AreEqual(bankedAfterKills - staked, gc.Meta.BankedSalvage,
                "the withdrawn stake did not leave the pot.");
            // A Withdraw UNEARNS from the run tally as the money leaves the bank, or a pile that
            // converts back through a window would be counted twice (SalvageLedger.Unearn).
            Assert.AreEqual(runBeforeKill + flooredPerKill * 4L - staked, gc.Sim.Salvage.RunSalvage,
                "the run tally still counts salvage that has left the bank.");

            long bankedAtDeath = gc.Meta.BankedSalvage;
            long grossAtDeath = gc.Sim.Salvage.RunSalvage;
            Assert.AreEqual(staked, gc.Sim.Unbanked, "the stake is not in the seat's pocket.");

            gc.PlayerDied();
            Assert.AreEqual(RunPhase.Lost, gc.Sim.Phase, "player death ends the run");
            Assert.AreEqual(0L, gc.Sim.Unbanked,
                $"dying must wipe the {staked} staked pile — that is what makes a Withdraw a bet");
            Assert.AreEqual(bankedAtDeath, gc.Meta.BankedSalvage,
                $"DYING CHANGED THE SHIP BANK: {bankedAtDeath} -> {gc.Meta.BankedSalvage}. " +
                "Death takes what was staked and cannot reach the bank behind it.");
            Assert.Greater(gc.Meta.BankedSalvage, banked0,
                "The player died with nothing to show for it. Half of what four kills paid was " +
                "never staked, and the ruling says that half is already the realm's.");
            Assert.AreEqual(pods0 + 1, gc.Meta.PodEjections);
            // Dying does NOT open the wheel. It used to — the gate was PodEjections >= 1 —
            // and this assertion was left behind when save v11 re-keyed it onto
            // HatchConversions (Docs/Decisions.md, the wheel opens on the first bank). The
            // Nothing came through a Hatch WINDOW — no Convert ran — so the conversion counter
            // stays at zero. The wheel opens anyway, and that is the point: the 2026-08-25
            // ruling moved that gate off HatchConversions and onto LifetimeSalvageEarned,
            // because the Convert left the demo and a counter nothing can raise is a shop that
            // never opens. Four banked kills are the whole key now.
            Assert.AreEqual(0, gc.Meta.HatchConversions, "nothing converted through the Hatch");
            Assert.Greater(gc.Meta.LifetimeSalvageEarned, 0L, "four kills earned nothing");
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(gc.Meta),
                "the Run banked, and banking is what opens the Armory rows — a save that can "
                + "only unlock through a Convert can never unlock at all");

            // Report shape (cause/grossEarned/repairSpent, and no stake members). Gross was 0
            // when the tally only counted conversions; it is the Run's banked take now, net of
            // what was withdrawn back out and lost, which is what the lose screen must print.
            RunReportAssertions.AssertShape(gc.Sim, "player", grossAtDeath);

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
