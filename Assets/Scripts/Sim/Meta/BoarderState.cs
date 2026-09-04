using System;
using System.Text;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The half of meta-progression that belongs to the player rather than to the world: identity,
    /// weapons owned with their wheel points, and the equipped protocol / passive / equipment /
    /// payload. A Boarder travels — the same document is carried into a host's realm as into the
    /// player's own — so nothing world-shaped may live here. The salvage ledger, the run tallies,
    /// the unlock graph and the demo flags are the realm's
    /// (Docs/Milestones — the realm and the Boarder).
    /// </summary>
    /// <remarks>
    /// Serializes by hand through <see cref="JsonText"/>, field-for-field compatible with what
    /// JsonUtility would produce. The game layer owns file paths, IO and id generation; this type
    /// touches none of them.
    /// </remarks>
    public sealed class BoarderState
    {
        /// <summary>Display name a Boarder is created with when the caller supplies none.</summary>
        const string DefaultName = "BOARDER";

        /// <summary>Stable identity, assigned by the game layer — Sim mints no ids. Never null
        /// after a load: a document missing the key loads with an empty id, and the caller, which
        /// knows the file it came from, fills it in.</summary>
        public string Id = string.Empty;

        /// <summary>Display name, shown wherever Boarders are listed and chosen between.</summary>
        public string Name = DefaultName;

        // ---- Weapons ----
        bool[] _weaponsOwned;
        /// <summary>Flat ArraySize×5: index = weaponId * AttrCount + (int)WheelAttr.</summary>
        int[] _wheelPoints;
        /// <summary>Same layout as <see cref="_wheelPoints"/>: how many purchases have been
        /// CENTRED on each spoke. Neighbour spill does not count.</summary>
        int[] _wheelCentres;
        public WeaponId EquippedPrimary = WeaponId.None;
        public WeaponId EquippedSecondary = WeaponId.None;

        /// <summary>Equipped protocol. Fresh saves are None until CREW claims DEPOT.</summary>
        public BoarderProtocolId EquippedProtocol = BoarderProtocolId.None;
        /// <summary>Equipped passive. DEPOT has none; specialists may add one later.</summary>
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

        // ---- Realms this Boarder has been admitted to ----
        // The menu's half of the shared-realm roster: the realm's own member list decides
        // ADMISSION, these rows only decide what the chooser offers, and a row the realm no
        // longer honours is pruned when entry is refused (Docs/Milestones — a realm you can
        // open while its owner is offline). Parallel arrays in the house JSON style; absent
        // on an older document, which reads as none remembered.
        string[] _knownRealmIds = Array.Empty<string>();
        string[] _knownRealmOwners = Array.Empty<string>();
        string[] _knownRealmNames = Array.Empty<string>();

        /// <summary>A Boarder with the given identity and empty kit. Ids come from the game
        /// layer; an empty or null name falls back to the default rather than saving blank.</summary>
        /// <summary>Built through CreateNew or FromJson only: a bare instance has null
        /// arrays until EnsureArrays runs, which is a shape nothing should be handed.</summary>
        BoarderState() { }

        public static BoarderState CreateNew(string id, string name)
        {
            var b = new BoarderState();
            b.Id = id ?? string.Empty;
            b.Name = string.IsNullOrEmpty(name) ? DefaultName : name;
            b.EnsureArrays();
            return b;
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
            if (_wheelCentres == null || _wheelCentres.Length != need)
            {
                var next = new int[need];
                if (_wheelCentres != null)
                    Array.Copy(_wheelCentres, next, Math.Min(_wheelCentres.Length, next.Length));
                _wheelCentres = next;
            }
        }

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

        /// <summary>How many purchases have been centred on this spoke. Neighbour spill does
        /// not increment it. The next buy's cost reads this count rather than the derived
        /// Tier, which is why it is saved rather than recomputed.</summary>
        public int WheelCentresOf(WeaponId id, WheelAttr attr)
        {
            if (id == WeaponId.None) return 0;
            EnsureArrays();
            int i = (int)id;
            if (i < 0 || i >= WeaponCatalog.ArraySize) return 0;
            return _wheelCentres[i * WeaponWheel.AttrCount + (int)attr];
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
            int i = (int)id;
            if (i >= 0 && i < WeaponCatalog.ArraySize)
                _wheelCentres[i * WeaponWheel.AttrCount + (int)centre]++;
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
            FillCentresFromPoints(id);
        }

        /// <summary>Replace the counters with an exact points shape — the range adopting a
        /// realm's owned points into a fresh state. Centres derive from the points the way the
        /// flat-Tier shape's do; nothing on the range buys a spoke, so the count only has to
        /// read sanely. Tier stays derived.</summary>
        public void ReplacePoints(WeaponId id, int[] points)
        {
            if (points == null || points.Length != WeaponWheel.AttrCount)
                throw new ArgumentException("points must be length " + WeaponWheel.AttrCount, "points");
            if (id == WeaponId.None) return;
            EnsureArrays();
            int i = (int)id;
            if (i < 0 || i >= WeaponCatalog.ArraySize) return;
            WritePoints(id, points);
            FillCentresFromPoints(id);
        }

        /// <summary>Derive centre counts from points: centre count = floor(points /
        /// FocusAward). Exact after nought or one centred buy; a neighbour sitting at 6 points
        /// after two stacked centres reads as 1. Used for the flat-Tier shape, and for a
        /// document that somehow arrives without the key.</summary>
        void FillCentresFromPoints(WeaponId id)
        {
            if (id == WeaponId.None) return;
            EnsureArrays();
            int i = (int)id;
            if (i < 0 || i >= WeaponCatalog.ArraySize) return;
            int baseIdx = i * WeaponWheel.AttrCount;
            for (int a = 0; a < WeaponWheel.AttrCount; a++)
                _wheelCentres[baseIdx + a] = _wheelPoints[baseIdx + a] / WeaponWheel.FocusAward;
        }

        void FillAllCentresFromPoints()
        {
            EnsureArrays();
            for (int w = 0; w < WeaponCatalog.ArraySize; w++)
                FillCentresFromPoints((WeaponId)w);
        }

        void WritePoints(WeaponId id, int[] pts)
        {
            EnsureArrays();
            int i = (int)id;
            int baseIdx = i * WeaponWheel.AttrCount;
            for (int a = 0; a < WeaponWheel.AttrCount; a++)
                _wheelPoints[baseIdx + a] = pts[a];
        }

        /// <summary>Salvage this Boarder's kit cost to assemble: every owned weapon's catalog
        /// cost plus what its derived Tier took to reach, plus the Decoy when owned. Granted
        /// equipment is deliberately not counted.</summary>
        /// <remarks>
        /// The realm's spend floor no longer includes this term. A floor has to be computable
        /// from one document: a well-equipped Boarder walking into a fresh realm would otherwise
        /// raise that realm's LifetimeSalvageSpent to cover kit the realm never paid for, and
        /// inflate its ledger by the same amount. The value is still worth having — the ARMORY
        /// and the run summary both want to say what a Boarder is carrying — so it lives here,
        /// on the document that actually owns the kit.
        /// </remarks>
        /// <summary>Drop any equipped id this build has no row for.</summary>
        /// <remarks>
        /// The pre-split loader sanitised the drone bay on every load, not only on an old
        /// document — the version gate was never what made it necessary. Losing it with the
        /// migration would have let <c>"EquippedDrone":99</c> reach
        /// <c>PayloadCatalog.Def</c>'s switch, which has no arm for it. The weapon and
        /// equipment slots get the same treatment for the same reason.
        /// </remarks>
        void ValidateEquippedIds()
        {
            if (!PayloadCatalog.IsCatalogued(EquippedDrone))
                EquippedDrone = PayloadId.None;
            if (!EquipmentCatalog.IsCatalogued(EquippedEquipment))
                EquippedEquipment = EquipmentId.None;
            EquippedPrimary = KnownWeaponOr(EquippedPrimary, WeaponSlot.Primary);
            EquippedSecondary = KnownWeaponOr(EquippedSecondary, WeaponSlot.Secondary);
        }

        /// <summary>The weapon if this build catalogues it for that slot, else None.</summary>
        static WeaponId KnownWeaponOr(WeaponId id, WeaponSlot slot)
        {
            if (id == WeaponId.None) return WeaponId.None;
            int i = (int)id;
            if (i < 0 || i >= WeaponCatalog.ArraySize) return WeaponId.None;
            return WeaponCatalog.Def(id).slot == slot ? id : WeaponId.None;
        }

        public long ReconstructSpentOnKit()
        {
            EnsureArrays();
            long spent = 0;
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

        // ---- Known-realm rows ----

        /// <summary>One remembered shared realm: enough to list it and to fetch it.</summary>
        public struct KnownRealm
        {
            public string RealmId;
            public string OwnerId;
            public string Name;
        }

        public int KnownRealmCount { get { return _knownRealmIds.Length; } }

        public KnownRealm KnownRealmAt(int index)
        {
            var k = new KnownRealm();
            if (index < 0 || index >= _knownRealmIds.Length) return k;
            k.RealmId = _knownRealmIds[index];
            k.OwnerId = _knownRealmOwners[index];
            k.Name = _knownRealmNames[index];
            return k;
        }

        /// <summary>Remember a shared realm, or refresh its name. Idempotent — playing in the
        /// same realm twice is one row.</summary>
        public void RememberRealm(string realmId, string ownerId, string name)
        {
            if (string.IsNullOrEmpty(realmId) || string.IsNullOrEmpty(ownerId)) return;
            for (int i = 0; i < _knownRealmIds.Length; i++)
            {
                if (_knownRealmIds[i] != realmId) continue;
                _knownRealmOwners[i] = ownerId;
                _knownRealmNames[i] = name ?? "";
                return;
            }
            int n = _knownRealmIds.Length;
            Array.Resize(ref _knownRealmIds, n + 1);
            Array.Resize(ref _knownRealmOwners, n + 1);
            Array.Resize(ref _knownRealmNames, n + 1);
            _knownRealmIds[n] = realmId;
            _knownRealmOwners[n] = ownerId;
            _knownRealmNames[n] = name ?? "";
        }

        /// <summary>Drop a remembered realm. True when there was a row to drop. Called when
        /// the realm refuses this player, and by the chooser's forget action.</summary>
        public bool ForgetRealm(string realmId)
        {
            if (string.IsNullOrEmpty(realmId)) return false;
            int at = -1;
            for (int i = 0; i < _knownRealmIds.Length; i++)
                if (_knownRealmIds[i] == realmId) { at = i; break; }
            if (at < 0) return false;
            int n = _knownRealmIds.Length - 1;
            var ids = new string[n];
            var owners = new string[n];
            var names = new string[n];
            for (int i = 0, k = 0; i <= n; i++)
            {
                if (i == at) continue;
                ids[k] = _knownRealmIds[i];
                owners[k] = _knownRealmOwners[i];
                names[k] = _knownRealmNames[i];
                k++;
            }
            _knownRealmIds = ids;
            _knownRealmOwners = owners;
            _knownRealmNames = names;
            return true;
        }

        /// <summary>The document as written to disk. Always stamped with the current version —
        /// a live object is by definition current.</summary>
        public string ToJson()
        {
            EnsureArrays();
            var sb = new StringBuilder(256);
            sb.Append('{');
            JsonText.AppendLong(sb, "SaveVersion", SaveFormat.CurrentVersion, false);
            JsonText.AppendString(sb, "Id", Id ?? string.Empty, true);
            JsonText.AppendString(sb, "Name", string.IsNullOrEmpty(Name) ? DefaultName : Name, true);
            JsonText.AppendLong(sb, "EquippedProtocol", (int)EquippedProtocol, true);
            JsonText.AppendLong(sb, "EquippedPassive", (int)EquippedPassive, true);
            JsonText.AppendBool(sb, "EquipmentSlotPurchased", EquipmentSlotPurchased, true);
            JsonText.AppendLong(sb, "OwnedEquipmentMask", OwnedEquipmentMask, true);
            JsonText.AppendLong(sb, "EquippedEquipment", (int)EquippedEquipment, true);
            JsonText.AppendBoolArray(sb, "WeaponsOwned", _weaponsOwned);
            JsonText.AppendIntArray(sb, "WeaponWheelPoints", _wheelPoints);
            JsonText.AppendIntArray(sb, "WeaponWheelCentres", _wheelCentres);
            JsonText.AppendLong(sb, "EquippedPrimary", (int)EquippedPrimary, true);
            JsonText.AppendLong(sb, "EquippedSecondary", (int)EquippedSecondary, true);
            JsonText.AppendLong(sb, "EquippedDrone", (int)EquippedDrone, true);
            JsonText.AppendLong(sb, "OwnedPayloadMask", OwnedPayloadMask, true);
            JsonText.AppendStringArray(sb, "KnownRealmIds", _knownRealmIds);
            JsonText.AppendStringArray(sb, "KnownRealmOwners", _knownRealmOwners);
            JsonText.AppendStringArray(sb, "KnownRealmNames", _knownRealmNames);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Read a Boarder back. Returns <c>null</c> when the document's version is not
        /// supported — there is no migration ladder any more, and the refusal belongs in the
        /// caller's log where a player can see it (<see cref="SaveFormat.RefusalFor"/>). Null or
        /// empty input is a programming error, not a bad save, and still throws.</summary>
        public static BoarderState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("json is null or empty", "json");

            // A missing key reads as 0, which is unsupported — a document with no readable
            // version must be refused rather than assumed current.
            int version = JsonText.ExtractInt(json, "SaveVersion", 0);
            if (!SaveFormat.IsSupported(version)) return null;

            var b = new BoarderState();
            b.Id = JsonText.ExtractString(json, "Id", string.Empty) ?? string.Empty;
            string name = JsonText.ExtractString(json, "Name", DefaultName);
            b.Name = string.IsNullOrEmpty(name) ? DefaultName : name;

            b.EquippedProtocol =
                (BoarderProtocolId)JsonText.ExtractInt(json, "EquippedProtocol", (int)BoarderProtocolId.None);
            b.EquippedPassive =
                (BoarderAbilityId)JsonText.ExtractInt(json, "EquippedPassive", (int)BoarderAbilityId.None);

            b.EquipmentSlotPurchased = JsonText.ExtractBool(json, "EquipmentSlotPurchased", false);
            b.OwnedEquipmentMask = JsonText.ExtractInt(json, "OwnedEquipmentMask", 0);
            b.EquippedEquipment =
                (EquipmentId)JsonText.ExtractInt(json, "EquippedEquipment", (int)EquipmentId.None);

            b.EnsureArrays();
            JsonText.ExtractBoolArray(json, "WeaponsOwned", b._weaponsOwned);
            JsonText.ExtractIntArray(json, "WeaponWheelPoints", b._wheelPoints);
            // Absent only on a hand-edited document — this build always writes the key. Derive
            // rather than zero, or a Boarder would silently have every spoke back at its
            // first-purchase price.
            if (json.IndexOf("\"WeaponWheelCentres\"", StringComparison.Ordinal) >= 0)
                JsonText.ExtractIntArray(json, "WeaponWheelCentres", b._wheelCentres);
            else
                b.FillAllCentresFromPoints();
            b.EquippedPrimary = (WeaponId)JsonText.ExtractInt(json, "EquippedPrimary", (int)WeaponId.None);
            b.EquippedSecondary = (WeaponId)JsonText.ExtractInt(json, "EquippedSecondary", (int)WeaponId.None);

            b.EquippedDrone = (PayloadId)JsonText.ExtractInt(json, "EquippedDrone", (int)PayloadId.None);
            b.OwnedPayloadMask = JsonText.ExtractInt(json, "OwnedPayloadMask", 0);

            b._knownRealmIds = JsonText.ExtractStringArray(json, "KnownRealmIds") ?? Array.Empty<string>();
            b._knownRealmOwners = JsonText.ExtractStringArray(json, "KnownRealmOwners") ?? Array.Empty<string>();
            b._knownRealmNames = JsonText.ExtractStringArray(json, "KnownRealmNames") ?? Array.Empty<string>();
            // The three are parallel or they are nothing: a hand-edited document that broke
            // the alignment must not let row 3's name stand beside row 2's owner.
            if (b._knownRealmOwners.Length != b._knownRealmIds.Length
                || b._knownRealmNames.Length != b._knownRealmIds.Length)
            {
                b._knownRealmIds = Array.Empty<string>();
                b._knownRealmOwners = Array.Empty<string>();
                b._knownRealmNames = Array.Empty<string>();
            }

            // An id this build has no catalog row for — retired, or written by a build that
            // sold more — has to resolve to None here rather than reach a switch with no arm
            // for it. This is not migration and did not go with it: it applies to a document
            // of the CURRENT version just as much, because a hand-edited file or a rolled-back
            // build can put any integer in these fields.
            BoarderProtocol.ValidateEquip(b);
            b.ValidateEquippedIds();
            return b;
        }
    }
}
