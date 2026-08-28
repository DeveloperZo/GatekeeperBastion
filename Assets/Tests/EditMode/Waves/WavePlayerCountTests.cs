using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The playerCount arm of NextWave: bodies scale with the party, composition does
    /// not, and the 120 cap is per player.
    ///
    /// Every figure here is DERIVED from BaseEnemyCount / CountPerWave / MaxEnemyCount. The 18s
    /// and the wave-9 cap probe used to be literals, and the 2026-08-27 roster cut (18+12i ->
    /// 11+7i) turned all four red at once — the cap probe worst of all, because wave 9 is no
    /// longer over the cap and `AreEqual(120, NextWave(9, ...))` would have had to be rewritten to
    /// some other guessed index every time the curve moved.</summary>
    public class WavePlayerCountTests
    {
        /// <summary>The dial-0 body curve, off the two constants that own it.</summary>
        private static int BaseCount(int waveIndex)
        {
            return WaveDirector.BaseEnemyCount + WaveDirector.CountPerWave * waveIndex;
        }

        /// <summary>The first wave index whose raw curve exceeds the per-player cap. Searched for
        /// rather than named, so the cap tests keep testing the cap.</summary>
        private static int FirstCappedWave()
        {
            int w = 0;
            while (BaseCount(w) <= WaveDirector.MaxEnemyCount) w++;
            return w;
        }

        [Test]
        public void NextWave_PlayerCount1_MatchesSoloTable()
        {
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var a = director.NextWave(w, 0);
                var b = director.NextWave(w, 0, 1);
                Assert.AreEqual(a.enemyCount, b.enemyCount, "wave " + w);
                Assert.AreEqual(a.hunterCount, b.hunterCount, "hunters wave " + w);
                Assert.AreEqual(a.thiefCount, b.thiefCount, "thieves wave " + w);
                Assert.AreEqual(a.cloakerCount, b.cloakerCount, "cloakers wave " + w);
            }
            Assert.AreEqual(BaseCount(0), director.NextWave(0, 0, 1).enemyCount);
            Assert.AreEqual(WaveDirector.MaxEnemyCount,
                director.NextWave(FirstCappedWave(), 0, 1).enemyCount);
        }

        [Test]
        public void NextWave_PlayerCount3_TriplesBodies_HuntersUnscaled()
        {
            var director = new WaveDirector();
            var solo = director.NextWave(0, 0, 1);
            var three = director.NextWave(0, 0, 3);
            Assert.AreEqual(BaseCount(0), solo.enemyCount);
            Assert.AreEqual(3 * solo.enemyCount, three.enemyCount,
                "three players draw three times the bodies");
            Assert.AreEqual(solo.hunterCount, three.hunterCount);
            Assert.AreEqual(solo.thiefCount, three.thiefCount);
            Assert.AreEqual(solo.cloakerCount, three.cloakerCount);
            Assert.AreEqual(three.enemyCount - three.hunterCount - three.thiefCount
                            - three.cloakerCount,
                three.defaultCount);

            // The rule behind the equalities above, said once: the whole party surplus is Default,
            // exactly as the siphon dial's surplus is.
            Assert.AreEqual(solo.defaultCount + (three.enemyCount - solo.enemyCount),
                three.defaultCount, "the party surplus is entirely Default");
        }

        [Test]
        public void NextWave_PlayerCount3_CapScales()
        {
            var director = new WaveDirector();
            int capped = FirstCappedWave();
            Assert.AreEqual(WaveDirector.MaxEnemyCount, director.NextWave(capped, 0, 1).enemyCount);
            Assert.AreEqual(3 * WaveDirector.MaxEnemyCount,
                director.NextWave(capped, 0, 3).enemyCount,
                "the cap is PER PLAYER, so a three-player draw clamps at three times it");
        }
    }
}
