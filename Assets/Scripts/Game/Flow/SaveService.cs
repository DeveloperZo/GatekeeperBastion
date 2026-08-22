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

        /// <summary>Extension for a half-written document. Not ".json.tmp" on purpose — see
        /// the remarks on <see cref="WriteDocument"/>.</summary>
        const string TempSuffix = ".writing";
        const string BoardersFolder = "boarders";
        const string RealmsFolder = "realms";

        /// <summary>Where deleted documents go. Deleting a realm throws away every run anyone
        /// ever banked into it, and a chooser is one mispress away from doing that, so the file
        /// is moved rather than unlinked. Nothing reads this folder — it exists so a mistake is
        /// recoverable by hand instead of not at all.</summary>
        const string RetiredFolder = "retired";

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

        /// <summary>Where deleted documents are kept. Not a save location: nothing lists it and
        /// nothing loads from it.</summary>
        public static string RetiredDirectory { get { return Path.Combine(SaveRoot, RetiredFolder); } }

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

        /// <summary>Move a realm out of play. True when the document is gone from the realms
        /// directory afterwards, which is the only thing a caller can act on.</summary>
        public static bool RetireRealm(string id)
        {
            return Retire(RealmPath(id), RealmsFolder, "realm");
        }

        /// <summary>Move a Boarder out of play.</summary>
        public static bool RetireBoarder(string id)
        {
            return Retire(BoarderPath(id), BoardersFolder, "Boarder");
        }

        /// <summary>The move itself. A name already taken in the retired folder is suffixed
        /// rather than overwritten — deleting a second time must not destroy the first
        /// mistake's only copy.</summary>
        static bool Retire(string path, string kindFolder, string kind)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Warn(Tag, "retire refused — no path for that " + kind);
                return false;
            }
            if (!File.Exists(path))
            {
                Log.Warn(Tag, "retire refused — no " + kind + " at " + path);
                return false;
            }
            try
            {
                string dir = Path.Combine(RetiredDirectory, kindFolder);
                Directory.CreateDirectory(dir);
                string name = Path.GetFileNameWithoutExtension(path);
                string target = Path.Combine(dir, name + ".json");
                int n = 1;
                while (File.Exists(target) && n < 1000)
                    target = Path.Combine(dir, name + "-" + n++ + ".json");
                File.Move(path, target);
                Log.Event(Tag, "retired " + kind + " to " + target);
                return true;
            }
            catch (Exception e)
            {
                // The document is still where it was, so the caller's list is still true.
                Log.Error(Tag, "retire failed for " + path + " (" + e.GetType().Name + ": "
                    + e.Message + ")");
                return false;
            }
        }

        static string[] FilesIn(string directory)
        {
            try
            {
                if (!Directory.Exists(directory)) return Array.Empty<string>();
                string[] all = Directory.GetFiles(directory, "*.json");
                // Windows matches *.json against 8.3 short names too, so re-check the real
                // extension rather than trusting the pattern.
                var kept = new System.Collections.Generic.List<string>(all.Length);
                for (int i = 0; i < all.Length; i++)
                    if (all[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        kept.Add(all[i]);
                return kept.ToArray();
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
        public static bool SaveBoarder(BoarderState boarder)
        {
            if (boarder == null)
            {
                Log.Warn(Tag, "SaveBoarder called with no Boarder; nothing written.");
                return false;
            }
            string path = BoarderPath(boarder.Id);
            if (path == null)
            {
                Log.Error(Tag, "Boarder \"" + boarder.Name + "\" has no usable id; refusing to write it.");
                return false;
            }
            return WriteDocument(path, boarder.ToJson(), "Boarder");
        }

        /// <summary>Write one realm. Requires nothing of the Boarder — only the host of a world
        /// writes that world, and it writes it whether or not anyone's kit changed.</summary>
        public static bool SaveRealm(RealmState realm)
        {
            if (realm == null)
            {
                Log.Warn(Tag, "SaveRealm called with no realm; nothing written.");
                return false;
            }
            string path = RealmPath(realm.Id);
            if (path == null)
            {
                Log.Error(Tag, "Realm \"" + realm.Name + "\" has no usable id; refusing to write it.");
                return false;
            }
            return WriteDocument(path, realm.ToJson(), "Realm");
        }

        /// <summary>Write, then swap. A direct write to the live path leaves a truncated
        /// document if the process dies mid-write, and a truncated document is worse than a
        /// missing one: it still carries its version key, so it loads as a realm that has
        /// simply forgotten everything. Writing beside the target and moving it into place
        /// means the live file is either the old document or the new one.</summary>
        /// <remarks>
        /// The temp file deliberately does NOT end in <c>.json</c>. Windows still matches
        /// <c>*.json</c> against 8.3 short names, so <c>&lt;id&gt;.json.tmp</c> could show up in a
        /// directory listing and be offered to the player as a realm.
        /// </remarks>
        static bool WriteDocument(string path, string json, string kind)
        {
            string temp = path + TempSuffix;
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
                Log.Info(Tag, kind + " saved to " + path + ".");
                return true;
            }
            catch (Exception e)
            {
                Log.Error(Tag, "Failed to save " + kind.ToLowerInvariant() + " to " + path
                    + " (" + e.GetType().Name + ": " + e.Message + "). The previous "
                    + kind.ToLowerInvariant() + " on disk is unchanged.");
                TryDeleteTemp(temp);
                return false;
            }
        }

        static void TryDeleteTemp(string temp)
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
                AdoptFileIdentity(ref boarder.Id, id, "Boarder");
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
                AdoptFileIdentity(ref realm.Id, id, "Realm");
                return realm;
            }
            catch (Exception e)
            {
                Log.Error(Tag, "Realm " + Path.GetFileName(path) + " could not be read ("
                    + e.GetType().Name + ": " + e.Message + "); it was not loaded.");
                return null;
            }
        }

        /// <summary>The file name IS the identity, always — not merely when the document
        /// forgot to carry one.</summary>
        /// <remarks>
        /// Letting an embedded id win looks harmless and loses data. Copy a realm file to
        /// back it up and both documents carry the original's id; enter the copy, and every
        /// save writes over the original while the copy silently stops changing. Taking the
        /// name it was found under makes a copied file a real, separate realm, which is what
        /// the person copying it plainly meant.
        /// </remarks>
        static void AdoptFileIdentity(ref string documentId, string fileId, string kind)
        {
            if (!string.IsNullOrEmpty(documentId) && documentId != fileId)
                Log.Warn(Tag, kind + " " + fileId + ".json carries the id \"" + documentId
                    + "\"; the file name wins and it will save as " + fileId + ".json.");
            documentId = fileId;
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
                if (!LooksWhole(json))
                {
                    // Every document opens with its version, so a file cut short still passes
                    // the version gate and then reads every remaining key as its default —
                    // loading as a realm that has forgotten its salvage, its doors and its
                    // clears. Refusing here is the difference between a visible problem and a
                    // silent one, and the next save would have made the loss permanent.
                    Log.Error(Tag, kind + " " + Path.GetFileName(path)
                        + " is truncated or corrupt (it does not end where a document should); "
                        + "it was NOT loaded and has been left on disk untouched.");
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

        /// <summary>Cheap structural check that a document is not cut short: it must open and
        /// close as one object. This is not a parser — it is the smallest test that separates
        /// "half a file" from "a file", which the version key alone cannot do because the
        /// version is the first thing written.</summary>
        static bool LooksWhole(string json)
        {
            string trimmed = json.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[trimmed.Length - 1] == '}';
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
