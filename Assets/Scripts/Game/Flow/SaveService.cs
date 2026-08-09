using System;
using System.IO;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Static load/save of MetaState JSON at Application.persistentDataPath/gatekeeper_meta.json,
    /// falling back to a fresh MetaState (with a warning) on missing or corrupt files.
    /// IO backend for GameSession — ALL other code must go through GameSession, never call this directly.</summary>
    public static class SaveService
    {
        const string Tag = "SaveService";
        const string FileName = "gatekeeper_meta.json";

        /// <summary>Full path of the meta save file.</summary>
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>True when a save file exists on disk. (GameSession.HasSave delegates here.)</summary>
        public static bool Exists()
        {
            try { return File.Exists(SavePath); }
            catch (Exception) { return false; }
        }

        /// <summary>Loads the persisted MetaState, or returns a fresh one if absent/corrupt.</summary>
        public static MetaState Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var meta = MetaState.FromJson(File.ReadAllText(SavePath));
                    if (meta != null) return meta;
                    Log.Warn(Tag, "Save file parsed to null; starting a fresh MetaState.");
                }
            }
            catch (Exception e)
            {
                Log.Warn(Tag, $"Corrupt or unreadable save at {SavePath} ({e.Message}); starting a fresh MetaState.");
            }
            return new MetaState();
        }

        /// <summary>Serializes the given MetaState to disk. Errors are logged, never thrown.</summary>
        public static void Save(MetaState meta)
        {
            if (meta == null)
            {
                Log.Warn(Tag, "Save called with null MetaState; ignored.");
                return;
            }
            try
            {
                File.WriteAllText(SavePath, meta.ToJson());
                Log.Info(Tag, $"Meta saved to {SavePath}.");
            }
            catch (Exception e)
            {
                Log.Error(Tag, $"Failed to save meta: {e.Message}");
            }
        }
    }
}
