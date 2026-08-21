using System;
using System.Reflection;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class BoarderProtocolTests
    {
        [Test]
        public void Catalog_DefaultSheet_Pins()
        {
            Assert.AreEqual(BoarderProtocolId.Default, BoarderProtocol.ShopProtocols()[0]);
            Assert.AreEqual(3, BoarderProtocol.ShopProtocols().Length);
            Assert.AreEqual(DoctrineMode.Stock, BoarderProtocol.DoctrineOf(BoarderProtocolId.Default));
            Assert.AreEqual(0, (int)DoctrineMode.Stock);
            Assert.AreEqual(BoarderAbilityId.Barrage, BoarderProtocol.CrewAbilityOf(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderAbilityId.Rush, BoarderProtocol.PersonalAbilityOf(BoarderProtocolId.Default));
            Assert.AreEqual(0, BoarderProtocol.PassiveCount(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderAbilityId.None, BoarderProtocol.PassiveAt(BoarderProtocolId.Default, 0));
            Assert.AreEqual(5, (int)BoarderAbilityId.Barrage);
            Assert.AreEqual(6, (int)BoarderAbilityId.Rush);
            Assert.AreEqual(8f, BoarderProtocol.Window(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(30f, BoarderProtocol.Cooldown(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.FireMul(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(8f, BoarderProtocol.Radius(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(6f, BoarderProtocol.Window(BoarderAbilityId.Rush), 1e-5f);
            Assert.AreEqual(40f, BoarderProtocol.Cooldown(BoarderAbilityId.Rush), 1e-5f);
            Assert.AreEqual(1.5f, BoarderProtocol.FireMul(BoarderAbilityId.Rush), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.Magnitude(BoarderAbilityId.Anchor), 1e-5f);
            Assert.AreEqual(0.70f, BoarderProtocol.Magnitude(BoarderAbilityId.Ghost), 1e-5f);
            Assert.IsTrue(BoarderProtocol.IsLegalPair(BoarderProtocolId.Default, BoarderAbilityId.None));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.Default, BoarderAbilityId.Ghost));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.None, BoarderAbilityId.None));
            Assert.AreEqual(BoarderProtocol.CrewSheetDefault, BoarderProtocol.CrewSheet(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderProtocol.PersonalSheetDefault, BoarderProtocol.PersonalSheet(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderProtocol.PassiveSheetDefault, BoarderProtocol.PassiveSheet(BoarderProtocolId.Default));
            Assert.AreEqual("", BoarderProtocol.CrewSheet(BoarderProtocolId.None));
            var sheet = BoarderProtocol.SheetOf(BoarderProtocolId.Default);
            Assert.AreEqual(BoarderProtocol.ProtocolName(BoarderProtocolId.Default), sheet.Name);
            Assert.AreEqual(BoarderProtocol.DoctrineText(BoarderProtocolId.Default), sheet.Doctrine);
            Assert.AreEqual(BoarderProtocol.CrewSheetDefault, sheet.Crew);
            Assert.AreEqual(BoarderProtocol.PersonalSheetDefault, sheet.Personal);
            Assert.AreEqual(BoarderProtocol.PassiveSheetDefault, sheet.Passive);
        }

        [Test]
        public void OverlayFill_ReadyIsZero_WindowAndCooldownAreFractions()
        {
            Assert.AreEqual(0f, BoarderProtocol.OverlayFill(0f, 0f, 6f, 40f), 1e-5f);
            Assert.AreEqual(0f, BoarderProtocol.OverlayFill(0.04f, 0.04f, 6f, 40f), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.OverlayFill(3f, 0f, 6f, 40f), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.OverlayFill(0f, 20f, 6f, 40f), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.OverlayFill(8f, 0f, 8f, 30f), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.OverlayFill(4f, 15f, 8f, 30f), 1e-5f);
        }

        [Test]
        public void ComingSoonNames_AreNotCatalogIds()
        {
            // VEIL now parses — it is a sold protocol. AEGIS and LEDGER are still only copy,
            // and the catalog must not answer to either.
            BoarderProtocolId aegis, ledger;
            Assert.IsFalse(Enum.TryParse("AEGIS", true, out aegis));
            Assert.IsFalse(Enum.TryParse("LEDGER", true, out ledger));
            var shop = BoarderProtocol.ShopProtocols();
            for (int i = 0; i < shop.Length; i++)
            {
                string name = BoarderProtocol.ProtocolName(shop[i]);
                Assert.AreNotEqual("AEGIS", name);
                Assert.AreNotEqual("LEDGER", name);
                Assert.IsNotEmpty(name);
            }
        }

        [Test]
        public void ComposeFireMul_SetThenProduct()
        {
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, true, 1f), 1e-5f);
            Assert.AreEqual(1.5f, BoarderProtocol.ComposeFireMul(true, false, 1f), 1e-5f);
            Assert.AreEqual(3f, BoarderProtocol.ComposeFireMul(true, true, 2f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, false, 2f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, true, 2f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, false, 4f), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.ComposeFireMul(false, false, 1f), 1e-5f);
        }

        [Test]
        public void AbilityState_ActivateTickExpire_ThenCooldown()
        {
            var s = new BoarderAbilityState();
            Assert.IsTrue(s.IsReady);
            Assert.IsTrue(s.TryActivate(6f, 40f));
            Assert.IsTrue(s.WindowOpen);
            Assert.IsFalse(s.TryActivate(6f, 40f), "no refresh while open");
            s.Tick(6f);
            Assert.IsFalse(s.WindowOpen);
            Assert.Greater(s.CooldownRemaining, 0f);
            s.Tick(40f);
            Assert.IsTrue(s.IsReady);
        }

        [Test]
        public void AbilityState_EndWindowEarly_StartsCooldown_SecondsSatReadyOnActivate()
        {
            var s = new BoarderAbilityState();
            s.Tick(3.5f);
            Assert.AreEqual(3.5f, s.SecondsSatReady, 1e-4f);
            Assert.IsTrue(s.TryActivate(8f, 30f));
            Assert.AreEqual(0f, s.SecondsSatReady, 1e-5f);
            s.EndWindowEarly();
            Assert.IsFalse(s.WindowOpen);
            Assert.AreEqual(30f, s.CooldownRemaining, 1e-5f);
            s.EndWindowEarly();
            Assert.AreEqual(30f, s.CooldownRemaining, 1e-5f);
        }

        [Test]
        public void FreshSave_NoneUntilClaim()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(BoarderProtocolId.None, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
            // v9 landed with the demo flags; the pin follows the schema, not a literal.
            Assert.AreEqual(SaveFormat.CurrentVersion, m.SaveVersion);
            Assert.IsTrue(BoarderProtocol.TryClaim(m, BoarderProtocolId.Default));
            Assert.AreEqual(BoarderProtocolId.Default, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
            Assert.IsFalse(BoarderProtocol.TryClaim(m, BoarderProtocolId.Default));
            Assert.IsFalse(BoarderProtocol.TryClaim(m, BoarderProtocolId.None));
            m.PayloadLoadoutLocked = true;
            var locked = MetaState.CreateNew();
            locked.PayloadLoadoutLocked = true;
            Assert.IsFalse(BoarderProtocol.TryClaim(locked, BoarderProtocolId.Default));
        }

        [Test]
        public void PickPassive_Refuses_NoDefaultPassives()
        {
            var m = MetaState.CreateNew();
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Ghost));
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
            BoarderProtocol.TryClaim(m, BoarderProtocolId.Default);
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Foresight));
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
        }

        [Test]
        public void V7Save_RefundsTracks_NoneEquipped_LedgerHolds()
        {
            // 4 maxed tracks = 4 × 200 × (32-1) = 24,800
            const string json =
                "{\"SaveVersion\":7,\"BankedSalvage\":100,\"LifetimeSalvageEarned\":24900," +
                "\"LifetimeSalvageSpent\":24800,\"HatchArmorLevel\":0," +
                "\"VitalityLevel\":5,\"StaminaLevel\":5,\"ScavengingLevel\":5,\"LuckLevel\":5}";
            var m = MetaState.FromSnapshotJson(json);
            Assert.AreEqual(24800L, MetaState.SpentOnTrack(5) * 4L);
            Assert.AreEqual(24900L, m.BankedSalvage);
            Assert.AreEqual(0L, m.LifetimeSalvageSpent);
            Assert.AreEqual(24900L, m.LifetimeSalvageEarned);
            Assert.IsTrue(m.SalvageLedgerBalances);
            Assert.AreEqual(BoarderProtocolId.None, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
            Assert.AreEqual(SaveFormat.CurrentVersion, m.SaveVersion);
        }

        [Test]
        public void V8Save_DefaultGhost_CoercesToDefaultNone()
        {
            const string json =
                "{\"SaveVersion\":8,\"BankedSalvage\":0,\"LifetimeSalvageEarned\":0," +
                "\"LifetimeSalvageSpent\":0,\"EquippedProtocol\":1,\"EquippedPassive\":3}";
            var m = MetaState.FromSnapshotJson(json);
            Assert.AreEqual(BoarderProtocolId.Default, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
        }

        [Test]
        public void V8Save_IllegalPair_BecomesNone()
        {
            const string json =
                "{\"SaveVersion\":8,\"BankedSalvage\":0,\"LifetimeSalvageEarned\":0," +
                "\"LifetimeSalvageSpent\":0,\"EquippedProtocol\":99,\"EquippedPassive\":1}";
            var m = MetaState.FromSnapshotJson(json);
            Assert.AreEqual(BoarderProtocolId.None, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
        }

        [Test]
        public void RemovedTracksAreGone()
        {
            var upgrades = typeof(MetaUpgrades);
            Assert.IsNull(upgrades.GetMethod("PlayerMaxHp",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null));
            Assert.IsNotNull(upgrades.GetMethod("PlayerMaxHp",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null));
            Assert.IsNull(typeof(MetaState).GetField("VitalityLevel"));
            Assert.IsNull(typeof(MetaState).GetField("StaminaLevel"));
            Assert.IsNull(typeof(MetaState).GetField("ScavengingLevel"));
            Assert.IsNull(typeof(MetaState).GetField("LuckLevel"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Vitality"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Stamina"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Scavenging"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Luck"));
        }

        [Test]
        public void Constants_AreLevelZeroValues()
        {
            Assert.AreEqual(100f, MetaUpgrades.PlayerMaxHp(), 1e-5f);
            Assert.AreEqual(100f, MetaUpgrades.StaminaMax(), 1e-5f);
            Assert.AreEqual(25f, MetaUpgrades.SalvagePerKill(), 1e-5f);
            Assert.AreEqual(0f, MetaUpgrades.CritChance(), 1e-5f);
        }

        [Test]
        public void DecoyTryPlace_WritesStock_AndBaselineUnchanged()
        {
            var s = new DecoyState();
            s.ResetForRun(PayloadId.Decoy);
            float pull = s.PullRadius;
            float hp = s.MaxHp;
            int maxLive = s.MaxLive;
            Assert.IsTrue(s.TryPlace(1f, 0f, 2f, DoctrineMode.Stock, out _));
            Assert.AreEqual(DoctrineMode.Stock, s.Mode);
            Assert.AreEqual(pull, s.PullRadius, 1e-5f);
            Assert.AreEqual(hp, s.MaxHp, 1e-5f);
            Assert.AreEqual(maxLive, s.MaxLive);
            Assert.IsTrue(DecoyState.ShouldPull(GatekeeperKind.Default, false, true, 1f, pull));
        }
    }
}
