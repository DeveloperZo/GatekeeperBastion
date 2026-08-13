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

        public bool ControlRoomReachable => _graph != null && _graph.ControlRoomReachable;

        void RebuildGraph()
        {
            _doors.Clear();
            _terminals.Clear();
            GetComponentsInChildren(true, _doors);
            GetComponentsInChildren(true, _terminals);

            var doorSpecs = new BastionUnlockGraph.DoorSpec[_doors.Count];
            for (int i = 0; i < _doors.Count; i++)
            {
                BastionDoor door = _doors[i];
                doorSpecs[i] = new BastionUnlockGraph.DoorSpec
                {
                    DoorId = door.DoorId,
                    RequiredTerminalIds = door.RequiredTerminalIds,
                    OpensControlRoom = door.OpensControlRoom,
                };
            }

            var terminalSpecs = new BastionUnlockGraph.TerminalSpec[_terminals.Count];
            for (int i = 0; i < _terminals.Count; i++)
            {
                BastionTerminal terminal = _terminals[i];
                terminalSpecs[i] = new BastionUnlockGraph.TerminalSpec
                {
                    TerminalId = terminal.TerminalId,
                    PressureSpawnIds = terminal.PressureSpawnIds,
                    DoorId = terminal.DoorId,
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

            bool done = _graph.IsTerminalDone(terminal.TerminalId);
            terminal.SetPressureSpawnsActive(done);
        }
    }
}
