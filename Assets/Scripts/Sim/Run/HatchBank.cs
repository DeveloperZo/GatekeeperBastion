using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Hatch-bank convert rules. RunSim owns the live pile and calls these.
    /// Numbers are #12 constants — retune here and the 4-Data GEN block together.
    /// Convert and Hatch composition both read Waves carried since the last Hatch-bank.
    /// Pile-size convert is gone: more Waves is more Unbanked, and two levers cannot be
    /// attributed.</summary>
    public static class HatchBank
    {
        public const float DripPerSecond = 8f;
        /// <summary>Hatch-bank convert window. #12 — Wave 6 delivery is ~35 s; 45 s covers
        /// spawn plus a short fight so Unbanked visibly falls (8/s × 45 = 360) before dump.
        /// Enemy wipe and Unbanked hitting 0 do not end it.</summary>
        public const float DurationSeconds = 45f;
        /// <summary>Extra Thieves on a 3-Wave Hatch-bank. Field Wave 3 stays at table
        /// thiefCount; the first Hatch-bank after three field Waves is the designed overrun
        /// for an unupgraded MK-1. [unconfirmed: 2026-08-16]</summary>
        public const int Carry3ExtraThief = 1;
        /// <summary>Share of each slot's leftover Unbanked a Returned run keeps; Lost still
        /// wipes it all. Deliberately slight: greed must stay carrying a bigger pile into
        /// fewer extracts, and a large survival rebate turns the dock into the optimal line
        /// (Docs/Decisions.md, the wheel opens on the first bank). Door-state persistence
        /// already rides the successful-bank snapshot and is not part of this number.
        /// Tunable after a logged Run.</summary>
        public const float ReturnedKeepShare = 0.10f;

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

        /// <summary>Thieves added on top of <see cref="WaveDirector.NextWave"/> for a
        /// Hatch-bank of this carry. Extra bodies, not converted Defaults.</summary>
        public static int ExtraThief(int carry)
        {
            return ClampCarry(carry) == 3 ? Carry3ExtraThief : 0;
        }

        /// <summary>Hatch-bank spec: same curve as the matching field Wave, plus
        /// <see cref="ExtraThief"/> on carry 3. Counts still sum to enemyCount.</summary>
        public static WaveSpec ApplyHatchComposition(WaveSpec spec, int carry)
        {
            int extra = ExtraThief(carry);
            if (extra <= 0)
                return spec;
            spec.thiefCount += extra;
            spec.enemyCount += extra;
            return spec;
        }

        /// <summary>Salvage a Returned run keeps from one slot's leftover Unbanked.</summary>
        public static long ReturnedKeep(long unbanked)
        {
            if (unbanked <= 0L) return 0L;
            return (long)Math.Floor(unbanked * (double)ReturnedKeepShare);
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
            long perKill = (long)Math.Floor((double)MetaUpgrades.SalvagePerKill());
            long cap = 0L;
            for (int i = 0; i < d; i++)
                cap += (long)director.NextWave(i, 0, 1).enemyCount * perKill;
            return cap;
        }

        /// <summary>Unbanked from Master Control's terminal half at dial 0, solo:
        /// <see cref="MasterControl.TerminalWaves"/> draws of the Wave-5 table. That half
        /// does not walk waves 1..5 — each window is the Wave-5 count — so this is not
        /// <see cref="BankCap"/>.</summary>
        public static long MasterTerminalUnbanked()
        {
            var director = new WaveDirector();
            long perKill = (long)Math.Floor((double)MetaUpgrades.SalvagePerKill());
            long one = (long)director.NextWave(WaveDirector.MaxWaves - 1, 0, 1).enemyCount * perKill;
            return one * MasterControl.TerminalWaves;
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
