using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// END-TO-END EVIDENCE FOR ZONE 2, against the REAL Bastion scene and the real
    /// GameController — the seam the EditMode suites structurally cannot see, because they
    /// build a bare <see cref="RunSim"/> and never touch a BastionLayout, a BastionTerminal or
    /// a BastionDoor (Docs/Demo - Single/5-repeat-to-control-room.md).
    ///
    /// What is proved here, in the order the beat is played:
    ///   1. the Hatch-bank refuses while the CURRENT zone is 2 and its terminal is not done —
    ///      even though zone 1's terminals are complete and D1 stands open;
    ///   2. that terminal's trigger launches ONE composite of the zone's whole Wave span, not
    ///      one draw per Wave: the chain never returns to Prep between them;
    ///   3. the chain's clear completes the zone and unblocks the bank;
    ///   4. a loss inside the zone's bank window writes the Primary gate and leaves the zone's
    ///      door shut;
    ///   5. surviving that window opens the door and leaves the gate unwritten.
    ///
    /// Every zone fact — terminal id, door id, Wave span, Waves per trigger — is read from
    /// <see cref="DemoZones"/>. Nothing here is hardcoded to "T3" / "D2" / 3 / 5, so raising a
    /// zone's Wave count in the table moves this test with it instead of breaking it.
    /// </summary>
    /// <remarks>
    /// THE REAL SAVE. GameSession.Meta is the developer's live campaign. Setup serialises it
    /// with MetaState.ToSnapshotJson() BEFORE anything replaces it and teardown adopts the parsed
    /// snapshot back, so the in-memory session is handed back exactly as found. The disk file is
    /// never written at all: AdoptDevState taints the session and GameSession.Save() is a no-op
    /// for a tainted session, and the assembly-wide PlayModeSaveFixture guards the file on top
    /// of that. The snapshot covers only a session that ALREADY exists — this fixture never
    /// forces one into being, because the session it would load is the developer's mid-demo
    /// campaign and every later Ship fixture would then load a demo-active Ship.
    ///
    /// WHAT THIS TEST CANNOT SEE. It does not fight: gatekeepers are despawned rather than
    /// killed, delivery-complete is reported on the spawner's behalf, and the inter-Wave gap and
    /// the bank window are advanced by direct RunSim.Tick calls instead of by wall-clock frames.
    /// So it says nothing about difficulty, about whether a rung-1 kit can hold three Waves, or
    /// about spawn placement and pathing. Those are the playthrough's to answer. It also asserts
    /// on state, not on log strings.
    /// </remarks>
    public class ZoneTwoLoopPlayModeTests
    {
        /// <summary>Zone under test — the first zone the table authors above one Wave per
        /// trigger. Derived, so a table that grows a third composite zone still lands here.</summary>
        static int CompositeZone
        {
            get
            {
                for (int zone = 1; zone <= DemoZones.ZoneCount; zone++)
                {
                    if (DemoZones.WavesPerTrigger(zone) > 1) return zone;
                }
                return 0;
            }
        }

        /// <summary>Range handed to the host-side Prep verbs. Wide on purpose: proximity is
        /// another milestone's contract and must not be what decides these assertions.</summary>
        const float ReachMetres = 1000f;

        /// <summary>Kills awarded per field Wave, so the bank window has a pile to convert and
        /// the refuse under test can never be mistaken for "unbanked=0".</summary>
        const int KillsPerWave = 8;

        string _campaignJson;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Snapshot only what is ALREADY here. An earlier draft called
            // GameSession.EnsureLoaded() so there would always be something to hand back, and
            // that pulled the developer's real campaign into the test session — a campaign
            // that is mid-demo. Every LATER Ship fixture then loaded a demo-active Ship,
            // which logs "[Demo] no teleporter wired" as an ERROR, which the test runner
            // turns into a failure: one new test took ten unrelated ones down with it.
            // A fixture may not decide what the session is for the fixtures after it.
            _campaignJson = GameSession.Meta == null ? null : GameSession.Meta.ToSnapshotJson();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Hand the session back as found. Where there was nothing to find, leave an inert
            // one rather than this test's demo checkpoint — arriving first must not decide
            // what everyone else sees.
            GameSession.AdoptDevState(_campaignJson == null
                ? InertSession()
                : MetaState.FromSnapshotJson(_campaignJson));
            _campaignJson = null;
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>A session that gates nothing: the demo is over, so no Ship station is
        /// hidden and nothing expects the beat-0 wake pieces to be authored in the scene.</summary>
        static MetaState InertSession()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            return m;
        }

        // ------------------------------------------------------------------ criteria 1-4

        [UnityTest]
        public IEnumerator ZoneTwo_RefusesTheBank_RunsOneComposite_ThenTheFallenBankWritesTheGate()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            int zone = CompositeZone;
            Assert.Greater(zone, 0,
                "DemoZones authors no zone above one Wave per trigger — there is no composite "
                + "zone left for this test to drive.");

            yield return EnterZoneTwo(zone);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            RunSim sim = gc.Sim;
            MetaState meta = gc.Meta;
            string terminalId = DemoZones.TerminalIdAt(zone, 0);
            string doorId = DemoZones.DoorId(zone);

            // ---- criterion 1: the bank refuses while this zone is incomplete ----------------
            // The previous zone is entirely behind the party — every one of its terminals is
            // done and its door stands open — so a build whose bank gate still named zone 1's
            // terminals would read UNBLOCKED right here. Blocked is the new behaviour.
            AssertPreviousZoneIsBehindUs(layout, zone);
            Assert.IsFalse(layout.IsDoorOpen(doorId),
                doorId + " is open before this zone was ever played — the fixture is not at the "
                + "start of the beat.");
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " already reads done before the zone is played.");

            Assert.IsTrue(gc.HatchBankStartBlocked,
                "the Hatch-bank must refuse while zone " + zone + "'s terminal " + terminalId
                + " is unfinished, even with the previous zone complete and its door open.");
            Assert.IsFalse(StartBankWindow(gc),
                "the bank started while the current zone was incomplete.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the refused bank left Prep.");
            Assert.AreNotEqual(WaveMode.HatchBank, sim.Mode, "the refused bank changed the mode.");

            // ---- criterion 2: one trigger, one composite, no Prep between the Waves ---------
            int firstWave = DemoZones.FirstWave(zone);
            int endWave = DemoZones.EndWave(zone);
            int wavesPerTrigger = DemoZones.WavesPerTrigger(zone);

            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal,
                "the Bastion scene has no terminal " + terminalId + " — zone " + zone
                + " is unreachable.");

            // TryNetStartTerminal is the host-side entry: it selects the terminal and makes
            // exactly one StartTerminalWave call through the same CanSelectTerminal gate the
            // solo E-then-T path uses. Preferred over reaching into the private select field.
            Assert.IsTrue(gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
                terminalId + " refused its trigger.");

            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the trigger did not start a Wave.");
            Assert.AreEqual(WaveMode.Terminal, sim.Mode, "a field trigger drew something else.");
            Assert.AreEqual(firstWave, sim.CurrentWaveIndex,
                "the composite must open on zone " + zone + "'s first Wave.");
            Assert.IsTrue(sim.ChainPending,
                "one trigger in a " + wavesPerTrigger + "-Wave zone must arm a chain; without "
                + "ChainPending this is a single draw and the remaining Waves would each need "
                + "their own Prep and their own trigger.");

            int wavesLaunched = 1;
            for (int wave = firstWave; wave < endWave; wave++)
            {
                yield return null; // one scene frame: the spawner emits into the real rooms
                AwardKills(gc);
                StripAndDeliver(sim);

                Assert.IsTrue(sim.ChainPending,
                    "Wave " + wave + " delivered and the chain stopped being pending.");
                sim.NotifyAssaultCleared();
                Assert.AreEqual(AssaultState.Assault, sim.Assault,
                    "clearing Wave " + wave + " INSIDE the chain returned the Run to Prep — "
                    + "that is " + wavesPerTrigger + " separate draws, not one composite.");
                Assert.AreEqual(wave, sim.CurrentWaveIndex,
                    "the refused clear moved the Wave cursor.");

                sim.Tick(WaveDirector.InterWaveGap);
                wavesLaunched++;

                Assert.AreEqual(wave + 1, sim.CurrentWaveIndex,
                    "the chain did not roll straight into Wave " + (wave + 1) + ".");
                Assert.AreEqual(AssaultState.Assault, sim.Assault,
                    "Wave " + (wave + 1) + " started from Prep instead of from the chain.");
                Assert.AreEqual(WaveMode.Terminal, sim.Mode, "the chain changed mode mid-run.");
            }

            yield return null;
            AwardKills(gc);
            StripAndDeliver(sim);

            Assert.AreEqual(endWave, sim.CurrentWaveIndex,
                "the chain stopped short of zone " + zone + "'s end Wave.");
            Assert.IsFalse(sim.ChainPending, "the chain is still pending after its last Wave.");
            sim.NotifyAssaultCleared();

            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                "the composite's last Wave did not hand the Run back to Prep.");
            Assert.AreEqual(wavesPerTrigger, wavesLaunched,
                "one trigger ran " + wavesLaunched + " Waves; the table authors "
                + wavesPerTrigger + " per trigger in zone " + zone + ".");
            Assert.AreEqual(wavesPerTrigger, sim.WavesSinceBank,
                "carry into the bank must be the zone's whole Wave span — that is what makes "
                + "this bank heavier than the previous zone's.");

            // ---- criterion 3: the zone completes and the bank unblocks ----------------------
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                terminalId + " did not complete on the composite's clear, so zone " + zone
                + " never closes and the bank stays refused forever.");
            Assert.IsFalse(gc.HatchBankStartBlocked,
                "the Hatch-bank is still refused after zone " + zone + " completed.");
            Assert.IsFalse(layout.IsDoorOpen(doorId),
                doorId + " opened on terminal completion — only a survived bank may open a door.");

            // ---- criterion 4: the bank falls, the gate is written, the door stays shut ------
            Assert.Greater(sim.TotalUnbanked, 0L,
                "the field Waves banked nothing, so the window below would refuse on unbanked=0 "
                + "rather than on anything this test is about.");
            Assert.IsFalse(meta.DemoZone2BankLost,
                "the gate was already written before this Run's bank was ever opened.");

            Assert.IsTrue(StartBankWindow(gc),
                "the bank refused after the zone completed — the criterion-1 refuse above would "
                + "then prove nothing.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the bank window is not live.");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode, "the window is not a Hatch-bank.");

            int podsBefore = meta.PodEjections;
            sim.Ship.Hatch.Damage(99999f); // the Hatch is breached inside the live window
            yield return null;

            Assert.AreEqual(RunPhase.Lost, sim.Phase, "the breached Hatch did not end the Run.");
            Assert.IsTrue(meta.DemoZone2BankLost,
                "the bank that fell in zone " + zone + " is the loss that puts a Primary on the "
                + "rack; without this flag nothing opens the rung.");
            Assert.IsFalse(layout.IsDoorOpen(doorId),
                doorId + " opened on a bank that FELL. A failed bank must never unlock.");
            Assert.AreEqual(podsBefore + 1, meta.PodEjections,
                "the ejection count is untouched by the zone gate and must still move.");
        }

        // ------------------------------------------------------------------ criterion 5

        [UnityTest]
        public IEnumerator ZoneTwo_SurvivingTheBankWindow_OpensTheDoor_AndLeavesTheGateShut()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            int zone = CompositeZone;
            Assert.Greater(zone, 0, "DemoZones authors no composite zone.");

            yield return EnterZoneTwo(zone);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            RunSim sim = gc.Sim;
            MetaState meta = gc.Meta;
            string doorId = DemoZones.DoorId(zone);

            Assert.IsFalse(layout.IsDoorOpen(doorId), doorId + " is open before the zone is played.");

            yield return RunZoneComposite(gc, zone);

            Assert.IsTrue(StartBankWindow(gc), "the bank refused after the zone completed.");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            // The whole window in one advance: no wall-clock frames pass, so nothing in the
            // scene can breach the Hatch and turn this into the other test.
            sim.Tick(HatchBank.DurationSeconds);
            yield return null;

            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "the survived window ended the Run.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the window did not close on its timer.");
            Assert.IsTrue(layout.IsDoorOpen(doorId),
                doorId + " did not open on a survived zone-" + zone + " bank. Surviving the bank "
                + "is the only path that opens it.");
            Assert.IsFalse(meta.DemoZone2BankLost,
                "surviving the window is the path that opens the door, not the path that sells "
                + "the Primary.");
            Assert.Contains(doorId, meta.OpenDoorIds,
                doorId + " opened in the layout but was not snapshotted to the save, so a Docked "
                + "return would find it shut again.");
        }

        // ------------------------------------------------------------------ arrange

        /// <summary>
        /// Put the session at the demo point where the previous zone is behind the party and
        /// this zone is the next fight, then load the real Bastion into it.
        /// </summary>
        /// <remarks>
        /// The state is built by <see cref="GameController.BuildZoneTwoReadyCheckpoint"/> rather
        /// than hand-rolled, so this test cannot drift from the D1-open shop-spent meta the
        /// seven F12 beats no longer expose as their own digit. It is
        /// adopted BEFORE the scene loads because GameController.Awake hands that exact
        /// MetaState instance to the RunSim, and a swap afterwards would leave the sim writing
        /// the gate flag onto an object nothing reads.
        /// </remarks>
        static IEnumerator EnterZoneTwo(int zone)
        {
            MetaState checkpoint = GameController.BuildZoneTwoReadyCheckpoint();
            Assert.IsNotNull(checkpoint,
                "GameController.BuildZoneTwoReadyCheckpoint returned null — the state that "
                + "stands at the mouth of zone 2 no longer exists.");
            GameSession.AdoptDevState(checkpoint);
            // A MAP deep start left behind by an earlier fixture would chain Waves at embark and
            // make the composite below unreadable. This beat is a plain embark.
            GameSession.PendingStartWave = 1;

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    GameController g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(gc, "the Bastion scene has no GameController.");
            Assert.IsNotNull(gc.Sim, "GameController.Sim was never created.");
            Assert.AreSame(checkpoint, gc.Meta,
                "the scene resolved its own session instead of the adopted checkpoint.");
            Assert.IsTrue(gc.Meta.DemoActive, "the zone rules are all gated on an active demo.");
            Assert.AreEqual(RunPhase.Tethered, gc.Sim.Phase, "entering the Bastion auto-boards.");
            Assert.AreEqual(AssaultState.Prep, gc.Sim.Assault, "boarding lands in Prep.");
            Assert.AreEqual(1, gc.Sim.StartWave, "this beat embarks plain, at Wave 1.");
            Assert.AreEqual(0, gc.Sim.CurrentWaveIndex, "no Wave has been drawn yet.");

            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsNotNull(layout, "the Bastion scene has no BastionLayout.");

            // Start() restores door state on every embark but restores completed terminals only
            // on a deep start, and this is a Wave-1 embark. Push the checkpoint's completions in
            // through the layout's own public restore — the same call Start() makes — so the
            // previous zone reads finished without reaching into private state.
            layout.RestoreCompletedTerminals(gc.Meta.CompletedTerminalIds);
            yield return null;
        }

        /// <summary>Every terminal of the zone below <paramref name="zone"/> is complete and its
        /// door is open — the party is standing in this zone, not the last one.</summary>
        static void AssertPreviousZoneIsBehindUs(BastionLayout layout, int zone)
        {
            int previous = zone - 1;
            Assert.Greater(previous, 0, "zone " + zone + " has no zone in front of it.");

            int count = DemoZones.TerminalCount(previous);
            Assert.Greater(count, 0, "zone " + previous + " authors no terminals.");
            for (int i = 0; i < count; i++)
            {
                string id = DemoZones.TerminalIdAt(previous, i);
                Assert.IsTrue(layout.IsTerminalDone(id),
                    id + " does not read done, so the bank could be refusing zone " + previous
                    + " rather than zone " + zone + ".");
            }
            Assert.IsTrue(layout.IsDoorOpen(DemoZones.DoorId(previous)),
                DemoZones.DoorId(previous) + " is shut, so the current zone is still "
                + previous + ".");
        }

        // ------------------------------------------------------------------ drive

        /// <summary>Trigger the zone and run its composite to the clear. The assertions that
        /// make the chain a chain live in the first test; this is the second test's arrange.</summary>
        static IEnumerator RunZoneComposite(GameController gc, int zone)
        {
            RunSim sim = gc.Sim;
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            string terminalId = DemoZones.TerminalIdAt(zone, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the Bastion scene has no terminal " + terminalId + ".");

            Assert.IsTrue(gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
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
            Assert.IsTrue(layout.IsTerminalDone(terminalId), terminalId + " did not complete.");
            Assert.Greater(sim.TotalUnbanked, 0L, "the field Waves banked nothing.");
        }

        /// <summary>
        /// End the current Wave of the chain without fighting it: drop the delivered bodies
        /// (no kills awarded by this path) and report the delivery the spawner would otherwise
        /// have reported once its roster drained. Standing in for the spawner is what keeps the
        /// chain bounded — Waves this deep deliver dozens of actors over many seconds.
        /// </summary>
        static void StripAndDeliver(RunSim sim)
        {
            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            // Unity fake-null: a spawner destroyed during teardown must read as absent.
            if (spawner != null) spawner.DespawnAllLive();
            sim.NotifyDeliveryComplete();
        }

        /// <summary>Pay the Run a pile through the real command, so the bank window has
        /// something to convert.</summary>
        static void AwardKills(GameController gc)
        {
            for (int i = 0; i < KillsPerWave; i++) gc.GatekeeperKilled();
        }

        /// <summary>Open a Hatch-bank window through the host-side Prep verb — it selects the
        /// Hatch and makes one StartHatchBankWave call, which is where the zone gate sits.</summary>
        static bool StartBankWindow(GameController gc)
        {
            HatchStation hatch = HatchStation.Instance;
            // Unity fake-null: no Hatch in the scene must read as absent, not as a live object.
            Vector3 at = hatch == null ? Vector3.zero : hatch.transform.position;
            return gc.TryNetStartHatchBank(at, ReachMetres);
        }
    }
}
