using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Hatch-bank convert rules. RunSim owns the live pile and calls these.
    /// Numbers are #12 constants — retune here and the 4-Data GEN block together.
    /// Convert and Hatch composition both read Waves carried since the last Hatch-bank.
    /// Pile-size convert is gone: more Waves is more Unbanked, and two levers cannot be
    /// attributed.</summary>
    public static class HatchBank
    {
        public const int SlotsPerRun = 3;
        public const float DripPerSecond = 20f;
        /// <summary>Hatch-bank convert window. #12 — Wave 6 delivery is ~35 s; 45 s covers
        /// spawn plus a short fight so Unbanked visibly falls (20/s × 45 = 900) before dump.
        /// Enemy wipe and Unbanked hitting 0 do not end it.</summary>
        public const float DurationSeconds = 45f;
        public const float MultLow = 1.00f;
        public const float Mult2 = 1.10f;
        public const float MultMid = 1.25f;
        public const float Mult4 = 1.35f;
        public const float MultHigh = 1.50f;

        /// <summary>Salvage/s withdrawn at a full Hatch. Catalog repair HP/s times salvage/HP
        /// so the two holds stay on one rate.</summary>
        public static float WithdrawPerSecond =>
            EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond
            * ShipSystems.RepairSalvagePerHp;

        /// <summary>Field Waves cleared since the last Hatch-bank, clamped to 1..MaxWaves.</summary>
        public static int ClampCarry(int waves)
        {
            if (waves < 1) return 1;
            if (waves > WaveDirector.MaxWaves) return WaveDirector.MaxWaves;
            return waves;
        }

        /// <summary>Convert multiplier for a Hatch-bank started after this many field Waves.
        /// 1 → ×1.00, 5+ → ×1.50.</summary>
        public static float CarryConvert(int waves)
        {
            switch (ClampCarry(waves))
            {
                case 1: return MultLow;
                case 2: return Mult2;
                case 3: return MultMid;
                case 4: return Mult4;
                default: return MultHigh;
            }
        }

        /// <summary>0-based <see cref="WaveDirector.NextWave"/> index for that carry.
        /// 1 Wave → Wave 1, 5 Waves → Wave 5.</summary>
        public static int CarryWaveIndex(int waves)
        {
            return ClampCarry(waves) - 1;
        }

        public static long BankedFrom(long unbankedTaken, float multiplier)
        {
            if (unbankedTaken <= 0L) return 0L;
            if (multiplier <= 0f) return 0L;
            return (long)Math.Floor(unbankedTaken * (double)multiplier);
        }

        /// <summary>Max salvage one guest may bank from a conversion, given their 1-based
        /// <c>DeepestWaveReached</c>. Solo expected unbanked for waves 1..deepest at dial 0
        /// and scavenging 0 (25 / kill). Excess is lost, not redistributed. Not applied when
        /// only one player is in the Run — that path must stay byte-identical.</summary>
        public static long BankCap(int deepestWave)
        {
            int d = deepestWave < 1 ? 1 : deepestWave;
            if (d > WaveDirector.MaxWaves) d = WaveDirector.MaxWaves;
            var director = new WaveDirector();
            long perKill = (long)Math.Floor((double)MetaUpgrades.SalvagePerKill(0));
            long cap = 0L;
            for (int i = 0; i < d; i++)
                cap += (long)director.NextWave(i, 0, 1).enemyCount * perKill;
            return cap;
        }

        public static long DripUnits(float dt, ref float accrued)
        {
            if (dt <= 0f) return 0L;
            accrued += DripPerSecond * dt;
            long units = (long)Math.Floor((double)accrued);
            if (units <= 0L) return 0L;
            accrued -= units;
            return units;
        }

        public static long AttritUnits(float hatchDamage)
        {
            if (hatchDamage <= 0f) return 0L;
            return (long)Math.Floor((double)hatchDamage);
        }

        public static long WithdrawUnits(float dt, ref float accrued)
        {
            if (dt <= 0f) return 0L;
            accrued += WithdrawPerSecond * dt;
            long units = (long)Math.Floor((double)accrued);
            if (units <= 0L) return 0L;
            accrued -= units;
            return units;
        }
    }
}
