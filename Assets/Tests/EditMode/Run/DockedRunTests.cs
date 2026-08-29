using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The Run can end Docked: surviving a Hatch-bank window opens a window in which
    /// held Interact at the Hatch ends the Run by choice. Docked is not Lost — PodEjections
    /// does not move, so docking never pays the first-loss unlock.</summary>
    public class DockedRunTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        /// <summary>Clear a field wave, STAKE what it paid, then run a full Hatch-bank window
        /// to its timer. Kills bank as they land (Zo, 2026-08-25), so the window's pile is
        /// withdrawn back out through the real Hatch verb — the Convert is an opt-in bet on
        /// money you already own, not a toll on money you have not collected yet.</summary>
        static void SurviveABankWindow(RunSim sim, int kills = 24)
        {
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, kills);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
        }

        // ================================================================
        //  Closing a zone is a way home (Zo, 2026-08-28)
        // ================================================================

        [Test]
        public void ShuttingOffAZoneTerminal_OffersTheDock_SoTheDemoHasAWayHome()
        {
            // THE DEMO HAD NO WAY HOME. OfferDockIfBankSurvived opens the dock only after a
            // surviving HATCH-BANK window, and Convert was struck from the demo on 2026-08-25,
            // so DockOffered could never become true and RepairInteractable's
            // "[E] Dock — return to Ship" prompt was unreachable. The only exits were dying,
            // abandoning, or running the whole spine to Master Control. Zo played the 07:17
            // run and reported the missing call to action; there was no action to call for.
            //
            // Deliberately asserted through a FIELD wave with no bank window anywhere in it —
            // that is the whole point, and routing it through SurviveABankWindow would test
            // the path that already worked.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            Assert.AreEqual(WaveMode.Terminal, sim.Mode,
                "this fixture is meaningless if the draw was not a field wave");
            Assert.IsFalse(sim.DockOffered, "the dock was open before the zone closed");

            Kill(sim, 4);
            Assert.IsTrue(sim.ShutOffFieldWave(), "the terminal shutoff did not take");
            Assert.IsTrue(sim.DockOffered,
                "closing a zone left the player with no way home — the demo's only exits are "
                + "dying, abandoning, or the whole spine to Master Control");
        }

        [Test]
        public void StartingTheNextZone_TakesTheDockOfferBackAgain()
        {
            // Push-on is still the answer, exactly as it was for the bank window: the offer is
            // a resting point between zones, not a permanent escape hatch the player can hold
            // in reserve while fighting the next one.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            Kill(sim, 4);
            sim.ShutOffFieldWave();
            Assert.IsTrue(sim.DockOffered);

            sim.TriggerDraw();
            Assert.IsFalse(sim.DockOffered,
                "the way home survived the next wave being started");
        }

        [Test]
        public void NoDockOffer_BeforeAnyBankWindow()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsFalse(sim.DockOffered);

            sim.TriggerDraw();
            Kill(sim, 4);
            sim.NotifyAssaultCleared();
            Assert.IsFalse(sim.DockOffered, "a cleared FIELD wave is not a survived bank window");

            Assert.IsFalse(sim.DockRun(), "DockRun is refused with no offer open");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void SurvivedBankWindow_OffersTheDock_AndRaisesTheEvent()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            int survived = 0;
            sim.BankWindowSurvived += () => survived++;

            SurviveABankWindow(sim);

            Assert.AreEqual(1, survived, "the survival event fires exactly once per window");
            Assert.IsTrue(sim.DockOffered);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "surviving a window does not end the Run");
        }

        [Test]
        public void BreachedBankWindow_NeverOffersTheDock()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            int survived = 0;
            sim.BankWindowSurvived += () => survived++;

            // Re-key 2026-08-25: kills no longer leave a pile, so the bare Kill+TriggerDraw here
            // was silently refused and the window never opened — DamageHatch is a no-op outside
            // one, and the "breach" quietly did nothing. Staking through the real Withdraw is
            // what puts an actual window on the board to breach.
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 24);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(5f);
            sim.DamageHatch(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0, survived, "a failed bank never reaches the survival path");
            Assert.IsFalse(sim.DockOffered);
        }

        [Test]
        public void TriggeringTheNextWave_ClosesTheOffer()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveABankWindow(sim);
            Assert.IsTrue(sim.DockOffered);

            sim.TriggerDraw();
            Assert.IsFalse(sim.DockOffered, "pushing on is the other answer; the offer is spent");
            Assert.IsFalse(sim.DockRun(), "held Interact after the trigger does nothing");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void DockRun_EndsReturned_NotLost_AndKeepsBanked()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveABankWindow(sim);

            long banked = meta.BankedSalvage;
            Assert.Greater(banked, 0L, "the window converted something worth going home with");
            Assert.AreEqual(0L, sim.Unbanked,
                "dump-on-survive already banked the pile — the plain dock has no leftover");
            int pods = meta.PodEjections;

            Assert.IsTrue(sim.DockRun());
            Assert.AreEqual(RunPhase.Returned, sim.Phase);
            Assert.AreEqual(pods, meta.PodEjections, "a Docked end is not a lost Run");
            Assert.AreEqual(0, meta.CleanExits, "nor is it the Master Control win");
            Assert.AreEqual(banked, meta.BankedSalvage, "banked Salvage survives the dock");
            Assert.AreEqual(RunSim.DockedCause, sim.LastRunReport.cause);
            Assert.AreEqual(0L, sim.Unbanked);
        }

        [Test]
        public void DockRun_KeepsTheStatedShare_OfUnbankedHeldAtTheEnd()
        {
            // Dump-on-survive banks everything the window moved, so Unbanked at a Returned
            // end is post-window money only — today that is a withdraw. The share is the
            // guard on that pile: Returned keeps HatchBank.ReturnedKeepShare of it; Lost
            // wipes it all (criterion 0b).
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveABankWindow(sim);
            long bankedAfterWindow = meta.BankedSalvage;

            long withdrawn = sim.Withdraw(5f); // full hatch, Prep, unbanked 0 — legal
            Assert.Greater(withdrawn, 0L, "the withdraw is what puts Unbanked back in play");
            Assert.IsTrue(sim.DockOffered, "withdrawing does not answer the dock offer");

            long share = HatchBank.ReturnedKeep(withdrawn);
            Assert.Greater(share, 0L);
            Assert.Less(share, withdrawn, "surviving pays a little, and only a little");

            Assert.IsTrue(sim.DockRun());
            Assert.AreEqual(RunPhase.Returned, sim.Phase);
            Assert.AreEqual(bankedAfterWindow - withdrawn + share, meta.BankedSalvage,
                "the Returned end keeps exactly the stated share of the pile it held");
            Assert.AreEqual(0L, sim.Unbanked);
        }

        [Test]
        public void LostRun_KeepsNoShare()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveABankWindow(sim);
            long bankedAfterWindow = meta.BankedSalvage;
            long withdrawn = sim.Withdraw(5f);
            Assert.Greater(withdrawn, 0L);

            sim.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(bankedAfterWindow - withdrawn, meta.BankedSalvage,
                "Lost still wipes every leftover unit — the rebate is Returned's alone");
        }

        [Test]
        public void SurvivedWindow_CountsTheFirstHatchConversion()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.AreEqual(0, meta.HatchConversions);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));

            SurviveABankWindow(sim);

            Assert.AreEqual(1, meta.HatchConversions,
                "salvage came through the Hatch — the wheel key, whether this Run then "
                + "ends Lost or Returned");
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta));
        }

        [Test]
        public void BreachedWindow_ThatConverted_StillCounts_ButPaysNoShare()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            // Re-key 2026-08-25: same refused-window trap as the test above. Staking the whole
            // take also empties the bank first, so the figure the drip puts back is the ONLY
            // thing in it — which is what makes "what dripped through still counts" readable.
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 24);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            Assert.AreEqual(0L, meta.BankedSalvage, "the whole take is out on the table");
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(5f); // some of it dripped home before the breach
            long banked = meta.BankedSalvage;
            Assert.Greater(banked, 0L);
            Assert.Less(banked, earned, "the breach catches the window mid-drip");

            sim.DamageHatch(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(1, meta.HatchConversions,
                "what dripped through before the fall still came through the Hatch");
            Assert.AreEqual(banked, meta.BankedSalvage, "a Lost end adds nothing");
        }

        [Test]
        public void RunWithNoBank_CountsNothing()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            Kill(sim, 4);
            sim.NotifyPlayerDown();
            // Re-key 2026-08-25: HatchConversions counts WINDOWS, and this Run never opened one,
            // so the counter is still the subject and still 0. What changed is the companion
            // claim: those four kills banked, so this player does have something to spend. The
            // two facts have come apart, and the wheel now follows the money (UpgradesUnlocked
            // is LifetimeSalvageEarned, not HatchConversions).
            Assert.AreEqual(0, meta.HatchConversions,
                "no salvage came through the Hatch — no Convert window was ever opened");
            Assert.AreEqual(4L * (long)MetaUpgrades.SalvagePerKill(), meta.BankedSalvage,
                "the four kills are still the player's");
        }

        [Test]
        public void DockRun_RaisesPhaseChangedOnce_AndIsIdempotent()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveABankWindow(sim);

            int returned = 0;
            sim.PhaseChanged += p => { if (p == RunPhase.Returned) returned++; };

            Assert.IsTrue(sim.DockRun());
            Assert.IsFalse(sim.DockRun(), "a second dock on a finished Run is refused");
            Assert.AreEqual(1, returned);
            Assert.AreEqual(0, meta.PodEjections);
        }

        [Test]
        public void AfterDocking_TheRunIsInertLikeAnyTerminalPhase()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveABankWindow(sim);
            Assert.IsTrue(sim.DockRun());

            long banked = meta.BankedSalvage;
            sim.NotifyPlayerDown();
            sim.Ship.Hatch.Damage(99999f);
            sim.Tick(10f);

            Assert.AreEqual(RunPhase.Returned, sim.Phase);
            Assert.AreEqual(0, meta.PodEjections, "a post-dock death cannot retro-charge the Run");
            Assert.AreEqual(banked, meta.BankedSalvage);
        }

        [Test]
        public void SoloDown_AndHatchBreach_StillEndLost()
        {
            MetaState metaA;
            var died = Tethered(out metaA);
            died.TriggerDraw();
            died.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, died.Phase);
            Assert.AreEqual("player", died.LastRunReport.cause);
            Assert.AreEqual(1, metaA.PodEjections);

            MetaState metaB;
            var breached = Tethered(out metaB);
            breached.TriggerDraw();
            breached.Ship.Hatch.Damage(99999f);
            Assert.AreEqual(RunPhase.Lost, breached.Phase);
            Assert.AreEqual("hatch", breached.LastRunReport.cause);
            Assert.AreEqual(1, metaB.PodEjections);
        }

        [Test]
        public void OpenDoorIds_RoundTripJson_AndAbsentIsEmpty()
        {
            var meta = MetaState.CreateNew();
            meta.SetOpenDoorIds(new[] { "D1", "D2" });

            var back = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(2, back.OpenDoorIds.Length);
            Assert.AreEqual("D1", back.OpenDoorIds[0]);
            Assert.AreEqual("D2", back.OpenDoorIds[1]);

            var absent = RealmState.FromJson(
                "{\"SaveVersion\":13,\"CleanExits\":1}");
            Assert.IsNotNull(absent);
            Assert.AreEqual(0, absent.OpenDoorIds.Length,
                "a realm with no doors key stands with no doors open");
        }

        [Test]
        public void SetOpenDoorIds_NullOrEmpty_Clears()
        {
            var meta = MetaState.CreateNew();
            meta.SetOpenDoorIds(new[] { "D1" });
            meta.SetOpenDoorIds(null);
            Assert.AreEqual(0, meta.OpenDoorIds.Length);
            meta.SetOpenDoorIds(new string[0]);
            Assert.AreEqual(0, meta.OpenDoorIds.Length);
        }
    }
}
