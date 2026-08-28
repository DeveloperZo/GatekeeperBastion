using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>Drives a live Run through the host-side verbs a player's inputs reach — the
    /// terminal trigger, the Hatch-bank window, the Master Control pull — inside the real
    /// Bastion scene. Waves are STRIPPED rather than fought: every fixture built on this
    /// proves the Run's wiring and progression, never its difficulty. Shared so the chain
    /// and the campaign spine speak one vocabulary instead of two drifting copies.</summary>
    internal static class BastionRunDrive
    {
        /// <summary>Range given to the host-side verbs. The tests stand where they load, so
        /// the reach check is deliberately not the thing under test.</summary>
        public const float ReachMetres = 1000f;

        /// <summary>Kills credited per Wave. Enough to bank a pile the window can convert;
        /// the exact figure is not a rule any assertion here reads.</summary>
        public const int KillsPerWave = 8;

        /// <summary>Seconds of Withdraw held per step. Small enough that the accrual remainder
        /// cannot overshoot an ask by anything an assertion would notice.</summary>
        const float WithdrawStepSeconds = 0.25f;

        /// <summary>Safety valve: a Withdraw that can never reach the ask (empty pot, damaged
        /// Hatch, wrong phase) must fail loudly rather than spin.</summary>
        const int MaxWithdrawSteps = 4000;

        /// <summary>
        /// THE POST-2026-08-25 ROUTE TO A CARRIED PILE. Kills bank as they land (Zo — "it
        /// should be impossible for a player to die with no progress to show for it"), so the
        /// Waves these helpers strip leave NOTHING in the seat's pocket, and a Convert window
        /// asked to open on unbanked=0 is refused outright. A pile exists now only because
        /// somebody withdrew it back out of the pot at the Hatch and chose to put it at risk.
        ///
        /// It holds the REAL verb rather than writing the wallet, for the same reason the
        /// EditMode twin does (Assets/Tests/EditMode/RunEconomyFixture.cs — a different
        /// assembly, so this is a deliberate second copy and not a shared one): a fixture that
        /// reached past the Withdraw gate would keep passing after that gate broke, and the
        /// gate is now the only door into a carried pile there is.
        ///
        /// Returns what actually MOVED, which may exceed <paramref name="want"/> by less than
        /// one step: the verb pays whole salvage units per tick and cannot pay a partial one.
        /// Read the return value rather than assuming the ask — <see cref="StakeTheBank"/> is
        /// the only exact case, because Withdraw clamps itself to the pot.
        /// </summary>
        public static long Stake(RunSim sim, long want)
        {
            Assert.IsNotNull(sim, "Stake needs a live Run.");
            if (want <= 0L) return 0L;

            long start = sim.TotalUnbanked;
            long target = start + want;
            int steps = 0;
            while (sim.TotalUnbanked < target)
            {
                if (sim.Withdraw(WithdrawStepSeconds) <= 0L)
                {
                    Assert.Fail("Withdraw refused with " + (target - sim.TotalUnbanked)
                        + " of " + want + " still to move — pot=" + sim.BankedSalvage
                        + ", phase=" + sim.Phase + ", assault=" + sim.Assault
                        + ", hatch=" + sim.Ship.Hatch.Hp + "/" + sim.Ship.Hatch.MaxHp
                        + ". The gate is Prep + a full Hatch + a funded pot.");
                }
                if (++steps > MaxWithdrawSteps)
                {
                    Assert.Fail("Withdraw never reached " + want + " — moved "
                        + (sim.TotalUnbanked - start) + " in " + MaxWithdrawSteps + " steps.");
                }
            }
            return sim.TotalUnbanked - start;
        }

        /// <summary>Stake the realm's WHOLE pot. This is the closest the new economy comes to
        /// the state these fixtures used to arrive in, where the stripped Waves' whole take sat
        /// in the pocket waiting for a window — the pile is the same size, only the route to it
        /// changed. Asserting the pot first means a later "the window refused" can never be
        /// read as "the Waves paid nothing".</summary>
        public static long StakeTheBank(RunSim sim)
        {
            Assert.IsNotNull(sim, "StakeTheBank needs a live Run.");
            long pot = sim.BankedSalvage;
            Assert.Greater(pot, 0L,
                "the realm's pot is empty, so there is nothing to stake. Kills bank as they "
                + "land (Zo, 2026-08-25), so an empty pot here means the Waves before this "
                + "point credited nobody.");
            long moved = Stake(sim, pot);
            Assert.AreEqual(pot, moved, "the Withdraw left part of the pot behind.");
            return moved;
        }

        /// <summary>One field terminal, one Wave, cleared without fighting.</summary>
        public static IEnumerator RunSingleTerminal(
            GameController gc, RunSim sim, BastionLayout layout, string terminalId)
        {
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the bastion has no terminal " + terminalId + ".");
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " is already done, so the Hatch will answer at some other "
                + "terminal and this helper would drive the wrong Wave.");
            Assert.IsTrue(gc.StartZoneWaveFromHatch(),
                "the Hatch refused to start the Wave " + terminalId + " answers.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault,
                "the Hatch start did not put a Wave on the field for " + terminalId + ".");

            yield return null; // one scene frame: the spawner emits into the real rooms
            AwardKills(gc);
            StripAndDeliver(sim);
            sim.NotifyAssaultCleared();

            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                terminalId + "'s Wave did not hand the Run back to Prep.");
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " completed on the clear alone — surviving the Wave is not the "
                + "objective, reaching the terminal is (Docs/Design/0-Gameplay.md).");
            WalkUpAndShutOff(gc, layout, terminalId);
        }

        /// <summary>The second half of every zone: reach the terminal and shut it off. This is
        /// what completes a zone and opens its door, and no fixture may skip it — a helper that
        /// stopped at the clear would keep passing on a build where the walk did nothing.</summary>
        public static void WalkUpAndShutOff(
            GameController gc, BastionLayout layout, string terminalId)
        {
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the bastion has no terminal " + terminalId + ".");
            Assert.IsTrue(gc.CanShutOffTerminal(terminalId),
                terminalId + " does not offer its shutoff after its Waves ran — the zone can "
                + "never be closed.");
            Assert.IsTrue(gc.ShutOffTerminal(terminal), terminalId + " refused the shutoff.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                terminalId + " did not complete on the shutoff.");
        }

        /// <summary>The zone's whole Wave span on one trigger (the T3 composite).</summary>
        public static IEnumerator RunZoneComposite(
            GameController gc, RunSim sim, BastionLayout layout, int zone)
        {
            string terminalId = DemoZones.TerminalIdAt(zone, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the bastion has no terminal " + terminalId + ".");
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " is already done, so the Hatch will answer at some other zone.");
            Assert.IsTrue(gc.StartZoneWaveFromHatch(),
                "the Hatch refused to start zone " + zone + "'s composite.");

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
            Assert.IsFalse(layout.IsTerminalDone(terminalId),
                terminalId + " completed on the composite's clear — the walk up the lane is "
                + "the objective (Docs/Design/0-Gameplay.md).");
            WalkUpAndShutOff(gc, layout, terminalId);
        }

        /// <summary>Open a bank window through the host-side verb and survive its whole
        /// timer in one sim advance, so nothing in the scene can breach the Hatch.
        /// The pile the window converts is STAKED here rather than left behind by the field
        /// Waves: since the 2026-08-25 ruling their kills are already in the pot, and
        /// TriggerDraw(HatchBank) refuses outright on unbanked=0. The bet is what a Convert
        /// window is for, so making the fixture place it is the point, not a workaround.</summary>
        public static IEnumerator SurviveBank(GameController gc, RunSim sim)
        {
            long staked = StakeTheBank(sim);
            long potBefore = sim.BankedSalvage;
            HatchStation hatch = HatchStation.Instance;
            Vector3 at = hatch == null ? Vector3.zero : hatch.transform.position;
            Assert.IsTrue(gc.TryNetStartHatchBank(at, ReachMetres), "the bank refused.");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            sim.Tick(HatchBank.DurationSeconds);
            yield return null;

            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "the survived window ended the Run.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the window did not close.");
            Assert.AreEqual(0L, sim.TotalUnbanked,
                "the survived window left the stake carried instead of converting it.");
            // Nothing pays during a Hatch window — CurrentKillAward is 0 in HatchBank mode and
            // this helper awards none — so any rise in the pot across those 45 s IS the drip
            // and the dump returning the stake. A flat pot means the window ran empty.
            Assert.Greater(sim.BankedSalvage, potBefore,
                "the survived window converted none of the " + staked + " staked into it.");
        }

        /// <summary>The Master Control pull and its whole sequence: the Terminal half's Waves,
        /// the roll into the Hatch half on the follow-up timer, and the survived finale window
        /// that completes the Run. Leaves the sim in <see cref="RunPhase.Escaped"/>.</summary>
        public static IEnumerator WinThroughMasterControl(
            GameController gc, RunSim sim, BastionLayout layout)
        {
            BastionTerminal master = layout.FindMaster();
            Assert.IsNotNull(master, "the Control Room has no Master terminal.");
            long potAtPull = sim.BankedSalvage;

            Assert.IsTrue(
                gc.TryNetStartMaster(master.TerminalId, master.transform.position, ReachMetres),
                "the Master pull refused.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the pull did not start a Wave.");
            Assert.IsTrue(sim.MasterSequenceLive, "the pull did not arm the sequence.");

            long staked = 0L;
            for (int wave = 0; wave < MasterControl.TerminalWaves; wave++)
            {
                yield return null;
                AwardKills(gc);
                StripAndDeliver(sim);
                sim.NotifyAssaultCleared();

                if (wave == MasterControl.TerminalWaves - 1)
                {
                    // THE BET, placed in the last quiet gap before the finale. The finale DUMPS
                    // a carried pile, and since the 2026-08-25 ruling the only thing that fills
                    // one is a deliberate Withdraw at the Hatch — the Terminal half's kills are
                    // already in the realm's pot. This gap is the last Prep the sequence offers
                    // (AdvanceMasterControl leaves Prep armed with a follow-up timer), and it is
                    // where a player would stand to place the same bet.
                    //
                    // Master Control's Hatch half is the ONE window that draws happily on an
                    // empty pocket — TriggerDraw skips the unbanked>0 refusal for
                    // MasterControlPhase.Hatch — so without this the finale would prove the door
                    // and say nothing at all about the pot.
                    Assert.AreEqual(AssaultState.Prep, sim.Assault,
                        "the Terminal half's last clear did not leave a Prep gap to stake in.");
                    Assert.Greater(sim.BankedSalvage, potAtPull,
                        "the Terminal half's " + MasterControl.TerminalWaves + " Waves paid the "
                        + "realm nothing — kills bank as they land (Zo, 2026-08-25), so a flat "
                        + "pot means AwardKill never reached the Run.");
                    staked = StakeTheBank(sim);
                }

                sim.Tick(WaveDirector.InterWaveGap + 0.1f);
            }

            Assert.AreEqual(WaveMode.HatchBank, sim.Mode,
                "the Terminal half did not roll into the Hatch half.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the finale window did not draw.");
            // Re-keyed from "the Terminal half banked nothing": the pile is staked now, so what
            // has to be true here is that the roll into the finale carried it through intact.
            Assert.AreEqual(staked, sim.TotalUnbanked,
                "the stake did not reach the finale window intact.");

            // A Hatch window is SURVIVED, not kill-cleared: NotifyAssaultCleared is refused
            // while its timer runs, so strip the bodies and let the window expire.
            long potBeforeFinale = sim.BankedSalvage;
            yield return null;
            StripAndDeliver(sim);
            sim.Tick(HatchBank.DurationSeconds + 0.1f);
            yield return null;

            Assert.AreEqual(RunPhase.Escaped, sim.Phase,
                "surviving the finale Hatch wave must complete the Run.");
            Assert.AreEqual(0L, sim.TotalUnbanked, "the finale left the stake carried.");
            // Nothing pays inside a Hatch window, so the whole rise is the conversion. This is
            // the assertion that keeps "banks the pot" about the DUMP rather than about the
            // Terminal half's kills, which now bank whether the finale runs or not.
            Assert.Greater(sim.BankedSalvage, potBeforeFinale,
                "the finale converted none of the " + staked + " staked into it.");
        }

        public static void StripAndDeliver(RunSim sim)
        {
            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            if (spawner != null) spawner.DespawnAllLive();
            sim.NotifyDeliveryComplete();
        }

        public static void AwardKills(GameController gc)
        {
            for (int i = 0; i < KillsPerWave; i++) gc.GatekeeperKilled();
        }
    }
}
