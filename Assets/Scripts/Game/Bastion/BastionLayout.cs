using System.Collections.Generic;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: scene root — builds the unlock graph from child doors and terminals.</summary>
    public sealed class BastionLayout : MonoBehaviour
    {
        readonly List<BastionDoor> _doors = new List<BastionDoor>();
        readonly List<BastionTerminal> _terminals = new List<BastionTerminal>();

        BastionUnlockGraph _graph;
        bool _built;

        void Awake()
        {
            RebuildGraph();
        }

        public void RegisterDoor(BastionDoor door)
        {
            if (door == null)
                return;
            if (!_doors.Contains(door))
                _doors.Add(door);
            if (_built)
                ApplyDoorState(door);
        }

        public void UnregisterDoor(BastionDoor door)
        {
            if (door == null)
                return;
            _doors.Remove(door);
        }

        public void RegisterTerminal(BastionTerminal terminal)
        {
            if (terminal == null)
                return;
            if (!_terminals.Contains(terminal))
                _terminals.Add(terminal);
            if (_built)
                ApplyTerminalState(terminal);
        }

        public void UnregisterTerminal(BastionTerminal terminal)
        {
            if (terminal == null)
                return;
            _terminals.Remove(terminal);
        }

        public bool TryCompleteTerminal(string terminalId, out string refuseReason)
        {
            refuseReason = null;
            if (_graph == null)
            {
                refuseReason = "graph not built";
                return false;
            }

            if (!_graph.TryCompleteTerminal(terminalId, out refuseReason))
                return false;

            RefreshDoorStates();
            RefreshTerminalStates();
            return true;
        }

        public bool IsDoorOpen(string doorId)
        {
            return _graph != null && _graph.IsDoorOpen(doorId);
        }

        public bool IsTerminalDone(string terminalId)
        {
            return _graph != null && _graph.IsTerminalDone(terminalId);
        }

        public bool TryGetFieldProgress(string terminalId, out int cleared, out int needed)
        {
            cleared = 0;
            needed = 1;
            if (_graph == null)
                return false;
            needed = _graph.WavesNeeded(terminalId);
            cleared = _graph.ClearedWaves(terminalId);
            return true;
        }

        /// <summary>Hatch-bank uses T1's Wave Spawns (hatch bay) plus completed Field terminals.</summary>
        public void ActivateHatchBankSpawns()
        {
            for (int i = 0; i < _terminals.Count; i++)
            {
                BastionTerminal terminal = _terminals[i];
                if (terminal == null) continue;
                if (terminal.Kind != BastionTerminalKind.Field) continue;
                if (terminal.TerminalId == "T1" || IsTerminalDone(terminal.TerminalId))
                    terminal.SetUnlockedSpawnsActive(true);
            }
        }

        public void RestoreFieldSpawns()
        {
            RefreshTerminalStates();
        }

        public bool RecordWaveCleared(string terminalId, int waves, out int cleared, out int needed, out string refuseReason)
        {
            cleared = 0;
            needed = 1;
            refuseReason = null;
            if (_graph == null)
            {
                refuseReason = "graph not built";
                return false;
            }

            needed = _graph.WavesNeeded(terminalId);
            if (!_graph.RecordWaveCleared(terminalId, waves, out refuseReason))
            {
                cleared = _graph.ClearedWaves(terminalId);
                return false;
            }

            cleared = _graph.ClearedWaves(terminalId);
            if (_graph.IsTerminalDone(terminalId))
            {
                RefreshDoorStates();
                RefreshTerminalStates();
            }

            return true;
        }

        public bool ControlRoomReachable => _graph != null && _graph.ControlRoomReachable;

        void RebuildGraph()
        {
            _doors.Clear();
            _terminals.Clear();
            GetComponentsInChildren(true, _doors);
            GetComponentsInChildren(true, _terminals);

            var requiredByDoor = new Dictionary<BastionDoor, List<string>>();
            for (int i = 0; i < _doors.Count; i++)
            {
                BastionDoor door = _doors[i];
                if (door == null)
                    continue;
                var list = new List<string>();
                string[] authored = door.RequiredTerminalIds;
                if (authored != null)
                {
                    for (int r = 0; r < authored.Length; r++)
                    {
                        if (!string.IsNullOrEmpty(authored[r]) && !list.Contains(authored[r]))
                            list.Add(authored[r]);
                    }
                }

                requiredByDoor[door] = list;
            }

            int fieldCount = 0;
            for (int i = 0; i < _terminals.Count; i++)
            {
                BastionTerminal t = _terminals[i];
                if (t == null || t.Kind != BastionTerminalKind.Field || string.IsNullOrEmpty(t.TerminalId))
                    continue;
                fieldCount++;

                BastionDoor door = t.UnlocksDoor;
                if (door == null)
                    continue;
                if (!requiredByDoor.TryGetValue(door, out List<string> list))
                {
                    list = new List<string>();
                    requiredByDoor[door] = list;
                }

                if (!list.Contains(t.TerminalId))
                    list.Add(t.TerminalId);
            }

            var doorSpecs = new BastionUnlockGraph.DoorSpec[_doors.Count];
            for (int i = 0; i < _doors.Count; i++)
            {
                BastionDoor door = _doors[i];
                string[] required = System.Array.Empty<string>();
                if (door != null && requiredByDoor.TryGetValue(door, out List<string> list))
                    required = list.ToArray();

                doorSpecs[i] = new BastionUnlockGraph.DoorSpec
                {
                    DoorId = door != null ? door.DoorId : null,
                    RequiredTerminalIds = required,
                    OpensControlRoom = door != null && door.OpensControlRoom,
                };
            }

            var terminalSpecs = new BastionUnlockGraph.TerminalSpec[fieldCount];
            int w = 0;
            for (int i = 0; i < _terminals.Count; i++)
            {
                BastionTerminal terminal = _terminals[i];
                if (terminal == null || terminal.Kind != BastionTerminalKind.Field)
                    continue;
                if (string.IsNullOrEmpty(terminal.TerminalId))
                    continue;
                terminalSpecs[w++] = new BastionUnlockGraph.TerminalSpec
                {
                    TerminalId = terminal.TerminalId,
                    WavesToComplete = terminal.WavesToComplete,
                };
            }

            _graph = BastionUnlockGraph.Build(doorSpecs, terminalSpecs);
            _built = true;

            string validationError = _graph.Validate();
            if (validationError != null)
                Log.Warn("BastionLayout", "unlock graph invalid — " + validationError);

            RefreshDoorStates();
            RefreshTerminalStates();
        }

        void RefreshDoorStates()
        {
            for (int i = 0; i < _doors.Count; i++)
            {
                BastionDoor door = _doors[i];
                if (door == null)
                    continue;
                ApplyDoorState(door);
            }
        }

        void ApplyDoorState(BastionDoor door)
        {
            if (door == null || _graph == null)
                return;

            bool open = !door.StartsLocked || _graph.IsDoorOpen(door.DoorId);
            door.SetOpen(open);
        }

        void RefreshTerminalStates()
        {
            for (int i = 0; i < _terminals.Count; i++)
            {
                BastionTerminal terminal = _terminals[i];
                if (terminal == null)
                    continue;
                ApplyTerminalState(terminal);
            }
        }

        void ApplyTerminalState(BastionTerminal terminal)
        {
            if (terminal == null || _graph == null)
                return;
            if (terminal.Kind != BastionTerminalKind.Field)
                return;

            // Incomplete: keep Wave Spawns off so only a live pull (or sealed door open) registers
            // them. Done: leave them on for continued pressure after the door opens.
            bool done = _graph.IsTerminalDone(terminal.TerminalId);
            if (done)
                terminal.SetUnlockedSpawnsActive(true);
            else
                terminal.SetUnlockedSpawnsActive(false);
        }
    }
}
