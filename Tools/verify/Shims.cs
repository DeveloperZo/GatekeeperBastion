// Gatekeeper's Protocol — EditMode harness shims.
//
// The sim compiles and runs outside Unity. Two things it touches do not exist out here:
// UnityEngine.Debug (reached only through Util/Log.cs) and NUnit.Framework (the test
// attributes and Assert). Both are stubbed below at exactly the surface the EditMode
// suite uses — nothing more, so a test that reaches for a wider NUnit API fails to
// compile here rather than passing under a weaker assertion than Unity would apply.
//
// Under Unity these shims are never compiled: this file lives outside Assets/.

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>Console-backed stand-in for UnityEngine.Debug. Log.cs is its only caller.</summary>
    public static class Debug
    {
        public static bool Silent = true;

        public static void Log(object message) { if (!Silent) Console.WriteLine(message); }
        public static void LogWarning(object message) { if (!Silent) Console.WriteLine("WARN  " + message); }
        public static void LogError(object message) { Console.WriteLine("ERROR " + message); }
    }

    /// <summary>Only the members Sim/ or the EditMode tests actually reference.</summary>
    public static class Mathf
    {
        public const float Epsilon = 1.401298E-45f;

        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Abs(float v) => Math.Abs(v);
        public static int Abs(int v) => Math.Abs(v);
        public static float Pow(float b, float e) => (float)Math.Pow(b, e);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Floor(float v) => (float)Math.Floor(v);
        public static float Ceil(float v) => (float)Math.Ceiling(v);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static int RoundToInt(float v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float InverseLerp(float a, float b, float v)
            => Math.Abs(b - a) < 1e-9f ? 0f : Clamp01((v - a) / (b - a));
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Acos(float v) => (float)Math.Acos(v);
    }

    /// <summary>Only the members Sim/ or the EditMode tests actually reference
    /// (CloakerWatch and its rule tests). Semantics match UnityEngine.Vector3.</summary>
    public struct Vector3 : IEquatable<Vector3>
    {
        public float x, y, z;

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 up => new Vector3(0f, 1f, 0f);
        public static Vector3 down => new Vector3(0f, -1f, 0f);
        public static Vector3 forward => new Vector3(0f, 0f, 1f);

        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 operator *(float s, Vector3 a) => a * s;
        public static bool operator ==(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        /// <summary>Unity's semantics: degrees, 0 for a degenerate input.</summary>
        public static float Angle(Vector3 from, Vector3 to)
        {
            float denominator = (float)Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < 1e-15f) return 0f;
            float dot = Mathf.Clamp(Dot(from, to) / denominator, -1f, 1f);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }

        public bool Equals(Vector3 other) => this == other;
        public override bool Equals(object obj) => obj is Vector3 other && this == other;
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => $"({x:0.0}, {y:0.0}, {z:0.0})";
    }
}

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class TestFixtureAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class SetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class TearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class IgnoreAttribute : Attribute
    {
        public IgnoreAttribute(string reason) { Reason = reason; }
        public string Reason { get; private set; }
    }
    [AttributeUsage(AttributeTargets.Method)] public sealed class OneTimeSetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class OneTimeTearDownAttribute : Attribute { }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    /// <summary>
    /// The assertions the EditMode suite uses, with NUnit's numeric semantics: AreEqual
    /// compares numbers by value across int/long/float/double rather than by boxed type,
    /// which is the one place a naive Equals() would silently diverge from Unity.
    /// </summary>
    public static class Assert
    {
        static string With(string msg, string detail)
            => string.IsNullOrEmpty(msg) ? detail : detail + "\n  " + msg;

        static bool IsNumeric(object o)
            => o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint
            || o is long || o is ulong || o is float || o is double || o is decimal;

        static string Show(object o)
        {
            if (o == null) return "null";
            if (o is string s) return "\"" + s + "\"";
            if (o is IEnumerable e && !(o is string))
            {
                var parts = new List<string>();
                foreach (var item in e) parts.Add(Show(item));
                return "[" + string.Join(", ", parts) + "]";
            }
            return Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture);
        }

        static bool ValueEquals(object expected, object actual)
        {
            if (expected == null || actual == null) return ReferenceEquals(expected, actual);
            if (IsNumeric(expected) && IsNumeric(actual))
            {
                double a = Convert.ToDouble(expected, System.Globalization.CultureInfo.InvariantCulture);
                double b = Convert.ToDouble(actual, System.Globalization.CultureInfo.InvariantCulture);
                return a.Equals(b);
            }
            return expected.Equals(actual);
        }

        public static void Fail(string message = "") => throw new AssertionException(
            string.IsNullOrEmpty(message) ? "Assert.Fail" : message);

        public static void Pass(string message = "") { }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!ValueEquals(expected, actual))
                throw new AssertionException(With(message,
                    "Expected: " + Show(expected) + "\n  But was:  " + Show(actual)));
        }

        public static void AreEqual(double expected, double actual, double delta, string message = null)
        {
            if (double.IsNaN(actual) || Math.Abs(expected - actual) > delta)
                throw new AssertionException(With(message,
                    "Expected: " + Show(expected) + " +/- " + Show(delta) + "\n  But was:  " + Show(actual)));
        }

        public static void AreNotEqual(object expected, object actual, string message = null)
        {
            if (ValueEquals(expected, actual))
                throw new AssertionException(With(message, "Expected not: " + Show(expected)));
        }

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition) throw new AssertionException(With(message, "Expected: True\n  But was:  False"));
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition) throw new AssertionException(With(message, "Expected: False\n  But was:  True"));
        }

        public static void IsNull(object o, string message = null)
        {
            if (o != null) throw new AssertionException(With(message, "Expected: null\n  But was:  " + Show(o)));
        }

        public static void IsNotNull(object o, string message = null)
        {
            if (o == null) throw new AssertionException(With(message, "Expected: not null"));
        }

        /// <summary>NUnit's string overload only — the protocol-catalog suite asserts that no
        /// sheet row is blank. A null reads as empty, same as NUnit.</summary>
        public static void IsNotEmpty(string value, string message = null)
        {
            if (string.IsNullOrEmpty(value))
                throw new AssertionException(With(message, "Expected: not empty string"));
        }

        static int Compare(object a, object b)
        {
            double x = Convert.ToDouble(a, System.Globalization.CultureInfo.InvariantCulture);
            double y = Convert.ToDouble(b, System.Globalization.CultureInfo.InvariantCulture);
            return x.CompareTo(y);
        }

        public static void Greater(object arg1, object arg2, string message = null)
        {
            if (Compare(arg1, arg2) <= 0)
                throw new AssertionException(With(message,
                    "Expected: greater than " + Show(arg2) + "\n  But was:  " + Show(arg1)));
        }

        public static void GreaterOrEqual(object arg1, object arg2, string message = null)
        {
            if (Compare(arg1, arg2) < 0)
                throw new AssertionException(With(message,
                    "Expected: at least " + Show(arg2) + "\n  But was:  " + Show(arg1)));
        }

        public static void Less(object arg1, object arg2, string message = null)
        {
            if (Compare(arg1, arg2) >= 0)
                throw new AssertionException(With(message,
                    "Expected: less than " + Show(arg2) + "\n  But was:  " + Show(arg1)));
        }

        public static void LessOrEqual(object arg1, object arg2, string message = null)
        {
            if (Compare(arg1, arg2) > 0)
                throw new AssertionException(With(message,
                    "Expected: at most " + Show(arg2) + "\n  But was:  " + Show(arg1)));
        }

        /// <summary>NUnit's member test: the item is somewhere in the collection. Added
        /// 2026-08-20 for Meta/PrimaryDifferentiationTests, which could not compile offline
        /// without it — the same gap CollectionAssert.AreEqual was added to close.</summary>
        public static void Contains(object expected, IEnumerable actual, string message = null)
        {
            if (actual != null)
            {
                foreach (object item in actual)
                    if (Equals(item, expected)) return;
            }
            throw new AssertionException(With(message,
                "Expected: collection containing " + Show(expected) +
                "\n  But was:  " + Show(actual)));
        }

        public static T Throws<T>(Action code, string message = null) where T : Exception
        {
            try { code(); }
            catch (T expected) { return expected; }
            catch (Exception other)
            {
                throw new AssertionException(With(message,
                    "Expected: " + typeof(T).Name + "\n  But was:  " + other.GetType().Name));
            }
            throw new AssertionException(With(message,
                "Expected: " + typeof(T).Name + "\n  But was:  no exception"));
        }
    }

    public static class CollectionAssert
    {
        public static void AreEqual(IEnumerable expected, IEnumerable actual, string message = null)
        {
            var exp = new List<object>();
            var act = new List<object>();
            if (expected != null) foreach (var x in expected) exp.Add(x);
            if (actual != null) foreach (var x in actual) act.Add(x);
            if (exp.Count != act.Count)
                throw new AssertionException(
                    (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                    "Expected count " + exp.Count + " but was " + act.Count);
            for (int i = 0; i < exp.Count; i++)
            {
                bool same = (exp[i] == null && act[i] == null)
                    || (exp[i] != null && exp[i].Equals(act[i]));
                if (!same)
                    throw new AssertionException(
                        (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                        "Differs at index " + i + ": expected " + exp[i] + " but was " + act[i]);
            }
        }

        public static void AreEquivalent(IEnumerable expected, IEnumerable actual, string message = null)
        {
            var exp = new List<object>();
            var act = new List<object>();
            if (expected != null) foreach (var x in expected) exp.Add(x);
            if (actual != null) foreach (var x in actual) act.Add(x);
            if (exp.Count != act.Count)
                throw new AssertionException(
                    (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                    "Expected count " + exp.Count + " but was " + act.Count);
            foreach (var e in exp)
            {
                int idx = act.FindIndex(a =>
                    (e == null && a == null) || (e != null && e.Equals(a)));
                if (idx < 0)
                    throw new AssertionException(
                        (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                        "Missing expected element: " + e);
                act.RemoveAt(idx);
            }
        }
    }

    public static class StringAssert
    {
        public static void Contains(string expected, string actual, string message = null)
        {
            if (actual == null || expected == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new AssertionException(
                    (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                    "Expected string to contain \"" + expected + "\"\n  But was:  " +
                    (actual == null ? "null" : "\"" + actual + "\""));
        }

        public static void StartsWith(string expected, string actual, string message = null)
        {
            if (actual == null || expected == null || !actual.StartsWith(expected, StringComparison.Ordinal))
                throw new AssertionException(
                    (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                    "Expected string to start with \"" + expected + "\"\n  But was:  " +
                    (actual == null ? "null" : "\"" + actual + "\""));
        }

        public static void DoesNotContain(string expected, string actual, string message = null)
        {
            if (actual != null && expected != null && actual.IndexOf(expected, StringComparison.Ordinal) >= 0)
                throw new AssertionException(
                    (string.IsNullOrEmpty(message) ? "" : message + "\n  ") +
                    "Expected string NOT to contain \"" + expected + "\"\n  But was:  \"" + actual + "\"");
        }
    }
}
