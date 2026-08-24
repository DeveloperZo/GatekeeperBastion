using System.Text;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// One member's copy of a shared realm's sharing state: who owns it, who it admits,
    /// how far its history has advanced, and whether this copy carries a claim on it.
    /// </summary>
    /// <remarks>
    /// The store only lets a player write their OWN data, so a shared realm exists as one
    /// copy per member, each self-written. This envelope is the part of a copy that the
    /// sharing rules read; the realm body travels beside it as a second key. A reader
    /// fetches every member's copy and <see cref="RealmShare.PickFreshest"/> says which one
    /// is the realm — the lock keeps writers single-file, so revisions are a chain and the
    /// copies never need merging (Docs/Milestones — a realm you can open while its owner is
    /// offline).
    /// </remarks>
    public sealed class RealmShareEnvelope
    {
        public const int CurrentVersion = 1;

        public string RealmId = "";
        public string RealmName = "";
        public string OwnerId = "";

        /// <summary>Admitted players beside the owner. Parallel to <see cref="MemberNames"/>.</summary>
        public string[] MemberIds = System.Array.Empty<string>();

        /// <summary>Display names for refusals and logs. Never identity.</summary>
        public string[] MemberNames = System.Array.Empty<string>();

        /// <summary>How far this copy's history has advanced. Bumped on release, never on
        /// heartbeat — a heartbeat refreshes the same open session, not a new chapter.</summary>
        public long Revision;

        /// <summary>Player id holding the realm open, or empty when this copy carries no claim.</summary>
        public string LockHolderId = "";
        public string LockHolderName = "";
        public long LockTakenUtcTicks;
        public long LockHeartbeatUtcTicks;

        public bool HasLock { get { return !string.IsNullOrEmpty(LockHolderId); } }

        public RealmShareEnvelope Clone()
        {
            var c = new RealmShareEnvelope
            {
                RealmId = RealmId,
                RealmName = RealmName,
                OwnerId = OwnerId,
                MemberIds = (string[])MemberIds.Clone(),
                MemberNames = (string[])MemberNames.Clone(),
                Revision = Revision,
                LockHolderId = LockHolderId,
                LockHolderName = LockHolderName,
                LockTakenUtcTicks = LockTakenUtcTicks,
                LockHeartbeatUtcTicks = LockHeartbeatUtcTicks
            };
            return c;
        }

        public string ToJson()
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            JsonText.AppendLong(sb, "ShareVersion", CurrentVersion, false);
            JsonText.AppendString(sb, "RealmId", RealmId ?? "", true);
            JsonText.AppendString(sb, "RealmName", RealmName ?? "", true);
            JsonText.AppendString(sb, "OwnerId", OwnerId ?? "", true);
            JsonText.AppendStringArray(sb, "MemberIds", MemberIds);
            JsonText.AppendStringArray(sb, "MemberNames", MemberNames);
            JsonText.AppendLong(sb, "Revision", Revision, true);
            JsonText.AppendString(sb, "LockHolderId", LockHolderId ?? "", true);
            JsonText.AppendString(sb, "LockHolderName", LockHolderName ?? "", true);
            JsonText.AppendLong(sb, "LockTakenUtcTicks", LockTakenUtcTicks, true);
            JsonText.AppendLong(sb, "LockHeartbeatUtcTicks", LockHeartbeatUtcTicks, true);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Read a copy back, or null when it is missing, empty, or written by a
        /// newer build than this one. Null is a copy that does not exist for the rules.</summary>
        public static RealmShareEnvelope FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            long version = JsonText.ExtractLong(json, "ShareVersion", 0);
            if (version < 1 || version > CurrentVersion) return null;
            var e = new RealmShareEnvelope();
            e.RealmId = JsonText.ExtractString(json, "RealmId", "") ?? "";
            e.RealmName = JsonText.ExtractString(json, "RealmName", "") ?? "";
            e.OwnerId = JsonText.ExtractString(json, "OwnerId", "") ?? "";
            e.MemberIds = JsonText.ExtractStringArray(json, "MemberIds") ?? System.Array.Empty<string>();
            e.MemberNames = JsonText.ExtractStringArray(json, "MemberNames") ?? System.Array.Empty<string>();
            if (e.MemberNames.Length != e.MemberIds.Length)
            {
                // Names are display only; identity must never depend on them lining up.
                var names = new string[e.MemberIds.Length];
                for (int i = 0; i < names.Length; i++)
                    names[i] = i < e.MemberNames.Length ? e.MemberNames[i] : "";
                e.MemberNames = names;
            }
            e.Revision = JsonText.ExtractLong(json, "Revision", 0);
            e.LockHolderId = JsonText.ExtractString(json, "LockHolderId", "") ?? "";
            e.LockHolderName = JsonText.ExtractString(json, "LockHolderName", "") ?? "";
            e.LockTakenUtcTicks = JsonText.ExtractLong(json, "LockTakenUtcTicks", 0);
            e.LockHeartbeatUtcTicks = JsonText.ExtractLong(json, "LockHeartbeatUtcTicks", 0);
            return e;
        }
    }

    /// <summary>
    /// The sharing rules: who may open a shared realm, which copy is the realm, what makes
    /// a claim stale, and who backs off when two members claim at once. Pure decisions —
    /// the clock is handed in as UTC ticks and the store lives in the game layer.
    /// </summary>
    public static class RealmShare
    {
        /// <summary>How often a holder rewrites its copy. Each beat also carries the live
        /// realm body, so this is the write-back cadence too.</summary>
        public const int HeartbeatSeconds = 45;

        /// <summary>A claim this long past its last beat only LOOKS stale — it must be
        /// confirmed by observation before it may be walked past, so another machine's
        /// clock cannot break a live session's lock.</summary>
        public const int StaleSeconds = 180;

        /// <summary>How long an opener watches a stale-looking claim before believing it.
        /// A live holder beats every <see cref="HeartbeatSeconds"/>, far inside the
        /// stale window, so an unchanged heartbeat across this wait means the process
        /// behind it is gone.</summary>
        public const int ConfirmSeconds = 10;

        const long TicksPerSecond = 10000000L;

        /// <summary>Store key of the envelope half of a member's copy.</summary>
        public static string EnvelopeKey(string realmId) { return "gkr_env_" + realmId; }

        /// <summary>Store key of the realm body half of a member's copy.</summary>
        public static string BodyKey(string realmId) { return "gkr_body_" + realmId; }

        public enum OpenVerdict
        {
            /// <summary>Free to claim.</summary>
            Open,
            /// <summary>The freshest copy does not admit this player.</summary>
            NotAMember,
            /// <summary>A live claim exists and it is not this player's.</summary>
            Locked,
            /// <summary>Only stale-looking claims stand in the way — observe them for
            /// <see cref="ConfirmSeconds"/> and decide again with the first look as witness.</summary>
            ConfirmStale
        }

        public struct OpenDecision
        {
            public OpenVerdict Verdict;
            /// <summary>Index into the copies of the one that is the realm. -1 when none exist.</summary>
            public int FreshestIndex;
            /// <summary>Holder of the blocking or displaced claim, for refusals and logs.</summary>
            public string HolderId;
            public string HolderName;
            /// <summary>Last-seen heartbeat of a stale-looking claim; the witness a second
            /// look compares against.</summary>
            public long WitnessHeartbeatTicks;
            /// <summary>The one line the player is shown when the verdict refuses.</summary>
            public string Refusal;
        }

        /// <summary>True when this player may read the realm at all: the owner always, and
        /// anyone the copy lists.</summary>
        public static bool Admits(RealmShareEnvelope env, string playerId)
        {
            if (env == null || string.IsNullOrEmpty(playerId)) return false;
            if (env.OwnerId == playerId) return true;
            for (int i = 0; i < env.MemberIds.Length; i++)
                if (env.MemberIds[i] == playerId) return true;
            return false;
        }

        /// <summary>Admit a player, or refresh their display name. Idempotent.</summary>
        public static void AddMember(RealmShareEnvelope env, string playerId, string name)
        {
            if (env == null || string.IsNullOrEmpty(playerId)) return;
            if (playerId == env.OwnerId) return;
            for (int i = 0; i < env.MemberIds.Length; i++)
            {
                if (env.MemberIds[i] != playerId) continue;
                env.MemberNames[i] = name ?? "";
                return;
            }
            int n = env.MemberIds.Length;
            var ids = new string[n + 1];
            var names = new string[n + 1];
            System.Array.Copy(env.MemberIds, ids, n);
            System.Array.Copy(env.MemberNames, names, n);
            ids[n] = playerId;
            names[n] = name ?? "";
            env.MemberIds = ids;
            env.MemberNames = names;
        }

        /// <summary>Remove a player from the admitted list. True when they were on it.</summary>
        public static bool RemoveMember(RealmShareEnvelope env, string playerId)
        {
            if (env == null || string.IsNullOrEmpty(playerId)) return false;
            int at = -1;
            for (int i = 0; i < env.MemberIds.Length; i++)
                if (env.MemberIds[i] == playerId) { at = i; break; }
            if (at < 0) return false;
            int n = env.MemberIds.Length - 1;
            var ids = new string[n];
            var names = new string[n];
            for (int i = 0, k = 0; i <= n; i++)
            {
                if (i == at) continue;
                ids[k] = env.MemberIds[i];
                names[k] = env.MemberNames[i];
                k++;
            }
            env.MemberIds = ids;
            env.MemberNames = names;
            return true;
        }

        /// <summary>Everyone whose copy a reader must fetch: the owner plus every member the
        /// given copy names. The list is read off a copy because there is nowhere else it
        /// could live.</summary>
        public static string[] ReadSet(RealmShareEnvelope env)
        {
            if (env == null) return System.Array.Empty<string>();
            var set = new string[env.MemberIds.Length + 1];
            set[0] = env.OwnerId;
            System.Array.Copy(env.MemberIds, 0, set, 1, env.MemberIds.Length);
            return set;
        }

        /// <summary>Which copy is the realm: highest revision, then freshest heartbeat —
        /// a crashed holder's copy carries the latest body even though its revision never
        /// closed. -1 when there are no copies.</summary>
        public static int PickFreshest(RealmShareEnvelope[] copies)
        {
            if (copies == null) return -1;
            int best = -1;
            for (int i = 0; i < copies.Length; i++)
            {
                if (copies[i] == null) continue;
                if (best < 0) { best = i; continue; }
                var b = copies[best];
                var c = copies[i];
                if (c.Revision > b.Revision
                    || (c.Revision == b.Revision && c.LockHeartbeatUtcTicks > b.LockHeartbeatUtcTicks))
                    best = i;
            }
            return best;
        }

        /// <summary>A claim is live when its last beat is within the stale window. A beat
        /// FROM THE FUTURE is live too — that is the other machine's clock running ahead,
        /// not a stale claim.</summary>
        public static bool ClaimLooksLive(RealmShareEnvelope env, long nowUtcTicks)
        {
            if (env == null || !env.HasLock) return false;
            long age = nowUtcTicks - env.LockHeartbeatUtcTicks;
            return age < StaleSeconds * TicksPerSecond;
        }

        /// <summary>Decide whether <paramref name="selfId"/> may open the realm these copies
        /// describe. Pass the previous decision's witness on the second look at a
        /// stale-looking claim: a claim whose heartbeat has not moved since the witness is
        /// dead and may be walked past; one that moved is alive after all.</summary>
        public static OpenDecision DecideOpen(
            RealmShareEnvelope[] copies, string selfId, long nowUtcTicks,
            long witnessHeartbeatTicks = -1L)
        {
            var d = new OpenDecision { FreshestIndex = PickFreshest(copies), WitnessHeartbeatTicks = -1L };
            if (d.FreshestIndex < 0)
            {
                d.Verdict = OpenVerdict.NotAMember;
                d.Refusal = "That realm is not in the store any more.";
                return d;
            }
            var freshest = copies[d.FreshestIndex];
            if (!Admits(freshest, selfId))
            {
                d.Verdict = OpenVerdict.NotAMember;
                d.Refusal = "\"" + freshest.RealmName + "\" does not admit this player any more.";
                return d;
            }

            // The lock question is asked of EVERY copy, not just the freshest: a new holder
            // writes its claim into its own copy, which may still carry a lower revision.
            int staleAt = -1;
            for (int i = 0; i < copies.Length; i++)
            {
                var c = copies[i];
                if (c == null || !c.HasLock || c.LockHolderId == selfId) continue;
                if (ClaimLooksLive(c, nowUtcTicks))
                {
                    d.Verdict = OpenVerdict.Locked;
                    d.HolderId = c.LockHolderId;
                    d.HolderName = c.LockHolderName;
                    d.Refusal = "\"" + freshest.RealmName + "\" is open right now by "
                        + DisplayName(c.LockHolderName) + ".";
                    return d;
                }
                // Stale-looking. With a witness that matches, it is dead; without one, or
                // with a beat that moved, it needs (another) observation window.
                if (witnessHeartbeatTicks >= 0 && c.LockHeartbeatUtcTicks == witnessHeartbeatTicks)
                {
                    d.HolderId = c.LockHolderId;
                    d.HolderName = c.LockHolderName;
                    continue; // confirmed dead — a break, reported via HolderId for the log
                }
                if (staleAt < 0 || c.LockHeartbeatUtcTicks > copies[staleAt].LockHeartbeatUtcTicks)
                    staleAt = i;
            }
            if (staleAt >= 0)
            {
                var s = copies[staleAt];
                d.Verdict = OpenVerdict.ConfirmStale;
                d.HolderId = s.LockHolderId;
                d.HolderName = s.LockHolderName;
                d.WitnessHeartbeatTicks = s.LockHeartbeatUtcTicks;
                d.Refusal = "";
                return d;
            }
            d.Verdict = OpenVerdict.Open;
            d.Refusal = "";
            return d;
        }

        /// <summary>This player's copy for an open session: the freshest copy's history and
        /// roster, carrying this player's claim. Written to their own slot — the one slot
        /// they can write.</summary>
        public static RealmShareEnvelope Claim(
            RealmShareEnvelope freshest, string selfId, string selfName, long nowUtcTicks)
        {
            var mine = freshest.Clone();
            mine.LockHolderId = selfId ?? "";
            mine.LockHolderName = selfName ?? "";
            mine.LockTakenUtcTicks = nowUtcTicks;
            mine.LockHeartbeatUtcTicks = nowUtcTicks;
            return mine;
        }

        /// <summary>Refresh a held claim. Revision does not move — see the field's remarks.</summary>
        public static void Beat(RealmShareEnvelope mine, long nowUtcTicks)
        {
            if (mine == null || !mine.HasLock) return;
            mine.LockHeartbeatUtcTicks = nowUtcTicks;
        }

        /// <summary>Close the session: the claim comes off and the history advances one
        /// revision, which is what makes this copy the freshest until somebody opens again.</summary>
        public static void Release(RealmShareEnvelope mine)
        {
            if (mine == null) return;
            mine.LockHolderId = "";
            mine.LockHolderName = "";
            mine.LockTakenUtcTicks = 0;
            mine.LockHeartbeatUtcTicks = 0;
            mine.Revision++;
        }

        /// <summary>Back out of a claim that lost its contest. The claim comes off and the
        /// revision does NOT move — this session never happened.</summary>
        public static void Concede(RealmShareEnvelope mine)
        {
            if (mine == null) return;
            mine.LockHolderId = "";
            mine.LockHolderName = "";
            mine.LockTakenUtcTicks = 0;
            mine.LockHeartbeatUtcTicks = 0;
        }

        /// <summary>After claiming and uploading, look again: if another live claim exists,
        /// exactly one side concedes. The earlier claim wins; a dead-even tie falls to the
        /// lower player id. Both sides run this same rule on the same two numbers, so they
        /// cannot both proceed.</summary>
        public static bool LosesContest(
            RealmShareEnvelope[] copies, string selfId, long nowUtcTicks)
        {
            if (copies == null || string.IsNullOrEmpty(selfId)) return false;
            RealmShareEnvelope mine = null;
            for (int i = 0; i < copies.Length; i++)
                if (copies[i] != null && copies[i].LockHolderId == selfId) { mine = copies[i]; break; }
            if (mine == null) return false;
            for (int i = 0; i < copies.Length; i++)
            {
                var c = copies[i];
                if (c == null || !c.HasLock || c.LockHolderId == selfId) continue;
                if (!ClaimLooksLive(c, nowUtcTicks)) continue;
                if (c.LockTakenUtcTicks < mine.LockTakenUtcTicks) return true;
                if (c.LockTakenUtcTicks == mine.LockTakenUtcTicks
                    && string.CompareOrdinal(c.LockHolderId, selfId) < 0) return true;
            }
            return false;
        }

        static string DisplayName(string name)
        {
            return string.IsNullOrEmpty(name) ? "another member" : name;
        }
    }
}
