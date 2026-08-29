using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class MasterControlTests
    {
        /// <summary>The finale stopped reading <see cref="MasterControl.HatchWaves"/> on
        /// 2026-08-28, but the static is still there and docgen still prints it, so exactly
        /// one fixture below still writes it — to prove the finale IGNORES it. Restored here
        /// so that proof cannot leak into the next test.</summary>
        [TearDown]
        public void RestoreHatchWaves()
        {
            MasterControl.HatchWaves = 1;
        }

        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static void EscapeMasterControl(RunSim sim)
        {
            EscapeSequence(sim, 0);
        }

        /// <summary>The whole finale, the way a player finishes it: fight to the armed
        /// terminal, then THROW THE SWITCH. Since 2026-08-28 the second half is the only
        /// thing that ends the Run — <see cref="SurviveSequence"/> deliberately stops one
        /// beat short of it, so every fixture that means "the player finished" has to say so.
        /// Used by RunCompleteTests.</summary>
        public static void EscapeSequence(RunSim sim, int killsPerTerminalWave)
        {
            SurviveSequence(sim, killsPerTerminalWave);
            Assert.IsTrue(sim.ShutOffMasterControl(),
                "the armed Master terminal refused the switch");
            Assert.AreEqual(RunPhase.Escaped, sim.Phase,
                "the switch was thrown and the Run did not end Escaped");
        }

        /// <summary>Drive the sequence to the gate and STOP THERE: five Terminal Waves, the
        /// elite, and a Run that is still live because nobody has thrown the switch yet.
        /// Callers that want the ending call <see cref="EscapeSequence"/>.</summary>
        public static void SurviveSequence(RunSim sim, int killsPerTerminalWave)
        {
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                Assert.AreEqual(AssaultState.Assault, sim.Assault);
                Assert.AreEqual(WaveMode.Terminal, sim.Mode);
                for (int k = 0; k < killsPerTerminalWave; k++)
                    sim.AwardKill();
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            // THE SECOND HALF IS THE ELITE, IN TERMINAL MODE (Zo, 2026-08-28). It used to be
            // a Hatch-bank window back at the door; the finale now ends where every other wave
            // ends, and the elite is what stands between the player and the switch.
            Assert.AreEqual(MasterControlPhase.Elite, sim.MasterPhase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            sim.Tick(WaveDirector.InterWaveGap);

            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode,
                "the elite was drawn as a Convert window - the finale left the lane again");
            // The elite is ONE body and it is paid for like any other. It answers to the
            // caller's kill count rather than always landing: RunCompleteTests drives the
            // ending at zero kills to prove a cleared Run banks nothing, and a body the
            // fixture never asked for would quietly pay it SalvagePerKill.
            // NotifyAssaultCleared, not AwardKill, is what says the wave is down.
            if (killsPerTerminalWave > 0) sim.AwardKill();
            sim.NotifyAssaultCleared();

            // KILLING IT DOES NOT END THE RUN. That is the whole gate: the sequence stalls
            // here until the terminal is thrown, so a finale cannot end by attrition any more
            // than a zone can.
            Assert.IsTrue(sim.MasterAwaitsShutoff,
                "the elite died and the terminal was not armed");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase,
                "the run ended on its own - the player never threw the switch");
        }

        [Test]
        public void Wave5Clear_UnbankedZero_StaysPrep()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                sim.NotifyAssaultCleared();
            }

            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(0, meta.CleanExits);
        }

        /// <summary>The finale end to end. RENAMED 2026-08-28 (was
        /// <c>Sequence_FiveTerminalThenHatchWindows_Escapes</c>): there are no Hatch windows
        /// in it any more, and it does not escape on its own — the elite is the last Terminal
        /// wave and the shutoff is the ending.</summary>
        [Test]
        public void Sequence_FiveTerminalThenTheElite_EscapesOnTheShutoff()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            // Kills bank as they land (Zo, 2026-08-25), so the finale needs a pile PUT there:
            // the run stakes back exactly what the Wave paid, by the route the player now takes.
            // The figure used to be written as the literal 200 (eight kills at 25); SalvagePerKill
            // 25 -> 42 makes it 336, and the claim was only ever "whatever the wave paid is what
            // goes on the table".
            long expected = 8L * (long)MetaUpgrades.SalvagePerKill();
            long earned = RunEconomyFixture.KillAndStake(sim, 8);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(expected, earned);
            RunEconomyFixture.Stake(sim, earned);
            Assert.AreEqual(expected, sim.Unbanked);
            // Zero — the stake came straight back out of the bank to reach the table.
            long bankBefore = meta.BankedSalvage;

            // The event still carries its old name in Sim/; what it announces is the ELITE
            // half now, and the Game layer's finale voice line hangs off it.
            bool eliteHalf = false;
            sim.MasterHatchHalfStarted += () => eliteHalf = true;
            EscapeMasterControl(sim);

            Assert.IsTrue(eliteHalf, "the elite half never announced itself");
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.IsFalse(sim.MasterAwaitsShutoff, "the switch is still armed after the Run");
            Assert.IsTrue(sim.AllWavesCleared);
            Assert.AreEqual(0L, sim.Unbanked);

            // THE SWITCH PAYS. Flagged on 2026-08-28 as a divergence and RULED the same day:
            // removing the finale's Convert window quietly removed the payout with it, so for
            // a few hours the Master switch was the one switch in the game that CONFISCATED a
            // stake — and it did that to the player who had just won, while a player who
            // staked nothing lost nothing. ShutOffFieldWave has always banked the carry at a
            // zone terminal on the stated rule that "Reaching the switch IS the payout"; the
            // Master terminal is a terminal, so it pays too.
            //
            // Asserted as GREATER rather than as an exact figure: what is load-bearing is that
            // winning cannot cost you the pile, not what the pile happened to be.
            Assert.Greater(meta.BankedSalvage, bankBefore,
                "winning Master Control confiscated the staked pile - the switch must pay");
            Assert.AreEqual(1, meta.CleanExits);
            Assert.AreEqual("cleared", sim.LastRunReport.cause);
            Assert.AreEqual(MasterControlPhase.None, sim.MasterPhase);
        }

        /// <summary>HATCH-BANK MACHINERY, NOT THE FINALE. Renamed 2026-08-28 from
        /// <c>HatchHalf_DoesNotDumpUntilLastWindow</c>: the multi-window Master half was only
        /// ever the vehicle this rode in on, and the finale has left Hatch-bank mode entirely.
        /// The rule itself is alive and belongs to Convert — a window METERS a staked pile
        /// away by the drip while it is open, and the dump is what the CLOSE does. Driven by a
        /// real Convert window now, which is the only thing that still runs this code.</summary>
        [Test]
        public void ConvertWindow_DripsWhileOpen_AndDumpsOnlyWhenItCloses()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            // Staked back out of the bank: the drip has nothing to meter otherwise, because
            // kills no longer leave a carried pile behind (Zo, 2026-08-25).
            long earned = RunEconomyFixture.KillAndStake(sim, 40);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);

            long pile = sim.Unbanked;
            // The pile has to OUTLAST the drip, or the window empties on its own and the dump
            // at the close is proved by nothing.
            Assert.Greater(pile, (long)(HatchBank.DripPerSecond * HatchBank.DurationSeconds),
                "the drip drains this pile inside the window - the close would find it empty");
            long bankBefore = meta.BankedSalvage;

            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode, "the Convert window did not open");
            sim.Tick(10f);

            // MID-WINDOW: metered, not dumped. Both halves are load-bearing — "less than the
            // pile" alone would also pass on a window that banked the lot on its first tick,
            // and "greater than zero" alone would pass on one that converted nothing.
            Assert.Less(sim.Unbanked, pile, "the drip converted nothing");
            Assert.Greater(sim.Unbanked, 0L, "the window dumped the pile instead of dripping it");
            Assert.Greater(meta.BankedSalvage, bankBefore, "the drip reached no bank");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the window closed early");

            sim.Tick(HatchBank.DurationSeconds - 10f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "a survived Convert is not a run end");
            Assert.AreEqual(0L, sim.Unbanked, "the close did not dump the remainder");
        }

        /// <summary>HATCH-BANK MACHINERY, NOT THE FINALE. Renamed 2026-08-28 from
        /// <c>HatchHalfBreach_KeepsDripped_WipesRemainder</c>. The bet a Convert window is —
        /// what the drip already banked is SAFE and what is still on the table is GONE — has
        /// nothing to do with Master Control, which merely used to be the only place a window
        /// ran. Driven by a real Convert window now.</summary>
        [Test]
        public void ConvertBreach_KeepsDripped_WipesRemainder()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            // The whole point of this test is a pile that is AT RISK when the Hatch goes, and
            // since 2026-08-25 (Zo) the only pile at risk is one the player staked.
            long earned = RunEconomyFixture.KillAndStake(sim, 24);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);

            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode, "the Convert window did not open");
            sim.Tick(10f);
            long banked = meta.BankedSalvage;
            Assert.Greater(banked, 0L, "ten seconds of drip banked nothing to be kept");
            Assert.Greater(sim.Unbanked, 0L, "nothing is left on the table to be wiped");

            sim.Ship.Hatch.Damage(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked, "the remainder survived the breach");
            Assert.AreEqual(banked, meta.BankedSalvage,
                "the breach reached back into what the drip had already banked");
            Assert.AreEqual(0, meta.CleanExits);
            Assert.IsTrue(meta.DemoActive, "a Lost window must not end the demo");
        }

        /// <summary>The behavioural half of DemoFinaleSourceTests' widened dock guard: no beat
        /// of the sequence — not the Terminal Waves, not the elite, not the armed gate the
        /// elite's death leaves standing — may offer the player a way home. Walking out mid
        /// finale would be a way to dodge the elite, and the gate makes the stall long enough
        /// for that to matter.</summary>
        [Test]
        public void NoBeatOfTheSequence_OffersTheDock()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                Assert.IsFalse(sim.DockOffered, "Terminal Wave " + (i + 1) + " offered the dock");
                sim.NotifyAssaultCleared();
                Assert.IsFalse(sim.DockOffered, "the clear of Wave " + (i + 1) + " offered it");
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            sim.Tick(WaveDirector.InterWaveGap);
            Assert.IsFalse(sim.DockOffered, "the elite's draw offered the dock");
            sim.AwardKill();
            sim.NotifyAssaultCleared();
            Assert.IsTrue(sim.MasterAwaitsShutoff);
            // THE STALL IS THE NEW EXPOSURE. The elite is down and the Run is still live, so
            // this is the one moment a dock offer would actually be reachable.
            Assert.IsFalse(sim.DockOffered, "the armed gate offered a way home past the elite");
            Assert.IsFalse(sim.DockRun(), "DockRun answered during the finale");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void SecondPull_RefusedWhileLive()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.StartMasterControl());
            Assert.IsFalse(sim.StartMasterControl());
            Assert.AreEqual(MasterControlPhase.Terminal, sim.MasterPhase);
        }

        /// <summary>THE FINALE, renamed 2026-08-28 from
        /// <c>HatchBankWithUnbankedZero_StillRunsThenEscapes</c>. The finale used to borrow
        /// Hatch-bank mode for its last window and needed an EXEMPTION from that mode's
        /// "refuse at unbanked 0" rule; it is a Terminal wave now, so an empty-handed Run
        /// walks to the switch with no exemption anywhere near it. The other side of that
        /// change — the exemption being gone from Convert — is
        /// <see cref="ConvertDraw_WithNothingStaked_IsRefused"/>.</summary>
        [Test]
        public void MasterControl_WithNothingStaked_StillEscapes()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.AreEqual(0L, sim.TotalUnbanked,
                "the fixture staked something — an empty table is the premise");
            EscapeMasterControl(sim);
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(1, meta.CleanExits, "an empty-handed win is still a win");
        }

        /// <summary>The exemption's other side. With the finale out of Hatch-bank mode, a
        /// Convert draw is once again ONLY ever a Convert — and one with nothing on the table
        /// is refused outright rather than waved through for the finale's sake.</summary>
        [Test]
        public void ConvertDraw_WithNothingStaked_IsRefused()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.AreEqual(0L, sim.TotalUnbanked);

            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "an empty Convert window opened");
            Assert.AreNotEqual(WaveMode.HatchBank, sim.Mode,
                "the refused draw still switched the Run into Convert mode");

            // And it is the EMPTY table doing the refusing, not the verb being dead: stake
            // something and the same call opens the window.
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 4);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode, "a funded Convert draw was refused");
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
        }

        [Test]
        public void CompletedTerminalIds_RoundTripJson_AndAbsentIsEmpty()
        {
            var meta = MetaState.CreateNew();
            meta.SetCompletedTerminalIds(new[] { "T2", "T1" });
            var loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(SaveFormat.CurrentVersion, loaded.SaveVersion);
            Assert.AreEqual(2, loaded.CompletedTerminalIds.Length);
            Assert.AreEqual("T2", loaded.CompletedTerminalIds[0]);
            Assert.AreEqual("T1", loaded.CompletedTerminalIds[1]);

            var absent = RealmState.FromJson(
                "{\"SaveVersion\":13,\"BankedSalvage\":0}");
            Assert.IsNotNull(absent);
            Assert.AreEqual(0, absent.CompletedTerminalIds.Length);
        }

        [Test]
        public void RestoreCompletedTerminals_KeepsProgress_AndOpenDoorsRestoreSeparately()
        {
            var graph = BastionUnlockGraph.Build(
                new[]
                {
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D1",
                        RequiredTerminalIds = new[] { "T1" },
                    },
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D_control",
                        RequiredTerminalIds = new[] { "T4" },
                        OpensControlRoom = true,
                    },
                },
                new[]
                {
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T4" },
                });

            Assert.IsTrue(graph.TryCompleteTerminal("T1", out _));
            string[] ids = graph.CopyCompletedTerminalIds();
            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual("T1", ids[0]);

            var restored = BastionUnlockGraph.Build(
                new[]
                {
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D1",
                        RequiredTerminalIds = new[] { "T1" },
                    },
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D_control",
                        RequiredTerminalIds = new[] { "T4" },
                        OpensControlRoom = true,
                    },
                },
                new[]
                {
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T4" },
                });
            restored.RestoreCompletedTerminals(ids);
            Assert.IsTrue(restored.IsTerminalDone("T1"));
            Assert.IsFalse(restored.IsDoorOpen("D1"),
                "terminal progress no longer implies an open door");

            graph.OpenEligibleDoors(out _);
            restored.RestoreOpenDoors(graph.CopyOpenDoorIds());
            Assert.IsTrue(restored.IsDoorOpen("D1"));
            Assert.IsFalse(restored.IsDoorOpen("D_control"));
        }

        /// <summary>Renamed 2026-08-28 from <c>HatchWaves_DefaultsToOne</c>, which had become
        /// a pin on a lever nothing pulls: the finale stopped reading
        /// <see cref="MasterControl.HatchWaves"/> when it left Hatch-bank mode, so asserting
        /// its default constrained no behaviour at all. The default is still worth a line —
        /// docgen prints it as the "Hatch-bank Waves" row — but the CLAIM worth pinning is
        /// that the finale IGNORES it: write five and the elite half is still exactly one
        /// wave, and still ends at the gate rather than after five.</summary>
        [Test]
        public void FinaleIgnoresHatchWaves_TheEliteIsAlwaysOneWave()
        {
            Assert.AreEqual(1, MasterControl.HatchWaves, "the docgen row moved");

            MasterControl.HatchWaves = 5;
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            Assert.AreEqual(MasterControlPhase.Elite, sim.MasterPhase);
            Assert.AreEqual(1, sim.MasterWavesLeft,
                "the elite half sized itself off HatchWaves again");

            sim.Tick(WaveDirector.InterWaveGap);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            sim.NotifyAssaultCleared();
            Assert.IsTrue(sim.MasterAwaitsShutoff,
                "one elite wave did not reach the gate — the finale is counting HatchWaves");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void FinaleWindow_IsOneEliteThief()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            WaveSpec hatch = default;
            sim.WaveStarted += s => hatch = s;
            sim.Tick(WaveDirector.InterWaveGap);

            Assert.IsTrue(hatch.finaleElite, "the final window carries the elite flag");
            Assert.AreEqual(MasterControl.FinaleEliteCount, hatch.enemyCount);
            Assert.AreEqual(MasterControl.FinaleEliteCount, hatch.thiefCount);
            Assert.AreEqual(0, hatch.hunterCount, "no escorts");
            Assert.AreEqual(0, hatch.defaultCount, "no escorts");
            Assert.AreEqual(1, hatch.groupSize);
            Assert.Greater(MasterControl.FinaleEliteHp * hatch.enemyHealthMultiplier,
                WaveDirector.StatsFor(GatekeeperKind.Thief).maxHp * hatch.enemyHealthMultiplier,
                "spawned elite HP must sit above a Wave-5 Thief");
        }

        [Test]
        public void SkipToMasterHatchHalf_DrawsEliteWithExpectedUnbanked()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            WaveSpec hatch = default;
            bool hatchHalf = false;
            sim.WaveStarted += s => hatch = s;
            sim.MasterHatchHalfStarted += () => hatchHalf = true;

            Assert.IsTrue(sim.SkipToMasterHatchHalf());
            Assert.IsTrue(hatchHalf, "the elite's spawns arm before the draw");
            Assert.AreEqual(MasterControlPhase.Elite, sim.MasterPhase);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode,
                "the checkpoint dropped into a Convert window the finale no longer has");
            Assert.AreEqual(HatchBank.MasterTerminalUnbanked(), sim.Unbanked);
            Assert.IsTrue(hatch.finaleElite);
            Assert.AreEqual(1, hatch.enemyCount);
            Assert.AreEqual(1, hatch.thiefCount);

            // The checkpoint must land on the same gate the real sequence reaches, or it stops
            // being a way to test the finale and becomes a second, divergent one.
            sim.AwardKill();
            sim.NotifyAssaultCleared();
            Assert.IsTrue(sim.MasterAwaitsShutoff);
            Assert.IsTrue(sim.ShutOffMasterControl(), "the armed terminal refused the switch");
            Assert.AreNotEqual(RunPhase.Tethered, sim.Phase, "the switch did not end the Run");
        }
    }
}
