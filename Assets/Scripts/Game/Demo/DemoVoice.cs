using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>The demo mission line runner — one live line, one queue slot, no timeScale
    /// write anywhere. Engine-free on purpose (the offline harness compiles and tests it):
    /// <see cref="DemoVoiceView"/> owns rendering, cursor, input sampling and the per-frame
    /// Tick; the Ship/Bastion Esc dispatchers own Esc; the net mirror owns the party RPC and
    /// registers <see cref="SetBroadcaster"/> on the host.
    ///
    /// Contract (Docs/Demo - Single/voice.md): Lock eats move/look/fire/interact until Continue —
    /// the input eating rides <see cref="LocksInput"/> plus the freed cursor (gun and
    /// Interactor both refuse while the cursor is unlocked). Subtitle leaves fight input
    /// live and auto-advances in <see cref="SubtitleSeconds"/>. A new Play preempts a live
    /// line, except the same-frame Docked collision: the first-loss line plays first and
    /// the other Lock line queues, starting on its Continue. The sim ticks throughout.</summary>
    public static class DemoVoice
    {
        const string Tag = "Demo";

        /// <summary>Auto-advance dwell for a Subtitle line with no duration of its own.</summary>
        public const float SubtitleSeconds = 4f;

        static bool _live;
        static DemoLine _current;
        static DemoVoiceMode _mode;
        static string _text;
        static bool _hasQueued;
        static DemoLine _queued;
        static DemoVoiceMode _queuedMode;
        static string _queuedText;
        static float _subtitleLeft;
        static int _frameToken;
        static int _currentStartToken;
        static bool _tableMissingLogged;
        static int _views;
        static Action<string, DemoVoiceMode> _broadcaster;

        /// <summary>True while a line is up. The view reads this to draw.</summary>
        public static bool IsLive => _live;

        /// <summary>True while a Lock line is live — FirstPersonController, ShipController
        /// and the gun/Interactor cursor gate all key off this.</summary>
        public static bool LocksInput => _live && _mode == DemoVoiceMode.Lock;

        /// <summary>Live line id, or null.</summary>
        public static string CurrentId => _live ? _current.Id : null;

        public static DemoVoiceMode CurrentMode => _mode;

        /// <summary>Speaker label for the live line ("Game" until a guide is named).</summary>
        public static string CurrentSpeaker => _live ? _current.Speaker : null;

        /// <summary>Copy for the live line — the table row's, or the caller's override
        /// (the embark-refuse id carries three reason strings under one id).</summary>
        public static string CurrentText => _live ? _text : null;

        /// <summary>Queued line id, or null. At most one line queues, and only behind the
        /// first-loss line's same-frame collision.</summary>
        public static string QueuedId => _hasQueued ? _queued.Id : null;

        /// <summary>True once the table-missing guard has logged. Test-visible so log-once
        /// is checkable without a log capture.</summary>
        public static bool TableMissingLogged => _tableMissingLogged;

        /// <summary>Start a line on this seat. Unknown id or missing table logs and no-ops —
        /// the demo may be mute; it may not be stuck. With no view in the scene the line is
        /// dropped with an error for the same reason: nothing would tick or Continue it.</summary>
        public static void Play(string id, DemoVoiceMode mode, string textOverride = null)
        {
            if (DemoLines.RowCount == 0)
            {
                if (!_tableMissingLogged)
                {
                    _tableMissingLogged = true;
                    Log.Event(Tag, "line table missing");
                }
                return;
            }
            if (!DemoLines.TryGet(id, out DemoLine row))
            {
                Log.Event(Tag, "line skipped — unknown id=" + id);
                return;
            }
            if (_views == 0)
            {
                // Warn, not Error: an Error here fails every Unity test that plays a line
                // in a fixture scene with no authored view (mute is legal there), and the
                // drop already guarantees not-stuck. The scene authoring miss still reads
                // loudly in the log.
                Log.Warn(Tag, "line dropped — no DemoVoiceView in scene id=" + id);
                return;
            }

            string text = textOverride != null ? textOverride : row.Text;

            if (_live)
            {
                bool sameFrame = _currentStartToken == _frameToken;
                bool bothLock = _mode == DemoVoiceMode.Lock && mode == DemoVoiceMode.Lock;
                if (sameFrame && bothLock
                    && _current.Id == DemoConversationIds.FirstHatchDown)
                {
                    // The only queue. A second queue-worthy Play replaces the queued line —
                    // one slot, latest wins, each arrival logged.
                    QueueLine(row, mode, text, _current.Id);
                    return;
                }
                if (sameFrame && bothLock && row.Id == DemoConversationIds.FirstHatchDown)
                {
                    // Order-independent: the first-loss line wins the frame even when it
                    // arrives second. The earlier line moves to the queue, not preempted.
                    QueueLine(_current, _mode, _text, row.Id);
                    StartLine(row, mode, text);
                    return;
                }
                if (sameFrame && _mode == DemoVoiceMode.Lock && mode == DemoVoiceMode.Subtitle)
                {
                    // A Subtitle raised on the same frame as a live Lock is a follow-on, not a
                    // replacement — the door line, then the offer it sets up. Queue it so the
                    // order the caller wrote is the order the player hears.
                    QueueLine(row, mode, text, _current.Id);
                    return;
                }
                Log.Event(Tag, "line preempted from=" + _current.Id + " to=" + row.Id);
            }

            StartLine(row, mode, text);
        }

        /// <summary>Host decision seat for a Party-scope line: play here, then broadcast so
        /// every connected seat plays it locally. Solo (no broadcaster registered) is just
        /// a local play.</summary>
        public static void PlayLocalAndBroadcast(string id, DemoVoiceMode mode)
        {
            Play(id, mode);
            if (_broadcaster != null) _broadcaster(id, mode);
        }

        /// <summary>Continue (click / Space / Enter, or the Esc path): end the live line and
        /// start the queued one if any. Ending is the same act on every path — closing a
        /// station is the model.</summary>
        public static void Continue()
        {
            if (!_live) return;
            EndCurrent(startQueued: true);
        }

        /// <summary>The scene Esc dispatchers call this first. A live Lock line ends (same
        /// as Continue); a live Subtitle dismisses. True means Esc is consumed — the
        /// dispatcher must not also open pause/settings this frame.</summary>
        public static bool TryHandleEscape()
        {
            if (!_live) return false;
            EndCurrent(startQueued: true);
            return true;
        }

        /// <summary>Per-frame drive from the view: frame boundary for the same-frame
        /// collision rule, and the Subtitle auto-advance clock.</summary>
        public static void Tick(float dt)
        {
            _frameToken++;
            if (!_live || _mode != DemoVoiceMode.Subtitle) return;
            _subtitleLeft -= dt;
            if (_subtitleLeft <= 0f)
                EndCurrent(startQueued: true);
        }

        /// <summary>The scene's view registers here (Awake/OnDestroy). The last view leaving
        /// ends any live and queued line — a line must not outlive the scene that showed it.</summary>
        public static void ViewAttached() => _views++;

        public static void ViewDetached()
        {
            if (_views > 0) _views--;
            if (_views > 0) return;
            if (_live) EndCurrent(startQueued: false);
            _hasQueued = false;
        }

        /// <summary>Host-side party broadcast, registered by the net mirror on spawn.
        /// Never set on a guest — guests play from the RPC and do not re-broadcast.</summary>
        public static void SetBroadcaster(Action<string, DemoVoiceMode> broadcaster)
            => _broadcaster = broadcaster;

        /// <summary>Unregister on despawn; only the registered owner clears itself.</summary>
        public static void ClearBroadcaster(Action<string, DemoVoiceMode> broadcaster)
        {
            if (_broadcaster == broadcaster) _broadcaster = null;
        }

        /// <summary>Exact start-log payload, exposed so tests pin the format the beat docs
        /// grep for without a log capture.</summary>
        public static string StartLogPayload(DemoLine row, DemoVoiceMode mode)
            => "line id=" + row.Id + " mode=" + mode + " scope=" + row.Scope
               + " speaker=" + row.Speaker;

        /// <summary>Full reset for tests: no live/queued line, guards re-armed, no views,
        /// no broadcaster.</summary>
        public static void ResetForTest()
        {
            _live = false;
            _hasQueued = false;
            _subtitleLeft = 0f;
            _frameToken = 0;
            _currentStartToken = -1;
            _tableMissingLogged = false;
            _views = 0;
            _broadcaster = null;
        }

        static void StartLine(DemoLine row, DemoVoiceMode mode, string text)
        {
            _live = true;
            _current = row;
            _mode = mode;
            _text = text;
            _currentStartToken = _frameToken;
            if (mode == DemoVoiceMode.Subtitle) _subtitleLeft = SubtitleSeconds;
            Log.Event(Tag, StartLogPayload(row, mode));
        }

        static void QueueLine(DemoLine row, DemoVoiceMode mode, string text, string behindId)
        {
            _hasQueued = true;
            _queued = row;
            _queuedMode = mode;
            _queuedText = text;
            Log.Event(Tag, "line queued id=" + row.Id + " behind=" + behindId);
        }

        static void EndCurrent(bool startQueued)
        {
            Log.Event(Tag, "line ended id=" + _current.Id);
            _live = false;
            if (!startQueued || !_hasQueued) return;
            _hasQueued = false;
            StartLine(_queued, _queuedMode, _queuedText);
        }
    }
}
