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
            Assert.IsNull(GameController.BuildDemoCheckpoint(9), "row 9 (I) is the stock beat");
            Assert.IsNull(GameController.BuildDemoCheckpoint(10), "row 10 (J) is the live reset");
        }

        [Test]
        public void Overlay_HasEightBeats_TheStockRow_AndTheLiveReset()
        {
            // Eight beats, then the in-place stock row, then the live reset LAST. The order is
            // load-bearing, not cosmetic: DevConsole derives the letter range (A..) from this
            // array's length AND treats the final row as the reset, so a row appended rather
            // than inserted would silently take the reset's letter.
            Assert.AreEqual(10, GameController.DemoCheckpointLabels.Length);
            Assert.AreEqual(GameController.StockDigit,
                GameController.DemoCheckpointLabels.Length - 1,
                "the stock row sits immediately before the reset");
            StringAssert.Contains("reset",
                GameController.DemoCheckpointLabels[GameController.DemoCheckpointLabels.Length - 1],
                "the live reset stays last");
            // Rows are keyed by letter, in order — the label leads with its own key cap.
            for (int i = 0; i < GameController.DemoCheckpointLabels.Length; i++)
                StringAssert.StartsWith(((char)('A' + i)).ToString(),
                    GameController.DemoCheckpointLabels[i]);
            Assert.IsTrue(GameController.IsDemoFightCheckpoint(2));
            Assert.IsTrue(GameController.IsDemoFightCheckpoint(4));
            Assert.IsTrue(GameController.IsDemoFightCheckpoint(6),
                "control-room-ready (F) is a Bastion fight beat");
            Assert.IsTrue(GameController.IsDemoFightCheckpoint(7));
            Assert.IsTrue(GameController.IsDemoFightCheckpoint(8));
            Assert.IsFalse(GameController.IsDemoFightCheckpoint(1));
            Assert.IsFalse(GameController.IsDemoFightCheckpoint(3));
            Assert.IsFalse(GameController.IsDemoFightCheckpoint(5));
            Assert.IsFalse(GameController.IsDemoFightCheckpoint(GameController.StockDigit),
                "stocking mutates the session in place and loads nothing");
        }

        [Test]
        public void EveryZoneTerminalDone_AndTheControlDoorIsOpen()
        {
            var m = GameController.BuildDemoCheckpoint(6);
            Assert.IsTrue(m.CompletedTerminalIds.Contains(DemoZones.TerminalIdAt(1, 0)));
            Assert.IsTrue(m.CompletedTerminalIds.Contains(DemoZones.TerminalIdAt(2, 0)));
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(1)));
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(2)),
                "digit 6 stands in the control room now — the spine has no third zone, so "
                + "zone 2's bank has already opened its door by this beat");
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void BeatZeroDone_IsEmbarkable_AndNothingElse()
        {
            var m = GameController.BuildDemoCheckpoint(1);
            Assert.IsTrue(m.DemoActive);
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(m),
                "the whole point: skip beat 0, EMBARK is the next verb");
            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(0L, m.LifetimeSalvageEarned, "nothing earned yet — the wheel stays shut");
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m));
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void PostZoneOneTerminals_MarksTheZoneDone_AndBanksNothing()
        {
            var m = GameController.BuildDemoCheckpoint(2);
            Assert.IsTrue(m.CompletedTerminalIds.Contains(FirstTerminal.Id));
            Assert.AreEqual(FirstTerminal.ZoneTerminalCount, m.CompletedTerminalIds.Length,
                "the checkpoint records exactly the zone's terminals — no more, no fewer");
            Assert.AreEqual(0L, m.BankedSalvage,
                "beat 1 ends before the walk home — the Run's take is seeded live, not in Meta");
            Assert.AreEqual(FirstTerminal.ZoneOneEndWave, m.ZoneDeepest(FirstTerminal.ZoneOneNumber));
            Assert.AreEqual(0, m.OpenDoorIds.Length);
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void FirstReturn_LandsOnTheTopOfTheBeat()
        {
            var m = GameController.BuildDemoCheckpoint(3);
            Assert.AreEqual(1, m.PodEjections);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m),
                "the salvage this realm has earned is what opens the wheel");
            Assert.AreEqual(500L, m.BankedSalvage, "the authored loss-1 aim funds both buys");
            Assert.IsFalse(m.DemoCrewMenuUnlocked,
                "unwritten on purpose — the first-return lines and the CREW write must play");
            Assert.AreEqual(FirstTerminal.ZoneOneEndWave,
                m.ZoneDeepest(FirstTerminal.ZoneOneNumber));
            Assert.AreEqual(0, m.OpenDoorIds.Length, "D1 is shut — the loss is why we are here");
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m),
                "armed at beat 3, so co-op is open — the MK-1 is the key since 2026-08-25");
            Assert.IsTrue(m.SalvageLedgerBalances);

            var offer = DemoRun.RetriggerOffer(m, FirstTerminal.Id, 0, false);
            Assert.IsTrue(offer.Available, "the next embark meets the composite retrigger");
            Assert.IsTrue(offer.PartialAvailable);
        }

        [Test]
        public void PostZoneTwoTerminals_HasDoorOneAndT3Done()
        {
            var m = GameController.BuildDemoCheckpoint(4);
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(1)));
            Assert.IsFalse(m.OpenDoorIds.Contains(DemoZones.DoorId(2)));
            Assert.IsTrue(m.CompletedTerminalIds.Contains(DemoZones.TerminalIdAt(2, 0)));
            Assert.AreEqual(WaveDirector.MaxWaves, m.ZoneDeepest(2));
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(m),
                "zone 2's field pay is earned here, which is past the rack's price");
            Assert.IsTrue(m.SalvageLedgerBalances);
        }

        [Test]
        public void DoorOneOpen_HasTheShopPassSpent_AndTheClaimDone()
        {
            var m = GameController.BuildZoneTwoReadyCheckpoint();
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m));
            Assert.AreEqual(2, m.TierOf(WeaponId.Mk1Sidearm), "both Secondary buys landed");
            Assert.AreEqual(BoarderProtocolId.Depot, m.EquippedProtocol);
            Assert.IsTrue(m.DemoCrewMenuUnlocked);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m),
                "standing at zone 2's mouth means zone 1 paid for something");
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(1)));
            Assert.IsFalse(m.OpenDoorIds.Contains(DemoZones.DoorId(2)));
            Assert.IsFalse(m.CompletedTerminalIds.Contains(DemoZones.TerminalIdAt(2, 0)),
                "T3 is the next fight — this is the mouth of zone 2");
            Assert.IsTrue(m.SalvageLedgerBalances);

            bool doorOpen = m.OpenDoorIds.Contains(DemoZones.DoorId(1));
            Assert.IsFalse(DemoRun.RetriggerOffer(m, FirstTerminal.Id, 0, doorOpen)
                .Available, "a surviving bank made the retrigger moot");
        }

        [Test]
        public void DoorTwoOpen_CarriesThePrimaryKit()
        {
            var m = GameController.BuildDemoCheckpoint(5);
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(1)));
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(2)));
            Assert.IsTrue(m.DemoZone2BankLost,
                "zone 2 resolved is still recorded, though the rack no longer reads it");
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
        public void MasterControl_OpensD3_WithT4T5Done()
        {
            var m = GameController.BuildDemoCheckpoint(7);
            Assert.IsTrue(m.OpenDoorIds.Contains(DemoZones.DoorId(2)));
            Assert.IsTrue(m.CompletedTerminalIds.Contains(DemoZones.TerminalIdAt(1, 0)));
            Assert.IsTrue(m.CompletedTerminalIds.Contains(DemoZones.TerminalIdAt(2, 0)));
            Assert.IsTrue(m.Owns(WeaponId.PriC));
            Assert.Greater(m.BankedSalvage, 0L, "prior banks remain; Unbanked is seeded on the Run");
            Assert.IsTrue(m.SalvageLedgerBalances);

            var finale = GameController.BuildDemoCheckpoint(8);
            Assert.AreEqual(m.OpenDoorIds.Length, finale.OpenDoorIds.Length);
            Assert.AreEqual(m.CompletedTerminalIds.Length, finale.CompletedTerminalIds.Length);
            Assert.AreEqual(m.BankedSalvage, finale.BankedSalvage,
                "beats G and H share meta; the elite pile is Unbanked on the live Run");
        }

        [Test]
        public void ConfigZero_AndOutOfRange_AreTheBeatAsAuthored()
        {
            var plain = GameController.BuildDemoCheckpoint(4);
            var configured = GameController.BuildDemoCheckpoint(4);
            GameController.ApplyCheckpointConfig(configured, 0);
            Assert.AreEqual(plain.ToSnapshotJson(), configured.ToSnapshotJson(),
                "config 0 must be a byte-for-byte no-op");
            GameController.ApplyCheckpointConfig(configured, 99);
            Assert.AreEqual(plain.ToSnapshotJson(), configured.ToSnapshotJson(),
                "an out-of-range config must be a no-op, not a throw or a mutation");
        }

        [Test]
        public void EveryConfig_OnEveryBeat_EquipsItsKit_AndTheLedgerBalances()
        {
            for (int beat = 1; beat <= 8; beat++)
            {
                for (int c = 1; c <= GameController.CheckpointConfigs.Length; c++)
                {
                    var m = GameController.BuildDemoCheckpoint(beat);
                    GameController.ApplyCheckpointConfig(m, c);
                    var row = GameController.CheckpointConfigs[c - 1];
                    string at = "beat " + beat + " config " + c;
                    if (row.Primary != WeaponId.None)
                    {
                        Assert.IsTrue(m.Owns(row.Primary), at + " — primary not owned");
                        Assert.IsTrue(m.TierOf(row.Primary) >= row.PrimaryTier,
                            at + " — primary under tier");
                        Assert.AreEqual(row.Primary, m.EquippedPrimary, at);
                    }
                    if (row.Secondary != WeaponId.None)
                    {
                        Assert.IsTrue(m.Owns(row.Secondary), at + " — secondary not owned");
                        Assert.IsTrue(m.TierOf(row.Secondary) >= row.SecondaryTier,
                            at + " — secondary under tier");
                        Assert.AreEqual(row.Secondary, m.EquippedSecondary, at);
                    }
                    if (row.Protocol != BoarderProtocolId.None)
                        Assert.AreEqual(row.Protocol, m.EquippedProtocol, at);
                    Assert.IsTrue(m.SalvageLedgerBalances, at + " — ledger broken");
                    var back = MetaState.FromSnapshotJson(m.ToSnapshotJson());
                    Assert.IsFalse(back.SalvageStatsWereRepaired,
                        at + " — round-trip tripped the repair");
                }
            }
        }

        [Test]
        public void EveryCheckpoint_SurvivesTheSaveRoundTrip_Unrepaired()
        {
            for (int digit = 1; digit <= 8; digit++)
            {
                var m = GameController.BuildDemoCheckpoint(digit);
                var back = MetaState.FromSnapshotJson(m.ToSnapshotJson());
                Assert.IsFalse(back.SalvageStatsWereRepaired, "checkpoint " + digit);
                Assert.AreEqual(m.BankedSalvage, back.BankedSalvage, "checkpoint " + digit);
                Assert.AreEqual(m.HatchConversions, back.HatchConversions, "checkpoint " + digit);
                Assert.AreEqual(m.OpenDoorIds.Length, back.OpenDoorIds.Length, "checkpoint " + digit);
            }
        }
    }
}
