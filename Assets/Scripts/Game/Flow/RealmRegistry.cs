using System;
using System.Collections.Generic;
using System.IO;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// What is on disk, listed cheaply, plus the minting of new Boarders and realms.
    /// </summary>
    /// <remarks>
    /// Identity is this layer's job and never the Sim's: a document's id is a short minted
    /// string that is safe to put in a file name, and it is emphatically NOT the display name.
    /// A player may call a realm anything at all, including something that would steer a write
    /// out of the save directory, so the name never reaches a path.
    ///
    /// Listing reads the two identifying keys out of the raw text rather than deserializing a
    /// whole document. A menu that lists ten realms should not pay for ten full loads, and a
    /// document this build would refuse still has a readable name — the list can show it and
    /// say why it cannot be entered, which is friendlier than hiding it.
    /// </remarks>
    public static class RealmRegistry
    {
        const string Tag = "RealmRegistry";
        const string LastRealmKey = "gkb.last_realm";
        const string LastBoarderKey = "gkb.last_boarder";
        const int IdLength = 12;
        const int MaxNameLength = 24;

        /// <summary>Whose co-op a row is part of. Two states rather than one flag because a
        /// realm you own and shared and a realm someone shared with you are not the same row:
        /// the first is a document on this disk that DELETE destroys, the second is a memory
        /// on the Boarder that DELETE only forgets. One "shared" mark on both would say the
        /// two are interchangeable, and the wrong one of them is unrecoverable.</summary>
        public enum CoopRole
        {
            /// <summary>Solo — no share sidecar, and not a borrowed row.</summary>
            None = 0,
            /// <summary>This machine's realm, opened to friends.</summary>
            Owned = 1,
            /// <summary>Someone else's realm this Boarder was admitted to.</summary>
            Guest = 2
        }

        /// <summary>One line in a chooser: which document it is, and what to call it.</summary>
        public struct SaveRow
        {
            public readonly string Id;
            public readonly string Name;
            /// <summary>False when this build refuses the document's version. The row still
            /// lists — a realm you cannot enter is better shown with a reason than hidden.</summary>
            public readonly bool Loadable;
            /// <summary>Whether co-op is on for this row, and from which side. Decided by the
            /// chooser, never read off the document — a realm's own JSON knows nothing about
            /// sharing, which lives in a sidecar beside it and on the Boarder.</summary>
            public readonly CoopRole Coop;

            public SaveRow(string id, string name, bool loadable)
                : this(id, name, loadable, CoopRole.None)
            {
            }

            public SaveRow(string id, string name, bool loadable, CoopRole coop)
            {
                Id = id;
                Name = name;
                Loadable = loadable;
                Coop = coop;
            }
        }

        // ---- Listing ----

        /// <summary>Every realm on disk. Never null; empty when there are none.</summary>
        public static SaveRow[] Realms()
        {
            return RowsFrom(SaveService.RealmFiles(), "REALM");
        }

        /// <summary>Every Boarder on disk. Never null; empty when there are none.</summary>
        public static SaveRow[] Boarders()
        {
            return RowsFrom(SaveService.BoarderFiles(), "BOARDER");
        }

        static SaveRow[] RowsFrom(string[] paths, string fallbackName)
        {
            if (paths == null || paths.Length == 0) return Array.Empty<SaveRow>();
            var rows = new List<SaveRow>(paths.Length);
            for (int i = 0; i < paths.Length; i++)
            {
                string id = Path.GetFileNameWithoutExtension(paths[i]);
                if (!SaveService.IsIdSafe(id))
                {
                    Log.Warn(Tag, "Skipping " + Path.GetFileName(paths[i]) + " — its name is not a usable id.");
                    continue;
                }
                string json = TryRead(paths[i]);
                if (json == null) continue;
                string name = JsonText.ExtractString(json, "Name", null);
                if (string.IsNullOrEmpty(name)) name = fallbackName;
                rows.Add(new SaveRow(id, name, SaveFormat.IsSupported(SaveService.VersionIn(json))));
            }
            return rows.ToArray();
        }

        static string TryRead(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Log.Warn(Tag, "Could not read " + Path.GetFileName(path) + " ("
                    + e.GetType().Name + ": " + e.Message + "); it is not listed.");
                return null;
            }
        }

        // ---- Minting ----

        /// <summary>Create and write a realm nobody has played yet.</summary>
        public static RealmState CreateRealm(string name)
        {
            bool written;
            return CreateRealm(name, out written);
        }

        /// <summary>As above, reporting whether the document actually reached disk. A caller
        /// that is about to treat this as the player's campaign needs to know.</summary>
        public static RealmState CreateRealm(string name, out bool written)
        {
            var realm = RealmState.CreateNew(MintId(), CleanName(name, "REALM"));
            written = SaveService.SaveRealm(realm);
            Log.Event("Ship", "realm created — \"" + realm.Name + "\"");
            return realm;
        }

        /// <summary>Create and write a Boarder carrying nothing.</summary>
        public static BoarderState CreateBoarder(string name)
        {
            bool written;
            return CreateBoarder(name, out written);
        }

        /// <summary>As above, reporting whether the document actually reached disk.</summary>
        public static BoarderState CreateBoarder(string name, out bool written)
        {
            var boarder = BoarderState.CreateNew(MintId(), CleanName(name, "BOARDER"));
            written = SaveService.SaveBoarder(boarder);
            Log.Event("Ship", "Boarder created — \"" + boarder.Name + "\"");
            return boarder;
        }

        /// <summary>A short id that is safe as a file name and will not collide in practice.
        /// Minted here rather than in Sim, which has no business knowing about files.</summary>
        static string MintId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, IdLength);
        }

        /// <summary>What the player typed, made fit to display: trimmed, length-capped, and with
        /// control characters dropped. Nothing here is about path safety — the name never becomes
        /// a path. An empty or all-whitespace name falls back rather than being refused, because
        /// refusing a name at a "name your realm" prompt is a worse experience than defaulting.</summary>
        public static string CleanName(string raw, string fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            var sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length && sb.Length < MaxNameLength; i++)
            {
                char c = raw[i];
                if (c < ' ') continue;
                sb.Append(c);
            }
            string cleaned = sb.ToString().Trim();
            return cleaned.Length == 0 ? fallback : cleaned;
        }

        // ---- Which pair was last played ----

        /// <summary>Id of the realm entered last, or empty. Kept in PlayerPrefs beside the other
        /// machine-local preferences: which world you were last in is a property of this
        /// installation, not of any save document, and it must not travel with a Boarder.</summary>
        public static string LastRealmId
        {
            get { return PlayerPrefs.GetString(LastRealmKey, ""); }
            set { PlayerPrefs.SetString(LastRealmKey, value == null ? "" : value); PlayerPrefs.Save(); }
        }

        /// <summary>Id of the Boarder taken in last, or empty.</summary>
        public static string LastBoarderId
        {
            get { return PlayerPrefs.GetString(LastBoarderKey, ""); }
            set { PlayerPrefs.SetString(LastBoarderKey, value == null ? "" : value); PlayerPrefs.Save(); }
        }

        // ---- Deleting ----

        /// <summary>Take a realm out of play. The document is moved, not unlinked
        /// (<see cref="SaveService.RetireRealm"/>), so a mispress on a chooser is recoverable
        /// by hand rather than final.</summary>
        /// <remarks>
        /// The last-played pointer is cleared when it named this realm. Leaving it would make
        /// CONTINUE resolve to a document that is no longer there, and the resolve falls back
        /// to whatever is first on disk — so pressing CONTINUE would silently open somebody
        /// else's world. That reads as the save being swapped, which is worse than being told
        /// there is nothing to continue.
        /// </remarks>
        public static bool DeleteRealm(string id)
        {
            if (!SaveService.RetireRealm(id)) return false;
            if (LastRealmId == id) LastRealmId = "";
            Log.Event(Tag, "realm deleted id=" + id);
            return true;
        }

        /// <summary>Take a Boarder out of play. The kit dies with it; realms are untouched,
        /// which is the whole point of the two documents being separate.</summary>
        public static bool DeleteBoarder(string id)
        {
            if (!SaveService.RetireBoarder(id)) return false;
            if (LastBoarderId == id) LastBoarderId = "";
            Log.Event(Tag, "Boarder deleted id=" + id);
            return true;
        }

        /// <summary>True when there is a pair this build can actually open.</summary>
        /// <remarks>
        /// Counting files is not enough. A directory full of documents from an older build is
        /// not a campaign this build can continue, and offering CONTINUE for it means pressing
        /// it silently starts a new game — which reads to a player exactly like their save
        /// being deleted. That is the failure the refusal exists to prevent, so the check has
        /// to be "loadable", not "present".
        /// </remarks>
        public static bool HasLoadablePair()
        {
            return AnyLoadable(Realms()) && AnyLoadable(Boarders());
        }

        static bool AnyLoadable(SaveRow[] rows)
        {
            if (rows == null) return false;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Loadable) return true;
            return false;
        }

        /// <summary>Say once, per document, why something on disk cannot be opened. Called
        /// when a continue was expected and could not happen — the player is owed the reason
        /// at the moment it matters, not silence.</summary>
        public static void ReportUnloadable()
        {
            ReportUnloadable(Realms(), SaveService.RealmsDirectory, "Realm");
            ReportUnloadable(Boarders(), SaveService.BoardersDirectory, "Boarder");
        }

        static void ReportUnloadable(SaveRow[] rows, string directory, string kind)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].Loadable) continue;
                string json = TryRead(System.IO.Path.Combine(directory, rows[i].Id + ".json"));
                int version = json == null ? 0 : SaveService.VersionIn(json);
                Log.Warn(Tag, kind + " \"" + rows[i].Name + "\" (" + rows[i].Id + ".json): "
                    + SaveFormat.RefusalFor(version) + ".");
            }
        }

        /// <summary>The pair to continue into: whatever was played last, falling back to the
        /// first of each that is actually loadable. Either may come back null, which the caller
        /// reads as "there is nothing to continue" rather than as an error.</summary>
        public static void ResolveContinuePair(out string realmId, out string boarderId)
        {
            realmId = FirstUsable(Realms(), LastRealmId);
            boarderId = FirstUsable(Boarders(), LastBoarderId);
        }

        static string FirstUsable(SaveRow[] rows, string preferred)
        {
            if (rows == null || rows.Length == 0) return null;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Loadable && rows[i].Id == preferred) return rows[i].Id;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Loadable) return rows[i].Id;
            return null;
        }
    }
}
