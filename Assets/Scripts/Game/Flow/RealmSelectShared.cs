using System.Threading.Tasks;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// The chooser's shared-realm half: the rows a picked Boarder remembers, and the
    /// store-gated ways in and out of them. Its own type because borrowed rows are their own
    /// responsibility — which rows are remote, whose store slot seeds them, and the
    /// open/forget flows — while <see cref="RealmSelectController"/> keeps owning the screen.
    /// </summary>
    public sealed class RealmSelectShared
    {
        const string Tag = "RealmSelect";

        int _localCount;
        string[] _owners = System.Array.Empty<string>();

        /// <summary>What a shared entry attempt came to, for the controller to paint.</summary>
        public struct Outcome
        {
            /// <summary>True when the session left for the Ship — nothing left to paint.</summary>
            public bool Left;
            /// <summary>True when a row was pruned and the rows need rebuilding.</summary>
            public bool RowsChanged;
            public string Status;
        }

        /// <summary>This machine's realms plus the shared realms the given Boarder remembers,
        /// each stamped with which side of co-op it is. The Boarder decides the remote tail
        /// because admission travels with the player, not the install. Null boarder id —
        /// nothing picked — is local only, but the local rows are still stamped: whether your
        /// own realm is open to friends has nothing to do with which Boarder you are taking in.
        /// </summary>
        /// <remarks>
        /// The co-op mark is a fact on the row, not text in the name. It was text once — the
        /// remote rows carried "(shared)" glued into <c>Name</c> — and that name then travelled
        /// into the store's refusals and into the FORGET confirmation, so the player was asked
        /// to forget a realm called "ORION  (shared)". The view composes the label now.
        /// </remarks>
        public RealmRegistry.SaveRow[] BuildRows(RealmRegistry.SaveRow[] local, string boarderId)
        {
            _localCount = local.Length;
            _owners = System.Array.Empty<string>();

            // One sidecar probe per owned realm, per rebuild. That is a small read of a small
            // file and it is the ONLY definition of "shared" there is — a cheaper File.Exists
            // here would be a second answer to the same question, free to drift from the first.
            var rows = new System.Collections.Generic.List<RealmRegistry.SaveRow>(local.Length + 4);
            for (int i = 0; i < local.Length; i++)
            {
                var row = local[i];
                rows.Add(new RealmRegistry.SaveRow(row.Id, row.Name, row.Loadable,
                    RealmShareService.IsShared(row.Id)
                        ? RealmRegistry.CoopRole.Owned
                        : RealmRegistry.CoopRole.None));
            }

            if (string.IsNullOrEmpty(boarderId)) return rows.ToArray();
            var boarder = SaveService.LoadBoarder(boarderId);
            if (boarder == null || boarder.KnownRealmCount == 0) return rows.ToArray();

            var owners = new System.Collections.Generic.List<string>();
            for (int i = 0; i < boarder.KnownRealmCount; i++)
            {
                var known = boarder.KnownRealmAt(i);
                bool alreadyLocal = false;
                for (int k = 0; k < local.Length; k++)
                    if (local[k].Id == known.RealmId) { alreadyLocal = true; break; }
                if (alreadyLocal) continue;
                rows.Add(new RealmRegistry.SaveRow(known.RealmId,
                    string.IsNullOrEmpty(known.Name) ? "REALM" : known.Name, true,
                    RealmRegistry.CoopRole.Guest));
                owners.Add(known.OwnerId);
            }
            _owners = owners.ToArray();
            return rows.ToArray();
        }

        /// <summary>True for a row past the local realms — one entered through the store.</summary>
        public bool IsRemote(int index)
        {
            return index >= _localCount && index - _localCount < _owners.Length;
        }

        /// <summary>Open a remembered realm: fetch, lock, enter, sail — criterion 1's path.
        /// A refused admission prunes the row off the Boarder (criterion 8): the realm's own
        /// list is the authority, this row was only a memory.</summary>
        public async Task<Outcome> Enter(RealmRegistry.SaveRow row, int index, string boarderId)
        {
            string ownerId = _owners[index - _localCount];
            var open = await RealmShareRemote.OpenRemote(row.Id, ownerId, row.Name);
            if (!open.Ok)
            {
                Log.Event(Tag, "shared enter refused — " + open.Refusal);
                if (open.PruneRow) Forget(boarderId, row.Id);
                return new Outcome { RowsChanged = open.PruneRow, Status = open.Refusal };
            }
            if (!GameSession.EnterRemoteRealm(open.Realm, boarderId))
            {
                // The beacon notices a hold with no matching session and releases it.
                return new Outcome { Status = "That realm could not be opened. Nothing was changed." };
            }
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShipCoop())
            {
                Log.Warn(Tag, "shared realm entered but the co-op Ship scene would not load.");
                return new Outcome { Status = "The co-op Ship scene would not load." };
            }
            return new Outcome { Left = true };
        }

        /// <summary>An owned realm with a share sidecar opens under the lock; anything else
        /// has nothing to acquire. Null when the way is clear, else the refusal to show.</summary>
        public static Task<string> AcquireIfShared(string realmId)
        {
            return RealmShareService.IsShared(realmId)
                ? RealmShareService.TryAcquireOwned(realmId)
                : Task.FromResult<string>(null);
        }

        /// <summary>DELETE on a remote row is a two-press FORGET — recoverable, unlike a real
        /// delete, but consistency with the button it shares matters more than the distinction.
        /// True once the row is gone.</summary>
        public bool ArmedForget(RealmRegistry.SaveRow row, string boarderId, ArmedAction armed,
            float now, out string status)
        {
            if (!armed.Fire(row.Id, now))
            {
                status = "Press again to forget \"" + row.Name + "\" — joining a session there brings it back.";
                return false;
            }
            Forget(boarderId, row.Id);
            status = "Forgot \"" + row.Name + "\".";
            return true;
        }

        /// <summary>Drop a remembered realm from a Boarder's document, on disk.</summary>
        public static void Forget(string boarderId, string realmId)
        {
            var boarder = SaveService.LoadBoarder(boarderId);
            if (boarder == null) return;
            if (boarder.ForgetRealm(realmId))
                SaveService.SaveBoarder(boarder);
        }
    }
}
