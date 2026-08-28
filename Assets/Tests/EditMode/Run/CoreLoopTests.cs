using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// Explicit end-to-end check of the shipped loop, in the three-wave shape:
    /// Splash/Menu (StartRun, Docked) → Ship → Embark (EngageTether, Prep) → Start
    /// (TriggerDraw) → Fight (each kill banks currency instantly — nothing drips from the
    /// clock) → clear wave 1 → draw wave 2 → DIE mid-wave-2, exactly where the retuned curve
    /// says an unupgraded player dies → keep every credit earned → respawn on the ship for
    /// run 2 to spend it. Sim-level twin of the in-editor play verification; all numbers exact.
    /// </summary>
    public class CoreLoopTests
    {
        private static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        /// <summary>What the bank sees for <paramref name="kills"/> bodies at
        /// <paramref name="siphonLevel"/>, floored per body as AwardKill does. DERIVED — the
        /// 2026-08-27 pass moved SalvagePerKill 25 -> 42 AND cut the roster ~40%, so every
        /// running total in this fixture moved twice over.</summary>
        private static long Banked(int siphonLevel, int kills) =>
            kills * (long)(MetaUpgrades.SalvagePerKill()
                           * TetherSiphon.RewardMultiplierFor(siphonLevel));

        [Test]
        public void CoreLoop_Start_Prep_Draw_Kills_Clear_DieOnWaveTwo_RespawnKeepingEveryCredit()
        {
            // A real save: 0 banked, nothing owned — MK-1 is free at the ARMORY.
            var meta = MetaState.CreateNew();
            Assert.AreEqual(0L, meta.BankedSalvage);

            // --- Start: menu aboard the ship ---
            var sim = new RunSim(meta, 42);
            sim.StartRun();
            Assert.AreEqual(RunPhase.Docked, sim.Phase);
            Assert.AreEqual(1, meta.TotalRuns);

            // --- Board: lands in PREP; idling earns NOTHING and spawns nothing ---
            sim.EngageTether();
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            var specs = new System.Collections.Generic.List<WaveSpec>();
            int assaultsEnded = 0;
            sim.WaveStarted += w => specs.Add(w);
            sim.AssaultEnded += () => assaultsEnded++;
            for (int i = 0; i < 20; i++) sim.Tick(0.5f); // 10 idle seconds
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "prep pays nothing");
            Assert.AreEqual(0L, meta.BankedSalvage, "and pays nothing into the bank either");
            Assert.AreEqual(0, specs.Count, "no auto waves — the draw is the bell");
            Assert.AreEqual(10f, sim.RunElapsed, 1e-4f, "tethered time still counts");

            // --- Wave 1: the unupgraded, dial-at-rest draw ---
            sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            int waveOne = new WaveDirector().NextWave(0, 0).enemyCount;
            Assert.AreEqual(waveOne, specs[0].enemyCount, "wave 1 at siphon 0");
            Assert.AreEqual(waveOne, specs[0].defaultCount,
                "all Default: wave 1 is the game as it was");
            Assert.AreEqual(1.0f, specs[0].enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(3, specs[0].groupSize, "three at a time");
            Assert.AreEqual(7.3f, specs[0].spawnInterval, 1e-5f, "seconds between GROUPS");

            for (int i = 0; i < 60; i++) sim.Tick(0.5f); // 30 assault seconds, zero kills
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "the clock never pays");
            Assert.AreEqual(0L, meta.BankedSalvage, "still zero — no grubstake");

            // Re-key 2026-08-25: kills bank as they land, so the wave's whole payout is already
            // the player's here. The Hatch-bank is no longer the toll that decides whether wave
            // 1 counted; it is an opt-in bet on top of a take that is already secured.
            Kill(sim, waveOne); // the whole wave down
            long afterWaveOne = Banked(0, waveOne);
            Assert.AreEqual(afterWaveOne, meta.BankedSalvage,
                "kills bank the instant the body falls");
            Assert.AreEqual(0L, sim.Unbanked, "and stake nothing — only a Withdraw fills the pile");
            Assert.AreEqual(afterWaveOne, sim.Salvage.RunSalvage);

            // --- Clear wave 1: back to PREP, the run is still live (waves remain) ---
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(1, assaultsEnded);
            Assert.IsFalse(sim.AllWavesCleared);
            Kill(sim, 4); // no gatekeepers exist in prep — refused
            for (int i = 0; i < 20; i++) sim.Tick(0.5f);
            Assert.AreEqual(afterWaveOne, meta.BankedSalvage,
                "prep income is zero and refused kills pay nothing");
            Assert.AreEqual(0L, sim.Unbanked);

            // --- Wave 2: the wall. The first Hunters arrive, at one slope step above
            // BaseEnemyHp, four at a time every 4.40s. That step has moved three times in two
            // days — 0.40 -> 0.30 -> 0.40 on the slope, then BaseEnemyHp 30 -> 50 — so it is
            // DERIVED here and not transcribed: this fixture's subject is the shape of the loop
            // (draw, kill, bank, clear, die, keep) and it should not be one of the places a
            // tuning pass has to hunt down.
            //
            // THE BODY COUNTS ARE DERIVED NOW TOO. They were literals — 30 / 26 / 4 — on the
            // grounds that the authored wave table is a claim this test makes; the roster cut
            // turned all three red at once and proved the claim belongs in the fixture that owns
            // that table (GatekeeperVariantsTests). What survives here, and is what this test
            // means by "the wall", is that wave 2 is BIGGER than wave 1 and is the wave the
            // Hunter debuts in while the Thief and the Cloaker still wait. ---
            sim.TriggerDraw();
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            Assert.AreEqual(new WaveDirector().NextWave(1, 0).enemyCount, specs[1].enemyCount);
            Assert.Greater(specs[1].enemyCount, waveOne, "wave 2 is the step up, or it is no wall");
            Assert.AreEqual(specs[1].enemyCount - specs[1].hunterCount, specs[1].defaultCount,
                "everything that is not a Hunter is still a Default on wave 2");
            Assert.Greater(specs[1].hunterCount, 0, "the wave that teaches the Hunter");
            Assert.AreEqual(0, specs[1].thiefCount, "the Thief waits for wave 3");
            Assert.AreEqual(0, specs[1].cloakerCount, "and the Cloaker for wave 4");
            Assert.AreEqual(1f + WaveDirector.EnemyHealthPerWave,
                specs[1].enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave),
                WaveDirector.BaseEnemyHp * specs[1].enemyHealthMultiplier, 1e-3f);

            Kill(sim, 4); // four down before being overwhelmed
            long atDeath = afterWaveOne + Banked(0, 4);
            Assert.AreEqual(atDeath, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked, "still nothing staked");

            // --- Die mid-wave-2, exactly where the retuned curve says an unupgraded player
            // dies. THE RULING (Zo, 2026-08-25): it must be impossible to die with no progress
            // to show for it. This is the test that says so — the pod leaves with all 550,
            // because the death's wipe can only reach a pile the player chose to stake and
            // there is no such pile. Re-key 2026-08-25 of the old "down wipes unbanked; the
            // ship bank never received it", which was the same death with nothing behind it. ---
            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual("player", sim.LastRunReport.cause);
            Assert.AreEqual(atDeath, sim.LastRunReport.grossEarned,
                (waveOne + 4) + " kills, all of them kept");
            Assert.AreEqual(atDeath, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual(0, meta.CleanExits, "a death is not a clean exit");
            // Re-key 2026-08-25: the gate itself moved (MetaUpgrades.UpgradesUnlocked is now
            // LifetimeSalvageEarned > 0, not HatchConversions >= 1). The old assertion said the
            // shop stays shut for a player who died before reaching a Convert window; that
            // player is exactly who the ruling was written for, and the answer is now the
            // opposite — they die with 550 AND somewhere to spend it. Second re-key of this
            // line; the 2026-08-18 one moved it off PodEjections.
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "the first banked kill is the unlock — dying broke, and dying with the wheel "
                + "still locked, are both now impossible");

            // --- persistence: exactly the JSON SaveService round-trips ---
            MetaState loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(atDeath, loaded.BankedSalvage, "the run's take survives the save");
            Assert.AreEqual(1, loaded.PodEjections);
            Assert.AreEqual(1, loaded.TotalRuns);

            // --- Respawn: fresh RunSim over the same meta, back on the menu ---
            var sim2 = new RunSim(loaded, 43);
            sim2.StartRun();
            Assert.AreEqual(RunPhase.Docked, sim2.Phase);
            Assert.AreEqual(AssaultState.Prep, sim2.Assault);
            Assert.AreEqual(atDeath, loaded.BankedSalvage, "run 1's kills are still here to spend");
            Assert.AreEqual(0L, sim2.Salvage.RunSalvage, "the per-run tally resets");
            Assert.AreEqual(2, loaded.TotalRuns);
            Assert.AreEqual(ShipSystems.HatchMaxHp, sim2.Ship.Hatch.Hp, 1e-5f, "fresh run, fresh hatch");
            Assert.AreEqual(0, sim2.CurrentWaveIndex, "three fresh waves");
            Assert.IsFalse(sim2.AllWavesCleared);

            // And run 2 spends it. The grubstake used to have to be conjured here with an
            // EarnSalvage call because run 1 legitimately ended broke; now it is the money the
            // player actually walked out of wave 2 with, which is the loop this fixture names.
            loaded.SetOwned(WeaponId.Mk1Sidearm, true);
            loaded.SetEquipped(WeaponId.Mk1Sidearm);
            Assert.IsTrue(loaded.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(atDeath, loaded.BankedSalvage, "MK-1 costs 0");
            Assert.GreaterOrEqual(atDeath, WeaponTier.Cost(0),
                "a death on wave 2 has to leave enough for the first tier, or the loop this "
                + "fixture is named for does not close");
            Assert.IsTrue(loaded.SpendSalvage(WeaponTier.Cost(0)));
            loaded.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 1);
            Assert.AreEqual(atDeath - WeaponTier.Cost(0), loaded.BankedSalvage);
        }

        [Test]
        public void CoreLoop_DialUp_MakesKillsPayMore_AndIsLockedOnceTheDrawIsPulled()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, 7);
            sim.StartRun();
            sim.EngageTether();

            sim.SetSiphonLevel(3);            // x1.25 reward, and x1.25 gatekeepers — the same table
            Assert.AreEqual(MetaUpgrades.SalvagePerKill() * TetherSiphon.RewardMultiplierFor(3),
                sim.CurrentKillValue, 1e-5f);
            var seen = new System.Collections.Generic.List<WaveSpec>();
            sim.WaveStarted += w => seen.Add(w);
            sim.TriggerDraw();
            int dialled = new WaveDirector().NextWave(0, 3).enemyCount;
            Assert.AreEqual(dialled, seen[0].enemyCount);
            Assert.Greater(dialled, new WaveDirector().NextWave(0, 0).enemyCount,
                "the dial has to buy bodies, or 'pay more for more' is only half true");
            Assert.AreEqual(dialled, seen[0].defaultCount);    // and every extra body is a Default
            Assert.AreEqual(1.0f, seen[0].enemyHealthMultiplier, 1e-5f); // the dial no longer buffs hp
            Assert.AreEqual(3, seen[0].groupSize);                       // nor enlarges the group
            Assert.AreEqual(7.3f, seen[0].spawnInterval, 1e-5f);         // nor compresses the gap

            sim.SetSiphonLevel(0);            // the draw size is a pre-commitment
            Assert.AreEqual(3, sim.Siphon.Level, "dial locked during assault");
            Kill(sim, dialled);
            // Re-key 2026-08-25: the dial's effect on kill value is the subject and is unchanged;
            // the take is simply read off the bank now, at face value — the Convert window's
            // carry multiplier is a separate reward and no Convert has happened here.
            Assert.AreEqual(Banked(3, dialled), meta.BankedSalvage);
            Assert.Greater(Banked(3, dialled), Banked(0, dialled),
                "the whole point of the dial: the same body count pays strictly more");
            Assert.AreEqual(0L, sim.Unbanked, "the dial pays into the bank, not into a stake");
        }
    }
}
