using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Roguelite meta-progression that persists across runs: banked salvage, attributes,
    /// equipment, and per-weapon ownership / wheel points / equipped slots. Serializes to/from
    /// JSON by hand (field-for-field identical to JsonUtility on SaveData; no Newtonsoft).
    /// The game layer owns file paths and IO.
    /// </summary>
    public sealed partial class MetaState
    {
        public long BankedSalvage;
        public long LifetimeSalvageEarned;
        public long LifetimeSalvageSpent;

        public int TotalRuns;
        public int CleanExits;
        // Save/API trap: PodEjections means lost Runs; keep this field and its JSON key unchanged.
        // Endings and demo.second_loss read it. It is NOT a progression gate (re-key 2026-08-18).
        public int PodEjections;

        /// <summary>Bank windows in which salvage converted through the Hatch for this seat
        /// (v11). The wheel and the equipment slot key on <c>&gt;= 1</c>: the wheel spends
        /// salvage that came through the Hatch, so it opens when salvage first comes through
        /// the Hatch — for the player who banks and docks as well as the one who banks and
        /// dies (re-key 2026-08-18 Zo; Docs/Decisions.md).</summary>
        public int HatchConversions;

        /// <summary>Highest 1-based wave the player has cleared this campaign. Starts at 1,
        /// rises only on clear (never on reach), never lowers. Caps the MAP start-wave selector.</summary>
        public int DeepestWaveReached = 1;

        public int SaveVersion = SaveData.CurrentVersion;

        /// <summary>Terminal ids completed at the last successful Hatch-bank. Deep start restores
        /// this graph; a Wave-1 start does not.</summary>
        string[] _completedTerminalIds = Array.Empty<string>();

        public string[] CompletedTerminalIds =>
            _completedTerminalIds ?? (_completedTerminalIds = Array.Empty<string>());

        /// <summary>Replace the Hatch-bank graph snapshot. Null or empty clears it.</summary>
        public void SetCompletedTerminalIds(string[] ids)
        {
            _completedTerminalIds = ids == null || ids.Length == 0
                ? Array.Empty<string>()
                : (string[])ids.Clone();
        }

        /// <summary>Door ids standing open, saved on a survived Hatch-bank window. Not derivable
        /// from CompletedTerminalIds: a completed terminal only makes a door eligible.</summary>
        string[] _openDoorIds = Array.Empty<string>();

        public string[] OpenDoorIds =>
            _openDoorIds ?? (_openDoorIds = Array.Empty<string>());

        /// <summary>Replace the open-door snapshot. Null or empty clears it.</summary>
        public void SetOpenDoorIds(string[] ids)
        {
            _openDoorIds = ids == null || ids.Length == 0
                ? Array.Empty<string>()
                : (string[])ids.Clone();
        }

        public int HatchArmorLevel;

        // ---- First-Bastion demo (v9) — Docs/Demo - Single/README.md, Shared flags ----
        /// <summary>Zones the per-zone deepest array covers (0 unused; 1, 2, finale).</summary>
        public const int DemoZoneCount = 4;

        /// <summary>True until this save's first Escaped. Fresh campaigns start on demo.</summary>
        public bool DemoActive = true;
        // "Capsule" predates the teleporter that replaced the wake bed. It is a
        // serialized save field: renaming it costs a SaveData version bump and a migration
        // for no player-visible gain, so the name stays.
        public bool DemoCapsuleOpened;
        public bool DemoNavPowered;
        public bool DemoArmoryPowered;
        public bool DemoWeaponGranted;
        public bool DemoShipFightDone;
        public bool DemoCrewMenuUnlocked;
        public bool DemoFabPowered;
        /// <summary>Wheel ladder rung 1: Secondary buys remaining while DemoActive. The
        /// multiplayer gate reads it — the first buy (2 → 1) unlocks friends.</summary>
        public int DemoWheelBuysLeft = 2;

        /// <summary>A Run ended Lost inside zone 2's Hatch-bank window. This is the gate that
        /// sells the Primary — the event the player just played. It is NOT derived from
        /// PodEjections and must never be re-keyed to it: a save that docked through zone 1
        /// arrives here on its first ejection and must still be offered the rack
        /// (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        public bool DemoZone2BankLost;

        /// <summary>Once-per-save latch for the rung-2 Docked resolve: the demo.second_loss
        /// line and the ladder log fire exactly once, however many times the player docks
        /// afterwards (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        public bool DemoRungTwoOpened;

        /// <summary>Wheel ladder rung 2: buys remaining, set to 2 when rung 2 opens and
        /// counted SEPARATELY from <see cref="DemoWheelBuysLeft"/>. It is a second counter
        /// rather than a raise of the first because the first one's absolute value is
        /// load-bearing for another feature: DemoProgress.MultiplayerUnlocked is
        /// <c>DemoWheelBuysLeft &lt; 2</c>, and the once-per-save multiplayer line keys on
        /// that counter passing exactly 1. Raising it for rung 2 would re-lock multiplayer
        /// mid-demo and replay the line. Do not merge the two counters
        /// (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        public int DemoRungTwoBuysLeft;

        int[] _demoZoneDeepest = new int[DemoZoneCount];

        /// <summary>Deepest cleared field Wave in <paramref name="zone"/> (0 = none yet).</summary>
        public int ZoneDeepest(int zone)
        {
            EnsureDemoZones();
            if (zone < 0 || zone >= DemoZoneCount) return 0;
            return _demoZoneDeepest[zone];
        }

        /// <summary>Raise-only write on field-Wave clear. A lower wave never overwrites.</summary>
        public void SetZoneDeepest(int zone, int wave)
        {
            EnsureDemoZones();
            if (zone < 0 || zone >= DemoZoneCount) return;
            if (wave > _demoZoneDeepest[zone]) _demoZoneDeepest[zone] = wave;
        }

        void EnsureDemoZones()
        {
            if (_demoZoneDeepest == null || _demoZoneDeepest.Length != DemoZoneCount)
            {
                var next = new int[DemoZoneCount];
                if (_demoZoneDeepest != null)
                    Array.Copy(_demoZoneDeepest, next,
                        Math.Min(_demoZoneDeepest.Length, next.Length));
                _demoZoneDeepest = next;
            }
        }

        /// <summary>Equipped protocol. Fresh saves are None until CREW claims Default.</summary>
        public BoarderProtocolId EquippedProtocol = BoarderProtocolId.None;
        /// <summary>Equipped passive. Default has none; specialists may add one later.</summary>
        public BoarderAbilityId EquippedPassive = BoarderAbilityId.None;

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

        public bool SetEquippedPassive(BoarderAbilityId id)
        {
            if (PayloadLoadoutLocked)
            {
                Log.Event("Ship", "PickPassive refused — run in progress track=crew");
                return false;
            }
            if (!BoarderProtocol.OwnsPassive(EquippedProtocol, id))
            {
                Log.Event("Ship", "PickPassive refused — not on this protocol track=crew");
                return false;
            }
            EquippedPassive = id;
            return true;
        }

        /// <summary>Null when repair is available; otherwise the next missing step for prompts.</summary>
        public string RepairMissingStep()
        {
            if (HatchConversions < 1)
                return "equipment slot locked until salvage banks through the Hatch";
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

        public static long SpentOnTrack(int level)
        {
            if (level <= 0) return 0;
            if (level > MetaUpgrades.MaxLevel) level = MetaUpgrades.MaxLevel;
            return MetaUpgrades.BaseCost * ((1L << level) - 1L);
        }

        void RestoreSalvageLedger(long earnedRaw, long spentRaw, long crewRefund)
        {
            long floor = ReconstructSpentFromLevels() + crewRefund;
            LifetimeSalvageSpent = spentRaw > floor ? spentRaw : floor;
            LifetimeSalvageEarned = BankedSalvage + LifetimeSalvageSpent;
            SalvageStatsWereRepaired =
                spentRaw != LifetimeSalvageSpent || earnedRaw != LifetimeSalvageEarned;
            if (crewRefund <= 0) return;
            BankedSalvage += crewRefund;
            LifetimeSalvageSpent -= crewRefund;
            if (LifetimeSalvageSpent < 0L) LifetimeSalvageSpent = 0L;
            Log.Event("Save", "crew tracks refunded " + crewRefund);
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
            d.HatchConversions = HatchConversions;
            d.DeepestWaveReached = DeepestWaveReached < 1 ? 1 : DeepestWaveReached;
            d.HatchArmorLevel = HatchArmorLevel;
            d.EquippedProtocol = (int)EquippedProtocol;
            d.EquippedPassive = (int)EquippedPassive;
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
            d.CompletedTerminalIds = (string[])CompletedTerminalIds.Clone();
            d.OpenDoorIds = (string[])OpenDoorIds.Clone();
            EnsureDemoZones();
            d.DemoActive = DemoActive;
            d.DemoCapsuleOpened = DemoCapsuleOpened;
            d.DemoNavPowered = DemoNavPowered;
            d.DemoArmoryPowered = DemoArmoryPowered;
            d.DemoWeaponGranted = DemoWeaponGranted;
            d.DemoShipFightDone = DemoShipFightDone;
            d.DemoCrewMenuUnlocked = DemoCrewMenuUnlocked;
            d.DemoFabPowered = DemoFabPowered;
            d.DemoWheelBuysLeft = DemoWheelBuysLeft;
            d.DemoZone2BankLost = DemoZone2BankLost;
            d.DemoRungTwoOpened = DemoRungTwoOpened;
            d.DemoRungTwoBuysLeft = DemoRungTwoBuysLeft;
            d.DemoZoneDeepestWave = (int[])_demoZoneDeepest.Clone();
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
            // v11: HatchConversions is its own saved fact. Pre-v11 saves ran under the
            // loss key (UpgradesUnlocked == PodEjections >= 1): a save that has lost a
            // Run — or Escaped, whose final bank converted — had the wheel open, and
            // the re-key must not re-lock it.
            if (d.SaveVersion >= 11)
                m.HatchConversions = d.HatchConversions;
            else if (d.PodEjections >= 1 || d.CleanExits >= 1)
                m.HatchConversions = 1;
            m.DeepestWaveReached = d.DeepestWaveReached < 1 ? 1 : d.DeepestWaveReached;
            m.HatchArmorLevel = d.HatchArmorLevel;
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

            if (d.SaveVersion >= 7 && d.CompletedTerminalIds != null)
                m.SetCompletedTerminalIds(d.CompletedTerminalIds);

            if (d.SaveVersion >= 10 && d.OpenDoorIds != null)
                m.SetOpenDoorIds(d.OpenDoorIds);

            ApplyDemoFlags(m, d);

            long refund = BoarderProtocol.ApplySaveEquip(m, d);
            m.RestoreSalvageLedger(d.LifetimeSalvageEarned, d.LifetimeSalvageSpent, refund);
            m.SaveVersion = SaveData.CurrentVersion;
            return m;
        }

        /// <summary>v9 reads the stored flags. Pre-v9 migrates: demo-off when the save has
        /// already played (CleanExits >= 1 or DeepestWaveReached > 1 — do not trap them in
        /// the capsule), demo-on with fresh gates otherwise (Docs/Demo - Single/README.md).</summary>
        static void ApplyDemoFlags(MetaState m, SaveData d)
        {
            m.EnsureDemoZones();
            if (d.SaveVersion >= 9)
            {
                m.DemoActive = d.DemoActive;
                m.DemoCapsuleOpened = d.DemoCapsuleOpened;
                m.DemoNavPowered = d.DemoNavPowered;
                m.DemoArmoryPowered = d.DemoArmoryPowered;
                m.DemoWeaponGranted = d.DemoWeaponGranted;
                m.DemoShipFightDone = d.DemoShipFightDone;
                m.DemoCrewMenuUnlocked = d.DemoCrewMenuUnlocked;
                m.DemoFabPowered = d.DemoFabPowered;
                m.DemoWheelBuysLeft = d.DemoWheelBuysLeft;
                m.DemoZone2BankLost = d.DemoZone2BankLost;
                m.DemoRungTwoOpened = d.DemoRungTwoOpened;
                m.DemoRungTwoBuysLeft = d.DemoRungTwoBuysLeft;
                if (d.DemoZoneDeepestWave != null)
                    Array.Copy(d.DemoZoneDeepestWave, m._demoZoneDeepest,
                        Math.Min(d.DemoZoneDeepestWave.Length, DemoZoneCount));
                return;
            }

            bool played = d.CleanExits >= 1 || d.DeepestWaveReached > 1;
            m.DemoActive = !played;
            m.DemoCapsuleOpened = played;
            m.DemoNavPowered = played;
            m.DemoArmoryPowered = played;
            m.DemoWeaponGranted = played;
            m.DemoShipFightDone = played;
            m.DemoCrewMenuUnlocked = played;
            m.DemoFabPowered = played;
            m.DemoWheelBuysLeft = played ? 0 : 2;
            // Deliberately NOT keyed to played, unlike every flag above. This is not an
            // oversight and must not be "fixed": a pre-v9 save never played zone 2's bank,
            // so its ladder rung 2 is unearned whatever else the save has done. Defaults
            // both ways (Docs/Demo - Single/5-repeat-to-control-room.md).
            m.DemoZone2BankLost = false;
            m.DemoRungTwoOpened = false;
            m.DemoRungTwoBuysLeft = 0;
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
    }
}
