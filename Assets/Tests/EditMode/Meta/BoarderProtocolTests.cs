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
            Assert.AreEqual(DoctrineMode.Stock, BoarderProtocol.DoctrineOf(BoarderProtocolId.Default));
            Assert.AreEqual(0, (int)DoctrineMode.Stock);
            Assert.AreEqual(BoarderAbilityId.Anchor, BoarderProtocol.CrewAbilityOf(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderAbilityId.Bulkhead, BoarderProtocol.PersonalAbilityOf(BoarderProtocolId.Default));
            Assert.AreEqual(2, BoarderProtocol.PassiveCount(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderAbilityId.Ghost, BoarderProtocol.PassiveAt(BoarderProtocolId.Default, 0));
            Assert.AreEqual(BoarderAbilityId.Foresight, BoarderProtocol.PassiveAt(BoarderProtocolId.Default, 1));
            Assert.AreEqual(8f, BoarderProtocol.Window(BoarderAbilityId.Anchor), 1e-5f);
            Assert.AreEqual(30f, BoarderProtocol.Cooldown(BoarderAbilityId.Anchor), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.Magnitude(BoarderAbilityId.Anchor), 1e-5f);
            Assert.AreEqual(8f, BoarderProtocol.Radius(BoarderAbilityId.Anchor), 1e-5f);
            Assert.AreEqual(6f, BoarderProtocol.Window(BoarderAbilityId.Bulkhead), 1e-5f);
            Assert.AreEqual(40f, BoarderProtocol.Cooldown(BoarderAbilityId.Bulkhead), 1e-5f);
            Assert.AreEqual(0.70f, BoarderProtocol.Magnitude(BoarderAbilityId.Ghost), 1e-5f);
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
        public void FreshSave_DefaultGhost_NoPurchase()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(BoarderProtocolId.Default, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
            Assert.AreEqual(8, m.SaveVersion);
        }

        [Test]
        public void PickPassive_LockAndLegal()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(m.SetEquippedPassive(BoarderAbilityId.Foresight));
            Assert.AreEqual(BoarderAbilityId.Foresight, m.EquippedPassive);
            m.PayloadLoadoutLocked = true;
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Ghost));
            Assert.AreEqual(BoarderAbilityId.Foresight, m.EquippedPassive);
            m.PayloadLoadoutLocked = false;
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Anchor));
            Assert.AreEqual(BoarderAbilityId.Foresight, m.EquippedPassive);
        }

        [Test]
        public void V7Save_RefundsTracks_DefaultEquipped_LedgerHolds()
        {
            // 4 maxed tracks = 4 × 200 × (32-1) = 24,800
            const string json =
                "{\"SaveVersion\":7,\"BankedSalvage\":100,\"LifetimeSalvageEarned\":24900," +
                "\"LifetimeSalvageSpent\":24800,\"HatchArmorLevel\":0," +
                "\"VitalityLevel\":5,\"StaminaLevel\":5,\"ScavengingLevel\":5,\"LuckLevel\":5}";
            var m = MetaState.FromJson(json);
            Assert.AreEqual(24800L, MetaState.SpentOnTrack(5) * 4L);
            Assert.AreEqual(24900L, m.BankedSalvage);
            Assert.AreEqual(0L, m.LifetimeSalvageSpent);
            Assert.AreEqual(24900L, m.LifetimeSalvageEarned);
            Assert.IsTrue(m.SalvageLedgerBalances);
            Assert.AreEqual(BoarderProtocolId.Default, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
            Assert.AreEqual(SaveData.CurrentVersion, m.SaveVersion);
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
