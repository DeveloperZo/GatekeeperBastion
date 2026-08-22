namespace GatekeepersProtocol.Game
{
    /// <summary>A destructive button that has to be pressed twice.
    ///
    /// The first press arms it against one named target and the second commits, provided the
    /// second lands inside the window and names the same target. Anything else disarms: picking
    /// a different row, pressing a different button, or simply waiting.
    ///
    /// It is a type rather than three fields on a controller because the rule is worth testing
    /// on its own — "armed for the realm you were looking at, not the one you have since
    /// clicked" is the part that would otherwise be got wrong quietly, and deleting the wrong
    /// world is not a mistake a player can undo from inside the game.
    ///
    /// Time is passed in. Nothing here reads a clock, so a test can step it.</summary>
    public sealed class ArmedAction
    {
        /// <summary>How long an arming stands. Long enough to read the label and decide, short
        /// enough that a button left armed and forgotten does not commit on a stray click much
        /// later.</summary>
        public const float WindowSeconds = 4f;

        string _target;
        float _armedAt;
        bool _armed;

        /// <summary>What is currently armed, or null. Null when nothing is.</summary>
        public string Target
        {
            get { return _armed ? _target : null; }
        }

        /// <summary>True when <paramref name="target"/> is armed and still inside the window.
        /// Reading this does not consume the arming — <see cref="Fire"/> does.</summary>
        public bool IsArmed(string target, float now)
        {
            if (!_armed || string.IsNullOrEmpty(target)) return false;
            if (_target != target) return false;
            return now - _armedAt <= WindowSeconds;
        }

        /// <summary>Arm against a target, replacing any previous arming.</summary>
        public void Arm(string target, float now)
        {
            _target = target;
            _armedAt = now;
            _armed = !string.IsNullOrEmpty(target);
        }

        public void Disarm()
        {
            _armed = false;
            _target = null;
        }

        /// <summary>The whole interaction in one call: true when this press is the committing
        /// one. A press that is not — first press, expired, or a different target — arms
        /// against this target instead and returns false, so pressing again commits.</summary>
        public bool Fire(string target, float now)
        {
            if (IsArmed(target, now))
            {
                Disarm();
                return true;
            }
            Arm(target, now);
            return false;
        }

        /// <summary>Drop an arming that has run out. Call once a frame so the label stops
        /// offering a confirmation the next press would not honour.</summary>
        public bool TickExpiry(float now)
        {
            if (!_armed) return false;
            if (now - _armedAt <= WindowSeconds) return false;
            Disarm();
            return true;
        }
    }
}
