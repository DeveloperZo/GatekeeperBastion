using System.Threading.Tasks;

namespace GatekeepersProtocol.Game
{
    /// <summary>One member's copy in the remote store, as fetched: the envelope half and the
    /// realm body half, or the fact that the player has no copy.</summary>
    public struct RealmStoreRead
    {
        /// <summary>False when the store could not be asked at all — network, sign-in, service.
        /// Distinct from a player simply having no copy, which is Ok with Found false.</summary>
        public bool Ok;
        public bool Found;
        public string EnvelopeJson;
        public string BodyJson;
        public string Error;
    }

    public struct RealmStoreWrite
    {
        public bool Ok;
        public string Error;
    }

    /// <summary>
    /// The store seam for shared realms. The real store is Cloud Save's public access class,
    /// whose one rule shapes everything above it: a player writes ONLY their own slot, and
    /// anybody may read a named player's slot. There is deliberately no "write to another
    /// player" call here, so nothing above the seam can be written to assume one.
    /// </summary>
    public interface IRealmStore
    {
        /// <summary>This machine's signed-in player id, or empty before sign-in.</summary>
        string SelfId { get; }

        /// <summary>Make <see cref="SelfId"/> real: initialize services and sign in. Every
        /// flow calls this before its first read or write, so the store owns its own
        /// authentication and a faked store simply owns a fake one.</summary>
        Task<RealmStoreWrite> SignIn();

        /// <summary>Write this player's copy: envelope and body under the realm's two keys,
        /// in one batch.</summary>
        Task<RealmStoreWrite> WriteOwnCopy(string realmId, string envelopeJson, string bodyJson);

        /// <summary>Read the named player's copy of the realm. Their slot, our read.</summary>
        Task<RealmStoreRead> ReadCopyOf(string playerId, string realmId);

        /// <summary>Remove this player's copy entirely (delete of an owned realm).</summary>
        Task<RealmStoreWrite> DeleteOwnCopy(string realmId);
    }
}
