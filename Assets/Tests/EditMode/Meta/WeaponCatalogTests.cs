using System;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M11 catalog + cost curve + migration, updated for M21's wheel-point shape.
    /// T0 (zero points) is the authored row's own duty cycle; higher "tiers" use the flat-tier
    /// migration distribution (damage+clip only) so pacing gates stay comparable.
    /// Every dps and HP figure below is DERIVED from WeaponCatalog / WaveDirector rather than
    /// transcribed — the 2026-08-27 pass moved the whole Primary tier and the enemy HP slope in
    /// one session, and a transcribed column would have gone red on both.</summary>
    public class WeaponCatalogTests
    {
        static int[] Flat(int tier)
        {
            var p = new int[WeaponWheel.AttrCount];
            WeaponWheel.DistributeFlatTier(p, tier);
            return p;
        }

        static float Dps(WeaponId id, int flatTier) =>
            WeaponWheel.EffectiveDps(WeaponCatalog.Def(id), Flat(flatTier));

        /// <summary>Duty-cycle dps read straight off a catalog row: one clip fired, then a
        /// reload. Derived so this fixture holds no second copy of the dps column.</summary>
        static float SustainedDps(WeaponDef def)
        {
            float perPull = def.damage * Math.Max(1, def.pellets);
            float cycle = def.clipSize / def.shotsPerSecond + def.reloadSeconds;
            return def.clipSize * perPull / cycle;
        }

        /// <summary>Sustained SHOTS per second — one clip, then a reload.</summary>
        static float ShotRate(WeaponDef def) =>
            def.clipSize / (def.clipSize / def.shotsPerSecond + def.reloadSeconds);

        /// <summary>Damage one trigger pull puts into ONE body, before overkill is charged.</summary>
        static float PerShot(WeaponDef def) => def.damage * Math.Max(1, def.pellets);

        /// <summary>Single-target kills per second against a body of <paramref name="hp"/>, at
        /// point blank and zero wheel points. THIS IS THE COLUMN THE CATALOG'S OWN COMMENTS SAY TO
        /// JUDGE A ROW ON, because it charges for overkill where dps does not: a shot that deals
        /// 133 into a 50 hp body still buys exactly one kill.</summary>
        static float KillsPerSecond(WeaponDef def, float hp) =>
            ShotRate(def) / (float)Math.Ceiling(hp / PerShot(def));

        /// <summary>Kills per second through a QUEUE — the pierce column, with every body's share
        /// capped at its hp so overkill is not paid for down the line either. PRI-C's row says in
        /// so many words that this is the number its damage was sized for.</summary>
        static float LineKillsPerSecond(WeaponDef def, float hp)
        {
            float delivered = Math.Min(PerShot(def), hp);
            for (int i = 0; i < def.pierceCount; i++)
                delivered += Math.Min(PerShot(def) * def.pierceDamageMul, hp);
            return ShotRate(def) * delivered / hp;
        }

        /// <summary>A Default body's hp on wave <paramref name="w"/> at dial 0.</summary>
        static float DefaultHp(int w) =>
            WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave * w);

        static float DeepestDefaultHp() => DefaultHp(WaveDirector.MaxWaves - 1);

        /// <summary>Single-target kills/s averaged over the authored ladder — one number for
        /// "how fast does this row clear a run", overkill charged wave by wave.</summary>
        static float RunAverageKillsPerSecond(WeaponDef def)
        {
            float sum = 0f;
            for (int w = 0; w < WaveDirector.MaxWaves; w++) sum += KillsPerSecond(def, DefaultHp(w));
            return sum / WaveDirector.MaxWaves;
        }

        /// <summary>Renamed FROM _MatchesPrintedBand on 2026-08-27. There is no printed band any
        /// more and there should not have been one: the numbers 33 / 37 / 38 / 59 sat here as
        /// literals and the 2x Primary raise turned three of them red in a single edit, which is
        /// the cost of transcribing a column instead of deriving it.
        ///
        /// WHAT THE BAND IS FOR, restated as assertions rather than as a table. The free MK-1
        /// Secondary sustained 59.0 dps (56.2 since the 2026-09-02 retune to 50 at 1.4/s, rate
        /// solved so the rifle still leads it on wave-1 kills/s). Every PURCHASABLE Primary sat at 33-37, so at equal tier
        /// the starting sidearm out-damaged a 1200-salvage rifle on every wave of the ladder —
        /// which is the whole reason the tier was raised ~2x (Zo: "It is the rifle. We should push
        /// the primary damage up"). That inversion is the thing that must never come back, so it
        /// is the thing pinned. The internal SHAPE of the Primary band is pinned beside it,
        /// because the raise deliberately preserved it: BR-12 ~10% under MR-35 and PRI-C within
        /// ~1.5% of it, before (33.2 / 37.0 / 36.5) and after (66.3 / 74.0 / 73.0) alike.
        ///
        /// AND THEN BR-12 LEFT THE DPS COLUMN ALTOGETHER, 2026-08-27. BaseEnemyHp 30 -> 50 would
        /// have pushed a wave-3 Default past the shotgun's 84-damage volley and halved the weapon
        /// at a stroke, so its pellets went 12 -> 19 to hold the one-shot. That bought no power —
        /// BR-12 one-shot every Default before and one-shots every Default now, so its kills/s is
        /// unchanged at 0.789 flat — but its dps column jumped to ~105, because dps counts every
        /// point of a 133-damage volley fired into a 50 hp body. The "BR-12 is 10% under MR-35"
        /// pin was therefore re-keyed rather than renumbered: the band's shape is now asserted in
        /// KILLS/S, where BR-12 and MR-35 sit within ~2% of each other across the ladder, and the
        /// dps comparison is kept only for the two rows whose columns are still honest.</summary>
        [Test]
        public void EffectiveDps_T0_IsTheRowsOwnDutyCycle_AndEveryPrimaryBeatsTheFreeSidearm()
        {
            // Zero points = old T0. Higher flat tiers diverge from the old dual-curve table by
            // design (M21); only the zero-investment band is pinned here.
            //
            // At zero wheel points there are no multipliers, so EffectiveDps must be nothing but
            // the authored row's duty cycle. That holds for every id in the catalog, throwaway
            // and disabled rows included, and it is what makes the comparisons below readable as
            // balance claims instead of claims about EffectiveDps.
            foreach (WeaponId id in WeaponCatalog.All())
            {
                var def = WeaponCatalog.Def(id);
                Assert.AreEqual(SustainedDps(def), Dps(id, 0), 0.01f, def.displayName + " T0");
            }

            // THE INVERSION GUARD. A Primary is a purchase; the sidearm is free.
            float sidearm = Dps(WeaponId.Mk1Sidearm, 0);
            foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
            {
                var def = WeaponCatalog.Def(id);
                Assert.Greater(Dps(id, 0), sidearm,
                    $"{def.displayName} costs {def.cost} salvage and sustains {Dps(id, 0):0.#} dps "
                    + $"against the FREE MK-1's {sidearm:0.#} — a Primary that loses to the "
                    + "starting sidearm at equal tier is not a purchase, it is a reskin");
            }

            // THE BAND SHAPE. PRI-C and MR-35 still shadow each other on the dps column, because
            // neither row moved on 2026-08-27 — that half of the shape is unchanged and is still
            // asserted where it always was.
            float mr35 = Dps(WeaponId.Mr35Rifle, 0);
            float priC = Dps(WeaponId.PriC, 0);
            Assert.LessOrEqual(Math.Abs(priC - mr35) / mr35, 0.02f,
                "PRI-C shadows MR-35 within ~1.5%, as it did at 36.5 against 37.0");

            // BR-12's HALF OF THE SHAPE MOVED TO A DIFFERENT COLUMN, and comparing it on dps is
            // now a category error rather than a balance claim: at 133 damage a volley into a
            // 50 hp body, most of that column is overkill. The claim that survives — and the one
            // the row was actually retuned to preserve — is that the shotgun and the rifle clear
            // the authored ladder at nearly the same rate.
            var br12Def = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            var mr35Def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float br12Kps = RunAverageKillsPerSecond(br12Def);
            float mr35Kps = RunAverageKillsPerSecond(mr35Def);
            Assert.LessOrEqual(Math.Abs(mr35Kps - br12Kps) / mr35Kps, 0.05f,
                $"BR-12 clears the ladder at {br12Kps:0.###} kills/s against MR-35's {mr35Kps:0.###}"
                + " — the band's shape lives in this column now, not in the dps one");

            // ...and BR-12's one-shot is FLAT across the whole ladder, which is both why its dps
            // column is meaningless and why the 12 -> 19 raise bought no power. If a Default ever
            // survives a volley this goes to two shots and the weapon halves in one step.
            Assert.AreEqual(KillsPerSecond(br12Def, DefaultHp(0)),
                            KillsPerSecond(br12Def, DeepestDefaultHp()), 1e-4f,
                "BR-12 must kill every Default in the table in exactly one volley");
            Assert.GreaterOrEqual(PerShot(br12Def), DeepestDefaultHp(),
                $"BR-12's volley is {PerShot(br12Def)} against a deepest Default of "
                + $"{DeepestDefaultHp()} — three points of slack, and it halves if that closes");
        }

        [Test]
        public void FlatMigration_Step_ExceedsNoiseFloor_AllSteps()
        {
            Assert.AreEqual(10, WeaponTier.MaxTier);
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var def = WeaponCatalog.Def(id);
                for (int t = 0; t < WeaponTier.MaxTier; t++)
                {
                    float a = WeaponWheel.EffectiveDps(def, Flat(t));
                    float b = WeaponWheel.EffectiveDps(def, Flat(t + 1));
                    float ratio = b / a;
                    Assert.Greater(ratio, 1.20f,
                        $"{def.displayName} flat T{t}→T{t + 1} ratio {ratio:0.###} must exceed 1.20");
                }
            }
        }

        [Test]
        public void TierCosts_LinearRamp_SumsTo13000()
        {
            long[] expected = { 400L, 600L, 800L, 1000L, 1200L, 1400L, 1600L, 1800L, 2000L, 2200L };
            for (int t = 0; t < WeaponTier.MaxTier; t++)
            {
                Assert.AreEqual(expected[t], WeaponTier.Cost(t), "cost at T" + t);
                Assert.AreEqual(expected[t], WeaponTier.Cost(WeaponSlot.Primary, t),
                    "Primary slot overload must match the shared curve at T" + t);
            }
            Assert.AreEqual(13000L, WeaponTier.SpentToReach(WeaponTier.MaxTier));
        }

        [Test]
        public void SecondaryTierCosts_LinearRamp_SumsTo8250()
        {
            long[] expected = { 150L, 300L, 450L, 600L, 750L, 900L, 1050L, 1200L, 1350L, 1500L };
            for (int t = 0; t < WeaponTier.MaxTier; t++)
                Assert.AreEqual(expected[t], WeaponTier.Cost(WeaponSlot.Secondary, t), "cost at T" + t);
            Assert.AreEqual(8250L,
                WeaponTier.SpentToReach(WeaponSlot.Secondary, WeaponTier.MaxTier));
        }

        /// <summary>Renamed FROM PrimaryBand_T0_AvailablePrimaries_Within15Percent on 2026-08-27,
        /// because the thing it measured stopped being a band.
        ///
        /// The old test took min and max of the RAW DPS COLUMN across the three available
        /// Primaries and required a spread under 15%. That held at 66.3 / 74.0 / 73.0. The
        /// BaseEnemyHp raise forced BR-12's pellets 12 -> 19 to keep its one-shot, its dps column
        /// went to ~105, and the spread went to 0.439. RE-KEYING THE TOLERANCE TO 0.44 WOULD HAVE
        /// BEEN THE WRONG FIX: it would have pinned the overkill instead of the balance, and gone
        /// on passing however far the column drifted.
        ///
        /// TWO OF THE THREE ROWS NOW OVERKILL EVERY BODY IN THE TABLE — BR-12 at 133 a volley and
        /// PRI-C at 180 a shot, against a deepest Default of 130 — so their dps columns describe
        /// damage that lands in a corpse. There is no single column all three can be compared on:
        /// on single-target kills/s PRI-C reads as half a weapon (its own row says so, and says
        /// the pierce is the weapon); on pierce throughput it leads by 2x. So what is asserted is
        /// the claim the 15% band existed to protect — NOTHING IN THE SHOP IS DOMINATED — by
        /// showing each available Primary is the outright best at one thing.</summary>
        [Test]
        public void PrimaryBand_T0_EachAvailablePrimaryLeadsOnOneAxis_SoNoneIsDominated()
        {
            var br12 = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            var mr35 = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            var priC = WeaponCatalog.Def(WeaponId.PriC);

            // The premise: those three ARE the shop's Primaries. If a fourth arrives, this test
            // has to be extended rather than silently continuing to describe three of four.
            int available = 0;
            foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary)) available++;
            Assert.AreEqual(3, available,
                "a Primary was added or removed - the dominance axes below cover three rows");

            float deepest = DeepestDefaultHp();
            float shallow = DefaultHp(0);

            // WHICH ROWS ARE OVERKILL ROWS, stated as arithmetic. This is what disqualifies the
            // dps column as a common yardstick, so it is pinned rather than asserted in prose.
            Assert.GreaterOrEqual(PerShot(br12), deepest, "BR-12 one-shots the deepest Default");
            Assert.GreaterOrEqual(PerShot(priC), deepest, "PRI-C one-shots the deepest Default");
            Assert.Less(PerShot(mr35), deepest,
                "MR-35 does not one-shot anything, which is why its dps column is still honest");

            // AXIS 1 — single-target kills/s on the DEEPEST body. The shotgun's whole point.
            Assert.Greater(KillsPerSecond(br12, deepest), KillsPerSecond(mr35, deepest));
            Assert.Greater(KillsPerSecond(br12, deepest), KillsPerSecond(priC, deepest));

            // AXIS 2 — single-target kills/s on the SHALLOW body. The rifle's whole point: small
            // bites land faster than one big one when the body is small.
            Assert.Greater(KillsPerSecond(mr35, shallow), KillsPerSecond(br12, shallow));
            Assert.Greater(KillsPerSecond(mr35, shallow), KillsPerSecond(priC, shallow));

            // AXIS 3 — throughput down a QUEUE. PRI-C's row says not to read its single-target
            // column, and this is the column it says to read instead.
            Assert.Greater(LineKillsPerSecond(priC, deepest), LineKillsPerSecond(br12, deepest));
            Assert.Greater(LineKillsPerSecond(priC, deepest), LineKillsPerSecond(mr35, deepest));

            // ...and the trap guard the band was really for: nothing in the shop loses to the FREE
            // sidearm on the metric it is best at.
            var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            Assert.Greater(KillsPerSecond(br12, deepest), KillsPerSecond(mk1, deepest));
            Assert.Greater(KillsPerSecond(mr35, shallow), KillsPerSecond(mk1, shallow));
            Assert.Greater(LineKillsPerSecond(priC, deepest), LineKillsPerSecond(mk1, deepest));
        }

        [Test]
        public void Vk7_StaysDisabled_NotInShop()
        {
            Assert.IsFalse(WeaponCatalog.IsAvailable(WeaponId.Vk7Rifle));
            foreach (var id in WeaponCatalog.ShopWeapons())
                Assert.AreNotEqual(WeaponId.Vk7Rifle, id);
        }

        [Test]
        public void Br12_OneShotsWave2Default_Inside8m_FailsAt14m()
        {
            var def = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            float wave2Hp = WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave);
            float at8 = WeaponWheel.DamageAtRange(def, 0, 8f) * def.pellets;
            float at14 = WeaponWheel.DamageAtRange(def, 0, 14f) * def.pellets;
            Assert.GreaterOrEqual(at8, wave2Hp, $"BR-12 at 8m deals {at8}, need ≥{wave2Hp}");
            Assert.Less(at14, wave2Hp, $"BR-12 at 14m deals {at14}, must fail to one-shot {wave2Hp}");
        }

        /// <summary>Renamed four times. From _InSevenShots for the 2026-08-27 recut — seven was
        /// the unit the old 32-damage cadence happened to be written in, not the claim — then from
        /// _InThreeAndAThirdSeconds when EnemyHealthPerWave fell 0.40 → 0.30 the same day, then
        /// from _InThreeSeconds for the pair of moves that landed together (MR-35 damage 8 → 16
        /// with the Primary tier raise, and EnemyHealthPerWave restored 0.30 → 0.40), and now from
        /// _InOneAndThreeQuarterSeconds for the fewer-and-tougher pass: the Thief went 120 → 200
        /// base, so a wave-3 Thief is 360 HP rather than 216 and the kill rose 1.75 s → 2.875 s.
        /// The claim is how long a wave-3 Thief survives focused fire, so the SECONDS are the pin,
        /// and the seconds move whenever either curve does — which is exactly why they are in the
        /// name and why the name is rewritten every time rather than left lying. Every HP figure
        /// below is DERIVED from WaveDirector rather than restated: this file must not carry a
        /// second copy of the enemy curve (Tools/lint-constants/run.sh enforces that).</summary>
        [Test]
        public void Mr35_KillsWave3Thief_InTwoAndSevenEighthsSeconds_AndTheClipStillOutlastsIt()
        {
            // 7 shots at 1.6/s was 4.375 s of held trigger. At 8 shots/s the same Thief dies off a
            // single held pull, which is the fantasy working. Nothing about a 27-round magazine is
            // absurd for an assault rifle against the one Gatekeeper built to eat sustained fire.
            //
            // THE MAGAZINE BOUNDARY, WHICH IS THE WHOLE REASON THIS PIN EXISTS. The clip was sized
            // against the toughest body one pull should kill. It has been on the wrong side of that
            // line before: at 26 rounds it fell 8 damage short and the kill spanned a reload for
            // 5.745 s of wall clock. Both assertions about that boundary are written over DERIVED
            // values so they keep meaning something when either curve moves again — a pin on the
            // exact shot count would have to be rewritten every time and would stop being a guard.
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float thiefBase = WaveDirector.StatsFor(GatekeeperKind.Thief).maxHp;
            float wave3ThiefHp = thiefBase * (1f + WaveDirector.EnemyHealthPerWave * 2f);
            float dmg = WeaponWheel.EffectiveDamage(def, 0);
            Assert.AreEqual(def.damage, dmg, 0.01f, "T0 is the authored row, no wheel multiplier");

            int shots = (int)Math.Ceiling(wave3ThiefHp / dmg);
            Assert.AreEqual(2.875f, shots / def.shotsPerSecond, 0.01f,
                $"{shots} shots on a wave-3 Thief ({wave3ThiefHp} HP) is the held-trigger time, "
                + "and it must stay well under the old 4.375 s marksman cadence");
            Assert.AreEqual(2, def.pierceCount);

            // THE MAGAZINE BOUNDARY HAS MOVED BACK ACROSS THE TABLE, ON PURPOSE.
            // This pin has now been on both sides of the line inside two days. Before the Primary
            // raise it hunted for "the first wave whose Thief outlasts a full magazine" and found
            // WAVE 4; the raise to 16 damage pushed every authored Thief inside one pull and the
            // search ran off the end, so the claim was restated as "no authored wave crosses a
            // reload". The 2026-08-27 fewer-and-tougher pass put the Thief at 200 base, and one
            // pull of 27 x 16 = 432 no longer covers a wave-4 Thief at 440 — so wave 4 is the
            // boundary again, and WaveDirector's own row says that is intended: "at 200 base it is
            // 520 HP on wave 5, and an MR-35 magazine carries 432, so the deepest Thief no longer
            // dies inside one pull. That is intended — it is the heavy."
            //
            // Pinning the boundary WAVE rather than "0" or "never" is what makes this survive the
            // next move in either direction: it goes red when the boundary walks, whichever way.
            int firstWaveNeedingAReload = 0;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                float hp = thiefBase * (1f + WaveDirector.EnemyHealthPerWave * w);
                if (Math.Ceiling(hp / dmg) > def.clipSize) { firstWaveNeedingAReload = w + 1; break; }
            }
            Assert.AreEqual(4, firstWaveNeedingAReload,
                "the Thief is the one body a magazine is sized against, and wave 4 is where it "
                + "stops fitting; a boundary at wave 3 or earlier means the rifle lost the mid-run "
                + "Thief, and one past wave 5 means the heavy stopped being heavy");

            float magazine = def.clipSize * dmg;
            float deepestThiefHp = thiefBase
                * (1f + WaveDirector.EnemyHealthPerWave * (WaveDirector.MaxWaves - 1));
            Assert.Less(magazine, deepestThiefHp,
                $"one pull carries {magazine} damage and the deepest Thief is {deepestThiefHp} HP "
                + "— the deepest Thief is MEANT to cost a reload now");

            // THE HALF THAT STILL HAS TO HOLD, and the reason this test is still called
            // _AndTheClipStillOutlastsIt: the wave-3 Thief — the one the rifle is bought to answer
            // — must die inside a single pull, with the spare rounds named so the margin is a
            // number and not a feeling.
            Assert.LessOrEqual(shots, (int)def.clipSize,
                $"a wave-3 Thief costs {shots} of {def.clipSize} rounds; crossing a reload here is "
                + "the regression that once cost 5.745 s of wall clock");
            Assert.AreEqual(4, (int)def.clipSize - shots, "rounds to spare on a wave-3 Thief");

            // The slope at which even THAT would stop holding, so the guard is stated in the units
            // of the curve that would eat it: EnemyHealthPerWave has to reach 0.58 before a wave-3
            // Thief outlasts one pull, against a live 0.40. The slope has moved twice and
            // BaseEnemyHp once inside a week, so this guard is not idle.
            float slopeThatCrossesAReload = (magazine / thiefBase - 1f) / 2f;
            Assert.Greater(slopeThatCrossesAReload, WaveDirector.EnemyHealthPerWave,
                $"EnemyHealthPerWave {WaveDirector.EnemyHealthPerWave} must stay under "
                + $"{slopeThatCrossesAReload:0.###}, where a wave-3 Thief first outlasts one pull");
        }

        [Test]
        public void ThrowawayComposite_OwnableEquippable_ComposesAllModes()
        {
            var id = WeaponId.ThrowawayComposite;
            var def = WeaponCatalog.Def(id);
            Assert.Greater(def.pellets, 1, "pellets");
            Assert.Greater(def.pierceCount, 0, "pierce");
            Assert.Greater(def.falloffStart, 0f, "falloff");

            var m = MetaState.CreateNew();
            m.EarnSalvage(def.cost);
            m.SetOwned(id, true);
            m.SetEquipped(id);
            Assert.IsTrue(m.Owns(id));
            Assert.AreEqual(id, m.Equipped(def.slot));

            float near = WeaponWheel.DamageAtRange(def, 0, def.falloffStart);
            float far = WeaponWheel.DamageAtRange(def, 0, def.range);
            Assert.Greater(near, far);
            Assert.AreEqual(0f, far, 0.01f);
        }

        [Test]
        public void Mk1_CostsZero_AndIsAlwaysAffordableOnFreshSave()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.IsFalse(m.OwnsAnyWeapon);
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost);
        }

        [Test]
        public void Json_RoundTrip_PreservesWeapons()
        {
            var m = MetaState.CreateNew();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.SetOwned(WeaponId.Br12Shotgun, true);
            m.ReplacePointsFromFlatTier(WeaponId.Br12Shotgun, 5);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            m.EquippedPrimary = WeaponId.Br12Shotgun;

            var loaded = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.IsTrue(loaded.Owns(WeaponId.Br12Shotgun));
            Assert.AreEqual(5, loaded.TierOf(WeaponId.Br12Shotgun));
            Assert.AreEqual(30, loaded.PointOf(WeaponId.Br12Shotgun, WheelAttr.Damage));
            Assert.AreEqual(WeaponId.Br12Shotgun, loaded.EquippedPrimary);
        }

        [Test]
        public void NoSetTier_OnMetaState()
        {
            Assert.IsNull(typeof(MetaState).GetMethod("SetTier"));
        }

        /// <summary>Criterion 14 / M16 — cheapest available Primary under three-death earnings.
        /// Uses flat-tier migration points as the comparable spend ladder.</summary>
        [Test]
        public void PacingTarget_CheapestAvailablePrimary_UnderThreeDeathsEarnings()
        {
            var waves = new[]
            {
                new { Wave = 2, Need = 50f, ThreeDeaths = 2700L },
                new { Wave = 3, Need = 98f, ThreeDeaths = 5490L },
            };

            foreach (var w in waves)
            {
                long cheapest = long.MaxValue;
                string who = null;
                foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
                {
                    var def = WeaponCatalog.Def(id);
                    for (int t = 0; t <= WeaponTier.MaxTier; t++)
                    {
                        if (WeaponWheel.EffectiveDps(def, Flat(t)) + 0.01f < w.Need) continue;
                        long cost = def.cost + WeaponTier.SpentToReach(t);
                        if (cost < cheapest)
                        {
                            cheapest = cost;
                            who = $"{def.displayName} T{t}";
                        }
                        break;
                    }
                }

                Assert.AreNotEqual(long.MaxValue, cheapest,
                    $"wave {w.Wave}: no available Primary reaches {w.Need} theoretical dps");
                Assert.Less(cheapest, w.ThreeDeaths,
                    $"wave {w.Wave}: {who} costs {cheapest}, must be < 3× death earnings {w.ThreeDeaths}");
            }
        }
    }
}
