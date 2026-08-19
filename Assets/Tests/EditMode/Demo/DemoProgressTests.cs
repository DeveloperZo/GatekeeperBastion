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
        public void Multiplayer_LockedUntilFirstWheelBuy()
        {
            var m = DemoMeta();
            Assert.IsFalse(DemoProgress.MultiplayerUnlocked(m), "buys-left 2 is locked");
            m.DemoWheelBuysLeft = 1;
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(m), "first buy unlocks");
            m.DemoWheelBuysLeft = 2;
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
        public void Armory_Hidden_UntilTheWheelUnlocks()
        {
            var m = DemoMeta();
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.ArmoryVerdict(m),
                "no name, no interaction — the MK-1 comes from the ship terminal");
            m.DemoArmoryPowered = true; // power alone does not surface it
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.ArmoryVerdict(m));
            m.PodEjections = 1; // a loss alone is no longer the key (re-key 2026-08-18)
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.ArmoryVerdict(m));
            m.HatchConversions = 1; // UpgradesUnlocked — the first bank
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.ArmoryVerdict(m));
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
        public void Fabrication_StaysHiddenForTheWholeDemo()
        {
            var m = DemoMeta();
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.FabricationVerdict(m));
            m.DemoNavPowered = true;
            m.DemoArmoryPowered = true;
            m.DemoShipFightDone = true;
            m.HatchConversions = 1;
            Assert.AreEqual(DemoStationVerdict.Hidden, DemoProgress.FabricationVerdict(m));
            Assert.AreEqual(DemoStationVerdict.Open,
                DemoProgress.FabricationVerdict(CampaignMeta()));
        }

        // ---- Beat 3's exit-standing state key (M78 criterion 4) ----

        static MetaState FirstReturnMeta(long banked)
        {
            var m = DemoMeta();
            m.HatchConversions = 1;
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            m.BankedSalvage = banked;
            return m;
        }

        [Test]
        public void ExitStanding_HoldsWhileAWheelBuyIsAffordable()
        {
            var m = FirstReturnMeta(450);
            Assert.IsFalse(DemoProgress.NoBuyableUpgradeRemains(m),
                "banked 450, buys-left 2, MK-1 at T0 (150) — spend first");
        }

        [Test]
        public void ExitStanding_FlipsAtBuysLeftZero_NoThirdClickNeeded()
        {
            var m = FirstReturnMeta(10_000);
            m.DemoWheelBuysLeft = 0;
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "the cap alone flips it — the third refuse produces nothing");
        }

        [Test]
        public void ExitStanding_FlipsWhenBankedFallsBelowTheNextCost()
        {
            // The salvage-short seat: one buy landed (tier 1, next cost 300), 150 left.
            var m = FirstReturnMeta(150);
            m.DemoWheelBuysLeft = 1;
            m.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Damage);
            Assert.AreEqual(1, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "banked below the next cost is the other half of the state key");
        }

        [Test]
        public void ExitStanding_UnownedCostZeroSecondary_NeverHoldsItOpen()
        {
            var m = FirstReturnMeta(0);
            m.DemoWheelBuysLeft = 1;
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "SEC-D's free row is a weapon, not a buyable upgrade (beat 3, Traps)");
        }

        [Test]
        public void ExitStanding_AnOwnedSecondWeaponsWheel_CountsHonestly()
        {
            var m = FirstReturnMeta(150);
            m.DemoWheelBuysLeft = 1;
            m.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Damage); // MK-1 next cost 300
            m.SetOwned(WeaponId.SecD, true);                     // SEC-D wheel at T0: 150
            Assert.IsFalse(DemoProgress.NoBuyableUpgradeRemains(m),
                "once OWNED, a second Secondary's wheel is a real buyable upgrade");
        }

        [Test]
        public void ExitStanding_LockedWheel_HasNothingBuyable()
        {
            var m = DemoMeta();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.BankedSalvage = 10_000;
            Assert.IsTrue(DemoProgress.NoBuyableUpgradeRemains(m),
                "before the first bank there is no wheel to spend at");
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
        public void TerminalStart_GrantsBeforeTheIntrudersLine()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Ship/ShipTerminal.cs"));
            int start = src.IndexOf("void StartTerminal");
            int grant = src.IndexOf("DemoShipFight.GrantWeapon", start);
            int line = src.IndexOf("DemoConversationIds.Intruders", start);
            Assert.Greater(grant, start, "the terminal's start hands the MK-1 over");
            Assert.Greater(line, grant,
                "the intruders copy says the MK-1 is in hand — grant first");
        }

        [Test]
        public void UnlockReload_PlacesAtCapsuleSpawn()
        {
            string src = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/Game/Ship/DemoShipDirector.cs"));
            StringAssert.Contains("PlaceAt(player, _spawnCapsule)", src);
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
