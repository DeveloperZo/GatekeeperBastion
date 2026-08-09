using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>
    /// Shared plumbing for the Play-mode contract tests: build-settings guards (so a fresh
    /// clone with no generated scenes reports INCONCLUSIVE instead of hard-failing), bounded
    /// waits (every loop has BOTH a wall-clock and a frame cap — no test can hang), scene
    /// walking that also sees inactive objects, reflection lookups for members that other
    /// agents are still writing, a save-file guard so tests never clobber the developer's
    /// real campaign, and an error-log capture used by the death-path test.
    /// </summary>
    /// <remarks>
    /// Everything here is deliberately reflection-tolerant: these tests must keep COMPILING
    /// while sibling agents are mid-edit in Assets/Scripts. Anything not on the agreed stable
    /// API surface is reached by name, and a missing member produces a descriptive
    /// Assert.Fail that documents the requirement rather than a compile error.
    /// </remarks>
    internal static class PlayTestUtil
    {
        /// <summary>Wall-clock cap for every wait helper (seconds).</summary>
        public const float DefaultTimeout = 15f;

        /// <summary>Hard frame cap for every wait helper — belt and braces against a hang.</summary>
        public const int MaxWaitFrames = 900;

        // ------------------------------------------------------------------ build settings

        /// <summary>True when a scene of this name is registered (and enabled) in Build Settings.</summary>
        public static bool IsSceneInBuild(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path)) continue;
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Comma-separated list of the scenes currently in Build Settings (for messages).</summary>
        public static string RegisteredScenes()
        {
            int count = SceneManager.sceneCountInBuildSettings;
            if (count == 0) return "(none)";
            var names = new List<string>(count);
            for (int i = 0; i < count; i++)
                names.Add(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)));
            return string.Join(", ", names);
        }

        /// <summary>Skip (INCONCLUSIVE, never a hard failure) when the scene has not been
        /// generated/registered yet — the fresh-clone case, since scenes are built by the
        /// GatekeeperBastion > Build All Scenes editor tool and are not committed.</summary>
        public static void RequireSceneInBuild(string sceneName)
        {
            if (IsSceneInBuild(sceneName)) return;
            Assert.Inconclusive(
                $"SKIPPED: scene '{sceneName}' is not in Build Settings (registered: {RegisteredScenes()}). " +
                "Run menu GatekeeperBastion > Build All Scenes, then re-run this test.");
        }

        // ------------------------------------------------------------------ scene loading

        /// <summary>Load a scene single-mode and let it settle for a few frames (Awake/OnEnable/
        /// Start plus the first physics + render ticks). Bounded by the async op itself.</summary>
        public static IEnumerator LoadSceneAndSettle(string sceneName, int settleFrames = 4)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.IsNotNull(op, $"LoadSceneAsync('{sceneName}') returned null — scene not loadable.");
            yield return op;

            for (int i = 0; i < settleFrames; i++) yield return null;
            Physics.SyncTransforms(); // colliders are queried right after load in these tests
        }

        /// <summary>Teardown helper: leave a blank active scene so the next fixture (including
        /// the pre-existing CoreLoopPlayModeTests, which builds its objects into whatever scene
        /// is active) does not inherit a fully populated game scene.</summary>
        public static IEnumerator ReturnToBlankScene()
        {
            Scene active = SceneManager.GetActiveScene();
            bool isGameScene = active.IsValid() &&
                               (active.name == GatekeeperBastion.Game.SceneFlow.Ship ||
                                active.name == GatekeeperBastion.Game.SceneFlow.Bastion ||
                                active.name == GatekeeperBastion.Game.SceneFlow.MainMenu ||
                                active.name == GatekeeperBastion.Game.SceneFlow.Splash);
            if (!isGameScene)
            {
                yield return null;
                yield break;
            }

            Scene blank = SceneManager.CreateScene("PlayTests_Blank_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            SceneManager.SetActiveScene(blank);
            AsyncOperation op = SceneManager.UnloadSceneAsync(active);
            if (op != null) yield return op;
            yield return null;
        }

        // ------------------------------------------------------------------ bounded waiting

        /// <summary>Yield until the condition holds, the timeout expires, or the frame cap is
        /// hit — whichever comes first. Callers assert the condition afterwards so the failure
        /// message is theirs. NEVER blocks forever.</summary>
        public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds = DefaultTimeout)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            int frames = 0;
            while (frames < MaxWaitFrames && Time.realtimeSinceStartup < deadline)
            {
                bool done;
                try { done = condition(); }
                catch (NullReferenceException) { done = false; } // scene may still be tearing down
                if (done) yield break;
                frames++;
                yield return null;
            }
        }

        /// <summary>Yield a fixed number of frames (used to force Update/LateUpdate/OnGUI passes).</summary>
        public static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        // ------------------------------------------------------------------ scene searching

        /// <summary>Every component of type T (interfaces included) in the ACTIVE scene,
        /// inactive objects included. Preferred over FindObjectsByType here because it works
        /// identically for interfaces and for reflected types.</summary>
        public static List<T> FindAll<T>()
        {
            var results = new List<T>();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return results;
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren<T>(true));
            return results;
        }

        /// <summary>Same as <see cref="FindAll{T}"/> for a Type discovered by reflection.</summary>
        public static List<Component> FindAll(Type type)
        {
            var results = new List<Component>();
            if (type == null) return results;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return results;
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren(type, true));
            return results;
        }

        // ------------------------------------------------------------------ reflection

        /// <summary>Find a type by full name (or, failing that, by short name) across every
        /// loaded assembly. Returns null when the type does not exist yet.</summary>
        public static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            Type direct = Type.GetType(fullName);
            if (direct != null) return direct;

            string shortName = fullName;
            int dot = fullName.LastIndexOf('.');
            if (dot >= 0 && dot < fullName.Length - 1) shortName = fullName.Substring(dot + 1);

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Fast pass: exact full name in any loaded assembly.
            foreach (Assembly asm in assemblies)
            {
                Type t;
                try { t = asm.GetType(fullName); }
                catch (Exception) { continue; }
                if (t != null) return t;
            }

            // Slow pass (only when the type was renamed/moved): match the short name.
            foreach (Assembly asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }
                if (types == null) continue;
                foreach (Type candidate in types)
                {
                    if (candidate != null && candidate.Name == shortName) return candidate;
                }
            }
            return null;
        }

        /// <summary>First zero-argument instance method matching one of the candidate names,
        /// public ones preferred. Returns null when none exists.</summary>
        public static MethodInfo FindZeroArgMethod(Type type, string[] candidateNames, out bool isPublic)
        {
            isPublic = false;
            if (type == null || candidateNames == null) return null;

            foreach (string name in candidateNames)
            {
                MethodInfo m = SafeGetMethod(type, name, BindingFlags.Instance | BindingFlags.Public);
                if (m != null && m.GetParameters().Length == 0) { isPublic = true; return m; }
            }
            foreach (string name in candidateNames)
            {
                MethodInfo m = SafeGetMethod(type, name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (m != null && m.GetParameters().Length == 0) { isPublic = m.IsPublic; return m; }
            }
            return null;
        }

        static MethodInfo SafeGetMethod(Type type, string name, BindingFlags flags)
        {
            try { return type.GetMethod(name, flags); }
            catch (AmbiguousMatchException) { return null; }
        }

        /// <summary>
        /// Invoke a public instance method by name, supplying only the leading arguments we care
        /// about and filling any remaining parameters with their declared defaults (or
        /// null/zero). Lets these tests survive a signature growing an extra parameter — e.g.
        /// PlayerHealth.TakeDamage(float) becoming TakeDamage(float, Transform) — without a
        /// compile break or a silently wrong call. Returns false only when no such method exists;
        /// exceptions thrown BY the method are deliberately allowed to propagate and fail the test.
        /// </summary>
        public static bool TryInvokeWithDefaults(object target, string methodName, object[] leadingArgs,
            out object result)
        {
            result = null;
            if (target == null || string.IsNullOrEmpty(methodName)) return false;
            if (leadingArgs == null) leadingArgs = new object[0];

            MethodInfo best = null;
            foreach (MethodInfo m in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (m.Name != methodName) continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length < leadingArgs.Length) continue;

                bool matches = true;
                for (int i = 0; i < leadingArgs.Length; i++)
                {
                    if (leadingArgs[i] == null)
                    {
                        if (ps[i].ParameterType.IsValueType) { matches = false; break; }
                        continue;
                    }
                    if (!ps[i].ParameterType.IsInstanceOfType(leadingArgs[i])) { matches = false; break; }
                }
                if (!matches) continue;
                if (best == null || ps.Length < best.GetParameters().Length) best = m;
            }
            if (best == null) return false;

            ParameterInfo[] parameters = best.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < leadingArgs.Length) { args[i] = leadingArgs[i]; continue; }
                if (parameters[i].HasDefaultValue) { args[i] = parameters[i].DefaultValue; continue; }
                args[i] = parameters[i].ParameterType.IsValueType
                    ? Activator.CreateInstance(parameters[i].ParameterType)
                    : null;
            }
            result = best.Invoke(target, args);
            return true;
        }

        /// <summary>Read the first non-empty public string field/property/parameterless method
        /// on <paramref name="target"/> whose name contains any of the given fragments
        /// (case-insensitive). Used to discover a "why can't I shoot" reason string that another
        /// agent may or may not have added yet. Never throws.</summary>
        public static string TryReadStringMember(object target, params string[] nameFragments)
        {
            if (target == null || fragmentsEmpty(nameFragments)) return null;
            Type type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

            foreach (PropertyInfo p in type.GetProperties(flags))
            {
                if (p.PropertyType != typeof(string) || !NameMatches(p.Name, nameFragments)) continue;
                if (p.GetIndexParameters().Length != 0 || !p.CanRead) continue;
                string value = SafeToString(() => p.GetValue(target, null));
                if (!string.IsNullOrEmpty(value)) return value;
            }
            foreach (FieldInfo f in type.GetFields(flags))
            {
                if (f.FieldType != typeof(string) || !NameMatches(f.Name, nameFragments)) continue;
                string value = SafeToString(() => f.GetValue(target));
                if (!string.IsNullOrEmpty(value)) return value;
            }
            foreach (MethodInfo m in type.GetMethods(flags))
            {
                if (m.ReturnType != typeof(string) || m.GetParameters().Length != 0) continue;
                if (!NameMatches(m.Name, nameFragments)) continue;
                string value = SafeToString(() => m.Invoke(target, null));
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return null;
        }

        static bool fragmentsEmpty(string[] fragments)
        {
            return fragments == null || fragments.Length == 0;
        }

        static bool NameMatches(string name, string[] fragments)
        {
            foreach (string fragment in fragments)
            {
                if (!string.IsNullOrEmpty(fragment) &&
                    name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        static string SafeToString(Func<object> getter)
        {
            try { return getter() as string; }
            catch (Exception) { return null; }
        }

        // ------------------------------------------------------------------ save-file guard

        /// <summary>Save path for the assembly-level fixture, which needs it before any
        /// <see cref="SaveFileGuard"/> exists in order to recover a backup a crashed run left
        /// behind. Same reflection lookup the guard uses; exposed rather than duplicated.</summary>
        public static string ResolveSavePathForFixture() => SaveFileGuard.ResolveSavePath();

        /// <summary>
        /// Backs up the developer's real save file (SaveService.SavePath, located by reflection)
        /// and restores it on Dispose, so these tests can start new sessions and buy things
        /// without eating anyone's campaign.
        /// </summary>
        public sealed class SaveFileGuard : IDisposable
        {
            /// <summary>Suffix shared with <c>PlayModeSaveFixture</c> orphan recovery.
            /// One process-wide backup file: a nested guard that overwrote it was how the
            /// assembly fixture lost the developer's campaign (M17).</summary>
            public const string BackupSuffix = ".playtestbackup";

            readonly string _path;
            readonly string _backup;
            readonly bool _existed;
            readonly bool _ownsBackup;

            public SaveFileGuard()
            {
                _path = ResolveSavePath();
                if (string.IsNullOrEmpty(_path)) return;

                string backup = _path + BackupSuffix;
                // Outer fixture (or a prior guard) already owns the real save. Nesting
                // would File.Copy over that backup and Dispose would delete it, leaving
                // the assembly fixture unable to restore.
                if (File.Exists(backup))
                {
                    Debug.LogWarning(
                        "[SaveFileGuard] .playtestbackup already present — nested guard is a no-op.");
                    return;
                }

                bool existed = File.Exists(_path);
                try
                {
                    if (existed)
                        File.Copy(_path, backup, true);
                    _backup = backup;
                    _existed = existed;
                    _ownsBackup = true;
                }
                catch (IOException) { /* leave ownsBackup false */ }
                catch (UnauthorizedAccessException) { }
            }

            public void Dispose()
            {
                if (!_ownsBackup || string.IsNullOrEmpty(_path)) return;
                try
                {
                    if (_existed && !string.IsNullOrEmpty(_backup) && File.Exists(_backup))
                    {
                        File.Copy(_backup, _path, true);
                        File.Delete(_backup);
                        // Pull the restored file back into the live session so later tests and
                        // the editor session see the developer's real progression again.
                        // By reflection: ContinueGame is not on the agreed stable API surface.
                        ReloadSession();
                    }
                    else if (!_existed && File.Exists(_path))
                    {
                        File.Delete(_path);
                    }
                }
                catch (IOException) { /* best effort: never fail a test in teardown */ }
                catch (UnauthorizedAccessException) { }
            }

            static void ReloadSession()
            {
                Type session = FindType("GatekeeperBastion.Game.GameSession");
                if (session == null) return;
                MethodInfo reload = session.GetMethod("ContinueGame", BindingFlags.Static | BindingFlags.Public);
                if (reload == null || reload.GetParameters().Length != 0) return;
                try { reload.Invoke(null, null); }
                catch (TargetInvocationException) { /* best effort in teardown */ }
            }

            internal static string ResolveSavePath()
            {
                Type saveService = FindType("GatekeeperBastion.Game.SaveService");
                if (saveService == null) return null;
                PropertyInfo prop = saveService.GetProperty("SavePath",
                    BindingFlags.Static | BindingFlags.Public);
                if (prop == null || prop.PropertyType != typeof(string)) return null;
                try { return prop.GetValue(null, null) as string; }
                catch (Exception) { return null; }
            }
        }

        // ------------------------------------------------------------------ error-log capture

        /// <summary>
        /// Fails a test if ANY LogType.Error / Exception / Assert reaches the console while it
        /// is active. Takes over from the test framework's own log assertions
        /// (LogAssert.ignoreFailingMessages) so that a single, readable message names every
        /// error and the stage it happened in.
        /// </summary>
        public sealed class ErrorLogCapture : IDisposable
        {
            readonly List<string> _errors = new List<string>();
            readonly List<string> _environmentNoise = new List<string>();
            readonly bool _previousIgnoreFailing;
            bool _disposed;

            public ErrorLogCapture()
            {
                _previousIgnoreFailing = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true; // this capture is the judge instead
                Application.logMessageReceived += OnLog;
            }

            /// <summary>Errors seen so far (for custom messages).</summary>
            public IReadOnlyList<string> Errors { get { return _errors; } }

            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                string entry = type + ": " + condition + "\n" + stackTrace;
                if (IsEnvironmentNoise(condition)) { _environmentNoise.Add(entry); return; }
                _errors.Add(entry);
            }

            /// <summary>
            /// Noise that is about the test ENVIRONMENT, not the code path under test: enemy
            /// gatekeepers carry NavMeshAgents and a freshly cloned project has no baked NavMesh
            /// asset, and off-screen cameras can emit frustum spam in a headless run. Those have
            /// their own contract; letting them fail the death-path test would make it useless.
            /// </summary>
            static bool IsEnvironmentNoise(string condition)
            {
                if (string.IsNullOrEmpty(condition)) return false;
                string c = condition.ToLowerInvariant();
                return c.Contains("navmesh") ||
                       c.Contains("setdestination") ||
                       c.Contains("not close enough") ||
                       (c.Contains("agent") && c.Contains("nav")) ||
                       c.Contains("out of view frustum");
            }

            /// <summary>Fail now if anything was logged as an error since construction.</summary>
            public void AssertNoErrors(string stage)
            {
                if (_errors.Count == 0) return;
                Assert.Fail($"{_errors.Count} error(s)/exception(s) were logged during: {stage}. " +
                            "The death -> lose screen -> return-to-ship path must never throw.\n" +
                            string.Join("\n---\n", _errors));
            }

            /// <summary>Ignored environment noise, for diagnostics in other messages.</summary>
            public string NoiseSummary()
            {
                return _environmentNoise.Count == 0
                    ? "(none)"
                    : _environmentNoise.Count + " ignored environment message(s) (NavMesh/frustum)";
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Application.logMessageReceived -= OnLog;
                LogAssert.ignoreFailingMessages = _previousIgnoreFailing;
            }
        }

        // ------------------------------------------------------------------ cursor support

        /// <summary>
        /// Probe whether this run can actually hold a cursor lock. In an unfocused Game view
        /// (typical for a batch/CI test run) the editor refuses to keep CursorLockMode.Locked,
        /// which would make the walk-mode assertion flaky. Leaves the cursor FREE so the scene
        /// under test is the thing that locks it.
        /// </summary>
        public static IEnumerator ProbeCursorLockSupport(Action<bool> result)
        {
            Cursor.lockState = CursorLockMode.Locked;
            yield return null;
            yield return null;
            bool supported = Cursor.lockState == CursorLockMode.Locked;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return null;

            if (result != null) result(supported);
        }
    }
}
