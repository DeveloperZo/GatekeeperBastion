using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Builds shortcut-panel line text from <see cref="Controls"/>. Rendering is
    /// <see cref="ShortcutPanelView"/>; Bastion HUD still calls legacy Draw until it ports.</summary>
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

        static GUIStyle _wrap;

        /// <summary>Legacy IMGUI draw for Bastion until HudController ports.</summary>
        public static void Draw(
            float vw, float vh, ControlContext active, GUIStyle labelStyle,
            int dialLevel = -1, GUIStyle boxStyle = null)
        {
            Controls.TickPanelToggle();
            var lines = new List<string>(16);
            if (Controls.Collapsed)
            {
                DrawCollapsedImgui(vw, vh, labelStyle, boxStyle);
                return;
            }

            FillLines(active, lines, dialLevel);
            if (lines.Count == 0)
            {
                LastDrawnHeight = 0f;
                return;
            }

            var style = WrapStyle(labelStyle);
            float x = Margin;
            float bottom = vh - Margin;
            float height = EstimateHeight(lines.Count, vh);
            var panel = new Rect(x, bottom - height, BandWidth, height);
            GUI.Box(panel, GUIContent.none, boxStyle ?? GUI.skin.box);

            float y = panel.y + 6f;
            int shown = Mathf.Min(VisibleLineCount(height), lines.Count);
            for (int i = 0; i < shown; i++)
            {
                GUI.Label(new Rect(panel.x + 10f, y, panel.width - 16f, LineHeight), lines[i], style);
                y += LineHeight;
            }
            if (shown < lines.Count)
            {
                GUI.Label(new Rect(panel.x + 10f, panel.yMax - LineHeight - 4f, panel.width - 16f, LineHeight),
                    "BKSP hide keys", style);
            }

            LastDrawnHeight = height + Margin;
        }

        static void DrawCollapsedImgui(float vw, float vh, GUIStyle labelStyle, GUIStyle boxStyle)
        {
            float x = Margin;
            float bottom = vh - Margin;
            const float tipW = 168f;
            var tip = new Rect(x, bottom - CollapsedTipHeight, tipW, CollapsedTipHeight);
            GUI.Box(tip, GUIContent.none, boxStyle ?? GUI.skin.box);
            GUI.Label(tip, "  BKSP — show keys", WrapStyle(labelStyle));
            LastDrawnHeight = CollapsedTipHeight + Margin;
        }

        /// <summary>Fills <paramref name="lines"/> for uGUI. Does not call TickPanelToggle — caller polls first.</summary>
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

        static int VisibleLineCount(float height) =>
            Mathf.Max(1, Mathf.FloorToInt((height - 20f) / LineHeight));

        static GUIStyle WrapStyle(GUIStyle baseStyle)
        {
            if (_wrap == null || _wrap.fontSize != (baseStyle != null ? baseStyle.fontSize : 14))
            {
                _wrap = new GUIStyle(baseStyle ?? GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = Mathf.Max(14, baseStyle != null ? baseStyle.fontSize : 14),
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
            }
            return _wrap;
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
        /// at Theme.Small; biased slightly wide so lines wrap early rather than clip.</summary>
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
                        LineBuf.Append(' ').Append(b.Label);
                        if (b.Label == "siphon" && dialLevel >= 0)
                            LineBuf.Append(' ').Append('L').Append(dialLevel);
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
            label == "weapons" || label == "siphon" || label == "return";

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
