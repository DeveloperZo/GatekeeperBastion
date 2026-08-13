using System.Globalization;

namespace GatekeeperBastion.Sim
{
    /// <summary>Pure kill-line formatter. WaveSpawner owns when to emit; this owns the words
    /// so share-of-kills per slot is grepable and Path 1 can pin the shape.</summary>
    public static class KillLog
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Combat kill line including the slot that landed the killing blow, plus
        /// planar metres to the Hatch surface and to where the body spawned.</summary>
        public static string Format(
            string kindName, WeaponSlot slot, long award, int aliveLeft,
            float hatchDist, float spawnDist)
        {
            return "killed a " + kindName + " with " + SlotWord(slot)
                + " (+" + award.ToString(Inv) + " unbanked) -- " + aliveLeft.ToString(Inv)
                + " left" + DistTail(hatchDist, spawnDist);
        }

        /// <summary>Combat kill line when no damaging slot was recorded (should be rare).</summary>
        public static string FormatUnknownSlot(
            string kindName, long award, int aliveLeft, float hatchDist, float spawnDist)
        {
            return "killed a " + kindName
                + " (+" + award.ToString(Inv) + " unbanked) -- " + aliveLeft.ToString(Inv)
                + " left" + DistTail(hatchDist, spawnDist);
        }

        /// <summary>Planar (y-ignored) metres between two points. Used for hatchDist and
        /// spawnDist — no clamp, no normalisation.</summary>
        public static float PlanarMetres(
            float ax, float ay, float az, float bx, float by, float bz)
        {
            float dx = bx - ax;
            float dz = bz - az;
            return (float)System.Math.Sqrt(dx * dx + dz * dz);
        }

        static string DistTail(float hatchDist, float spawnDist)
        {
            return " hatchDist=" + hatchDist.ToString("0.0", Inv)
                + " spawnDist=" + spawnDist.ToString("0.0", Inv);
        }

        public static string SlotWord(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary: return "Primary";
                case WeaponSlot.Secondary: return "Secondary";
                case WeaponSlot.Drone: return "Drone";
                default: return slot.ToString();
            }
        }
    }
}
