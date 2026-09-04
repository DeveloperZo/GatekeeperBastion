using System.Text;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the range's verbs as keys, live anywhere on the lane. One key per
    /// dimension — kind, distance, stand, clear, wave, shape — plus the fight's own start and
    /// dial keys for the draw and the two rules. Nothing here is looked at or walked to: the
    /// 2026-09-02 cut put a post on every mark, a console at the firing line and a terminal 90 m
    /// away, and Zo's play the next day was "I shouldnt have to walk to generate anything. Its
    /// a shooting range."
    ///
    /// The board's state is one line the readout prints — every key beside its current value
    /// — so the keys are read off the screen rather than remembered. Rebuilt only when a value
    /// changes; the readout repaints on its own clock.</summary>
    public sealed class RangeBoard : MonoBehaviour
    {
        const string Tag = "Range";

        /// <summary>Index 0 is the wave's own shape; the rest map to <see cref="WaveShape"/>.</summary>
        static readonly string[] ShapeLabels = { "own", "Mass", "Pack", "Convoy" };

        [SerializeField, Tooltip("The range console that owns the lane, the kind and the marks.")]
        RangeConsole _console;

        [SerializeField, Tooltip("The range's rules — the two toggles.")]
        RangeRules _rules;

        [SerializeField, Tooltip("The readout, reset when the lane is cleared.")]
        RangeReadout _readout;

        [SerializeField, Tooltip("The range's GameController; a draw goes to its sim.")]
        GameController _game;

        readonly StringBuilder _sb = new StringBuilder(192);
        int _distanceIndex;
        int _wave = 1;
        int _shape;

        string _line = "";
        bool _lineBuilt;
        GatekeeperKind _lineKind;
        int _lineDistance;
        int _lineWave;
        int _lineShape;
        bool _lineBypass;
        bool _lineRespawn;
        bool _lineLive;

        public int Wave
        {
            get { return _wave; }
        }

        /// <summary>The shape the next draw asks for, or null for the wave's own.</summary>
        public WaveShape? Shape
        {
            get { return _shape == 0 ? (WaveShape?)null : (WaveShape)(_shape - 1); }
        }

        /// <summary>Metres the next stand goes to; 0 when the scene carries no marks.</summary>
        public int Distance
        {
            get
            {
                var console = _console;
                if (console == null) return 0;
                int[] marks = console.Marks;
                if (marks.Length == 0) return 0;
                return marks[Mathf.Clamp(_distanceIndex, 0, marks.Length - 1)];
            }
        }

        bool WaveLive()
        {
            var game = _game;
            return game != null && game.Sim != null && game.Sim.Assault == AssaultState.Assault;
        }

        /// <summary>"Z KIND Hunter   X 30 m   C STAND   V CLEAR   |   B WAVE 3   N SHAPE own (Mass)
        /// T DRAW   |   + COOLDOWNS BYPASSED   - RESPAWN OFF". The readout's third line.</summary>
        public string Line
        {
            get
            {
                var console = _console;
                var rules = _rules;
                GatekeeperKind kind = console != null ? console.SelectedKind : GatekeeperKind.Default;
                int distance = Distance;
                bool bypass = rules != null && rules.CooldownBypass;
                bool respawn = rules != null && rules.RespawnOn;
                bool live = WaveLive();
                if (_lineBuilt && kind == _lineKind && distance == _lineDistance && _wave == _lineWave
                    && _shape == _lineShape && bypass == _lineBypass && respawn == _lineRespawn
                    && live == _lineLive)
                    return _line;

                _lineBuilt = true;
                _lineKind = kind;
                _lineDistance = distance;
                _lineWave = _wave;
                _lineShape = _shape;
                _lineBypass = bypass;
                _lineRespawn = respawn;
                _lineLive = live;

                _sb.Length = 0;
                _sb.Append(Controls.DisplayOf(ControlId.RangeKind)).Append(" KIND ").Append(kind);
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.RangeDistance)).Append(' ')
                    .Append(distance).Append(" m");
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.RangeStand)).Append(" STAND");
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.RangeClear)).Append(" CLEAR");
                _sb.Append("   |   ").Append(Controls.DisplayOf(ControlId.RangeWave)).Append(" WAVE ").Append(_wave);
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.RangeShape)).Append(" SHAPE ").Append(ShapeName());
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.PullDraw))
                    .Append(live ? " LIVE — clears on the last body" : " DRAW");
                _sb.Append("   |   ").Append(Controls.DisplayOf(ControlId.DialUp))
                    .Append(" COOLDOWNS ").Append(bypass ? "BYPASSED" : "REAL");
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.DialDown))
                    .Append(" RESPAWN ").Append(respawn ? "ON" : "OFF");
                _line = _sb.ToString();
                return _line;
            }
        }

        /// <summary>"Convoy", or "own (Pack)" when the wave's own shape is what will draw.</summary>
        string ShapeName()
        {
            if (_shape != 0) return ShapeLabels[_shape];
            return ShapeLabels[0] + " (" + WaveShapes.Name(WaveShapes.For(_wave)) + ")";
        }

        void Update()
        {
            // One verb per frame: two keys in one frame would log two lines for one intent.
            if (Controls.Down(ControlId.RangeKind)) { CycleKind(); return; }
            if (Controls.Down(ControlId.RangeDistance)) { CycleDistance(); return; }
            if (Controls.Down(ControlId.RangeStand)) { Stand(); return; }
            if (Controls.Down(ControlId.RangeClear)) { ClearLane(); return; }
            if (Controls.Down(ControlId.RangeWave)) { CycleWave(); return; }
            if (Controls.Down(ControlId.RangeShape)) { CycleShape(); return; }
            if (Controls.Down(ControlId.DialUp)) { ToggleBypass(); return; }
            if (Controls.Down(ControlId.DialDown)) { ToggleRespawn(); return; }
            if (Controls.Down(ControlId.PullDraw)) Draw();
        }

        void CycleKind()
        {
            var console = _console;
            if (console == null) return;
            console.CycleKind();
        }

        void CycleDistance()
        {
            var console = _console;
            if (console == null) return;
            int[] marks = console.Marks;
            if (marks.Length == 0) return;
            _distanceIndex = (_distanceIndex + 1) % marks.Length;
            Log.Event(Tag, "range board — " + marks[_distanceIndex] + " m");
        }

        void Stand()
        {
            var console = _console;
            if (console == null) return;
            int metres = Distance;
            if (metres <= 0)
            {
                Log.Warn(Tag, "stand refused — no marks in this scene");
                return;
            }
            GatekeeperKind kind = console.SelectedKind;
            if (console.SpawnAt(kind, metres) == null) return;
            Log.Event(Tag, "range target — " + kind + " at " + metres + " m");
        }

        /// <summary>Everything off the lane, and the numbers with it: a time-to-kill carried
        /// over from bodies that are no longer standing would be attributed to the next
        /// ones.</summary>
        void ClearLane()
        {
            var console = _console;
            if (console != null) console.ClearLane();
            var readout = _readout;
            if (readout != null) readout.ResetCounters();
        }

        void CycleWave()
        {
            _wave = _wave % WaveDirector.MaxWaves + 1;
            Log.Event(Tag, "range wave — " + _wave + " " + ShapeName() + " selected");
        }

        void CycleShape()
        {
            _shape = (_shape + 1) % ShapeLabels.Length;
            Log.Event(Tag, "range wave — " + _wave + " " + ShapeName() + " selected");
        }

        void ToggleBypass()
        {
            var rules = _rules;
            if (rules != null) rules.ToggleCooldownBypass();
        }

        void ToggleRespawn()
        {
            var rules = _rules;
            if (rules != null) rules.ToggleRespawn();
        }

        void Draw()
        {
            var game = _game;
            if (game == null || game.Sim == null)
            {
                Log.Warn(Tag, "range wave refused — no sim in this scene");
                return;
            }
            if (game.Net != null && game.Net.IsClientOnly)
            {
                Log.Event(Tag, "range wave refused — a guest does not draw");
                return;
            }
            if (WaveLive())
            {
                Log.Event(Tag, "range wave refused — a wave is live, wait for the last body");
                return;
            }
            string name = ShapeName();
            if (!game.Sim.DrawPracticeWave(_wave, Shape))
            {
                Log.Event(Tag, "range wave refused — see the sim line above");
                return;
            }
            Log.Event(Tag, "range wave — " + _wave + " " + name + " drawn");
        }
    }
}
