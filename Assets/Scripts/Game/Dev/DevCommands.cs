#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: the Dev mode binding table — one row per action. Adding a command
    /// is one entry here plus a call on <see cref="GameController"/>. The F1 overlay lists these
    /// labels; they are the only discovery surface.</summary>
    public static class DevCommands
    {
        public enum Kind
        {
            ToggleOverlay,
            ReplayWave,
            ArmJumpToWave,
            ClearGatekeepers,
            RestoreHatch,
            ToggleInvulnerability,
            LoadoutPicker,
            GrantSalvage,
            ToggleGatekeeperReadout,
            StartCoopHost,
            StartCoopClient,
        }

        public readonly struct Binding
        {
            public readonly KeyCode Key;
            public readonly string Label;
            public readonly Kind Action;

            public Binding(KeyCode key, string label, Kind action)
            {
                Key = key;
                Label = label;
                Action = action;
            }
        }

        /// <summary>Every binding, in overlay order. F3 arms jump-to-wave; the next digit 1..Max
        /// completes it (≤2 keypresses).</summary>
        public static readonly Binding[] All =
        {
            new Binding(KeyCode.F1, "toggle this overlay", Kind.ToggleOverlay),
            new Binding(KeyCode.F2, "replay this wave", Kind.ReplayWave),
            new Binding(KeyCode.F3, "jump to wave N (then digit)", Kind.ArmJumpToWave),
            new Binding(KeyCode.F4, "kill all live gatekeepers", Kind.ClearGatekeepers),
            new Binding(KeyCode.F5, "refill hatch and player", Kind.RestoreHatch),
            new Binding(KeyCode.F6, "toggle player invulnerability", Kind.ToggleInvulnerability),
            new Binding(KeyCode.F7, "loadout picker (slot / weapon / tier)", Kind.LoadoutPicker),
            new Binding(KeyCode.F8, "grant 5,000 salvage", Kind.GrantSalvage),
            new Binding(KeyCode.F9, "toggle gatekeeper-state readout", Kind.ToggleGatekeeperReadout),
#if UNITY_EDITOR
            new Binding(KeyCode.F10, "host co-op session", Kind.StartCoopHost),
            new Binding(KeyCode.F11, "join 127.0.0.1", Kind.StartCoopClient),
#endif
        };

        /// <summary>Compact panel token for the shortcut strip (e.g. "F2 replay"). Kept here so
        /// the DEV section and the F1 overlay cannot disagree on which keys exist.</summary>
        public static string PanelDisplay(Binding b)
        {
            string key = b.Key.ToString(); // F1..F11
            string shortLabel = ShortLabel(b.Action);
            return string.IsNullOrEmpty(shortLabel) ? key : key + " " + shortLabel;
        }

        static string ShortLabel(Kind action)
        {
            switch (action)
            {
                case Kind.ToggleOverlay: return "overlay";
                case Kind.ReplayWave: return "replay";
                case Kind.ArmJumpToWave: return "jump";
                case Kind.ClearGatekeepers: return "clear";
                case Kind.RestoreHatch: return "refill";
                case Kind.ToggleInvulnerability: return "god";
                case Kind.LoadoutPicker: return "loadout";
                case Kind.GrantSalvage: return "salvage";
                case Kind.ToggleGatekeeperReadout: return "readout";
                case Kind.StartCoopHost: return "host";
                case Kind.StartCoopClient: return "join";
                default: return "";
            }
        }
    }
}
#endif
