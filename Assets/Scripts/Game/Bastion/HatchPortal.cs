using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: the way home, made visible. A dormant pad beside the Hatch that
    /// lights when a zone closes and takes the Boarder back to the ship on a held Interact.
    ///
    /// <para>WHY THIS EXISTS AT ALL. Docking was already a verb — hold E at the Hatch — but it
    /// was an invisible one: nothing in the world said it was there, and until 2026-08-28 it
    /// could not even be offered, because the only thing that opened it was a surviving Convert
    /// window and Convert had been struck from the demo. Zo played a full run and reported no
    /// call to action at the Hatch. The verb is fixed; this is the part that makes it legible.
    /// </para>
    ///
    /// <para>IT DOES NOT OWN THE RULE. <see cref="RunSim.DockOffered"/> decides whether leaving
    /// is legal and <see cref="RunSim.DockRun"/> performs it — the same two the Hatch's own
    /// prompt uses. This component only shows a thing and holds a timer, so the two surfaces
    /// cannot disagree about when going home is allowed. The Hatch prompt is deliberately left
    /// in place: a player who walks to the door instead of the pad still gets an answer.</para>
    ///
    /// <para>The hold matches the Hatch's, because they are the same action and a different
    /// duration on each would read as one of them being broken.</para></summary>
    public sealed class HatchPortal : MonoBehaviour, IInteractable
    {
        const string Tag = "Portal";

        /// <summary>Seconds of held Interact before the Boarding ends. Mirrors
        /// RepairInteractable's dock hold — same verb, same commitment.</summary>
        const float HoldSeconds = 1.5f;

        [SerializeField, Tooltip("Everything that should light up when the way home opens. "
            + "Hidden while the run has nowhere to go, so a dormant pad reads as scenery.")]
        GameObject[] litParts;

        GameController _game;
        float _hold;
        int _holdFrame = -1;
        bool _shownLit;
        bool _loggedOpen;

        void Awake()
        {
            // Cached in Awake per the repair discipline; re-resolved at the point of use only
            // if it comes back fake-null, which a scene reload can produce.
            _game = FindFirstObjectByType<GameController>();
            ApplyLit(false);
        }

        /// <summary>Is the way home open right now? One question, asked of the sim, so this
        /// surface and the Hatch's prompt cannot drift apart.</summary>
        bool Open
        {
            get
            {
                var gc = _game;
                if (gc == null) gc = _game = FindFirstObjectByType<GameController>();
                if (gc == null || gc.RunIsOver) return false;
                var sim = gc.Sim;
                if (sim == null) return false;
                return sim.Phase == RunPhase.Tethered && sim.DockOffered;
            }
        }

        public string Prompt
        {
            get
            {
                if (!Open) return null;
                // A hold that is not being held decays on its own — see Update. Reading the
                // frame counter here keeps the prompt honest about which state it is in.
                if (Time.frameCount - _holdFrame > 1) _hold = 0f;
                return _hold > 0f ? "[E] leaving..." : "[E] Portal — return to Ship";
            }
        }

        public void Interact(float dt)
        {
            if (!Open)
            {
                _hold = 0f;
                return;
            }
            _holdFrame = Time.frameCount;
            _hold += dt;
            if (_hold < HoldSeconds) return;
            _hold = 0f;

            var gc = _game;
            if (gc == null || gc.Sim == null) return;
            Log.Event(Tag, "portal entered at the Hatch — the Boarding ends Docked");
            gc.Sim.DockRun();
        }

        void Update()
        {
            bool open = Open;
            // Release resets the hold, exactly as every other hold in the Bastion does: a
            // player who looks away mid-hold has changed their mind, not paused.
            if (Time.frameCount - _holdFrame > 1) _hold = 0f;

            if (open == _shownLit) return;
            ApplyLit(open);
            if (open && !_loggedOpen)
            {
                _loggedOpen = true;
                Log.Event(Tag, "the way home is lit at the Hatch");
            }
        }

        /// <summary>Show or hide the lit parts. Nothing is created here — the pad is authored
        /// in the scene and this only switches it, because a scene that looks wrong is fixed by
        /// editing the scene, never by a script that rebuilds it.</summary>
        void ApplyLit(bool lit)
        {
            _shownLit = lit;
            if (litParts == null) return;
            for (int i = 0; i < litParts.Length; i++)
            {
                var go = litParts[i];
                if (go == null) continue;   // Unity-null safe: a part deleted in the scene
                if (go.activeSelf != lit) go.SetActive(lit);
            }
        }
    }
}
