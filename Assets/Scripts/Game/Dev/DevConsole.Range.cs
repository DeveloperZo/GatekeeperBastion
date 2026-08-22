#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>DEV-ONLY (partial of DevConsole): the keys that make the practice range usable.
    ///
    /// These are NOT in <see cref="DevCommands.All"/>. That table is fenced at exactly twelve
    /// bindings, F1 through F12, by a test that exists to stop the dev surface sprawling — and
    /// range keys are not a general dev facility, they are meaningless in every other scene. So
    /// they live here, behind Alt, and no-op wherever there is no range.
    ///
    /// Alt is a modifier rather than a bare key because the range still runs a live
    /// GameController: bare digits are weapon slots and bare letters are fight controls, and a
    /// range that fought the player's own keybinds for the number row would be worse than no
    /// shortcut (Docs/Milestones — a range you can fight in).</summary>
    public sealed partial class DevConsole
    {
        /// <summary>Kinds a target can be. The shipped three — the range exercises the game's
        /// Gatekeepers or it is not measuring the game.</summary>
        static readonly GatekeeperKind[] RangeKinds =
        {
            GatekeeperKind.Default, GatekeeperKind.Hunter, GatekeeperKind.Thief
        };

        /// <summary>Marks down the lane, matching the markers the scene builder authored.</summary>
        static readonly int[] RangeDistances = { 5, 14, 30, 60, 90 };

        static readonly BoarderProtocolId[] RangeProtocols =
        {
            BoarderProtocolId.Default, BoarderProtocolId.Veil, BoarderProtocolId.Forge
        };

        RangeConsole _rangeCached;
        int _rangeKind;
        int _rangeProtocol;

        /// <summary>The range's console, or null in every other scene. Cached like
        /// <c>Game</c> is, and re-resolved when the cached one has been destroyed.</summary>
        RangeConsole Range
        {
            get
            {
                if (_rangeCached == null) _rangeCached = FindFirstObjectByType<RangeConsole>();
                return _rangeCached;
            }
        }

        /// <summary>Alt-chords, read once a frame. Returns true when one fired, so the caller can
        /// stop before a bare-key handler sees the same frame.</summary>
        bool TickRangeKeys()
        {
            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) return false;
            RangeConsole range = Range;
            if (range == null) return false;

            if (Input.GetKeyDown(KeyCode.T))
            {
                _rangeKind = (_rangeKind + 1) % RangeKinds.Length;
                Log.Event(Tag, "range target — " + RangeKinds[_rangeKind]);
                return true;
            }

            for (int i = 0; i < RangeDistances.Length; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i) && !Input.GetKeyDown(KeyCode.Keypad1 + i))
                    continue;
                range.SpawnAt(RangeKinds[_rangeKind], RangeDistances[i]);
                return true;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                range.ClearLane();
                var readout = FindFirstObjectByType<RangeReadout>();
                // The numbers describe the bodies that just left; carrying them past a clear
                // would attribute the last kit's time-to-kill to the next one.
                if (readout != null) readout.ResetCounters();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                _rangeProtocol = (_rangeProtocol + 1) % RangeProtocols.Length;
                range.GrantProtocol(RangeProtocols[_rangeProtocol]);
                return true;
            }

            var rules = FindFirstObjectByType<RangeRules>();
            if (rules == null) return false;

            if (Input.GetKeyDown(KeyCode.B))
            {
                rules.ToggleCooldownBypass();
                return true;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                rules.ToggleRespawn();
                return true;
            }
            return false;
        }
    }
}
#endif
