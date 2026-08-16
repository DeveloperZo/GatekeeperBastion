using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>AwardKill floors once into Unbanked; ship bank does not move.</summary>
    public class SalvageDriftTests
    {
        static RunSim LiveAssault(MetaState meta, int siphonLevel)
        {
            var sim = new RunSim(meta, seed: 42);
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(siphonLevel);
            sim.TriggerDraw();
            return sim;
        }

        [Test]
        public void MixedKills_AtFractionalDial_UnbankedEqualsFlooredAwards()
        {
            var meta = new MetaState();
            long bankedBefore = meta.BankedSalvage;
            var sim = LiveAssault(meta, siphonLevel: 3);

            const int kills = 17;
            for (int i = 0; i < kills; i++) sim.AwardKill();

            long expected = kills * 31L; // floor(31.25) each
            Assert.AreEqual(31.25f, sim.CurrentKillValue, 1e-5f);
            Assert.AreEqual(expected, sim.Unbanked);
            Assert.AreEqual(bankedBefore, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);
        }

        [Test]
        public void FractionalDial_UnbankedAgreesWithFloor()
        {
            var meta = new MetaState();
            var sim = LiveAssault(meta, siphonLevel: 3);
            Assert.AreEqual(31.25f, sim.CurrentKillValue, 1e-4f);

            long before = meta.BankedSalvage;
            const int kills = 9;
            for (int i = 0; i < kills; i++) sim.AwardKill();

            long expected = kills * 31L;
            Assert.AreEqual(expected, sim.Unbanked);
            Assert.AreEqual(before, meta.BankedSalvage);
        }
    }
}
