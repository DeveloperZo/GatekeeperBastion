#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>DEV-ONLY (partial of DevConsole): what the console does differently on the
    /// practice range.
    ///
    /// The range's own verbs — standing a target, cycling its kind, the two toggles, clearing
    /// the lane, drawing a wave — are keys on the <see cref="RangeBoard"/>, and the F3
    /// range menu with its nine digits was deleted when they landed. What is left here is the
    /// overlay: it opens on arrival, rows that mean nothing on a lane are hidden, and F4's
    /// clear also empties the lane's own ledger and readout. Drawn here rather than in
    /// DevConsole.cs, which is at its line cap.</summary>
    public sealed partial class DevConsole
    {
        RangeConsole _rangeCached;

        /// <summary>True when this scene has a range. No range, and nothing here applies.</summary>
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

        /// <summary>The binding rows, minus the ones that mean nothing here. A range has no
        /// Hatch to refill, no economy to grant into and hosts no party, so restore/salvage/
        /// host/join/checkpoint are lines of noise above the rows that matter. Returns the
        /// next y.</summary>
        float DrawBindingRows(float x, float y, float w)
        {
            bool range = HasRange;
            for (int i = 0; i < DevCommands.All.Length; i++)
            {
                var b = DevCommands.All[i];
                if (range && HiddenInRange(b.Action)) continue;
                string more = OpensMenu(b.Action) ? "  >" : "";
                GUI.Label(new Rect(x, y, w, 18f), $"{b.Key}  {b.Label}{more}", _label);
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

        /// <summary>F4's extra job on a lane. Killing the bodies is the shared meaning; the
        /// standing ledger, the respawn queue and the readout are the range's own, and a clear
        /// that left any of them standing would attribute the last kit's numbers to the next
        /// one — or refuse the next target for a cap the lane no longer holds.</summary>
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
