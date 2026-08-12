using System;
using System.Collections.Generic;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Ids for every player-facing binding. Input sites call
    /// <see cref="Controls.Down"/> / <see cref="Controls.Held"/>; the shortcut panel
    /// iterates the same list. Adding a row here is what makes a key usable and visible.</summary>
    public enum ControlId
    {
        Move,
        Look,
        Sprint,
        Fire,
        AltFire,
        Reload,
        SlotPrimary,
        SlotSecondary,
        SlotDrone,
        SlotLast,
        SlotCycle,
        WeaponAbility,
        Interact,
        PullDraw,
        DialDown,
        DialUp,
        WheelRotatePrev,
        WheelRotateNext,
        FreeCursor,
        Confirm,
        TogglePanel,
        /// <summary>Bastion right-edge mission drawer (Tab). Omitted from the strip.</summary>
        ToggleMissionDrawer,
    }

    /// <summary>When a binding is live. Flags so Prep∩Assault is empty and a collision
    /// test can ask whether two bindings can fire in the same state.</summary>
    [Flags]
    public enum ControlContext
    {
        None = 0,
        Fight = 1 << 0,
        Prep = 1 << 1,
        Assault = 1 << 2,
        NearInteract = 1 << 3,
        Ship = 1 << 4,
        EndScreen = 1 << 5,
    }

    /// <summary>One row in the binding list. <see cref="DisplayOnly"/> rows (WASD, wheel)
    /// appear on the panel but have no KeyCode poll — axes/scroll stay on their Unity APIs.</summary>
    public readonly struct ControlBinding
    {
        public readonly ControlId Id;
        public readonly KeyCode Key;
        public readonly KeyCode AltKey;
        public readonly string Display;
        public readonly string Label;
        public readonly string Section;
        public readonly ControlContext Context;
        public readonly bool DisplayOnly;
        public readonly bool ShowOnPanel;

        public ControlBinding(
            ControlId id, KeyCode key, KeyCode altKey, string display, string label,
            string section, ControlContext context, bool displayOnly = false, bool showOnPanel = true)
        {
            Id = id;
            Key = key;
            AltKey = altKey;
            Display = display;
            Label = label;
            Section = section;
            Context = context;
            DisplayOnly = displayOnly;
            ShowOnPanel = showOnPanel;
        }
    }

    /// <summary>LOCAL-OWNER: the single player-facing binding list. Every input site and the
    /// shortcut panel read from here so a new key cannot ship invisible, and so two live
    /// contexts cannot silently share a KeyCode (the Prep dial/slot collision this exists to kill).</summary>
    public static class Controls
    {
        /// <summary>Fight-wide actions that stay live in both Prep and Assault.</summary>
        public const ControlContext FightAlways = ControlContext.Fight | ControlContext.Prep | ControlContext.Assault;

        static readonly ControlBinding[] Builtin =
        {
            new(ControlId.Move, KeyCode.None, KeyCode.None, "WASD", "move", "MOVE",
                FightAlways | ControlContext.Ship, displayOnly: true),
            new(ControlId.Look, KeyCode.None, KeyCode.None, "MOUSE", "look", "MOVE",
                FightAlways | ControlContext.Ship, displayOnly: true),
            new(ControlId.Sprint, KeyCode.LeftShift, KeyCode.RightShift, "SHIFT", "sprint", "MOVE",
                FightAlways | ControlContext.Ship),

            new(ControlId.Fire, KeyCode.Mouse0, KeyCode.None, "LMB", "fire", "FIGHT", FightAlways),
            new(ControlId.AltFire, KeyCode.Mouse1, KeyCode.None, "RMB", "cancel place", "FIGHT", FightAlways),
            new(ControlId.Reload, KeyCode.R, KeyCode.None, "R", "reload", "FIGHT", FightAlways),
            // Labels teach the mechanic (criterion 6): "weapons" not opaque "slots".
            new(ControlId.SlotPrimary, KeyCode.Alpha1, KeyCode.Keypad1, "1", "weapons", "FIGHT", FightAlways),
            new(ControlId.SlotSecondary, KeyCode.Alpha2, KeyCode.Keypad2, "2", "weapons", "FIGHT", FightAlways),
            new(ControlId.SlotDrone, KeyCode.Alpha3, KeyCode.Keypad3, "3", "weapons", "FIGHT", FightAlways),
            new(ControlId.SlotLast, KeyCode.Q, KeyCode.None, "Q", "last", "FIGHT", FightAlways),
            new(ControlId.SlotCycle, KeyCode.None, KeyCode.None, "WHEEL", "cycle", "FIGHT", FightAlways,
                displayOnly: true),
            new(ControlId.WeaponAbility, KeyCode.F, KeyCode.None, "F", "ability", "FIGHT", FightAlways),

            new(ControlId.PullDraw, KeyCode.T, KeyCode.None, "T", "siphon draw", "RUN", ControlContext.Prep),
            new(ControlId.DialDown, KeyCode.LeftBracket, KeyCode.None, "[", "siphon", "RUN", ControlContext.Prep),
            new(ControlId.DialUp, KeyCode.RightBracket, KeyCode.None, "]", "siphon", "RUN", ControlContext.Prep),
            // Same keys, Ship-only — Prep siphon and ARMORY wheel never share a live context.
            new(ControlId.WheelRotatePrev, KeyCode.LeftBracket, KeyCode.None, "[", "wheel", "ARMORY",
                ControlContext.Ship),
            new(ControlId.WheelRotateNext, KeyCode.RightBracket, KeyCode.None, "]", "wheel", "ARMORY",
                ControlContext.Ship),

            // Same key, non-overlapping contexts: hatch is a hold; ship is a tap-to-open.
            // "hold repair" is the panel teaching — prompts must not restate a different key.
            new(ControlId.Interact, KeyCode.E, KeyCode.None, "E", "hold repair", "HATCH",
                ControlContext.NearInteract),
            new(ControlId.Interact, KeyCode.E, KeyCode.None, "E", "interact", "SHIP",
                ControlContext.Ship),

            // Esc is owned by each scene's dispatcher (the Escape contract). The strip labels
            // match what Esc does: pause on the Bastion, settings on the Ship, return on end.
            new(ControlId.FreeCursor, KeyCode.Escape, KeyCode.None, "ESC", "pause", "MOVE",
                FightAlways),
            new(ControlId.FreeCursor, KeyCode.Escape, KeyCode.None, "ESC", "settings", "SHIP",
                ControlContext.Ship),

            new(ControlId.Confirm, KeyCode.Return, KeyCode.KeypadEnter, "ENTER", "return", "RUN",
                ControlContext.EndScreen),
            new(ControlId.Confirm, KeyCode.Space, KeyCode.None, "SPACE", "return", "RUN",
                ControlContext.EndScreen),
            new(ControlId.Confirm, KeyCode.Escape, KeyCode.None, "ESC", "return", "RUN",
                ControlContext.EndScreen),

            // Left-edge controls drawer (Bastion) / strip collapse (Ship). Omitted from the list.
            new(ControlId.TogglePanel, KeyCode.Backspace, KeyCode.None, "BKSP", "controls", "MOVE",
                FightAlways | ControlContext.Ship | ControlContext.EndScreen, showOnPanel: false),
            // Right-edge mission drawer — Bastion fight only.
            new(ControlId.ToggleMissionDrawer, KeyCode.Tab, KeyCode.None, "TAB", "mission", "RUN",
                FightAlways, showOnPanel: false),
        };

        static readonly List<ControlBinding> Extra = new List<ControlBinding>(4);
        static bool _collapsed;
        static bool _missionDrawerOpen;

        /// <summary>Built-in rows plus any test extras. Not a copy — do not mutate.</summary>
        public static IReadOnlyList<ControlBinding> All
        {
            get
            {
                if (Extra.Count == 0) return Builtin;
                var merged = new ControlBinding[Builtin.Length + Extra.Count];
                Array.Copy(Builtin, merged, Builtin.Length);
                Extra.CopyTo(merged, Builtin.Length);
                return merged;
            }
        }

        /// <summary>Bastion left drawer closed / Ship shortcut strip collapsed when true.</summary>
        public static bool Collapsed => _collapsed;

        /// <summary>Bastion right mission drawer open.</summary>
        public static bool MissionDrawerOpen => _missionDrawerOpen;

        /// <summary>Poll Backspace and flip the left drawer / strip collapse.</summary>
        public static void TickPanelToggle()
        {
            if (Down(ControlId.TogglePanel)) _collapsed = !_collapsed;
        }

        /// <summary>Poll Tab and flip the Bastion mission drawer.</summary>
        public static void TickMissionDrawerToggle()
        {
            if (Down(ControlId.ToggleMissionDrawer)) _missionDrawerOpen = !_missionDrawerOpen;
        }

        public static void SetCollapsed(bool collapsed) => _collapsed = collapsed;

        public static void SetMissionDrawerOpen(bool open) => _missionDrawerOpen = open;

        public static bool Down(ControlId id)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DevConsole.Suppresses(id)) return false;
#endif
            var rows = All;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b.Id != id || b.DisplayOnly) continue;
                if (b.Key != KeyCode.None && Input.GetKeyDown(b.Key)) return true;
                if (b.AltKey != KeyCode.None && Input.GetKeyDown(b.AltKey)) return true;
            }
            return false;
        }

        public static bool Held(ControlId id)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DevConsole.Suppresses(id)) return false;
#endif
            var rows = All;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b.Id != id || b.DisplayOnly) continue;
                if (b.Key != KeyCode.None && Input.GetKey(b.Key)) return true;
                if (b.AltKey != KeyCode.None && Input.GetKey(b.AltKey)) return true;
            }
            return false;
        }

        /// <summary>Mouse wheel for slot cycle — not a KeyCode, but owned here so the panel
        /// row and the gun cannot drift apart on whether wheel switching exists.</summary>
        public static float ScrollDelta() => Input.mouseScrollDelta.y;

        /// <summary>First display string for an id (e.g. prompts that must stay in sync with the list).</summary>
        public static string DisplayOf(ControlId id)
        {
            var rows = All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Id == id) return rows[i].Display;
            }
            return id.ToString();
        }

        /// <summary>Panel-visible bindings whose context intersects <paramref name="active"/>.</summary>
        public static void CollectPanelRows(ControlContext active, List<ControlBinding> into)
        {
            into.Clear();
            var rows = All;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (!b.ShowOnPanel) continue;
                if ((b.Context & active) == 0) continue;
                into.Add(b);
            }
        }

        /// <summary>Test hook: append a binding so criterion 4 can prove the panel reads the list.
        /// Returns a dispose action that removes it.</summary>
        public static Action PushForTest(ControlBinding binding)
        {
            Extra.Add(binding);
            return () => Extra.Remove(binding);
        }

        /// <summary>Test hook: drop every PushForTest row.</summary>
        public static void ClearTestBindings() => Extra.Clear();
    }
}
