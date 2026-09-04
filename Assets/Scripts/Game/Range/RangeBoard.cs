using System.Text;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the range's verbs as keys, live anywhere on the lane. Three
    /// dimensions and a draw (Zo, 2026-09-03): the kind on one key, the wave size on another,
    /// the fight's own start key to draw, and the breach distance on the
    /// <see cref="RangeDistanceTerminal"/> beside the firing line, 20 m unless it is pressed.
    /// Nothing is walked to; nothing bypasses a cooldown — the cooldowns are reset when
    /// the wave starts, so every wave is judged from a full kit at the shipped wait.
    ///
    /// The board's state is one line the readout prints — every key beside its current value
    /// — so the keys are read off the screen rather than remembered. Rebuilt only when a value
    /// changes; the readout repaints on its own clock.</summary>
    public sealed class RangeBoard : MonoBehaviour
    {
        const string Tag = "Range";

        [SerializeField, Tooltip("The range console that owns the selected kind.")]
        RangeConsole _console;

        [SerializeField, Tooltip("The range's rules — the cooldown reset on a draw.")]
        RangeRules _rules;

        [SerializeField, Tooltip("The readout, reset when a wave is drawn so its numbers are that wave's.")]
        RangeReadout _readout;

        [SerializeField, Tooltip("The range's GameController; a draw goes to its sim.")]
        GameController _game;

        [SerializeField, Tooltip("The distance terminal beside the firing line; its spawn point is handed to the spawner on a draw.")]
        RangeDistanceTerminal _distance;

        readonly StringBuilder _sb = new StringBuilder(160);
        PracticeSize _size = PracticeSize.Normal;

        string _line = "";
        bool _lineBuilt;
        GatekeeperKind _lineKind;
        PracticeSize _lineSize;
        int _lineMetres;
        int _lineAlive = -1;

        public PracticeSize Size
        {
            get { return _size; }
        }

        bool WaveLive()
        {
            var game = _game;
            return game != null && game.Sim != null && game.Sim.Assault == AssaultState.Assault;
        }

        int Alive()
        {
            var game = _game;
            if (game == null || game.Spawner == null || !WaveLive()) return -1;
            return game.Spawner.AliveCount;
        }

        /// <summary>"Z KIND Hunter   X SIZE normal (11)   E SPAWN 20 m   T DRAW", or while a
        /// wave is live "T LIVE — 7 standing". The readout's third line.</summary>
        public string Line
        {
            get
            {
                var console = _console;
                GatekeeperKind kind = console != null ? console.SelectedKind : GatekeeperKind.Default;
                int alive = Alive();
                int metres = Metres();
                if (_lineBuilt && kind == _lineKind && _size == _lineSize && alive == _lineAlive
                    && metres == _lineMetres)
                    return _line;

                _lineBuilt = true;
                _lineKind = kind;
                _lineSize = _size;
                _lineMetres = metres;
                _lineAlive = alive;

                _sb.Length = 0;
                _sb.Append(Controls.DisplayOf(ControlId.RangeKind)).Append(" KIND ").Append(kind);
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.RangeSize)).Append(" SIZE ")
                    .Append(PracticeWave.Name(_size)).Append(" (").Append(PracticeWave.Count(_size)).Append(')');
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.Interact)).Append(" SPAWN ")
                    .Append(metres).Append(" m");
                _sb.Append("   ").Append(Controls.DisplayOf(ControlId.PullDraw));
                if (alive >= 0)
                    _sb.Append(" LIVE — ").Append(alive).Append(" standing");
                else
                    _sb.Append(" DRAW");
                _line = _sb.ToString();
                return _line;
            }
        }

        int Metres()
        {
            var distance = _distance;
            return distance != null ? distance.SelectedMetres : PracticeWave.DefaultMetres;
        }

        void Update()
        {
            // One verb per frame: two keys in one frame would log two lines for one intent.
            if (Controls.Down(ControlId.RangeKind)) { CycleKind(); return; }
            if (Controls.Down(ControlId.RangeSize)) { CycleSize(); return; }
            if (Controls.Down(ControlId.PullDraw)) Draw();
        }

        void CycleKind()
        {
            var console = _console;
            if (console == null) return;
            console.CycleKind();
        }

        void CycleSize()
        {
            _size = PracticeWave.Next(_size);
            Log.Event(Tag, "range wave — " + PracticeWave.Name(_size) + " (" + PracticeWave.Count(_size) + ")");
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
            var console = _console;
            GatekeeperKind kind = console != null ? console.SelectedKind : GatekeeperKind.Default;
            // The breach is the terminal's choice, handed to the spawner before the sim launches
            // so the first group already lands there.
            var distance = _distance;
            SpawnPoint point = distance != null ? distance.SelectedPoint : null;
            if (point != null && game.Spawner != null)
                game.Spawner.spawnPoints = new[] { point };
            if (!game.Sim.DrawPracticeWave(kind, _size))
            {
                Log.Event(Tag, "range wave refused — see the sim line above");
                return;
            }
            // The wave is judged from a full kit at the shipped wait: reset, never bypass.
            var rules = _rules;
            if (rules != null) rules.ResetCooldowns();
            var readout = _readout;
            if (readout != null) readout.ResetCounters();
            Log.Event(Tag, "range wave — " + PracticeWave.Count(_size) + " " + kind + " ("
                + PracticeWave.Name(_size) + ") drawn at " + Metres() + " m");
        }
    }
}
