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
            Assert.AreEqual(18, specs[0].enemyCount, "wave 1 at siphon 0");
            Assert.AreEqual(18, specs[0].defaultCount, "all Default: wave 1 is the game as it was");
            Assert.AreEqual(1.0f, specs[0].enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(3, specs[0].groupSize, "three at a time");
            Assert.AreEqual(4.2f, specs[0].spawnInterval, 1e-5f, "seconds between GROUPS");

            for (int i = 0; i < 60; i++) sim.Tick(0.5f); // 30 assault seconds, zero kills
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "the clock never pays");
            Assert.AreEqual(0L, meta.BankedSalvage, "still zero — no grubstake");

            Kill(sim, 18); // the whole wave down: 18 * 25
            Assert.AreEqual(450L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage, "kills stay unbanked until a Hatch-bank");
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);

            // --- Clear wave 1: back to PREP, the run is still live (waves remain) ---
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(1, assaultsEnded);
            Assert.IsFalse(sim.AllWavesCleared);
            Kill(sim, 4); // no gatekeepers exist in prep — refused
            for (int i = 0; i < 20; i++) sim.Tick(0.5f);
            Assert.AreEqual(450L, sim.Unbanked, "prep income is zero");
            Assert.AreEqual(0L, meta.BankedSalvage, "and refused kills bank nothing");

            // --- Wave 2: the wall. Thirty gatekeepers — 26 Default and the first 4 Hunters —
            // at 42 HP, four at a time every 4.00s ---
            sim.TriggerDraw();
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            Assert.AreEqual(30, specs[1].enemyCount);
            Assert.AreEqual(26, specs[1].defaultCount);
            Assert.AreEqual(4, specs[1].hunterCount, "the wave that teaches the Hunter");
            Assert.AreEqual(0, specs[1].thiefCount, "the Thief waits for wave 3");
            Assert.AreEqual(1.4f, specs[1].enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(42f, WaveDirector.BaseEnemyHp * specs[1].enemyHealthMultiplier, 1e-3f);

            Kill(sim, 4); // four down before being overwhelmed: +100
            Assert.AreEqual(550L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage, "still unbanked");

            // --- Die mid-wave-2. Down wipes unbanked; ship bank never received it. ---
            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual("player", sim.LastRunReport.cause);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned, "never converted");
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual(0, meta.CleanExits, "a death is not a clean exit");
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta),
                "nothing converted this run — the shop keys on the first bank, not the death "
                + "(re-key 2026-08-18)");

            // --- persistence: exactly the JSON SaveService round-trips ---
            MetaState loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(0L, loaded.BankedSalvage);
            Assert.AreEqual(1, loaded.PodEjections);
            Assert.AreEqual(1, loaded.TotalRuns);

            // --- Respawn: fresh RunSim over the same meta, back on the menu ---
            var sim2 = new RunSim(loaded, 43);
            sim2.StartRun();
            Assert.AreEqual(RunPhase.Docked, sim2.Phase);
            Assert.AreEqual(AssaultState.Prep, sim2.Assault);
            Assert.AreEqual(0L, loaded.BankedSalvage, "nothing converted last run");
            Assert.AreEqual(0L, sim2.Salvage.RunSalvage, "the per-run tally resets");
            Assert.AreEqual(2, loaded.TotalRuns);
            Assert.AreEqual(ShipSystems.HatchMaxHp, sim2.Ship.Hatch.Hp, 1e-5f, "fresh run, fresh hatch");
            Assert.AreEqual(0, sim2.CurrentWaveIndex, "three fresh waves");
            Assert.IsFalse(sim2.AllWavesCleared);

            // Spend path still works when the bank has money.
            loaded.EarnSalvage(550L);
            loaded.SetOwned(WeaponId.Mk1Sidearm, true);
            loaded.SetEquipped(WeaponId.Mk1Sidearm);
            Assert.IsTrue(loaded.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(550L, loaded.BankedSalvage, "MK-1 costs 0");
            Assert.IsTrue(loaded.SpendSalvage(WeaponTier.Cost(0)));
            loaded.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 1);
            Assert.AreEqual(550L - 400L, loaded.BankedSalvage);
        }

        [Test]
        public void CoreLoop_DialUp_MakesKillsPayMore_AndIsLockedOnceTheDrawIsPulled()
        {
            var meta = new MetaState();
            var sim = new RunSim(meta, 7);
            sim.StartRun();
            sim.EngageTether();

            sim.SetSiphonLevel(3);            // x1.25 reward, and x1.25 gatekeepers — the same table
            Assert.AreEqual(31.25f, sim.CurrentKillValue, 1e-5f); // 25 * 1.25
            var seen = new System.Collections.Generic.List<WaveSpec>();
            sim.WaveStarted += w => seen.Add(w);
            sim.TriggerDraw();
            Assert.AreEqual(23, seen[0].enemyCount);           // round(18 * 1.25) = 23
            Assert.AreEqual(23, seen[0].defaultCount);         // and every extra body is a Default
            Assert.AreEqual(1.0f, seen[0].enemyHealthMultiplier, 1e-5f); // the dial no longer buffs hp
            Assert.AreEqual(3, seen[0].groupSize);                       // nor enlarges the group
            Assert.AreEqual(4.2f, seen[0].spawnInterval, 1e-5f);         // nor compresses the gap

            sim.SetSiphonLevel(0);            // the draw size is a pre-commitment
            Assert.AreEqual(3, sim.Siphon.Level, "dial locked during assault");
            Kill(sim, 23);
            Assert.AreEqual(713L, sim.Unbanked); // 23 * floor(31.25) = 23 * 31
            Assert.AreEqual(0L, meta.BankedSalvage,
                "kill awards sit unbanked until a Hatch-bank");
        }
    }
}
