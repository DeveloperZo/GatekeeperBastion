using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// THERE IS EXACTLY ONE START LINE, AND IT IS THE HATCH.
    ///
    /// The 2026-08-25 playthrough went to the terminal to begin a wave, and the log says why:
    /// at 19:29:39 the Hatch was offering the start AND T2 was offering its own retrigger, and
    /// demo.terminal_look pointed at the terminal. Both verbs worked. Two start lines is one
    /// too many — the spine's loop only reads as a loop if the walk up the lane can never be
    /// the thing that begins a wave.
    ///
    /// So what is pinned here is the REFUSAL as much as the offer: while the Hatch is the
    /// start line, a zone terminal must decline to start one. The election it carries
    /// (composite vs partial, beat 4) is deliberately NOT taken away — it moves to the Hatch,
    /// which honours whatever was armed at the terminal.
    ///
    /// WHAT IT CANNOT SEE: it calls the host-side verbs rather than driving E/T through a look
    /// ray, so it says nothing about reaching the Hatch or reading its prompt.
    /// </summary>
    public class HatchStartLinePlayModeTests
    {
        const float ReachMetres = 1000f;

        string _campaignJson;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Snapshot only what already exists; never force a session into being. See the
            // remarks on ZoneTwoLoopPlayModeTests for why a fixture may not decide what the
            // session is for the fixtures after it.
            _campaignJson = GameSession.Meta == null ? null : GameSession.Meta.ToSnapshotJson();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MetaState restored;
            if (_campaignJson == null)
            {
                restored = MetaState.CreateNew();
                restored.DemoActive = false;
            }
            else
            {
                restored = MetaState.FromSnapshotJson(_campaignJson);
            }
            GameSession.AdoptDevState(restored);
            _campaignJson = null;
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>Salvage the second test's realm arrives holding. Beat 0's checkpoint is
        /// broke, and since kills bank as they land (Zo, 2026-08-25) a Withdraw at the Hatch is
        /// the ONLY thing that fills a carried pile — a Withdraw needs a funded pot in Prep,
        /// and Prep is over the moment the Wave starts. So the pile that walks up the lane has
        /// to be money the realm already had. A realm that has been out before is the ordinary
        /// case; beat 0's empty pot is the exception.</summary>
        const long ArrivingPot = 600L;

        /// <summary>Units withdrawn back out of that pot and carried into the field. Comfortably
        /// under <see cref="ArrivingPot"/> so the Withdraw can never be the thing that runs
        /// short, and well over one kill so a mis-credited award cannot fake it.</summary>
        const long StakeUnits = 200L;

        /// <summary>The shape the playthrough was in: ship tutorial behind us, embarking plain
        /// at Wave 1 with zone 1 unplayed. Nothing here is a deep start.
        /// <paramref name="pot"/> seeds the realm's bank before the scene loads, because
        /// GameController.Awake hands this exact MetaState instance to the RunSim and a later
        /// swap would leave the Run withdrawing from an object nothing reads.</summary>
        static IEnumerator EmbarkPlainAtWaveOne(long pot)
        {
            MetaState checkpoint = GameController.BuildDemoCheckpoint(1);
            Assert.IsNotNull(checkpoint, "digit 1 no longer builds a checkpoint.");
            if (pot > 0L) checkpoint.EarnSalvage(pot);
            GameSession.AdoptDevState(checkpoint);
            GameSession.PendingStartWave = 1;

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    GameController g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);
        }

        [UnityTest]
        public IEnumerator PlainEmbark_TheHatchStarts_AndTheZoneTerminalRefusesTo()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            yield return EmbarkPlainAtWaveOne(0L); // this half needs no pile, so stay broke

            GameController gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(gc, "the Bastion scene has no GameController.");
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsNotNull(layout, "the Bastion scene has no BastionLayout.");
            RunSim sim = gc.Sim;
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "boarding lands in Prep.");

            string terminalId = DemoZones.TerminalIdAt(1, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the Bastion scene has no terminal " + terminalId + ".");
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " already reads done — zone 1 is not unplayed, so the start line "
                + "under test is not the one the player meets.");

            Assert.IsTrue(gc.HatchOffersWaveStart,
                "the Hatch is not the start line on a plain embark with zone 1 unplayed.");

            // THE REFUSAL. This is the half the first build got wrong: the terminal answered
            // T here, so the player never had a reason to walk back to the Hatch.
            Assert.IsFalse(
                gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
                terminalId + " started a wave while the Hatch was the start line — the player "
                + "can begin the loop at the wrong end of the lane again.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                "the refused terminal start put a wave on the field anyway.");

            // THE OFFER.
            Assert.IsTrue(gc.StartZoneWaveFromHatch(),
                "the Hatch offered the start and then refused to draw.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault,
                "the Hatch start did not put a wave on the field.");
            Assert.AreEqual(WaveMode.Terminal, sim.Mode,
                "the Hatch start drew something other than the zone's field wave.");
        }

        [UnityTest]
        public IEnumerator KillingEveryBody_LeavesTheZoneOpen_UntilTheTerminal()
        {
            // THE HALF THAT WAS MISSING (Zo, 2026-08-26; Docs/Design/0-Gameplay.md). A wave
            // begins at the Hatch and ENDS AT A TERMINAL — but RecordCompositeClear used to
            // complete every terminal in the zone the moment the field emptied, so the
            // objective was satisfied by attrition and the 227 m lane was scenery. The 07:04
            // playthrough held the Hatch, killed all 48 bodies, watched "zone complete" fire
            // from 16 m away, then walked up to T2 and was refused three times.
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            yield return EmbarkPlainAtWaveOne(0L);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            RunSim sim = gc.Sim;
            string terminalId = DemoZones.TerminalIdAt(1, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the Bastion scene has no terminal " + terminalId + ".");
            string doorId = DemoZones.DoorId(1);

            // Before a wave is drawn the lane is quiet, and walking up must not skip the zone.
            Assert.IsFalse(gc.CanShutOffTerminal(terminalId),
                terminalId + " offered its shutoff before the zone was ever started — the "
                + "player could close a zone without running it.");
            Assert.IsFalse(gc.ShutOffTerminal(terminal), "the unrun zone accepted a shutoff.");

            Assert.IsTrue(gc.StartZoneWaveFromHatch(), "the Hatch refused to start the wave.");

            // Kill the field empty, the way a player holding the Hatch does. Every Wave of the
            // composite has to be STRIPPED AND DELIVERED as well as killed — NotifyAssaultCleared
            // is ignored while the chain still has spawns owed, which is why a bare kill loop
            // never reaches Prep.
            int guard = 0;
            while (sim.Assault == AssaultState.Assault && guard++ < 64)
            {
                yield return null;   // a scene frame, so the spawner has emitted
                BastionRunDrive.AwardKills(gc);
                BastionRunDrive.StripAndDeliver(sim);
                sim.NotifyAssaultCleared();
                if (sim.Assault == AssaultState.Assault)
                    sim.Tick(WaveDirector.InterWaveGap);
            }
            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                "the composite never returned to Prep — the fixture did not clear the field.");

            // THE ASSERTION. Surviving is not the objective.
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " completed itself when the field emptied — the walk up the lane "
                + "is the objective, and attrition just skipped it.");
            Assert.IsFalse(layout.IsDoorOpen(doorId),
                doorId + " opened without the zone being finished.");

            // ...and the walk still finishes it, from Prep, with the lane already empty.
            Assert.IsTrue(gc.CanShutOffTerminal(terminalId),
                terminalId + " refuses its shutoff in Prep — a player who killed everything "
                + "and walked up has no way to finish the zone.");
            Assert.IsTrue(gc.ShutOffTerminal(terminal), terminalId + " refused the shutoff.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId), "the shutoff did not complete it.");
            Assert.IsTrue(layout.IsDoorOpen(doorId), doorId + " did not open with the zone.");
        }

        [UnityTest]
        public IEnumerator TheWaveStartedAtTheHatch_IsShutOffAtTheTerminal_AndBanks()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            yield return EmbarkPlainAtWaveOne(ArrivingPot);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            RunSim sim = gc.Sim;
            string terminalId = DemoZones.TerminalIdAt(1, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the Bastion scene has no terminal " + terminalId + ".");

            // THE PILE IS WITHDRAWN, NOT EARNED (Zo, 2026-08-25 — "it should be impossible for
            // a player to die with no progress to show for it"). Eight kills used to leave the
            // pile this test carries up the lane; they now pay the realm the instant the body
            // falls, so the only thing that can be carried is what the player deliberately took
            // back out of the pot. It has to happen HERE, in the boarding Prep: Withdraw is
            // Prep-only and the start line below is what ends Prep.
            Assert.AreEqual(0L, sim.TotalUnbanked, "boarding must not hand out a carried pile.");
            long staked = BastionRunDrive.Stake(sim, StakeUnits);
            Assert.GreaterOrEqual(staked, StakeUnits, "the Hatch paid out short of the ask.");

            Assert.IsTrue(gc.StartZoneWaveFromHatch(), "the Hatch refused to start the wave.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "no wave on the field.");

            // The kills' own half of the ruling, and the reason the pile is not theirs: each
            // body pays the realm as it falls, and none of it joins what is carried.
            long potBeforeKills = sim.BankedSalvage;
            for (int i = 0; i < 8; i++) sim.AwardKill();
            Assert.Greater(sim.BankedSalvage, potBeforeKills, "eight kills banked nothing.");
            Assert.AreEqual(staked, sim.TotalUnbanked,
                "a kill joined the carried pile — AwardKill banks at face value now and must "
                + "never add to the stake the player chose to put at risk.");

            long carried = sim.TotalUnbanked;
            long before = sim.Salvage.RunSalvage;
            long beforePot = sim.BankedSalvage;

            Assert.IsTrue(gc.ShutOffTerminal(terminal), terminalId + " refused the shutoff.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the shutoff did not reach Prep.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                "reaching " + terminalId + " did not complete it — the objective is positional.");
            Assert.AreEqual(0L, sim.TotalUnbanked, "the terminal left the pile unbanked.");
            // No Hatch-bank window ever opened this Run, so the convert multiplier is still
            // MultLow and the shutoff must land exactly what was carried — no rake, no bonus.
            Assert.AreEqual(before + carried, sim.Salvage.RunSalvage,
                "the terminal banked something other than exactly what was carried.");
            Assert.AreEqual(beforePot + carried, sim.BankedSalvage,
                "the run tally moved but the realm's pot did not — the walk up the lane has to "
                + "put the carried pile somewhere the player keeps.");

            // THE DOOR. Completing the zone is what opens it, and this assertion is here
            // because the alternative shipped: the Convert window was for a while the ONLY
            // door-opening path in the game, so when the Convert left the demo (Zo,
            // 2026-08-25) D1 could never open and the spine was impassable end to end.
            // EditMode cannot see this — the unlock graph only exists on a real BastionLayout.
            string doorId = DemoZones.DoorId(1);
            Assert.IsTrue(layout.IsDoorOpen(doorId),
                doorId + " is still shut after zone 1 was completed — nothing else in the "
                + "demo can open it, so the run cannot reach zone 2.");
            Assert.IsTrue(gc.Meta.OpenDoorIds != null
                && System.Array.IndexOf(gc.Meta.OpenDoorIds, doorId) >= 0,
                doorId + " opened but was not snapshotted onto Meta — a Docked return would "
                + "find it shut again.");
        }
    }
}
