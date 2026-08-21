using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The hand-rolled JSON reader/writer the save documents share. Field-for-field compatible
    /// with what Unity's JsonUtility would produce, without taking a dependency on it or on
    /// Newtonsoft. Extracted from the single save document when that document became two, so
    /// both can serialize the same way without either owning the helpers.
    /// </summary>
    /// <remarks>
    /// Reads are key-addressed rather than positional: a missing key falls back, an unknown key
    /// is ignored. That is what lets a field be added to a document without a version bump when
    /// its absent value is also its correct default.
    /// </remarks>
    public static class JsonText
    {
        public static void AppendField(StringBuilder sb, string name, string value, bool leadingComma)
        {
            if (leadingComma) sb.Append(',');
            sb.Append('"').Append(name).Append("\":").Append(value);
        }

        public static void AppendLong(StringBuilder sb, string name, long value, bool leadingComma)
        {
            AppendField(sb, name, value.ToString(CultureInfo.InvariantCulture), leadingComma);
        }

        public static void AppendBool(StringBuilder sb, string name, bool value, bool leadingComma)
        {
            AppendField(sb, name, value ? "true" : "false", leadingComma);
        }

        /// <summary>A quoted string value. Player-typed text reaches this — a realm name may hold
        /// a quote or a backslash, and an unescaped one would truncate the document.</summary>
        public static void AppendString(StringBuilder sb, string name, string value, bool leadingComma)
        {
            if (leadingComma) sb.Append(',');
            sb.Append('"').Append(name).Append("\":\"");
            Escape(sb, value);
            sb.Append('"');
        }

        static void Escape(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        // Control characters have no business in a name and are dropped rather
                        // than escaped: nothing writes them and nothing should read them back.
                        if (c >= ' ') sb.Append(c);
                        break;
                }
            }
        }

        static string Unescape(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.IndexOf('\\') < 0) return raw;
            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c != '\\' || i + 1 >= raw.Length) { sb.Append(c); continue; }
                char n = raw[++i];
                switch (n)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }

        public static void AppendStringArray(StringBuilder sb, string name, string[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"');
                    Escape(sb, values[i] ?? "");
                    sb.Append('"');
                }
            }
            sb.Append(']');
        }

        public static void AppendBoolArray(StringBuilder sb, string name, bool[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(values[i] ? "true" : "false");
                }
            }
            sb.Append(']');
        }

        public static void AppendIntArray(StringBuilder sb, string name, int[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(values[i].ToString(CultureInfo.InvariantCulture));
                }
            }
            sb.Append(']');
        }

        public static string ExtractString(string json, string key, string fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return match.Success ? Unescape(match.Groups[1].Value) : fallback;
        }

        public static string[] ExtractStringArray(string json, string key)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return Array.Empty<string>();
            string inner = match.Groups[1].Value.Trim();
            if (inner.Length == 0) return Array.Empty<string>();
            string[] parts = inner.Split(',');
            int n = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length >= 2 && p[0] == '"' && p[p.Length - 1] == '"')
                    p = p.Substring(1, p.Length - 2);
                if (p.Length == 0) continue;
                parts[n++] = Unescape(p);
            }
            if (n == parts.Length) return parts;
            var trimmed = new string[n];
            Array.Copy(parts, trimmed, n);
            return trimmed;
        }

        public static void ExtractBoolArray(string json, string key, bool[] dest)
        {
            if (dest == null) return;
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return;
            string[] parts = match.Groups[1].Value.Split(',');
            for (int i = 0; i < dest.Length && i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                dest[i] = p == "true" || p == "1";
            }
        }

        public static void ExtractIntArray(string json, string key, int[] dest)
        {
            if (dest == null) return;
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return;
            string[] parts = match.Groups[1].Value.Split(',');
            for (int i = 0; i < dest.Length && i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length == 0) continue;
                long v;
                if (long.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    dest[i] = (int)v;
            }
        }

        public static bool ExtractBool(string json, string key, bool fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false|1|0)");
            if (!match.Success) return fallback;
            return match.Groups[1].Value == "true" || match.Groups[1].Value == "1";
        }

        public static long ExtractLong(string json, string key, long fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            return match.Success
                ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
                : fallback;
        }

        public static int ExtractInt(string json, string key, int fallback)
        {
            return (int)ExtractLong(json, key, fallback);
        }
    }
}
