using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>One shortcut-strip row: keycap label(s), what they do, and the section they
    /// belong to. <see cref="Dev"/> rows exist only in dev builds and render visually
    /// separated (Negative accent).</summary>
    public readonly struct ShortcutRow
    {
        /// <summary>Keycap text, e.g. "E", "WASD", or "1 2 3" for a grouped binding.</summary>
        public readonly string Key;
        /// <summary>What the key does, e.g. "interact".</summary>
        public readonly string Description;
        /// <summary>Section heading the row sits under, e.g. "MOVE", "FIGHT", "DEV".</summary>
        public readonly string Section;
        /// <summary>Dev-only row: appended last, never interleaved, Negative accent.</summary>
        public readonly bool Dev;

        public ShortcutRow(string key, string description, string section, bool dev = false)
        {
            Key = key;
            Description = description;
            Section = section;
            Dev = dev;
        }
    }

    /// <summary>Builds structured shortcut rows from <see cref="Controls"/>. Rendering is
    /// <see cref="Ui.ShortcutPanelView"/>'s pooled keycap rows. The legacy string-line path
    /// (<see cref="FillLines"/>) remains only until WP-3/WP-4 port ShipHud and HudController
    /// to <see cref="FillRows"/>; the IMGUI Draw path is deleted.</summary>
    public static class ShortcutPanel
    {
        public const float BandWidth = 420f;
        public const float Margin = 12f;
        const float LineHeight = 20f;
        const float CollapsedTipHeight = 28f;

        public static float LastDrawnHeight { get; private set; }

        static readonly List<ControlBinding> RowBuf = new List<ControlBinding>(32);
        static readonly List<string> OrderBuf = new List<string>(8);
        static readonly Dictionary<string, string> SectionBuf = new Dictionary<string, string>(8);
        static readonly HashSet<string> SeenLabels = new HashSet<string>();
        static readonly StringBuilder LineBuf = new StringBuilder(128);

        /// <summary>Fills <paramref name="rows"/> with one structured row per distinct binding
        /// label, in <see cref="Controls"/> order: grouped labels ("weapons", "juice",
        /// "return") merge their keycaps into one row. Dev rows are appended last with
        /// <see cref="ShortcutRow.Dev"/> set, only in dev builds. Does not call
        /// TickPanelToggle — the caller polls first.</summary>
        public static void FillRows(ControlContext active, IList<ShortcutRow> rows, int dialLevel = -1)
        {
            rows.Clear();
            Controls.CollectPanelRows(active, RowBuf);
            SeenLabels.Clear();

            for (int i = 0; i < RowBuf.Count; i++)
            {
                var b = RowBuf[i];
                // One row per (section, label); grouped labels collect every keycap.
                if (!SeenLabels.Add(b.Section + "\0" + b.Label)) continue;

                string key = b.Display;
                if (IsGrouped(b.Label))
                {
                    LineBuf.Clear();
                    AppendGroupedDisplays(RowBuf, b.Section, b.Label);
                    key = LineBuf.ToString();
                }

                string description = b.Label;
                if (b.Label == "juice" && dialLevel >= 0)
                    description = "Intensity " + dialLevel;

                rows.Add(new ShortcutRow(key, description, b.Section));
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if ((active & (Controls.FightAlways | ControlContext.Ship | ControlContext.EndScreen)) != 0)
            {
                var all = DevCommands.All;
                for (int i = 0; i < all.Length; i++)
                {
                    // PanelDisplay is "<key> <short label>"; split so the keycap carries the key.
                    string key = all[i].Key.ToString();
                    string display = DevCommands.PanelDisplay(all[i]);
                    string description = display.Length > key.Length + 1
                        ? display.Substring(key.Length + 1)
                        : "";
                    rows.Add(new ShortcutRow(key, description, "DEV", dev: true));
                }
            }
#endif
        }

        /// <summary>Legacy string-line fill for uGUI. Consumed by ShipHud / HudController until
        /// WP-3/WP-4 port them to <see cref="FillRows"/>. Does not call TickPanelToggle — caller
        /// polls first.</summary>
        public static void FillLines(ControlContext active, IList<string> lines, int dialLevel = -1)
        {
            lines.Clear();
            Controls.CollectPanelRows(active, RowBuf);
            BuildSections(RowBuf, dialLevel);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if ((active & (Controls.FightAlways | ControlContext.Ship | ControlContext.EndScreen)) != 0)
                AppendDevSection();
#endif

            BuildDrawLines(lines, BandWidth - 28f);
            if (lines.Count > 0)
                lines.Add("BKSP hide keys");

            LastDrawnHeight = lines.Count == 0
                ? 0f
                : EstimateHeight(lines.Count, Screen.height) + Margin;
        }

        public static void EstimateCollapsedHeight()
        {
            LastDrawnHeight = CollapsedTipHeight + Margin;
        }

        static float EstimateHeight(int lineCount, float viewportHeight)
        {
            float maxH = Mathf.Max(120f, viewportHeight * 0.42f);
            float needed = 12f + lineCount * LineHeight + 8f;
            return Mathf.Min(needed, maxH);
        }

        static void BuildDrawLines(IList<string> target, float maxWidth)
        {
            for (int s = 0; s < OrderBuf.Count; s++)
            {
                string section = OrderBuf[s];
                string body = SectionBuf[section];
                AppendWrapped($"{section,-5} {body}", "      ", maxWidth, target);
            }
        }

        static void AppendWrapped(string first, string indent, float maxWidth, IList<string> target)
        {
            if (string.IsNullOrEmpty(first)) return;
            // Never call GUIStyle.CalcSize outside OnGUI — FillLines runs from LateUpdate
            // (ShipHud / HudController) and throws ArgumentException in PlayMode.
            if (ApproxWidth(first) <= maxWidth)
            {
                target.Add(first);
                return;
            }

            string[] parts = first.Split(new[] { " · " }, System.StringSplitOptions.None);
            if (parts.Length <= 1)
            {
                target.Add(first);
                return;
            }

            LineBuf.Clear();
            LineBuf.Append(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                string candidate = LineBuf + " · " + parts[i];
                if (ApproxWidth(candidate) <= maxWidth)
                {
                    LineBuf.Append(" · ").Append(parts[i]);
                    continue;
                }
                target.Add(LineBuf.ToString());
                LineBuf.Clear();
                LineBuf.Append(indent).Append(parts[i]);
            }
            if (LineBuf.Length > 0) target.Add(LineBuf.ToString());
        }

        /// <summary>Character-width estimate for wrapping outside OnGUI. LiberationSans ~7.2px
        /// at FxColors.Small; biased slightly wide so lines wrap early rather than clip.
        /// Dies with FillLines when WP-3/WP-4 port to FillRows.</summary>
        static float ApproxWidth(string s) =>
            string.IsNullOrEmpty(s) ? 0f : s.Length * 7.2f;

        static void BuildSections(List<ControlBinding> rows, int dialLevel)
        {
            SectionBuf.Clear();
            OrderBuf.Clear();

            for (int i = 0; i < rows.Count; i++)
            {
                string section = rows[i].Section;
                if (SectionBuf.ContainsKey(section)) continue;
                SectionBuf[section] = "";
                OrderBuf.Add(section);
            }

            for (int s = 0; s < OrderBuf.Count; s++)
            {
                string section = OrderBuf[s];
                LineBuf.Clear();
                SeenLabels.Clear();

                for (int i = 0; i < rows.Count; i++)
                {
                    var b = rows[i];
                    if (b.Section != section) continue;
                    if (!SeenLabels.Add(b.Label)) continue;

                    if (LineBuf.Length > 0) LineBuf.Append(" · ");

                    if (IsGrouped(b.Label))
                    {
                        AppendGroupedDisplays(rows, section, b.Label);
                        if (b.Label == "juice" && dialLevel >= 0)
                            LineBuf.Append(" Intensity ").Append(dialLevel);
                        else
                            LineBuf.Append(' ').Append(b.Label);
                    }
                    else
                    {
                        LineBuf.Append(b.Display);
                        if (!string.IsNullOrEmpty(b.Label) && b.Label != b.Display)
                            LineBuf.Append(' ').Append(b.Label);
                    }
                }

                SectionBuf[section] = LineBuf.ToString();
            }
        }

        static bool IsGrouped(string label) =>
            label == "weapons" || label == "juice" || label == "return";

        static void AppendGroupedDisplays(List<ControlBinding> rows, string section, string label)
        {
            bool first = true;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b.Section != section || b.Label != label) continue;
                if (!first) LineBuf.Append(' ');
                LineBuf.Append(b.Display);
                first = false;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static void AppendDevSection()
        {
            LineBuf.Clear();
            var all = DevCommands.All;
            for (int i = 0; i < all.Length; i++)
            {
                if (i > 0) LineBuf.Append(" · ");
                LineBuf.Append(DevCommands.PanelDisplay(all[i]));
            }
            if (LineBuf.Length == 0) return;
            SectionBuf["DEV"] = LineBuf.ToString();
            if (!OrderBuf.Contains("DEV")) OrderBuf.Add("DEV");
        }
#endif
    }
}
