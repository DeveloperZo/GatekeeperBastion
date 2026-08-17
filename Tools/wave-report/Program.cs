// Gatekeeper's Protocol - wave-report: mean live Gatekeepers per Wave, offline from session logs.
//
// Reconstructs live(t) from group/breach and kill lines (criterion 1-6). Mean live is the
// time-weighted integral; mean lifetime is Little's Law. Assets/ gains no counter - the log
// carries events, this tool does the arithmetic (Decisions.md).
//
//   Tools/wave-report/run.sh <log>...
//   Tools/wave-report/run.sh --summary <log>...
//   Tools/wave-report/run.sh --csv <log>...
//   Tools/wave-report/run.sh --html <out.html> <log>...
//
// Interactive page (parses logs in-browser): Docs/dashboard.html

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class WaveReport
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Dashboard labels runs shorter than this. wave-report prints every Run.</summary>
    public const double MinRunSeconds = 5.0;

    // Era cutoffs from filename date (Docs/Design/4-Data.md, wave-report section).
    static readonly DateTime EraHordeStart = new DateTime(2026, 8, 4);
    static readonly DateTime EraWeaponsStart = new DateTime(2026, 8, 8);
    static readonly DateTime EraHatchBankStart = new DateTime(2026, 8, 13);

    static readonly Regex TsRe = new Regex(
        @"^\[(\d{2}):(\d{2}):(\d{2})\.(\d{3})\]", RegexOptions.Compiled);

    static readonly Regex WaveStartRe = new Regex(
        @"^wave\s+(\d+)/(\d+)\s+\p{Pd}\s+(\d+)\s+(gatekeepers|boarders)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex WaveClearRe = new Regex(
        @"^wave\s+(\d+)/(\d+)\s+cleared\s+\p{Pd}.*?hatch\s+(\d+(?:\.\d+)?)/(\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex GroupLiveRe = new Regex(
        @"^group\s+\d+:\s+.+?\s+breached\s+at\s+\S+\s+\((\d+)/(\d+)\)\s+\p{Pd}\s+(\d+)\s+live\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex GroupRe = new Regex(
        @"^group\s+\d+:\s+(.+?)\s+breached\s+at\s+\S+\s+\((\d+)/(\d+)\)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex SoloBreachRe = new Regex(
        @"^boarder\s+(\d+)/(\d+)\s+breached\s+at\s+\S+\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex KillRe = new Regex(
        @"^killed\s+a\s+(Gatekeeper|Hunter|Ram|Boarder|boarder|gatekeeper|hunter|ram)\b",
        RegexOptions.Compiled);

    static readonly Regex HatchDistRe = new Regex(
        @"\bhatchDist=(-?\d+(?:\.\d+)?)\b",
        RegexOptions.Compiled);

    static readonly Regex SpawnDistRe = new Regex(
        @"\bspawnDist=(-?\d+(?:\.\d+)?)\b",
        RegexOptions.Compiled);

    /// <summary>Half the Bastion floor depth (SceneBuilder floor z-extent 52 m). Share of
    /// kills beyond this is the far-wall reading.</summary>
    const double FarWallMetres = 25.0;

    static readonly Regex KindCountRe = new Regex(
        @"(\d+)\s+(Gatekeepers?|Hunters?|Rams?|boarders?|gatekeepers?)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex TrackRe = new Regex(
        @"\btrack=(weapon|crew|fabrication|equipment)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex RunBeginsRe = new Regex(
        @"^run begins\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex LogDateRe = new Regex(
        @"(?:GatekeepersProtocol|GatekeeperBastion)_(\d{4})-(\d{2})-(\d{2})_",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    enum Mode { Table, Summary, Csv, Html }

    struct Purchase
    {
        public string Track;
        public string Line;
        public double Cost;
        public bool PreToken;
    }

    sealed class Segment
    {
        public int RunIndex;
        public string File;
        public string Label;
        public readonly List<int> Waves = new List<int>();
        public int Bodies;
        public double StartSec;
        public double EndSec;
        public double Integral;
        public int Live;
        public int PeakLive;
        public double LastT;
        public bool Open;
        public bool Lost;
        public bool Dev;
        public int? HatchAtClear;
        public int HatchMax;
        public bool ParseFailed;
        public string FailReason;
        public int SpawnDistAbsent;
        public readonly Dictionary<string, List<double>> HatchDistByKind =
            new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

        public double Seconds => Math.Max(0.0, EndSec - StartSec);

        public double MeanLive => Seconds > 0 ? Integral / Seconds : 0;

        public double MeanLifetime
        {
            get
            {
                if (Bodies <= 0 || Seconds <= 0) return 0;
                return MeanLive / (Bodies / Seconds);
            }
        }
    }

    sealed class RunState
    {
        public int Index;
        public string File;
        public bool Dev;
        public bool Closed;
        public bool SawDrawSinceWave;
        public bool HadWave;
        public bool PlayMode;
        public double BeginSec;
        public double EndSec = -1;
        public Segment Current;
        public readonly List<Segment> Segments = new List<Segment>();
        public readonly List<Purchase> Purchases = new List<Purchase>();
        public string BeginLine;

        public double DurationSec
        {
            get
            {
                if (EndSec < 0) return 0;
                return Math.Max(0.0, EndSec - BeginSec);
            }
        }
    }

    public static int Main(string[] args)
    {
        var mode = Mode.Table;
        string htmlOut = null;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--summary") mode = Mode.Summary;
            else if (a == "--csv") mode = Mode.Csv;
            else if (a == "--dashboard" || a == "--force-page")
            {
                Console.Error.WriteLine(
                    "wave-report: the dashboard is Docs/dashboard.html - open it and choose the Logs folder.");
                Console.Error.WriteLine(
                    "             (--dashboard snapshot generation was removed; use --html <out> for a file dump.)");
                return 2;
            }
            else if (a == "--html")
            {
                mode = Mode.Html;
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("wave-report: --html needs an output path");
                    return 2;
                }
                htmlOut = args[++i];
            }
            else if (a.StartsWith("-", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("wave-report: unknown flag " + a);
                return 2;
            }
            else files.Add(a);
        }

        if (files.Count == 0)
        {
            Console.Error.WriteLine(
                "usage: wave-report [--summary|--csv|--html <out>] <log>...");
            return 2;
        }

        var runs = new List<RunState>();
        var failures = new List<string>();

        foreach (string path in files)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine("wave-report: missing file " + path);
                return 2;
            }
            try
            {
                ParseFile(path, runs, failures);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("wave-report: parse crashed on " + path + ": " + ex.Message);
                return 2;
            }
        }

        foreach (string f in failures)
            Console.Error.WriteLine("PARSE FAILURE: " + f);

        var realRuns = runs.Where(r => r.Index > 0).ToList();
        var rows = realRuns.SelectMany(r => r.Segments).ToList();

        switch (mode)
        {
            case Mode.Summary:
                PrintSummary(rows, realRuns.Count);
                break;
            case Mode.Csv:
                PrintCsv(rows);
                break;
            case Mode.Html:
                File.WriteAllText(htmlOut, BuildHtml(realRuns, rows), new UTF8Encoding(false));
                Console.WriteLine("wrote " + htmlOut);
                break;
            default:
                PrintTable(rows);
                break;
        }

        return failures.Count > 0 ? 1 : 0;
    }

    static void ParseFile(string path, List<RunState> runs, List<string> failures)
    {
        RunState run = null;
        int runCounter = 0;
        double? dayBase = null;
        int dayOffset = 0;
        double lastRaw = 0;
        bool filePlayMode = false;

        foreach (string raw in File.ReadLines(path))
        {
            // Discriminator is stage=playmode, not the IntegrationCheck/AutoVerify tag (M38 criterion 5).
            if (!filePlayMode
                && raw.IndexOf("stage=playmode", StringComparison.OrdinalIgnoreCase) >= 0)
                filePlayMode = true;

            if (!TryParseLine(raw, out double rawSec, out string tag, out string msg))
                continue;

            if (dayBase == null)
            {
                dayBase = rawSec;
                lastRaw = rawSec;
            }
            else if (rawSec + 1.0 < lastRaw)
            {
                // Midnight wrap inside a long editor session.
                dayOffset += 86400;
            }
            lastRaw = rawSec;
            double t = rawSec + dayOffset;

            if (tag.Equals("Dev", StringComparison.OrdinalIgnoreCase)
                || msg.IndexOf("[Dev]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Only taint while the Run is live. Dev after RUN OVER / BASTION STRIPPED is
                // post-session tooling and must not exclude a clean reading (M13 / criterion 5).
                if (run != null && !run.Closed) run.Dev = true;
            }

            if (tag.Equals("Ship", StringComparison.OrdinalIgnoreCase)
                && msg.StartsWith("bought ", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePending(runs, path, ref run, ref runCounter);
                run.Purchases.Add(ParsePurchase(msg));
            }

            if (tag.Equals("Run", StringComparison.OrdinalIgnoreCase) && RunBeginsRe.IsMatch(msg))
            {
                CloseOpenSegment(run, t, lost: false, hatch: null, hatchMax: 0, failures);
                if (run != null && run.Index > 0 && run.EndSec < 0)
                    run.EndSec = t;
                runCounter++;
                run = new RunState
                {
                    Index = runCounter,
                    File = path,
                    BeginLine = msg,
                    BeginSec = t,
                    PlayMode = filePlayMode
                };
                AbsorbPendingPurchases(runs, run);
                runs.Add(run);
                continue;
            }

            if (run == null) continue;

            if (tag.Equals("Run", StringComparison.OrdinalIgnoreCase)
                && msg.StartsWith("pulled the siphon draw", StringComparison.OrdinalIgnoreCase))
            {
                run.SawDrawSinceWave = true;
                continue;
            }

            if (tag.Equals("Run", StringComparison.OrdinalIgnoreCase))
            {
                var wm = WaveStartRe.Match(msg);
                if (wm.Success)
                {
                    int wave = int.Parse(wm.Groups[1].Value, Inv);
                    int bodies = int.Parse(wm.Groups[3].Value, Inv);
                    bool chain = run.HadWave && !run.SawDrawSinceWave && run.Current != null && run.Current.Open;
                    if (chain)
                    {
                        run.Current.Waves.Add(wave);
                        run.Current.Bodies += bodies;
                        run.Current.Label = FormatLabel(run.Current.Waves);
                    }
                    else
                    {
                        CloseOpenSegment(run, t, lost: false, hatch: null, hatchMax: 0, failures);
                        var seg = new Segment
                        {
                            RunIndex = run.Index,
                            File = path,
                            StartSec = t,
                            LastT = t,
                            Open = true,
                            Dev = run.Dev,
                            Bodies = bodies
                        };
                        seg.Waves.Add(wave);
                        seg.Label = FormatLabel(seg.Waves);
                        run.Current = seg;
                        run.Segments.Add(seg);
                    }
                    run.HadWave = true;
                    run.SawDrawSinceWave = false;
                    continue;
                }

                var cm = WaveClearRe.Match(msg);
                if (cm.Success)
                {
                    int hatch = (int)Math.Round(double.Parse(cm.Groups[3].Value, Inv));
                    int hatchMax = (int)Math.Round(double.Parse(cm.Groups[4].Value, Inv));
                    CloseOpenSegment(run, t, lost: false, hatch: hatch, hatchMax: hatchMax, failures);
                    continue;
                }

                if (msg.StartsWith("RUN OVER", StringComparison.OrdinalIgnoreCase)
                    || msg.StartsWith("BASTION STRIPPED", StringComparison.OrdinalIgnoreCase))
                {
                    bool lost = msg.StartsWith("RUN OVER", StringComparison.OrdinalIgnoreCase);
                    if (run.Current != null && run.Current.Open)
                        CloseOpenSegment(run, t, lost: lost, hatch: null, hatchMax: 0, failures);
                    run.Closed = true;
                    run.EndSec = t;
                    continue;
                }
            }

            if (tag.Equals("Hatch", StringComparison.OrdinalIgnoreCase)
                && msg.StartsWith("HATCH BREACHED", StringComparison.OrdinalIgnoreCase))
            {
                if (run.Current != null && run.Current.Open)
                    CloseOpenSegment(run, t, lost: true, hatch: 0, hatchMax: 0, failures);
                continue;
            }

            if (IsSpawnTag(tag))
            {
                if (TryParseSpawn(msg, out int n, out string err))
                {
                    ApplyDelta(run, t, n);
                }
                else if (err != null)
                {
                    failures.Add(path + " @" + FormatClock(t) + ": " + err + " - " + msg);
                }
                continue;
            }

            if (tag.Equals("Combat", StringComparison.OrdinalIgnoreCase))
            {
                var km = KillRe.Match(msg);
                if (km.Success)
                {
                    ApplyDelta(run, t, -1);
                    RecordKillDistances(run, km.Groups[1].Value, msg);
                }
                continue;
            }
        }

        double endT = lastRaw + dayOffset;
        CloseOpenSegment(run, endT, lost: false, hatch: null, hatchMax: 0, failures);
        if (run != null && run.Index > 0 && run.EndSec < 0)
            run.EndSec = endT;

        foreach (var r in runs.Where(x => x.File == path))
        {
            if (filePlayMode) r.PlayMode = true;
            foreach (var s in r.Segments)
                s.Dev = r.Dev || s.Dev;
        }
    }

    static void EnsurePending(List<RunState> runs, string path, ref RunState run, ref int runCounter)
    {
        if (run != null) return;
        run = runs.LastOrDefault(r => r.File == path && r.Index == 0);
        if (run != null) return;
        run = new RunState { Index = 0, File = path };
        runs.Add(run);
    }

    static void AbsorbPendingPurchases(List<RunState> runs, RunState into)
    {
        var pending = runs.LastOrDefault(r => r.File == into.File && r.Index == 0);
        if (pending == null || pending.Purchases.Count == 0) return;
        into.Purchases.AddRange(pending.Purchases);
        pending.Purchases.Clear();
    }

    static Purchase ParsePurchase(string msg)
    {
        var tm = TrackRe.Match(msg);
        bool preToken = !tm.Success;
        string track = tm.Success
            ? tm.Groups[1].Value.ToLowerInvariant()
            : InferTrackFromName(msg);
        double cost = 0;
        var cm = Regex.Match(msg, @"\bfor\s+(\d+)\b");
        if (cm.Success) double.TryParse(cm.Groups[1].Value, NumberStyles.Integer, Inv, out cost);
        return new Purchase { Track = track, Line = msg, Cost = cost, PreToken = preToken };
    }

    /// <summary>
    /// Name -> track mapping for pre-token bought lines (M29 string-match, kept for old logs).
    /// Unknown names land in pre-token - never legacy.
    /// </summary>
    static int TrackOrder(string track)
    {
        switch (track)
        {
            case "weapon": return 0;
            case "crew": return 1;
            case "fabrication": return 2;
            case "equipment": return 3;
            case "pre-token": return 4;
            default: return 9;
        }
    }

    static string InferTrackFromName(string msg)
    {
        string m = msg ?? "";
        // Equipment / payloads first (Repair Rig used to share fabrication wording).
        if (ContainsAny(m, "Repair Rig", "Decoy", "equipment slot", "Equipment Slot"))
            return "equipment";
        if (ContainsAny(m, "Hatch Armor"))
            return "fabrication";
        if (ContainsAny(m, "Vitality", "Stamina", "Scavenging", "Luck"))
            return "crew";
        // Weapons: catalog names, wheel focus lines, and pre-wheel flat Damage/Fire Rate/Clip.
        if (ContainsAny(m,
                "MK-1", "MR-35", "BR-12", "SEC-D", "PRI-C", "VK-7",
                "Sidearm", "Rifle", "Shotgun",
                "focus ", "Damage L", "Fire Rate L", "Clip Size L", "Clip L", "Reload L"))
            return "weapon";
        return "pre-token";
    }

    static bool ContainsAny(string hay, params string[] needles)
    {
        foreach (string n in needles)
        {
            if (hay.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    static bool IsSpawnTag(string tag)
    {
        return tag.Equals("Gatekeepers", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("Boarders", StringComparison.OrdinalIgnoreCase);
    }

    static bool TryParseSpawn(string msg, out int n, out string err)
    {
        n = 0;
        err = null;

        var live = GroupLiveRe.Match(msg);
        if (live.Success)
        {
            n = SumComposition(msg);
            if (n <= 0)
            {
                err = "group line with live suffix but no readable composition count";
                return false;
            }
            return true;
        }

        var g = GroupRe.Match(msg);
        if (g.Success)
        {
            n = SumComposition(g.Groups[1].Value);
            if (n <= 0)
            {
                err = "group line composition unreadable";
                return false;
            }
            return true;
        }

        var s = SoloBreachRe.Match(msg);
        if (s.Success)
        {
            n = 1;
            return true;
        }

        if (msg.StartsWith("boarder provoked", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("boarder reached", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("boarder hitting", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("gatekeeper ", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("Gatekeeper "))
            return false;

        if (msg.IndexOf("breached", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.StartsWith("group ", StringComparison.OrdinalIgnoreCase)
            || SoloBreachRe.IsMatch(msg)
            || msg.StartsWith("boarder ", StringComparison.OrdinalIgnoreCase)
               && msg.IndexOf('/') >= 0)
        {
            err = "unrecognised Gatekeeper breach form";
            return false;
        }
        return false;
    }

    static int SumComposition(string text)
    {
        int sum = 0;
        foreach (Match m in KindCountRe.Matches(text))
            sum += int.Parse(m.Groups[1].Value, Inv);
        return sum;
    }

    static void RecordKillDistances(RunState run, string kindRaw, string msg)
    {
        if (run?.Current == null) return;
        var seg = run.Current;

        var sm = SpawnDistRe.Match(msg);
        if (sm.Success
            && double.TryParse(sm.Groups[1].Value, NumberStyles.Float, Inv, out double spawnDist)
            && spawnDist < 0)
        {
            seg.SpawnDistAbsent++;
        }

        var hm = HatchDistRe.Match(msg);
        if (!hm.Success) return;
        if (!double.TryParse(hm.Groups[1].Value, NumberStyles.Float, Inv, out double hatchDist))
            return;

        string kind = NormalizeKind(kindRaw);
        if (!seg.HatchDistByKind.TryGetValue(kind, out List<double> list))
        {
            list = new List<double>();
            seg.HatchDistByKind[kind] = list;
        }
        list.Add(hatchDist);
    }

    static string NormalizeKind(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Gatekeeper";
        switch (raw.ToLowerInvariant())
        {
            case "hunter": return "Hunter";
            case "ram": return "Ram";
            case "boarder":
            case "gatekeeper":
            default: return "Gatekeeper";
        }
    }

    static double Median(List<double> values)
    {
        if (values == null || values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        if ((sorted.Count & 1) == 1) return sorted[mid];
        return (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    static double ShareBeyond(List<double> values, double threshold)
    {
        if (values == null || values.Count == 0) return 0;
        int n = 0;
        for (int i = 0; i < values.Count; i++)
            if (values[i] > threshold) n++;
        return (double)n / values.Count;
    }

    static void ApplyDelta(RunState run, double t, int delta)
    {
        if (run?.Current == null || !run.Current.Open) return;
        var seg = run.Current;
        if (t > seg.LastT)
        {
            seg.Integral += seg.Live * (t - seg.LastT);
            seg.LastT = t;
        }
        seg.Live += delta;
        if (seg.Live < 0) seg.Live = 0;
        if (seg.Live > seg.PeakLive) seg.PeakLive = seg.Live;
    }

    static void CloseOpenSegment(RunState run, double t, bool lost, int? hatch, int hatchMax,
        List<string> failures)
    {
        if (run?.Current == null || !run.Current.Open) return;
        var seg = run.Current;
        if (t > seg.LastT)
        {
            seg.Integral += seg.Live * (t - seg.LastT);
            seg.LastT = t;
        }
        seg.EndSec = t;
        seg.Open = false;
        seg.Lost = lost;
        seg.Dev = run.Dev || seg.Dev;
        if (hatch.HasValue)
        {
            seg.HatchAtClear = hatch.Value;
            seg.HatchMax = hatchMax;
        }
        if (!lost && seg.Live != 0)
        {
            seg.ParseFailed = true;
            seg.FailReason = "segment cleared with live=" + seg.Live + " (expected 0)";
            failures.Add(seg.File + " run " + seg.RunIndex + " " + seg.Label + ": " + seg.FailReason);
        }
        run.Current = null;
    }

    static string FormatLabel(List<int> waves)
    {
        if (waves == null || waves.Count == 0) return "w?";
        if (waves.Count == 1) return "w" + waves[0];
        return "w" + string.Join("+", waves.Select(w => w.ToString(Inv)));
    }

    static string FormatClock(double sec)
    {
        var ts = TimeSpan.FromSeconds(sec % 86400);
        return ts.ToString(@"hh\:mm\:ss", Inv);
    }

    static bool TryParseLine(string raw, out double sec, out string tag, out string msg)
    {
        sec = 0;
        tag = null;
        msg = null;
        var tm = TsRe.Match(raw);
        if (!tm.Success) return false;
        int hh = int.Parse(tm.Groups[1].Value, Inv);
        int mm = int.Parse(tm.Groups[2].Value, Inv);
        int ss = int.Parse(tm.Groups[3].Value, Inv);
        int ms = int.Parse(tm.Groups[4].Value, Inv);
        sec = hh * 3600 + mm * 60 + ss + ms / 1000.0;

        int afterTs = tm.Length;
        string rest = raw.Substring(afterTs).TrimStart();
        if (rest.StartsWith("[", StringComparison.Ordinal))
        {
            int close = rest.IndexOf(']');
            if (close > 0) rest = rest.Substring(close + 1).TrimStart();
        }
        if (!rest.StartsWith("[", StringComparison.Ordinal)) return false;
        int tagClose = rest.IndexOf(']');
        if (tagClose <= 1) return false;
        tag = rest.Substring(1, tagClose - 1);
        msg = rest.Substring(tagClose + 1).TrimStart();
        return msg.Length > 0;
    }

    static void PrintTable(List<Segment> rows)
    {
        Console.WriteLine(string.Format(Inv,
            "{0,-6} {1,-14} {2,6} {3,8} {4,10} {5,9} {6,10} {7,10} {8}",
            "run", "wave", "bodies", "seconds", "meanLive", "peakLive", "meanLife", "hatch", "flags"));
        foreach (var s in rows)
        {
            string hatch = s.HatchAtClear.HasValue
                ? s.HatchAtClear.Value.ToString(Inv) + (s.HatchMax > 0 ? "/" + s.HatchMax.ToString(Inv) : "")
                : "-";
            string flags = "";
            if (s.Lost) flags += "lost";
            if (s.Dev) flags += (flags.Length > 0 ? "," : "") + "dev";
            if (s.ParseFailed) flags += (flags.Length > 0 ? "," : "") + "PARSE";
            Console.WriteLine(string.Format(Inv,
                "{0,-6} {1,-14} {2,6} {3,8:0.0} {4,10:0.00} {5,9} {6,10:0.00} {7,10} {8}",
                s.RunIndex, s.Label, s.Bodies, s.Seconds, s.MeanLive, s.PeakLive, s.MeanLifetime,
                hatch, flags));
        }

        PrintDistanceTable(rows);
    }

    static void PrintDistanceTable(List<Segment> rows)
    {
        bool any = rows.Any(s => s.HatchDistByKind.Count > 0);
        if (!any) return;

        Console.WriteLine();
        Console.WriteLine(string.Format(Inv,
            "{0,-6} {1,-14} {2,-11} {3,10} {4,10} {5,5}   (median hatchDist m; share >{6:0} m)",
            "run", "wave", "kind", "medHatch", "farShare", "n", FarWallMetres));

        int dropped = 0;
        foreach (var s in rows)
        {
            dropped += s.SpawnDistAbsent;
            foreach (string kind in new[] { "Gatekeeper", "Hunter", "Ram" })
            {
                if (!s.HatchDistByKind.TryGetValue(kind, out List<double> list) || list.Count == 0)
                    continue;
                Console.WriteLine(string.Format(Inv,
                    "{0,-6} {1,-14} {2,-11} {3,10:0.0} {4,10:0.00} {5,5}",
                    s.RunIndex, s.Label, kind, Median(list), ShareBeyond(list, FarWallMetres), list.Count));
            }
        }

        if (dropped > 0)
            Console.WriteLine(string.Format(Inv,
                "spawnDist absent (dropped from spawnDist reads): {0} kill row(s)", dropped));
    }

    static void PrintCsv(List<Segment> rows)
    {
        Console.WriteLine("run,wave,bodies,seconds,meanLive,peakLive,meanLifetime,hatch,hatchMax,lost,dev,file");
        foreach (var s in rows)
        {
            Console.WriteLine(string.Format(Inv,
                "{0},{1},{2},{3:0.###},{4:0.###},{5},{6:0.###},{7},{8},{9},{10},{11}",
                s.RunIndex, s.Label, s.Bodies, s.Seconds, s.MeanLive, s.PeakLive, s.MeanLifetime,
                s.HatchAtClear.HasValue ? s.HatchAtClear.Value.ToString(Inv) : "",
                s.HatchMax > 0 ? s.HatchMax.ToString(Inv) : "",
                s.Lost ? "1" : "0",
                s.Dev ? "1" : "0",
                EscapeCsv(s.File)));
        }
    }

    static string EscapeCsv(string s)
    {
        if (s.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    static void PrintSummary(List<Segment> rows, int runCount)
    {
        var clean = rows.Where(s => !s.Dev && !s.ParseFailed).ToList();
        int cleanRuns = clean.Select(s => s.File + "#" + s.RunIndex).Distinct().Count();
        Console.WriteLine(string.Format(Inv,
            "summary across {0} file-run(s) read; {1} clean Run(s) in mean",
            runCount, cleanRuns));
        Console.WriteLine(string.Format(Inv, "{0,-14} {1,10} {2,6}", "wave", "meanLive", "n"));

        var byLabel = clean
            .GroupBy(s => s.Label)
            .OrderBy(g => g.Min(s => s.Waves.Count > 0 ? s.Waves[0] : 99))
            .ThenBy(g => g.Key);
        foreach (var g in byLabel)
        {
            Console.WriteLine(string.Format(Inv, "{0,-14} {1,10:0.00} {2,6}",
                g.Key, g.Average(s => s.MeanLive), g.Count()));
        }
    }

    static string BuildHtml(List<RunState> runs, List<Segment> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>Gatekeeper's Protocol - wave report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font:14px/1.45 system-ui,Segoe UI,sans-serif;margin:24px;color:#1a1a1a;background:#f7f7f5}");
        sb.AppendLine("h1{font-size:22px;margin:0 0 8px} h2{font-size:16px;margin:28px 0 8px}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;background:#fff}");
        sb.AppendLine("th,td{border:1px solid #ccc;padding:6px 8px;text-align:left}");
        sb.AppendLine("th{background:#eee} .lost{color:#a30} .dev{color:#666}");
        sb.AppendLine(".bars{display:flex;align-items:flex-end;gap:6px;height:160px;margin:12px 0;padding:8px;background:#fff;border:1px solid #ccc}");
        sb.AppendLine(".bar{flex:1;background:#3a5;min-width:24px;position:relative}");
        sb.AppendLine(".bar span{position:absolute;bottom:100%;left:0;right:0;text-align:center;font-size:11px}");
        sb.AppendLine(".bar label{display:block;text-align:center;font-size:11px;margin-top:4px}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>How full the room got</h1>");
        sb.AppendLine("<p>Mean live Gatekeepers per Wave segment - offline from session logs. " +
                      "No CDN. Regenerated by <code>Tools/wave-report/run.sh --html</code>.</p>");

        double maxMean = rows.Count > 0 ? rows.Max(r => r.MeanLive) : 1;
        if (maxMean <= 0) maxMean = 1;
        sb.AppendLine("<h2>Per-Wave curve</h2><div class=\"bars\">");
        foreach (var s in rows)
        {
            double h = 100.0 * s.MeanLive / maxMean;
            string cls = s.Lost ? " lost" : "";
            sb.AppendFormat(Inv,
                "<div class=\"bar{0}\" style=\"height:{1:0.#}%\" title=\"run {2} {3}\"><span>{4:0.00}</span><label>{3}</label></div>\n",
                cls, h, s.RunIndex, Html(s.Label), s.MeanLive);
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>Run outcomes</h2><table><tr><th>Run</th><th>File</th><th>Segments</th><th>Dev</th><th>Outcome</th></tr>");
        foreach (var r in runs.Where(x => x.Index > 0))
        {
            string outcome = r.Segments.Any(s => s.Lost) ? "lost"
                : r.Segments.Count > 0 ? "cleared segments" : "no segments";
            sb.AppendFormat(Inv,
                "<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>\n",
                r.Index, Html(Path.GetFileName(r.File)), r.Segments.Count,
                r.Dev ? "yes" : "no", outcome);
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Spend split</h2><table><tr><th>Track</th><th>Count</th><th>Salvage</th></tr>");
        var spend = runs.SelectMany(r => r.Purchases)
            .GroupBy(p => p.Track)
            .OrderBy(g => TrackOrder(g.Key));
        foreach (var g in spend)
        {
            sb.AppendFormat(Inv, "<tr><td>{0}</td><td>{1}</td><td>{2:0}</td></tr>\n",
                Html(g.Key), g.Count(), g.Sum(p => p.Cost));
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Every row</h2><table><tr>" +
                      "<th>Run</th><th>Wave</th><th>Bodies</th><th>Seconds</th><th>Mean live</th>" +
                      "<th>Peak</th><th>Mean life</th><th>Hatch</th><th>Flags</th><th>File</th></tr>");
        foreach (var s in rows)
        {
            string flags = (s.Lost ? "lost " : "") + (s.Dev ? "dev " : "") + (s.ParseFailed ? "PARSE" : "");
            string hatch = s.HatchAtClear.HasValue ? s.HatchAtClear.Value.ToString(Inv) : "-";
            sb.AppendFormat(Inv,
                "<tr class=\"{8}\"><td>{0}</td><td>{1}</td><td>{2}</td><td>{3:0.0}</td><td>{4:0.00}</td>" +
                "<td>{5}</td><td>{6:0.00}</td><td>{7}</td><td>{8}</td><td>{9}</td></tr>\n",
                s.RunIndex, Html(s.Label), s.Bodies, s.Seconds, s.MeanLive, s.PeakLive, s.MeanLifetime,
                hatch, flags.Trim(), Html(Path.GetFileName(s.File)));
        }
        sb.AppendLine("</table>");
        sb.AppendLine("<script>");
        sb.AppendLine("document.querySelectorAll('tr.lost').forEach(function(r){r.style.background='#fff5f0';});");
        sb.AppendLine("</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    static string Html(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
