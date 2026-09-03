using System.IO;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The demo's pure gate rules: the multiplayer lock, the EMBARK refuse order,
    /// and each station's verdict matrix. EditMode on purpose — DemoProgress is engine-free
    /// and these matrices are what the beat 0 refuse tables promise.</summary>
    public class DemoProgressTests
    {
        static MetaState DemoMeta()
        {
            var m = MetaState.CreateNew(); // fresh campaign: demo on, nothing done
            return m;
        }

        static MetaState CampaignMeta()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            return m;
        }

        [Test]
        public void Multiplayer_LockedUntilTheMk1()
        {
            // Co-op unlocks the moment the ARMORY arms you (Zo, 2026-08-25) — it used to
            // wait for the first wheel buy, which stranded friends behind a bank.
            var m = DemoMeta();
            Assert.IsFalse(DemoProgress.MultiplayerUnlocked(m), "unarmed is locked");
            m.DemoWeaponGranted = true;
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m), "the MK-1 unlocks");
            m.DemoWeaponGranted = false;
            m.DemoActive = false;
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m), "demo off never locks");
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(null), "no meta never locks");
        }

        [Test]
        public void EmbarkRefusals_FollowTheBeatOrder()
        {
            var m = DemoMeta();
            Assert.AreEqual(DemoEmbarkRefusal.ShipTerminal, DemoProgress.EmbarkRefusal(m));
            m.DemoArmoryPowered = true;
            Assert.AreEqual(DemoEmbarkRefusal.NoWeapon, DemoProgress.EmbarkRefusal(m));
            m.DemoWeaponGranted = true;
            Assert.AreEqual(DemoEmbarkRefusal.Intruders, DemoProgress.EmbarkRefusal(m));
            m.DemoShipFightDone = true;
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(m));
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(CampaignMeta()));
        }

        [Test]
        public void Nav_DarkThenNoPanelThenOpen()
        {
            var m = DemoMeta();
            Assert.AreEqual(DemoStationVerdict.Dark, DemoProgress.NavVerdict(m));
            m.DemoNavPowered = true;
            Assert.AreEqual(DemoStationVerdict.NoPanelYet, DemoProgress.NavVerdict(m));
            m.DemoShipFightDone = true;
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.NavVerdict(m));
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.NavVerdict(CampaignMeta()));
        }

        [Test]
        public void Armory_Hidden_UntilTheTerminalPowersIt()
        {
            // The armory is the ARMING BEAT (2026-08-25 recut): the terminal routes power
            // and points here; the rack's free MK-1 readies embark, no ship fight.
            var m = DemoMeta();
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.ArmoryVerdict(m),
                "no name, no interaction — until the ship terminal routes power");
            m.DemoArmoryPowered = true; // the terminal's start surfaces the rack
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.ArmoryVerdict(m));
            var late = DemoMeta();
            // UpgradesUnlocked is the other door in, and since 2026-08-25 (Zo) it asks whether
            // the realm has EARNED, not whether salvage came through a Convert window — that
            // window is out of the demo, so HatchConversions can never rise again.
            late.EarnSalvage(1);
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.ArmoryVerdict(late));
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.ArmoryVerdict(CampaignMeta()));
        }

        [Test]
        public void Crew_Hidden_ThenHealOnlyAfterTheFight()
        {
            var m = DemoMeta();
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.CrewVerdict(m));
            m.DemoNavPowered = true; // power alone does not surface it
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.CrewVerdict(m));
            m.DemoShipFightDone = true;
            Assert.AreEqual(DemoStationVerdict.HealOnly, DemoProgress.CrewVerdict(m),
                "the post-fight heal is CREW's first live function");
            m.DemoCrewMenuUnlocked = true;
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.CrewVerdict(m));
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.CrewVerdict(CampaignMeta()));
        }

        [Test]
        public void Fabrication_StaysLockedPreviewForTheWholeDemo()
        {
            // Visible-but-locked for the whole demo (ruling 2026-08-25): the console shows
            // its name and a full-version prompt at every beat, and never opens.
            var m = DemoMeta();
            Assert.AreEqual(DemoStationVerdict.LockedPreview, DemoProgress.FabricationVerdict(m));
            m.DemoNavPowered = true;
            m.DemoArmoryPowered = true;
            m.DemoShipFightDone = true;
            m.EarnSalvage(1); // every other station's key turned; FAB's never does
            Assert.AreEqual(DemoStationVerdict.LockedPreview, DemoProgress.FabricationVerdict(m));
            Assert.AreEqual(DemoStationVerdict.Open,
                DemoProgress.FabricationVerdict(CampaignMeta()));
        }

        // ---- Beat 3's exit-standing state key (M78 criterion 4) ----

        static MetaState FirstReturnMeta(long banked)
        {
            var m = DemoMeta();
            // The wheel's unlock is LifetimeSalvageEarned since 2026-08-25 (Zo) — the Convert
            // window that used to raise HatchConversions is out of the demo. Earn a token and
            // spend it straight back so the realm reads as having earned at ANY bank, zero
            // included: spending down to empty must never re-lock the wheel. Earning rather
            // than assigning BankedSalvage also keeps the ledger balanced, which is the state
            // every one of these rows is really standing in.
            m.EarnSalvage(1);
            Assert.IsTrue(m.SpendSalvage(1));
            m.EarnSalvage(banked);
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            return m;
        }

        [Test]
        public void ExitStanding_HoldsWhileAWheelBuyIsAffordable()
        {
            var m = FirstReturnMeta(450);
            Assert.IsFalse(DemoProgress.NoBuyableUpgradeRemains(m),
                "banked 450, buys-left 2, MK-1 at T0 (150) — spend first");
        }

        /// <summary>Re-keyed 2026-09-02 (Zo: "rack opened when player earns enough") from the
        /// deepest Wave entered, and before that from a zone-2 Convert window the demo no longer
        /// has. Lifetime earned, so the gate is the price and nothing else.</summary>
        [Test]
        public void PrimaryRows_StayShutUntilTheRealmHasEarnedTheCheapestPrimary()
        {
            long gate = DemoProgress.PrimaryRackSalvage;
            Assert.AreEqual(WeaponCatalog.Def(WeaponId.Br12Shotgun).cost, gate,
                "the gate is the cheapest Primary on the rack, read off the catalog");
            var m = DemoMeta();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.EarnSalvage(gate - 1);
            m.DeepestWaveEntered = WaveDirector.MaxWaves;
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(m),
                "depth buys nothing — one salvage short of the price is shut");
            m.EarnSalvage(1);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(m), "earning the price opens the rack");
            m.SpendSalvage(m.BankedSalvage);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(m),
                "lifetime, not the pocket — spending to zero cannot re-lock it");
        }

        [Test]
        public void PrimaryRows_ReadTheLedger_NotAnAssignedPile()
        {
            var m = DemoMeta();
            m.BankedSalvage = 10_000; // a grant, a dev write — never came through the ledger
            Assert.AreEqual(0L, m.LifetimeSalvageEarned);
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(m),
                "a pile that was not earned opens nothing, exactly as the wheel's lock reads");
        }

        [Test]
        public void PrimaryRows_AreNotKeyedOnEndings_OrOnZoneTwosBank()
        {
            var m = DemoMeta();
            m.PodEjections = 5;
            m.DemoZone2BankLost = true;
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(m),
                "ejections and the zone-2 record open nothing — only Salvage earned does");
            m.PodEjections = 0;
            m.EarnSalvage(DemoProgress.PrimaryRackSalvage);
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(m),
                "and the gate opens at zero ejections: the player who docked out alive with\n"
                + "the price earned is owed the same rack as the one who died earning it");
        }

        [Test]
        public void PrimaryRows_AreOpenOffTheDemo()
        {
            var m = FirstReturnMeta(10_000);
            m.DemoActive = false;
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(m), "a campaign save has the whole catalog");
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(null), "no meta never closes the rack");
        }

        [Test]
        public void ExitStanding_FlipsWhenBankedFallsBelowTheCheapestSpoke()
        {
            // One Damage buy landed; Reload (and every other spoke) is still 150.
            // 149 cannot buy any remaining spoke. 150 still can.
            var m = FirstReturnMeta(149);
            m.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Damage);
            Assert.AreEqual(1, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(1, m.WheelCentresOf(WeaponId.Mk1Sidearm, WheelAttr.Damage));
            Assert.AreEqual(0, m.WheelCentresOf(WeaponId.Mk1Sidearm, WheelAttr.Reload));
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "banked below every remaining spoke cost is the other half of the state key");

            m.EarnSalvage(1); // 150
            Assert.IsFalse(DemoProgress.NoBuyableUpgradeRemains(m),
                "150 still buys a spoke that has never been centred");
        }

        [Test]
        public void ExitStanding_UnownedCostZeroSecondary_NeverHoldsItOpen()
        {
            var m = FirstReturnMeta(0);
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "SEC-D's free row is a weapon, not a buyable upgrade (beat 3, Traps)");
        }

        [Test]
        public void ExitStanding_AnOwnedSecondWeaponsWheel_CountsHonestly()
        {
            var m = FirstReturnMeta(150);
            m.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Damage); // MK-1 Damage next 300; other spokes 150
            m.SetOwned(WeaponId.SecD, true);                     // SEC-D wheel at T0: 150
            Assert.IsFalse(DemoProgress.NoBuyableUpgradeRemains(m),
                "once OWNED, a second Secondary's wheel is a real buyable upgrade");
        }

        [Test]
        public void ExitStanding_LockedWheel_HasNothingBuyable()
        {
            var m = DemoMeta();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            // Assigned, not earned. Since 2026-08-25 (Zo) the wheel's key is
            // LifetimeSalvageEarned, so a pile that never came through the ledger — a grant, a
            // dev write, a loaded file being rebuilt — leaves the wheel exactly as shut as an
            // empty realm. The gate reads the history, never the pocket.
            m.BankedSalvage = 10_000;
            Assert.AreEqual(0L, m.LifetimeSalvageEarned);
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "a realm that has earned nothing has no wheel to spend at");
        }

        [Test]
        public void EndDemo_FlipsTheTwoWrites_AndLeavesTheRungTwoRecord()
        {
            var m = DemoMeta();
            m.DemoZone2BankLost = true;
            m.DemoRungTwoOpened = true;
            DemoProgress.EndDemo(m);
            Assert.IsFalse(m.DemoActive);
            Assert.IsTrue(m.DemoFabPowered);
            Assert.IsTrue(m.DemoZone2BankLost,
                "the loss record stays — since 2026-08-25 it is the pre-re-key saves' only\n"
                + "claim on the rack, so an Escape must not quietly erase it");
            Assert.IsTrue(m.DemoRungTwoOpened);
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.FabricationVerdict(m));
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m));
            Assert.IsTrue(DemoProgress.PrimaryRowsOpen(m),
                "DemoActive-gated readers open the moment the flag flips");
        }

        [Test]
        public void EndDemo_IsIdempotent_AndIgnoresNull()
        {
            DemoProgress.EndDemo(null);
            var m = DemoMeta();
            m.DemoActive = false;
            DemoProgress.EndDemo(m);
            Assert.IsFalse(m.DemoFabPowered, "a campaign save is not a demo end");
        }

        [Test]
        public void GrantLog_FiresBeforeBuyWeapon()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Ship/DemoShipFight.cs"));
            int grant = src.IndexOf("granted MK-1");
            int buy = src.IndexOf("BuyWeapon(WeaponId.Mk1Sidearm)");
            Assert.Greater(grant, 0);
            Assert.Greater(buy, grant, "reads-back order is granted MK-1 then BuyWeapon");
        }

        [Test]
        public void TerminalStart_PointsAtTheArmory_AndNeverArmsOrSpawns()
        {
            // Armory recut (2026-08-25 — Zo): the terminal routes power and points at the
            // rack; the grant rides the ARMORY buy and the intruder fight is struck.
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Ship/ShipTerminal.cs"));
            int start = src.IndexOf("void StartTerminal");
            Assert.Greater(start, 0);
            string body = src.Substring(start);
            StringAssert.Contains("DemoConversationIds.ArmoryCall", body);
            Assert.AreEqual(-1, body.IndexOf("DemoShipFight.GrantWeapon"),
                "the terminal must not grant — the ARMORY rack is the arming beat");
            Assert.AreEqual(-1, body.IndexOf("DemoConversationIds.Intruders"),
                "the intruder fight is struck — the terminal never plays its line");
            Assert.AreEqual(-1, body.IndexOf("ArmRelease"),
                "nothing spawns from the terminal's start");
        }

        [Test]
        public void ReleaseReload_PlacesAtTeleporterSpawn()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Ship/DemoShipDirector.cs"));
            StringAssert.Contains("PlaceAt(player, _spawnTeleporter)", src);
        }

        [Test]
        public void ReplicatedHp_HonoursShipFloor()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Player/PlayerHealth.cs"));
            int apply = src.IndexOf("public void ApplyReplicatedHp");
            int floor = src.IndexOf("DemoShipFight.FloorsPlayerHp", apply);
            int die = src.IndexOf("Die(replicatedHp: true)", apply);
            Assert.Greater(floor, apply);
            Assert.Greater(die, floor, "Ship demo floor must run before replicated Die");
        }
    }
}
