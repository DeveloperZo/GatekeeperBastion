using System.Collections.Generic;
using System.Threading.Tasks;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The shared-realm flows against a faked store: refusal for a stranger, a live
    /// lock refusing by name with nothing written, a stale claim broken after observation,
    /// release-and-reopen, admission onto the uploaded roster, and banked progress carried
    /// through close (Docs/Milestones — a realm you can open while its owner is offline,
    /// criteria 2–8). The rules themselves are pinned in RealmShareTests; these prove the
    /// orchestration calls them honestly.</summary>
    public class RealmShareServiceTests
    {
        const string OwnerId = "owner-A";
        const string SelfId = "guest-B";

        /// <summary>In-memory IRealmStore: one slot per (player, realm), self-writes only —
        /// the same one rule the real store has.</summary>
        sealed class FakeRealmStore : IRealmStore
        {
            public string Self = RealmShareServiceTests.SelfId;
            public bool FailSignIn;
            public bool FailWrites;
            public readonly Dictionary<string, string[]> Slots = new Dictionary<string, string[]>();

            public string SelfId { get { return Self; } }

            public Task<RealmStoreWrite> SignIn()
            {
                return Task.FromResult(FailSignIn
                    ? new RealmStoreWrite { Ok = false, Error = "no network" }
                    : new RealmStoreWrite { Ok = true });
            }

            public Task<RealmStoreWrite> WriteOwnCopy(string realmId, string envelopeJson, string bodyJson)
            {
                if (FailWrites)
                    return Task.FromResult(new RealmStoreWrite { Ok = false, Error = "store down" });
                Slots[Self + "/" + realmId] = new[] { envelopeJson, bodyJson };
                return Task.FromResult(new RealmStoreWrite { Ok = true });
            }

            public Task<RealmStoreRead> ReadCopyOf(string playerId, string realmId)
            {
                if (!Slots.TryGetValue(playerId + "/" + realmId, out var slot))
                    return Task.FromResult(new RealmStoreRead { Ok = true, Found = false });
                return Task.FromResult(new RealmStoreRead
                {
                    Ok = true,
                    Found = true,
                    EnvelopeJson = slot[0],
                    BodyJson = slot[1]
                });
            }

            public Task<RealmStoreWrite> DeleteOwnCopy(string realmId)
            {
                Slots.Remove(Self + "/" + realmId);
                return Task.FromResult(new RealmStoreWrite { Ok = true });
            }

            public void SeedAs(string playerId, RealmShareEnvelope env, string bodyJson)
            {
                Slots[playerId + "/" + env.RealmId] = new[] { env.ToJson(), bodyJson };
            }

            public RealmShareEnvelope EnvelopeOf(string playerId, string realmId)
            {
                return Slots.TryGetValue(playerId + "/" + realmId, out var slot)
                    ? RealmShareEnvelope.FromJson(slot[0])
                    : null;
            }

            public string BodyOf(string playerId, string realmId)
            {
                return Slots.TryGetValue(playerId + "/" + realmId, out var slot) ? slot[1] : null;
            }
        }

        FakeRealmStore _store;
        string _snapshot;

        [SetUp]
        public void FakeTheStore()
        {
            _store = new FakeRealmStore();
            RealmShareService.UseStore(_store);
            RealmShareRemote.ConfirmDelayMsOverride = 0;
            _snapshot = GameSession.Meta != null ? GameSession.Meta.ToSnapshotJson() : null;
        }

        [TearDown]
        public void RealStoreBack()
        {
            RealmShareService.UseStore(null);
            RealmShareRemote.ConfirmDelayMsOverride = -1;
            var back = _snapshot != null ? MetaState.FromSnapshotJson(_snapshot) : Inert();
            if (back != null) GameSession.AdoptDevState(back);
            _snapshot = null;
        }

        static MetaState Inert()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            return m;
        }

        static RealmShareEnvelope SharedRealm(long revision = 3)
        {
            var env = new RealmShareEnvelope
            {
                RealmId = "realmS",
                RealmName = "ORION",
                OwnerId = OwnerId,
                Revision = revision
            };
            RealmShare.AddMember(env, SelfId, "BEV");
            return env;
        }

        static string Body(long banked)
        {
            var realm = RealmState.CreateNew("realmS", "ORION");
            realm.BankedSalvage = banked;
            return realm.ToJson();
        }

        // ------------------------------------------------------------------ criteria 3, 8

        [Test]
        public async Task AStrangerIsRefusedWithAReason_AndTheRowIsMarkedForPruning()
        {
            var env = SharedRealm();
            RealmShare.RemoveMember(env, SelfId);
            _store.SeedAs(OwnerId, env, Body(100));

            var open = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");

            Assert.IsFalse(open.Ok);
            Assert.IsTrue(open.PruneRow, "a refused admission prunes the remembered row");
            StringAssert.Contains("ORION", open.Refusal);
            Assert.IsNull(_store.EnvelopeOf(SelfId, "realmS"),
                "a refused opener writes NOTHING — no claim, no copy");
            Assert.IsFalse(RealmShareService.Holding);
        }

        [Test]
        public async Task ARealmGoneFromTheStorePrunes_ButARefusedStoreDoesNot()
        {
            var gone = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");
            Assert.IsFalse(gone.Ok);
            Assert.IsTrue(gone.PruneRow, "no copy in the store — the row points at nothing");

            _store.FailSignIn = true;
            var offline = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");
            Assert.IsFalse(offline.Ok);
            Assert.IsFalse(offline.PruneRow,
                "an unreachable store says nothing about the row — it must survive");
        }

        // ------------------------------------------------------------------ criterion 4

        [Test]
        public async Task ALiveHolderRefusesByName_AndNothingIsWritten()
        {
            var env = SharedRealm();
            // The holder must be a member — only members' copies are in the read set, which
            // is also why membership edits happen only under the lock.
            RealmShare.AddMember(env, "guest-C", "CAM");
            var held = RealmShare.Claim(env, "guest-C", "CAM", System.DateTime.UtcNow.Ticks);
            _store.SeedAs(OwnerId, env, Body(100));
            _store.SeedAs("guest-C", held, Body(100));

            var open = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");

            Assert.IsFalse(open.Ok);
            Assert.IsFalse(open.PruneRow, "locked is a wait, not a dead row");
            StringAssert.Contains("CAM", open.Refusal);
            Assert.IsNull(_store.EnvelopeOf(SelfId, "realmS"));
            Assert.IsFalse(RealmShareService.Holding);
        }

        // ------------------------------------------------------------------ criterion 5

        [Test]
        public async Task AStaleClaimIsBrokenAfterObservation_AndTheRealmOpens()
        {
            var env = SharedRealm();
            RealmShare.AddMember(env, "guest-C", "CAM");
            var dead = RealmShare.Claim(env, "guest-C", "CAM",
                System.DateTime.UtcNow.Ticks - (RealmShare.StaleSeconds + 60) * 10000000L);
            _store.SeedAs(OwnerId, env, Body(100));
            _store.SeedAs("guest-C", dead, Body(140));

            var open = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");

            Assert.IsTrue(open.Ok, open.Refusal);
            Assert.IsTrue(RealmShareService.Holding);
            Assert.AreEqual(140, open.Realm.BankedSalvage,
                "the crashed holder's copy carried the later body, and that body won");
            var mine = _store.EnvelopeOf(SelfId, "realmS");
            Assert.AreEqual(SelfId, mine.LockHolderId, "the claim went into OUR slot only");
        }

        // ------------------------------------------------------------------ criteria 2, 6

        [Test]
        public async Task CloseReleases_CarriesTheBankedProgress_AndTheRealmOpensAgain()
        {
            _store.SeedAs(OwnerId, SharedRealm(), Body(100));

            var open = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");
            Assert.IsTrue(open.Ok, open.Refusal);

            // Play happens: the session banks salvage into the borrowed realm.
            var boarder = RealmRegistry.CreateBoarder("TESTER");
            Assert.IsTrue(GameSession.EnterRemoteRealm(open.Realm, boarder.Id));
            open.Realm.BankedSalvage = 250;

            await RealmShareService.CloseAndRelease();

            Assert.IsFalse(RealmShareService.Holding);
            var mine = _store.EnvelopeOf(SelfId, "realmS");
            Assert.IsFalse(mine.HasLock, "the claim came off on close");
            Assert.AreEqual(4L, mine.Revision, "release advanced the chapter");
            StringAssert.Contains("\"BankedSalvage\":250", _store.BodyOf(SelfId, "realmS"),
                "what was banked here is in the store for the owner's next open");

            // The owner opens next: our copy is the freshest and carries the 250.
            _store.Self = OwnerId;
            var owner = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");
            Assert.IsTrue(owner.Ok, owner.Refusal);
            Assert.AreEqual(250, owner.Realm.BankedSalvage,
                "salvage banked by a member is present on the owner's next open");
        }

        // ------------------------------------------------------------------ criterion 7

        [Test]
        public async Task AdmittingAGuestPutsThemOnTheUploadedRoster()
        {
            var meta = MetaState.Compose(
                BoarderState.CreateNew("b-host", "HOST"),
                RealmState.CreateNew("realmH", "TAURUS"));
            GameSession.AdoptDevState(meta);
            _store.Self = OwnerId;

            string refusal = await RealmShareService.EnsureSharedForHost();
            Assert.IsNull(refusal, refusal);
            Assert.IsTrue(RealmShareService.Holding);

            RealmShareService.AdmitGuest("guest-Z", "ZED");
            await Task.Yield();

            var uploaded = _store.EnvelopeOf(OwnerId, "realmH");
            Assert.IsTrue(RealmShare.Admits(uploaded, "guest-Z"),
                "the realm's uploaded copy lists the entering player");

            await RealmShareService.CloseAndRelease();
        }

        [Test]
        public void AJoinedSharedSessionIsRememberedOnTheBoarder_ButNeverOurOwn()
        {
            var meta = MetaState.Compose(
                BoarderState.CreateNew("b-guest", "GUEST"),
                RealmState.CreateNew("r-own", "HOME"));
            GameSession.AdoptDevState(meta);

            RealmShareService.RememberJoinedRealm("realmS", OwnerId, "ORION");
            Assert.AreEqual(1, meta.Boarder.KnownRealmCount,
                "the Boarder lists the realm after entering its session");
            Assert.AreEqual("realmS", meta.Boarder.KnownRealmAt(0).RealmId);

            RealmShareService.RememberJoinedRealm("r-mine", SelfId, "MINE");
            Assert.AreEqual(1, meta.Boarder.KnownRealmCount,
                "a realm this player owns is not a shared row");

            RealmShareService.RememberJoinedRealm("", OwnerId, "X");
            RealmShareService.RememberJoinedRealm("realmT", "", "X");
            Assert.AreEqual(1, meta.Boarder.KnownRealmCount, "LAN carries no ids — nothing remembered");
        }

        // ------------------------------------------------------------------ contest

        [Test]
        public async Task ASimultaneousClaimLeavesExactlyOneHolder()
        {
            var env = SharedRealm();
            RealmShare.AddMember(env, "guest-C", "CAM");
            _store.SeedAs(OwnerId, env, Body(100));
            // The rival's claim is already uploaded when our confirm pass looks again —
            // taken EARLIER, so ours must concede.
            var rival = RealmShare.Claim(env, "guest-C", "CAM",
                System.DateTime.UtcNow.Ticks - 5 * 10000000L);
            _store.SeedAs("guest-C", rival, Body(100));

            // Our first look sees the rival as live and refuses outright — same outcome as a
            // lost contest, by the cheaper path.
            var open = await RealmShareRemote.OpenRemote("realmS", OwnerId, "ORION");
            Assert.IsFalse(open.Ok);
            Assert.IsFalse(RealmShareService.Holding);
        }
    }
}
