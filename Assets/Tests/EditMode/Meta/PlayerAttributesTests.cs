using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Hatch Armor still ramps; Boarder HP / stamina / kill-value / crit are frozen
    /// AGAINST THE TRACKS — no level argument reaches any of them — which is not the same thing as
    /// never retuned. SalvagePerKill moved 25 -> 42 on 2026-08-27 with the ~40% roster cut, and
    /// this fixture is one of the two places that names the live figure on purpose. Crew tracks
    /// themselves are gone — see BoarderProtocolTests.</summary>
    public class PlayerAttributesTests
    {
        const float Tol = 1e-5f;

        [Test]
        public void HatchHp_StillRamps25Percent()
        {
            float atZero = MetaUpgrades.HatchHp(0);
            Assert.AreEqual(ShipSystems.HatchMaxHp, atZero, Tol);
            Assert.AreEqual(1.25f, MetaUpgrades.HatchHp(MetaUpgrades.MaxLevel) / atZero, Tol);
        }

        [Test]
        public void BoarderConstants_FrozenAtLevelZero()
        {
            Assert.AreEqual(100f, MetaUpgrades.PlayerMaxHp(), Tol);
            Assert.AreEqual(100f, MetaUpgrades.StaminaMax(), Tol);
            Assert.AreEqual(42f, MetaUpgrades.SalvagePerKill(), Tol);  // 25 x 5/3, 2026-08-27
            Assert.AreEqual(0f, MetaUpgrades.CritChance(), Tol);
        }

        [Test]
        public void WeaponTracks_GatedBehindTheFirstSalvageEarned_NotBehindLoss()
        {
            var meta = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));
            meta.PodEjections = 1;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta),
                "a loss is no longer a progression gate (re-key 2026-08-18): a player who "
                + "banks and docks never ejects, and the wheel must open for them too");
            meta.EarnSalvage(25L);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "the wheel spends salvage the realm has earned, so it opens on the first "
                + "salvage earned (re-key 2026-08-25: kills bank as they land, and the "
                + "Convert window that used to be this gate is out of the demo)");
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
