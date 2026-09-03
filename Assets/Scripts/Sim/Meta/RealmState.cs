using System;
using System.Text;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The half of meta-progression that belongs to the world rather than to the player: the
    /// salvage ledger, the run tallies, the Hatch armour track, the terminal/door graph and the
    /// first-Bastion demo ladder. A realm stays where it is — a guest playing in a host's realm
    /// banks into and spends from THIS document, while their Boarder travels home with them.
    /// </summary>
    /// <remarks>
    /// Serializes by hand through <see cref="JsonText"/>, field-for-field compatible with what
    /// JsonUtility would have produced, so the keys a player's save already carries keep their
    /// spelling. The game layer owns file paths and IO; nothing here touches the disk.
    /// </remarks>
    public sealed class RealmState
    {
        /// <summary>Stable identity, assigned by the game layer. Ids are never minted in Sim.</summary>
        public string Id = "";

        /// <summary>Display name, shown on the realm select rows.</summary>
        public string Name = DefaultName;

        /// <summary>What an unnamed realm is called.</summary>
        public const string DefaultName = "REALM";

        public long BankedSalvage;
        public long LifetimeSalvageEarned;
        public long LifetimeSalvageSpent;

        public int TotalRuns;
        public int CleanExits;
        // Save/API trap: PodEjections means lost Runs; keep this field and its JSON key unchanged.
        // Endings and demo.second_loss read it. It is NOT a progression gate (re-key 2026-08-18).
        public int PodEjections;

        /// <summary>Bank windows in which salvage converted through the Hatch for this seat.
        /// The wheel and the equipment slot key on <c>&gt;= 1</c>: the wheel spends salvage that
        /// came through the Hatch, so it opens when salvage first comes through the Hatch — for
        /// the player who banks and docks as well as the one who banks and dies
        /// (re-key 2026-08-18 Zo; Docs/Decisions.md).</summary>
        public int HatchConversions;

        /// <summary>Highest 1-based wave the player has cleared this campaign. Starts at 1,
        /// rises only on clear (never on reach), never lowers. Caps the MAP start-wave selector.
        /// <para>NO LONGER CAPS THE START-WAVE SELECTOR (2026-08-27). It read "clear-only is
        /// load-bearing: deep start may only offer a Wave the player has actually beaten." The
        /// rule was guarding against an exploit that does not exist — a deep start CHAINS waves
        /// 1..N into one assault, so a deeper choice is a longer, harder run, and dying to buy
        /// one buys the player nothing. <see cref="DeepestWaveEntered"/> caps it now. This value
        /// is still the honest record of what has been BEATEN and is still worth keeping
        /// separate; it simply is not a permission any more.</para></summary>
        public int DeepestWaveReached = 1;

        /// <summary>Highest 1-based FIELD wave this campaign has ever had on the table, cleared
        /// or not. Rises the moment the wave is drawn, never lowers, and gates nothing the
        /// player could exploit by dying — unlike <see cref="DeepestWaveReached"/> it is not a
        /// permission, it is a record of what has been seen.
        /// <para>Added 2026-08-25 (Zo) for the Primary rack. The rack has to open for the
        /// player who MET the wall, and the 2026-08-25 playthrough died on Wave 2 holding 227
        /// banked with nothing to buy — a state DeepestWaveReached cannot distinguish from
        /// never having drawn Wave 2 at all.</para></summary>
        public int DeepestWaveEntered = 1;

        public int HatchArmorLevel;

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

        // ---- First-Bastion demo — Docs/Demo - Single/README.md, Shared flags ----
        /// <summary>Zones the per-zone deepest array covers (0 unused; 1, 2, finale).</summary>
        public const int DemoZoneCount = 4;

        /// <summary>True until this save's first Escaped. Fresh campaigns start on demo.</summary>
        public bool DemoActive = true;
        // "Capsule" predates the teleporter that replaced the wake bed. It is a
        // serialized save field: renaming it costs a save version bump and a migration
        // for no player-visible gain, so the name stays.
        public bool DemoCapsuleOpened;
        public bool DemoNavPowered;
        public bool DemoArmoryPowered;
        public bool DemoWeaponGranted;
        public bool DemoShipFightDone;
        public bool DemoCrewMenuUnlocked;

        /// <summary>demo.juice_teach played — once per save, on the first field-terminal
        /// look with Unbanked to spend (beat 1). Additive JSON key: an older v13 document
        /// simply replays the line once, which is the harmless direction.</summary>
        public bool DemoJuiceTaught;
        public bool DemoFabPowered;
        /// <summary>A Run ended Lost inside zone 2's Hatch-bank window. This is the gate that
        /// sells the Primary — the event the player just played. It is NOT derived from
        /// PodEjections and must never be re-keyed to it: a save that docked through zone 1
        /// arrives here on its first ejection and must still be offered the rack
        /// (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        /// <summary>Zone 2 RESOLVED — a zone-2 Convert window survived OR the run was
        /// lost during one. Opens the demo's Primary rack either way (Zo, 2026-08-25).
        /// The name keeps its original loss-only spelling because it is a save v13 JSON
        /// key; renaming it is a save-version bump for a word.</summary>
        public bool DemoZone2BankLost;

        /// <summary>Once-per-save latch for the rack-opened Docked resolve: the demo.second_loss
        /// line and its log fire exactly once, however many times the player docks afterwards
        /// (Docs/Demo - Single/5-repeat-to-control-room.md). Named for the wheel ladder's second
        /// rung, cut 2026-09-02 (Zo: anything affordable is buyable); the name is a save v13
        /// JSON key and stays.</summary>
        public bool DemoRungTwoOpened;

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

        public bool SalvageLedgerBalances =>
            LifetimeSalvageEarned == BankedSalvage + LifetimeSalvageSpent;

        [NonSerialized] public bool SalvageStatsWereRepaired;

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

        /// <summary>
        /// Spend floor for this realm's ledger: only what this realm itself sold, which is the
        /// Hatch armour track. Weapon costs, derived Tiers and payloads are NOT counted here —
        /// they moved to <c>BoarderState.ReconstructSpentOnKit</c>.
        /// </summary>
        /// <remarks>
        /// The floor has to be reconstructible from inside a single document. A Boarder is
        /// portable, so a well-equipped one walking into a fresh realm would otherwise raise
        /// that realm's floor by kit it bought somewhere else entirely, and the ledger would
        /// then claim salvage the realm never earned. Anything the Boarder carries is the
        /// Boarder's spend to account for; the realm counts only its own.
        /// Granted armour IS counted, because nothing records how a level was come by. The
        /// floor is reconstructed from what the realm HAS, not from what it bought, so a
        /// scripted or debug grant raises it exactly as a purchase would and the ledger will
        /// report salvage that was never spent. That is a known limit of reconstructing a
        /// floor at all, and `EquipmentTests.GrantsNotCountedInSpendFloor` pins the armour
        /// term at its face value rather than at a purchase history.
        /// </remarks>
        public long ReconstructSpentFromLevels()
        {
            return SpentOnTrack(HatchArmorLevel);
        }

        public static long SpentOnTrack(int level)
        {
            if (level <= 0) return 0;
            if (level > MetaUpgrades.MaxLevel) level = MetaUpgrades.MaxLevel;
            return MetaUpgrades.BaseCost * ((1L << level) - 1L);
        }

        /// <summary>Rebuild the ledger from what the file claimed, clamped up to the floor the
        /// realm's own purchases prove. A file whose numbers had to move is flagged so the game
        /// layer can say so rather than silently changing the player's totals.</summary>
        void RestoreSalvageLedger(long earnedRaw, long spentRaw)
        {
            long floor = ReconstructSpentFromLevels();
            LifetimeSalvageSpent = spentRaw > floor ? spentRaw : floor;
            LifetimeSalvageEarned = BankedSalvage + LifetimeSalvageSpent;
            SalvageStatsWereRepaired =
                spentRaw != LifetimeSalvageSpent || earnedRaw != LifetimeSalvageEarned;
        }

        /// <summary>A realm nobody has played yet. Ids and names come from the game layer.</summary>
        /// <summary>A realm always has something to be called. An empty name reaches a log
        /// line and a chooser row, and "" in either reads as a bug rather than as a realm.</summary>
        static string NameOrDefault(string name)
        {
            return string.IsNullOrEmpty(name) ? DefaultName : name;
        }

        /// <summary>Built through CreateNew or FromJson only, for the same reason as
        /// BoarderState's: a bare instance has not had its zone array sized.</summary>
        RealmState() { }

        public static RealmState CreateNew(string id, string name)
        {
            var r = new RealmState();
            r.Id = id ?? "";
            r.Name = NameOrDefault(name);
            r.EnsureDemoZones();
            return r;
        }

        public string ToJson()
        {
            EnsureDemoZones();
            var sb = new StringBuilder(256);
            sb.Append('{');
            JsonText.AppendLong(sb, "SaveVersion", SaveFormat.CurrentVersion, false);
            JsonText.AppendString(sb, "Id", Id, true);
            JsonText.AppendString(sb, "Name", NameOrDefault(Name), true);
            JsonText.AppendLong(sb, "BankedSalvage", BankedSalvage, true);
            JsonText.AppendLong(sb, "LifetimeSalvageEarned", LifetimeSalvageEarned, true);
            JsonText.AppendLong(sb, "LifetimeSalvageSpent", LifetimeSalvageSpent, true);
            JsonText.AppendLong(sb, "TotalRuns", TotalRuns, true);
            JsonText.AppendLong(sb, "CleanExits", CleanExits, true);
            JsonText.AppendLong(sb, "PodEjections", PodEjections, true);
            JsonText.AppendLong(sb, "HatchConversions", HatchConversions, true);
            JsonText.AppendLong(sb, "DeepestWaveReached",
                DeepestWaveReached < 1 ? 1 : DeepestWaveReached, true);
            JsonText.AppendLong(sb, "DeepestWaveEntered",
                DeepestWaveEntered < 1 ? 1 : DeepestWaveEntered, true);
            JsonText.AppendLong(sb, "HatchArmorLevel", HatchArmorLevel, true);
            JsonText.AppendStringArray(sb, "CompletedTerminalIds", CompletedTerminalIds);
            JsonText.AppendStringArray(sb, "OpenDoorIds", OpenDoorIds);
            JsonText.AppendBool(sb, "DemoActive", DemoActive, true);
            JsonText.AppendBool(sb, "DemoCapsuleOpened", DemoCapsuleOpened, true);
            JsonText.AppendBool(sb, "DemoNavPowered", DemoNavPowered, true);
            JsonText.AppendBool(sb, "DemoArmoryPowered", DemoArmoryPowered, true);
            JsonText.AppendBool(sb, "DemoWeaponGranted", DemoWeaponGranted, true);
            JsonText.AppendBool(sb, "DemoShipFightDone", DemoShipFightDone, true);
            JsonText.AppendBool(sb, "DemoCrewMenuUnlocked", DemoCrewMenuUnlocked, true);
            JsonText.AppendBool(sb, "DemoJuiceTaught", DemoJuiceTaught, true);
            JsonText.AppendBool(sb, "DemoFabPowered", DemoFabPowered, true);
            JsonText.AppendBool(sb, "DemoZone2BankLost", DemoZone2BankLost, true);
            JsonText.AppendBool(sb, "DemoRungTwoOpened", DemoRungTwoOpened, true);
            JsonText.AppendIntArray(sb, "DemoZoneDeepestWave", _demoZoneDeepest);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Read a realm document. Returns null when the version is one this build does
        /// not read — the caller logs <see cref="SaveFormat.RefusalFor"/> naming the file, because
        /// a refusal the player never sees reads as data loss. Null or empty input is a caller
        /// bug rather than a bad save, and throws.</summary>
        public static RealmState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("json is null or empty", "json");

            // A missing key must not impersonate the current version: 0 is unsupported and refuses.
            int version = JsonText.ExtractInt(json, "SaveVersion", 0);
            if (!SaveFormat.IsSupported(version)) return null;

            var r = new RealmState();
            r.Id = JsonText.ExtractString(json, "Id", "");
            r.Name = NameOrDefault(JsonText.ExtractString(json, "Name", DefaultName));
            r.BankedSalvage = JsonText.ExtractLong(json, "BankedSalvage", 0L);
            r.TotalRuns = JsonText.ExtractInt(json, "TotalRuns", 0);
            r.CleanExits = JsonText.ExtractInt(json, "CleanExits", 0);
            r.PodEjections = JsonText.ExtractInt(json, "PodEjections", 0);
            r.HatchConversions = JsonText.ExtractInt(json, "HatchConversions", 0);
            int deepest = JsonText.ExtractInt(json, "DeepestWaveReached", 1);
            r.DeepestWaveReached = deepest < 1 ? 1 : deepest;
            // A save written before the field existed has entered at least what it cleared,
            // so the cleared figure is the honest floor rather than 1.
            int entered = JsonText.ExtractInt(json, "DeepestWaveEntered", r.DeepestWaveReached);
            r.DeepestWaveEntered = entered < r.DeepestWaveReached ? r.DeepestWaveReached : entered;
            r.HatchArmorLevel = JsonText.ExtractInt(json, "HatchArmorLevel", 0);

            r.SetCompletedTerminalIds(JsonText.ExtractStringArray(json, "CompletedTerminalIds"));
            r.SetOpenDoorIds(JsonText.ExtractStringArray(json, "OpenDoorIds"));

            r.DemoActive = JsonText.ExtractBool(json, "DemoActive", false);
            r.DemoCapsuleOpened = JsonText.ExtractBool(json, "DemoCapsuleOpened", false);
            r.DemoNavPowered = JsonText.ExtractBool(json, "DemoNavPowered", false);
            r.DemoArmoryPowered = JsonText.ExtractBool(json, "DemoArmoryPowered", false);
            r.DemoWeaponGranted = JsonText.ExtractBool(json, "DemoWeaponGranted", false);
            r.DemoShipFightDone = JsonText.ExtractBool(json, "DemoShipFightDone", false);
            r.DemoCrewMenuUnlocked = JsonText.ExtractBool(json, "DemoCrewMenuUnlocked", false);
            r.DemoJuiceTaught = JsonText.ExtractBool(json, "DemoJuiceTaught", false);
            r.DemoFabPowered = JsonText.ExtractBool(json, "DemoFabPowered", false);
            r.DemoZone2BankLost = JsonText.ExtractBool(json, "DemoZone2BankLost", false);
            r.DemoRungTwoOpened = JsonText.ExtractBool(json, "DemoRungTwoOpened", false);
            r.EnsureDemoZones();
            JsonText.ExtractIntArray(json, "DemoZoneDeepestWave", r._demoZoneDeepest);

            // Missing ledger keys → -1, which is below every floor and so reads as "repair me".
            long earnedRaw = JsonText.ExtractLong(json, "LifetimeSalvageEarned", -1L);
            long spentRaw = JsonText.ExtractLong(json, "LifetimeSalvageSpent", -1L);
            r.RestoreSalvageLedger(earnedRaw, spentRaw);
            return r;
        }
    }
}
