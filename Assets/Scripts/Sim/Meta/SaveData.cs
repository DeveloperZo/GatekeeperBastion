using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// Flat serializable DTO for meta-progression persistence. Public fields only, no dictionaries —
    /// compatible with Unity's JsonUtility. File IO lives in the game layer, not here.
    /// </summary>
    /// <remarks>
    /// Version history:
    ///   1 — banked salvage + run tallies
    ///   2 — lifetime salvage ledger (Earned/Spent)
    ///   3 — per-weapon ownership, Tiers, equipped slots (replaces GunDamage/FireRate/Clip/WeaponOwned)
    ///   4 — equipment slot + wheel points (replaces RepairRig/Engineering tracks and flat WeaponTiers)
    ///   5 — third weapon slot renamed Melee -> Drone. The JSON key changed with it
    ///       ("EquippedMelee" -> "EquippedDrone"). NOT backward compatible, deliberately
    ///       (Zo, 2026-08-08): the slot has never held anything, so a v4 save loses a
    ///       WeaponId.None and nothing else. Do not add a compatibility shim for it.
    ///   6 — EquippedDrone holds a PayloadId (was WeaponId.None sentinel); OwnedPayloadMask
    ///       added. Pre-v6 EquippedDrone values of -1 / unknown sanitize to PayloadId.None.
    /// Migration from earlier versions lives in <see cref="MetaState.FromJson"/> /
    /// <see cref="MetaState.FromSaveData"/>.
    /// </remarks>
    [Serializable]
    public class SaveData
    {
        /// <summary>Schema version written by the current build.</summary>
        public const int CurrentVersion = 6;

        public int SaveVersion = CurrentVersion;
        public long BankedSalvage;

        public long LifetimeSalvageEarned;
        public long LifetimeSalvageSpent;

        public int TotalRuns;
        public int CleanExits;
        public int PodEjections;

        /// <summary>Highest 1-based wave cleared this campaign. Absent/0 in old saves → 1.</summary>
        public int DeepestWaveReached = 1;

        public int HatchArmorLevel;

        public int VitalityLevel;
        public int StaminaLevel;
        public int ScavengingLevel;
        public int LuckLevel;

        // ---- Equipment (v4) ----
        public bool EquipmentSlotPurchased;
        public int OwnedEquipmentMask;
        public int EquippedEquipment; // EquipmentId as int

        // ---- Payloads (v6) — bay contents; EquippedDrone is the bay's equipped PayloadId ----
        public int OwnedPayloadMask;

        // ---- Weapons (indexed by WeaponId) ----
        public bool[] WeaponsOwned;
        public int EquippedPrimary;
        public int EquippedSecondary;
        /// <summary>PayloadId in the drone bay (v6+). Pre-v6 WeaponId sentinels sanitize to None.</summary>
        public int EquippedDrone;

        /// <summary>v4: weapon-major flat ArraySize×5 — index = weaponId * AttrCount + WheelAttr.</summary>
        public int[] WeaponWheelPoints;

        // ---- Legacy fields: parse-only so old saves migrate ----
        public int[] WeaponTiers;
        public int RepairRigLevel;
        public int EngineeringLevel;
        public int GunDamageLevel;
        public int FireRateLevel;
        public int ClipSizeLevel;
        public bool WeaponOwned;
    }
}
