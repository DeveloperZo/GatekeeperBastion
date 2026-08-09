using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>M22 criterion 2: AwardKill floors once; RunSalvage equals the sum banked.</summary>
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
        public void MixedKills_AtFractionalDial_RunTallyEqualsBankedAwards()
        {
            // Dial 3 → CurrentKillValue = 25 * 1.25 = 31.25. Floor once → 31 per kill.
            // Pre-M22 the tally kept 31.25 and the bank took 31; after N kills they drifted by N*0.25.
            var meta = new MetaState();
            long bankedBefore = meta.BankedSalvage;
            var sim = LiveAssault(meta, siphonLevel: 3);

            const int kills = 17;
            for (int i = 0; i < kills; i++) sim.AwardKill();

            long expected = kills * 31L; // floor(31.25) each
            Assert.AreEqual(31.25f, sim.CurrentKillValue, 1e-5f);
            Assert.AreEqual(expected, sim.Salvage.RunSalvage);
            Assert.AreEqual(bankedBefore + expected, meta.BankedSalvage);
            Assert.AreEqual(sim.Salvage.RunSalvage, meta.BankedSalvage - bankedBefore,
                "RunSalvage must equal the total credited to MetaState, to the unit");
        }

        [Test]
        public void FractionalScavenging_AndDial_TallyAgreesWithBank()
        {
            // Scavenging 3 → 28.75 base; dial 3 → x1.25 → 35.9375 → floor 35.
            var meta = new MetaState();
            meta.ScavengingLevel = 3;
            var sim = LiveAssault(meta, siphonLevel: 3);
            Assert.AreEqual(35.9375f, sim.CurrentKillValue, 1e-4f);

            long before = meta.BankedSalvage;
            const int kills = 9;
            for (int i = 0; i < kills; i++) sim.AwardKill();

            long expected = kills * 35L;
            Assert.AreEqual(expected, sim.Salvage.RunSalvage);
            Assert.AreEqual(before + expected, meta.BankedSalvage);
        }
    }
}
