#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The F12 demo-checkpoint prompt (partial of DevConsole): letter capture, the
    /// overlay rows, and the dispatch. Beats are keyed A.. so they never collide with the
    /// weapon-slot digits. Split from DevConsole.cs when the picker pushed it over the Game
    /// line cap — same console, one capture mode per file. State construction and the live
    /// reset live on GameController (DevCheckpoints partial).</summary>
    public sealed partial class DevConsole
    {
        /// <summary>1-based beat picked in stage 1, waiting on its configuration (Enter =
        /// as authored, a digit = a CheckpointConfigs row). 0 = still waiting on the letter.</summary>
        int _checkpointBeat;

        /// <summary>Which hull this prompt targets: F11 armed = the co-op bastion and its
        /// ship, F12 armed = the solo pair. Same beats, same configurations.</summary>
        bool _checkpointCoop;

        /// <summary>Arm the checkpoint prompt for one hull (F11 co-op / F12 solo).</summary>
        void ArmCheckpointPrompt(bool coop)
        {
            _awaitingCheckpointDigit = true;
            _checkpointBeat = 0; // re-arm always starts at the letter stage
            _checkpointCoop = coop;
            _overlay = true; // discovery: the overlay lists the checkpoint rows
            Log.Event(Tag, (coop ? "co-op" : "demo") + " checkpoint armed — press A.."
                + (char)('A' + GameController.DemoCheckpointLabels.Length - 1)
                + " (Esc cancel)");
        }

        /// <summary>Esc, a letter, then Enter or a config digit while the checkpoint prompt
        /// is armed. True = consumed.</summary>
        bool TickCheckpointDigit()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _awaitingCheckpointDigit = false;
                _checkpointBeat = 0;
                RefreshCapture();
                Log.Event(Tag, "checkpoint cancelled");
                return true;
            }

            if (_checkpointBeat == 0)
            {
                if (!TryReadLetter(GameController.DemoCheckpointLabels.Length, out int beat))
                    return false;

                if (beat == GameController.DemoCheckpointLabels.Length)
                {
                    // The live reset takes no configuration. It needs the Bastion
                    // controller; DevConsole holds the sanctioned Dev-only handle.
                    CloseCheckpointPrompt();
                    var gc = Game;
                    if (gc != null) gc.ResetZoneProgress();
                    else Log.Warn(Tag, "reset ignored — no GameController (Ship scene?).");
                    return true;
                }
                if (beat == GameController.StockDigit)
                {
                    // Stocking mutates the standing session; a loadout overlay has no seat.
                    CloseCheckpointPrompt();
                    GameController.LoadDemoCheckpoint(beat, 0, _checkpointCoop);
                    return true;
                }

                _checkpointBeat = beat;
                Log.Event(Tag, "beat " + (char)('A' + beat - 1)
                    + " — Enter as authored, 1.." + GameController.CheckpointConfigs.Length
                    + " for a configuration (Esc cancel)");
                return true;
            }

            // Stage 2: the configuration. Enter keeps 0 — the beat exactly as authored.
            int config;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                config = 0;
            else if (!TryReadDigit(GameController.CheckpointConfigs.Length, out config))
                return false;

            int picked = _checkpointBeat;
            bool coop = _checkpointCoop;
            CloseCheckpointPrompt();
            GameController.LoadDemoCheckpoint(picked, config, coop);
            return true;
        }

        /// <summary>Keep suppress through the rest of this frame so the consumed key is not
        /// also read as a fight verb (E interact, F/G/H abilities, slot digits).</summary>
        void CloseCheckpointPrompt()
        {
            _awaitingCheckpointDigit = false;
            _checkpointBeat = 0;
            _capture = CaptureKind.None;
            _suppressFightKeys = true;
        }

        void DrawCheckpointRows(float x, float y, float w)
        {
            if (_checkpointBeat == 0)
            {
                GUI.Label(new Rect(x, y, w, 20f), _checkpointCoop
                    ? "F11  CO-OP CHECKPOINT — Esc back"
                    : "F12  DEMO CHECKPOINT — Esc back", _title);
                y += 22f;
                var labels = GameController.DemoCheckpointLabels;
                for (int i = 0; i < labels.Length; i++)
                {
                    GUI.Label(new Rect(x, y, w, 18f), labels[i], _label);
                    y += 18f;
                }
                return;
            }

            GUI.Label(new Rect(x, y, w, 20f), "BEAT " + (char)('A' + _checkpointBeat - 1)
                + " — Enter as authored, or a configuration (Esc back)", _title);
            y += 22f;
            var configs = GameController.CheckpointConfigs;
            for (int i = 0; i < configs.Length; i++)
            {
                GUI.Label(new Rect(x, y, w, 18f), (i + 1) + "  " + configs[i].Name, _label);
                y += 18f;
            }
        }
    }
}
#endif
