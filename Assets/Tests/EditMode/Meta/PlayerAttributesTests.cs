using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
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
        public void WeaponTracks_StillGatedBehindFirstDeath()
        {
            var meta = new MetaState();
            Assert.AreEqual(0, meta.PodEjections);
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));
            meta.PodEjections = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta));
        }

        [Test]
        public void HatchArmor_BuyableAndPersists()
        {
            var meta = MetaState.CreateNew();
            meta.BankedSalvage += 200L;
            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel));
            Assert.AreEqual(1, meta.HatchArmorLevel);
            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(1, loaded.HatchArmorLevel);
            Assert.AreEqual(BoarderProtocolId.Default, loaded.EquippedProtocol);
        }
    }
}
