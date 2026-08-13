using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GatekeeperBastion.Util;

namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// Roguelite meta-progression that persists across runs: banked salvage, attributes,
    /// equipment, and per-weapon ownership / wheel points / equipped slots. Serializes to/from
    /// JSON by hand (field-for-field identical to JsonUtility on SaveData; no Newtonsoft).
    /// The game layer owns file paths and IO.
    /// </summary>
    public sealed class MetaState
    {
        public long BankedSalvage;
        public long LifetimeSalvageEarned;
        public long LifetimeSalvageSpent;

        public int TotalRuns;
        public int CleanExits;
        // Save/API trap: PodEjections means lost Runs; keep this field and its JSON key unchanged.
        public int PodEjections;

        /// <summary>Highest 1-based wave the player has cleared this campaign. Starts at 1,
        /// rises only on clear (never on reach), never lowers. Caps the MAP start-wave selector.</summary>
        public int DeepestWaveReached = 1;

        public int SaveVersion = SaveData.CurrentVersion;

        public int HatchArmorLevel;

        public int VitalityLevel;
        public int StaminaLevel;
        public int ScavengingLevel;
        public int LuckLevel;

        // ---- Equipment ----
        public bool EquipmentSlotPurchased;
        public int OwnedEquipmentMask;
        public EquipmentId EquippedEquipment = EquipmentId.None;

        // ---- Payloads — drone bay ----
        public int OwnedPayloadMask;
        /// <summary>Payload sitting in the drone bay. Field name is the bay; value is the payload.</summary>
        public PayloadId EquippedDrone = PayloadId.None;

        /// <summary>When true, <see cref="SetEquippedPayload"/> is a logged no-op (Run in progress).</summary>
        [NonSerialized] public bool PayloadLoadoutLocked;

        // ---- Weapons ----
        bool[] _weaponsOwned;
        /// <summary>Flat ArraySize×5: index = weaponId * AttrCount + (int)WheelAttr.</summary>
        int[] _wheelPoints;
        public WeaponId EquippedPrimary = WeaponId.None;
        public WeaponId EquippedSecondary = WeaponId.None;

        public bool SalvageLedgerBalances =>
            LifetimeSalvageEarned == BankedSalvage + LifetimeSalvageSpent;

        [NonSerialized] public bool SalvageStatsWereRepaired;

        public bool OwnsAnyWeapon
        {
            get
            {
                EnsureArrays();
                for (int i = 0; i < _weaponsOwned.Length; i++)
                    if (_weaponsOwned[i]) return true;
                return false;
            }
        }

        public bool HasRepairRigEquipped =>
            EquippedEquipment == EquipmentId.RepairRig && OwnsEquipment(EquipmentId.RepairRig);

        public bool Owns(WeaponId id)
        {
            if (id == WeaponId.None) return false;
            EnsureArrays();
            int i = (int)id;
            return i >= 0 && i < _weaponsOwned.Length && _weaponsOwned[i];
        }

        public bool OwnsEquipment(EquipmentId id)
        {
            int bit = EquipmentCatalog.OwnershipBit(id);
            return bit != 0 && (OwnedEquipmentMask & bit) != 0;
        }

        public void SetEquipmentOwned(EquipmentId id, bool owned)
        {
            int bit = EquipmentCatalog.OwnershipBit(id);
            if (bit == 0) return;
            if (owned) OwnedEquipmentMask |= bit;
            else OwnedEquipmentMask &= ~bit;
        }

        public void SetEquippedEquipment(EquipmentId id)
        {
            if (id != EquipmentId.None && !OwnsEquipment(id)) return;
            EquippedEquipment = id;
        }

        public bool OwnsPayload(PayloadId id)
        {
            int bit = PayloadCatalog.OwnershipBit(id);
            return bit != 0 && (OwnedPayloadMask & bit) != 0;
        }

        public void SetPayloadOwned(PayloadId id, bool owned)
        {
            int bit = PayloadCatalog.OwnershipBit(id);
            if (bit == 0) return;
            if (owned) OwnedPayloadMask |= bit;
            else OwnedPayloadMask &= ~bit;
        }

        /// <summary>Equip a payload into the drone bay. Logged no-op while a Run locks the loadout.</summary>
        public bool SetEquippedPayload(PayloadId id)
        {
            if (PayloadLoadoutLocked)
            {
                Log.Event("Ship", "SetEquippedPayload refused — run in progress");
                return false;
            }
            if (id != PayloadId.None && !OwnsPayload(id)) return false;
            EquippedDrone = id;
            return true;
        }

        /// <summary>Null when repair is available; otherwise the next missing step for prompts.</summary>
        public string RepairMissingStep()
        {
            if (PodEjections < 1)
                return "equipment slot locked until first lost Run";
            if (!EquipmentSlotPurchased)
                return "equipment slot not purchased — buy it at FABRICATION";
            if (!OwnsEquipment(EquipmentId.RepairRig))
                return "repair rig not owned — buy it at FABRICATION";
            if (EquippedEquipment != EquipmentId.RepairRig)
                return "repair rig not equipped — equip it at FABRICATION";
            return null;
        }

        /// <summary>Derived Tier: total wheel points / 11, clamped 0..MaxTier. Never stored.</summary>
        public int TierOf(WeaponId id)
        {
            int t = WeaponWheel.DerivedTier(PointsOf(id));
            if (t < 0) return 0;
            if (t > WeaponTier.MaxTier) return WeaponTier.MaxTier;
            return t;
        }

        public int[] PointsOf(WeaponId id)
        {
            var pts = new int[WeaponWheel.AttrCount];
            CopyPointsInto(id, pts);
            return pts;
        }

        public int PointOf(WeaponId id, WheelAttr attr)
        {
            if (id == WeaponId.None) return 0;
            EnsureArrays();
            int i = (int)id;
            if (i < 0 || i >= WeaponCatalog.ArraySize) return 0;
            return _wheelPoints[i * WeaponWheel.AttrCount + (int)attr];
        }

        public void CopyPointsInto(WeaponId id, int[] dest)
        {
            if (dest == null || dest.Length != WeaponWheel.AttrCount)
                throw new ArgumentException("dest must be length " + WeaponWheel.AttrCount, "dest");
            for (int a = 0; a < WeaponWheel.AttrCount; a++) dest[a] = 0;
            if (id == WeaponId.None) return;
            EnsureArrays();
            int i = (int)id;
            if (i < 0 || i >= WeaponCatalog.ArraySize) return;
            int baseIdx = i * WeaponWheel.AttrCount;
            for (int a = 0; a < WeaponWheel.AttrCount; a++)
                dest[a] = _wheelPoints[baseIdx + a];
        }

        public void AwardWheel(WeaponId id, WheelAttr centre)
        {
            if (id == WeaponId.None) return;
            var ring = WeaponWheel.RingOf(id);
            if (ring == null)
                throw new InvalidOperationException(id + " has no upgrade ring");
            EnsureArrays();
            var scratch = PointsOf(id);
            WeaponWheel.Award(scratch, ring, centre);
            WritePoints(id, scratch);
        }

        /// <summary>Dev / migration: replace counters with the flat-tier migration shape.
        /// Tier remains derived — there is no Tier setter.</summary>
        public void ReplacePointsFromFlatTier(WeaponId id, int tier)
        {
            if (id == WeaponId.None) return;
            EnsureArrays();
            var pts = new int[WeaponWheel.AttrCount];
            WeaponWheel.DistributeFlatTier(pts, tier);
            WritePoints(id, pts);
        }

        void WritePoints(WeaponId id, int[] pts)
        {
            EnsureArrays();
            int i = (int)id;
            int baseIdx = i * WeaponWheel.AttrCount;
            for (int a = 0; a < WeaponWheel.AttrCount; a++)
                _wheelPoints[baseIdx + a] = pts[a];
        }

        public WeaponId Equipped(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary: return EquippedPrimary;
                case WeaponSlot.Secondary: return EquippedSecondary;
                case WeaponSlot.Drone: return WeaponId.None; // bay holds a payload, not a weapon
                default: return WeaponId.None;
            }
        }

        public void SetOwned(WeaponId id, bool owned)
        {
            if (id == WeaponId.None) return;
            EnsureArrays();
            int i = (int)id;
            if (i < 0 || i >= _weaponsOwned.Length) return;
            _weaponsOwned[i] = owned;
        }

        public void SetEquipped(WeaponId id)
        {
            if (id == WeaponId.None) return;
            var def = WeaponCatalog.Def(id);
            switch (def.slot)
            {
                case WeaponSlot.Primary: EquippedPrimary = id; break;
                case WeaponSlot.Secondary: EquippedSecondary = id; break;
                // Drone bay is payload-only — refuse weapon equip into it.
            }
        }

        public void EarnSalvage(long amount)
        {
            if (amount <= 0) return;
            BankedSalvage += amount;
            LifetimeSalvageEarned += amount;
        }

        public bool SpendSalvage(long amount)
        {
            if (amount <= 0) return false;
            if (BankedSalvage < amount) return false;
            BankedSalvage -= amount;
            LifetimeSalvageSpent += amount;
            return true;
        }

        /// <summary>Spend floor from owned tracks + weapons + derived Tiers. Granted equipment
        /// (migration) is not counted — see EquipmentTests.GrantsNotCountedInSpendFloor.</summary>
        public long ReconstructSpentFromLevels()
        {
            long spent = 0;
            spent += SpentOnTrack(HatchArmorLevel);
            spent += SpentOnTrack(VitalityLevel);
            spent += SpentOnTrack(StaminaLevel);
            spent += SpentOnTrack(ScavengingLevel);
            spent += SpentOnTrack(LuckLevel);

            EnsureArrays();
            for (int i = 0; i < _weaponsOwned.Length; i++)
            {
                if (!_weaponsOwned[i]) continue;
                var id = (WeaponId)i;
                spent += WeaponCatalog.Def(id).cost;
                spent += WeaponTier.SpentToReach(TierOf(id));
            }

            if (OwnsPayload(PayloadId.Decoy))
                spent += PayloadCatalog.Def(PayloadId.Decoy).cost;

            return spent;
        }

        static long SpentOnTrack(int level)
        {
            if (level <= 0) return 0;
            if (level > MetaUpgrades.MaxLevel) level = MetaUpgrades.MaxLevel;
            return MetaUpgrades.BaseCost * ((1L << level) - 1L);
        }

        void RestoreSalvageLedger(long earnedRaw, long spentRaw)
        {
            long floor = ReconstructSpentFromLevels();
            LifetimeSalvageSpent = spentRaw > floor ? spentRaw : floor;
            LifetimeSalvageEarned = BankedSalvage + LifetimeSalvageSpent;
            SalvageStatsWereRepaired =
                spentRaw != LifetimeSalvageSpent || earnedRaw != LifetimeSalvageEarned;
        }

        public static MetaState CreateNew()
        {
            var m = new MetaState();
            m.EnsureArrays();
            return m;
        }

        public void EnsureArrays()
        {
            if (_weaponsOwned == null || _weaponsOwned.Length != WeaponCatalog.ArraySize)
            {
                var next = new bool[WeaponCatalog.ArraySize];
                if (_weaponsOwned != null)
                    Array.Copy(_weaponsOwned, next, Math.Min(_weaponsOwned.Length, next.Length));
                _weaponsOwned = next;
            }
            int need = WeaponCatalog.ArraySize * WeaponWheel.AttrCount;
            if (_wheelPoints == null || _wheelPoints.Length != need)
            {
                var next = new int[need];
                if (_wheelPoints != null)
                    Array.Copy(_wheelPoints, next, Math.Min(_wheelPoints.Length, next.Length));
                _wheelPoints = next;
            }
        }

        public SaveData ToSaveData()
        {
            EnsureArrays();
            var d = new SaveData();
            d.SaveVersion = SaveVersion;
            d.BankedSalvage = BankedSalvage;
            d.LifetimeSalvageEarned = LifetimeSalvageEarned;
            d.LifetimeSalvageSpent = LifetimeSalvageSpent;
            d.TotalRuns = TotalRuns;
            d.CleanExits = CleanExits;
            d.PodEjections = PodEjections;
            d.DeepestWaveReached = DeepestWaveReached < 1 ? 1 : DeepestWaveReached;
            d.HatchArmorLevel = HatchArmorLevel;
            d.VitalityLevel = VitalityLevel;
            d.StaminaLevel = StaminaLevel;
            d.ScavengingLevel = ScavengingLevel;
            d.LuckLevel = LuckLevel;
            d.EquipmentSlotPurchased = EquipmentSlotPurchased;
            d.OwnedEquipmentMask = OwnedEquipmentMask;
            d.EquippedEquipment = (int)EquippedEquipment;
            d.WeaponsOwned = (bool[])_weaponsOwned.Clone();
            d.WeaponWheelPoints = (int[])_wheelPoints.Clone();
            d.WeaponTiers = null;
            d.RepairRigLevel = 0;
            d.EngineeringLevel = 0;
            d.EquippedPrimary = (int)EquippedPrimary;
            d.EquippedSecondary = (int)EquippedSecondary;
            d.EquippedDrone = (int)EquippedDrone;
            d.OwnedPayloadMask = OwnedPayloadMask;
            return d;
        }

        public static MetaState FromSaveData(SaveData d)
        {
            if (d == null) throw new ArgumentNullException("d");
            var m = new MetaState();
            m.SaveVersion = d.SaveVersion;
            m.BankedSalvage = d.BankedSalvage;
            m.TotalRuns = d.TotalRuns;
            m.CleanExits = d.CleanExits;
            m.PodEjections = d.PodEjections;
            m.DeepestWaveReached = d.DeepestWaveReached < 1 ? 1 : d.DeepestWaveReached;
            m.HatchArmorLevel = d.HatchArmorLevel;
            m.VitalityLevel = d.VitalityLevel;
            m.StaminaLevel = d.StaminaLevel;
            m.ScavengingLevel = d.ScavengingLevel;
            m.LuckLevel = d.LuckLevel;
            m.EnsureArrays();

            if (d.SaveVersion >= 4)
            {
                m.EquipmentSlotPurchased = d.EquipmentSlotPurchased;
                m.OwnedEquipmentMask = d.OwnedEquipmentMask;
                m.EquippedEquipment = (EquipmentId)d.EquippedEquipment;
            }

            if (d.SaveVersion >= 3 && d.WeaponsOwned != null)
            {
                Array.Copy(d.WeaponsOwned, m._weaponsOwned,
                    Math.Min(d.WeaponsOwned.Length, m._weaponsOwned.Length));
                m.EquippedPrimary = (WeaponId)d.EquippedPrimary;
                m.EquippedSecondary = (WeaponId)d.EquippedSecondary;
                m.EquippedDrone = SanitizePayloadId(d.EquippedDrone);

                if (d.SaveVersion >= 6)
                    m.OwnedPayloadMask = d.OwnedPayloadMask;

                if (d.SaveVersion >= 4 && d.WeaponWheelPoints != null)
                {
                    Array.Copy(d.WeaponWheelPoints, m._wheelPoints,
                        Math.Min(d.WeaponWheelPoints.Length, m._wheelPoints.Length));
                }
                else if (d.WeaponTiers != null)
                {
                    MigrateFlatTiersArray(m, d.WeaponTiers);
                }
            }
            else
            {
                MigrateV1WeaponFields(m, d.WeaponOwned, d.GunDamageLevel, d.FireRateLevel, d.ClipSizeLevel);
            }

            if (d.SaveVersion < 4)
                MigrateLegacyRepairTracks(m, d.RepairRigLevel, d.EngineeringLevel);

            // Ledger fields carry the missing-key sentinel (-1) from the JSON path; do not
            // re-derive them from SaveVersion here or the two pipelines diverge again.
            m.RestoreSalvageLedger(d.LifetimeSalvageEarned, d.LifetimeSalvageSpent);
            m.SaveVersion = SaveData.CurrentVersion;
            return m;
        }

        static void MigrateFlatTiersArray(MetaState m, int[] tiers)
        {
            m.EnsureArrays();
            int n = Math.Min(tiers.Length, WeaponCatalog.ArraySize);
            for (int i = 0; i < n; i++)
            {
                int tier = tiers[i];
                if (tier < 0) tier = 0;
                if (tier > WeaponTier.MaxTier) tier = WeaponTier.MaxTier;
                if (tier == 0) continue;
                m.ReplacePointsFromFlatTier((WeaponId)i, tier);
            }
        }

        /// <summary>Any legacy repair-track investment grants slot + rig free.</summary>
        static void MigrateLegacyRepairTracks(MetaState m, int repairRigLevel, int engineeringLevel)
        {
            if (repairRigLevel <= 0 && engineeringLevel <= 0) return;
            m.EquipmentSlotPurchased = true;
            m.SetEquipmentOwned(EquipmentId.RepairRig, true);
            m.EquippedEquipment = EquipmentId.RepairRig;
        }

        /// <summary>v5 EquippedDrone was WeaponId.None (-1). Only known PayloadIds survive.</summary>
        static PayloadId SanitizePayloadId(int raw)
        {
            if (raw == (int)PayloadId.Decoy) return PayloadId.Decoy;
            return PayloadId.None;
        }

        static void MigrateV1WeaponFields(MetaState m, bool weaponOwned,
            int gunDamageLevel, int fireRateLevel, int clipSizeLevel)
        {
            m.EnsureArrays();
            if (!weaponOwned) return;
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            int tier = gunDamageLevel;
            if (fireRateLevel > tier) tier = fireRateLevel;
            if (clipSizeLevel > tier) tier = clipSizeLevel;
            if (tier > WeaponTier.MaxTier) tier = WeaponTier.MaxTier;
            if (tier < 0) tier = 0;
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, tier);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
        }

        public string ToJson()
        {
            SaveData d = ToSaveData();
            var sb = new StringBuilder(256);
            sb.Append('{');
            AppendField(sb, "SaveVersion", d.SaveVersion.ToString(CultureInfo.InvariantCulture), false);
            AppendField(sb, "BankedSalvage", d.BankedSalvage.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "LifetimeSalvageEarned", d.LifetimeSalvageEarned.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "LifetimeSalvageSpent", d.LifetimeSalvageSpent.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "TotalRuns", d.TotalRuns.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "CleanExits", d.CleanExits.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "PodEjections", d.PodEjections.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "DeepestWaveReached",
                d.DeepestWaveReached.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "HatchArmorLevel", d.HatchArmorLevel.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "VitalityLevel", d.VitalityLevel.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "StaminaLevel", d.StaminaLevel.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "ScavengingLevel", d.ScavengingLevel.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "LuckLevel", d.LuckLevel.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquipmentSlotPurchased", d.EquipmentSlotPurchased ? "true" : "false", true);
            AppendField(sb, "OwnedEquipmentMask", d.OwnedEquipmentMask.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedEquipment", d.EquippedEquipment.ToString(CultureInfo.InvariantCulture), true);
            AppendBoolArray(sb, "WeaponsOwned", d.WeaponsOwned);
            AppendIntArray(sb, "WeaponWheelPoints", d.WeaponWheelPoints);
            AppendField(sb, "EquippedPrimary", d.EquippedPrimary.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedSecondary", d.EquippedSecondary.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedDrone", d.EquippedDrone.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "OwnedPayloadMask", d.OwnedPayloadMask.ToString(CultureInfo.InvariantCulture), true);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Parse keys into <see cref="SaveData"/> then migrate via
        /// <see cref="FromSaveData"/>. Version gates live only in FromSaveData.</summary>
        public static MetaState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("json is null or empty", "json");
            var d = new SaveData();
            d.SaveVersion = (int)ExtractLong(json, "SaveVersion", SaveData.CurrentVersion);
            d.BankedSalvage = ExtractLong(json, "BankedSalvage", 0);
            d.TotalRuns = (int)ExtractLong(json, "TotalRuns", 0);
            d.CleanExits = (int)ExtractLong(json, "CleanExits", 0);
            d.PodEjections = (int)ExtractLong(json, "PodEjections", 0);
            int deepest = (int)ExtractLong(json, "DeepestWaveReached", 1);
            d.DeepestWaveReached = deepest < 1 ? 1 : deepest;
            d.HatchArmorLevel = (int)ExtractLong(json, "HatchArmorLevel", 0);
            d.VitalityLevel = (int)ExtractLong(json, "VitalityLevel", 0);
            d.StaminaLevel = (int)ExtractLong(json, "StaminaLevel", 0);
            d.ScavengingLevel = (int)ExtractLong(json, "ScavengingLevel", 0);
            d.LuckLevel = (int)ExtractLong(json, "LuckLevel", 0);

            // Missing ledger keys → -1 at any version (JSON contract; FromSaveData must not gate).
            d.LifetimeSalvageEarned = ExtractLong(json, "LifetimeSalvageEarned", -1L);
            d.LifetimeSalvageSpent = ExtractLong(json, "LifetimeSalvageSpent", -1L);

            d.RepairRigLevel = (int)ExtractLong(json, "RepairRigLevel", 0);
            d.EngineeringLevel = (int)ExtractLong(json, "EngineeringLevel", 0);

            d.EquipmentSlotPurchased = ExtractBool(json, "EquipmentSlotPurchased", false);
            d.OwnedEquipmentMask = (int)ExtractLong(json, "OwnedEquipmentMask", 0);
            d.EquippedEquipment = (int)ExtractLong(json, "EquippedEquipment", (int)EquipmentId.None);
            d.OwnedPayloadMask = (int)ExtractLong(json, "OwnedPayloadMask", 0);

            bool hasV3Weapons = json.IndexOf("\"WeaponsOwned\"", StringComparison.Ordinal) >= 0;
            if (hasV3Weapons)
            {
                d.WeaponsOwned = new bool[WeaponCatalog.ArraySize];
                ExtractBoolArray(json, "WeaponsOwned", d.WeaponsOwned);
                d.EquippedPrimary = (int)ExtractLong(json, "EquippedPrimary", (int)WeaponId.None);
                d.EquippedSecondary = (int)ExtractLong(json, "EquippedSecondary", (int)WeaponId.None);
                d.EquippedDrone = (int)ExtractLong(json, "EquippedDrone", (int)PayloadId.None);

                bool hasWheel = json.IndexOf("\"WeaponWheelPoints\"", StringComparison.Ordinal) >= 0;
                if (hasWheel)
                {
                    d.WeaponWheelPoints = new int[WeaponCatalog.ArraySize * WeaponWheel.AttrCount];
                    ExtractIntArray(json, "WeaponWheelPoints", d.WeaponWheelPoints);
                }
                else
                {
                    d.WeaponTiers = new int[WeaponCatalog.ArraySize];
                    ExtractIntArray(json, "WeaponTiers", d.WeaponTiers);
                }
            }
            else
            {
                d.WeaponOwned = ExtractBool(json, "WeaponOwned", false);
                d.GunDamageLevel = (int)ExtractLong(json, "GunDamageLevel", 0);
                d.FireRateLevel = (int)ExtractLong(json, "FireRateLevel", 0);
                d.ClipSizeLevel = (int)ExtractLong(json, "ClipSizeLevel", 0);
            }

            return FromSaveData(d);
        }

        static void AppendField(StringBuilder sb, string name, string value, bool leadingComma)
        {
            if (leadingComma) sb.Append(',');
            sb.Append('"').Append(name).Append("\":").Append(value);
        }

        static void AppendBoolArray(StringBuilder sb, string name, bool[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(values[i] ? "true" : "false");
            }
            sb.Append(']');
        }

        static void AppendIntArray(StringBuilder sb, string name, int[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(']');
        }

        static void ExtractBoolArray(string json, string key, bool[] dest)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return;
            string[] parts = match.Groups[1].Value.Split(',');
            for (int i = 0; i < dest.Length && i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                dest[i] = p == "true" || p == "1";
            }
        }

        static void ExtractIntArray(string json, string key, int[] dest)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return;
            string[] parts = match.Groups[1].Value.Split(',');
            for (int i = 0; i < dest.Length && i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length == 0) continue;
                long v;
                if (long.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    dest[i] = (int)v;
            }
        }

        static bool ExtractBool(string json, string key, bool fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false|1|0)");
            if (!match.Success) return fallback;
            return match.Groups[1].Value == "true" || match.Groups[1].Value == "1";
        }

        static long ExtractLong(string json, string key, long fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            return match.Success
                ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
                : fallback;
        }
    }
}
