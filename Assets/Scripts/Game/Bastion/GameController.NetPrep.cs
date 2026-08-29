using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): client Prep RPCs from
    /// <see cref="NetPrep"/>. Split so Terminals.cs stays under the Game line cap.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Server: client Prep juice. Range to a Field/Master terminal or Hatch.</summary>
        public bool TryNetJuice(int delta, Vector3 from, float range)
        {
            if (_net != null && _net.IsClientOnly) return false;
            if (Sim == null) return false;
            if (!NetPrepInRangeOfTrigger(from, range))
            {
                Log.Event("Run", "juice refused — out of range");
                return false;
            }
            if (_armedHatch || !string.IsNullOrEmpty(_armedTerminalId))
            {
                Log.Event("Run", "juice refused — intensity selected");
                return false;
            }
            return Sim.TryJuice(delta, true);
        }

        /// <summary>Server: client Field Wave start after range + select gate.</summary>
        public bool TryNetStartTerminal(string terminalId, Vector3 from, float range)
        {
            if (_net != null && _net.IsClientOnly) return false;
            BastionTerminal terminal = FindTerminalById(terminalId);
            if (terminal == null)
            {
                Log.Event("Terminal", "start refused — unknown id=" + terminalId);
                return false;
            }
            if (!NetPrepInRange(from, terminal.transform.position, range))
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — out of range");
                return false;
            }
            if (!CanSelectTerminal(terminal))
            {
                string block = FieldStartBlockReason(terminal.TerminalId);
                if (block != null)
                {
                    Log.Event("Demo", "start refused — " + block);
                    return false;
                }
                Log.Event("Terminal", terminal.TerminalId + " refused — not selectable");
                return false;
            }
            _armedTerminalId = terminal.TerminalId;
            return StartTerminalWave(terminal);
        }

        /// <summary>Server: client Hatch-bank start after range to the Hatch.</summary>
        public bool TryNetStartHatchBank(Vector3 from, float range)
        {
            if (_net != null && _net.IsClientOnly) return false;
            if (!NetPrepInRangeOfHatch(from, range))
            {
                Log.Event("Hatch", "hatch-bank refused — out of range");
                return false;
            }
            _armedHatch = true;
            return StartHatchBankWave();
        }

        /// <summary>Server: a client asked to start Master Control at its terminal.
        /// <para>REFUSED SINCE 2026-08-28 — the finale begins at the Hatch like every other
        /// zone. The local path (TickPrepCommands) was taught to refuse when the start moved,
        /// and this one was not, so the net verb remained a SECOND START LINE: exactly the
        /// shape Docs/Design/0-Gameplay.md forbids, reachable by any client walking up to the
        /// terminal. Its sibling TryNetStartTerminal never had this hole because it gates on
        /// CanSelectTerminal, which already refuses while the Hatch is the start line.</para>
        /// <para>Kept rather than deleted: NetPrep still calls it, and a refusal that names
        /// itself is worth more than a missing method. Range and kind are still checked so the
        /// refusal is about the RULE and not about a malformed request.</para></summary>
        public bool TryNetStartMaster(string terminalId, Vector3 from, float range)
        {
            if (_net != null && _net.IsClientOnly) return false;
            BastionTerminal terminal = FindTerminalById(terminalId);
            if (terminal == null)
            {
                Log.Event("Terminal", "master refused — unknown id=" + terminalId);
                return false;
            }
            if (terminal.Kind != BastionTerminalKind.Master)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — not Master");
                return false;
            }
            if (!NetPrepInRange(from, terminal.transform.position, range))
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — out of range");
                return false;
            }
            Log.Event("Terminal", terminal.TerminalId
                + " refused — Master Control starts at the Hatch");
            return false;
        }
        #endregion

        #region Private
        BastionTerminal FindTerminalById(string terminalId)
        {
            if (_layout != null)
                return _layout.FindTerminal(terminalId);
            return null;
        }

        static bool NetPrepInRange(Vector3 from, Vector3 to, float range)
        {
            return Vector3.Distance(from, to) <= range + 0.05f;
        }

        bool NetPrepInRangeOfHatch(Vector3 from, float range)
        {
            var hatch = HatchStation.Instance;
            if (hatch == null) return true;
            Vector3 surface = hatch.ClosestSurfacePoint(from);
            return NetPrepInRange(from, surface, range);
        }

        bool NetPrepInRangeOfTrigger(Vector3 from, float range)
        {
            if (NetPrepInRangeOfHatch(from, range)) return true;
            return _layout != null && _layout.AnyTriggerInRange(from, range);
        }
        #endregion
    }
}
