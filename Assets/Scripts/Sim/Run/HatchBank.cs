using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Hatch-bank convert rules. RunSim owns the live pile and calls these.
    /// Numbers are #12 constants — retune here and the 4-Data GEN block together.</summary>
    public static class HatchBank
    {
        public const int SlotsPerRun = 3;
        public const float DripPerSecond = 20f;
        /// <summary>Hatch-bank convert window. #12 — Wave 6 delivery is ~35 s; 45 s covers
        /// spawn plus a short fight so Unbanked visibly falls (20/s × 45 = 900) before dump.
        /// Enemy wipe and Unbanked hitting 0 do not end it.</summary>
        public const float DurationSeconds = 45f;
        public const long MidAt = 400L;
        public const long HighAt = 800L;
        public const float MultLow = 1.00f;
        public const float MultMid = 1.25f;
        public const float MultHigh = 1.50f;

        /// <summary>Salvage/s withdrawn at a full Hatch. Catalog repair HP/s times salvage/HP
        /// so the two holds stay on one rate.</summary>
        public static float WithdrawPerSecond =>
            EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond
            * ShipSystems.RepairSalvagePerHp;

        public static float ConvertMultiplier(long unbanked)
        {
            if (unbanked >= HighAt) return MultHigh;
            if (unbanked >= MidAt) return MultMid;
            return MultLow;
        }

        public static long BankedFrom(long unbankedTaken, float multiplier)
        {
            if (unbankedTaken <= 0L) return 0L;
            if (multiplier <= 0f) return 0L;
            return (long)Math.Floor(unbankedTaken * (double)multiplier);
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
