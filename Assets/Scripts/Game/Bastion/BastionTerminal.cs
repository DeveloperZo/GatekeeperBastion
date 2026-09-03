using System.Text;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
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

        /// <summary>Editor/sketch emit — Master UnlockedSpawns are Control Room closets,
        /// wired after spawn marks exist.</summary>
        public void SetUnlockedSpawns(SpawnPoint[] spawns)
        {
            unlockedSpawns = spawns != null ? spawns : System.Array.Empty<SpawnPoint>();
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

        /// <summary>The finale named before it is started, the way a field terminal names its
        /// last wave's shape: the same words the Hatch logs on the press. Per frame, so it
        /// appends an int rather than building a string.</summary>
        static void AppendFinaleShape(StringBuilder sb)
        {
            sb.Append(" — ").Append(MasterControl.TerminalWaves)
                .Append(" waves, then the elite and its escort");
        }

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
                        if (!Ante.Enabled)
                        {
                            s_prompt.Append('[').Append(startKey).Append("] Master Control");
                            AppendFinaleShape(s_prompt);
                            return s_prompt.ToString();
                        }
                        bool masterArmed = _game != null && _game.IsWaveStartArmed(terminalId);
                        if (masterArmed)
                            s_prompt.Append('[').Append(startKey).Append("] Master Control");
                        else
                            s_prompt.Append('[').Append(plus).Append('/').Append(minus)
                                .Append("] Master Control");
                        AppendFinaleShape(s_prompt);
                        AppendStake(s_prompt, _game);
                        s_prompt.Append("  [").Append(key).Append(masterArmed ? "] edit" : "] select");
                        return s_prompt.ToString();
                    case BastionTerminalKind.HatchBank:
                        return "[" + key + "] Convert — later";
                    case BastionTerminalKind.Resource:
                        return "[" + key + "] Resources — later";
                    default:
                        break;
                }

                // The live faucet cycle's origin outranks every other row — it is the off
                // switch, and it stays the off switch even once the terminal completes.
                if (_game != null && _game.Sim != null && _game.IsFaucetOrigin(terminalId))
                    return FaucetPrompt(_game);
                if (_layout != null && _layout.IsTerminalDone(terminalId))
                {
                    // No "[E] Open door" branch: the door opened with the zone. Shutting this
                    // terminal off is what opened it (GameController.OpenDoorsFor).
                    s_prompt.Clear();
                    s_prompt.Append("Terminal ").Append(terminalId).Append(" — complete");
                    return s_prompt.ToString();
                }
                var gc = _game;
                if (gc != null && gc.Sim != null && DemoZones.ZoneOf(terminalId) > 0
                    && gc.CanCallFinalWave(terminalId))
                {
                    // M115: the terminal's first press draws the zone's last wave. M116: the
                    // prompt names its shape, so the player reads the fight before calling it.
                    s_prompt.Clear();
                    s_prompt.Append('[').Append(startKey).Append("] call the last wave — ")
                        .Append(WaveShapes.Name(WaveShapes.For(
                            DemoZones.EndWave(DemoZones.ZoneOf(terminalId)))));
                    return s_prompt.ToString();
                }
                // THE LOOP (Zo, 2026-08-25, enforced 2026-08-26): the wave began at the Hatch
                // and this is its off switch — in Assault while bodies are still coming, and
                // in Prep once the player has killed them all and walked up anyway. The zone
                // is not finished until this is pressed, so the prompt must say so in both.
                if (gc != null && gc.Sim != null && gc.Sim.Mode != WaveMode.HatchBank
                    && DemoZones.ZoneOf(terminalId) > 0 && gc.CanShutOffTerminal(terminalId))
                {
                    s_prompt.Clear();
                    s_prompt.Append('[').Append(startKey).Append("] shut it down");
                    return s_prompt.ToString();
                }
                if (gc != null && gc.Sim != null && gc.Sim.Assault == AssaultState.Assault
                    && gc.Sim.ZoneFinalWaveDrawn && DemoZones.ZoneOf(terminalId) > 0)
                {
                    // M115: the last wave was called and is not down yet.
                    s_prompt.Clear();
                    s_prompt.Append("Terminal ").Append(terminalId)
                        .Append(" — the last wave is still standing");
                    return s_prompt.ToString();
                }
                if (gc != null && gc.Sim != null && gc.Sim.Assault == AssaultState.Assault)
                {
                    // A Convert window has no off switch — that one really is just live.
                    s_prompt.Clear();
                    s_prompt.Append("Terminal ").Append(terminalId).Append(" — wave live");
                    return s_prompt.ToString();
                }
                if (gc != null && gc.Sim != null && gc.Sim.SequentialPending)
                {
                    s_prompt.Clear();
                    s_prompt.Append("Terminal ").Append(terminalId).Append(" — next Wave incoming");
                    return s_prompt.ToString();
                }
                if (gc != null && gc.HatchOffersWaveStart && DemoZones.ZoneOf(terminalId) > 0)
                {
                    s_prompt.Clear();
                    s_prompt.Append("Terminal ").Append(terminalId)
                        .Append(" — start the wave at the Hatch");
                    return s_prompt.ToString();
                }
                if (gc != null)
                {
                    // The zone's deepest terminal remembers the zone: composite by default,
                    // partial start electable (beat 4). Checked before the block reason —
                    // the T1-first rule is exactly what the retrigger supersedes post-loss.
                    DemoRetriggerOffer offer = gc.RetriggerOfferFor(this);
                    if (offer.Available)
                        return RetriggerPrompt(gc, offer, key, startKey, plus, minus);
                }
                string blocked = gc != null ? gc.FieldStartBlockReason(terminalId) : null;
                if (blocked != null)
                {
                    s_prompt.Clear();
                    s_prompt.Append("Terminal ").Append(terminalId).Append(" — ").Append(blocked);
                    return s_prompt.ToString();
                }
                int k = 0;
                int n = WavesToComplete;
                if (_layout != null)
                    _layout.TryGetFieldProgress(terminalId, out k, out n);
                if (!Ante.Enabled)
                {
                    s_prompt.Clear();
                    s_prompt.Append('[').Append(startKey).Append("] Terminal ").Append(terminalId)
                        .Append(" — start Wave (").Append(k).Append('/').Append(n).Append(')');
                    return s_prompt.ToString();
                }
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

        /// <summary>The faucet origin's rows (faucet graybox): before the mark, the count to
        /// hold for; at the mark, the shutoff verb and the draft size the hold has earned.</summary>
        string FaucetPrompt(GameController gc)
        {
            var sim = gc.Sim;
            string startKey = Controls.DisplayOf(ControlId.PullDraw);
            s_prompt.Clear();
            if (sim.FaucetAwaitingDraft)
            {
                s_prompt.Append("Terminal ").Append(terminalId).Append(" — off. Draft open");
                return s_prompt.ToString();
            }
            if (sim.FaucetMarked)
                s_prompt.Append('[').Append(startKey).Append("] Terminal ").Append(terminalId)
                    .Append(" — SHUT OFF (draft ")
                    .Append(FaucetLoop.DraftSize(sim.FaucetWavesEndured)).Append(')');
            else
                s_prompt.Append("Terminal ").Append(terminalId)
                    .Append(" — faucet wave ").Append(sim.FaucetWavesEndured + 1)
                    .Append(" (off at ").Append(FaucetLoop.MarkAfterWaves).Append(')');
            return s_prompt.ToString();
        }

        /// <summary>Retrigger offer rows (copy draft — Zo, beat 4): the elected row leads
        /// with the start key; +/- swaps the election; E selects or edits.</summary>
        string RetriggerPrompt(GameController gc, DemoRetriggerOffer offer,
            string key, string startKey, string plus, string minus)
        {
            bool armed = gc.IsWaveStartArmed(terminalId);
            bool partial = gc.RetriggerPartialElected;
            s_prompt.Clear();
            if (!armed)
            {
                s_prompt.Append('[').Append(key).Append("] Terminal ").Append(terminalId)
                    .Append(" — RESTART ZONE (").Append(offer.WavesFrom(offer.CompositeStart))
                    .Append(" waves) — select");
                return s_prompt.ToString();
            }

            int start = partial ? offer.PartialStart : offer.CompositeStart;
            s_prompt.Append('[').Append(startKey).Append("] ");
            if (partial)
                s_prompt.Append("START AT WAVE ").Append(start)
                    .Append(" — ").Append(offer.WavesFrom(start))
                    .Append(offer.WavesFrom(start) == 1 ? " wave" : " waves");
            else
                s_prompt.Append("RESTART ZONE — ").Append(offer.WavesFrom(start))
                    .Append(" waves");
            if (offer.PartialAvailable)
            {
                s_prompt.Append("  [").Append(plus).Append('/').Append(minus).Append("] ");
                s_prompt.Append(partial
                    ? "RESTART ZONE"
                    : "START AT WAVE ");
                if (!partial) s_prompt.Append(offer.PartialStart);
            }
            s_prompt.Append("  [").Append(key).Append("] edit");
            return s_prompt.ToString();
        }

        static void AppendStake(StringBuilder sb, GameController gc)
        {
            if (!Ante.Enabled) return;
            if (gc == null || gc.Sim == null) return;
            var sim = gc.Sim;
            int idx = sim.CurrentWaveIndex;
            if (idx >= WaveDirector.MaxWaves) idx = WaveDirector.MaxWaves - 1;
            if (idx < 0) idx = 0;
            int bodies = s_preview.NextWave(idx, sim.Siphon.Level, sim.PlayerCount).enemyCount;
            sb.Append("  Ante ").Append(sim.Siphon.Level)
                .Append("  ante up ").Append(IntensityJuice.CostPerStep)
                .Append("  unbanked ").Append(sim.TotalUnbanked)
                .Append("  ").Append(bodies);
            if (gc.Meta != null && gc.Meta.EquippedPassive == BoarderAbilityId.Foresight)
            {
                var spec = s_preview.NextWave(idx, 0, sim.PlayerCount);
                sb.Append(' ').Append(spec.hunterCount).Append('H')
                    .Append('/').Append(spec.thiefCount).Append('R')
                    .Append('/').Append(spec.cloakerCount).Append('C')
                    .Append('/').Append(spec.defaultCount).Append('D');
                if (idx != _foresightLoggedWave)
                {
                    _foresightLoggedWave = idx;
                    Log.Event("Protocol", "foresight wave " + idx
                        + " H" + spec.hunterCount + " R" + spec.thiefCount
                        + " C" + spec.cloakerCount + " D" + spec.defaultCount);
                }
            }
        }

        static int _foresightLoggedWave = int.MinValue;

        /// <summary>Held Interact. Wave start is Prep T after E-select, handled by
        /// GameController.TickPrepCommands — a terminal has nothing of its own to do on E.
        /// The completed-Field door-open branch was retired: doors key on a survived
        /// Hatch-bank window now, so opening a door here would re-assert a rule the graph no
        /// longer has and the next RefreshDoorStates would shut it again.</summary>
        public void Interact(float dt)
        {
        }

        /// <summary>Enable or mute this terminal's UnlockedSpawns (Field Wave Spawns or
        /// Master Control Room closets).</summary>
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
