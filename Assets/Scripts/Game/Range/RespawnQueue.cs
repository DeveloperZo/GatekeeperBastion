using System.Collections.Generic;

namespace GatekeepersProtocol.Game
{
    /// <summary>Bodies waiting to come back, and when.
    ///
    /// The range's respawn mode returns a killed Gatekeeper to its marker after a stated delay,
    /// so a burst can be repeated without walking back to the console. The bookkeeping is here,
    /// as plain C# with time passed in, because "which bodies are due" is the part that goes
    /// quietly wrong — a body queued twice, or one that comes back after the lane was cleared —
    /// and neither shows up as anything but a confusing range.
    ///
    /// It holds ids, not actors. What an id means belongs to the caller that spawned it.</summary>
    public sealed class RespawnQueue
    {
        /// <summary>Long enough to see the body drop and read the readout, short enough that
        /// repeating a burst does not become waiting.</summary>
        public const float DefaultDelaySeconds = 2f;

        readonly Dictionary<int, float> _due = new Dictionary<int, float>();
        readonly List<int> _ready = new List<int>(8);

        float _delay = DefaultDelaySeconds;

        /// <summary>Off by default: a range that resurrects everything the moment you enter it
        /// is harder to reason about than one that stays where you left it.</summary>
        public bool Enabled { get; set; }

        /// <summary>Seconds between a body going down and returning. Never negative; zero is
        /// allowed and means the next tick brings it back.</summary>
        public float Delay
        {
            get { return _delay; }
            set { _delay = value < 0f ? 0f : value; }
        }

        /// <summary>How many bodies are waiting to return.</summary>
        public int PendingCount
        {
            get { return _due.Count; }
        }

        public bool IsPending(int id)
        {
            return _due.ContainsKey(id);
        }

        /// <summary>A body went down. Ignored while respawn is off, so turning the mode on does
        /// not resurrect everything killed before it was switched on — the queue only holds
        /// what was killed under the rule the player had set.</summary>
        public bool NoteDown(int id, float now)
        {
            if (!Enabled) return false;
            // Re-queueing would push the return further out every time a corpse was hit again.
            if (_due.ContainsKey(id)) return false;
            _due[id] = now + _delay;
            return true;
        }

        /// <summary>Ids whose delay has elapsed, removed from the queue as they are handed over.
        /// The returned list is reused between calls — read it before calling again.</summary>
        public List<int> TakeDue(float now)
        {
            _ready.Clear();
            if (_due.Count == 0) return _ready;
            foreach (var kv in _due)
                if (now >= kv.Value)
                    _ready.Add(kv.Key);
            for (int i = 0; i < _ready.Count; i++)
                _due.Remove(_ready[i]);
            return _ready;
        }

        /// <summary>Drop one body's pending return — it has been despawned, or the lane was
        /// cleared out from under it.</summary>
        public void Forget(int id)
        {
            _due.Remove(id);
        }

        /// <summary>Clearing the lane clears what is waiting to come back to it. Without this,
        /// a clear would be followed by bodies arriving from the run before it.</summary>
        public void Clear()
        {
            _due.Clear();
            _ready.Clear();
        }
    }
}
