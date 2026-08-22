namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// What a realm has to show for itself: runs survived, runs lost, how deep it has been
    /// taken, and what its hatch has been upgraded to.
    /// </summary>
    /// <remarks>
    /// This exists as its own small value because a guest cannot read the host's
    /// <see cref="RealmState"/> — it is not on their machine. The record is the part of a
    /// realm that has to travel, so it is shaped to be packed into a single replicated number
    /// and read back identically on both sides.
    ///
    /// Packing follows <c>ShipHatches</c>: fixed-width fields in one integer, shifts named
    /// rather than sprinkled, and the whole thing testable in EditMode without a
    /// NetworkManager. Values are CLAMPED on the way in rather than truncated — a realm played
    /// for long enough to overflow a field should show a capped number, not a wrapped one.
    /// </remarks>
    public struct RealmRecord
    {
        /// <summary>Bits per field. Four fields in a long, with room to spare.</summary>
        public const int FieldBits = 16;

        /// <summary>Largest value any field can carry. A realm past this shows the cap.</summary>
        public const int FieldMax = (1 << FieldBits) - 1;

        const int CleanExitsShift = 0;
        const int RunsLostShift = FieldBits;
        const int DeepestWaveShift = FieldBits * 2;
        const int HatchArmorShift = FieldBits * 3;
        const long FieldMask = FieldMax;

        /// <summary>Runs that left this realm's bastion standing. One mark on the wall each.</summary>
        public int CleanExits;

        /// <summary>Runs this realm lost.</summary>
        public int RunsLost;

        /// <summary>Deepest wave cleared here. Never below 1 — a realm nobody has played is
        /// still standing at its first wave, not at a zeroth one.</summary>
        public int DeepestWave;

        /// <summary>How far the hatch has been upgraded. One track today.</summary>
        public int HatchArmor;

        /// <summary>True when nothing has happened here yet. The display reads this rather
        /// than testing counters one by one, so "empty" is one decision in one place.</summary>
        public bool IsUntouched
        {
            get { return CleanExits == 0 && RunsLost == 0 && DeepestWave <= 1 && HatchArmor == 0; }
        }

        /// <summary>The record a realm is currently showing.</summary>
        public static RealmRecord From(RealmState realm)
        {
            var r = new RealmRecord();
            if (realm == null) return Fresh();
            r.CleanExits = Clamp(realm.CleanExits);
            r.RunsLost = Clamp(realm.PodEjections);
            r.DeepestWave = Clamp(realm.DeepestWaveReached < 1 ? 1 : realm.DeepestWaveReached);
            r.HatchArmor = Clamp(realm.HatchArmorLevel);
            return r;
        }

        /// <summary>A realm nobody has played.</summary>
        public static RealmRecord Fresh()
        {
            var r = new RealmRecord();
            r.DeepestWave = 1;
            return r;
        }

        static int Clamp(int value)
        {
            if (value < 0) return 0;
            return value > FieldMax ? FieldMax : value;
        }

        /// <summary>Squeeze into one number for the wire.</summary>
        public long Pack()
        {
            long packed = (long)Clamp(CleanExits) << CleanExitsShift;
            packed |= (long)Clamp(RunsLost) << RunsLostShift;
            packed |= (long)Clamp(DeepestWave) << DeepestWaveShift;
            packed |= (long)Clamp(HatchArmor) << HatchArmorShift;
            return packed;
        }

        /// <summary>Read one back. A zero packs to a fresh realm rather than to a realm at
        /// wave zero, which is a state that does not exist.</summary>
        public static RealmRecord Unpack(long packed)
        {
            if (packed == 0L) return Fresh();
            var r = new RealmRecord();
            r.CleanExits = (int)((packed >> CleanExitsShift) & FieldMask);
            r.RunsLost = (int)((packed >> RunsLostShift) & FieldMask);
            r.DeepestWave = (int)((packed >> DeepestWaveShift) & FieldMask);
            r.HatchArmor = (int)((packed >> HatchArmorShift) & FieldMask);
            if (r.DeepestWave < 1) r.DeepestWave = 1;
            return r;
        }

        public bool Equals(RealmRecord other)
        {
            return CleanExits == other.CleanExits
                && RunsLost == other.RunsLost
                && DeepestWave == other.DeepestWave
                && HatchArmor == other.HatchArmor;
        }
    }
}
