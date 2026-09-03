using System;
using System.Collections.Generic;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M116 lever 2: waves are SHAPES — Mass (all Default), Pack (Hunters, a few
    /// Cloakers from wave 4), Convoy (Thieves and Cloakers) — shares of the unchanged
    /// <c>11 + 7i</c> roster with Default the remainder, the debut gates kept as the per-wave
    /// caps in WaveDirector, and the dial's and the party's surplus still all Default. The
    /// shape-by-wave map is 1 Mass, 2 Pack, 3 Mass, 4 Pack, 5 Convoy; the spawner's queue is
    /// grouped by kind. Every count here is DERIVED from the shares and the roster constants,
    /// so a retune of either moves the expectation with it; the only literals are the shares'
    /// declaration pins and the map.</summary>
    public class WaveShapesTests
    {
        static int Roster(int waveIndex)
        {
            return Math.Min(WaveDirector.MaxEnemyCount,
                            WaveDirector.BaseEnemyCount + WaveDirector.CountPerWave * waveIndex);
        }

        static int Share(float share, int bodies)
        {
            return (int)Math.Round(Math.Round((double)share * bodies, 4, MidpointRounding.AwayFromZero),
                                   MidpointRounding.AwayFromZero);
        }

        static int Sum(WaveSpec s)
        {
            return s.defaultCount + s.hunterCount + s.thiefCount + s.cloakerCount;
        }

        [Test]
        public void TheMap_IsMassPackMassPackConvoy()
        {
            Assert.AreEqual(WaveShape.Mass, WaveShapes.For(1));
            Assert.AreEqual(WaveShape.Pack, WaveShapes.For(2), "zone 1's last wave: the Thief is not legal yet");
            Assert.AreEqual(WaveShape.Mass, WaveShapes.For(3));
            Assert.AreEqual(WaveShape.Pack, WaveShapes.For(4));
            Assert.AreEqual(WaveShape.Convoy, WaveShapes.For(5), "zone 2's last wave is the heavy draw");
            // Every zone's opening starts on a Mass and its last wave is never a Mass.
            for (int zone = 1; zone <= DemoZones.ZoneCount; zone++)
            {
                Assert.AreEqual(WaveShape.Mass, WaveShapes.For(DemoZones.FirstWave(zone)), "zone " + zone + " opens on a Mass");
                Assert.AreNotEqual(WaveShape.Mass, WaveShapes.For(DemoZones.EndWave(zone)), "zone " + zone + " ends on a shape");
            }
            // Off the table the shapes alternate and never draw a Convoy — the finale pins its own.
            for (int w = WaveDirector.MaxWaves + 1; w < 40; w++)
            {
                Assert.AreNotEqual(WaveShape.Convoy, WaveShapes.For(w), "wave " + w);
                Assert.AreNotEqual(WaveShapes.For(w), WaveShapes.For(w + 1), "wave " + w + " repeats its neighbour");
            }
            Assert.AreEqual(WaveShapes.For(1), WaveShapes.For(0), "below 1 clamps to wave 1");
        }

        [Test]
        public void Names_AreThePlayerFacingWords()
        {
            Assert.AreEqual("Mass", WaveShapes.Name(WaveShape.Mass));
            Assert.AreEqual("Pack", WaveShapes.Name(WaveShape.Pack));
            Assert.AreEqual("Convoy", WaveShapes.Name(WaveShape.Convoy));
        }

        [Test]
        public void Shares_AreTheLockedTable()
        {
            // The declaration pins (Decisions.md, "waves differ by rhythm, shape and finale").
            Assert.AreEqual(0.35f, WaveShapes.PackHunterShare, 1e-6f);
            Assert.AreEqual(0.05f, WaveShapes.PackCloakerShare, 1e-6f);
            Assert.AreEqual(0.15f, WaveShapes.ConvoyThiefShare, 1e-6f);
            Assert.AreEqual(0.15f, WaveShapes.ConvoyCloakerShare, 1e-6f);
        }

        [Test]
        public void EveryWave_FourCountsSumToEnemyCount_AtEveryDialAndPartySize()
        {
            var director = new WaveDirector();
            int[] probes = { 0, 1, 2, 3, 4, 5, 6, 9, 40, 1000 };
            foreach (int w in probes)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                    for (int players = 1; players <= WaveDirector.SupportedPlayerCount; players++)
                    {
                        var spec = director.NextWave(w, level, players);
                        Assert.AreEqual(spec.enemyCount, Sum(spec),
                            "wave " + w + " dial " + level + " players " + players);
                        Assert.IsTrue(spec.defaultCount >= 0 && spec.hunterCount >= 0
                            && spec.thiefCount >= 0 && spec.cloakerCount >= 0,
                            "no negative count, wave " + w);
                    }
        }

        [Test]
        public void TheShapes_AreWhatTheirNamesSay()
        {
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                switch (WaveShapes.For(w + 1))
                {
                    case WaveShape.Mass:
                        Assert.AreEqual(spec.enemyCount, spec.defaultCount, "a Mass is all Default, wave " + (w + 1));
                        break;
                    case WaveShape.Pack:
                        Assert.Greater(spec.hunterCount, 0, "a Pack has Hunters, wave " + (w + 1));
                        Assert.AreEqual(0, spec.thiefCount, "a Pack has no Thief, wave " + (w + 1));
                        break;
                    case WaveShape.Convoy:
                        Assert.Greater(spec.thiefCount, 0, "a Convoy has Thieves, wave " + (w + 1));
                        Assert.Greater(spec.cloakerCount, 0, "a Convoy has Cloakers, wave " + (w + 1));
                        Assert.AreEqual(0, spec.hunterCount, "a Convoy has no Hunter, wave " + (w + 1));
                        break;
                }
            }
        }

        [Test]
        public void TheCounts_AreRoundedSharesOfTheRoster_AndDefaultIsTheRemainder()
        {
            // The cap never bites on the authored table: every non-Default count is exactly its
            // share of the dial-0 solo roster, gated only by the debut.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                int roster = Roster(w);
                Assert.AreEqual(roster, spec.enemyCount, "dial 0 solo IS the roster, wave " + (w + 1));
                float hunter = 0f, thief = 0f, cloaker = 0f;
                switch (WaveShapes.For(w + 1))
                {
                    case WaveShape.Pack: hunter = WaveShapes.PackHunterShare; cloaker = WaveShapes.PackCloakerShare; break;
                    case WaveShape.Convoy: thief = WaveShapes.ConvoyThiefShare; cloaker = WaveShapes.ConvoyCloakerShare; break;
                }
                int wantHunter = w + 1 >= 2 ? Share(hunter, roster) : 0;
                int wantThief = w + 1 >= 3 ? Share(thief, roster) : 0;
                int wantCloaker = w + 1 >= 4 ? Share(cloaker, roster) : 0;
                Assert.AreEqual(wantHunter, spec.hunterCount, "hunters, wave " + (w + 1));
                Assert.AreEqual(wantThief, spec.thiefCount, "thieves, wave " + (w + 1));
                Assert.AreEqual(wantCloaker, spec.cloakerCount, "cloakers, wave " + (w + 1));
                Assert.AreEqual(roster - wantHunter - wantThief - wantCloaker, spec.defaultCount,
                    "Default is the remainder, wave " + (w + 1));
            }
        }

        [Test]
        public void TheDebutGates_Hold_ForEveryShapeAtEveryWaveDialAndPartySize()
        {
            // No Thief before wave 3, no Cloaker before wave 4, no Hunter before wave 2 — through
            // NextWave, and through Apply with EVERY shape forced onto EVERY wave, so a future
            // map cannot smuggle a kind past its gate.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                    for (int players = 1; players <= WaveDirector.SupportedPlayerCount; players++)
                    {
                        var spec = director.NextWave(w, level, players);
                        if (w + 1 < 2) Assert.AreEqual(0, spec.hunterCount, "Hunter before wave 2");
                        if (w + 1 < 3) Assert.AreEqual(0, spec.thiefCount, "Thief before wave 3 (wave " + (w + 1) + ")");
                        if (w + 1 < 4) Assert.AreEqual(0, spec.cloakerCount, "Cloaker before wave 4 (wave " + (w + 1) + ")");
                        foreach (WaveShape shape in Enum.GetValues(typeof(WaveShape)))
                        {
                            var forced = WaveShapes.Apply(spec, shape, w);
                            Assert.AreEqual(forced.enemyCount, Sum(forced), shape + " wave " + (w + 1));
                            if (w + 1 < 2) Assert.AreEqual(0, forced.hunterCount, shape + " Hunter before wave 2");
                            if (w + 1 < 3) Assert.AreEqual(0, forced.thiefCount, shape + " Thief before wave 3");
                            if (w + 1 < 4) Assert.AreEqual(0, forced.cloakerCount, shape + " Cloaker before wave 4");
                            Assert.LessOrEqual(forced.hunterCount, WaveDirector.KindCap(GatekeeperKind.Hunter, w));
                            Assert.LessOrEqual(forced.thiefCount, WaveDirector.KindCap(GatekeeperKind.Thief, w));
                            Assert.LessOrEqual(forced.cloakerCount, WaveDirector.KindCap(GatekeeperKind.Cloaker, w));
                        }
                    }
        }

        [Test]
        public void TheCaps_AreTheGateAndTheCeiling_NeverBitingOnAnAuthoredShape()
        {
            // 0 before the debut; after it, the largest share any shape gives the kind, so a
            // legal shape is never clipped and the cap row is not a second composition table.
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                int roster = Roster(w);
                int hunterCap = WaveDirector.KindCap(GatekeeperKind.Hunter, w);
                int thiefCap = WaveDirector.KindCap(GatekeeperKind.Thief, w);
                int cloakerCap = WaveDirector.KindCap(GatekeeperKind.Cloaker, w);
                Assert.AreEqual(w + 1 >= 2 ? Share(WaveShapes.PackHunterShare, roster) : 0, hunterCap, "Hunter cap, wave " + (w + 1));
                Assert.AreEqual(w + 1 >= 3 ? Share(WaveShapes.ConvoyThiefShare, roster) : 0, thiefCap, "Thief cap, wave " + (w + 1));
                float cloakerShare = Math.Max(WaveShapes.PackCloakerShare, WaveShapes.ConvoyCloakerShare);
                Assert.AreEqual(w + 1 >= 4 ? Share(cloakerShare, roster) : 0, cloakerCap, "Cloaker cap, wave " + (w + 1));
            }
            Assert.AreEqual(int.MaxValue, WaveDirector.KindCap(GatekeeperKind.Default, 0), "Default is uncapped");
            Assert.AreEqual(WaveDirector.KindCap(GatekeeperKind.Thief, WaveDirector.MaxWaves - 1),
                WaveDirector.KindCap(GatekeeperKind.Thief, 1000), "probe indices hold the last row");
        }

        [Test]
        public void TheDialAndThePartyBuyDefaultsOnly()
        {
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var atZero = director.NextWave(w, 0, 1);
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                    for (int players = 1; players <= WaveDirector.SupportedPlayerCount; players++)
                    {
                        var spec = director.NextWave(w, level, players);
                        int surplus = spec.enemyCount - atZero.enemyCount;
                        Assert.GreaterOrEqual(surplus, 0);
                        Assert.AreEqual(atZero.hunterCount, spec.hunterCount, "hunters wave " + w + " dial " + level + " players " + players);
                        Assert.AreEqual(atZero.thiefCount, spec.thiefCount, "thieves wave " + w + " dial " + level + " players " + players);
                        Assert.AreEqual(atZero.cloakerCount, spec.cloakerCount, "cloakers wave " + w + " dial " + level + " players " + players);
                        Assert.AreEqual(atZero.defaultCount + surplus, spec.defaultCount, "the whole surplus is Default");
                    }
            }
        }

        [Test]
        public void OneDebutPerWave_StillHolds()
        {
            // Hunter on 2, Cloaker on 4 (the Pack's share), Thief on 5 (the Convoy): never two
            // new rules in one wave.
            var director = new WaveDirector();
            bool hunterSeen = false, thiefSeen = false, cloakerSeen = false;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                int debuts = 0;
                if (!hunterSeen && spec.hunterCount > 0) { hunterSeen = true; debuts++; }
                if (!thiefSeen && spec.thiefCount > 0) { thiefSeen = true; debuts++; }
                if (!cloakerSeen && spec.cloakerCount > 0) { cloakerSeen = true; debuts++; }
                Assert.LessOrEqual(debuts, 1, "two kinds debut on wave " + (w + 1));
            }
            Assert.IsTrue(hunterSeen && thiefSeen && cloakerSeen, "every kind appears inside the five waves");
        }

        // ---- The spawner's queue: groups authored by kind ----

        static List<GatekeeperKind> Queue(WaveSpec spec, Random rng)
        {
            var q = new List<GatekeeperKind>();
            WaveShapes.GroupByKind(spec, rng, q);
            return q;
        }

        static bool OneKind(List<GatekeeperKind> q, int start, int width)
        {
            for (int i = start + 1; i < start + width && i < q.Count; i++)
                if (q[i] != q[start]) return false;
            return true;
        }

        [Test]
        public void GroupByKind_HoldsTheFourCounts_AndFillsEveryGroupWithOneKindWhereItCan()
        {
            var director = new WaveDirector();
            for (int seed = 1; seed <= 12; seed++)
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                    for (int players = 1; players <= WaveDirector.SupportedPlayerCount; players++)
                    {
                        var spec = director.NextWave(w, 0, players);
                        var q = Queue(spec, new Random(seed));
                        Assert.AreEqual(spec.enemyCount, q.Count, "the queue is the wave, wave " + (w + 1));
                        int d = 0, h = 0, t = 0, c = 0;
                        foreach (var k in q)
                        {
                            if (k == GatekeeperKind.Default) d++;
                            else if (k == GatekeeperKind.Hunter) h++;
                            else if (k == GatekeeperKind.Thief) t++;
                            else c++;
                        }
                        Assert.AreEqual(spec.defaultCount, d);
                        Assert.AreEqual(spec.hunterCount, h);
                        Assert.AreEqual(spec.thiefCount, t);
                        Assert.AreEqual(spec.cloakerCount, c);

                        // Every full group of a non-Default kind is that kind alone — the
                        // authored shapes never run short of Defaults to fill with, so a mixed
                        // group can only be a kind's short last group topped up with Defaults.
                        int width = spec.groupSize;
                        for (int g = 0; g * width < q.Count; g++)
                        {
                            int start = g * width;
                            if (OneKind(q, start, width)) continue;
                            int nonDefault = 0;
                            GatekeeperKind lead = GatekeeperKind.Default;
                            for (int i = start; i < start + width && i < q.Count; i++)
                                if (q[i] != GatekeeperKind.Default) { nonDefault++; lead = q[i]; }
                            for (int i = start; i < start + width && i < q.Count; i++)
                                Assert.IsTrue(q[i] == GatekeeperKind.Default || q[i] == lead,
                                    "group " + (g + 1) + " mixes two non-Default kinds, wave " + (w + 1) + " seed " + seed);
                            Assert.Less(nonDefault, width, "a mixed group is a kind's short remainder, wave " + (w + 1));
                        }
                    }
        }

        [Test]
        public void GroupByKind_PackHuntersArriveAsHunterGroups_NotOneHunterAmongDefaults()
        {
            // Wave 2 at dial 0: the Hunters fill at least one whole group on their own.
            var spec = new WaveDirector().NextWave(1, 0);
            Assert.AreEqual(WaveShape.Pack, WaveShapes.For(2));
            Assert.GreaterOrEqual(spec.hunterCount, spec.groupSize, "the share must fill a group or the shape cannot be read at the door");
            var q = Queue(spec, new Random(3));
            int hunterGroups = 0;
            for (int g = 0; g * spec.groupSize < q.Count; g++)
            {
                int start = g * spec.groupSize;
                if (q[start] == GatekeeperKind.Hunter && OneKind(q, start, spec.groupSize)) hunterGroups++;
            }
            Assert.AreEqual(spec.hunterCount / spec.groupSize, hunterGroups, "whole Hunter groups");
        }

        [Test]
        public void GroupByKind_IsSeeded_AndTheShortTailStaysLast()
        {
            var spec = new WaveDirector().NextWave(4, 0);
            var a = Queue(spec, new Random(11));
            var b = Queue(spec, new Random(11));
            CollectionAssert.AreEqual(a, b, "same seed, same queue");
            int tail = spec.enemyCount % spec.groupSize;
            Assert.Greater(tail, 0, "wave 5 has a short last group, or this test pins nothing");
            bool differs = false;
            for (int seed = 1; seed <= 20 && !differs; seed++)
            {
                var c = Queue(spec, new Random(seed));
                for (int i = 0; i < c.Count; i++) if (c[i] != a[i]) { differs = true; break; }
            }
            Assert.IsTrue(differs, "the group order never moves with the seed");
            // Unshuffled build order: the tail is the last Defaults, and a null rng leaves it.
            var plain = Queue(spec, null);
            for (int i = plain.Count - tail; i < plain.Count; i++)
                Assert.AreEqual(GatekeeperKind.Default, plain[i], "the short tail is Defaults");
            var seeded = Queue(spec, new Random(5));
            for (int i = seeded.Count - tail; i < seeded.Count; i++)
                Assert.AreEqual(GatekeeperKind.Default, seeded[i], "the shuffle must not move the short tail");
        }
    }
}
