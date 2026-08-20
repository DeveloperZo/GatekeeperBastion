using System.Linq;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The F12 demo checkpoints: each constructed state must land exactly on its
    /// beat — the gates the beat reads open, the ones it has not reached still shut, and the
    /// salvage ledger balanced (a checkpoint with a broken ledger would trip the load-time
    /// repair and stop resembling a played save). The adopt/reload/reset halves are Unity
    /// routing; these pin the pure state construction.</summary>
    public class DevCheckpointTests
    {
        [Test]
        public void UnknownDigit_BuildsNothing()
        {
            Assert.IsNull(GameController.BuildDemoCheckpoint(0));
            Assert.IsNull(GameController.BuildDemoCheckpoint(5), "digit 5 is the live reset");
            Assert.IsNull(GameController.BuildDemoCheckpoint(6));
        }

        [Test]
        public void BeatZeroDone_IsEmbarkable_AndNothingElse()
        {
            var m = GameController.BuildDemoCheckpoint(1);
            Assert.IsTrue(m.DemoActive);
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(m),
                "the whole point: skip beat 0, EMBARK is the next verb");
            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(0, m.HatchConversions, "no bank yet — the wheel stays shut");
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m));
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void FirstReturn_LandsOnTheTopOfTheBeat()
        {
            var m = GameController.BuildDemoCheckpoint(2);
            Assert.AreEqual(1, m.PodEjections);
            Assert.AreEqual(1, m.HatchConversions);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "the first bank opened the wheel");
            Assert.AreEqual(500L, m.BankedSalvage, "the authored loss-1 aim funds both buys");
            Assert.AreEqual(2, m.DemoWheelBuysLeft);
            Assert.IsFalse(m.DemoCrewMenuUnlocked,
                "unwritten on purpose — the first-return lines and the CREW write must play");
            Assert.AreEqual(FirstTerminal.ZoneOneEndWave,
                m.ZoneDeepest(FirstTerminal.ZoneOneNumber));
            Assert.AreEqual(0, m.OpenDoorIds.Length, "D1 is shut — the loss is why we are here");
            Assert.IsFalse(DemoProgress.MultiplayerUnlocked(m), "no wheel buy yet");
            Assert.IsTrue(m.SalvageLedgerBalances);

            var offer = DemoRun.RetriggerOffer(m, FirstTerminal.SecondId, 0, false);
            Assert.IsTrue(offer.Available, "the next embark meets the composite retrigger");
            Assert.IsTrue(offer.PartialAvailable);
        }

        [Test]
        public void DoorOneOpen_HasTheShopPassSpent_AndTheClaimDone()
        {
            var m = GameController.BuildDemoCheckpoint(3);
            Assert.AreEqual(0, m.DemoWheelBuysLeft);
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m));
            Assert.AreEqual(2, m.TierOf(WeaponId.Mk1Sidearm), "both Secondary buys landed");
            Assert.AreEqual(BoarderProtocolId.Default, m.EquippedProtocol);
            Assert.IsTrue(m.DemoCrewMenuUnlocked);
            Assert.AreEqual(2, m.HatchConversions, "zone 1's bank was survived");
            Assert.IsTrue(m.OpenDoorIds.Contains("D1"));
            Assert.IsFalse(m.OpenDoorIds.Contains("D2"));
            Assert.IsTrue(m.SalvageLedgerBalances);

            bool doorOpen = m.OpenDoorIds.Contains("D1");
            Assert.IsFalse(DemoRun.RetriggerOffer(m, FirstTerminal.SecondId, 0, doorOpen)
                .Available, "a surviving bank made the retrigger moot");
        }

        [Test]
        public void DoorTwoOpen_CarriesThePrimaryKit()
        {
            var m = GameController.BuildDemoCheckpoint(4);
            Assert.IsTrue(m.OpenDoorIds.Contains("D1"));
            Assert.IsTrue(m.OpenDoorIds.Contains("D2"));
            Assert.IsTrue(m.DemoZone2BankLost,
                "the bank that fell is the gate — this is what sells the rack");
            Assert.IsTrue(m.DemoRungTwoOpened,
                "the rung already opened here, so the line does not replay on this load");
            Assert.AreEqual(2, m.PodEjections,
                "bookkeeping only — endings are counted, they do not open the rack");
            Assert.IsTrue(m.Owns(WeaponId.PriC), "the Primary this checkpoint bought");
            Assert.AreEqual(WeaponId.PriC, m.EquippedPrimary);
            Assert.IsTrue(m.DemoActive, "the demo does not end until Escaped");
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void EveryCheckpoint_SurvivesTheSaveRoundTrip_Unrepaired()
        {
            // The taint keeps these off disk in play; the round trip still must hold so a
            // checkpoint behaves like a loaded save everywhere meta is re-read.
            for (int digit = 1; digit <= 4; digit++)
            {
                var m = GameController.BuildDemoCheckpoint(digit);
                var back = MetaState.FromJson(m.ToJson());
                Assert.IsFalse(back.SalvageStatsWereRepaired, "checkpoint " + digit);
                Assert.AreEqual(m.BankedSalvage, back.BankedSalvage, "checkpoint " + digit);
                Assert.AreEqual(m.HatchConversions, back.HatchConversions, "checkpoint " + digit);
                Assert.AreEqual(m.OpenDoorIds.Length, back.OpenDoorIds.Length, "checkpoint " + digit);
            }
        }
    }
}
