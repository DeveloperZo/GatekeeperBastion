using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class WavePlayerCountTests
    {
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
                Assert.AreEqual(a.ramCount, b.ramCount, "rams wave " + w);
            }
            Assert.AreEqual(18, director.NextWave(0, 0, 1).enemyCount);
            Assert.AreEqual(120, director.NextWave(9, 0, 1).enemyCount);
        }

        [Test]
        public void NextWave_PlayerCount3_TriplesBodies_HuntersUnscaled()
        {
            var director = new WaveDirector();
            var solo = director.NextWave(0, 0, 1);
            var three = director.NextWave(0, 0, 3);
            Assert.AreEqual(18, solo.enemyCount);
            Assert.AreEqual(54, three.enemyCount);
            Assert.AreEqual(solo.hunterCount, three.hunterCount);
            Assert.AreEqual(solo.ramCount, three.ramCount);
            Assert.AreEqual(54 - three.hunterCount - three.ramCount, three.defaultCount);
        }

        [Test]
        public void NextWave_PlayerCount3_CapScales()
        {
            var director = new WaveDirector();
            Assert.AreEqual(120, director.NextWave(9, 0, 1).enemyCount);
            Assert.AreEqual(360, director.NextWave(9, 0, 3).enemyCount);
        }
    }
}
