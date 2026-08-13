using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>
    /// The two-runs-in-one-process gate. Two independent Sim runs — each a RunContext with its
    /// own MetaState and RunSim — advance in the same process with different command sequences
    /// and must not touch each other. "Run simulation" here means Sim-owned state only; the
    /// Game layer (MonoBehaviours, NavMesh, spawned objects) is covered by greps and the
    /// written tick order, not by this test.
    ///
    /// Three teeth, so the gate cannot be satisfied by construction alone:
    ///   1. The advance goes through the deep-start chain — the only Tick branch with logic —
    ///      so wave indexes diverge because the chain advanced, not because two floats differ.
    ///   2. Replaying a seed reproduces its run exactly, even when the original was interleaved
    ///      with a second, noisy run. This is the assertion that fails the moment an ambient
    ///      Random or an ambient clock creeps back into the sim.
    ///   3. The Sim assembly holds no static mutable state at all, checked by reflection. This
    ///      is the standing guard against the next Settings-shaped leak, and the tooth that was
    ///      red before the Settings statics moved out of Sim.
    /// </summary>
    public class TwoRunIsolationTests
    {
        /// <summary>Deterministic snapshot of everything the gate compares.</summary>
        private struct RunSnapshot
        {
            public RunPhase Phase;
            public AssaultState Assault;
            public int WaveIndex;
            public float RunElapsed;
            public float HatchHp;
            public long RunSalvage;
            public long Unbanked;
            public long Banked;
            public int Deepest;
            public int NextRngDraw;
            public bool DecoyLive;
            public float DecoyHp;

            public static RunSnapshot Of(RunContext ctx)
            {
                return new RunSnapshot
                {
                    Phase = ctx.Sim.Phase,
                    Assault = ctx.Sim.Assault,
                    WaveIndex = ctx.Sim.CurrentWaveIndex,
                    RunElapsed = ctx.Sim.RunElapsed,
                    HatchHp = ctx.Sim.Ship.Hatch.Hp,
                    RunSalvage = ctx.Sim.Salvage.RunSalvage,
                    Unbanked = ctx.Sim.Unbanked,
                    Banked = ctx.Meta.BankedSalvage,
                    Deepest = ctx.Meta.DeepestWaveReached,
                    NextRngDraw = ctx.Sim.Rng.Next(),
                    DecoyLive = ctx.Decoy.IsLive,
                    DecoyHp = ctx.Decoy.Hp
                };
            }
        }

        private static RunContext DeepStartContext(int seed)
        {
            var meta = MetaState.CreateNew();
            meta.DeepestWaveReached = 3; // earned in a prior run; lets StartRun(3) chain 1..3
            return new RunContext(meta, seed);
        }

        /// <summary>
        /// The scripted command sequence for the deep-start run: board, chain waves 1..3 through
        /// delivery notify and the inter-wave timer, take hatch damage, kill four, place and
        /// damage the decoy, clear. Interleavable: the caller may advance other contexts between
        /// steps. Consumes Rng draws at fixed points the way the spawner does, so replay
        /// equality also proves the random stream belongs to this context alone.
        /// </summary>
        private static void DriveDeepStartRun(RunContext ctx, Action interleave)
        {
            var sim = ctx.Sim;
            sim.StartRun(3);
            sim.EngageTether();
            interleave();

            sim.TriggerDraw(); // wave 1 launches; chain 1..3 begins, delivery pending
            Assert.AreEqual(1, sim.CurrentWaveIndex, "deep start launches wave 1 first");
            int draw1 = sim.Rng.Next(100); // stand-in for the spawner's placement draws
            interleave();

            sim.NotifyDeliveryComplete(); // starts the inter-wave gap toward wave 2
            sim.Tick(1.0f);
            Assert.AreEqual(1, sim.CurrentWaveIndex, "gap not elapsed — wave 2 must not launch");
            interleave();
            sim.Tick(2.5f); // gap (3s) elapses inside this tick
            Assert.AreEqual(2, sim.CurrentWaveIndex, "the chain, not the clock, advanced the index");
            sim.Rng.Next(100);
            interleave();

            sim.NotifyDeliveryComplete();
            sim.Tick(3.5f); // wave 3 launches; chain complete
            Assert.AreEqual(3, sim.CurrentWaveIndex);
            sim.Rng.Next(100);
            interleave();

            ctx.Decoy.ResetForRun(PayloadId.Decoy);
            string reason;
            if (ctx.Decoy.ChargesRemaining > 0)
            {
                Assert.IsTrue(ctx.Decoy.TryPlace(1f, 0f, 2f, out reason), "decoy placement refused: " + reason);
                ctx.Decoy.TakeDamage(15f);
            }
            sim.Ship.Hatch.Damage(40f);
            for (int i = 0; i < 4; i++) sim.AwardKill();
            interleave();

            sim.NotifyAssaultCleared(); // chain fully delivered — back to Prep
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
        }

        /// <summary>The second, deliberately different run: normal start, siphon up, two kills,
        /// no repair, still mid-assault when the first run has already cleared its chain.</summary>
        private static void DriveNoisyRun(RunContext ctx)
        {
            var sim = ctx.Sim;
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(3);
            sim.TriggerDraw();
            sim.Rng.Next(100);
            sim.Tick(0.7f);
            sim.Ship.Hatch.Damage(90f);
            sim.AwardKill();
            sim.AwardKill();
        }

        [Test]
        public void TwoContexts_AdvanceInterleaved_ThroughTheDeepStartChain_WithNoCrossTalk()
        {
            var a = DeepStartContext(101);
            var b = new RunContext(MetaState.CreateNew(), 202);

            Assert.IsFalse(ReferenceEquals(a.Meta, b.Meta), "each context owns its MetaState");
            Assert.IsFalse(ReferenceEquals(a.Sim, b.Sim));
            Assert.IsFalse(ReferenceEquals(a.Decoy, b.Decoy));

            // Drive B in slices BETWEEN A's steps, so the two runs genuinely interleave in one
            // process rather than running back to back.
            var bSteps = new Queue<Action>(new Action[]
            {
                () => { b.Sim.StartRun(); b.Sim.EngageTether(); },
                () => { b.Sim.SetSiphonLevel(3); b.Sim.TriggerDraw(); b.Sim.Rng.Next(100); },
                () => { b.Sim.Tick(0.7f); b.Sim.Ship.Hatch.Damage(90f); },
                () => { b.Sim.AwardKill(); b.Sim.AwardKill(); },
                () => { b.Sim.Tick(0.3f); },
                () => { }
            });
            DriveDeepStartRun(a, () => bSteps.Dequeue()());

            // A cleared its chain: wave 3 delivered, back in Prep, four kills banked at dial 0.
            Assert.AreEqual(RunPhase.Tethered, a.Sim.Phase);
            Assert.AreEqual(AssaultState.Prep, a.Sim.Assault);
            Assert.AreEqual(3, a.Sim.CurrentWaveIndex);
            Assert.AreEqual(3, a.Meta.DeepestWaveReached);
            Assert.AreEqual(100L, a.Sim.Unbanked, "4 kills x 25 at Scavenging 0, siphon 0");
            Assert.AreEqual(0L, a.Meta.BankedSalvage);
            Assert.AreEqual(a.Sim.Ship.Hatch.MaxHp - 40f, a.Sim.Ship.Hatch.Hp, 1e-4f);
            Assert.IsTrue(a.Decoy.IsLive);

            // B is still mid-assault on wave 1 with its own numbers.
            Assert.AreEqual(RunPhase.Tethered, b.Sim.Phase);
            Assert.AreEqual(AssaultState.Assault, b.Sim.Assault);
            Assert.AreEqual(1, b.Sim.CurrentWaveIndex);
            Assert.AreEqual(1, b.Meta.DeepestWaveReached, "B never cleared anything");
            Assert.AreEqual(62L, b.Sim.Unbanked, "2 kills x floor(25 x 1.25 siphon-3) = 2 x 31");
            Assert.AreEqual(0L, b.Meta.BankedSalvage);
            Assert.AreEqual(b.Sim.Ship.Hatch.MaxHp - 90f, b.Sim.Ship.Hatch.Hp, 1e-4f);
            Assert.IsFalse(b.Decoy.IsLive, "B never armed a decoy");

            // No cross-talk in either direction.
            Assert.AreEqual(1, a.Meta.TotalRuns);
            Assert.AreEqual(1, b.Meta.TotalRuns);
            Assert.AreNotEqual(a.Sim.CurrentWaveIndex, b.Sim.CurrentWaveIndex);
            Assert.AreNotEqual(a.Sim.Assault, b.Sim.Assault);
            Assert.AreNotEqual(a.Sim.Unbanked, b.Sim.Unbanked);
        }

        [Test]
        public void ReplayingASeed_ReproducesItsRunExactly_DespiteAnInterleavedSecondRun()
        {
            // First pass: the deep-start run interleaved with a noisy neighbour.
            var a = DeepStartContext(101);
            var noise = new RunContext(MetaState.CreateNew(), 202);
            var noiseSteps = new Queue<Action>(new Action[]
            {
                () => DriveNoisyRun(noise),
                () => noise.Sim.Tick(0.9f),
                () => noise.Sim.AwardKill(),
                () => noise.Sim.Rng.Next(),
                () => noise.Sim.Ship.Hatch.Damage(5f),
                () => { }
            });
            DriveDeepStartRun(a, () => noiseSteps.Dequeue()());

            // Second pass: same seed, same commands, alone in the world.
            var replay = DeepStartContext(101);
            DriveDeepStartRun(replay, () => { });

            RunSnapshot first = RunSnapshot.Of(a);
            RunSnapshot second = RunSnapshot.Of(replay);

            Assert.AreEqual(first.Phase, second.Phase);
            Assert.AreEqual(first.Assault, second.Assault);
            Assert.AreEqual(first.WaveIndex, second.WaveIndex);
            Assert.AreEqual(first.RunElapsed, second.RunElapsed, "same ticks, same order — exact");
            Assert.AreEqual(first.HatchHp, second.HatchHp);
            Assert.AreEqual(first.RunSalvage, second.RunSalvage);
            Assert.AreEqual(first.Unbanked, second.Unbanked);
            Assert.AreEqual(first.Banked, second.Banked);
            Assert.AreEqual(first.Deepest, second.Deepest);
            Assert.AreEqual(first.NextRngDraw, second.NextRngDraw,
                "the random stream is the context's own — an ambient draw anywhere breaks this");
            Assert.AreEqual(first.DecoyLive, second.DecoyLive);
            Assert.AreEqual(first.DecoyHp, second.DecoyHp);
        }

        [Test]
        public void SimAssembly_HoldsNoStaticMutableState()
        {
            Assert.AreEqual("GatekeeperBastion.Sim", typeof(RunSim).Assembly.GetName().Name,
                "RunSim must live in its own Sim assembly — the asmdef is the fence");

            var violations = new List<string>();
            foreach (Type type in typeof(RunSim).Assembly.GetTypes())
            {
                // Everything of ours in the Sim assembly, whatever its namespace (Log rides
                // along under GatekeeperBastion.Util). The prefix filter drops the harness's
                // UnityEngine / NUnit stub types, which only exist in the offline build.
                if (type.Namespace == null || !type.Namespace.StartsWith("GatekeeperBastion"))
                    continue;
                if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    continue; // compiler lambda caches are not designer state

                foreach (FieldInfo f in type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsLiteral) continue;      // const
                    if (f.IsInitOnly) continue;     // static readonly tables are fine and stay
                    if (f.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;
                    violations.Add(type.FullName + "." + f.Name);
                }
            }

            Assert.AreEqual(0, violations.Count,
                "Sim/ holds static mutable state — the shape of the next isolation leak:\n  " +
                string.Join("\n  ", violations.ToArray()));
        }

        [Test]
        public void RunContext_HasNoStaticMembers_SoNoAmbientInstanceCanReturn()
        {
            var offenders = new List<string>();
            foreach (MemberInfo m in typeof(RunContext).GetMembers(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (m is ConstructorInfo) continue; // static ctor only exists if statics do; fields catch that
                offenders.Add(m.Name);
            }
            Assert.AreEqual(0, offenders.Count,
                "RunContext grew static members — a RunContext.Instance (or equivalent) is exactly " +
                "what this gate exists to keep out:\n  " + string.Join("\n  ", offenders.ToArray()));
        }
    }
}
