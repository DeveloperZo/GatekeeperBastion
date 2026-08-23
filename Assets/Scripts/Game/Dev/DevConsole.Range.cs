#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>DEV-ONLY (partial of DevConsole): the practice range, as F3 then a digit.
    ///
    /// A sequence, not a chord (Zo, 2026-08-22). The console already works this way — F3 then a
    /// wave, F12 then a beat — and one armed key with a printed menu is easier to remember and
    /// easier to show than six modifier combinations.
    ///
    /// It borrows F3 rather than claiming a thirteenth key. <see cref="DevCommands.All"/> is
    /// fenced at exactly twelve bindings by a test that exists to stop this surface sprawling,
    /// and F3 means "jump to wave" — in a scene that never runs a Wave that key does nothing at
    /// all. So where there is a range, F3 arms the range; everywhere else it still jumps.
    ///
    /// Clearing the lane is F4, which already means "kill all live gatekeepers". Giving it a
    /// second name here would have been a second way to do one thing.</summary>
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

        /// <summary>Digits past the five distances.</summary>
        const int RangeDigitKind = 6;
        const int RangeDigitProtocol = 7;
        const int RangeDigitBypass = 8;
        const int RangeDigitRespawn = 9;

        RangeConsole _rangeCached;
        int _rangeKind;
        int _rangeProtocol;

        /// <summary>True when this scene has a range to drive. The whole surface hangs off
        /// this: no range, and F3 keeps its ordinary meaning.</summary>
        bool HasRange
        {
            get { return Range != null; }
        }

        /// <summary>The range's console, or null in every other scene. Cached like
        /// <c>Game</c> is, and re-resolved once the cached one has been destroyed.</summary>
        RangeConsole Range
        {
            get
            {
                if (_rangeCached == null) _rangeCached = FindFirstObjectByType<RangeConsole>();
                return _rangeCached;
            }
        }

        /// <summary>The binding rows, minus the ones that mean nothing here. A range runs no
        /// Wave, has no Hatch and hosts no party, so replay/jump/restore/host/join/checkpoint are
        /// six lines of noise above the five that matter. Drawn here rather than in
        /// DevConsole.cs, which is at its line cap. Returns the next y.</summary>
        float DrawBindingRows(float x, float y, float w)
        {
            bool range = HasRange;
            for (int i = 0; i < DevCommands.All.Length; i++)
            {
                var b = DevCommands.All[i];
                if (range && HiddenInRange(b.Action)) continue;
                string more = OpensMenu(b.Action) ? "  >" : "";
                GUI.Label(new Rect(x, y, w, 18f), $"{b.Key}  {LabelFor(b)}{more}", _label);
                y += 18f;
            }
            return y;
        }

        /// <summary>Keys that arm a second panel instead of doing something outright. Marked in
        /// the parent list so the player can see where the depth is without pressing every key
        /// to find out.</summary>
        static bool OpensMenu(DevCommands.Kind action)
        {
            return action == DevCommands.Kind.ArmJumpToWave
                || action == DevCommands.Kind.ArmDemoCheckpoint;
        }

        /// <summary>Commands with no meaning on a lane. Hidden rather than disabled: a row you
        /// cannot use is still a row you have to read past.</summary>
        static bool HiddenInRange(DevCommands.Kind action)
        {
            switch (action)
            {
                case DevCommands.Kind.ReplayWave:
                case DevCommands.Kind.RestoreHatch:
                case DevCommands.Kind.GrantSalvage:
                case DevCommands.Kind.StartCoopHost:
                case DevCommands.Kind.StartCoopClient:
                case DevCommands.Kind.ArmDemoCheckpoint:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The armed menu, printed so the digits do not have to be memorised — five
        /// distances and four toggles is more than anyone should hold in their head to shoot at
        /// something. Drawn here rather than in DevConsole.cs, which is at its line cap.</summary>
        void DrawRangeRows(float x, float y, float w)
        {
            // Only drawn while F3 is armed, so the heading names the parent key and the way
            // back rather than explaining how to get here.
            GUI.Label(new Rect(x, y, w, 18f), "F3  RANGE — press a digit, Esc back", _title);
            y += 20f;
            string[] rows =
            {
                "1  target at 5 m       6  cycle kind (now " + RangeKinds[_rangeKind] + ")",
                "2  target at 14 m      7  cycle protocol (now " + RangeProtocols[_rangeProtocol] + ")",
                "3  target at 30 m      8  cooldown bypass",
                "4  target at 60 m      9  respawn",
                "5  target at 90 m      F4 clears the lane",
            };
            for (int i = 0; i < rows.Length; i++)
            {
                GUI.Label(new Rect(x, y, w, 18f), rows[i], _label);
                y += 18f;
            }
        }

        /// <summary>Arm the range menu instead of the wave jump, where there is a range. A range
        /// never runs a Wave, so the jump has nothing to jump to. False elsewhere, and F3 keeps
        /// its ordinary meaning.</summary>
        bool TryArmRange()
        {
            if (!HasRange) return false;
            _awaitingRangeDigit = true;
            Log.Event(Tag, "range armed — press 1..9, Esc to cancel");
            return true;
        }

        /// <summary>The overlay row for a binding, renamed where the range has taken it over.
        /// A key that says "jump to wave" in a scene with no waves is a lie on screen.</summary>
        string LabelFor(DevCommands.Binding b)
        {
            return b.Action == DevCommands.Kind.ArmJumpToWave && HasRange
                ? "range menu (then digit)"
                : b.Label;
        }

        /// <summary>Esc or a digit while the range prompt is armed. True = consumed.</summary>
        bool TickRangeDigit()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _awaitingRangeDigit = false;
                RefreshCapture();
                Log.Event(Tag, "range cancelled");
                return true;
            }
            if (!TryReadDigit(RangeDigitRespawn, out int digit)) return false;

            // Keep suppress through the rest of this frame so PlayerGun does not also read the
            // digit as a weapon-slot switch — the same rule the wave and checkpoint digits use.
            _awaitingRangeDigit = false;
            _capture = CaptureKind.None;
            _suppressFightKeys = true;

            RangeConsole range = Range;
            if (range == null)
            {
                Log.Warn(Tag, "range digit ignored — no range in this scene");
                return true;
            }

            if (digit >= 1 && digit <= RangeDistances.Length)
            {
                range.SpawnAt(RangeKinds[_rangeKind], RangeDistances[digit - 1]);
                return true;
            }

            switch (digit)
            {
                case RangeDigitKind:
                    _rangeKind = (_rangeKind + 1) % RangeKinds.Length;
                    Log.Event(Tag, "range target — " + RangeKinds[_rangeKind]);
                    return true;

                case RangeDigitProtocol:
                    _rangeProtocol = (_rangeProtocol + 1) % RangeProtocols.Length;
                    range.GrantProtocol(RangeProtocols[_rangeProtocol]);
                    return true;

                case RangeDigitBypass:
                    {
                        var rules = FindFirstObjectByType<RangeRules>();
                        if (rules != null) rules.ToggleCooldownBypass();
                        return true;
                    }

                case RangeDigitRespawn:
                    {
                        var rules = FindFirstObjectByType<RangeRules>();
                        if (rules != null) rules.ToggleRespawn();
                        return true;
                    }
            }
            return true;
        }

        /// <summary>F4's extra job on a lane. Killing the bodies is the shared meaning; the
        /// queue and the readout are the range's own, and a clear that left either standing
        /// would attribute the last kit's numbers to the next one.</summary>
        void ClearRangeLane()
        {
            RangeConsole range = Range;
            if (range == null) return;
            range.ClearLane();
            var readout = FindFirstObjectByType<RangeReadout>();
            if (readout != null) readout.ResetCounters();
        }
    }
}
#endif
