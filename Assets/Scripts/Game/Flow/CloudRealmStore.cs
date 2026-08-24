using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
// The package ships two SaveOptions/DeleteOptions; the Data.Player ones carry the
// access-class argument, which is the entire point of this adapter.
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using DeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// The real store: Cloud Save player data in the PUBLIC access class. Public means
    /// readable by any signed-in player who can name the owner and the key, and writable
    /// only by the owner — which is why the whole sharing model is per-member copies
    /// (Docs/Milestones — a realm you can open while its owner is offline).
    /// </summary>
    /// <remarks>
    /// Every failure is caught and returned as a result with the reason — a store call sits
    /// on menu paths and the way out of a session, where an exception would cost more than
    /// the write it reports. Sign-in is not attempted here: callers go through
    /// <see cref="RelayJoin.EnsureServices"/> first, so there is one sign-in path.
    /// </remarks>
    public sealed class CloudRealmStore : IRealmStore
    {
        const string Tag = "RealmShare";

        public string SelfId { get { return RelayJoin.SignedInPlayerId; } }

        public async Task<RealmStoreWrite> SignIn()
        {
            var init = await RelayJoin.EnsureServices();
            return init.Ok
                ? new RealmStoreWrite { Ok = true }
                : new RealmStoreWrite { Ok = false, Error = init.Error };
        }

        public async Task<RealmStoreWrite> WriteOwnCopy(string realmId, string envelopeJson, string bodyJson)
        {
            try
            {
                var batch = new Dictionary<string, object>
                {
                    { RealmShare.EnvelopeKey(realmId), envelopeJson },
                    { RealmShare.BodyKey(realmId), bodyJson }
                };
                await CloudSaveService.Instance.Data.Player.SaveAsync(
                    batch, new SaveOptions(new PublicWriteAccessClassOptions()));
                return new RealmStoreWrite { Ok = true };
            }
            catch (Exception e)
            {
                return Failed("write", realmId, e);
            }
        }

        public async Task<RealmStoreRead> ReadCopyOf(string playerId, string realmId)
        {
            try
            {
                string envKey = RealmShare.EnvelopeKey(realmId);
                string bodyKey = RealmShare.BodyKey(realmId);
                var keys = new HashSet<string> { envKey, bodyKey };
                var options = playerId == SelfId
                    ? new LoadOptions(new PublicReadAccessClassOptions())
                    : new LoadOptions(new PublicReadAccessClassOptions(playerId));
                var items = await CloudSaveService.Instance.Data.Player.LoadAsync(keys, options);

                var read = new RealmStoreRead { Ok = true };
                if (items != null && items.TryGetValue(envKey, out var env) && env != null)
                {
                    read.EnvelopeJson = env.Value.GetAs<string>();
                    read.Found = !string.IsNullOrEmpty(read.EnvelopeJson);
                }
                if (items != null && items.TryGetValue(bodyKey, out var body) && body != null)
                    read.BodyJson = body.Value.GetAs<string>();
                return read;
            }
            catch (Exception e)
            {
                Log.Warn(Tag, "store read failed for " + realmId + " of " + playerId
                    + " (" + e.GetType().Name + ": " + e.Message + ")");
                return new RealmStoreRead { Ok = false, Error = e.Message };
            }
        }

        public async Task<RealmStoreWrite> DeleteOwnCopy(string realmId)
        {
            try
            {
                var options = new DeleteOptions(new PublicWriteAccessClassOptions());
                await CloudSaveService.Instance.Data.Player.DeleteAsync(
                    RealmShare.EnvelopeKey(realmId), options);
                await CloudSaveService.Instance.Data.Player.DeleteAsync(
                    RealmShare.BodyKey(realmId), options);
                return new RealmStoreWrite { Ok = true };
            }
            catch (Exception e)
            {
                return Failed("delete", realmId, e);
            }
        }

        static RealmStoreWrite Failed(string verb, string realmId, Exception e)
        {
            Log.Warn(Tag, "store " + verb + " failed for " + realmId
                + " (" + e.GetType().Name + ": " + e.Message + ")");
            return new RealmStoreWrite { Ok = false, Error = e.Message };
        }
    }
}
