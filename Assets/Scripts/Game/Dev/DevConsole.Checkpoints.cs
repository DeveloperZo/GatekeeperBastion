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
        /// <summary>Esc or a letter while the checkpoint prompt is armed. True = consumed.</summary>
        bool TickCheckpointDigit()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _awaitingCheckpointDigit = false;
                RefreshCapture();
                Log.Event(Tag, "checkpoint cancelled");
                return true;
            }
            if (!TryReadLetter(GameController.DemoCheckpointLabels.Length, out int checkpoint))
                return false;

            // Keep suppress through the rest of this frame so the letter is not also read
            // as a fight verb (E interact, F/G/H abilities) — same rule as the F3 digit.
            _awaitingCheckpointDigit = false;
            _capture = CaptureKind.None;
            _suppressFightKeys = true;

            if (checkpoint == GameController.DemoCheckpointLabels.Length)
            {
                // The live reset needs the Bastion controller; DevConsole holds the
                // sanctioned Dev-only handle.
                var gc = Game;
                if (gc != null) gc.ResetZoneProgress();
                else Log.Warn(Tag, "reset ignored — no GameController (Ship scene?).");
                return true;
            }

            // State checkpoints are meta-only and load the Ship — legal from either scene.
            GameController.LoadDemoCheckpoint(checkpoint);
            return true;
        }

        void DrawCheckpointRows(float x, float y, float w)
        {
            GUI.Label(new Rect(x, y, w, 20f), "F12  DEMO CHECKPOINT — Esc back", _title);
            y += 22f;
            var labels = GameController.DemoCheckpointLabels;
            for (int i = 0; i < labels.Length; i++)
            {
                GUI.Label(new Rect(x, y, w, 18f), labels[i], _label);
                y += 18f;
            }
        }
    }
}
#endif
