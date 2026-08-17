using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Util
{
    /// <summary>
    /// Passthrough file sink. Everything the process sends to Debug.* — ours, Unity's, any
    /// package's — is written to one file per session:
    ///
    ///     Editor : &lt;projectRoot&gt;/Logs/GatekeepersProtocol_yyyy-MM-dd_HH-mm-ss_p{pid}.log
    ///     Player : Application.persistentDataPath/Logs/GatekeepersProtocol_yyyy-MM-dd_HH-mm-ss_p{pid}.log
    ///
    /// The pid suffix is load-bearing for MPPM: two processes that enter Play in the same
    /// second would otherwise FileMode.Create the same path and wipe the first file.
    ///
    /// Line format: [HH:mm:ss.fff] [Log|Warning|Error] message   (wall clock, Unity's own level
    /// words). Stack traces are appended for errors and exceptions only.
    ///
    /// No filtering, no dedup, no rotation beyond keeping the newest few sessions. Every IO path
    /// is wrapped: this sink can never throw into gameplay.
    /// </summary>
    public static class LogFile
    {
        public const string Product = "GatekeepersProtocol";

        /// <summary>Session files retained; older ones are deleted when a new session opens.
        /// MPPM writes one file per process, so this is roughly half as many Play sessions.</summary>
        public const int KeepSessions = 30;

        static readonly object Gate = new object();
        static StreamWriter _writer;

        /// <summary>Full path of this session's file, or empty if the sink never opened.</summary>
        public static string CurrentPath { get; private set; }

        /// <summary>Directory the session files live in.</summary>
        public static string Directory
        {
            get
            {
#if UNITY_EDITOR
                // Project folder in the editor: a remote agent reads the log from the repo.
                return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs");
#else
                return Path.Combine(Application.persistentDataPath, "Logs");
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Open()
        {
            lock (Gate)
            {
                if (_writer != null) return;

                DateTime now = DateTime.Now;
                try
                {
                    string dir = Directory;
                    System.IO.Directory.CreateDirectory(dir);
                    Prune(dir);

                    int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                    string path = Path.Combine(
                        dir, Product + "_" + now.ToString("yyyy-MM-dd_HH-mm-ss") + "_p" + pid + ".log");

                    var stream = new FileStream(path, FileMode.Create, FileAccess.Write,
                                                FileShare.ReadWrite | FileShare.Delete);
                    _writer = new StreamWriter(stream) { AutoFlush = true };
                    CurrentPath = path;

                    _writer.WriteLine("=====================================");
                    _writer.WriteLine("Gatekeeper's Protocol Log File");
                    _writer.WriteLine("Started: " + now.ToString("yyyy-MM-dd HH:mm:ss"));
                    _writer.WriteLine("Process: " + pid);
                    _writer.WriteLine("Unity Version: " + Application.unityVersion);
                    _writer.WriteLine("Platform: " + Application.platform);
                    _writer.WriteLine("Device: " + SystemInfo.deviceName + " (" + SystemInfo.deviceModel + ")");
                    _writer.WriteLine("=====================================");
                    _writer.WriteLine();
                }
                catch
                {
                    _writer = null;
                    CurrentPath = string.Empty;
                    return;
                }

                Application.logMessageReceived += OnLogMessage;
                Application.quitting += Close;
            }

            Debug.Log("[LogFile] File logging active - Writing to: " + CurrentPath);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SessionBanner()
        {
            Debug.Log("---------- Game Session Started ----------");
            Debug.Log("Scene: " + SceneManager.GetActiveScene().name);
            Debug.Log("----------------------------------------");
        }

        public static void Close()
        {
            lock (Gate)
            {
                if (_writer == null) return;
                Application.logMessageReceived -= OnLogMessage;
                Application.quitting -= Close;
                try { _writer.Dispose(); } catch { }
                _writer = null;
            }
        }

        static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            lock (Gate)
            {
                if (_writer == null) return;
                try
                {
                    _writer.WriteLine(DateTime.Now.ToString("[HH:mm:ss.fff] ") +
                                      "[" + Level(type) + "] " + message);

                    if ((type == LogType.Error || type == LogType.Exception) &&
                        !string.IsNullOrEmpty(stackTrace))
                    {
                        _writer.WriteLine(stackTrace.TrimEnd());
                    }
                }
                catch { /* logging must never break gameplay */ }
            }
        }

        // Unity's own words, so the file reads like the console.
        static string Level(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return "Warning";
                case LogType.Error: return "Error";
                case LogType.Assert: return "Assert";
                case LogType.Exception: return "Exception";
                default: return "Log";
            }
        }

        // Keep the newest KeepSessions files; delete the rest.
        static void Prune(string dir)
        {
            try
            {
                var files = new DirectoryInfo(dir).GetFiles(Product + "_*.log");
                if (files.Length < KeepSessions) return;
                Array.Sort(files, (a, b) => b.CreationTimeUtc.CompareTo(a.CreationTimeUtc));
                for (int i = KeepSessions - 1; i < files.Length; i++)
                {
                    try { files[i].Delete(); } catch { }
                }
            }
            catch { }
        }
    }
}
