using NUnit.Framework;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>What puts a Primary on the demo's rack. The class keeps its old name because
    /// the file does, but the beat it was written for is gone: THE CONVERT / HATCH-BANK WINDOW
    /// IS OUT OF THE DEMO (Zo, 2026-08-25), and with it both authored-loss beats that used to
    /// resolve zone 2. The gate moved to the Wave the player REACHED —
    /// <see cref="DemoProgress.PrimaryRowsWave"/>, zone 1's last — because the 2026-08-25
    /// playthrough died on Wave 2 holding 227 banked and "no buyable upgrades remain": the only
    /// kit that could carry zone 1 sat behind clearing zone 1. The run that proves you need a
    /// Primary is now the run that sells you one.
    ///
    /// <para><see cref="MetaState.DemoZone2BankLost"/> survives as a COMPATIBILITY read only —
    /// a save written before the re-key keeps its rack — so the tests below assert the rack,
    /// not the flag. The tests that pinned the flag's WRITE are [Ignore]d rather than deleted;
    /// deletion is the owner's call. These are behaviour tests over RunSim; the source-shape
    /// pins live in FirstReturnSourceTests.</para></summary>
    public class ZoneTwoBankLossTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        /// <summary>Clear field Waves up to and including <paramref name="throughWave"/>. One
        /// draw per Wave rather than the zone's composite, so every Wave lands in
        /// <see cref="MetaState.DeepestWaveEntered"/> — the figure the rack now reads. ENTERED, not Reached: Reached rises only on a CLEAR because deep start reads it as a permission, so it cannot tell "died on the Wave" from "never drew it".</summary>
        static void ClearFieldThrough(RunSim sim, int throughWave)
        {
            for (int w = 1; w <= throughWave; w++)
            {
                sim.SeekWave(w);
                sim.TriggerDraw();
                for (int i = 0; i < 8; i++) sim.AwardKill();
                sim.NotifyAssaultCleared();
            }
        }

        /// <summary>Open a Hatch-bank window on a pile the player deliberately staked. Kills
        /// bank as they land now (Zo, 2026-08-25), so a window has nothing to run on until
        /// salvage is withdrawn back out of the bank — the window is opt-in, not the demo's
        /// only route home.</summary>
        static void StakeAndOpenBankWindow(RunSim sim, long stake)
        {
            RunEconomyFixture.Stake(sim, stake);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the bank window is live");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
        }

        [Test]
        [Ignore("Retired 2026-08-25 (Zo): the demo has no Convert window for a Run to be Lost "
            + "inside, so this beat cannot be played. Recommend deletion.")]
        public void HatchBreachInsideZoneTwosWindow_WritesTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            StakeAndOpenBankWindow(sim, 50L);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(meta.DemoZone2BankLost,
                "the bank that fell in zone 2 is what puts a Primary on the rack");
        }

        [Test]
        [Ignore("Retired 2026-08-25 (Zo): same retired Convert window, reached by player-down "
            + "instead of a breach. Recommend deletion.")]
        public void PlayerDownInsideZoneTwosWindow_WritesTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            StakeAndOpenBankWindow(sim, 50L);

            sim.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(meta.DemoZone2BankLost, "player-down inside the window is the same loss");
        }

        /// <summary>Re-keyed from LossDuringZoneTwosFieldWaves_DoesNotWriteTheGate. INVERTED BY
        /// DESIGN (Zo, 2026-08-25): a field loss used to be the case that must NOT sell a
        /// Primary, and is now precisely the case that must — it is the death the ruling was
        /// written for. What survives unchanged is the rest: the legacy flag stays unwritten,
        /// and the loss still counts an ending.</summary>
        [Test]
        public void LossDuringTheFieldWaves_LeavesTheRackOpen()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoProgress.PrimaryRowsWave);
            sim.SeekWave(DemoZones.EndWave(2));
            sim.TriggerDraw();
            Assert.AreEqual(WaveMode.Terminal, sim.Mode, "a field Wave, not a bank window");

            sim.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(meta),
                "the Wave was already reached — dying past it cannot take the rack back");
            Assert.IsFalse(meta.DemoZone2BankLost,
                "and it opened on the Wave reached, not on the legacy flag");
            Assert.AreEqual(1, meta.PodEjections, "it still counts as an ending");
        }

        /// <summary>Re-keyed from ZoneOnesBankFalling_DoesNotWriteTheGate. INVERTED BY DESIGN
        /// (Zo, 2026-08-25): zone 1's end used to be the case that pointedly sold nothing, and
        /// is now the whole key. The stepwise walk is the point — the rack must still be SHUT
        /// one Wave short, or the gate has stopped gating.</summary>
        [Test]
        public void ReachingZoneOnesLastWave_OpensThePrimaryRack()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(meta),
                "a fresh realm has reached nothing");

            ClearFieldThrough(sim, DemoProgress.PrimaryRowsWave - 1);
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(meta),
                "one Wave short of zone 1's end is still short");

            ClearFieldThrough(sim, DemoProgress.PrimaryRowsWave);
            Assert.AreEqual(DemoProgress.PrimaryRowsWave, meta.DeepestWaveEntered);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(meta),
                "reaching zone 1's last Wave is what sells the Primary that carries zone 1");

            sim.Ship.Hatch.Damage(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(meta),
                "THE RULE (Zo, 2026-08-25): nobody dies with no progress to show for it");
        }

        /// <summary>Re-keyed from BothLossKinds_StillCountAnEnding. The two loss KINDS were the
        /// Convert window's and the field's, and only one of those is left; the assertion worth
        /// keeping is the separation itself — PodEjections counts endings and never opens the
        /// rack, so the gate cannot be reached by dying at it over and over.</summary>
        [Test]
        public void ALossShortOfZoneOnesEnd_CountsAnEnding_AndOpensNothing()
        {
            // Falling short means never DRAWING zone 1's last Wave. An earlier draft cleared
            // up to it and then drew it before dying, which under the entered-not-cleared rule
            // is reaching it — and reaching it is exactly what sells the Primary.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            Assert.Less(meta.DeepestWaveEntered, DemoProgress.PrimaryRowsWave,
                "this Run has to end short of zone 1's last Wave or it proves nothing");
            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(1, meta.PodEjections,
                "PodEjections is untouched by this milestone — it counts endings either way");
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(meta),
                "an ending is not progress; the Wave reached is");
        }

        [Test]
        [Ignore("Retired 2026-08-25 (Zo): 'zone 2 resolved' is no longer a gate — the rack "
            + "opens three Waves earlier, on zone 1's end. Recommend deletion.")]
        public void SurvivingTheWindow_OpensTheGate_ResolvedEitherWay()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            StakeAndOpenBankWindow(sim, 50L);

            sim.Tick(HatchBank.DurationSeconds);

            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the window closed on its timer");
            Assert.AreNotEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(meta.DemoZone2BankLost,
                "zone 2 RESOLVED opens the rack — survived or lost, either outcome");
        }

        /// <summary>Re-keyed from DockingOutOfASurvivedWindow_ArrivesWithTheGateOpen. The window
        /// is out of the demo's spine but is still the only door to a Docked end, and a player
        /// who withdraws a stake to run one can still walk home through it. Re-keyed is the
        /// payoff: the rack they dock with was bought by the Waves they reached.</summary>
        [Test]
        public void DockingOutOfAStakedWindow_ArrivesWithTheRackOpen()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoProgress.PrimaryRowsWave);
            StakeAndOpenBankWindow(sim, 50L);
            sim.Tick(HatchBank.DurationSeconds);

            Assert.IsTrue(sim.DockRun(), "a survived window offers the dock");

            Assert.AreEqual(RunPhase.Returned, sim.Phase);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(meta),
                "the rack was already earned on the field and the dock does not spend it");
            Assert.AreEqual(0, meta.PodEjections, "a Returned end is still not an ejection");
        }

        [Test]
        [Ignore("Retired 2026-08-25 (Zo): the finale's window cannot mis-sell a rack the Wave "
            + "count opened long before it. Recommend deletion.")]
        public void AWaveDeeperThanZoneTwo_DoesNotWriteTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            // Terminals past the authored table draw past the last zone's end Wave, and so
            // does the Master Control terminal half. ZoneOfWave FLOORS those onto zone 2 for
            // logging; the gate must not, or the finale's lost window sells a Primary the
            // player already owns (Docs/Demo - Single/7-hatch-finale.md).
            int pastTheTable = DemoZones.EndWave(DemoZones.ZoneCount) + 1;
            Assert.AreEqual(DemoZones.ZoneCount, DemoZones.ZoneOfWave(pastTheTable),
                "the floor is still the right read for a log");
            Assert.IsFalse(DemoZones.WaveIsIn(2, pastTheTable),
                "but the gate asks whether the Wave BELONGS to the zone");

            ClearFieldThrough(sim, DemoZones.EndWave(2));
            // SeekWave clamps to MaxWaves, so it cannot reach past the table. Drawing onward
            // can: a Terminal draw may continue past MaxWaves and only the COMPOSITION is
            // clamped, so the Wave counter genuinely runs deeper.
            sim.TriggerDraw();
            for (int i = 0; i < 8; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
            Assert.AreEqual(pastTheTable, sim.CurrentWaveIndex,
                "the Run is now on a Wave the zone table does not author");
            StakeAndOpenBankWindow(sim, 50L);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "a bank lost past zone 2's Waves is not zone 2's bank falling");
        }

        [Test]
        [Ignore("Retired 2026-08-25 (Zo): the flag this guarded is no longer written by the "
            + "demo at all, and DemoProgressTests.PrimaryRows_AreOpenOffTheDemo carries the "
            + "campaign-save half. Recommend deletion.")]
        public void OffTheDemo_TheGateIsNeverWritten()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.DemoActive = false;
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            StakeAndOpenBankWindow(sim, 50L);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "a campaign save past the demo has no ladder to open");
        }
    }
}
