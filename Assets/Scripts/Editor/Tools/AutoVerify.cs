using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>
    /// File-bridge verification runner: when <projectRoot>/gkb_verify_request.txt exists (or the
    /// menu item is used), runs the full EditMode suite then the PlayMode suite via TestRunnerApi
    /// and writes <projectRoot>/gkb_verify_report.json with per-test results. Lets an external
    /// agent request and read verification without any live MCP connection. Survives the domain
    /// reloads PlayMode runs cause by re-registering callbacks from [InitializeOnLoad] and
    /// staging progress in SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoVerify
    {
        const string StageKey = "GKB_VERIFY_STAGE";           // "", "editmode", "playmode"
        const string EditResultsKey = "GKB_VERIFY_EDIT_JSON"; // leaf results captured after stage 1
        const string StageSinceKey = "GKB_VERIFY_STAGE_SINCE"; // last progress (stage set / RunStarted / TestStarted)
        const string PlayKickKey = "GKB_VERIFY_PLAY_KICK";   // attempts to start PlayMode after EditMode
        const string PendingKey = "GKB_VERIFY_PENDING";      // sentinel consumed; run starts next idle tick
        // Measured from last progress, not from stage entry: PlayMode suites idle between tests
        // while the editor is out of play, and a 90s-from-start timer aborted live runs (M17).
        const double StuckStageSeconds = 180.0;

        static TestRunnerApi _api;

        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_verify_request.txt");
        static string ReportPath => Path.Combine(Root, "gkb_verify_report.json");

        static double _nextSentinelPoll;

        static AutoVerify()
        {
            EditorApplication.delayCall += OnEditorReady;
            EditorApplication.update += PollSentinel;
        }

        static void OnEditorReady()
        {
            EnsureApi();
            // A sentinel always wins: if a previous run died mid-flight (play-mode collision,
            // domain reload during an unexpected error) StageKey can stick on "editmode" forever
            // and block every later request. Consuming the sentinel clears that.
            if (TryConsumeSentinel())
                return;
            // A pending run armed before a domain reload starts here (or on a later poll).
            if (TryStartPending())
                return;
            string stage = SessionState.GetString(StageKey, "");
            if (stage == "playmode")
            {
                // delayCall chains die across domain reload. Re-arm the PlayMode kick so a
                // reload between EditMode finish and PlayMode start cannot leave us idle until
                // the stuck timer fires.
                Debug.Log("[AutoVerify] Resuming PlayMode kick after domain reload.");
                SchedulePlayMode();
                return;
            }
            if (stage == "editmode")
                return; // live run surviving a domain reload; callbacks above will finish it
        }

        /// <summary>Pick up an agent-written sentinel without waiting for a domain reload.</summary>
        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextSentinelPoll) return;
            _nextSentinelPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Domain reload / script compile leaves StageKey set and TestRunner mute —
            // do not treat that idle as a stuck stage, or a live PlayMode run is aborted.
            if (EditorApplication.isCompiling) return;
            // Sentinel always wins — same rule as OnEditorReady. A stuck StageKey from a
            // mid-run domain reload must not permanently ignore agent requests.
            if (TryConsumeSentinel()) return;
            // A consumed sentinel starts once the refresh (and any compile it queued) settles.
            if (TryStartPending()) return;
            // If PlayMode is armed but the kick loop went silent (lost delayCall), re-kick
            // once per poll. PlayKickKey stays >0 until PlayTests RunStarted.
            if (SessionState.GetString(StageKey, "") == "playmode"
                && SessionState.GetInt(PlayKickKey, -1) != 0)
            {
                KickPlayMode();
                return;
            }
            ClearStuckStageIfIdle();
        }

        /// <summary>If StageKey says a run is live but the editor is idle (not playing) past
        /// <see cref="StuckStageSeconds"/>, the TestRunner callback was lost — clear so the
        /// next sentinel/menu can start. Does not invent a green report.</summary>
        static void ClearStuckStageIfIdle()
        {
            string stage = SessionState.GetString(StageKey, "");
            if (stage != "editmode" && stage != "playmode") return;
            double since = SessionState.GetFloat(StageSinceKey, -1f);
            if (since < 0) { MarkStage(stage); return; } // first sighting after domain reload
            if (EditorApplication.timeSinceStartup - since < StuckStageSeconds) return;

            Debug.LogWarning(
                $"[AutoVerify] Clearing stuck stage '{stage}' after {StuckStageSeconds:0}s idle " +
                "(TestRunner never finished). Write gkb_verify_request.txt or use Run Verification Now.");
            SessionState.SetString(StageKey, "");
            SessionState.SetFloat(StageSinceKey, -1f);
            WriteReport("error", "stuck", null, null,
                $"TestRunner never finished stage '{stage}'; cleared after {StuckStageSeconds:0}s idle.");
        }

        static void MarkStage(string stage)
        {
            SessionState.SetString(StageKey, stage);
            SessionState.SetFloat(StageSinceKey, (float)EditorApplication.timeSinceStartup);
        }

        /// <summary>Two-phase sentinel consumption. Consuming used to Refresh and Execute in the
        /// same tick; when the refresh queued a script recompile (agents edit scripts on disk),
        /// the domain reload wiped the queued TestRunner job before RunStarted — the run was
        /// never serialized, StageKey stuck on "editmode", and the 180 s timer wrote the
        /// "stuck" report (M35: the diagnosed cause of the broken verify loop). Now the
        /// sentinel is consumed into a SessionState pending flag, which survives the reload;
        /// the run starts on a later idle tick with fresh assemblies.</summary>
        static bool TryConsumeSentinel()
        {
            if (!File.Exists(SentinelPath)) return false;
            // Never start EditMode while PlayMode tests (or the user) are in play —
            // TestRunner throws "cannot be used during play mode" and wipes the report.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[AutoVerify] Sentinel waiting — editor is in play mode.");
                return false;
            }
            try { File.Delete(SentinelPath); }
            catch (Exception e)
            {
                Debug.LogWarning("[AutoVerify] Could not delete sentinel: " + e.Message);
                return false;
            }
            // Sentinel always wins: a stale stage from a dead run must not block this request.
            SessionState.SetString(StageKey, "");
            SessionState.SetFloat(StageSinceKey, -1f);
            SessionState.SetString(PendingKey, "sentinel file");
            Debug.Log("[AutoVerify] Sentinel consumed — verification pending after refresh.");
            // External agents edit scripts on disk; without a refresh Unity may still hold the
            // previous assemblies. Any compile/domain reload this queues happens before the
            // pending run starts.
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>Starts a pending (sentinel-requested) run once the editor is idle: not
        /// compiling, not updating, not entering play mode. Returns true when it consumed
        /// the pending flag.</summary>
        static bool TryStartPending()
        {
            string pending = SessionState.GetString(PendingKey, "");
            if (string.IsNullOrEmpty(pending)) return false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            SessionState.SetString(PendingKey, "");
            BeginVerification(pending);
            return true;
        }

        [MenuItem("GatekeeperBastion/Run Verification Now")]
        public static void RunFromMenu() => BeginVerification("menu");

        // No Reset command: a stuck StageKey is already cleared by TryConsumeSentinel, which
        // always wins over a stale stage. A recovery button here would be a defect with a
        // button on it (Docs/Working-Rules.md, "Tools, scripts and the menu").
        static void BeginVerification(string trigger)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[AutoVerify] Refusing '" + trigger + "' — editor is in play mode.");
                return;
            }

            try { if (File.Exists(SentinelPath)) File.Delete(SentinelPath); }
            catch (Exception e) { Debug.LogWarning("[AutoVerify] Could not delete sentinel: " + e.Message); }

            // A test assembly that did not build is the most common failure here, and TestRunner
            // reports it as the same opaque "unexpected error" as everything else. Name it before
            // starting, or the report says only that something went wrong.
            //
            // Two failure shapes: (1) scripts failed to compile this session — EditorUtility
            // knows, but AppDomain may still hold the last-good assemblies, so a load check
            // alone is blind; (2) cold start where the assembly never loaded at all.
            if (EditorUtility.scriptCompilationFailed)
            {
                const string why =
                    "Scripts failed to compile — fix the errors in the Unity console; no tests were run.";
                Debug.LogError($"[AutoVerify] {why}");
                SessionState.SetString(StageKey, "");
                SessionState.SetFloat(StageSinceKey, -1f);
                WriteReport("error", "precheck", null, null, why);
                return;
            }

            string missing = MissingTestAssembly();
            if (missing != null)
            {
                string why = EditorApplication.isCompiling
                    ? $"Editor is still compiling; '{missing}' is not loaded yet. Retry when compilation finishes."
                    : $"Test assembly '{missing}' is not loaded — it failed to compile. "
                      + "Fix the compile errors in the Unity console; no tests were run.";
                Debug.LogError("[AutoVerify] " + why);
                SessionState.SetString(StageKey, "");
                SessionState.SetFloat(StageSinceKey, -1f);
                WriteReport("error", "precheck", null, null, why);
                return;
            }

            Debug.Log("[AutoVerify] Verification started (" + trigger + ").");
            MarkStage("editmode");
            SessionState.SetString(EditResultsKey, "");
            WriteReport("running", "editmode", null, null);
            Run(TestMode.EditMode);
        }

        /// <summary>Name of the first test assembly that is not loaded, or null when both are.
        /// Plain reflection over the loaded assemblies rather than an editor-internal compile
        /// flag: an assembly that failed to build is simply absent.</summary>
        static string MissingTestAssembly()
        {
            foreach (string name in new[] { "GatekeeperBastion.Tests", "GatekeeperBastion.PlayTests" })
            {
                bool loaded = false;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name != name) continue;
                    loaded = true;
                    break;
                }
                if (!loaded) return name;
            }
            return null;
        }

        static void EnsureApi()
        {
            if (_api == null)
            {
                _api = ScriptableObject.CreateInstance<TestRunnerApi>();
                _api.RegisterCallbacks(new Callbacks());
            }
        }

        static void Run(TestMode mode)
        {
            EnsureApi();
            // Pin the assembly so a stale EditMode RunFinished cannot be mistaken for PlayMode
            // (and so PlayMode cannot accidentally pull the editor-only suite).
            string assembly = mode == TestMode.PlayMode
                ? "GatekeeperBastion.PlayTests"
                : "GatekeeperBastion.Tests";
            Debug.Log("[AutoVerify] TestRunner.Execute " + mode + " assembly=" + assembly);
            _api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = new[] { assembly }
            }));
        }

        /// <summary>
        /// EditMode's TestJob is still tearing down when RunFinished returns. A single delayCall
        /// Execute for PlayMode often no-ops; retry on a short cadence until RunStarted clears
        /// the kick counter or we give up.
        /// </summary>
        static void SchedulePlayMode()
        {
            // Seed at 1 so PollSentinel's "kick != 0" resume can see an armed kick before the
            // first KickPlayMode body runs. RunStarted (PlayTests) clears it back to 0.
            SessionState.SetInt(PlayKickKey, 1);
            MarkStage("playmode"); // heartbeat — otherwise stuck timer dates from EditMode end
            EditorApplication.delayCall += KickPlayMode;
        }

        static void KickPlayMode()
        {
            if (SessionState.GetString(StageKey, "") != "playmode") return;
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += KickPlayMode;
                return;
            }

            int attempt = SessionState.GetInt(PlayKickKey, 0);
            // PlayKickKey stays >0 until RunStarted. Attempt 0/1 fires immediately (1 is the
            // armed seed from SchedulePlayMode); later attempts re-fire every 5 ticks.
            bool shouldExecute = attempt <= 1 || (attempt % 5 == 0);
            if (attempt >= 50)
            {
                string edit = SessionState.GetString(EditResultsKey, "");
                SessionState.SetString(StageKey, "");
                SessionState.SetFloat(StageSinceKey, -1f);
                SessionState.SetInt(PlayKickKey, 0);
                const string why =
                    "PlayMode TestRunner.Execute never started after EditMode (gave up after retries).";
                Debug.LogError("[AutoVerify] " + why);
                WriteReport("error", "playmode", string.IsNullOrEmpty(edit) ? null : edit, null, why);
                return;
            }

            SessionState.SetInt(PlayKickKey, attempt + 1);
            if (shouldExecute)
            {
                try
                {
                    Debug.Log("[AutoVerify] PlayMode Execute attempt " + attempt);
                    Run(TestMode.PlayMode);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[AutoVerify] PlayMode Execute attempt " + attempt + " threw: " + e.Message);
                }
            }

            // Reschedule while still waiting for RunStarted to zero PlayKickKey.
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetString(StageKey, "") != "playmode") return;
                if (SessionState.GetInt(PlayKickKey, 0) == 0) return;
                KickPlayMode();
            };
        }

        sealed class Callbacks : ICallbacks, IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                string stage = SessionState.GetString(StageKey, "");
                if (stage != "editmode" && stage != "playmode") return;
                TouchProgress();
                // Only clear the PlayMode kick when PlayTests actually starts. A duplicate EditMode
                // RunStarted after we've moved to stage=playmode used to zero PlayKickKey and
                // kill the retry loop, leaving the stuck-idle timer to fire with no suite run.
                if (stage == "playmode" && AdaptorContains(testsToRun, "GatekeeperBastion.PlayTests"))
                    SessionState.SetInt(PlayKickKey, 0);
                Debug.Log("[AutoVerify] RunStarted stage=" + stage);
            }

            public void TestStarted(ITestAdaptor test) => TouchProgress();
            public void TestFinished(ITestResultAdaptor result) { }

            static void TouchProgress()
            {
                string stage = SessionState.GetString(StageKey, "");
                if (stage == "editmode" || stage == "playmode")
                    SessionState.SetFloat(StageSinceKey, (float)EditorApplication.timeSinceStartup);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try { HandleRunFinished(result); }
                catch (Exception e)
                {
                    Debug.LogError("[AutoVerify] Report failed: " + e);
                    string why = string.IsNullOrWhiteSpace(e.Message)
                        ? "Report assembly failed with no exception message."
                        : e.Message;
                    WriteReport("error", "report", null, null, why);
                    SessionState.SetString(StageKey, "");
                }
            }

            public void OnError(string message)
            {
                // Callbacks accumulate across domain reloads, so a single failure arrives several
                // times. Only the first one owns the report — later duplicates would overwrite a
                // diagnosed failure with an identical, or emptier, one.
                string stage = SessionState.GetString(StageKey, "");
                if (string.IsNullOrEmpty(stage)) return;

                string why = string.IsNullOrWhiteSpace(message)
                    ? "TestRunner OnError with no message."
                    : message;
                Debug.LogError("[AutoVerify] Test run error: " + why);
                string edit = SessionState.GetString(EditResultsKey, "");
                SessionState.SetString(StageKey, "");
                SessionState.SetFloat(StageSinceKey, -1f);
                WriteReport("error", stage, string.IsNullOrEmpty(edit) ? null : edit, null, why);
            }
        }

        static void HandleRunFinished(ITestResultAdaptor result)
        {
            string stage = SessionState.GetString(StageKey, "");
            if (string.IsNullOrEmpty(stage)) return;

            // Duplicate EditMode RunFinished after we've already moved on would otherwise
            // write the editor suite into playMode and drop the real PlayTests run.
            bool playLeaves = ResultContains(result, "GatekeeperBastion.PlayTests");
            bool editLeaves = ResultContains(result, "GatekeeperBastion.Tests.");
            if (stage == "editmode" && playLeaves && !editLeaves)
            {
                Debug.LogWarning("[AutoVerify] Ignoring PlayMode result while still in editmode stage.");
                return;
            }
            if (stage == "playmode" && !playLeaves)
            {
                Debug.LogWarning("[AutoVerify] Ignoring non-PlayMode RunFinished while waiting for PlayMode.");
                return;
            }

            var leaves = new StringBuilder();
            int pass = 0, fail = 0, skip = 0;
            CollectLeaves(result, leaves, ref pass, ref fail, ref skip);
            string block = "{\"passed\":" + pass + ",\"failed\":" + fail + ",\"skipped\":" + skip
                         + ",\"tests\":[" + leaves + "]}";

            if (stage == "editmode")
            {
                SessionState.SetString(EditResultsKey, block);
                MarkStage("playmode");
                Debug.Log($"[AutoVerify] EditMode done: {pass} passed, {fail} failed. Starting PlayMode.");
                WriteReport("running", "playmode", block, null);
                SchedulePlayMode();
            }
            else if (stage == "playmode")
            {
                string editBlock = SessionState.GetString(EditResultsKey, "null");
                SessionState.SetString(StageKey, "");
                SessionState.SetFloat(StageSinceKey, -1f);
                Debug.Log($"[AutoVerify] PlayMode done: {pass} passed, {fail} failed. Report written.");
                WriteReport("complete", "done", editBlock, block);
            }
        }

        static bool ResultContains(ITestResultAdaptor node, string fragment)
        {
            if (node == null) return false;
            if (!string.IsNullOrEmpty(node.Name) && node.Name.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                return true;
            if (!string.IsNullOrEmpty(node.FullName) && node.FullName.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                return true;
            if (!node.HasChildren) return false;
            foreach (var child in node.Children)
                if (ResultContains(child, fragment)) return true;
            return false;
        }

        static bool AdaptorContains(ITestAdaptor node, string fragment)
        {
            if (node == null) return false;
            if (!string.IsNullOrEmpty(node.Name) && node.Name.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                return true;
            if (!string.IsNullOrEmpty(node.FullName) && node.FullName.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                return true;
            if (!node.HasChildren) return false;
            foreach (var child in node.Children)
                if (AdaptorContains(child, fragment)) return true;
            return false;
        }

        static void CollectLeaves(ITestResultAdaptor node, StringBuilder sb, ref int pass, ref int fail, ref int skip)
        {
            if (!node.HasChildren)
            {
                if (node.Test.IsSuite) return;
                string status = node.TestStatus.ToString();
                if (node.TestStatus == TestStatus.Passed) pass++;
                else if (node.TestStatus == TestStatus.Failed) fail++;
                else skip++;
                if (sb.Length > 0) sb.Append(',');
                sb.Append("{\"name\":").Append(Quote(node.FullName))
                  .Append(",\"status\":").Append(Quote(status));
                if (node.TestStatus == TestStatus.Failed)
                    sb.Append(",\"message\":").Append(Quote(Truncate(node.Message, 500)));
                sb.Append('}');
                return;
            }
            foreach (var child in node.Children)
                CollectLeaves(child, sb, ref pass, ref fail, ref skip);
        }

        static void WriteReport(string status, string stage, string editBlock, string playBlock,
                                string message = null)
        {
            // Error statuses must never ship an empty message — that was the original bridge defect.
            if (status == "error" && string.IsNullOrWhiteSpace(message))
                message = "Verification failed with no message (internal).";

            var sb = new StringBuilder();
            sb.Append("{\"status\":").Append(Quote(status))
              .Append(",\"stage\":").Append(Quote(stage))
              .Append(",\"message\":").Append(Quote(Truncate(message ?? "", 800)))
              .Append(",\"unity\":").Append(Quote(Application.unityVersion))
              .Append(",\"timestampUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")))
              .Append(",\"editMode\":").Append(string.IsNullOrEmpty(editBlock) ? "null" : editBlock)
              .Append(",\"playMode\":").Append(string.IsNullOrEmpty(playBlock) ? "null" : playBlock)
              .Append('}');
            WriteRaw(sb.ToString());
        }

        static void WriteRaw(string json)
        {
            try { File.WriteAllText(ReportPath, json); }
            catch (Exception e)
            {
                Debug.LogError("[AutoVerify] Could not write report: " + e.Message);
                // Last-chance path so an agent still sees why the bridge failed.
                try
                {
                    string fallback = ReportPath + ".writefail";
                    string body = "{\"status\":\"error\",\"stage\":\"report\",\"message\":"
                                  + Quote(Truncate("Could not write gkb_verify_report.json: " + e.Message, 800))
                                  + "}";
                    File.WriteAllText(fallback, body);
                }
                catch (Exception) { /* nowhere left to write */ }
            }
        }

        static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') { }
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }
    }
}
