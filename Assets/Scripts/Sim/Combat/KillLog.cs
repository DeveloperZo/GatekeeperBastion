namespace GatekeeperBastion.Sim
{
    /// <summary>Pure kill-line formatter. WaveSpawner owns when to emit; this owns the words
    /// so share-of-kills per slot is grepable and Path 1 can pin the shape.</summary>
    public static class KillLog
    {
        /// <summary>Combat kill line including the slot that landed the killing blow.</summary>
        public static string Format(string kindName, WeaponSlot slot, float award, int aliveLeft)
        {
            return "killed a " + kindName + " with " + SlotWord(slot)
                + " (+" + award.ToString("0") + " salvage) — " + aliveLeft + " left";
        }

        /// <summary>Combat kill line when no damaging slot was recorded (should be rare).</summary>
        public static string FormatUnknownSlot(string kindName, float award, int aliveLeft)
        {
            return "killed a " + kindName
                + " (+" + award.ToString("0") + " salvage) — " + aliveLeft + " left";
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
