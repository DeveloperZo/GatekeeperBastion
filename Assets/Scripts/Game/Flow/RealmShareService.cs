using System.Threading.Tasks;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// LOCAL-OWNER: this machine's side of a shared realm — holding the lock, admitting
    /// guests, heartbeating the live body into the store, and releasing on close. The rules
    /// live in <see cref="RealmShare"/>; the remote-open flow lives in
    /// <see cref="RealmShareRemote"/>; this owns the held state between them.
    /// </summary>
    /// <remarks>
    /// The store is swappable so the flows are testable without a network; production uses
    /// <see cref="CloudRealmStore"/>. Every refusal is returned as the line the player sees
    /// and logged with the detail, and a failed store write is warned about, never swallowed
    /// (Docs/Milestones — a realm you can open while its owner is offline, criterion 10).
    /// </remarks>
    public static class RealmShareService
    {
        const string Tag = "RealmShare";

        static IRealmStore _store = new CloudRealmStore();
        static RealmShareEnvelope _held;
        static string _heldRealmId = "";
        static bool _heldIsRemote;

        /// <summary>Test seam. Production never calls it.</summary>
        public static void UseStore(IRealmStore store)
        {
            _store = store != null ? store : new CloudRealmStore();
            _held = null;
            _heldRealmId = "";
            _heldIsRemote = false;
        }

        public static IRealmStore Store { get { return _store; } }

        /// <summary>True while this machine holds a shared realm's lock.</summary>
        public static bool Holding { get { return _held != null; } }

        public static string HeldRealmId { get { return _heldRealmId; } }

        /// <summary>The held realm's OWNER in the store — the slot readers seed from. Not
        /// necessarily this machine: a member can hold a borrowed realm open.</summary>
        public static string HeldOwnerId { get { return _held != null ? _held.OwnerId : ""; } }

        /// <summary>Called by the open flows once the claim survived its contest. Spawns the
        /// beacon that heartbeats and releases.</summary>
        internal static void InstallHold(RealmShareEnvelope mine, bool isRemote)
        {
            _held = mine;
            _heldRealmId = mine != null ? mine.RealmId : "";
            _heldIsRemote = isRemote;
            if (mine != null) RealmShareBeacon.Ensure();
        }

        /// <summary>An OWNED realm is shared exactly when it has a sidecar. The chooser asks
        /// this before deciding whether ENTER needs the lock.</summary>
        public static bool IsShared(string realmId)
        {
            return SaveService.LoadShareSidecar(realmId) != null;
        }

        /// <summary>Take the lock on an owned, shared realm BEFORE entering it. Returns null
        /// on success — the lock is held and the local realm file carries the freshest body —
        /// or the refusal the player is shown. A realm with no sidecar is not shared and
        /// succeeds with nothing to do.</summary>
        public static async Task<string> TryAcquireOwned(string realmId)
        {
            string sidecarJson = SaveService.LoadShareSidecar(realmId);
            if (sidecarJson == null) return null;
            var sidecar = RealmShareEnvelope.FromJson(sidecarJson);
            if (sidecar == null)
            {
                Log.Warn(Tag, "share sidecar for " + realmId + " is unreadable; treating the realm as unshared.");
                return null;
            }

            var signIn = await _store.SignIn();
            if (!signIn.Ok)
                return "\"" + sidecar.RealmName + "\" is shared, so opening it needs the store — " + signIn.Error;

            var open = await RealmShareRemote.FetchAndDecide(_store, sidecar, _store.SelfId);
            if (open.Refusal != null) return open.Refusal;

            // The store may know a newer chapter than this machine — a member played here
            // while we were away. The freshest body becomes the local file, under the lock.
            if (open.FreshestEnvelope.Revision > sidecar.Revision && open.FreshestBody != null)
            {
                var fetched = RealmState.FromJson(open.FreshestBody);
                if (fetched != null)
                {
                    fetched.Id = realmId;
                    SaveService.SaveRealm(fetched);
                    Log.Event(Tag, "realm " + realmId + " advanced in the store (rev "
                        + sidecar.Revision + " -> " + open.FreshestEnvelope.Revision
                        + ") — the fetched body is now the local file.");
                }
            }

            var local = SaveService.LoadRealm(realmId);
            if (local == null)
                return "\"" + sidecar.RealmName + "\" could not be opened. Nothing was changed.";
            return await RealmShareRemote.ClaimAndConfirm(
                _store, open.FreshestEnvelope, local.ToJson(), isRemote: false);
        }

        /// <summary>Make the CURRENT session's realm shared (first share happens here) and
        /// take the lock, called when hosting over Relay. Returns null on success, else the
        /// one-line reason sharing did not happen — the session itself may still proceed.</summary>
        public static async Task<string> EnsureSharedForHost()
        {
            if (Holding) return null;
            var meta = GameSession.Meta;
            if (meta == null || meta.Realm == null) return "No realm is open to share.";
            if (GameSession.CurrentRealmIsRemote)
            {
                // A borrowed realm is already held under its own lock by the remote open.
                return null;
            }

            var signIn = await _store.SignIn();
            if (!signIn.Ok) return "Sharing is off this session — " + signIn.Error;

            string realmId = meta.Realm.Id;
            string sidecarJson = SaveService.LoadShareSidecar(realmId);
            if (sidecarJson != null)
            {
                // Already shared: the lock dance happened in TryAcquireOwned before entry.
                // Being here without holding means that dance was skipped — refuse loudly
                // rather than fork.
                Log.Warn(Tag, "EnsureSharedForHost on shared realm " + realmId + " without the lock.");
                return "\"" + meta.Realm.Name + "\" is shared — reopen it from the chooser.";
            }

            var env = new RealmShareEnvelope
            {
                RealmId = realmId,
                RealmName = meta.Realm.Name,
                OwnerId = _store.SelfId,
                Revision = 1
            };
            var mine = RealmShare.Claim(env, _store.SelfId, meta.Boarder != null ? meta.Boarder.Name : "",
                System.DateTime.UtcNow.Ticks);
            var write = await _store.WriteOwnCopy(realmId, mine.ToJson(), meta.Realm.ToJson());
            if (!write.Ok) return "Sharing is off this session — the store write failed (" + write.Error + ").";
            SaveService.SaveShareSidecar(realmId, mine.ToJson());
            InstallHold(mine, isRemote: false);
            Log.Event(Tag, "realm " + realmId + " is now SHARED, owner " + _store.SelfId);
            return null;
        }

        /// <summary>Host side, on a guest connecting to a shared session: the guest becomes a
        /// member and the roster change is uploaded. Quietly does nothing when the session's
        /// realm is not shared (LAN, or sharing failed) — admission to nothing is nothing.</summary>
        public static async void AdmitGuest(string playerId, string playerName)
        {
            if (!Holding || _heldIsRemote) return;
            if (string.IsNullOrEmpty(playerId) || playerId == _held.OwnerId) return;
            if (RealmShare.Admits(_held, playerId))
            {
                RealmShare.AddMember(_held, playerId, playerName); // refresh the name
                return;
            }
            RealmShare.AddMember(_held, playerId, playerName);
            Log.Event(Tag, "admitted " + playerId + " (\"" + playerName + "\") to realm " + _heldRealmId);
            await UploadHeld("admit");
        }

        /// <summary>One heartbeat: refresh the claim and carry the live body with it.</summary>
        public static async Task Beat()
        {
            if (!Holding) return;
            RealmShare.Beat(_held, System.DateTime.UtcNow.Ticks);
            await UploadHeld("beat");
        }

        /// <summary>Close the session: release the lock, advance the chapter, upload, and
        /// forget the held state. Safe to call when nothing is held.</summary>
        public static async Task CloseAndRelease()
        {
            if (!Holding) return;
            var mine = _held;
            string realmId = _heldRealmId;
            bool remote = _heldIsRemote;
            _held = null;
            _heldRealmId = "";
            _heldIsRemote = false;

            RealmShare.Release(mine);
            string body = BodyForUpload(realmId, remote);
            if (string.IsNullOrEmpty(body))
            {
                // Uploading an empty body would erase the store's copy of the realm. The
                // claim is left to go stale instead, which the next opener may break.
                Log.Warn(Tag, "release for " + realmId + " has no body to carry — nothing "
                    + "uploaded; the claim will read as stale after " + RealmShare.StaleSeconds + "s.");
                return;
            }
            var write = await _store.WriteOwnCopy(realmId, mine.ToJson(), body);
            if (!write.Ok)
                Log.Warn(Tag, "release upload FAILED for " + realmId + " (" + write.Error
                    + ") — the claim will read as stale after " + RealmShare.StaleSeconds
                    + "s and the last banked progress may be behind the store.");
            else
                Log.Event(Tag, "released realm " + realmId + " at rev " + mine.Revision);
            if (!remote) SaveService.SaveShareSidecar(realmId, mine.ToJson());
        }

        /// <summary>Guest side: remember the realm a joined session put us in, so the chooser
        /// can offer it later. No-ops on LAN (no owner id crosses) and on our own realm.</summary>
        public static void RememberJoinedRealm(string realmId, string ownerId, string realmName)
        {
            if (string.IsNullOrEmpty(realmId) || string.IsNullOrEmpty(ownerId)) return;
            if (ownerId == _store.SelfId) return;
            var meta = GameSession.Meta;
            if (meta == null || meta.Boarder == null) return;
            meta.Boarder.RememberRealm(realmId, ownerId, realmName);
            GameSession.SaveBoarder();
            Log.Event(Tag, "remembered shared realm \"" + realmName + "\" (" + realmId
                + ") of " + ownerId);
        }

        static async Task UploadHeld(string reason)
        {
            var mine = _held;
            if (mine == null) return;
            string body = BodyForUpload(mine.RealmId, _heldIsRemote);
            if (string.IsNullOrEmpty(body))
            {
                Log.Warn(Tag, "upload on " + reason + " for " + mine.RealmId
                    + " has no body to carry; nothing written.");
                return;
            }
            var write = await _store.WriteOwnCopy(mine.RealmId, mine.ToJson(), body);
            if (!write.Ok)
                Log.Warn(Tag, "store upload failed on " + reason + " for " + mine.RealmId
                    + " (" + write.Error + ") — will retry on the next beat.");
            else if (!_heldIsRemote)
                SaveService.SaveShareSidecar(mine.RealmId, mine.ToJson());
        }

        /// <summary>The body a beat or release carries: the LIVE realm when this session still
        /// has it open, else the working copy on disk.</summary>
        static string BodyForUpload(string realmId, bool remote)
        {
            var meta = GameSession.Meta;
            if (meta != null && meta.Realm != null && GameSession.CurrentRealmId == realmId)
                return meta.Realm.ToJson();
            var fromDisk = remote ? SaveService.LoadRemoteRealm(realmId) : SaveService.LoadRealm(realmId);
            return fromDisk != null ? fromDisk.ToJson() : "";
        }
    }
}
