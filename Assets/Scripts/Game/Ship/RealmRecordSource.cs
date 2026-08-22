using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Game
{
    /// <summary>READ-ONLY seam: the realm numbers THIS machine should display. It never
    /// writes and never mutates a save.</summary>
    /// <remarks>
    /// A realm stays with the world; a Boarder travels. So a guest standing on a host's ship
    /// carries its own <c>GameSession.Meta.Realm</c> — the realm it is playing in is not on
    /// its machine at all. Reading the local realm there renders somebody else's deck with
    /// your own numbers: your cleared-wave cap on their map, your pot on their buy-back.
    /// Two branches, the same shape as <see cref="ShipHatchAssignment.Recompute"/>:
    ///   * no live session (solo) — the local realm IS the realm being played;
    ///   * party lead — same, the host's realm is the one everyone is in;
    ///   * guest — the host-written mirror, which is the only copy on this machine.
    /// The host side of the wire is <see cref="RealmRecordPublisher"/>.
    /// </remarks>
    public static class RealmRecordSource
    {
        /// <summary>The record to render. A guest before the host's first publish reads a
        /// fresh realm (packed 0), never a realm at wave zero.</summary>
        public static RealmRecord Current()
        {
            var session = NetSession.FindLive();
            if (session == null || !session.Active) return LocalRecord();
            if (!session.IsPartyLead) return GuestRecord(session);
            return LocalRecord();
        }

        /// <summary>Banked salvage of the realm being played — the pot a buy-back actually
        /// spends from. Not part of the packed record (it moves every bank window, the record
        /// does not), so a guest reads <see cref="NetRunMirror.Banked"/>, which the host
        /// writes each frame for the whole of a live run — the only window buy-back is
        /// offered in.</summary>
        public static long Banked()
        {
            var session = NetSession.FindLive();
            if (session == null || !session.Active) return LocalBanked();
            if (!session.IsPartyLead) return GuestBanked(session);
            return LocalBanked();
        }

        static RealmRecord LocalRecord()
        {
            var meta = GameSession.Meta;
            return RealmRecord.From(meta != null ? meta.Realm : null);
        }

        static RealmRecord GuestRecord(NetSession session)
        {
            var mirror = session.Mirror;
            if (mirror == null) return RealmRecord.Fresh();
            return RealmRecord.Unpack(mirror.RealmRecordPacked.Value);
        }

        static long LocalBanked()
        {
            var meta = GameSession.Meta;
            return meta != null ? meta.BankedSalvage : 0L;
        }

        static long GuestBanked(NetSession session)
        {
            var mirror = session.Mirror;
            if (mirror == null) return 0L;
            return mirror.Banked.Value;
        }
    }
}
