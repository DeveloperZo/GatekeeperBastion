// Gatekeeper's Protocol — EditMode harness runner.
//
// Reflects over the compiled assembly, runs every [Test] method with its [SetUp] and
// [TearDown], and prints one line per failure plus a total. Exit code 0 = all passed,
// 1 = a test failed, 2 = the harness itself could not run.
//
// Deliberately not a test framework: no categories, no parallelism, no fixtures with
// arguments, no output file. Unity's Test Runner is the authority; this is the fast
// signal in front of it, and anything it cannot express is a reason to run Unity.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public static class HarnessRunner
{
    public static int Main(string[] args)
    {
        bool verbose = args.Contains("--verbose") || args.Contains("-v");
        string only = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--filter") only = args[i + 1];

        UnityEngine.Debug.Silent = !verbose;

        var fixtures = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                         .Any(m => m.GetCustomAttribute<TestAttribute>() != null))
            .OrderBy(t => t.FullName)
            .ToList();

        if (fixtures.Count == 0)
        {
            Console.Error.WriteLine("harness: no [Test] methods found — nothing was compiled in.");
            return 2;
        }

        int passed = 0, failed = 0, skipped = 0, ignored = 0;
        var ignoredNames = new List<string>();
        var failures = new List<string>();
        var sw = Stopwatch.StartNew();

        foreach (var fixture in fixtures)
        {
            var setUp = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                               .FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
            var tearDown = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                  .FirstOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);
            var tests = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                               .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                               .OrderBy(m => m.Name);

            foreach (var test in tests)
            {
                string name = fixture.Name + "." + test.Name;
                if (only != null && name.IndexOf(only, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    skipped++;
                    continue;
                }

                var ignore = test.GetCustomAttribute<IgnoreAttribute>();
                if (ignore != null)
                {
                    ignored++;
                    ignoredNames.Add(name + " — " + ignore.Reason);
                    continue;
                }

                object instance;
                try { instance = Activator.CreateInstance(fixture); }
                catch (Exception e)
                {
                    failed++;
                    failures.Add(name + "\n  could not construct fixture: " + Unwrap(e).Message);
                    continue;
                }

                try
                {
                    setUp?.Invoke(instance, null);
                    test.Invoke(instance, null);
                    passed++;
                    if (verbose) Console.WriteLine("  pass  " + name);
                }
                catch (Exception e)
                {
                    var inner = Unwrap(e);
                    if (inner is IgnoreException)
                    {
                        ignored++;
                        ignoredNames.Add(name + " — " + inner.Message);
                        continue;
                    }
                    failed++;
                    string detail = inner is AssertionException
                        ? inner.Message
                        : inner.GetType().Name + ": " + inner.Message + FirstFrame(inner);
                    failures.Add(name + "\n  " + detail.Replace("\n", "\n  "));
                }
                finally
                {
                    try { tearDown?.Invoke(instance, null); }
                    catch (Exception e)
                    {
                        failed++;
                        failures.Add(name + " [TearDown]\n  " + Unwrap(e).Message);
                    }
                }
            }
        }

        sw.Stop();

        if (failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("FAILURES");
            Console.WriteLine("--------");
            foreach (var f in failures) { Console.WriteLine(f); Console.WriteLine(); }
        }

        if (ignoredNames.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("IGNORED");
            Console.WriteLine("-------");
            foreach (var i in ignoredNames) Console.WriteLine("  " + i);
            Console.WriteLine();
        }

        Console.WriteLine(
            (failed == 0 ? "PASS" : "FAIL") +
            "  " + passed + " passed, " + failed + " failed" +
            (ignored > 0 ? ", " + ignored + " ignored" : "") +
            (skipped > 0 ? ", " + skipped + " filtered out" : "") +
            "  (" + fixtures.Count + " fixtures, " + sw.ElapsedMilliseconds + " ms)");

        return failed == 0 ? 0 : 1;
    }

    static Exception Unwrap(Exception e)
        => e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;

    static string FirstFrame(Exception e)
    {
        var line = (e.StackTrace ?? "").Split('\n').FirstOrDefault(l => l.Contains("Tests"));
        return line == null ? "" : "\n" + line.Trim();
    }
}
