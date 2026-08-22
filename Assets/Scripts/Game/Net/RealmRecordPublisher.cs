using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY command layer for the realm record: packs the realm the party is
    /// actually in and writes it to the run mirror, so every seat can read the realm being
    /// played instead of the one in its own save. Called at the two moments the record can
    /// have moved — Ship load and the end of a run. Solo has nothing to replicate; a guest
    /// never writes, and every refusal says why. Same host/guest split as
    /// <see cref="ShipHatchAssignment"/>; the read side is
    /// <see cref="RealmRecordSource"/>.</summary>
    public static class RealmRecordPublisher
    {
        const string Tag = "Realm";

        /// <summary>Publish this machine's realm record to every seat.</summary>
        public static void Publish(string reason)
        {
            var session = NetSession.FindLive();
            if (session == null || !session.Active)
            {
                Log.Event(Tag, "publish skipped — solo, nothing to replicate (" + reason + ")");
                return;
            }
            if (!session.IsPartyLead)
            {
                Log.Event(Tag, "publish refused — not party lead (" + reason + ")");
                return;
            }
            var mirror = session.Mirror;
            if (mirror != null && !mirror.IsSpawned)
            {
                // Writing into an unspawned mirror is a silent no-op downstream, so saying
                // "published" here would hide an ordering regression rather than surface it.
                Log.Warn(Tag, "publish skipped — the mirror is not spawned yet (" + reason + ")");
                return;
            }
            if (mirror == null)
            {
                Log.Warn(Tag, "publish — no mirror to replicate (" + reason + ")");
                return;
            }
            var meta = GameSession.Meta;
            RealmRecord record = RealmRecord.From(meta != null ? meta.Realm : null);
            mirror.ServerSetRealmRecord(record);
            Log.Event(Tag, "publish reason=" + reason
                + " cleared=" + record.CleanExits
                + " lost=" + record.RunsLost
                + " deepest=" + record.DeepestWave
                + " armor=" + record.HatchArmor);
        }
    }
}
