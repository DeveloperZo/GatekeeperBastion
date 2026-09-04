using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the distance dimension, at a terminal beside the firing line
    /// (Zo, 2026-09-03: "We need a dimension for distance. This should be triggered at a
    /// terminal"). Each authored mark carries a spawn point; pressing the terminal cycles
    /// which one the next wave breaches from, and the board's draw hands that point to the
    /// spawner. The other dimensions stay on the board's keys; this one is the one surface on
    /// the lane, and it stands where the Boarder already is.
    ///
    /// A tap, not a hold: the Interactor calls <see cref="Interact"/> every frame the key is
    /// held, so the press edge is read off the same input frame the Interactor read.</summary>
    public sealed class RangeDistanceTerminal : MonoBehaviour, IInteractable
    {
        const string Tag = "Range";

        [SerializeField, Tooltip("The spawn point on each mark, in the order of _metres. Authored by the range builder.")]
        SpawnPoint[] _points;

        [SerializeField, Tooltip("Metres from the firing line for each entry of _points.")]
        int[] _metres;

        int _index = -1;

        string _prompt = "";
        int _promptIndex = -1;

        /// <summary>Metres from the firing line the next wave breaches at; the default when
        /// nothing has been pressed.</summary>
        public int SelectedMetres
        {
            get
            {
                int i = Index();
                return i < 0 ? PracticeWave.DefaultMetres : _metres[i];
            }
        }

        /// <summary>The spawn point for the selection, or null when the scene carries none.</summary>
        public SpawnPoint SelectedPoint
        {
            get
            {
                int i = Index();
                return i < 0 ? null : _points[i];
            }
        }

        int Index()
        {
            var metres = _metres;
            var points = _points;
            if (metres == null || points == null || metres.Length == 0 || points.Length != metres.Length)
                return -1;
            if (_index < 0)
            {
                _index = 0;
                for (int i = 0; i < metres.Length; i++)
                    if (metres[i] == PracticeWave.DefaultMetres) { _index = i; break; }
            }
            return _index;
        }

        public string Prompt
        {
            get
            {
                int i = Index();
                if (i < 0) return "";
                if (i != _promptIndex)
                {
                    _promptIndex = i;
                    int next = _metres[(i + 1) % _metres.Length];
                    _prompt = Controls.DisplayOf(ControlId.Interact) + " — breach at " + _metres[i]
                        + " m (next " + next + " m)";
                }
                return _prompt;
            }
        }

        public void Interact(float dt)
        {
            if (!Controls.Down(ControlId.Interact)) return;
            int i = Index();
            if (i < 0)
            {
                Log.Warn(Tag, "distance terminal has no spawn points");
                return;
            }
            _index = (i + 1) % _metres.Length;
            Log.Event(Tag, "range spawn — " + _metres[_index] + " m");
        }
    }
}
