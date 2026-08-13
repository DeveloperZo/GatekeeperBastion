namespace GatekeeperBastion.Util
{
    /// <summary>
    /// Gameplay logging front end. Every call goes straight to UnityEngine.Debug as
    /// "[Tag] message"; <see cref="LogFile"/> picks it up off Application.logMessageReceived like
    /// any other console message. One write path, no filtering, no bookkeeping.
    ///
    ///   <see cref="Event"/>  — things that happened during the playthrough. Always written.
    ///   <see cref="Metric"/> — end-of-run summary lines. Always written.
    ///   <see cref="Info"/>   — verbose detail; compiles away without the GKB_VERBOSE define.
    ///   <see cref="Warn"/>/<see cref="Error"/> — actual problems.
    ///
    /// This is the only file outside UnityEngine-facing code that Sim/ is allowed to touch.
    /// </summary>
    public static class Log
    {
        /// <summary>Something that happened during the playthrough.</summary>
        public static void Event(string tag, string msg)
        {
            UnityEngine.Debug.Log(Line(tag, msg));
        }

        /// <summary>End-of-run summary line.</summary>
        public static void Metric(string tag, string msg)
        {
            UnityEngine.Debug.Log(Line(tag, msg));
        }

        /// <summary>Verbose debug detail. Stripped without GKB_VERBOSE.</summary>
        [System.Diagnostics.Conditional("GKB_VERBOSE")]
        public static void Info(string tag, string msg)
        {
            UnityEngine.Debug.Log(Line(tag, msg));
        }

        /// <summary>A problem that did not stop the run.</summary>
        public static void Warn(string tag, string msg)
        {
            UnityEngine.Debug.LogWarning(Line(tag, msg));
        }

        /// <summary>An actual failure.</summary>
        public static void Error(string tag, string msg)
        {
            UnityEngine.Debug.LogError(Line(tag, msg));
        }

        static string Line(string tag, string msg)
        {
            return "[" + tag + "] " + msg;
        }
    }
}
