using System;
using System.Collections.Generic;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Doors open on a SURVIVED Hatch-bank window, not on terminal completion.
    /// Completing the required terminals (AND) only makes a door eligible; RecordBankSurvived
    /// is what turns eligibility into an open door, so a failed bank never unlocks anything.
    /// Control gate last.</summary>
    public sealed class BastionUnlockGraph
    {
        public struct DoorSpec
        {
            public string DoorId;
            public string[] RequiredTerminalIds;
            public bool OpensControlRoom;
        }

        public struct TerminalSpec
        {
            public string TerminalId;
            /// <summary>Waves that must clear on this id before <see cref="TryCompleteTerminal"/>.
            /// Values below 1 are treated as 1.</summary>
            public int WavesToComplete;
        }

        readonly Dictionary<string, DoorSpec> _doors = new Dictionary<string, DoorSpec>();
        readonly Dictionary<string, TerminalSpec> _terminals = new Dictionary<string, TerminalSpec>();
        readonly Dictionary<string, int> _clearedWaves = new Dictionary<string, int>();
        readonly HashSet<string> _completedTerminals = new HashSet<string>();
        readonly List<string> _openDoors = new List<string>();
        readonly HashSet<string> _openDoorSet = new HashSet<string>();
        string _controlGateId;

        public static BastionUnlockGraph Build(DoorSpec[] doors, TerminalSpec[] terminals)
        {
            var graph = new BastionUnlockGraph();
            graph.Load(doors, terminals);
            return graph;
        }

        void Load(DoorSpec[] doors, TerminalSpec[] terminals)
        {
            _doors.Clear();
            _terminals.Clear();
            _clearedWaves.Clear();
            _completedTerminals.Clear();
            _openDoors.Clear();
            _openDoorSet.Clear();
            _controlGateId = null;

            if (doors != null)
            {
                for (int i = 0; i < doors.Length; i++)
                {
                    DoorSpec spec = doors[i];
                    if (string.IsNullOrEmpty(spec.DoorId))
                        continue;
                    _doors[spec.DoorId] = spec;
                    if (spec.OpensControlRoom)
                        _controlGateId = spec.DoorId;
                }
            }

            if (terminals != null)
            {
                for (int i = 0; i < terminals.Length; i++)
                {
                    TerminalSpec spec = terminals[i];
                    if (string.IsNullOrEmpty(spec.TerminalId))
                        continue;
                    _terminals[spec.TerminalId] = spec;
                }
            }
        }

        public int ClearedWaves(string terminalId)
        {
            if (string.IsNullOrEmpty(terminalId))
                return 0;
            int n;
            return _clearedWaves.TryGetValue(terminalId, out n) ? n : 0;
        }

        public int WavesNeeded(string terminalId)
        {
            if (string.IsNullOrEmpty(terminalId)
                || !_terminals.TryGetValue(terminalId, out TerminalSpec spec))
                return 1;
            return spec.WavesToComplete < 1 ? 1 : spec.WavesToComplete;
        }

        /// <summary>Count <paramref name="waves"/> toward this terminal. Completes it when
        /// the running total reaches <see cref="WavesNeeded"/>. Dying must not call this.</summary>
        public bool RecordWaveCleared(string terminalId, int waves, out string refuseReason)
        {
            refuseReason = null;
            if (string.IsNullOrEmpty(terminalId))
            {
                refuseReason = "terminal id is empty";
                return false;
            }

            if (!_terminals.ContainsKey(terminalId))
            {
                refuseReason = "unknown terminal";
                return false;
            }

            if (_completedTerminals.Contains(terminalId))
            {
                refuseReason = "already complete";
                return false;
            }

            if (waves < 1)
            {
                refuseReason = "no waves cleared";
                return false;
            }

            int have = ClearedWaves(terminalId) + waves;
            _clearedWaves[terminalId] = have;
            if (have < WavesNeeded(terminalId))
                return true;
            return TryCompleteTerminal(terminalId, out refuseReason);
        }

        public bool TryCompleteTerminal(string terminalId, out string refuseReason)
        {
            refuseReason = null;
            if (string.IsNullOrEmpty(terminalId))
            {
                refuseReason = "terminal id is empty";
                return false;
            }

            if (!_terminals.ContainsKey(terminalId))
            {
                refuseReason = "unknown terminal";
                return false;
            }

            if (_completedTerminals.Contains(terminalId))
            {
                refuseReason = "already complete";
                return false;
            }

            _completedTerminals.Add(terminalId);
            return true;
        }

        /// <summary>A Hatch-bank window was survived: every eligible door opens. Returns true when
        /// at least one door opened, and hands back the ids that opened on THIS call so the caller
        /// can log and voice them. A failed bank never reaches here, so it never unlocks.</summary>
        public bool RecordBankSurvived(out string[] openedDoorIds)
        {
            int before = _openDoors.Count;
            TryOpenDoors();
            int opened = _openDoors.Count - before;
            if (opened <= 0)
            {
                openedDoorIds = Array.Empty<string>();
                return false;
            }
            var ids = new string[opened];
            for (int i = 0; i < opened; i++)
                ids[i] = _openDoors[before + i];
            Array.Sort(ids, StringComparer.Ordinal);
            openedDoorIds = ids;
            return true;
        }

        void TryOpenDoors()
        {
            foreach (KeyValuePair<string, DoorSpec> pair in _doors)
            {
                if (_openDoorSet.Contains(pair.Key))
                    continue;
                if (!AllRequiredTerminalsDone(pair.Value.RequiredTerminalIds))
                    continue;
                _openDoorSet.Add(pair.Key);
                _openDoors.Add(pair.Key);
            }
        }

        bool AllRequiredTerminalsDone(string[] required)
        {
            if (required == null || required.Length == 0)
                return false;
            for (int i = 0; i < required.Length; i++)
            {
                if (!_completedTerminals.Contains(required[i]))
                    return false;
            }
            return true;
        }

        public bool IsDoorOpen(string doorId)
        {
            return !string.IsNullOrEmpty(doorId) && _openDoorSet.Contains(doorId);
        }

        public bool IsTerminalDone(string terminalId)
        {
            return !string.IsNullOrEmpty(terminalId) && _completedTerminals.Contains(terminalId);
        }

        public IReadOnlyList<string> OpenDoorIds => _openDoors;

        public bool IsFullyUnlocked
        {
            get
            {
                if (_doors.Count == 0)
                    return false;
                return _openDoorSet.Count == _doors.Count;
            }
        }

        public bool ControlRoomReachable =>
            !string.IsNullOrEmpty(_controlGateId) && _openDoorSet.Contains(_controlGateId);

        /// <summary>Sorted completed Field terminal ids for save / deep-start restore.</summary>
        public string[] CopyCompletedTerminalIds()
        {
            if (_completedTerminals.Count == 0)
                return Array.Empty<string>();
            var ids = new string[_completedTerminals.Count];
            _completedTerminals.CopyTo(ids);
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        /// <summary>Sorted open door ids for save / return-to-Ship restore. Door state is its own
        /// saved fact now: terminal completions no longer imply it, so it cannot be re-derived.</summary>
        public string[] CopyOpenDoorIds()
        {
            if (_openDoors.Count == 0)
                return Array.Empty<string>();
            var ids = _openDoors.ToArray();
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        /// <summary>Re-open saved doors on a later embark. Unknown ids skipped; already-open ids
        /// are a no-op, so this is safe to call twice.</summary>
        public void RestoreOpenDoors(string[] ids)
        {
            if (ids == null)
                return;
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!_doors.ContainsKey(id))
                    continue;
                if (_openDoorSet.Contains(id))
                    continue;
                _openDoorSet.Add(id);
                _openDoors.Add(id);
            }
        }

        /// <summary>Mark saved ids complete. Does NOT open doors — RestoreOpenDoors does that,
        /// because a completed terminal is no longer an unlock. Unknown ids skipped.</summary>
        public void RestoreCompletedTerminals(string[] ids)
        {
            if (ids == null)
                return;
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!_terminals.ContainsKey(id))
                    continue;
                if (_completedTerminals.Contains(id))
                    continue;
                _clearedWaves[id] = WavesNeeded(id);
                TryCompleteTerminal(id, out _);
            }
        }

        /// <summary>Null when valid; otherwise a short reason.</summary>
        public string Validate()
        {
            if (_doors.Count == 0)
                return "graph has no doors";

            int controlGates = 0;
            foreach (KeyValuePair<string, DoorSpec> pair in _doors)
            {
                if (pair.Value.OpensControlRoom)
                    controlGates++;
            }

            if (controlGates == 0)
                return "graph has no control gate";
            if (controlGates > 1)
                return "graph has multiple control gates";

            if (string.IsNullOrEmpty(_controlGateId)
                || !_doors.TryGetValue(_controlGateId, out DoorSpec controlDoor))
                return "control gate missing";

            string[] controlTerminals = controlDoor.RequiredTerminalIds;
            if (controlTerminals == null)
                controlTerminals = Array.Empty<string>();

            foreach (KeyValuePair<string, DoorSpec> pair in _doors)
            {
                if (pair.Key == _controlGateId)
                    continue;
                string[] required = pair.Value.RequiredTerminalIds;
                if (required == null)
                    continue;
                for (int i = 0; i < required.Length; i++)
                {
                    if (!_terminals.ContainsKey(required[i]))
                        return "door " + pair.Key + " requires unknown terminal " + required[i];
                    for (int j = 0; j < controlTerminals.Length; j++)
                    {
                        if (required[i] == controlTerminals[j])
                            return "non-control door requires a control-only terminal";
                    }
                }
            }

            if (controlDoor.RequiredTerminalIds != null)
            {
                for (int i = 0; i < controlDoor.RequiredTerminalIds.Length; i++)
                {
                    if (!_terminals.ContainsKey(controlDoor.RequiredTerminalIds[i]))
                        return "control gate requires unknown terminal " + controlDoor.RequiredTerminalIds[i];
                }
            }

            return null;
        }
    }
}
