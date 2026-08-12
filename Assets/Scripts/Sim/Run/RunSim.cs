using System;
using GatekeeperBastion.Util;

namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// How the run ended, and what it was worth. Currency is banked per kill; repair spend is
    /// deducted from the bank as it happens. Gross and repair are carried separately — net is
    /// <c>grossEarned - repairSpent</c>, derived by the caller. There is no pre-subtracted
    /// "earned" field; that is what made the end screen lie.
    /// </summary>
    [Serializable]
    public struct RunReport
    {
        /// <summary>What ended the run: "hatch" (Hatch destroyed), "player" (player down), or
        /// "cleared" (all waves cleared — a SUCCESS).</summary>
        public string cause;

        /// <summary>Whole salvage units banked from kills this run. Already in the purse; equals
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
    /// or Escaped (all MaxWaves waves cleared). Death is the only FAILURE ender: either the Hatch
    /// is destroyed or the player goes down (NotifyPlayerDown) — both funnel through one EndRun
    /// path. The tether cannot be cut at will: there is no bail-out path in v0.
    /// BeginEscape/spool ticking were removed; they return with the escape-window design.
    ///
    /// Waves are DRAW-TRIGGERED and there are exactly WaveDirector.MaxWaves of them: while
    /// Tethered the sim alternates Prep ↔ Assault. There is no wave timer — the player pulls
    /// TriggerDraw() to start each assault, and the game layer calls NotifyAssaultCleared() when
    /// the wave is dead. Clearing the LAST wave completes the run.
    ///
    /// Deep start: StartRun(N) remembers a start wave. The first TriggerDraw delivers
    /// waves 1..N back to back — each next wave starts InterWaveGap seconds after the previous
    /// wave's *delivery* finishes, with no Prep between them. Overlap in the room is the point.
    /// After that chain clears, the run continues normally through wave MaxWaves.
    ///
    /// ECONOMY: AwardKill() banks MetaUpgrades.SalvagePerKill(meta.ScavengingLevel) times the
    /// siphon multiplier straight into MetaState.BankedSalvage. RepairHatch spends from that
    /// bank at ShipSystems.RepairSalvagePerHp as healing is applied. Nothing is staked or
    /// forfeited at run end — dying and clearing keep the same kill take, and repair spend is
    /// already gone. The two endings differ only in which counter ticks (PodEjections vs
    /// CleanExits).
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

        /// <summary>Seed passed at construction; all future run randomness derives from it.</summary>
        public int Seed { get; private set; }

        /// <summary>Deterministic RNG seeded from the ctor. Draw ALL sim randomness from here — never statics.</summary>
        public Random Rng { get; private set; }

        /// <summary>Current run phase. Lost (lost) and Escaped (completed) are both terminal.</summary>
        public RunPhase Phase { get; private set; }

        /// <summary>Tethered sub-state: Prep (quiet, dial free) or Assault (draw live, kills pay).
        /// Meaningful only while Phase == Tethered; always Prep otherwise.</summary>
        public AssaultState Assault { get; private set; }

        /// <summary>The risk/reward siphon dial.</summary>
        public TetherSiphon Siphon { get; private set; }

        /// <summary>The defended objective (the Hatch back to your ship).</summary>
        public ShipSystems Ship { get; private set; }

        /// <summary>Per-run salvage tally, for display. Purely informational — the real currency
        /// lives in MetaState.BankedSalvage and is credited per kill. Resets with each new run
        /// (each run gets a fresh RunSim and therefore a fresh ledger).</summary>
        public SalvageLedger Salvage { get; private set; }

        /// <summary>Filled when the run ends: gross kill take and repair spend. Bookkeeping only —
        /// salvage already moved at kill/repair time. Net is derived by the caller.</summary>
        public RunReport LastRunReport { get; private set; }

        /// <summary>Salvage spent on hatch repair this run. Deducted from the bank as healing is
        /// applied; reported alongside gross on the end screen.</summary>
        public long RepairSalvageSpent { get; private set; }

        /// <summary>Number of waves launched so far this run (0 before the first wave starts,
        /// never above WaveDirector.MaxWaves).</summary>
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

        /// <summary>True once the LAST wave (index MaxWaves-1) has been cleared via
        /// NotifyAssaultCleared — i.e. the run was completed rather than lost.</summary>
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

        public long CurrentKillAward => (long)Math.Floor((double)CurrentKillValue);

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
            Phase = RunPhase.Docked;
            Assault = AssaultState.Prep;

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
        /// Pull the Siphon Draw: Prep → Assault. Only valid in Tethered+Prep, and only while
        /// waves remain — a run is exactly WaveDirector.MaxWaves draws of *content*, so once
        /// CurrentWaveIndex has reached MaxWaves this is a logged no-op. Launches the next wave
        /// (WaveStarted fires). When StartWave &gt; 1 and this is the first draw, enters a
        /// chained delivery through that wave.
        /// </summary>
        public void TriggerDraw()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep)
            {
                Log.Info(Tag, "TriggerDraw ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            if (CurrentWaveIndex >= WaveDirector.MaxWaves)
            {
                Log.Info(Tag, "TriggerDraw refused: all " + WaveDirector.MaxWaves + " waves already drawn.");
                return;
            }
            Assault = AssaultState.Assault;

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

            LaunchWave();
            if (_chainActive && CurrentWaveIndex <= _chainUntil)
                _awaitingDelivery = true;
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
            if (CurrentWaveIndex > 0) CurrentWaveIndex--;
            ClearChainState();
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
        }

        /// <summary>
        /// Game layer reports one gatekeeper killed. An enemy dying IS the payday: the kill is
        /// worth CurrentKillValue (Scavenging's 25 + 5*level times the siphon's dial multiplier),
        /// which is added to
        /// the run tally for display AND credited to MetaState.BankedSalvage immediately. Once
        /// credited it can never be lost — dying later takes nothing back.
        ///
        /// Valid only while a draw is live (Tethered + Assault): kills pay nothing in Prep,
        /// Docked, or after the run has ended.
        /// </summary>
        public void AwardKill()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Assault)
            {
                Log.Info(Tag, "AwardKill ignored (phase " + Phase + ", " + Assault + ")");
                return;
            }
            // Floor once: the bank and the run tally must agree to the unit. Crediting the raw
            // float to the tally and the floor to the bank was the end-screen drift.
            long award = CurrentKillAward;
            Salvage.Earn(award);
            _meta.EarnSalvage(award);
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

        /// <summary>
        /// Game layer reports the PLAYER dead. Death is the only way a run FAILS in v0, and
        /// player death is exactly as terminal as losing the Hatch: PodEjections increments and
        /// the run goes Lost. No salvage is lost — everything the run earned was banked at
        /// kill time. Valid in either Tethered sub-state (Prep or Assault); ignored while
        /// Docked or already terminal.
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
        /// MetaState.DeepestWaveReached to CurrentWaveIndex when higher. If that was the LAST
        /// wave the run is COMPLETE and immediately finishes through CompleteRun. Ignored outside
        /// Tethered+Assault, and ignored while ChainPending (more waves still scheduled).
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

            // Cleared through CurrentWaveIndex (1-based wave number already launched).
            if (CurrentWaveIndex > _meta.DeepestWaveReached)
                _meta.DeepestWaveReached = CurrentWaveIndex;

            ClearChainState();
            Assault = AssaultState.Prep;
            var handler = AssaultEnded;
            if (handler != null) handler();

            // Re-check the phase: an AssaultEnded listener is free to end the run (kill the
            // player, drop the hatch), and a run already lost must not also complete.
            if (Phase == RunPhase.Tethered && CurrentWaveIndex >= WaveDirector.MaxWaves)
                CompleteRun();
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
            WaveSpec spec = _director.NextWave(CurrentWaveIndex, Siphon.Level);
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
        /// The single run-death path: hatch destruction and player death both land here.
        /// BOOKKEEPING ONLY — no salvage moves, because every kill was banked when it happened.
        /// Bumps PodEjections, records the report (cause + what the run earned), and goes
        /// terminal Lost. Callers guard Phase == Tethered.
        /// </summary>
        private void EndRun(string cause)
        {
            ClearChainState();
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
        /// The run-SUCCESS path: all WaveDirector.MaxWaves waves cleared. Also bookkeeping only —
        /// clearing the bastion pays exactly what the kills paid, no more. CleanExits increments
        /// (PodEjections does NOT) and the run goes terminal Escaped. Called only from
        /// NotifyAssaultCleared, which guards Phase == Tethered.
        /// </summary>
        private void CompleteRun()
        {
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
