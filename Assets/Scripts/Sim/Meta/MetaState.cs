using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The pair of progression documents a session plays with: a <see cref="BoarderState"/>
    /// that belongs to the player and travels between worlds, and a <see cref="RealmState"/>
    /// that belongs to the world and stays there.
    /// </summary>
    /// <remarks>
    /// This type owns no progression fields of its own. It exists so the several hundred call
    /// sites that grew up against a single document keep reading one object, and so the two
    /// or three rules that genuinely need to see both halves have somewhere to live.
    ///
    /// Which half a fact belongs to is ruled in Docs/Milestones — the realm and the Boarder.
    /// The short version: kit travels, world stays, and the salvage ledger stays whole on the
    /// realm side because splitting it would break its own invariant.
    ///
    /// On disk these are two files, written independently — a guest writes its Boarder while
    /// the realm it is playing in belongs to somebody else. The composite JSON on this class
    /// is a session snapshot for dev tooling and tests, NOT the save format.
    /// </remarks>
    public sealed class MetaState
    {
        public BoarderState Boarder { get; private set; }
        public RealmState Realm { get; private set; }

        MetaState(BoarderState boarder, RealmState realm)
        {
            Boarder = boarder ?? BoarderState.CreateNew("", null);
            Realm = realm ?? RealmState.CreateNew("", null);
        }

        /// <summary>Pair a Boarder with the realm it is about to play in. Neither document is
        /// copied — a session mutates the same objects the save layer will write back.</summary>
        public static MetaState Compose(BoarderState boarder, RealmState realm)
        {
            return new MetaState(boarder, realm);
        }

        /// <summary>A fresh Boarder in a fresh realm. Ids stay empty: naming and identity are
        /// the game layer's, which is the only layer that knows what is already on disk.</summary>
        public static MetaState CreateNew()
        {
            return new MetaState(BoarderState.CreateNew("", null), RealmState.CreateNew("", null));
        }

        /// <summary>Always the version this build writes. A live object is never stale — only a
        /// file can be, and a file too old to read is refused by <see cref="SaveFormat"/>.</summary>
        public int SaveVersion { get { return SaveFormat.CurrentVersion; } }

        // ---- Realm: the salvage ledger ----

        public long BankedSalvage
        {
            get { return Realm.BankedSalvage; }
            set { Realm.BankedSalvage = value; }
        }

        public long LifetimeSalvageEarned
        {
            get { return Realm.LifetimeSalvageEarned; }
            set { Realm.LifetimeSalvageEarned = value; }
        }

        public long LifetimeSalvageSpent
        {
            get { return Realm.LifetimeSalvageSpent; }
            set { Realm.LifetimeSalvageSpent = value; }
        }

        public bool SalvageLedgerBalances { get { return Realm.SalvageLedgerBalances; } }

        public bool SalvageStatsWereRepaired
        {
            get { return Realm.SalvageStatsWereRepaired; }
            set { Realm.SalvageStatsWereRepaired = value; }
        }

        public void EarnSalvage(long amount) { Realm.EarnSalvage(amount); }

        public bool SpendSalvage(long amount) { return Realm.SpendSalvage(amount); }

        /// <summary>The realm's own spend floor. The kit term lives on the Boarder now — see
        /// <see cref="BoarderState.ReconstructSpentOnKit"/> and the note on the realm's copy.</summary>
        public long ReconstructSpentFromLevels() { return Realm.ReconstructSpentFromLevels(); }

        public static long SpentOnTrack(int level) { return RealmState.SpentOnTrack(level); }

        // ---- Realm: run record and world progress ----

        public int TotalRuns
        {
            get { return Realm.TotalRuns; }
            set { Realm.TotalRuns = value; }
        }

        public int CleanExits
        {
            get { return Realm.CleanExits; }
            set { Realm.CleanExits = value; }
        }

        public int PodEjections
        {
            get { return Realm.PodEjections; }
            set { Realm.PodEjections = value; }
        }

        public int HatchConversions
        {
            get { return Realm.HatchConversions; }
            set { Realm.HatchConversions = value; }
        }

        /// <summary>Deepest field Wave CLEARED. Setting it also raises
        /// <see cref="DeepestWaveEntered"/>, because a Wave cannot have been cleared without
        /// being entered — the invariant lives here so no seeding site (dev checkpoints, the
        /// dev realm, a migrated save) can set one and forget the other.</summary>
        public int DeepestWaveReached
        {
            get { return Realm.DeepestWaveReached; }
            set
            {
                Realm.DeepestWaveReached = value;
                if (value > Realm.DeepestWaveEntered) Realm.DeepestWaveEntered = value;
            }
        }

        /// <summary>Deepest field Wave ever drawn this campaign, cleared or not. See
        /// <see cref="RealmState.DeepestWaveEntered"/> for why this is separate from
        /// <see cref="DeepestWaveReached"/>.</summary>
        public int DeepestWaveEntered
        {
            get { return Realm.DeepestWaveEntered; }
            set { Realm.DeepestWaveEntered = value; }
        }

        public int HatchArmorLevel
        {
            get { return Realm.HatchArmorLevel; }
            set { Realm.HatchArmorLevel = value; }
        }

        public string[] CompletedTerminalIds { get { return Realm.CompletedTerminalIds; } }

        public void SetCompletedTerminalIds(string[] ids) { Realm.SetCompletedTerminalIds(ids); }

        public string[] OpenDoorIds { get { return Realm.OpenDoorIds; } }

        public void SetOpenDoorIds(string[] ids) { Realm.SetOpenDoorIds(ids); }

        // ---- Realm: the first-Bastion demo. The demo is a world, so it stays with one ----

        public const int DemoZoneCount = RealmState.DemoZoneCount;

        public bool DemoJuiceTaught
        {
            get { return Realm.DemoJuiceTaught; }
            set { Realm.DemoJuiceTaught = value; }
        }

        public bool DemoActive
        {
            get { return Realm.DemoActive; }
            set { Realm.DemoActive = value; }
        }

        public bool DemoCapsuleOpened
        {
            get { return Realm.DemoCapsuleOpened; }
            set { Realm.DemoCapsuleOpened = value; }
        }

        public bool DemoNavPowered
        {
            get { return Realm.DemoNavPowered; }
            set { Realm.DemoNavPowered = value; }
        }

        public bool DemoArmoryPowered
        {
            get { return Realm.DemoArmoryPowered; }
            set { Realm.DemoArmoryPowered = value; }
        }

        public bool DemoWeaponGranted
        {
            get { return Realm.DemoWeaponGranted; }
            set { Realm.DemoWeaponGranted = value; }
        }

        public bool DemoShipFightDone
        {
            get { return Realm.DemoShipFightDone; }
            set { Realm.DemoShipFightDone = value; }
        }

        public bool DemoCrewMenuUnlocked
        {
            get { return Realm.DemoCrewMenuUnlocked; }
            set { Realm.DemoCrewMenuUnlocked = value; }
        }

        public bool DemoFabPowered
        {
            get { return Realm.DemoFabPowered; }
            set { Realm.DemoFabPowered = value; }
        }

        public bool DemoZone2BankLost
        {
            get { return Realm.DemoZone2BankLost; }
            set { Realm.DemoZone2BankLost = value; }
        }

        public bool DemoRungTwoOpened
        {
            get { return Realm.DemoRungTwoOpened; }
            set { Realm.DemoRungTwoOpened = value; }
        }

        public int ZoneDeepest(int zone) { return Realm.ZoneDeepest(zone); }

        public void SetZoneDeepest(int zone, int wave) { Realm.SetZoneDeepest(zone, wave); }

        // ---- Boarder: weapons and the focus rings ----

        public WeaponId EquippedPrimary
        {
            get { return Boarder.EquippedPrimary; }
            set { Boarder.EquippedPrimary = value; }
        }

        public WeaponId EquippedSecondary
        {
            get { return Boarder.EquippedSecondary; }
            set { Boarder.EquippedSecondary = value; }
        }

        public bool OwnsAnyWeapon { get { return Boarder.OwnsAnyWeapon; } }

        public bool Owns(WeaponId id) { return Boarder.Owns(id); }

        public void SetOwned(WeaponId id, bool owned) { Boarder.SetOwned(id, owned); }

        public void SetEquipped(WeaponId id) { Boarder.SetEquipped(id); }

        public WeaponId Equipped(WeaponSlot slot) { return Boarder.Equipped(slot); }

        public int TierOf(WeaponId id) { return Boarder.TierOf(id); }

        public int[] PointsOf(WeaponId id) { return Boarder.PointsOf(id); }

        public int PointOf(WeaponId id, WheelAttr attr) { return Boarder.PointOf(id, attr); }

        public void CopyPointsInto(WeaponId id, int[] dest) { Boarder.CopyPointsInto(id, dest); }

        public int WheelCentresOf(WeaponId id, WheelAttr attr)
        {
            return Boarder.WheelCentresOf(id, attr);
        }

        public void AwardWheel(WeaponId id, WheelAttr centre) { Boarder.AwardWheel(id, centre); }

        public void ReplacePointsFromFlatTier(WeaponId id, int tier)
        {
            Boarder.ReplacePointsFromFlatTier(id, tier);
        }

        public void EnsureArrays() { Boarder.EnsureArrays(); }

        // ---- Boarder: protocol, equipment, drone bay ----

        public BoarderProtocolId EquippedProtocol
        {
            get { return Boarder.EquippedProtocol; }
            set { Boarder.EquippedProtocol = value; }
        }

        public BoarderAbilityId EquippedPassive
        {
            get { return Boarder.EquippedPassive; }
            set { Boarder.EquippedPassive = value; }
        }

        public bool SetEquippedPassive(BoarderAbilityId id) { return Boarder.SetEquippedPassive(id); }

        public bool EquipmentSlotPurchased
        {
            get { return Boarder.EquipmentSlotPurchased; }
            set { Boarder.EquipmentSlotPurchased = value; }
        }

        public int OwnedEquipmentMask
        {
            get { return Boarder.OwnedEquipmentMask; }
            set { Boarder.OwnedEquipmentMask = value; }
        }

        public EquipmentId EquippedEquipment
        {
            get { return Boarder.EquippedEquipment; }
            set { Boarder.EquippedEquipment = value; }
        }

        public bool OwnsEquipment(EquipmentId id) { return Boarder.OwnsEquipment(id); }

        public void SetEquipmentOwned(EquipmentId id, bool owned) { Boarder.SetEquipmentOwned(id, owned); }

        public void SetEquippedEquipment(EquipmentId id) { Boarder.SetEquippedEquipment(id); }

        public bool HasRepairRigEquipped { get { return Boarder.HasRepairRigEquipped; } }

        public int OwnedPayloadMask
        {
            get { return Boarder.OwnedPayloadMask; }
            set { Boarder.OwnedPayloadMask = value; }
        }

        public PayloadId EquippedDrone
        {
            get { return Boarder.EquippedDrone; }
            set { Boarder.EquippedDrone = value; }
        }

        public bool OwnsPayload(PayloadId id) { return Boarder.OwnsPayload(id); }

        public void SetPayloadOwned(PayloadId id, bool owned) { Boarder.SetPayloadOwned(id, owned); }

        public bool SetEquippedPayload(PayloadId id) { return Boarder.SetEquippedPayload(id); }

        public bool PayloadLoadoutLocked
        {
            get { return Boarder.PayloadLoadoutLocked; }
            set { Boarder.PayloadLoadoutLocked = value; }
        }

        // ---- The rules that genuinely need both halves ----

        /// <summary>Null when repair is available; otherwise the next missing step for prompts.
        /// This reads across both documents on purpose — the gate is the realm's (it has to
        /// have earned something) but the rig is the Boarder's kit.
        /// <para>The first step MUST read the same key as
        /// <see cref="MetaUpgrades.UpgradesUnlocked"/>, or the prompt becomes a second vote on
        /// a rule it only exists to explain. It gated on HatchConversions until 2026-08-25;
        /// with the Convert window out of the demo that counter can never rise, so every demo
        /// player was told the slot was locked while it was genuinely buyable.</para></summary>
        public string RepairMissingStep()
        {
            if (!MetaUpgrades.UpgradesUnlocked(this))
                return "equipment slot locked until this realm has earned salvage";
            if (!Boarder.EquipmentSlotPurchased)
                return "equipment slot not purchased — buy it at FABRICATION";
            if (!Boarder.OwnsEquipment(EquipmentId.RepairRig))
                return "repair rig not owned — buy it at FABRICATION";
            if (Boarder.EquippedEquipment != EquipmentId.RepairRig)
                return "repair rig not equipped — equip it at FABRICATION";
            return null;
        }

        // ---- Session snapshot. NOT the save format: on disk these are two files ----

        /// <summary>Both documents in one string, for dev checkpoints and for the test guard
        /// that has to put the developer's campaign back exactly as it found it. Nothing loads
        /// a campaign through this — SaveService reads and writes the two files.</summary>
        public string ToSnapshotJson()
        {
            return "{\"Boarder\":" + Boarder.ToJson() + ",\"Realm\":" + Realm.ToJson() + "}";
        }

        /// <summary>Read back <see cref="ToSnapshotJson"/>. Returns null when either half is
        /// missing or refuses its version, so a caller restoring a snapshot can say so rather
        /// than quietly handing back a half-built session.</summary>
        public static MetaState FromSnapshotJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string boarder = Section(json, "\"Boarder\":");
            string realm = Section(json, "\"Realm\":");
            if (boarder == null || realm == null)
            {
                Log.Warn("Save", "snapshot is missing a document — not restoring a half session");
                return null;
            }
            var b = BoarderState.FromJson(boarder);
            var r = RealmState.FromJson(realm);
            if (b == null || r == null)
            {
                Log.Warn("Save", "snapshot refused — "
                    + (b == null ? "its Boarder" : "its realm")
                    + " was written by a version this build does not read.");
                return null;
            }
            return Compose(b, r);
        }

        /// <summary>The balanced-brace object that follows the given key. Hand-scanned rather
        /// than regexed because the two documents nest, and a lazy match would stop at the
        /// first inner brace.</summary>
        static string Section(string json, string key)
        {
            int at = json.IndexOf(key, System.StringComparison.Ordinal);
            if (at < 0) return null;
            int start = json.IndexOf('{', at + key.Length);
            if (start < 0) return null;
            int depth = 0;
            bool inString = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(start, i - start + 1);
                }
            }
            return null;
        }
    }
}
