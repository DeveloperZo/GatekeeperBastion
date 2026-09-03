using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class MasterControlTests
    {
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

        /// <summary>Drive the sequence to the gate and STOP THERE: two Terminal Waves, the
        /// beat, the escorted elite, and a Run that is still live because nobody has thrown
        /// the switch yet. Callers that want the ending call <see cref="EscapeSequence"/>.</summary>
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
            // THE BEAT (2026-09-03): the elite waits FinaleBeatSeconds, not the 3 s gap.
            Assert.AreEqual(MasterControlPhase.Elite, sim.MasterPhase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            sim.Tick(MasterControl.FinaleBeatSeconds);

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

        /// <summary>The finale end to end: two terminal waves, the beat, the escorted elite,
        /// the switch. RENAMED 2026-09-03 (was <c>Sequence_FiveTerminalThenTheElite_…</c>,
        /// and before that <c>…ThenHatchWindows_Escapes</c>): there are no Hatch windows in it,
        /// five waves became two, and it does not escape on its own — the elite is the last
        /// Terminal wave and the shutoff is the ending.</summary>
        [Test]
        public void Sequence_TwoTerminalTheBeatTheEscortedElite_EscapesOnTheShutoff()
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

            sim.Tick(MasterControl.FinaleBeatSeconds);
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

        /// <summary>The elite half is one wave. <see cref="MasterControl.HatchWaves"/> is a
        /// const the language table quotes; AdvanceMasterControl does not read it.</summary>
        [Test]
        public void FinaleEliteHalf_IsAlwaysOneWave()
        {
            Assert.AreEqual(1, MasterControl.HatchWaves, "the language-table const moved");

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

            sim.Tick(MasterControl.FinaleBeatSeconds);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            sim.NotifyAssaultCleared();
            Assert.IsTrue(sim.MasterAwaitsShutoff,
                "one elite wave did not reach the gate");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        /// <summary>The finale's shape (2026-09-03): two control-room waves, not five. The
        /// constant is read by the HUD readout, the Hatch's start line, HatchBank's master
        /// pile and the checkpoint, so it is pinned here once rather than restated in each.</summary>
        [Test]
        public void TerminalWaves_IsTwo()
        {
            Assert.AreEqual(2, MasterControl.TerminalWaves);
            Assert.AreEqual(12f, MasterControl.FinaleBeatSeconds, 1e-6f);
            Assert.Greater(MasterControl.FinaleBeatSeconds, WaveDirector.InterWaveGap,
                "a beat the player cannot tell from the gap is a gap");
        }

        /// <summary>The finale's shapes are ITS OWN: Mass then Pack, by ordinal — never
        /// WaveShapes.For of a wave number, which would hand a clamped wave-5 draw its Convoy
        /// or a wave-6/7 field alternation by accident.</summary>
        [Test]
        public void ShapeForTerminalWave_IsMassThenPack()
        {
            Assert.AreEqual(WaveShape.Mass, MasterControl.ShapeForTerminalWave(1));
            Assert.AreEqual(WaveShape.Pack, MasterControl.ShapeForTerminalWave(2));
            Assert.AreEqual(WaveShape.Mass, MasterControl.ShapeForTerminalWave(0), "below 1 clamps to the first");
            for (int i = 1; i <= 8; i++)
                Assert.AreNotEqual(WaveShape.Convoy, MasterControl.ShapeForTerminalWave(i),
                    "the finale never draws a Convoy — the elite IS its Thief");
        }

        /// <summary>The two control-room waves as the spawner will see them: the wave-5 SIZE
        /// (the roster index stays clamped at the deepest table once the field's five waves
        /// have run — the path a real finale takes, so the field is walked first) with the
        /// finale's shapes — the first all Default, the second a Pack with Hunters and no
        /// Thief. Derived from the director, with the roster's current figure beside it so a
        /// retune is named.</summary>
        [Test]
        public void TheTerminalWaves_DrawMassThenPack_AtWaveFiveSize()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                sim.NotifyAssaultCleared();
            }
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex, "the field was not stripped first");

            var launched = new System.Collections.Generic.List<WaveSpec>();
            sim.WaveStarted += s => launched.Add(s);

            int waveFive = new WaveDirector().NextWave(WaveDirector.MaxWaves - 1, 0, 1).enemyCount;
            Assert.AreEqual(39, waveFive, "the dial-0 solo wave-5 roster moved; re-read 4-Data.md");

            Assert.IsTrue(sim.StartMasterControl());
            sim.NotifyAssaultCleared();
            sim.Tick(WaveDirector.InterWaveGap);
            Assert.AreEqual(2, launched.Count, "two control-room waves were not drawn");

            WaveSpec mass = launched[0];
            Assert.AreEqual(waveFive, mass.enemyCount, "the first terminal wave is not wave-5 sized");
            Assert.AreEqual(mass.enemyCount, mass.defaultCount, "the first terminal wave is a Mass: all Default");
            Assert.IsFalse(mass.finaleElite);

            WaveSpec pack = launched[1];
            Assert.AreEqual(waveFive, pack.enemyCount, "the second terminal wave is not wave-5 sized");
            Assert.Greater(pack.hunterCount, 0, "the second terminal wave is a Pack: it has Hunters");
            Assert.AreEqual(0, pack.thiefCount, "a Pack has no Thief");
            Assert.AreEqual(pack.enemyCount,
                pack.defaultCount + pack.hunterCount + pack.thiefCount + pack.cloakerCount,
                "the four counts must still sum to enemyCount");
            Assert.IsFalse(pack.finaleElite);
            Assert.AreEqual(mass.enemyHealthMultiplier, pack.enemyHealthMultiplier, 1e-6f,
                "re-shaping a control-room wave must not touch its HP ladder");
        }

        /// <summary>The quiet beat: after the last terminal wave clears the elite waits
        /// FinaleBeatSeconds, not the 3 s inter-wave gap. Driven through Tick, and pinned from
        /// both sides — not launched a hair inside the beat, launched a hair past it.</summary>
        [Test]
        public void TheBeat_HoldsTheEliteForFinaleBeatSeconds()
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
            Assert.AreEqual(MasterControlPhase.Elite, sim.MasterPhase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the beat did not open on a Prep");
            Assert.AreEqual(MasterControl.FinaleBeatSeconds, sim.SequentialFollowUpRemaining, 1e-4f,
                "the elite's timer is not the beat");

            // The gap is not enough any more: the old 3 s would have drawn the elite here.
            sim.Tick(WaveDirector.InterWaveGap);
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the elite launched on the 3 s gap");

            sim.Tick(MasterControl.FinaleBeatSeconds - WaveDirector.InterWaveGap - 0.01f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                "the elite launched inside FinaleBeatSeconds - 0.01");

            sim.Tick(0.02f);
            Assert.AreEqual(AssaultState.Assault, sim.Assault,
                "the elite had not launched by FinaleBeatSeconds + 0.01");
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
        }

        /// <summary>The escort is the fight before the fight: while any escort body stands the
        /// elite refuses damage; the moment the last one falls it takes damage normally. The
        /// rule is pure so the Game layer can only supply a count, never a second rule.</summary>
        [Test]
        public void EliteShielded_WhileAnyEscortStands()
        {
            Assert.IsTrue(MasterControl.EliteShielded(MasterControl.EscortHunters + MasterControl.EscortCloakers));
            Assert.IsTrue(MasterControl.EliteShielded(1), "one escort standing still shields");
            Assert.IsFalse(MasterControl.EliteShielded(0), "no escort, no shield");
            Assert.IsFalse(MasterControl.EliteShielded(-1), "a ledger that undercounted must not shield forever");
        }

        /// <summary>The finale wave's roster in emission order: the escort lands first, the
        /// elite alone in the last group, on every seed — the shuffle only moves FULL groups
        /// and the elite is the short tail, so it cannot be dealt in ahead of its escort.</summary>
        [Test]
        public void FinaleQueue_LandsTheEscortFirst_AndTheEliteAloneLast()
        {
            WaveSpec spec = MasterControl.ApplyFinaleElite(new WaveDirector().NextWave(WaveDirector.MaxWaves - 1, 0, 1));
            int escort = MasterControl.EscortHunters + MasterControl.EscortCloakers;
            Assert.AreEqual(escort, spec.groupSize, "the group width is the whole escort");
            Assert.AreEqual(2, WaveCadence.GroupsFor(spec.enemyCount, spec.groupSize),
                "two groups: the escort, then the elite");

            var queue = new System.Collections.Generic.List<GatekeeperKind>();
            for (int seed = 1; seed <= 25; seed++)
            {
                WaveShapes.GroupByKind(spec, new System.Random(seed), queue);
                Assert.AreEqual(spec.enemyCount, queue.Count, "seed " + seed);
                Assert.AreEqual(GatekeeperKind.Thief, queue[queue.Count - 1], "seed " + seed + ": the elite is not last");
                int hunters = 0, cloakers = 0;
                for (int i = 0; i < escort; i++)
                {
                    Assert.AreNotEqual(GatekeeperKind.Thief, queue[i], "seed " + seed + ": a Thief in the escort group");
                    Assert.AreNotEqual(GatekeeperKind.Default, queue[i], "seed " + seed + ": a Default in the escort group");
                    if (queue[i] == GatekeeperKind.Hunter) hunters++;
                    if (queue[i] == GatekeeperKind.Cloaker) cloakers++;
                }
                Assert.AreEqual(MasterControl.EscortHunters, hunters, "seed " + seed);
                Assert.AreEqual(MasterControl.EscortCloakers, cloakers, "seed " + seed);
            }
            // And a field spec keeps its build order (Hunter, Thief, Cloaker, Defaults): the
            // Thief block sits ahead of the Cloakers there, not behind them.
            WaveShapes.GroupByKind(new WaveDirector().NextWave(WaveDirector.MaxWaves - 1, 0, 1), null, queue);
            Assert.Less(queue.IndexOf(GatekeeperKind.Thief), queue.IndexOf(GatekeeperKind.Cloaker),
                "a field Convoy's build order moved — GroupByKind's finale branch leaked");
        }

        /// <summary>The finale wave: one Thief wearing the HP, four Hunters and two Cloakers in
        /// front of it. RENAMED 2026-09-03 (was <c>FinaleWindow_IsOneEliteThief</c>, "no
        /// escorts"): the escort is the point now.</summary>
        [Test]
        public void FinaleWave_IsTheEliteAndItsEscort()
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

            WaveSpec finale = default;
            sim.WaveStarted += s => finale = s;
            sim.Tick(MasterControl.FinaleBeatSeconds);

            Assert.IsTrue(finale.finaleElite, "the finale wave carries the elite flag");
            Assert.AreEqual(1, MasterControl.FinaleEliteCount, "one elite; the escort is not elite");
            Assert.AreEqual(MasterControl.FinaleEliteCount, finale.thiefCount, "the Thief is the elite");
            Assert.AreEqual(4, MasterControl.EscortHunters);
            Assert.AreEqual(2, MasterControl.EscortCloakers);
            Assert.AreEqual(MasterControl.EscortHunters, finale.hunterCount);
            Assert.AreEqual(MasterControl.EscortCloakers, finale.cloakerCount);
            Assert.AreEqual(0, finale.defaultCount, "no Default in the finale");
            Assert.AreEqual(7, finale.enemyCount, "1 Thief + 4 Hunters + 2 Cloakers");
            Assert.AreEqual(finale.enemyCount,
                finale.defaultCount + finale.hunterCount + finale.thiefCount + finale.cloakerCount);
            Assert.AreEqual(MasterControl.EscortHunters + MasterControl.EscortCloakers, finale.groupSize,
                "the escort is one group and the elite the next");
            Assert.AreEqual(240f, MasterControl.FinaleEliteHp, 1e-6f, "the elite's HP did not move with the escort");
            Assert.Greater(MasterControl.FinaleEliteHp * finale.enemyHealthMultiplier,
                WaveDirector.StatsFor(GatekeeperKind.Thief).maxHp * finale.enemyHealthMultiplier,
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
            Assert.AreEqual(MasterControl.EscortHunters + MasterControl.EscortCloakers + 1, hatch.enemyCount,
                "the checkpoint's elite arrived without its escort");
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
