using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>Every UpgradeTrack value has a label, a language.md station, and finite
    /// level-0 / MaxLevel values — the table the docgen tracks emitter reads.</summary>
    public class UpgradeTrackTableTests
    {
        static readonly string[] Stations =
        {
            "ARMORY", "CREW QUARTERS", "FABRICATION", "NAV MAP"
        };

        [Test]
        public void EveryTrackHasLabelStationAndValues()
        {
            Assert.Greater(UpgradeTracks.All.Length, 0);
            foreach (var track in UpgradeTracks.All)
            {
                string label = UpgradeTracks.Label(track);
                Assert.IsFalse(string.IsNullOrEmpty(label), track + " label");

                string station = UpgradeTracks.Station(track);
                Assert.IsTrue(System.Array.IndexOf(Stations, station) >= 0,
                    track + " station '" + station + "' not in language.md");

                var at0 = UpgradeTracks.ValueAt(track, 0);
                var atMax = UpgradeTracks.ValueAt(track, MetaUpgrades.MaxLevel);
                Assert.IsFalse(float.IsNaN(at0.Value) || float.IsInfinity(at0.Value),
                    track + " L0");
                Assert.IsFalse(float.IsNaN(atMax.Value) || float.IsInfinity(atMax.Value),
                    track + " L" + MetaUpgrades.MaxLevel);
            }
        }
    }
}
