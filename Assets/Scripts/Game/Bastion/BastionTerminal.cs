using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: bastion console. Behaviour is <see cref="BastionTerminalKind"/>.
    /// Field terminals optionally name a door and spawn points; other kinds hide those fields.
    /// Not a Ship station.</summary>
    public sealed class BastionTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] string terminalId;
        [SerializeField] BastionTerminalKind kind = BastionTerminalKind.Field;
        [SerializeField, Tooltip("Field only — Waves that must clear before this terminal completes.")]
        int wavesToComplete = 1;
        [SerializeField, Tooltip("Field only — progression gate this terminal helps open.")]
        BastionDoor unlocksDoor;
        [SerializeField, Tooltip("Field only — spawn points for this terminal's Wave (enabled on pull).")]
        SpawnPoint[] unlockedSpawns;
        [SerializeField, Tooltip("Bastion GameController. Authored in the scene when possible.")]
        GameController _game;

        BastionLayout _layout;

        public string TerminalId => terminalId;
        public BastionTerminalKind Kind => kind;
        public int WavesToComplete => wavesToComplete < 1 ? 1 : wavesToComplete;
        public BastionDoor UnlocksDoor => unlocksDoor;
        public SpawnPoint[] UnlockedSpawns => unlockedSpawns;
        public bool IsMaster => kind == BastionTerminalKind.Master;
        public BastionLayout Layout => _layout;

        /// <summary>Editor/sketch emit — Field by default.</summary>
        public void Configure(
            string id,
            BastionTerminalKind terminalKind = BastionTerminalKind.Field,
            BastionDoor door = null,
            SpawnPoint[] spawns = null,
            int waves = 1)
        {
            terminalId = id;
            kind = terminalKind;
            unlocksDoor = door;
            unlockedSpawns = spawns != null ? spawns : System.Array.Empty<SpawnPoint>();
            wavesToComplete = waves < 1 ? 1 : waves;
        }

        void Reset()
        {
            kind = BastionTerminalKind.Field;
            wavesToComplete = 1;
        }

        void OnEnable()
        {
            _layout = GetComponentInParent<BastionLayout>();
            if (_layout != null)
                _layout.RegisterTerminal(this);
        }

        void OnDisable()
        {
            if (_layout != null)
                _layout.UnregisterTerminal(this);
        }

        public string Prompt
        {
            get
            {
                if (string.IsNullOrEmpty(terminalId))
                    return null;
                string key = Controls.DisplayOf(ControlId.Interact);
                switch (kind)
                {
                    case BastionTerminalKind.Master:
                        return "[" + key + "] Master Control — later";
                    case BastionTerminalKind.HatchBank:
                        return "[" + key + "] Hatch-bank — later";
                    case BastionTerminalKind.Resource:
                        return "[" + key + "] Resources — later";
                    default:
                        break;
                }

                if (_layout != null && _layout.IsTerminalDone(terminalId))
                {
                    if (unlocksDoor != null && !unlocksDoor.IsOpen)
                        return "[" + key + "] Open door";
                    return "Terminal " + terminalId + " — complete";
                }
                var gc = _game;
                if (gc != null && gc.Sim != null && gc.Sim.Assault == AssaultState.Assault)
                    return "Terminal " + terminalId + " — wave live";
                if (gc != null && gc.Sim != null && gc.Sim.SequentialPending)
                    return "Terminal " + terminalId + " — next Wave incoming";
                int k = 0;
                int n = WavesToComplete;
                if (_layout != null)
                    _layout.TryGetFieldProgress(terminalId, out k, out n);
                string noun = n == 1 ? "Wave" : "Waves";
                return "[" + key + "] Terminal " + terminalId + " — start " + noun + " (" + k + "/" + n + ")";
            }
        }

        /// <summary>Held Interact; Field starts the Wave on Interact Down. Completed Field
        /// with a shut door opens it.</summary>
        public void Interact(float dt)
        {
            if (!Controls.Down(ControlId.Interact))
                return;
            if (_game == null)
            {
                Log.Warn("Terminal", terminalId + " has no GameController");
                return;
            }

            if (kind == BastionTerminalKind.Field
                && _layout != null
                && _layout.IsTerminalDone(terminalId)
                && unlocksDoor != null
                && !unlocksDoor.IsOpen)
            {
                unlocksDoor.SetOpen(true);
                Log.Event("Terminal", terminalId + " opened " + unlocksDoor.DoorId);
                return;
            }

            _game.StartTerminalWave(this);
        }

        /// <summary>Field only — spawn points that wait on this terminal's clear.</summary>
        public void SetUnlockedSpawnsActive(bool active)
        {
            if (unlockedSpawns == null)
                return;

            for (int i = 0; i < unlockedSpawns.Length; i++)
            {
                SpawnPoint sp = unlockedSpawns[i];
                if (sp == null)
                    continue;
                sp.gameObject.SetActive(active);
            }
        }
    }
}
