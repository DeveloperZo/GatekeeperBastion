using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// DORMANT in v0: the tether cannot be cut at will, so nothing starts this spool.
    /// Kept (with its RunPhase.Spooling/Escaped enum slots) until the escape-window
    /// design lands, at which point RunSim re-wires it.
    /// Countdown timer for the escape sequence after cutting the tether.
    /// Start it with a duration, Tick it with sim deltaTime, watch Progress/IsComplete.
    /// </summary>
    public sealed class EscapeSpool
    {
        /// <summary>Total spool duration in seconds (set by Start).</summary>
        public float Duration { get; private set; }

        /// <summary>Seconds elapsed since Start, clamped to Duration.</summary>
        public float Elapsed { get; private set; }

        /// <summary>True once Start has been called (stays true after completion).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Normalized progress in [0, 1]. 0 before Start.</summary>
        public float Progress
        {
            get { return IsActive && Duration > 0f ? Elapsed / Duration : 0f; }
        }

        /// <summary>True once Elapsed has reached Duration.</summary>
        public bool IsComplete
        {
            get { return IsActive && Elapsed >= Duration; }
        }

        /// <summary>Begin (or restart) the spool with the given duration in seconds.</summary>
        public void Start(float duration)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException("duration");
            Duration = duration;
            Elapsed = 0f;
            IsActive = true;
        }

        /// <summary>Advance the spool. Ignored before Start and after completion.</summary>
        public void Tick(float dt)
        {
            if (!IsActive || dt <= 0f) return;
            Elapsed = Math.Min(Duration, Elapsed + dt);
        }
    }
}
