using System;
using GatekeeperBastion.Util;

namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// How the run ended, and what it was worth. Gross is Salvage converted this run (Hatch-bank
    /// drip + dump), not kill awards. Repair spend is deducted from the ship bank as it happens.
    /// Net is <c>grossEarned - repairSpent</c>, derived by the caller.
    /// </summary>
    [Serializable]
    public struct RunReport
    {
        /// <summary>What ended the run: "hatch" (Hatch destroyed), "player" (player down), or
        /// "cleared" (all waves cleared — a SUCCESS).</summary>
        public string cause;

        /// <summary>Whole salvage units converted this run (Hatch-bank drip + dump). Equals
        /// <see cref="SalvageLedger.RunSalvage"/>.</summary>
        public long grossEarned;

        /// <summary>Salvage spent repairing the hatch during this run (0 when untouched).</summary>
        public long repairSpent;
    }

    /// <summary>
    /// Facade and state machine for one run. Plain C#, deterministic, driven only by explicit
    /// Tick(deltaTime) — designed to run host-authoritative under NGO later. The Unity layer
    /// codes against exactly this public API.
    ///
    /// v0 loop: Docked (aboard ship menu) → Tethered (fight in the bastion) → Lost (died)
    /// or Escaped (Wave 5 terminal with nothing Unbanked). Death is the only FAILURE ender:
    /// either the Hatch is destroyed or the player goes down (NotifyPlayerDown) — both funnel
    /// through one EndRun path. The tether cannot be cut at will: there is no bail-out path
    /// in v0. BeginEscape/spool ticking were removed; they return with the escape-window design.
    /// Hatch-bank after the field is stripped stays Tethered so the opened door is usable.
    ///
    /// Waves are DRAW-TRIGGERED and there are exactly WaveDirector.MaxWaves of them: while
    /// Tethered the sim alternates Prep ↔ Assault. A field terminal Interact arms a
    /// sequential run of draws (clear, TerminalChainGap of Prep, next draw). Tests and Dev
    /// still call TriggerDraw() for a single assault.
    ///
    /// Deep start: StartRun(N) remembers a start wave. The first TriggerDraw delivers
    /// waves 1..N back to back — each next wave starts InterWaveGap seconds after the previous
    /// wave's *delivery* finishes, with no Prep between them. Overlap in the room is the point.
    /// After that chain clears, the run continues normally through wave MaxWaves.
    ///
    /// ECONOMY: AwardKill() credits Unbanked. Hatch-bank drip/dump calls EarnSalvage into
    /// MetaState.BankedSalvage. Down and hatch breach wipe leftover Unbanked without converting.
    /// Terminal Wave-5 clear keeps Unbanked; Hatch-bank after the field is stripped converts
    /// and stays Prep. RepairHatch spends from the ship bank only.
    /// </summary>
    public sealed class RunSim
    {
        private const string Tag = "RunSim";

        private readonly MetaState _meta;
        private readonly WaveDirector _director = new WaveDirector();
        private bool _runStarted;

        /// <summary>Fractional HP requested across RepairHatch ticks that has not yet bought a
        /// whole salvage unit. Lets per-frame rate*dt charge continuously without free micro-heals.</summary>
        private float _repairHpAccrued;

        // Deep-start chain (only the first assault of a run started above wave 1).
        int _startWave = 1;       // 1-based, clamped at StartRun
        int _chainUntil;          // 0-based inclusive last wave index to auto-launch
        bool _chainActive;        // this assault is (or was) a deep-start chain
        bool _awaitingDelivery;   // waiting for WaveSpawner to finish the current wave's delivery
        float _interWaveTimer;    // counts down WaveDirector.InterWaveGap between chained waves
        int _followUpsLeft;       // sequential draws remaining after the live assault clears
        float _followUpTimer;     // counts down TerminalChainGap in Prep
        float _hatchConvertMul = HatchBank.MultLow;
        float _dripAcc;
        float _withdrawAcc;
        float _hatchBankTimer;

        /// <summary>Seed passed at construction; all future run randomness derives from it.</summary>
        public int Seed { get; private set; }

        /// <summary>Deterministic RNG seeded from the ctor. Draw ALL sim randomness from here — never statics.</summary>
        public Random Rng { get; private set; }

        /// <summary>Current run phase. Lost (lost) and Escaped (completed) are both terminal.</summary>
        public RunPhase Phase { get; private set; }

        /// <summary>Tethered sub-state: Prep (quiet, dial free) or Assault (draw live, kills pay).
        /// Meaningful only while Phase == Tethered; always Prep otherwise.</summary>
        public AssaultState Assault { get; private set; }

        /// <summary>Who the live Wave is for. Set on each <see cref="TriggerDraw"/>.</summary>
        public WaveMode Mode { get; private set; }

        /// <summary>The risk/reward siphon dial.</summary>
        public TetherSiphon Siphon { get; private set; }

        /// <summary>The defended objective (the Hatch back to your ship).</summary>
        public ShipSystems Ship { get; private set; }

        /// <summary>Per-run converted tally (drip + dump). Unbanked kills are not this number.
        /// Resets with each new run (each run gets a fresh RunSim and therefore a fresh ledger).</summary>
        public SalvageLedger Salvage { get; private set; }

        /// <summary>Kill awards this Run that have not yet converted. Wiped on down, breach, or
        /// a terminal last-Wave clear. Converted only by Hatch-bank drip/dump.</summary>
        public long Unbanked { get; private set; }

        /// <summary>Hatch-bank starts remaining this Run. Starts at <see cref="HatchBank.SlotsPerRun"/>.</summary>
        public int HatchBankSlotsLeft { get; private set; }

        /// <summary>Convert multiplier snapshotted at the last Hatch-bank start. 1 while unused.</summary>
        public float HatchConvertMultiplier => _hatchConvertMul;

        /// <summary>Filled when the run ends: converted take and repair spend. Bookkeeping only —
        /// salvage already moved at convert/repair time. Net is derived by the caller.</summary>
        public RunReport LastRunReport { get; private set; }

        /// <summary>Salvage spent on hatch repair this run. Deducted from the bank as healing is
        /// applied; reported alongside gross on the end screen.</summary>
        public long RepairSalvageSpent { get; private set; }

        /// <summary>Number of Terminal waves launched so far this run (0 before the first,
        /// MaxWaves after the field is stripped, higher if a later terminal draws).</summary>
        public int CurrentWaveIndex { get; private set; }

        /// <summary>1-based wave the player chose to start at (1 = normal run). Clamped to
        /// 1..DeepestWaveReached at StartRun.</summary>
        public int StartWave => _startWave;

        /// <summary>True while a deep-start chain still has waves to launch (delivery wait or
        /// inter-wave gap). WaveSpawner must not NotifyAssaultCleared until this is false.</summary>
        public bool ChainPending
        {
            get
            {
                if (!_chainActive) return false;
                if (_awaitingDelivery) return true;
                if (_interWaveTimer > 0f) return true;
                return CurrentWaveIndex <= _chainUntil;
            }
        }

        /// <summary>True while a field-terminal sequence still has a follow-up draw armed
        /// or counting down in Prep.</summary>
        public bool SequentialPending => _followUpsLeft > 0 || _followUpTimer > 0f;

        /// <summary>Seconds of Prep left before a sequential follow-up draw. 0 when none.</summary>
        public float SequentialFollowUpRemaining => _followUpTimer;

        /// <summary>True once the run completed through CompleteRun (Wave 5 terminal with
        /// nothing unbanked) rather than lost.</summary>
        public bool AllWavesCleared { get; private set; }

        /// <summary>Base salvage one kill is worth before the siphon multiplier: the Scavenging
        /// attribute's 25 + 5*level. Read fresh from the MetaState so a mid-session purchase is
        /// live on the very next kill.</summary>
        private float BaseKillValue
        {
            get { return MetaUpgrades.SalvagePerKill(_meta.ScavengingLevel); }
        }

        /// <summary>Salvage the next kill is worth right now:
        /// MetaUpgrades.SalvagePerKill(ScavengingLevel) times the siphon's hand-authored dial
        /// multiplier (1 / 1.1 / 1.15 / 1.25 / 1.35 / 1.5) — 25 at both dials down, 46.875 at
        /// Scavenging 5 / siphon 5. The HUD shows this; it is the whole income model.</summary>
        public float KillValueAt(int siphonLevel) =>
            BaseKillValue * TetherSiphon.RewardMultiplierFor(siphonLevel);

        public float CurrentKillValue => KillValueAt(Siphon.Level);

        public long CurrentKillAward =>
            Mode == WaveMode.HatchBank ? 0L : (long)Math.Floor((double)CurrentKillValue);

        /// <summary>Seconds left on the Hatch-bank convert window. 0 outside Hatch-bank Assault.</summary>
        public float HatchBankRemaining => _hatchBankTimer;

        /// <summary>Sim seconds elapsed while Tethered.</summary>
        public float RunElapsed { get; private set; }

        /// <summary>Raised each time a wave launches (TriggerDraw or a chained follow-up), with its spec.</summary>
        public event Action<WaveSpec> WaveStarted;

        /// <summary>Raised when NotifyAssaultCleared returns the run to Prep (including after the
        /// final wave, immediately before the run completes).</summary>
        public event Action AssaultEnded;

        /// <summary>Raised on every phase transition (also once for Docked on StartRun).</summary>
        public event Action<RunPhase> PhaseChanged;

        public RunSim(MetaState meta, int seed)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            _meta = meta;
            Seed = seed;
            Rng = new Random(seed);
            Siphon = new TetherSiphon();
            Ship = new ShipSystems(MetaUpgrades.HatchHp(meta.HatchArmorLevel));
            Salvage = new SalvageLedger();
            Unbanked = 0L;
            HatchBankSlotsLeft = HatchBank.SlotsPerRun;
            Phase = RunPhase.Docked;
            Assault = AssaultState.Prep;
            Mode = WaveMode.Terminal;

            Ship.HatchDestroyed += OnHatchDestroyed;
        }

        /// <summary>Begin the run in Docked. Increments MetaState.TotalRuns. Callable once.
        /// <paramref name="startWave"/> is 1-based and clamped to 1..min(MaxWaves, DeepestWaveReached).</summary>
        public void StartRun(int startWave = 1)
        {
            if (_runStarted)
            {
                Log.Info(Tag, "StartRun ignored: run already started.");
                return;
            }
            _runStarted = true;
            _meta.TotalRuns++;
            _startWave = ClampStartWave(startWave);
            SetPhase(RunPhase.Docked);
        }

        /// <summary>Board the bastion: Docked → Tethered, landing in Prep. No waves until the
        /// player pulls TriggerDraw.</summary>
        public void EngageTether()
        {
            if (!_runStarted || Phase != RunPhase.Docked)
            {
                Log.Info(Tag, "EngageTether ignored in phase " + Phase);
                return;
            }
            Assault = AssaultState.Prep;
            SetPhase(RunPhase.Tethered);
        }

        /// <summary>Set the siphon dial (clamped 0..5). Only legal while Tethered+Prep —
        /// the draw size is a pre-commitment, so during Assault this is a no-op.</summary>
        public void SetSiphonLevel(int level)
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "SetSiphonLevel ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            Siphon.Level = level;
        }

        /// <summary>
        /// Pull a Terminal Wave: Prep → Assault. Tests and Dev keep this signature.
        /// </summary>
        public void TriggerDraw()
        {
            TriggerDraw(WaveMode.Terminal);
        }

        /// <summary>
        /// Pull a Wave in the given mode. Only valid in Tethered+Prep. Terminal may continue
        /// past MaxWaves (composition clamps). HatchBank does not consume a field Wave,
        /// refuses if Unbanked is 0 or no slots remain, and never deep-start chains.
        /// </summary>
        public void TriggerDraw(WaveMode mode)
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "TriggerDraw ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            if (mode == WaveMode.HatchBank)
            {
                if (Unbanked <= 0L)
                {
                    Log.Info(Tag, "hatch-bank refused — unbanked=0");
                    return;
                }
                if (HatchBankSlotsLeft <= 0)
                {
                    Log.Info(Tag, "hatch-bank refused — no slots");
                    return;
                }
                HatchBankSlotsLeft--;
                _hatchConvertMul = HatchBank.ConvertMultiplier(Unbanked);
                _dripAcc = 0f;
                _hatchBankTimer = HatchBank.DurationSeconds;
                _chainActive = false;
                _chainUntil = CurrentWaveIndex;
                _awaitingDelivery = false;
                _interWaveTimer = 0f;
            }
            else
            {
                // Deep start: only the first assault of the run chains. Later draws are normal.
                if (CurrentWaveIndex == 0 && _startWave > 1)
                {
                    _chainActive = true;
                    _chainUntil = _startWave - 1;
                }
                else
                {
                    _chainActive = false;
                    _chainUntil = CurrentWaveIndex; // single-wave assault
                }
            }

            Mode = mode;
            Assault = AssaultState.Assault;
            LaunchWave();
            if (_chainActive && CurrentWaveIndex <= _chainUntil)
                _awaitingDelivery = true;
        }

        /// <summary>Arm this draw plus <paramref name="total"/> − 1 follow-ups after each
        /// clear, separated by <see cref="WaveDirector.TerminalChainGap"/> of Prep. 1 is a
        /// single assault (tests and Dev).</summary>
        public void ArmSequentialDraws(int total)
        {
            if (total < 1) total = 1;
            _followUpsLeft = total - 1;
            _followUpTimer = 0f;
        }

        /// <summary>Drop a field-terminal sequence (Dev jump, refused draw, run end).</summary>
        public void CancelSequentialDraws()
        {
            _followUpsLeft = 0;
            _followUpTimer = 0f;
        }

        /// <summary>Hatch damage from the Game layer. Field-terminal Waves leave the Hatch
        /// invulnerable; Hatch-bank is the first mode that applies this.</summary>
        public void DamageHatch(float amount)
        {
            if (amount <= 0f)
                return;
            if (Mode == WaveMode.Terminal)
                return;
            Ship.Hatch.Damage(amount);
            if (Phase != RunPhase.Tethered)
                return;
            AttritUnbanked(HatchBank.AttritUnits(amount));
        }

        /// <summary>
        /// Game layer reports the current wave's roster has finished spawning. During a deep-start
        /// chain this starts the InterWaveGap timer toward the next wave; otherwise it is a no-op
        /// (clear still waits on every gatekeeper dead).
        /// </summary>
        public void NotifyDeliveryComplete()
        {
            if (!_chainActive || !_awaitingDelivery) return;
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Assault) return;

            _awaitingDelivery = false;
            if (CurrentWaveIndex <= _chainUntil)
                _interWaveTimer = WaveDirector.InterWaveGap;
        }

        /// <summary>
        /// Abort a live assault without counting it cleared: Assault → Prep and rewind
        /// CurrentWaveIndex so the same wave can be drawn again. Does NOT fire AssaultEnded
        /// (that would look like a clear and could CompleteRun). Used by Dev replay / jump;
        /// not a player-facing bail-out. Clears any in-flight deep-start chain.
        /// </summary>
        public void AbortAssault()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Assault)
            {
                Log.Info(Tag, "AbortAssault ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            Assault = AssaultState.Prep;
            if (Mode != WaveMode.HatchBank && CurrentWaveIndex > 0) CurrentWaveIndex--;
            _hatchBankTimer = 0f;
            ClearChainState();
            CancelSequentialDraws();
        }

        /// <summary>
        /// Point the next TriggerDraw at 1-based wave N. Only legal in Tethered+Prep.
        /// Clamps to 1..MaxWaves. Does not start the wave — call TriggerDraw after.
        /// Disables deep-start chaining for the rest of this run (Dev jump is a skip, not a chain).
        /// </summary>
        public void SeekWave(int oneBasedWave)
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "SeekWave ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            if (oneBasedWave < 1) oneBasedWave = 1;
            if (oneBasedWave > WaveDirector.MaxWaves) oneBasedWave = WaveDirector.MaxWaves;
            // TriggerDraw reads CurrentWaveIndex as the next wave's 0-based index, then increments.
            CurrentWaveIndex = oneBasedWave - 1;
            AllWavesCleared = false;
            // A seek mid-run is not a deep start — force single-wave assaults from here.
            _startWave = 1;
            ClearChainState();
            CancelSequentialDraws();
        }

        /// <summary>
        /// Game layer reports one gatekeeper killed. Credits Unbanked with the floored kill
        /// award. Does not touch the ship bank. Valid only while Tethered + Assault.
        /// </summary>
        public void AwardKill()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Assault)
            {
                Log.Info(Tag, "AwardKill ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            long award = CurrentKillAward;
            if (award <= 0L) return;
            Unbanked += award;
        }

        /// <summary>
        /// Heal the hatch by up to <paramref name="requestedHp"/>, charging
        /// <see cref="ShipSystems.RepairSalvagePerHp"/> salvage per HP from the bank as healing
        /// is applied. Returns the HP actually applied (0 when the hatch is full, destroyed, the
        /// run is not live, or the bank is empty). Caps by need before cost so a full hatch never
        /// charges. When the bank cannot cover the full request, spends the entire bank and heals
        /// the affordable fraction so the bank lands at exactly zero.
        /// </summary>
        public float RepairHatch(float requestedHp)
        {
            if (requestedHp <= 0f) return 0f;
            if (Phase != RunPhase.Tethered) return 0f;

            var hatch = Ship.Hatch;
            if (hatch.IsDestroyed) return 0f;

            float need = hatch.MaxHp - hatch.Hp;
            if (need <= 0f)
            {
                _repairHpAccrued = 0f;
                return 0f;
            }

            long bank = _meta.BankedSalvage;
            if (bank <= 0)
            {
                _repairHpAccrued = 0f;
                return 0f;
            }

            float rate = ShipSystems.RepairSalvagePerHp;
            float want = requestedHp < need ? requestedHp : need;
            _repairHpAccrued += want;

            // Whole salvage units this accrued HP can buy; bank is the hard cap (criterion 2).
            long units = (long)Math.Floor((double)_repairHpAccrued * rate + 1e-6);
            if (units <= 0) return 0f;
            if (units > bank) units = bank;

            float applied = (float)(units / (double)rate);
            if (applied > need)
            {
                applied = need;
                units = (long)Math.Ceiling((double)applied * rate - 1e-6);
                if (units > bank) units = bank;
                applied = (float)(units / (double)rate);
            }
            if (units <= 0 || applied <= 0f) return 0f;

            if (!_meta.SpendSalvage(units)) return 0f;

            hatch.Repair(applied);
            _repairHpAccrued -= applied;
            if (_repairHpAccrued < 0f) _repairHpAccrued = 0f;
            RepairSalvageSpent += units;
            return applied;
        }

        /// <summary>Move ship bank into Unbanked at <see cref="HatchBank.WithdrawPerSecond"/>.
        /// Prep and Hatch full. Returns units moved. Game starts the hold only when Unbanked is
        /// 0; once held, further ticks may run with Unbanked already raised.</summary>
        public long Withdraw(float dt)
        {
            if (dt <= 0f) return 0L;
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep) return 0L;
            var hatch = Ship.Hatch;
            if (hatch.IsDestroyed) return 0L;
            if (hatch.MaxHp - hatch.Hp > 0.01f) return 0L;

            long want = HatchBank.WithdrawUnits(dt, ref _withdrawAcc);
            if (want <= 0L) return 0L;
            long bank = _meta.BankedSalvage;
            if (want > bank) want = bank;
            if (want <= 0L) return 0L;
            if (!_meta.SpendSalvage(want)) return 0L;
            Unbanked += want;
            return want;
        }

        /// <summary>
        /// Game layer reports the PLAYER dead. Wipes Unbanked; ship bank is kept.
        /// </summary>
        public void NotifyPlayerDown()
        {
            if (Phase != RunPhase.Tethered)
            {
                Log.Info(Tag, "NotifyPlayerDown ignored in phase " + Phase);
                return;
            }
            EndRun("player");
        }

        /// <summary>
        /// Game layer reports the wave dead (all spawned gatekeepers down, spawning finished, and
        /// any deep-start chain fully delivered): Assault → Prep and AssaultEnded fires. Raises
        /// MetaState.DeepestWaveReached to CurrentWaveIndex when higher. Terminal Wave 5 with
        /// Unbanked keeps Prep (door already opened by the terminal complete). Hatch-bank dump
        /// after the field is stripped stays Prep so progression through that door continues.
        /// Unbanked 0 on Wave 5 Completes (nothing to bank). Later terminal clears do not.
        /// Ignored outside Tethered+Assault, and ignored while ChainPending.
        /// </summary>
        public void NotifyAssaultCleared()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Assault)
            {
                Log.Info(Tag, "NotifyAssaultCleared ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            if (ChainPending)
            {
                Log.Info(Tag, "NotifyAssaultCleared ignored: deep-start chain still pending.");
                return;
            }
            if (Mode == WaveMode.HatchBank && _hatchBankTimer > 0f)
            {
                Log.Info(Tag, "NotifyAssaultCleared ignored: hatch-bank timer still running");
                return;
            }

            bool hatchBank = Mode == WaveMode.HatchBank;
            if (hatchBank)
            {
                _hatchBankTimer = 0f;
                DumpUnbanked();
            }

            // Cleared through CurrentWaveIndex (1-based wave number already launched).
            int deepest = CurrentWaveIndex;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (deepest > _meta.DeepestWaveReached)
                _meta.DeepestWaveReached = deepest;

            ClearChainState();
            Assault = AssaultState.Prep;
            var handler = AssaultEnded;
            if (handler != null) handler();

            // Re-check the phase: an AssaultEnded listener is free to end the run (kill the
            // player, drop the hatch), and a run already lost must not also complete.
            if (Phase == RunPhase.Tethered && !hatchBank && Unbanked <= 0L
                && CurrentWaveIndex == WaveDirector.MaxWaves && _followUpsLeft <= 0)
            {
                CancelSequentialDraws();
                CompleteRun();
                return;
            }

            if (Phase == RunPhase.Tethered && CurrentWaveIndex >= WaveDirector.MaxWaves
                && _followUpsLeft <= 0)
            {
                if (hatchBank)
                    Log.Info(Tag, "hatch-bank done — unbanked=" + Unbanked + " door is open");
                else if (Unbanked > 0L)
                    Log.Info(Tag, "field stripped — unbanked=" + Unbanked + " hatch-bank to convert");
            }

            if (Phase == RunPhase.Tethered && _followUpsLeft > 0)
            {
                _followUpsLeft--;
                _followUpTimer = WaveDirector.TerminalChainGap;
            }
            else
                CancelSequentialDraws();
        }

        /// <summary>
        /// Advance the sim by dt seconds. Active only while Tethered. Advances RunElapsed and,
        /// during a deep-start chain, the inter-wave gap timer that auto-launches the next wave.
        /// Docked and terminal phases ignore ticks entirely.
        /// </summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (Phase != RunPhase.Tethered) return;

            RunElapsed += dt;

            if (Assault == AssaultState.Assault && Mode == WaveMode.HatchBank)
            {
                if (Unbanked > 0L)
                    DripTick(dt);
                _hatchBankTimer -= dt;
                if (_hatchBankTimer <= 0f)
                {
                    _hatchBankTimer = 0f;
                    NotifyAssaultCleared();
                }
                return;
            }

            if (Assault == AssaultState.Prep && _followUpTimer > 0f)
            {
                _followUpTimer -= dt;
                if (_followUpTimer <= 0f)
                {
                    _followUpTimer = 0f;
                    TriggerDraw(Mode);
                }
                return;
            }

            if (_interWaveTimer <= 0f) return;
            if (Assault != AssaultState.Assault || !_chainActive) return;

            _interWaveTimer -= dt;
            if (_interWaveTimer > 0f) return;

            _interWaveTimer = 0f;
            if (CurrentWaveIndex > _chainUntil) return;
            if (CurrentWaveIndex >= WaveDirector.MaxWaves) return;

            LaunchWave();
            if (CurrentWaveIndex <= _chainUntil)
                _awaitingDelivery = true;
        }

        private void LaunchWave()
        {
            int idx = CurrentWaveIndex;
            if (Mode != WaveMode.HatchBank && idx >= WaveDirector.MaxWaves)
                idx = WaveDirector.MaxWaves - 1;
            WaveSpec spec = _director.NextWave(idx, Siphon.Level);
            if (Mode != WaveMode.HatchBank)
                CurrentWaveIndex++;
            var handler = WaveStarted;
            if (handler != null) handler(spec);
        }

        private void ClearChainState()
        {
            _chainActive = false;
            _awaitingDelivery = false;
            _interWaveTimer = 0f;
        }

        private int ClampStartWave(int startWave)
        {
            if (startWave < 1) startWave = 1;
            int deepest = _meta.DeepestWaveReached;
            if (deepest < 1) deepest = 1;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (startWave > deepest) startWave = deepest;
            if (startWave > WaveDirector.MaxWaves) startWave = WaveDirector.MaxWaves;
            return startWave;
        }

        private void OnHatchDestroyed()
        {
            // Hatch loss only ends a live run; ignore while Docked or already terminal.
            if (Phase != RunPhase.Tethered) return;
            EndRun("hatch");
        }

        /// <summary>
        /// BOOKKEEPING ONLY — leftover Unbanked is wiped, not converted. Already-converted
        /// ship bank is kept.
        /// </summary>
        private void EndRun(string cause)
        {
            ClearChainState();
            CancelSequentialDraws();
            WipeUnbanked();
            RunReport report = BuildRunReport(cause);
            _meta.PodEjections++;
            LastRunReport = report;
            SetPhase(RunPhase.Lost);
            long net = report.grossEarned - report.repairSpent;
            Log.Info(Tag, "Run ended (" + cause + "). Gross " + report.grossEarned +
                          ", repair spent " + report.repairSpent + ", net " + net +
                          "; nothing refunded.");
        }

        /// <summary>
        /// The run-SUCCESS path: Wave 5 terminal with nothing left to bank. Bookkeeping only
        /// — leftover Unbanked is wiped if any remains (should already be 0). CleanExits
        /// increments (PodEjections does NOT) and the run goes terminal Escaped. Called only
        /// from NotifyAssaultCleared. Hatch-bank after the field is stripped does not call this.
        /// </summary>
        private void CompleteRun()
        {
            WipeUnbanked();
            RunReport report = BuildRunReport("cleared");
            _meta.CleanExits++;
            AllWavesCleared = true;
            LastRunReport = report;
            SetPhase(RunPhase.Escaped);
            long net = report.grossEarned - report.repairSpent;
            Log.Info(Tag, "Run CLEARED: all " + WaveDirector.MaxWaves + " waves down. Gross " +
                          report.grossEarned + ", repair spent " + report.repairSpent +
                          ", net " + net + ".");
        }

        private void DripTick(float dt)
        {
            long units = HatchBank.DripUnits(dt, ref _dripAcc);
            CreditConverted(units);
        }

        private void DumpUnbanked()
        {
            CreditConverted(Unbanked);
        }

        private void AttritUnbanked(long units)
        {
            if (units <= 0L) return;
            if (units > Unbanked) units = Unbanked;
            Unbanked -= units;
        }

        private void WipeUnbanked()
        {
            Unbanked = 0L;
            _dripAcc = 0f;
            _withdrawAcc = 0f;
        }

        private void CreditConverted(long unbankedTaken)
        {
            if (unbankedTaken <= 0L) return;
            if (unbankedTaken > Unbanked) unbankedTaken = Unbanked;
            Unbanked -= unbankedTaken;
            long credited = HatchBank.BankedFrom(unbankedTaken, _hatchConvertMul);
            if (credited <= 0L) return;
            Salvage.Earn(credited);
            _meta.EarnSalvage(credited);
        }

        RunReport BuildRunReport(string cause)
        {
            return new RunReport
            {
                cause = cause,
                grossEarned = Salvage.RunSalvage,
                repairSpent = RepairSalvageSpent
            };
        }

        private void SetPhase(RunPhase phase)
        {
            Phase = phase;
            var handler = PhaseChanged;
            if (handler != null) handler(phase);
        }
    }
}
