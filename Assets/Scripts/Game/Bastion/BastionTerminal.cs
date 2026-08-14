using System.Text;
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

        /// <summary>Editor/sketch emit — Interact and prompt arming dead-end without this.</summary>
        public void SetGameController(GameController game)
        {
            _game = game;
        }

        /// <summary>Editor/sketch emit — doors exist only after all marks, so the door
        /// reference cannot ride Configure.</summary>
        public void SetUnlocksDoor(BastionDoor door)
        {
            unlocksDoor = door;
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

        static readonly WaveDirector s_preview = new WaveDirector();
        static readonly StringBuilder s_prompt = new StringBuilder(160);

        public string Prompt
        {
            get
            {
                if (string.IsNullOrEmpty(terminalId))
                    return null;
                string key = Controls.DisplayOf(ControlId.Interact);
                string startKey = Controls.DisplayOf(ControlId.PullDraw);
                string plus = Controls.DisplayOf(ControlId.DialUp);
                string minus = Controls.DisplayOf(ControlId.DialDown);
                switch (kind)
                {
                    case BastionTerminalKind.Master:
                        s_prompt.Clear();
                        bool masterArmed = _game != null && _game.IsWaveStartArmed(terminalId);
                        if (masterArmed)
                            s_prompt.Append('[').Append(startKey).Append("] Master Control");
                        else
                            s_prompt.Append('[').Append(plus).Append('/').Append(minus)
                                .Append("] Master Control");
                        AppendStake(s_prompt, _game);
                        s_prompt.Append("  [").Append(key).Append(masterArmed ? "] edit" : "] select");
                        return s_prompt.ToString();
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
                bool armed = gc != null && gc.IsWaveStartArmed(terminalId);
                s_prompt.Clear();
                if (armed)
                    s_prompt.Append('[').Append(startKey).Append("] Terminal ").Append(terminalId)
                        .Append(" — start Wave (").Append(k).Append('/').Append(n).Append(')');
                else
                    s_prompt.Append('[').Append(plus).Append('/').Append(minus)
                        .Append("] Terminal ").Append(terminalId)
                        .Append(" — Wave (").Append(k).Append('/').Append(n).Append(')');
                AppendStake(s_prompt, gc);
                s_prompt.Append("  [").Append(key).Append(armed ? "] edit" : "] select");
                return s_prompt.ToString();
            }
        }

        static void AppendStake(StringBuilder sb, GameController gc)
        {
            if (gc == null || gc.Sim == null) return;
            var sim = gc.Sim;
            int idx = sim.CurrentWaveIndex;
            if (idx >= WaveDirector.MaxWaves) idx = WaveDirector.MaxWaves - 1;
            if (idx < 0) idx = 0;
            int bodies = s_preview.NextWave(idx, sim.Siphon.Level).enemyCount;
            sb.Append("  Intensity ").Append(sim.Siphon.Level)
                .Append("  juice ").Append(IntensityJuice.CostPerStep)
                .Append("  unbanked ").Append(sim.Unbanked)
                .Append("  ").Append(bodies);
        }

        /// <summary>Held Interact. Wave start is Prep T after E-select. Completed Field
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
            }
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
