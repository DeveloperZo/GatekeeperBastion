using System;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M32 — PRI-C catalog presence, full-room damage, sustained band vs MR-35,
    /// Wave-5 fire-time floor, Railshot dial, and ring row.</summary>
    public class PriCCatalogTests
    {
        const float Tol = 0.05f;

        /// <summary>Duty-cycle dps read straight off a catalog row: one clip fired, then a
        /// reload. DERIVED rather than transcribed, so this file never carries a second copy of
        /// the dps column — the whole Primary tier doubled on 2026-08-27 and a pasted band would
        /// have had to be re-typed in three fixtures instead of none.</summary>
        static float SustainedDps(WeaponDef def)
        {
            float perPull = def.damage * Math.Max(1, def.pellets);
            float cycle = def.clipSize / def.shotsPerSecond + def.reloadSeconds;
            return def.clipSize * perPull / cycle;
        }

        [Test]
        public void PriCIsInEveryCatalogList()
        {
            Assert.Greater(WeaponCatalog.ArraySize, (int)WeaponId.PriC);

            var def = WeaponCatalog.Def(WeaponId.PriC);
            Assert.AreEqual(WeaponSlot.Primary, def.slot);
            Assert.AreEqual("PRI-C", def.displayName);
            Assert.AreEqual(2500L, def.cost, "2,500 since 2026-09-02 (was 1,500); the rack gate reads the cheapest Primary, so a price move here moves the gate");

            bool inAll = false, inShop = false, inAvailable = false, inPrimary = false;
            foreach (WeaponId id in WeaponCatalog.All())
                if (id == WeaponId.PriC) inAll = true;
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
                if (id == WeaponId.PriC) inShop = true;
            inAvailable = WeaponCatalog.IsAvailable(WeaponId.PriC);
            foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
                if (id == WeaponId.PriC) inPrimary = true;

            Assert.IsTrue(inAll, "All()");
            Assert.IsTrue(inShop, "ShopWeapons()");
            Assert.IsTrue(inAvailable, "IsAvailable()");
            Assert.IsTrue(inPrimary, "ForSlot(Primary)");
        }

        [Test]
        public void Profile_MatchesAuthoredTable()
        {
            // Raised 90 -> 180 with the 2026-08-27 Primary tier raise. Nothing else on the row
            // moved: rate, clip, reload, range, falloff, pellets and pierce are the authored M32
            // values and this is the pin that says so.
            var def = WeaponCatalog.Def(WeaponId.PriC);
            Assert.AreEqual(180f, def.damage, Tol);
            Assert.AreEqual(0.6f, def.shotsPerSecond, Tol);
            Assert.AreEqual(4f, def.clipSize, Tol);
            Assert.AreEqual(3.2f, def.reloadSeconds, Tol);
            Assert.AreEqual(90f, def.range, Tol);
            Assert.AreEqual(55f, def.falloffStart, Tol);
            Assert.AreEqual(1, def.pellets);
            Assert.AreEqual(3, def.pierceCount);
            Assert.AreEqual(0.75f, def.pierceDamageMul, Tol);
            Assert.AreEqual(WeaponCrosshair.Dot, def.crosshair);
            Assert.AreEqual(AbilityId.PriCRailshot, def.abilityId);
        }

        [Test]
        public void FullDamage_AcrossBastionSightline()
        {
            // Criterion 3: falloffStart 55 exceeds ~50 m indoor sightlines, so no shot taken
            // inside the Bastion is ever a reduced one.
            //
            // WRITTEN AGAINST def.damage, NOT AGAINST 90. The literal was 90 and the 2026-08-27
            // raise took the row to 180 — at which point the last line, `Less(damage at 70 m, 90)`,
            // would have gone red for the wrong reason: 70 m still deals 102.9, which is more than
            // the OLD full-damage figure and a perfectly correct falloff. The claim was never
            // about the number 90, it is "full inside the room, reduced past falloffStart", and
            // reading the reference off the row is what makes it survive the next raise.
            var def = WeaponCatalog.Def(WeaponId.PriC);
            Assert.AreEqual(def.damage, WeaponWheel.DamageAtRange(def, 0, 50f), Tol);
            Assert.AreEqual(def.damage, WeaponWheel.DamageAtRange(def, 0, def.falloffStart), Tol);
            Assert.Less(WeaponWheel.DamageAtRange(def, 0, 70f), def.damage);
        }

        [Test]
        public void SustainedDps_WithinFivePercentOfMr35()
        {
            // Criterion 4, and the criterion is a RELATIONSHIP, not a pair of numbers. It read
            // "PRI-C 36.5 vs MR-35 37.0" until the 2026-08-27 Primary raise took both rows to
            // 73.0 and 74.0 — the same 1.4% apart. The band's internal shape is exactly what
            // survived that pass, so the shape is what is pinned here and the absolutes are
            // derived from the rows rather than transcribed beside them.
            var priCDef = WeaponCatalog.Def(WeaponId.PriC);
            var mr35Def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float priC = WeaponWheel.EffectiveDps(priCDef, new int[WeaponWheel.AttrCount]);
            float mr35 = WeaponWheel.EffectiveDps(mr35Def, new int[WeaponWheel.AttrCount]);

            // At zero wheel points the wheel applies no multiplier, so EffectiveDps must be the
            // plain duty cycle of the authored row. Pinning that is what lets the ratio below be
            // read as a balance claim rather than a claim about EffectiveDps.
            Assert.AreEqual(SustainedDps(priCDef), priC, 0.1f, "PRI-C T0 is its own row's duty cycle");
            Assert.AreEqual(SustainedDps(mr35Def), mr35, 0.1f, "MR-35 T0 is its own row's duty cycle");

            float ratio = Math.Abs(priC - mr35) / mr35;
            Assert.LessOrEqual(ratio, 0.05f, $"sustained gap {ratio:0.###} exceeds 5%");
        }

        /// <summary>Renamed FROM CannotCarryWave5Alone_PureFireTime on 2026-08-27: the 100 s
        /// threshold in the old assertion was 66 bodies at 0.6 shots/s, and the roster cut took
        /// wave 5 to 39 bodies and that figure to 65 s. Re-keying 100 to 65 would have pinned an
        /// arithmetic by-product of the roster instead of the criterion, and it would have had to
        /// be re-typed on the next roster move.
        ///
        /// The criterion is a COMPARISON — one PRI-C cannot keep up with what wave 5 delivers — so
        /// both sides are now read off the sim. It is stated twice, in the two units it can be
        /// wrong in: trigger rate against arrival rate, and total fire time against the delivery
        /// window. Both are measured in PRI-C's most generous case, ignoring reloads and assuming
        /// every shot kills.</summary>
        [Test]
        public void CannotCarryWave5Alone_TriggerRateLosesToArrivals()
        {
            var wave5 = new WaveDirector().NextWave(WaveDirector.MaxWaves - 1, siphonLevel: 0);
            var def = WeaponCatalog.Def(WeaponId.PriC);

            int groups = (wave5.enemyCount + wave5.groupSize - 1) / wave5.groupSize;
            float deliverySeconds = (groups - 1) * wave5.spawnInterval;
            float arrivalsPerSecond = wave5.enemyCount / deliverySeconds;

            Assert.Less(def.shotsPerSecond, arrivalsPerSecond,
                $"PRI-C pulls the trigger {def.shotsPerSecond}/s against {arrivalsPerSecond:0.##} "
                + "bodies/s of arrivals - even one kill per shot with no reload falls behind");

            float fireSeconds = wave5.enemyCount / def.shotsPerSecond;
            Assert.Greater(fireSeconds, deliverySeconds,
                $"pure fire time {fireSeconds:0.#}s must outlast the {deliverySeconds:0.#}s "
                + "delivery window, or one PRI-C could empty the field as it lands");
        }

        [Test]
        public void FarWall_PriCKillsWhereMr35DoesNot()
        {
            // Criterion 7 arithmetic: 45 m is inside PRI-C's falloffStart and well outside
            // MR-35's, so PRI-C deals its whole row and MR-35 deals ~43% of its own (15/35 of
            // the way from falloffStart 25 to range 60). Both sides read their own damage field:
            // the 2026-08-27 raise moved BOTH rows and the claim is unchanged by it, which is the
            // point of stating it as a comparison instead of as two numbers.
            float wave5Hp = WaveDirector.BaseEnemyHp
                * (1f + WaveDirector.EnemyHealthPerWave * (WaveDirector.MaxWaves - 1));
            var priC = WeaponCatalog.Def(WeaponId.PriC);
            var mr35 = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float priAt45 = WeaponWheel.DamageAtRange(priC, 0, 45f);
            float mrAt45 = WeaponWheel.DamageAtRange(mr35, 0, 45f);
            Assert.AreEqual(priC.damage, priAt45, Tol, "45 m is inside PRI-C's falloffStart");
            Assert.Less(mrAt45, mr35.damage, "45 m is outside MR-35's falloffStart");
            Assert.GreaterOrEqual(priAt45, wave5Hp);
            Assert.Less(mrAt45, wave5Hp);
        }

        [Test]
        public void RailshotDial_BodiesAtZeroAndMax()
        {
            Assert.AreEqual(3f, WeaponAbility.Magnitude(AbilityId.PriCRailshot, 0), Tol);
            Assert.AreEqual(8f,
                WeaponAbility.Magnitude(AbilityId.PriCRailshot, WeaponAbility.MaxDialPoints), Tol);
            Assert.AreEqual(12f, WeaponAbility.Cooldown(AbilityId.PriCRailshot), Tol);
            Assert.AreEqual(4f, WeaponAbility.PriCRailshotArmedSeconds, Tol);
        }

        [Test]
        public void Ring_MatchesAuthoredRow()
        {
            var ring = WeaponWheel.RingOf(WeaponId.PriC);
            Assert.IsNotNull(ring);
            Assert.AreEqual(5, ring.Length);
            Assert.AreEqual(WheelAttr.Clip, ring[0]);
            Assert.AreEqual(WheelAttr.Damage, ring[1]);
            Assert.AreEqual(WheelAttr.Reload, ring[2]);
            Assert.AreEqual(WheelAttr.Ability, ring[3]);
            Assert.AreEqual(WheelAttr.FireRate, ring[4]);
            Assert.IsTrue(WeaponWheel.TryIdentity(WeaponId.PriC, out var strength, out var weakness));
            Assert.AreEqual(WheelAttr.Damage, strength);
            Assert.AreEqual(WheelAttr.FireRate, weakness);
        }

        [Test]
        public void OneShot_NonThiefAtEveryWave()
        {
            // PRI-C's identity is one body per trigger pull at 0.6 shots/s, so the deepest body in
            // the table is what sizes its damage. That margin was 90 against 66 HP; the Primary
            // raise and the EnemyHealthPerWave restore to 0.40 both landed on 2026-08-27, then
            // BaseEnemyHp went 30 -> 50 the same day, so it is now 180 against 130 — a 1.38x
            // margin where it was 2.3x that morning. Still comfortable rather than knife-edge, but
            // the fewer-and-tougher pass spent most of the headroom this row was given, and the
            // wave the pin actually turns on is the LAST one, so it is named in the failure
            // message. Its PIERCED bodies are the tighter half: 180 x 0.75 = 135 against the same
            // 130, five points of slack, and the pierce is what this weapon is (see its row).
            var def = WeaponCatalog.Def(WeaponId.PriC);
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                float hp = WaveDirector.BaseEnemyHp
                    * (1f + WaveDirector.EnemyHealthPerWave * w);
                Assert.GreaterOrEqual(def.damage, hp,
                    $"Wave {w + 1} body {hp} must die to one PRI-C shot of {def.damage}");
            }
        }
    }
}
