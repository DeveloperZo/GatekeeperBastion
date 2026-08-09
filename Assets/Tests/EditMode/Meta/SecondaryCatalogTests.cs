using System;
using System.Collections.Generic;
using GatekeeperBastion.Sim;
using NUnit.Framework;

namespace GatekeeperBastion.Tests
{
    /// <summary>M26 Path 1 — SEC-D catalog presence, identity, power band, and kill-line shape.</summary>
    public class SecondaryCatalogTests
    {
        const float Tol = 0.05f;

        static int[] Flat(int tier)
        {
            var p = new int[WeaponWheel.AttrCount];
            WeaponWheel.DistributeFlatTier(p, tier);
            return p;
        }

        static float TimeToEmpty(WeaponDef def) =>
            WeaponWheel.EffectiveClip(def, 0) / WeaponWheel.EffectiveFireRate(def, 0);

        [Test]
        public void ArraySize_ExceedsEveryListedId()
        {
            foreach (WeaponId id in WeaponCatalog.All())
            {
                Assert.Greater(WeaponCatalog.ArraySize, (int)id,
                    $"ArraySize {WeaponCatalog.ArraySize} must exceed id {(int)id} ({id})");
            }
            Assert.Greater(WeaponCatalog.ArraySize, (int)WeaponId.SecD);
        }

        [Test]
        public void SecDIsInEveryCatalogList()
        {
            var def = WeaponCatalog.Def(WeaponId.SecD);
            Assert.AreEqual(WeaponSlot.Secondary, def.slot);
            Assert.AreEqual("SEC-D", def.displayName);
            Assert.AreEqual(0L, def.cost);

            bool inAll = false, inShop = false, inAvailable = false, inSecondary = false;
            foreach (var id in WeaponCatalog.All())
                if (id == WeaponId.SecD) inAll = true;
            foreach (var id in WeaponCatalog.ShopWeapons())
                if (id == WeaponId.SecD) inShop = true;
            inAvailable = WeaponCatalog.IsAvailable(WeaponId.SecD);
            foreach (var id in WeaponCatalog.ForSlot(WeaponSlot.Secondary))
                if (id == WeaponId.SecD) inSecondary = true;

            Assert.IsTrue(inAll, "All()");
            Assert.IsTrue(inShop, "ShopWeapons()");
            Assert.IsTrue(inAvailable, "IsAvailable()");
            Assert.IsTrue(inSecondary, "ForSlot(Secondary)");
        }

        [Test]
        public void SecondariesNeverRivalPrimaries()
        {
            for (int t = 0; t <= WeaponTier.MaxTier; t++)
            {
                var pts = Flat(t);
                float br12 = WeaponWheel.EffectiveDps(WeaponCatalog.Def(WeaponId.Br12Shotgun), pts);
                float mr35 = WeaponWheel.EffectiveDps(WeaponCatalog.Def(WeaponId.Mr35Rifle), pts);
                float primaryFloor = Math.Min(br12, mr35);

                foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Secondary))
                {
                    // Unringed Secondaries cannot buy points — equal investment is T0 only.
                    if (!WeaponWheel.HasRing(id) && t > 0) continue;
                    float dps = WeaponWheel.EffectiveDps(WeaponCatalog.Def(id), pts);
                    Assert.Less(dps, primaryFloor,
                        $"{id} flat T{t} dps {dps:0.##} must stay below Primary floor {primaryFloor:0.##}");
                }
            }
        }

        [Test]
        public void SecDAndMk1AreDifferentWeapons()
        {
            var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            var secd = WeaponCatalog.Def(WeaponId.SecD);

            float mk1AtWorking = WeaponWheel.DamageAtRange(mk1, 0, mk1.falloffStart);
            float secdAtMk1Working = WeaponWheel.DamageAtRange(secd, 0, mk1.falloffStart);
            Assert.Greater(mk1AtWorking, 0f);
            Assert.Less(secdAtMk1Working, mk1AtWorking * 0.05f,
                $"SEC-D at MK-1's {mk1.falloffStart} m working distance deals {secdAtMk1Working}, must be near-zero");

            float mk1Empty = TimeToEmpty(mk1);
            float secdEmpty = TimeToEmpty(secd);
            Assert.GreaterOrEqual(mk1Empty / secdEmpty, 2f,
                $"time-to-empty MK-1 {mk1Empty:0.##}s vs SEC-D {secdEmpty:0.##}s must differ by ≥2×");
        }

        [Test]
        public void IdentityCuesAreUnique()
        {
            var seen = new HashSet<string>();
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var def = WeaponCatalog.Def(id);
                string key = $"{def.crosshair}|{def.tracerR:0.###},{def.tracerG:0.###},{def.tracerB:0.###}";
                Assert.IsTrue(seen.Add(key), $"{id} shares crosshair+tracer with another shop weapon ({key})");
            }
        }

        [Test]
        public void BothSecondariesFree_ReachableOnFreshCampaign()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost);
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.SecD).cost);

            m.SetOwned(WeaponId.SecD, true);
            m.SetEquipped(WeaponId.SecD);
            Assert.AreEqual(WeaponId.SecD, m.EquippedSecondary);

            var loaded = MetaState.FromJson(m.ToJson());
            Assert.IsTrue(loaded.Owns(WeaponId.SecD));
            Assert.AreEqual(WeaponId.SecD, loaded.EquippedSecondary);
        }

        [Test]
        public void KillLog_NamesTheSlot()
        {
            string primary = KillLog.Format("Hunter", WeaponSlot.Primary, 25f, 3);
            Assert.AreEqual("killed a Hunter with Primary (+25 salvage) — 3 left", primary);

            string secondary = KillLog.Format("Default", WeaponSlot.Secondary, 10f, 0);
            Assert.AreEqual("killed a Default with Secondary (+10 salvage) — 0 left", secondary);

            string unknown = KillLog.FormatUnknownSlot("Ram", 40f, 1);
            Assert.AreEqual("killed a Ram (+40 salvage) — 1 left", unknown);
        }

        [Test]
        public void SecD_AbilityAndRing_UnsetPendingZo()
        {
            // M26 Open — do not invent these. Criterion 7/8 stay blocked until Zo authors them.
            Assert.AreEqual(AbilityId.None, WeaponCatalog.Def(WeaponId.SecD).abilityId);
            Assert.IsNull(WeaponWheel.RingOf(WeaponId.SecD));
            Assert.IsFalse(WeaponWheel.TryIdentity(WeaponId.SecD, out _, out _));
        }
    }
}
