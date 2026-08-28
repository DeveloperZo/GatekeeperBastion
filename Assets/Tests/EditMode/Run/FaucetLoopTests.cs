using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The faucet-loop graybox rules (RunSim.Faucet.cs / FaucetLoop.cs — spec in
    /// Docs/Brainstorms/coop-bastion-complexity.md, the faucet loop): the toggle is inert by
    /// default, faucet field Waves chain and pay per Wave endured, the mark gates the off
    /// verb, the draft grows with the hold and caps, and the picked axis hardens the bank
    /// Wave while raising its convert multiplier.</summary>
    public class FaucetLoopTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static RunSim FaucetOn(out MetaState meta)
        {
            var sim = Tethered(out meta);
            sim.SetFaucetLoop(true);
            return sim;
        }

        /// <summary>Endure <paramref name="waves"/> chained faucet Waves (draw the first,
        /// then clear + gap through the rest).</summary>
        static void Endure(RunSim sim, int waves)
        {
            sim.TriggerDraw();
            for (int i = 0; i < waves; i++)
            {
                sim.NotifyAssaultCleared();
                if (i < waves - 1)
                    sim.Tick(WaveDirector.InterWaveGap + 0.01f);
            }
        }

        [Test]
        public void ToggleOff_ShippedFlowUnchanged()
        {
            var sim = Tethered(out _);
            Assert.IsFalse(sim.FaucetActive);
            sim.TriggerDraw();
            Assert.IsFalse(sim.FaucetCycleLive);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(0L, sim.Unbanked); // no endure bonus off the toggle
            sim.Tick(WaveDirector.InterWaveGap + 1f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault); // no chain
            Assert.AreEqual(1, sim.CurrentWaveIndex);
        }

        [Test]
        public void Toggle_RefusedMidAssault()
        {
            var sim = Tethered(out _);
            sim.TriggerDraw();
            sim.SetFaucetLoop(true);
            Assert.IsFalse(sim.FaucetActive);
        }

        [Test]
        public void Faucet_ChainsOnInterWaveGap_AndKeepsTheDial()
        {
            var sim = FaucetOn(out _);
            sim.SetSiphonLevel(2);
            sim.TriggerDraw();
            Assert.IsTrue(sim.FaucetCycleLive);
            Assert.AreEqual(1, sim.CurrentWaveIndex);

            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(1, sim.FaucetWavesEndured);
            Assert.AreEqual(2, sim.Siphon.Level); // the cycle is one pull — dial survives

            sim.Tick(WaveDirector.InterWaveGap - 0.05f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            sim.Tick(0.1f);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
        }

        [Test]
        public void Faucet_EndureBonus_PaidPerWaveCleared()
        {
            var sim = FaucetOn(out _);
            Endure(sim, 2);
            Assert.AreEqual(2L * FaucetLoop.WaveEndureBonus, sim.Unbanked);
        }

        [Test]
        public void Faucet_ShutoffRefusedBeforeTheMark()
        {
            var sim = FaucetOn(out _);
            Endure(sim, 1);
            Assert.IsFalse(sim.FaucetMarked);
            Assert.IsFalse(sim.TryArmFaucetShutoff());
            Assert.IsFalse(sim.FaucetAwaitingDraft);
        }

        [Test]
        public void Faucet_ShutoffAtTheMark_OpensDraftOfOne()
        {
            var sim = FaucetOn(out _);
            Endure(sim, FaucetLoop.MarkAfterWaves);
            Assert.IsTrue(sim.FaucetMarked);
            Assert.IsTrue(sim.TryArmFaucetShutoff()); // Prep gap: draft opens now
            Assert.IsTrue(sim.FaucetAwaitingDraft);
            Assert.AreEqual(1, sim.FaucetDraftSize);
            // The pending chain was cancelled with the shutoff.
            sim.Tick(WaveDirector.InterWaveGap + 1f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
        }

        [Test]
        public void Faucet_ShutoffMidAssault_OpensDraftOnClear_AndCountsThatWave()
        {
            var sim = FaucetOn(out _);
            Endure(sim, FaucetLoop.MarkAfterWaves);
            sim.Tick(WaveDirector.InterWaveGap + 0.01f); // wave 3 live
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.IsTrue(sim.TryArmFaucetShutoff());
            Assert.IsTrue(sim.FaucetShutoffArmed);
            Assert.IsFalse(sim.FaucetAwaitingDraft);

            sim.NotifyAssaultCleared(); // wave 3 endured, then the draft opens
            Assert.IsTrue(sim.FaucetAwaitingDraft);
            Assert.AreEqual(3, sim.FaucetWavesEndured);
            Assert.AreEqual(2, sim.FaucetDraftSize);
        }

        [Test]
        public void Faucet_DraftCapsAtThree()
        {
            Assert.AreEqual(0, FaucetLoop.DraftSize(0));
            Assert.AreEqual(0, FaucetLoop.DraftSize(FaucetLoop.MarkAfterWaves - 1));
            Assert.AreEqual(1, FaucetLoop.DraftSize(FaucetLoop.MarkAfterWaves));
            Assert.AreEqual(2, FaucetLoop.DraftSize(FaucetLoop.MarkAfterWaves + 1));
            Assert.AreEqual(3, FaucetLoop.DraftSize(FaucetLoop.MarkAfterWaves + 2));
            Assert.AreEqual(3, FaucetLoop.DraftSize(FaucetLoop.MarkAfterWaves + 5));
        }

        [Test]
        public void Faucet_PickRefusesAnUnofferedAxis()
        {
            var sim = FaucetOn(out _);
            Endure(sim, FaucetLoop.MarkAfterWaves);
            sim.TryArmFaucetShutoff();
            Assert.AreEqual(1, sim.FaucetDraftSize);
            Assert.IsFalse(sim.TryPickFaucetAxis(FaucetAxis.Tempo));
            Assert.IsTrue(sim.TryPickFaucetAxis(FaucetAxis.Density));
            Assert.IsTrue(sim.HasPendingFaucetAxis);
            Assert.IsFalse(sim.FaucetCycleLive);
        }

        [Test]
        public void Faucet_PickedAxis_HardensBankWaveAndRaisesConvert()
        {
            var sim = FaucetOn(out _);
            Endure(sim, FaucetLoop.MarkAfterWaves);
            sim.TryArmFaucetShutoff();
            Assert.IsTrue(sim.TryPickFaucetAxis(FaucetAxis.Density));

            WaveSpec started = default;
            sim.WaveStarted += s => started = s;
            int carryWaves = sim.WavesSinceBank;
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.IsFalse(sim.HasPendingFaucetAxis); // consumed by the launch

            int carry = HatchBank.ClampCarry(carryWaves);
            float expectMul = HatchBank.CarryConvert(carry) + FaucetLoop.ConvertBonus;
            Assert.AreEqual(expectMul, sim.HatchConvertMultiplier, 1e-5f);

            var director = new WaveDirector();
            WaveSpec baseline = HatchBank.ApplyHatchComposition(
                director.NextWave(HatchBank.CarryWaveIndex(carry), sim.Siphon.Level, 1), carry);
            WaveSpec expected = FaucetLoop.ApplyAxis(baseline, FaucetAxis.Density);
            Assert.AreEqual(expected.defaultCount, started.defaultCount);
            Assert.AreEqual(expected.enemyCount, started.enemyCount);
            Assert.Greater(started.defaultCount, baseline.defaultCount);
        }

        [Test]
        public void ApplyAxis_KeepsTheSpecInvariant_AndFloorsTempo()
        {
            var director = new WaveDirector();
            WaveSpec spec = director.NextWave(2, 0, 1);

            WaveSpec dense = FaucetLoop.ApplyAxis(spec, FaucetAxis.Density);
            Assert.AreEqual(dense.enemyCount,
                dense.defaultCount + dense.hunterCount + dense.thiefCount + dense.cloakerCount);

            WaveSpec tough = FaucetLoop.ApplyAxis(spec, FaucetAxis.Toughness);
            Assert.AreEqual(spec.enemyHealthMultiplier * FaucetLoop.ToughnessHpMul,
                tough.enemyHealthMultiplier, 1e-5f);

            WaveSpec fast = spec;
            fast.spawnInterval = WaveDirector.MinSpawnInterval;
            fast = FaucetLoop.ApplyAxis(fast, FaucetAxis.Tempo);
            Assert.AreEqual(WaveDirector.MinSpawnInterval, fast.spawnInterval, 1e-5f);
        }

        [Test]
        public void Faucet_FieldWave_DamagesHatchAndAttritsUnbanked()
        {
            var sim = FaucetOn(out _);
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 4);
            sim.NotifyAssaultCleared();
            // Attrition needs a pile to bite. Since 2026-08-25 one exists only where the
            // player deliberately withdrew it, so the bet is placed at the full Hatch first
            // and the Hatch is hit after — which is the order the rule is really about.
            RunEconomyFixture.Stake(sim, earned);

            long before = sim.Unbanked;
            Assert.Greater(before, 0L, "there is a stake for the attrition to take from");
            float hpBefore = sim.Ship.Hatch.Hp;
            sim.DamageHatch(10f);
            Assert.Less(sim.Ship.Hatch.Hp, hpBefore);
            Assert.Less(sim.Unbanked, before);

            // 2026-08-27: the toggle no longer decides this. A plain field Wave damages the
            // Hatch too, so what the faucet still owns is TARGETING (FaucetHatchHunt suppresses
            // sight-provoke so the stream stays on the Hatch), not invulnerability. Asserted
            // rather than deleted: this line used to be the only pin distinguishing the two,
            // and a silent deletion would leave the faucet's actual remaining difference
            // untested.
            var plain = Tethered(out _);
            plain.TriggerDraw();
            float plainBefore = plain.Ship.Hatch.Hp;
            plain.DamageHatch(10f);
            Assert.AreEqual(plainBefore - 10f * HatchBank.FieldWaveDamageScale,
                plain.Ship.Hatch.Hp, 1e-5f,
                "a plain field Wave left the Hatch invulnerable");
            Assert.IsFalse(plain.FaucetHatchHunt,
                "the plain sim is running the faucet hunt, so this proves nothing about it");
        }

        [Test]
        public void Faucet_CycleResetsWhenTheRunEnds()
        {
            var sim = FaucetOn(out _);
            Endure(sim, 1);
            Assert.IsTrue(sim.FaucetCycleLive);
            sim.NotifyPlayerDown();
            Assert.IsFalse(sim.FaucetCycleLive);
            Assert.IsFalse(sim.FaucetAwaitingDraft);
        }
    }
}
