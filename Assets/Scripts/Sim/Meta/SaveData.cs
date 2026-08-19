using System;

namespace GatekeepersProtocol.Sim
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
    ///   7 — CompletedTerminalIds: last successful Hatch-bank unlock graph (deep start).
    ///   8 — Boarder Protocol + Passive; Vitality/Stamina/Scavenging/Luck levels become
    ///       parse-only and refund 200×(2^level−1) into banked.
    ///   9 — first-Bastion demo flags (DemoActive and the beat gates) plus per-zone
    ///       DemoZoneDeepestWave. Pre-v9 saves migrate demo-off when they have already
    ///       played (CleanExits >= 1 or DeepestWaveReached > 1), demo-on otherwise.
    ///  10 — OpenDoorIds: doors open on a SURVIVED Hatch-bank window, so door state stopped
    ///       being derivable from CompletedTerminalIds and became its own saved fact. Pre-v10
    ///       saves load with no open doors and no shim: their doors were opened by the retired
    ///       terminal rule, so re-deriving them would re-assert a rule that no longer exists.
    /// Migration from earlier versions lives in <see cref="MetaState.FromJson"/> /
    /// <see cref="MetaState.FromSaveData"/>.
    /// </remarks>
    [Serializable]
    public class SaveData
    {
        /// <summary>Schema version written by the current build.</summary>
        public const int CurrentVersion = 10;

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

        /// <summary>v8: BoarderProtocolId / BoarderAbilityId as ints.</summary>
        public int EquippedProtocol = (int)BoarderProtocolId.None;
        public int EquippedPassive = (int)BoarderAbilityId.None;

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

        /// <summary>v7: Field terminal ids complete at the last successful Hatch-bank.</summary>
        public string[] CompletedTerminalIds;

        /// <summary>v10: door ids standing open. Written on a survived bank window; restored on
        /// every embark so a Docked return finds the door as it was left.</summary>
        public string[] OpenDoorIds;

        // ---- First-Bastion demo (v9) — see Docs/Demo/README.md, Shared flags ----
        public bool DemoActive;
        public bool DemoCapsuleOpened;
        public bool DemoNavPowered;
        public bool DemoArmoryPowered;
        public bool DemoWeaponGranted;
        public bool DemoShipFightDone;
        public bool DemoCrewMenuUnlocked;
        public bool DemoFabPowered;
        public int DemoWheelBuysLeft;
        /// <summary>Deepest cleared field Wave per zone, index = zone number. Written on
        /// field-Wave clear; read by the composite retrigger and guest BankCap.</summary>
        public int[] DemoZoneDeepestWave;

        /// <summary>v4: weapon-major flat ArraySize×5 — index = weaponId * AttrCount + WheelAttr.</summary>
        public int[] WeaponWheelPoints;

        // ---- Legacy fields: parse-only so old saves migrate ----
        public int[] WeaponTiers;
        public int VitalityLevel;
        public int StaminaLevel;
        public int ScavengingLevel;
        public int LuckLevel;
        public int RepairRigLevel;
        public int EngineeringLevel;
        public int GunDamageLevel;
        public int FireRateLevel;
        public int ClipSizeLevel;
        public bool WeaponOwned;
    }
}
