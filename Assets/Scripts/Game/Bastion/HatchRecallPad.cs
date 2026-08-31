using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: pad beside a Field terminal. Hold Interact to teleport back to
    /// the Hatch. Authored in the Bastion scene as <c>{terminalId} Portal</c> using the same
    /// Pad/Emitter/Glass graybox as the Ship wake teleporter and the Hatch dock portal —
    /// this component only owns the verb and the lit state. Not created at runtime.</summary>
    public sealed class HatchRecallPad : MonoBehaviour, IInteractable
    {
        const string Tag = "Recall";
        const float HoldSeconds = 1.5f;

        [SerializeField, Tooltip("Pad / Emitter / Glass — same hierarchy as Hatch Portal and "
            + "Demo Teleporter. Lit while a Boarding is live.")]
        GameObject[] litParts;

        GameController _game;
        string _terminalId;
        float _hold;
        int _holdFrame = -1;
        bool _shownLit;

        void Awake()
        {
            _game = FindFirstObjectByType<GameController>();
            // "T2 Portal" → "T2". Master / Hatch portals keep their own names.
            string n = gameObject.name;
            if (n.EndsWith(" Portal"))
                _terminalId = n.Substring(0, n.Length - " Portal".Length);
            ApplyLit(false);
        }

        /// <summary>Wire every authored <c>{id} Portal</c> that already carries this component.
        /// No mesh is spawned — the Ship teleporter graybox is scene content.</summary>
        public static void BindAll()
        {
            var pads = Object.FindObjectsByType<HatchRecallPad>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < pads.Length; i++)
            {
                if (pads[i] != null)
                    pads[i].ApplyLit(pads[i].Available);
            }
        }

        bool Available
        {
            get
            {
                var gc = _game;
                if (gc == null) gc = _game = FindFirstObjectByType<GameController>();
                if (gc == null || gc.RunIsOver) return false;
                var sim = gc.Sim;
                if (sim == null) return false;
                return sim.Phase == RunPhase.Tethered;
            }
        }

        public string Prompt
        {
            get
            {
                if (!Available) return null;
                if (Time.frameCount - _holdFrame > 1) _hold = 0f;
                string e = Controls.DisplayOf(ControlId.Interact);
                return _hold > 0f
                    ? "[" + e + "] recalling..."
                    : "[" + e + "] Recall — Hatch";
            }
        }

        public void Interact(float dt)
        {
            if (!Available)
            {
                _hold = 0f;
                return;
            }
            _holdFrame = Time.frameCount;
            _hold += dt;
            if (_hold < HoldSeconds) return;
            _hold = 0f;

            var gc = _game;
            if (gc == null) return;
            Log.Event(Tag, "recall from "
                + (string.IsNullOrEmpty(_terminalId) ? name : _terminalId)
                + " — Hatch");
            gc.TeleportLocalToHatch();
        }

        void Update()
        {
            if (Time.frameCount - _holdFrame > 1) _hold = 0f;
            bool open = Available;
            if (open == _shownLit) return;
            ApplyLit(open);
        }

        void ApplyLit(bool lit)
        {
            _shownLit = lit;
            if (litParts == null) return;
            for (int i = 0; i < litParts.Length; i++)
            {
                var go = litParts[i];
                if (go == null) continue;
                if (go.activeSelf != lit) go.SetActive(lit);
            }
        }
    }
}
