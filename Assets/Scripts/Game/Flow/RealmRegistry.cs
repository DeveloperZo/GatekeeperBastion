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

        /// <summary>One line in a chooser: which document it is, and what to call it.</summary>
        public struct SaveRow
        {
            public readonly string Id;
            public readonly string Name;
            /// <summary>False when this build refuses the document's version. The row still
            /// lists — a realm you cannot enter is better shown with a reason than hidden.</summary>
            public readonly bool Loadable;

            public SaveRow(string id, string name, bool loadable)
            {
                Id = id;
                Name = name;
                Loadable = loadable;
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
            var realm = RealmState.CreateNew(MintId(), CleanName(name, "REALM"));
            SaveService.SaveRealm(realm);
            Log.Event("Ship", "realm created — \"" + realm.Name + "\"");
            return realm;
        }

        /// <summary>Create and write a Boarder carrying nothing.</summary>
        public static BoarderState CreateBoarder(string name)
        {
            var boarder = BoarderState.CreateNew(MintId(), CleanName(name, "BOARDER"));
            SaveService.SaveBoarder(boarder);
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
