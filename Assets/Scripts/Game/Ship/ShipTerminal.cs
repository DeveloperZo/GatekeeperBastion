using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the beat-0 mid-deck console — the terminal contract taught
    /// at home: E selects (arm), T starts, enemies come. The start routes power, puts
    /// the MK-1 in the player's hand (2026-08-18 revision — no rack walk), and fires
    /// the intruders line. Not a station panel and not a BastionTerminal. One-shot per
    /// save: after the start it reads power routed and does nothing. Inert on a save
    /// with the demo off.</summary>
    public sealed class ShipTerminal : MonoBehaviour, IInteractable
    {
        const string Tag = "Demo";
        const float RebindSeconds = 0.5f;

        bool _armed;
        Interactor _interactor;
        float _rebindTimer;

        public string Prompt
        {
            get
            {
                var m = GameSession.Meta;
                if (m == null || !m.DemoActive || m.DemoArmoryPowered)
                    return "[E] Ship terminal — power routed";
                if (!m.DemoNavPowered)
                    return "[E] Ship terminal — NAV first";
                return _armed
                    ? "[T] Ship terminal — start"
                    : "[E] Ship terminal — route power";
            }
        }

        /// <summary>Hold path unused — E is a tap (arm), T is the start.</summary>
        public void Interact(float dt) { }

        void Update()
        {
            var m = GameSession.Meta;
            if (m == null || !m.DemoActive || m.DemoArmoryPowered) return;

            if (_interactor == null)
            {
                _rebindTimer -= Time.deltaTime;
                if (_rebindTimer > 0f) return;
                _rebindTimer = RebindSeconds;
                var player = PlayerRegistry.Local;
                if (player != null)
                    _interactor = player.GetComponentInChildren<Interactor>();
                if (_interactor == null) return;
            }

            // Interface-typed compare: which interactable the crosshair is on, this frame.
            IInteractable self = this;
            if (_interactor.Current != self) return;

            if (!m.DemoNavPowered)
            {
                if (Controls.Down(ControlId.Interact))
                    Log.Event(Tag, "station dark Ship terminal");
                return;
            }

            if (!_armed && Controls.Down(ControlId.Interact))
            {
                _armed = true;
                return;
            }

            if (_armed && Controls.Down(ControlId.PullDraw))
                StartTerminal(m);
        }

        void StartTerminal(Sim.MetaState m)
        {
            m.DemoArmoryPowered = true;
            Log.Event(Tag, "ship terminal started");
            // Grant first (saves both flags together), then the line: the copy says the
            // MK-1 is already in hand, so it must be.
            DemoShipFight.GrantWeapon(m);
            // The line arms the intruder release; Continue executes it (gated execution —
            // the sim ticks, timeScale is never written).
            DemoVoice.Play(DemoConversationIds.Intruders, DemoVoiceMode.Lock);
            DemoShipFight.ArmRelease();
            DemoShipFight.SetStanding("Kill the Gatekeepers");
        }
    }
}
