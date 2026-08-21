using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Hatch Armor still ramps; Boarder HP / stamina / kill-value / crit are frozen
    /// constants. Crew tracks themselves are gone — see BoarderProtocolTests.</summary>
    public class PlayerAttributesTests
    {
        const float Tol = 1e-5f;

        [Test]
        public void HatchHp_StillRamps25Percent()
        {
            float atZero = MetaUpgrades.HatchHp(0);
            Assert.AreEqual(300f, atZero, Tol);
            Assert.AreEqual(1.25f, MetaUpgrades.HatchHp(MetaUpgrades.MaxLevel) / atZero, Tol);
        }

        [Test]
        public void BoarderConstants_FrozenAtLevelZero()
        {
            Assert.AreEqual(100f, MetaUpgrades.PlayerMaxHp(), Tol);
            Assert.AreEqual(100f, MetaUpgrades.StaminaMax(), Tol);
            Assert.AreEqual(25f, MetaUpgrades.SalvagePerKill(), Tol);
            Assert.AreEqual(0f, MetaUpgrades.CritChance(), Tol);
        }

        [Test]
        public void WeaponTracks_GatedBehindFirstBank_NotBehindLoss()
        {
            var meta = MetaState.CreateNew();
            Assert.AreEqual(0, meta.HatchConversions);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));
            meta.PodEjections = 1;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta),
                "a loss is no longer a progression gate (re-key 2026-08-18): a player who "
                + "banks and docks never ejects, and the wheel must open for them too");
            meta.HatchConversions = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "the wheel spends salvage that came through the Hatch, so it opens when "
                + "salvage first comes through the Hatch");
        }

        [Test]
        public void HatchArmor_BuyableAndPersists()
        {
            var meta = MetaState.CreateNew();
            meta.BankedSalvage += 200L;
            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.Realm.HatchArmorLevel));
            Assert.AreEqual(1, meta.HatchArmorLevel);
            var loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(1, loaded.HatchArmorLevel);
            Assert.AreEqual(BoarderProtocolId.None, loaded.EquippedProtocol);
        }
    }
}
