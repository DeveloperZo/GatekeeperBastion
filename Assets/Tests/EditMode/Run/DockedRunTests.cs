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
            meta = new MetaState();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        /// <summary>Clear a field wave, then run a full Hatch-bank window to its timer.</summary>
        static void SurviveABankWindow(RunSim sim, int kills = 24)
        {
            sim.TriggerDraw();
            Kill(sim, kills);
            sim.NotifyAssaultCleared();
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
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

            sim.TriggerDraw();
            Kill(sim, 24);
            sim.NotifyAssaultCleared();
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
            sim.TriggerDraw();
            Kill(sim, 24);
            sim.NotifyAssaultCleared();
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(5f); // ~40 units dripped home before the breach
            long banked = meta.BankedSalvage;
            Assert.Greater(banked, 0L);

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
            Assert.AreEqual(0, meta.HatchConversions,
                "a player who banked nothing has nothing to spend — and no wheel");
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
        public void OpenDoorIds_RoundTripJson_AndPreV10IsEmpty()
        {
            var meta = new MetaState();
            meta.SetOpenDoorIds(new[] { "D1", "D2" });

            var back = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(2, back.OpenDoorIds.Length);
            Assert.AreEqual("D1", back.OpenDoorIds[0]);
            Assert.AreEqual("D2", back.OpenDoorIds[1]);

            var old = MetaState.FromJson(
                "{\"SaveVersion\":9,\"OpenDoorIds\":[\"D1\"],\"CleanExits\":1}");
            Assert.AreEqual(0, old.OpenDoorIds.Length,
                "a pre-v10 save opened its doors under the retired terminal rule; it gets none");
        }

        [Test]
        public void SetOpenDoorIds_NullOrEmpty_Clears()
        {
            var meta = new MetaState();
            meta.SetOpenDoorIds(new[] { "D1" });
            meta.SetOpenDoorIds(null);
            Assert.AreEqual(0, meta.OpenDoorIds.Length);
            meta.SetOpenDoorIds(new string[0]);
            Assert.AreEqual(0, meta.OpenDoorIds.Length);
        }
    }
}
