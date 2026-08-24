using System.Threading.Tasks;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// The open flow for a shared realm: fetch every member's copy, decide, observe a
    /// stale-looking claim, claim, and survive the contest. Used by the chooser for a
    /// borrowed realm and by <see cref="RealmShareService.TryAcquireOwned"/> for an owned
    /// one — the same dance either way, because the lock does not care who the opener is.
    /// </summary>
    public static class RealmShareRemote
    {
        const string Tag = "RealmShare";

        /// <summary>Test seam: the observation wait in milliseconds when non-negative.
        /// Production leaves it at -1 and waits the real <see cref="RealmShare.ConfirmSeconds"/> —
        /// a test against a faked store must not sit through a wall-clock window.</summary>
        public static int ConfirmDelayMsOverride = -1;

        /// <summary>What fetching and deciding produced. Refusal is null when the open may
        /// claim; FreshestEnvelope/Body are set whenever a freshest copy existed.</summary>
        public struct Decision
        {
            public string Refusal;
            /// <summary>True when the refusal means the row itself is dead — the realm is
            /// gone or no longer admits this player — so the caller prunes it (criterion 8).</summary>
            public bool PruneRow;
            public RealmShareEnvelope FreshestEnvelope;
            public string FreshestBody;
        }

        public struct OpenResult
        {
            public bool Ok;
            public RealmState Realm;
            public string Refusal;
            public bool PruneRow;
        }

        /// <summary>Open a realm remembered on the Boarder: the whole dance, ending with the
        /// lock held and the fetched realm returned, or a refusal worth showing.</summary>
        public static async Task<OpenResult> OpenRemote(string realmId, string ownerId, string rowName)
        {
            var store = RealmShareService.Store;
            var signIn = await store.SignIn();
            if (!signIn.Ok)
                return new OpenResult { Refusal = "Shared realms need the store — " + signIn.Error };
            string selfId = store.SelfId;

            var seed = await store.ReadCopyOf(ownerId, realmId);
            if (!seed.Ok)
                return new OpenResult { Refusal = "The store could not be reached. Nothing was changed." };
            if (!seed.Found)
                return new OpenResult
                {
                    Refusal = "\"" + rowName + "\" is not in the store any more.",
                    PruneRow = true
                };
            var seedEnv = RealmShareEnvelope.FromJson(seed.EnvelopeJson);
            if (seedEnv == null)
                return new OpenResult
                { Refusal = "\"" + rowName + "\" was written by a newer build than this one." };

            var d = await FetchAndDecide(store, seedEnv, selfId);
            if (d.Refusal != null)
                return new OpenResult { Refusal = d.Refusal, PruneRow = d.PruneRow };

            // FromJson treats empty input as a programming error and throws; a copy with no
            // body is a refusal here, not a crash.
            RealmState realm = string.IsNullOrEmpty(d.FreshestBody)
                ? null : RealmState.FromJson(d.FreshestBody);
            if (realm == null)
                return new OpenResult
                { Refusal = "\"" + rowName + "\"'s document could not be read. Nothing was changed." };
            realm.Id = realmId;

            string claimRefusal = await ClaimAndConfirm(store, d.FreshestEnvelope,
                d.FreshestBody, isRemote: true);
            if (claimRefusal != null) return new OpenResult { Refusal = claimRefusal };
            return new OpenResult { Ok = true, Realm = realm };
        }

        /// <summary>Fetch every copy the seed envelope names and run the open decision,
        /// including the observation window on a stale-looking claim. Returns a refusal, or
        /// the freshest copy to continue with.</summary>
        public static async Task<Decision> FetchAndDecide(
            IRealmStore store, RealmShareEnvelope seed, string selfId)
        {
            var copies = await FetchCopies(store, seed);
            long now = System.DateTime.UtcNow.Ticks;
            var d = RealmShare.DecideOpen(copies.Envelopes, selfId, now);

            if (d.Verdict == RealmShare.OpenVerdict.ConfirmStale)
            {
                Log.Event(Tag, "claim by " + d.HolderId + " on " + seed.RealmId
                    + " looks stale — observing for " + RealmShare.ConfirmSeconds + "s.");
                await Task.Delay(ConfirmDelayMsOverride >= 0
                    ? ConfirmDelayMsOverride
                    : RealmShare.ConfirmSeconds * 1000);
                copies = await FetchCopies(store, seed);
                d = RealmShare.DecideOpen(copies.Envelopes, selfId,
                    System.DateTime.UtcNow.Ticks, d.WitnessHeartbeatTicks);
            }

            var result = new Decision();
            switch (d.Verdict)
            {
                case RealmShare.OpenVerdict.NotAMember:
                    result.Refusal = d.Refusal;
                    result.PruneRow = true;
                    return result;
                case RealmShare.OpenVerdict.Locked:
                    result.Refusal = d.Refusal;
                    return result;
                case RealmShare.OpenVerdict.ConfirmStale:
                    // A second stale-looking claim appeared between the two looks. Rare and
                    // resolvable by trying again; refusing beats waiting forever.
                    result.Refusal = "Another claim is settling on \"" + seed.RealmName
                        + "\" — try again in a moment.";
                    return result;
            }
            if (!string.IsNullOrEmpty(d.HolderId))
                Log.Event(Tag, "BROKE stale claim by " + d.HolderId + " (\""
                    + d.HolderName + "\") on " + seed.RealmId
                    + " — heartbeat unmoved through the observation window.");
            result.FreshestEnvelope = copies.Envelopes[d.FreshestIndex];
            result.FreshestBody = copies.Bodies[d.FreshestIndex];
            return result;
        }

        /// <summary>Write the claim into this player's own slot, then look again: if a rival
        /// claim was racing this one, exactly one side concedes. Null on success — the hold
        /// is installed — else the refusal.</summary>
        public static async Task<string> ClaimAndConfirm(
            IRealmStore store, RealmShareEnvelope freshest, string bodyJson, bool isRemote)
        {
            if (string.IsNullOrEmpty(bodyJson))
            {
                // A claim uploads envelope and body together; claiming with an empty body
                // would erase the store's copy of the realm the moment the lock was won.
                return "\"" + freshest.RealmName + "\"'s document could not be read. Nothing was changed.";
            }
            string selfId = store.SelfId;
            string selfName = GameSession.Meta != null && GameSession.Meta.Boarder != null
                ? GameSession.Meta.Boarder.Name : "";
            var mine = RealmShare.Claim(freshest, selfId, selfName, System.DateTime.UtcNow.Ticks);
            var write = await store.WriteOwnCopy(mine.RealmId, mine.ToJson(), bodyJson);
            if (!write.Ok)
                return "The store refused the claim (" + write.Error + "). Nothing was changed.";

            var copies = await FetchCopies(store, mine);
            if (RealmShare.LosesContest(copies.Envelopes, selfId, System.DateTime.UtcNow.Ticks))
            {
                RealmShare.Concede(mine);
                await store.WriteOwnCopy(mine.RealmId, mine.ToJson(), bodyJson);
                Log.Event(Tag, "conceded a simultaneous claim on " + mine.RealmId);
                return "\"" + mine.RealmName + "\" was opened by someone else at the same "
                    + "moment — they got it. Try again later.";
            }
            RealmShareService.InstallHold(mine, isRemote);
            return null;
        }

        struct Copies
        {
            public RealmShareEnvelope[] Envelopes;
            public string[] Bodies;
        }

        static async Task<Copies> FetchCopies(IRealmStore store, RealmShareEnvelope seed)
        {
            string[] who = RealmShare.ReadSet(seed);
            var envelopes = new RealmShareEnvelope[who.Length];
            var bodies = new string[who.Length];
            for (int i = 0; i < who.Length; i++)
            {
                var read = await store.ReadCopyOf(who[i], seed.RealmId);
                if (!read.Ok || !read.Found) continue;
                envelopes[i] = RealmShareEnvelope.FromJson(read.EnvelopeJson);
                bodies[i] = read.BodyJson;
            }
            return new Copies { Envelopes = envelopes, Bodies = bodies };
        }
    }
}
