using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>
    /// EditMode tests for the PLAYER ATTRIBUTES shop category (the third one, after ARMORY and
    /// FABRICATION): the four pinned effect formulas at every level plus clamping, the SHARED
    /// 200*2^level cost curve every attribute rides, persistence of all four levels (old saves
    /// default to 0), Scavenging driving run income end to end into MetaState.BankedSalvage,
    /// and the deliberate asymmetry that attributes are buyable before the first death while
    /// the weapon tracks stay gated behind it.
    /// </summary>
    public class PlayerAttributesTests
    {
        // ---- Track table -------------------------------------------------------------
        // The four attribute tracks addressed uniformly, so every cost/purchase rule can be
        // asserted against ALL of them rather than a representative one. Buy goes through the
        // exact same MetaUpgrades.Buy(meta, ref meta.XLevel) path the weapon tracks use.

        private delegate bool BuyFn(MetaState m);
        private delegate int LevelFn(MetaState m);

        private sealed class Track
        {
            public string Name;
            public BuyFn Buy;
            public LevelFn Level;
        }

        private static Track[] AttributeTracks()
        {
            return new[]
            {
                new Track { Name = "Vitality",    Buy = m => MetaUpgrades.Buy(m, ref m.VitalityLevel),    Level = m => m.VitalityLevel },
                new Track { Name = "Stamina",     Buy = m => MetaUpgrades.Buy(m, ref m.StaminaLevel),     Level = m => m.StaminaLevel },
                new Track { Name = "Scavenging",  Buy = m => MetaUpgrades.Buy(m, ref m.ScavengingLevel),  Level = m => m.ScavengingLevel },
                new Track { Name = "Luck",        Buy = m => MetaUpgrades.Buy(m, ref m.LuckLevel),        Level = m => m.LuckLevel },
            };
        }

        // ---- Effect formulas ---------------------------------------------------------
        // THE MODEST RETUNE: every track is base * (1 + 0.05*lvl) — +5% per level, +25% across
        // a full track. The old ramps grew +100%..+250% end to end; that was thrown out
        // deliberately. AllTracks_GrowExactly25PercentFromLevelZeroToMax at the bottom of this
        // file is the single guard on that intent.
        //
        // TOLERANCE: 1e-5f rather than 1e-6f. base*(1 + 0.05f*lvl) in float32 lands up to ~8e-6
        // from the decimal literal (100*(1+0.05*1) computes to 104.999992). 1e-5 absolute is
        // ~1e-7 relative and orders of magnitude below one level's 5% step, so these still pin
        // the formula exactly — they just stop pinning IEEE noise.
        private const float Tol = 1e-5f;

        [Test]
        public void Vitality_PlayerMaxHp_Exact100Base_Plus5PercentPerLevel()
        {
            float[] expected = { 100f, 105f, 110f, 115f, 120f, 125f };
            for (int l = 0; l <= MetaUpgrades.MaxLevel; l++)
                Assert.AreEqual(expected[l], MetaUpgrades.PlayerMaxHp(l), Tol, "level " + l);
        }

        [Test]
        public void Stamina_StaminaMax_Exact100Base_Plus5PercentPerLevel()
        {
            float[] expected = { 100f, 105f, 110f, 115f, 120f, 125f };
            for (int l = 0; l <= MetaUpgrades.MaxLevel; l++)
                Assert.AreEqual(expected[l], MetaUpgrades.StaminaMax(l), Tol, "level " + l);
        }

        [Test]
        public void Scavenging_SalvagePerKill_Exact25Base_Plus5PercentPerLevel()
        {
            // Fractional on purpose: 26.25 / 28.75 / 31.25 are real values, not rounding slop.
            // What the BANK receives is the floored long — see the banking tests below.
            float[] expected = { 25f, 26.25f, 27.5f, 28.75f, 30f, 31.25f };
            for (int l = 0; l <= MetaUpgrades.MaxLevel; l++)
                Assert.AreEqual(expected[l], MetaUpgrades.SalvagePerKill(l), Tol, "level " + l);
        }

        [Test]
        public void Luck_CritChance_Exact5PercentPerLevel_AsFractionNotPercent()
        {
            // Luck is the one ADDITIVE-from-zero track: 0.05*lvl, so it reaches 0.25 rather than
            // growing 25% off a nonzero base. Same +5%-per-level shape as everything else.
            float[] expected = { 0f, 0.05f, 0.10f, 0.15f, 0.20f, 0.25f };
            for (int l = 0; l <= MetaUpgrades.MaxLevel; l++)
                Assert.AreEqual(expected[l], MetaUpgrades.CritChance(l), Tol, "level " + l);

            // A fraction, never a percent: max Luck is 0.25, i.e. 25% — not 25.0.
            Assert.AreEqual(0.25f, MetaUpgrades.CritChance(MetaUpgrades.MaxLevel), Tol,
                "max crit chance is the fraction 0.25, not the number 25");
            Assert.AreEqual(0f, MetaUpgrades.CritChance(0), Tol, "no crit at all without Luck");
        }

        [Test]
        public void AllFour_ClampBelowZeroToLevelZero()
        {
            foreach (int bad in new[] { -1, -3, -100 })
            {
                Assert.AreEqual(100f, MetaUpgrades.PlayerMaxHp(bad), Tol, "hp " + bad);
                Assert.AreEqual(100f, MetaUpgrades.StaminaMax(bad), Tol, "stamina " + bad);
                Assert.AreEqual(25f, MetaUpgrades.SalvagePerKill(bad), Tol, "salvage " + bad);
                Assert.AreEqual(0f, MetaUpgrades.CritChance(bad), Tol, "crit " + bad);
            }
        }

        [Test]
        public void AllFour_ClampAboveMaxToLevelFive()
        {
            foreach (int bad in new[] { 6, 7, 99 })
            {
                Assert.AreEqual(125f, MetaUpgrades.PlayerMaxHp(bad), Tol, "hp " + bad);
                Assert.AreEqual(125f, MetaUpgrades.StaminaMax(bad), Tol, "stamina " + bad);
                Assert.AreEqual(31.25f, MetaUpgrades.SalvagePerKill(bad), Tol, "salvage " + bad);
                Assert.AreEqual(0.25f, MetaUpgrades.CritChance(bad), Tol, "crit " + bad);
            }
        }

        // ---- Shared cost curve and purchase rules ------------------------------------

        [Test]
        public void EveryAttributeTrack_UsesTheSharedDoublingCurve_200To3200()
        {
            long[] expected = { 200L, 400L, 800L, 1600L, 3200L };
            foreach (var track in AttributeTracks())
            {
                var meta = new MetaState();
                meta.BankedSalvage = 6200L; // 200+400+800+1600+3200
                for (int i = 0; i < MetaUpgrades.MaxLevel; i++)
                {
                    long before = meta.BankedSalvage;
                    Assert.IsTrue(track.Buy(meta), track.Name + " buy at level " + i);
                    Assert.AreEqual(i + 1, track.Level(meta), track.Name + " level after buy " + i);
                    Assert.AreEqual(expected[i], before - meta.BankedSalvage,
                        track.Name + " cost of level " + i + " → " + (i + 1));
                }
                Assert.AreEqual(MetaUpgrades.MaxLevel, track.Level(meta), track.Name + " maxed");
                Assert.AreEqual(0L, meta.BankedSalvage,
                    track.Name + ": the shared curve sums to exactly 6200");
            }
        }

        [Test]
        public void EveryAttributeTrack_RefusedAtMaxLevel_NoMutation()
        {
            foreach (var track in AttributeTracks())
            {
                var meta = new MetaState();
                meta.BankedSalvage = 6200L;
                for (int i = 0; i < MetaUpgrades.MaxLevel; i++) track.Buy(meta);

                meta.BankedSalvage = 1000000L; // rich, but there is nothing left to buy
                Assert.IsFalse(track.Buy(meta), track.Name + " refused at max");
                Assert.AreEqual(MetaUpgrades.MaxLevel, track.Level(meta), track.Name + " level unchanged");
                Assert.AreEqual(1000000L, meta.BankedSalvage, track.Name + " bank untouched");
            }
        }

        [Test]
        public void EveryAttributeTrack_RefusedWhenPoor_NoMutation()
        {
            foreach (var track in AttributeTracks())
            {
                var meta = new MetaState();
                meta.BankedSalvage = 199L; // one short of the 200 first level
                Assert.IsFalse(track.Buy(meta), track.Name + " refused at 199");
                Assert.AreEqual(0, track.Level(meta), track.Name + " level unchanged");
                Assert.AreEqual(199L, meta.BankedSalvage, track.Name + " bank untouched");

                meta.BankedSalvage = 200L; // exactly affordable
                Assert.IsTrue(track.Buy(meta), track.Name + " affordable at exactly 200");
                Assert.AreEqual(1, track.Level(meta), track.Name + " level 1");
                Assert.AreEqual(0L, meta.BankedSalvage, track.Name + " bank spent to zero");

                // Now one short of the 400 second level.
                meta.BankedSalvage = 399L;
                Assert.IsFalse(track.Buy(meta), track.Name + " refused at 399 for level 2");
                Assert.AreEqual(1, track.Level(meta), track.Name + " still level 1");
                Assert.AreEqual(399L, meta.BankedSalvage, track.Name + " bank untouched");
            }
        }

        [Test]
        public void AttributeTracks_ShareOneEconomy_BuyingOneSpendsTheSameBank()
        {
            // There is no second currency and no per-category wallet: all tracks draw down
            // the one BankedSalvage pool.
            var meta = new MetaState();
            meta.BankedSalvage = 1000L;

            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.VitalityLevel));
            Assert.AreEqual(800L, meta.BankedSalvage, "Vitality L1 costs 200");

            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.LuckLevel));
            Assert.AreEqual(600L, meta.BankedSalvage, "Luck L1 costs 200 from the SAME bank");

            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.HatchArmorLevel));
            Assert.AreEqual(400L, meta.BankedSalvage, "and the fab track spends that bank too");

            // Each track's own level drives its own price, independently of the others.
            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.VitalityLevel));
            Assert.AreEqual(0L, meta.BankedSalvage, "Vitality L2 costs 400");
            Assert.AreEqual(2, meta.VitalityLevel);
            Assert.AreEqual(1, meta.LuckLevel);
        }

        // ---- Persistence -------------------------------------------------------------

        [Test]
        public void Json_RoundTrip_PreservesAllFourAttributeLevels()
        {
            var meta = new MetaState();
            meta.VitalityLevel = 1;
            meta.StaminaLevel = 2;
            meta.ScavengingLevel = 4;
            meta.LuckLevel = 5;

            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(1, loaded.VitalityLevel);
            Assert.AreEqual(2, loaded.StaminaLevel);
            Assert.AreEqual(4, loaded.ScavengingLevel);
            Assert.AreEqual(5, loaded.LuckLevel);
        }

        [Test]
        public void Json_OldSaveWithoutAttributeFields_DefaultsAllFourToZero()
        {
            // A save written before PLAYER ATTRIBUTES existed: the fields are absent
            // entirely, and must load forward as 0 rather than throwing or corrupting.
            var loaded = MetaState.FromJson(
                "{\"SaveVersion\":1,\"BankedSalvage\":5000,\"TotalRuns\":9,\"PodEjections\":2," +
                "\"GunDamageLevel\":3,\"WeaponOwned\":true}");

            Assert.AreEqual(5000L, loaded.BankedSalvage, "pre-existing fields still load");
            Assert.IsTrue(loaded.Owns(WeaponId.Mk1Sidearm), "v1 WeaponOwned migrates to MK-1");
            Assert.AreEqual(3, loaded.TierOf(WeaponId.Mk1Sidearm), "tier from GunDamageLevel");

            Assert.AreEqual(0, loaded.VitalityLevel, "Vitality defaults to 0");
            Assert.AreEqual(0, loaded.StaminaLevel, "Stamina defaults to 0");
            Assert.AreEqual(0, loaded.ScavengingLevel, "Scavenging defaults to 0");
            Assert.AreEqual(0, loaded.LuckLevel, "Luck defaults to 0");
        }

        [Test]
        public void SaveData_RoundTrip_PreservesAllFourAttributeLevels()
        {
            var meta = new MetaState();
            meta.VitalityLevel = 5;
            meta.StaminaLevel = 4;
            meta.ScavengingLevel = 2;
            meta.LuckLevel = 1;

            SaveData dto = meta.ToSaveData();
            Assert.AreEqual(5, dto.VitalityLevel, "DTO carries Vitality");
            Assert.AreEqual(4, dto.StaminaLevel, "DTO carries Stamina");
            Assert.AreEqual(2, dto.ScavengingLevel, "DTO carries Scavenging");
            Assert.AreEqual(1, dto.LuckLevel, "DTO carries Luck");

            var restored = MetaState.FromSaveData(dto);
            Assert.AreEqual(5, restored.VitalityLevel);
            Assert.AreEqual(4, restored.StaminaLevel);
            Assert.AreEqual(2, restored.ScavengingLevel);
            Assert.AreEqual(1, restored.LuckLevel);
        }

        [Test]
        public void SaveData_FreshDto_HasAllFourAttributesAtZero()
        {
            var restored = MetaState.FromSaveData(new SaveData());
            Assert.AreEqual(0, restored.VitalityLevel);
            Assert.AreEqual(0, restored.StaminaLevel);
            Assert.AreEqual(0, restored.ScavengingLevel);
            Assert.AreEqual(0, restored.LuckLevel);
        }

        // ---- Scavenging drives income, end to end ------------------------------------

        /// <summary>Run one kill at the given Scavenging level and siphon dial, and return what
        /// actually landed in MetaState.BankedSalvage.</summary>
        private static long BankOneKill(int scavengingLevel, int siphonLevel, out float killValue)
        {
            var meta = new MetaState();
            meta.BankedSalvage = 0L;
            meta.ScavengingLevel = scavengingLevel;

            var sim = new RunSim(meta, seed: 4242);
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(siphonLevel);
            sim.TriggerDraw();
            killValue = sim.CurrentKillValue;
            sim.AwardKill();
            return sim.Unbanked;
        }

        [Test]
        public void Scavenging_Level0_SiphonZero_BanksExactly25()
        {
            float killValue;
            long banked = BankOneKill(0, 0, out killValue);
            Assert.AreEqual(25f, killValue, 1e-5f, "CurrentKillValue at Scavenging 0 / siphon 0");
            Assert.AreEqual(25L, banked, "one kill banks exactly 25");
        }

        [Test]
        public void Scavenging_Level3_SiphonZero_Banks28_TheFloorOf28Point75()
        {
            // The retune made SalvagePerKill fractional: 25 * (1 + 0.05*3) = 28.75. The HUD
            // number (CurrentKillValue) is the true 28.75; RunSim.AwardKill banks
            // (long)Math.Floor(CurrentKillValue), so the BANK gets 28. That gap is the code's
            // actual behaviour and is asserted as such rather than rounded away.
            float killValue;
            long banked = BankOneKill(3, 0, out killValue);
            Assert.AreEqual(28.75f, killValue, 1e-5f, "25 * (1 + 0.05*3) = 28.75, siphon 1x");
            Assert.AreEqual(28L, banked, "buying Scavenging raises the bank, floored: 28.75 → 28");
        }

        [Test]
        public void Scavenging_Level5_SiphonThree_Banks39_TheFloorOf39Point0625()
        {
            // Both dials multiply: 31.25 * 1.25 = 39.0625, floored into the bank as 39.
            float killValue;
            long banked = BankOneKill(5, 3, out killValue);
            Assert.AreEqual(39.0625f, killValue, 1e-5f, "31.25 * 1.25 = 39.0625");
            Assert.AreEqual(39L, banked, "Scavenging and the siphon multiply, then floor: 39");
        }

        [Test]
        public void Scavenging_Level0_SiphonFive_Banks37_TheFloorOf37Point5()
        {
            // The top of the dial on a fresh save: 25 * 1.5 = 37.5. The HUD number is the true
            // 37.5; RunSim.AwardKill banks (long)Math.Floor(CurrentKillValue), so 37 lands.
            // This is the whole reward retune in one assertion — it used to be 25 * 32 = 800.
            float killValue;
            long banked = BankOneKill(0, 5, out killValue);
            Assert.AreEqual(37.5f, killValue, 1e-5f, "25 * 1.5 = 37.5 at Scavenging 0 / dial 5");
            Assert.AreEqual(37L, banked, "banked as floor(37.5) = 37, not 38 and not 800");
        }

        [Test]
        public void Scavenging_EveryLevel_BanksFlooredBaseTimesSiphonMultiplier()
        {
            // The full grid. Base is 25 * (1 + 0.05*lvl) = 25 / 26.25 / 27.5 / 28.75 / 30 / 31.25,
            // times the siphon's hand-authored curve (1 / 1.1 / 1.15 / 1.25 / 1.35 / 1.5); the
            // BANK takes floor() of that product. The whole grid now spans 25..46, not 25..1000.
            //
            // The floor is applied to the PRODUCT, not to the base — that is why the columns are
            // not 5% apart. The sim computes in float32 (25f*(1f+0.05f*1) is 26.2499981), but at
            // this scale no product lands close enough to an integer for the shortfall to move a
            // floor, so every cell below is also the exact-arithmetic answer.
            float[] killBase = { 25f, 26.25f, 27.5f, 28.75f, 30f, 31.25f };
            float[] dial = { 1.00f, 1.10f, 1.15f, 1.25f, 1.35f, 1.50f };
            long[][] bankedGrid =
            {
                new[] { 25L, 27L, 28L, 31L, 33L, 37L }, // Scavenging 0
                new[] { 26L, 28L, 30L, 32L, 35L, 39L }, // Scavenging 1
                new[] { 27L, 30L, 31L, 34L, 37L, 41L }, // Scavenging 2
                new[] { 28L, 31L, 33L, 35L, 38L, 43L }, // Scavenging 3
                new[] { 30L, 33L, 34L, 37L, 40L, 45L }, // Scavenging 4
                new[] { 31L, 34L, 35L, 39L, 42L, 46L }, // Scavenging 5
            };

            for (int scav = 0; scav <= MetaUpgrades.MaxLevel; scav++)
            {
                for (int siphon = 0; siphon <= TetherSiphon.MaxLevel; siphon++)
                {
                    float killValue;
                    long banked = BankOneKill(scav, siphon, out killValue);
                    Assert.AreEqual(bankedGrid[scav][siphon], banked,
                        "banked, Scavenging " + scav + " / siphon " + siphon);
                    Assert.AreEqual(killBase[scav] * dial[siphon], killValue, 1e-4f,
                        "CurrentKillValue Scavenging " + scav + " / siphon " + siphon);
                    Assert.AreEqual(TetherSiphon.RewardMultiplierFor(siphon), dial[siphon], 1e-6f,
                        "the dial column must be the sim's own curve, siphon " + siphon);
                }
            }
        }

        [Test]
        public void Scavenging_PurchasedMidRun_AppliesToTheVeryNextKill()
        {
            // CurrentKillValue reads the MetaState fresh, so a purchase between waves pays out
            // immediately rather than at the next run.
            var meta = new MetaState();
            meta.BankedSalvage = 0L;
            var sim = new RunSim(meta, seed: 77);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            sim.AwardKill();
            Assert.AreEqual(25L, sim.Unbanked, "first kill at Scavenging 0");
            Assert.AreEqual(0L, meta.BankedSalvage);

            meta.ScavengingLevel = 5; // as if bought at the ship
            Assert.AreEqual(31.25f, sim.CurrentKillValue, 1e-5f,
                "the very next kill is already priced at the maxed rate");
            sim.AwardKill();
            Assert.AreEqual(56L, sim.Unbanked,
                "next kill is worth 31.25, unbanked as floor() = 31, not 25");
        }

        // ---- The gating asymmetry ----------------------------------------------------

        [Test]
        public void Attributes_BuyableAtZeroDeaths_WhileWeaponTracksAreStillGated()
        {
            // The whole point of the category: attributes are the ANSWER to dying, so they must
            // be purchasable before you have ever died. Weapon upgrades are not.
            var meta = new MetaState();
            meta.BankedSalvage = 10000L;
            Assert.AreEqual(0, meta.PodEjections, "a player who has never died");

            // Half one: the weapon-upgrade gate is SHUT. This is the flag the ship stations key
            // the ARMORY/FABRICATION tracks off; while it is false those tracks are unbuyable.
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta),
                "weapon tracks are gated behind the first death");

            // Half two: every attribute buys anyway, through the same Buy path, with the gate
            // still shut — the attribute tracks never consult UpgradesUnlocked.
            foreach (var track in AttributeTracks())
            {
                Assert.IsTrue(track.Buy(meta),
                    track.Name + " must be buyable with zero deaths");
                Assert.AreEqual(1, track.Level(meta), track.Name + " actually leveled");
            }
            Assert.AreEqual(9200L, meta.BankedSalvage, "four attributes at 200 each = 800 spent");

            // Buying attributes does not open the weapon gate either — only a death does.
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta),
                "still gated: buying attributes is not a substitute for dying");
            meta.PodEjections = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "the first death is what opens the weapon tracks");
        }

        [Test]
        public void Attributes_BuyableOnAFreshSave_BeforeTheFirstRunEvenStarts()
        {
            // A brand-new save that has banked some salvage from run one, still 0 deaths.
            var meta = MetaState.CreateNew();
            meta.BankedSalvage += 200L;
            Assert.AreEqual(0, meta.PodEjections);
            Assert.AreEqual(0, meta.TotalRuns, "not a single run finished");

            Assert.IsTrue(MetaUpgrades.Buy(meta, ref meta.VitalityLevel),
                "Vitality is buyable on a save that has never seen a death");
            Assert.AreEqual(1, meta.VitalityLevel);
            Assert.AreEqual(105f, MetaUpgrades.PlayerMaxHp(meta.VitalityLevel), Tol,
                "and it takes effect: 100 → 105 max HP");
        }

        // ---- The retune's intent, pinned once ----------------------------------------

        [Test]
        public void AllTracks_GrowExactly25PercentFromLevelZeroToMax()
        {
            // ONE guard on the whole retune. The developer's brief was that tracks growing
            // +100% to +250% end to end were "horribly huge" and everything should be modest,
            // "in the realm of 10-30 percent". Every track is therefore base * (1 + 0.05*lvl):
            // +5% per level, +25% from level 0 to level 5 — no exceptions, no track quietly
            // steeper than the rest.
            //
            // If a formula is ever made steeper (or flatter) again, this fails even if that
            // track's own table test was updated alongside it, because this asserts the SHAPE
            // rather than the numbers. Do not relax it to accommodate one "special" track —
            // one steep track is exactly the thing this exists to catch.
            var tracks = new[]
            {
                new { Name = "HatchHp",               F = (System.Func<int, float>)MetaUpgrades.HatchHp },
                new { Name = "PlayerMaxHp",           F = (System.Func<int, float>)MetaUpgrades.PlayerMaxHp },
                new { Name = "StaminaMax",            F = (System.Func<int, float>)MetaUpgrades.StaminaMax },
                new { Name = "SalvagePerKill",        F = (System.Func<int, float>)MetaUpgrades.SalvagePerKill },
            };

            foreach (var t in tracks)
            {
                float atZero = t.F(0);
                Assert.Greater(atZero, 0f, t.Name + " must have a nonzero level-0 base to grow from");

                Assert.AreEqual(1.25f, t.F(MetaUpgrades.MaxLevel) / atZero, 1e-5f,
                    t.Name + ": a full track must be exactly +25%, not more");

                // And it gets there in five equal +5% steps, so no track is back-loaded.
                for (int l = 0; l <= MetaUpgrades.MaxLevel; l++)
                    Assert.AreEqual(1f + 0.05f * l, t.F(l) / atZero, 1e-5f,
                        t.Name + ": level " + l + " must be +5% per level off the base");
            }

            // LUCK is the one additive-from-zero track — there is no base to grow 25% off, so the
            // same "modest" intent reads as reaching the fraction 0.25 in five 0.05 steps.
            Assert.AreEqual(0f, MetaUpgrades.CritChance(0), 1e-5f, "CritChance starts at zero");
            Assert.AreEqual(0.25f, MetaUpgrades.CritChance(MetaUpgrades.MaxLevel), 1e-5f,
                "CritChance: a full track reaches exactly 0.25");
            for (int l = 0; l <= MetaUpgrades.MaxLevel; l++)
                Assert.AreEqual(0.05f * l, MetaUpgrades.CritChance(l), 1e-5f,
                    "CritChance: level " + l + " must be a flat 0.05 per level");
        }
    }
}
