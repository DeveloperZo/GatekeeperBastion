using System;
using System.IO;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// The file layer for the two save documents. Boarders live at
    /// <c>SaveRoot/boarders/&lt;id&gt;.json</c>, realms at <c>SaveRoot/realms/&lt;id&gt;.json</c>,
    /// and the two are written independently — a guest saves its own Boarder while the realm it
    /// is playing in belongs to somebody else's machine.
    /// </summary>
    /// <remarks>
    /// Nothing outside this file touches the save directory except <see cref="RealmRegistry"/>,
    /// which enumerates it, and <see cref="GameSession"/>, which owns the live pair. Every refusal
    /// here says why in the log and names the file: a save that silently fails to load is
    /// indistinguishable from a save that was deleted.
    ///
    /// Errors are logged, never thrown. These calls sit on the death path and on the way out of a
    /// run, where an exception costs the player the session on top of the write.
    /// </remarks>
    public static class SaveService
    {
        const string Tag = "SaveService";

        /// <summary>The single-document save this build no longer reads. Left in place, never
        /// deleted — it is the player's old campaign, and it is not ours to throw away.</summary>
        const string LegacyFileName = "gatekeeper_meta.json";

        const string RootFolder = "gkb_save";
        const string BoardersFolder = "boarders";
        const string RealmsFolder = "realms";

        /// <summary>Longest id this layer will build a path from. Ids are minted short by
        /// <see cref="RealmRegistry"/>; anything longer arrived from somewhere it should not have.</summary>
        const int MaxIdLength = 64;

        /// <summary>Latches once the legacy save has been reported, so the refusal is a single
        /// plain line rather than one per menu open.</summary>
        static bool _legacyReported;

        /// <summary>The directory both document kinds live under.</summary>
        public static string SaveRoot { get { return Path.Combine(Application.persistentDataPath, RootFolder); } }

        /// <summary>Where Boarder documents are kept.</summary>
        public static string BoardersDirectory { get { return Path.Combine(SaveRoot, BoardersFolder); } }

        /// <summary>Where realm documents are kept.</summary>
        public static string RealmsDirectory { get { return Path.Combine(SaveRoot, RealmsFolder); } }

        /// <summary>Path of the pre-split single-document save. It is never read and never
        /// written any more, but the name STAYS: the PlayMode save guard resolves this member by
        /// reflection to back the developer's campaign up around a run of the suite. Renaming it
        /// does not fail the build — it fails silently, and then the suite eats the campaign.</summary>
        public static string SavePath { get { return Path.Combine(Application.persistentDataPath, LegacyFileName); } }

        /// <summary>Clears the once-only legacy report at play-mode start, so a developer who
        /// fixes the situation and comes back does not have to restart the editor to see it.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _legacyReported = false;
        }

        // ---- Paths and identity ----

        /// <summary>True when an id may be turned into a file name. The id is minted in the game
        /// layer and is never the display name — a realm called <c>"../saves"</c> must not be able
        /// to steer a write out of the save directory.</summary>
        public static bool IsIdSafe(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id.Length > MaxIdLength) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        /// <summary>Full path of a Boarder document, or null when the id is not usable.</summary>
        public static string BoarderPath(string id)
        {
            if (!IsIdSafe(id))
            {
                Log.Warn(Tag, "Boarder id \"" + id + "\" is not a usable file name; no path built.");
                return null;
            }
            return Path.Combine(BoardersDirectory, id + ".json");
        }

        /// <summary>Full path of a realm document, or null when the id is not usable.</summary>
        public static string RealmPath(string id)
        {
            if (!IsIdSafe(id))
            {
                Log.Warn(Tag, "Realm id \"" + id + "\" is not a usable file name; no path built.");
                return null;
            }
            return Path.Combine(RealmsDirectory, id + ".json");
        }

        /// <summary>Every Boarder document on disk, oldest name first. Empty on any IO trouble.</summary>
        public static string[] BoarderFiles() { return FilesIn(BoardersDirectory); }

        /// <summary>Every realm document on disk. Empty on any IO trouble.</summary>
        public static string[] RealmFiles() { return FilesIn(RealmsDirectory); }

        static string[] FilesIn(string directory)
        {
            try
            {
                if (!Directory.Exists(directory)) return Array.Empty<string>();
                return Directory.GetFiles(directory, "*.json");
            }
            catch (Exception e)
            {
                Log.Warn(Tag, "Could not list " + directory + " (" + e.GetType().Name + ": " + e.Message
                    + "); treating it as empty.");
                return Array.Empty<string>();
            }
        }

        /// <summary>True when there is a session to continue: at least one Boarder AND at least
        /// one realm. Half a pair is not a campaign — a Boarder with no world to stand in cannot
        /// be entered, so the boot path treats it as nothing on disk and starts fresh.</summary>
        public static bool Exists()
        {
            ReportLegacySaveOnce();
            return BoarderFiles().Length > 0 && RealmFiles().Length > 0;
        }

        // ---- Writes: independent by construction ----

        /// <summary>Write one Boarder. Requires nothing of the realm — a guest owns its Boarder
        /// even while the world around it is the host's.</summary>
        public static void SaveBoarder(BoarderState boarder)
        {
            if (boarder == null)
            {
                Log.Warn(Tag, "SaveBoarder called with no Boarder; nothing written.");
                return;
            }
            string path = BoarderPath(boarder.Id);
            if (path == null)
            {
                Log.Error(Tag, "Boarder \"" + boarder.Name + "\" has no usable id; refusing to write it.");
                return;
            }
            WriteDocument(path, boarder.ToJson(), "Boarder");
        }

        /// <summary>Write one realm. Requires nothing of the Boarder — only the host of a world
        /// writes that world, and it writes it whether or not anyone's kit changed.</summary>
        public static void SaveRealm(RealmState realm)
        {
            if (realm == null)
            {
                Log.Warn(Tag, "SaveRealm called with no realm; nothing written.");
                return;
            }
            string path = RealmPath(realm.Id);
            if (path == null)
            {
                Log.Error(Tag, "Realm \"" + realm.Name + "\" has no usable id; refusing to write it.");
                return;
            }
            WriteDocument(path, realm.ToJson(), "Realm");
        }

        static void WriteDocument(string path, string json, string kind)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(path, json);
                Log.Info(Tag, kind + " saved to " + path + ".");
            }
            catch (Exception e)
            {
                Log.Error(Tag, "Failed to save " + kind.ToLowerInvariant() + " to " + path
                    + " (" + e.GetType().Name + ": " + e.Message + ").");
            }
        }

        // ---- Reads: null on refusal, with the reason and the file name in the log ----

        /// <summary>Read one Boarder, or null when it is missing, unreadable, or written by a
        /// version this build refuses. The refusal is logged with the file name.</summary>
        public static BoarderState LoadBoarder(string id)
        {
            string path = BoarderPath(id);
            if (path == null) return null;
            string json = ReadDocument(path, "Boarder");
            if (json == null) return null;
            try
            {
                var boarder = BoarderState.FromJson(json);
                if (boarder == null)
                {
                    LogRefusal(path, json);
                    return null;
                }
                // The file IS the identity. A document written without its id key still belongs
                // to the name it was found under, and putting it back keeps the next write in place.
                if (string.IsNullOrEmpty(boarder.Id)) boarder.Id = id;
                return boarder;
            }
            catch (Exception e)
            {
                Log.Error(Tag, "Boarder " + Path.GetFileName(path) + " could not be read ("
                    + e.GetType().Name + ": " + e.Message + "); it was not loaded.");
                return null;
            }
        }

        /// <summary>Read one realm, or null when it is missing, unreadable, or written by a
        /// version this build refuses. The refusal is logged with the file name.</summary>
        public static RealmState LoadRealm(string id)
        {
            string path = RealmPath(id);
            if (path == null) return null;
            string json = ReadDocument(path, "Realm");
            if (json == null) return null;
            try
            {
                var realm = RealmState.FromJson(json);
                if (realm == null)
                {
                    LogRefusal(path, json);
                    return null;
                }
                if (string.IsNullOrEmpty(realm.Id)) realm.Id = id;
                return realm;
            }
            catch (Exception e)
            {
                Log.Error(Tag, "Realm " + Path.GetFileName(path) + " could not be read ("
                    + e.GetType().Name + ": " + e.Message + "); it was not loaded.");
                return null;
            }
        }

        static string ReadDocument(string path, string kind)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Log.Warn(Tag, kind + " " + Path.GetFileName(path) + " is not on disk; nothing loaded.");
                    ReportLegacySaveOnce();
                    return null;
                }
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                {
                    Log.Warn(Tag, kind + " " + Path.GetFileName(path) + " is empty; nothing loaded.");
                    return null;
                }
                return json;
            }
            catch (Exception e)
            {
                Log.Error(Tag, "Could not read " + path + " (" + e.GetType().Name + ": " + e.Message + ").");
                return null;
            }
        }

        /// <summary>The document refused its own version. Both FromJson calls answer null without
        /// saying which version they saw, so the number is read back out of the raw text here —
        /// the refusal is only useful to a player if it names the version and the file.</summary>
        static void LogRefusal(string path, string json)
        {
            Log.Warn(Tag, Path.GetFileName(path) + ": " + SaveFormat.RefusalFor(VersionIn(json)) + ".");
        }

        /// <summary>The SaveVersion in a raw document, or 0 when it carries none.</summary>
        public static int VersionIn(string json)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            return JsonText.ExtractInt(json, "SaveVersion", 0);
        }

        // ---- The campaign from before the split ----

        /// <summary>Say once, plainly, that the pre-split save cannot be loaded. The file is left
        /// exactly where it is: it is the only copy of that campaign, this build cannot tell which
        /// half of it was the world's, and deleting it to tidy up would be the data loss.</summary>
        public static void ReportLegacySaveOnce()
        {
            if (_legacyReported) return;
            string path = SavePath;
            try
            {
                if (!File.Exists(path)) return;
                _legacyReported = true;
                int version = VersionIn(File.ReadAllText(path));
                Log.Warn(Tag, LegacyFileName + ": " + SaveFormat.RefusalFor(version)
                    + ". It has been left untouched at " + path + ".");
            }
            catch (Exception e)
            {
                _legacyReported = true;
                Log.Warn(Tag, LegacyFileName + " is present at " + path + " but could not be read ("
                    + e.GetType().Name + ": " + e.Message + "). It has been left untouched.");
            }
        }
    }
}
