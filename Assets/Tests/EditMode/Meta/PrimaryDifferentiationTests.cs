using System;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The three shop Primaries stay distinguishable on combat fields, so a
    /// retune cannot collapse them into one gun with three names.</summary>
    public class PrimaryDifferentiationTests
    {
        static readonly WeaponId[] ShopPrimaries =
        {
            WeaponId.Br12Shotgun,
            WeaponId.Mr35Rifle,
            WeaponId.PriC,
        };

        const int MinDistinctFields = 3;
        const float Tol = 0.001f;

        [Test]
        public void ShopPrimarySet_IsExactlyTheThree()
        {
            int n = 0;
            foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
            {
                Assert.Contains(id, ShopPrimaries);
                n++;
            }
            Assert.AreEqual(ShopPrimaries.Length, n);
            Assert.IsFalse(WeaponCatalog.IsAvailable(WeaponId.Vk7Rifle));
        }

        [Test]
        public void EachPair_DiffersOnAtLeastThreeCombatFields()
        {
            for (int i = 0; i < ShopPrimaries.Length; i++)
            {
                var a = WeaponCatalog.Def(ShopPrimaries[i]);
                for (int j = i + 1; j < ShopPrimaries.Length; j++)
                {
                    var b = WeaponCatalog.Def(ShopPrimaries[j]);
                    int diffs = CombatFieldDiffs(a, b);
                    Assert.GreaterOrEqual(diffs, MinDistinctFields,
                        $"{a.displayName} vs {b.displayName}: {diffs} combat-field diffs, need ≥{MinDistinctFields}");
                }
            }
        }

        static int CombatFieldDiffs(WeaponDef a, WeaponDef b)
        {
            int n = 0;
            if (Math.Abs(a.damage - b.damage) > Tol) n++;
            if (Math.Abs(a.shotsPerSecond - b.shotsPerSecond) > Tol) n++;
            if (Math.Abs(a.clipSize - b.clipSize) > Tol) n++;
            if (Math.Abs(a.reloadSeconds - b.reloadSeconds) > Tol) n++;
            if (Math.Abs(a.range - b.range) > Tol) n++;
            if (a.pellets != b.pellets) n++;
            if (Math.Abs(a.spreadDegrees - b.spreadDegrees) > Tol) n++;
            if (a.pierceCount != b.pierceCount) n++;
            if (Math.Abs(a.pierceDamageMul - b.pierceDamageMul) > Tol) n++;
            if (Math.Abs(a.falloffStart - b.falloffStart) > Tol) n++;
            if (a.abilityId != b.abilityId) n++;
            return n;
        }
    }
}
