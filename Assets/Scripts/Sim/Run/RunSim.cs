using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
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
    /// or Escaped (Master Control's Hatch-bank half survived). Death is the only FAILURE ender:
    /// either the Hatch is destroyed or the player goes down (NotifyPlayerDown) — both funnel
    /// through one EndRun path. The tether cannot be cut at will: there is no bail-out path
    /// in v0. BeginEscape/spool ticking were removed; they return with the escape-window design.
    /// Hatch-bank after the field is stripped stays Tethered so the opened door is usable.
    /// Wave 5 terminal does not Escaped.
    ///
    /// Waves are DRAW-TRIGGERED and there are exactly WaveDirector.MaxWaves of them: while
    /// Tethered the sim alternates Prep ↔ Assault. A field terminal Interact is one draw.
    /// ArmSequentialDraws remains for later terminals that re-lock banking; the field loop
    /// does not arm it.
    ///
    /// Deep start: StartRun(N) remembers a start wave. The first TriggerDraw delivers
    /// waves 1..N back to back — each next wave starts InterWaveGap seconds after the previous
    /// wave's *delivery* finishes, with no Prep between them. Overlap in the room is the point.
    /// After that chain clears, the run continues normally through wave MaxWaves.
    ///
    /// ECONOMY (re-keyed 2026-08-25, Zo — "it must be impossible to die with no progress to
    /// show for it"): AwardKill() banks the floored award straight into the realm's pot at
    /// face value. A guest plays in somebody else's world and it is that world's salvage that
    /// fills, so the award is not split across seats — the whole thing lands in the one pot,
    /// which is the same total the split used to produce.
    /// Unbanked still exists but fills ONLY on a deliberate Withdraw at the Hatch, so nothing
    /// is at risk that the player did not choose to risk. A down seat's staked pile FREEZES;
    /// a death with no respawn, and the run's end, wipe leftover Unbanked — the bank behind it
    /// survives both. The Ante and the respawn fee spend the BANK (SpendBanked), as does
    /// RepairHatch. Hatch-bank drip/dump converts a staked pile back at a carry multiplier.
    /// </summary>
        public sealed partial class RunSim
        {
        private const string Tag = "RunSim";

        /// <summary>The demo zone whose RESOLVED bank window — survived or lost — is the
        /// authored Primary gate (Zo, 2026-08-25).</summary>
        private const int Zone2 = 2;

        private readonly MetaState _meta;
        private readonly WaveDirector _director = new WaveDirector();
        private readonly PlayerWallets _wallets = new PlayerWallets();
        /// <summary>Salvage already credited this Hatch-bank window, per slot. Cap is cumulative.</summary>
        private readonly long[] _convertedThisBank = new long[WaveDirector.PlayerCapacity];
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
        int _hatchCarry = 1;
        float _dripAcc;
        float _withdrawAcc;
        float _hatchBankTimer;
        MasterControlPhase _masterPhase;
        int _masterLeft;
        bool _masterHatchMulLocked;

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

        /// <summary>Per-run Ante switch. Defaults to <see cref="Ante.Enabled"/> (parked).
        /// Spend-path tests set this; production never writes it.</summary>
        public bool AnteEnabled { get; set; }

        /// <summary>The defended objective (the Hatch back to your ship).</summary>
        public ShipSystems Ship { get; private set; }

        /// <summary>Per-run tally of what this Run put in the realm's bank: kills as they
        /// land, plus a Convert's multiplied return, minus anything withdrawn back out and
        /// still at risk. Resets with each new run (each run gets a fresh RunSim and therefore
        /// a fresh ledger).</summary>
        public SalvageLedger Salvage { get; private set; }

        /// <summary>Slot 0's STAKED pile (solo / host) — salvage the player deliberately
        /// withdrew from the bank and put back at risk. Kills do not fill it (they bank on
        /// landing, 2026-08-25); only <see cref="Withdraw(float, int)"/> does. A death wipes
        /// it. Hatch-bank start looks at <see cref="TotalUnbanked"/>, and a Convert is the
        /// only thing that turns it back into banked salvage, multiplied.</summary>
        public long Unbanked
        {
            get { return _wallets.Of(0); }
            internal set { _wallets.Set(0, value); }
        }

        /// <summary>Sum of every connected slot's unbanked. Hatch-bank start/refuse uses this.</summary>
        public long TotalUnbanked => _wallets.Total;

        /// <summary>Connected players this Run is counting. 1 in solo. Capped at
        /// <see cref="WaveDirector.PlayerCapacity"/>.</summary>
        public int PlayerCount => _wallets.Count;

        /// <summary>Unbanked spent raising intensity this pull. Reset on idle Prep after the pull.</summary>
        public long JuiceSpentThisPull { get; internal set; }

        /// <summary>Unbanked pile for a connected slot. Slot 0 is solo / host.</summary>
        public long UnbankedOf(int slot) => _wallets.Of(slot);

        /// <summary>Field Waves cleared since the last Hatch-bank. 0 until the first Terminal draw.</summary>
        public int WavesSinceBank { get; private set; }

        /// <summary>Carry snapshotted at the current Hatch-bank start (1..MaxWaves).</summary>
        public int HatchCarry => _hatchCarry;

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
        public bool ChainPending =>
            _chainActive && (_awaitingDelivery || _interWaveTimer > 0f || CurrentWaveIndex <= _chainUntil);

        /// <summary>True while a field-terminal sequence still has a follow-up draw armed
        /// or counting down in Prep.</summary>
        public bool SequentialPending => _followUpsLeft > 0 || _followUpTimer > 0f;

        /// <summary>Seconds of Prep left before a sequential follow-up draw. 0 when none.</summary>
        public float SequentialFollowUpRemaining => _followUpTimer;

        /// <summary>True once the run completed through CompleteRun (Master Control Hatch-bank
        /// half survived) rather than lost.</summary>
        public bool AllWavesCleared { get; private set; }

        /// <summary>None until <see cref="StartMasterControl"/>; Terminal then Hatch.</summary>
        public MasterControlPhase MasterPhase => _masterPhase;

        /// <summary>True while a Master Control sequence is driving Waves.</summary>
        public bool MasterSequenceLive => _masterPhase != MasterControlPhase.None;

        /// <summary>Waves left in the live Master Control half, including the current one.</summary>
        public int MasterWavesLeft => _masterLeft;

        /// <summary>True during the terminal half — the Control Room door should be shut.</summary>
        public bool ControlRoomSealed => _masterPhase == MasterControlPhase.Terminal;

        /// <summary>Base salvage one kill is worth before the siphon multiplier: the Scavenging
        /// attribute's 25 + 5*level. Read fresh from the MetaState so a mid-session purchase is
        /// live on the very next kill.</summary>
        private float BaseKillValue => MetaUpgrades.SalvagePerKill();

        /// <summary>Salvage the next kill is worth right now:
        /// MetaUpgrades.SalvagePerKill() times the siphon's hand-authored dial
        /// multiplier (1 / 1.1 / 1.15 / 1.25 / 1.35 / 1.5) — 25 at both dials down, 46.875 at
        /// Scavenging 5 / siphon 5. The HUD shows this; it is the whole income model.</summary>
        public float KillValueAt(int siphonLevel) =>
            BaseKillValue * TetherSiphon.RewardMultiplierFor(siphonLevel);

        public float CurrentKillValue => KillValueAt(Siphon.Level);

        /// <summary>True while the zone's authored bodies are spent and the field is dripping
        /// until the player reaches the terminal (<see cref="FieldTrickle"/>). Set by the Game
        /// layer, which is the half that knows about terminals.</summary>
        public bool FieldTrickleLive { get; private set; }

        /// <summary>Game layer: the field has run dry / has been shut off. Idempotent.</summary>
        public void SetFieldTrickle(bool live)
        {
            if (FieldTrickleLive == live) return;
            FieldTrickleLive = live;
            Log.Event(Tag, live
                ? "field trickle on — the lane holds until the terminal is shut off"
                : "field trickle off");
        }

        /// <summary>What one body pays right now. Zero inside a Convert window (the stake is
        /// the pay there), and zero for a TRICKLE body — those are a clock, not an income, and
        /// paying for them would turn walking the lane into farming it (Zo, 2026-08-26).</summary>
        public long CurrentKillAward =>
            Mode == WaveMode.HatchBank || FieldTrickleLive
                ? 0L
                : (long)Math.Floor((double)CurrentKillValue);

        /// <summary>Seconds left on the Hatch-bank convert window. 0 outside Hatch-bank Assault.</summary>
        public float HatchBankRemaining => _hatchBankTimer;

        /// <summary>Sim seconds elapsed while Tethered.</summary>
        public float RunElapsed { get; private set; }

        /// <summary>Raised each time a wave launches (TriggerDraw or a chained follow-up), with its spec.</summary>
        public event Action<WaveSpec> WaveStarted;

        /// <summary>Raised when NotifyAssaultCleared returns the run to Prep (including after the
        /// final wave, immediately before the run completes).</summary>
        public event Action AssaultEnded;

        /// <summary>Raised when Master Control's terminal half has cleared and the Hatch half
        /// is armed (door should open; Hatch-bank follow-up is on InterWaveGap).</summary>
        public event Action MasterHatchHalfStarted;

        /// <summary>Raised on every phase transition (also once for Docked on StartRun).</summary>
        public event Action<RunPhase> PhaseChanged;

        /// <summary>Raised once per FIELD Wave cleared, with that Wave's 1-based number
        /// (clamped to <see cref="WaveDirector.MaxWaves"/>, the same clamp
        /// <see cref="MetaState.DeepestWaveReached"/> uses). Two call sites raise it and no
        /// others: <see cref="Tick"/> when a chained composite advances off a Wave, and
        /// <see cref="NotifyAssaultCleared"/> for the Wave that ends the assault — which is
        /// the last Wave of a chain, or the only Wave of a single draw. A Hatch-bank window
        /// is not a field Wave and never raises it. Per-Wave granularity is what lets a
        /// composite that dies mid-chain record the Waves it did clear
        /// (Docs/Demo - Single/5-repeat-to-control-room.md, the zone retrigger).</summary>
        public event Action<int> FieldWaveCleared;

        public RunSim(MetaState meta, int seed)
        {
            if (meta == null) throw new ArgumentNullException("meta");
            _meta = meta;
            Seed = seed;
            Rng = new Random(seed);
            Siphon = new TetherSiphon();
            AnteEnabled = Ante.Enabled;
            Ship = new ShipSystems(MetaUpgrades.HatchHp(meta.HatchArmorLevel));
            Salvage = new SalvageLedger();
            Phase = RunPhase.Docked;
            Assault = AssaultState.Prep;
            Mode = WaveMode.Terminal;

            Ship.HatchDestroyed += OnHatchDestroyed;
        }

        /// <summary>Begin the run in Docked. Callable once.
        /// <paramref name="startWave"/> is 1-based and clamped to 1..min(MaxWaves, DeepestWaveReached).
        /// <paramref name="countRun"/> ticks the realm's TotalRuns and defaults to true. It is
        /// false only for a sim that is a local shadow of a run owned elsewhere: the run belongs
        /// to the realm it is played in, and that realm's document is not on this machine. Every
        /// other effect — the phase, the clamped start wave, the once-only latch — still
        /// applies, because the shadow drives that machine's own phase and UI.</summary>
        public void StartRun(int startWave = 1, bool countRun = true)
        {
            if (_runStarted)
            {
                Log.Info(Tag, "StartRun ignored: run already started.");
                return;
            }
            _runStarted = true;
            if (countRun) _meta.TotalRuns++;
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
            SeedRespawns();
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

        /// <summary>Paid intensity at a looked-at trigger. Lowering is free and does not refund.</summary>
        public bool TryJuice(int delta, bool atTrigger) => IntensityJuice.Try(this, delta, atTrigger);

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
        /// refuses only if Unbanked is 0, and never deep-start chains. There is no per-Run
        /// cap on Hatch-banks (Docs/Demo - Single/5-repeat-to-control-room.md, Delete).
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
                // The Master finale used to borrow this mode for its last window and needed
                // an exemption from the unbanked check. It is a Terminal-mode wave now
                // (2026-08-28), so a Hatch-bank draw is once again only ever a Convert and
                // the exemption is gone with the thing that needed it.
                if (TotalUnbanked <= 0L)
                {
                    Log.Info(Tag, "hatch-bank refused — unbanked=0");
                    return;
                }
                SnapshotHatchCarry();
                ApplyFaucetConvertBonusIfPending();
                _dripAcc = 0f;
                ResetConvertedThisBank();
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
            if (mode != WaveMode.HatchBank) BeginFaucetCycleIfEligible();
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

        /// <summary>Hatch damage, on BOTH Wave modes (Zo, 2026-08-27: the field Wave gets a
        /// stake). Terminal Waves used to leave the Hatch invulnerable, which made every body
        /// that reached it inert — most visibly the Thief, whose only weapon is hatchDamage and
        /// which therefore could affect nothing at all on a field Wave: 12 of 13 walked the
        /// whole run untouched and unthreatening. An objective nobody can act on is not an
        /// objective. THIS ADDS A SECOND WAY TO LOSE A FIELD WAVE (cause "hatch") while the
        /// player is 66-156 m up the spine at a terminal — that is the intended cost, and it is
        /// what makes leaving the Hatch a decision rather than a formality.</summary>
        public void DamageHatch(float amount)
        {
            if (amount <= 0f)
                return;
            // Scaled BEFORE the hatch and the attrition both read it, so a field Wave cannot
            // bleed the carried stake at a rate the Hatch itself is armoured against.
            if (Mode == WaveMode.Terminal && !_faucetCycleLive)
                amount *= HatchBank.FieldWaveDamageScale;
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
            CancelMasterSequence();
            // Every Assault exit clears the drip flag, this one included: an abort that left
            // it set would make the redrawn wave's kills pay zero until the next wave start
            // happened to reset it.
            SetFieldTrickle(false);
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
            CancelMasterSequence();
        }

        /// <summary>
        /// Game layer reports one gatekeeper killed. BANKS the floored kill award into the
        /// realm's pot at face value, the moment the body falls (Zo, 2026-08-25) — see
        /// <see cref="BankKill"/>. It does not touch the carried pile and is not split across
        /// <see cref="PlayerCount"/>: every seat banks into the one pot, which is the same
        /// total the old per-seat split produced. Valid only while Tethered + Assault.
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
            BankKill(award);
        }

        /// <summary>KILLS BANK AS THEY LAND (Zo, 2026-08-25). A Run must never end with
        /// nothing to show for it, and the only two things that used to bank — reaching a
        /// terminal and surviving a Convert — both sit past where a starting kit dies. So a
        /// field kill is credited to the realm the moment the body falls, at face value:
        /// there is no carry multiplier on it, because nothing was carried.
        /// <para>The carried pile does not disappear — <see cref="TotalUnbanked"/> is now
        /// filled only by a deliberate Withdraw at the Hatch, which is what turns the Convert
        /// window from a toll into an opt-in bet.</para></summary>
        void BankKill(long award)
        {
            if (award <= 0L) return;
            _meta.EarnSalvage(award);
            Salvage.Earn(award);
        }

        /// <summary>Spend from the realm's bank — the Ante and the respawn fee both charge
        /// here now that kills bank on landing. False leaves the bank untouched.</summary>
        internal bool SpendBanked(long units)
        {
            if (units <= 0L) return true;
            if (_meta == null || _meta.BankedSalvage < units) return false;
            return _meta.SpendSalvage(units);
        }

        /// <summary>The realm's bank, read by the verbs that used to read the carried pile.</summary>
        public long BankedSalvage => _meta == null ? 0L : _meta.BankedSalvage;

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
        public long Withdraw(float dt) => Withdraw(dt, 0);

        /// <summary>Slot-aware withdraw: the pot's units land in the OPERATING seat's
        /// pile, whoever holds the verb — never implicitly slot 0. Symmetry ruling
        /// (Docs/Decisions.md): the pot may pre-fund any living seat's pocket in Prep,
        /// and rescues nobody in particular.</summary>
        public long Withdraw(float dt, int slot)
        {
            if (dt <= 0f) return 0L;
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Prep) return 0L;
            var hatch = Ship.Hatch;
            if (hatch.IsDestroyed) return 0L;
            if (hatch.MaxHp - hatch.Hp > 0.01f) return 0L;

            // Check the destination BEFORE touching the bank. PlayerWallets.Add silently
            // returns for a slot the sim is not counting, so debiting first would take the
            // salvage out of the bank AND out of the tally and hand it to nobody — an
            // unlogged destruction, reachable in the window where a seat has joined but
            // RefreshPlayerCount has not caught up.
            if (slot < 0 || slot >= PlayerCount)
            {
                Log.Info(Tag, "withdraw refused: slot " + slot + " is outside "
                    + PlayerCount + " counted seat(s)");
                return 0L;
            }
            long want = HatchBank.WithdrawUnits(dt, ref _withdrawAcc);
            if (want <= 0L) return 0L;
            long bank = _meta.BankedSalvage;
            if (want > bank) want = bank;
            if (want <= 0L) return 0L;
            if (!_meta.SpendSalvage(want)) return 0L;
            // Out of the bank means out of the Run's banked tally, or the same salvage is
            // counted twice when it converts back through CreditSlot. See SalvageLedger.Unearn.
            Salvage.Unearn(want);
            _wallets.Add(slot, want);
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

        /// <summary>The player reached the zone's terminal and shut it off. THE LOOP
        /// (Zo, 2026-08-25): a wave begins at the Hatch and ends at a terminal — it does not
        /// have to be killed to the last body. Cancels the rest of the zone's chained draws
        /// so the whole trigger ends here, then completes exactly as a cleared wave does, so
        /// deepest-wave bookkeeping, the terminal's own completion and the door all run
        /// through one path. Refuses during a Hatch-bank window: a Convert is not a field
        /// wave and has no off switch.</summary>
        public bool ShutOffFieldWave()
        {
            if (Phase != RunPhase.Tethered || Assault != AssaultState.Assault)
            {
                Log.Info(Tag, "ShutOffFieldWave ignored (phase " + Phase + ", " + Assault + ")");
                return false;
            }
            if (Mode == WaveMode.HatchBank)
            {
                Log.Info(Tag, "ShutOffFieldWave refused: a Hatch-bank window has no off switch");
                return false;
            }
            CancelSequentialDraws();
            ClearChainState();
            // The sim owns its own flag at every Assault exit: leaving it for the Game layer
            // to clear is a cross-seam contract, and an exit path that misses it makes the
            // NEXT wave pay zero until something else resets it.
            SetFieldTrickle(false);
            // Reaching the switch IS the payout: the whole carried pile banks here, so the
            // walk up the lane is what secures it and the trip home is not a second toll.
            long carried = TotalUnbanked;
            if (carried > 0L)
            {
                DumpUnbanked();
                Log.Event(Tag, "terminal shutoff banked " + carried);
            }
            NotifyAssaultCleared();
            // The zone is closed: the Hatch will take the player home from here until they
            // start the next one. Raised AFTER the clear so the offer outlives the assault.
            OfferDockAfterZoneShutoff();
            return Assault == AssaultState.Prep;
        }

        /// <summary>
        /// Game layer reports the wave dead (all spawned gatekeepers down, spawning finished, and
        /// any deep-start chain fully delivered): Assault → Prep and AssaultEnded fires. Raises
        /// MetaState.DeepestWaveReached to CurrentWaveIndex when higher. Terminal Wave 5 never
        /// Completes. Hatch-bank dump after the field is stripped stays Prep so progression
        /// through that door continues. Master Control's Hatch half Completes on its last window.
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
                // Unconditional again: the only Hatch-bank window left is a real Convert, and
                // the finale that used to be exempted here no longer runs in this mode.
                DumpUnbanked();
            }

            // Cleared through CurrentWaveIndex (1-based wave number already launched).
            int deepest = CurrentWaveIndex;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (deepest > _meta.DeepestWaveReached)
                _meta.DeepestWaveReached = deepest;
            for (int i = 0; i < PlayerCount; i++)
                _wallets.SetDeepest(i, deepest);

            // Raise site 2 of 2. ChainPending above already refused every earlier Wave of a
            // chain, so this is the LAST Wave of a chain (or the only Wave of a single draw)
            // and cannot double-fire with the advance in Tick. Mode is still the window's, so
            // the helper's own Hatch-bank guard is what drops a bank window here.
            RaiseFieldWaveCleared(deepest);

            ClearChainState();
            Assault = AssaultState.Prep;
            // Invariant: FieldTrickleLive never survives an Assault exit. This, AbortAssault,
            // ShutOffFieldWave and the run-end sweep are all four exits; each clears it
            // locally so no Game-layer caller is owed the reset.
            SetFieldTrickle(false);
            var handler = AssaultEnded;
            if (handler != null) handler();

            OfferDockIfBankSurvived(hatchBank);

            if (Phase == RunPhase.Tethered && _masterPhase != MasterControlPhase.None)
            {
                AdvanceMasterControl(hatchBank);
                return;
            }

            if (FaucetOwnsClear(hatchBank)) return;

            if (Phase == RunPhase.Tethered && CurrentWaveIndex >= WaveDirector.MaxWaves
                && _followUpsLeft <= 0)
            {
                string msg = hatchBank
                    ? "hatch-bank done — unbanked=" + TotalUnbanked + " door is open"
                    : TotalUnbanked > 0L
                        ? "field stripped — unbanked=" + TotalUnbanked + " hatch-bank to convert"
                        : "field stripped — unbanked=0 Master Control is the win";
                Log.Info(Tag, msg);
            }

            if (Phase == RunPhase.Tethered && _followUpsLeft > 0)
            {
                _followUpsLeft--;
                _followUpTimer = WaveDirector.TerminalChainGap;
            }
            else
            {
                CancelSequentialDraws();
                Siphon.Level = 0;
                JuiceSpentThisPull = 0L;
            }
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

            // Before the Hatch-bank branch below, which returns: a seat bleeds out on the same
            // clock in every mode.
            if (TickBleedOut(dt)) BleedOutExpired = true;

            if (Assault == AssaultState.Assault && Mode == WaveMode.HatchBank)
            {
                if (TotalUnbanked > 0L)
                    DripTick(dt);
                _hatchBankTimer -= dt;
                if (_hatchBankTimer <= 0f)
                {
                    _hatchBankTimer = 0f;
                    NotifyAssaultCleared();
                }
                return;
            }

            if (FaucetChainTick(dt)) return;

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

            // Raise site 1 of 2. CurrentWaveIndex is still the outgoing Wave's 1-based number
            // here — LaunchWave increments it. Raised only past both guards: when either
            // returns, the chain is over and NotifyAssaultCleared raises for this Wave instead.
            RaiseFieldWaveCleared(CurrentWaveIndex);
            LaunchWave();
            if (CurrentWaveIndex <= _chainUntil)
                _awaitingDelivery = true;
        }

        private void LaunchWave()
        {
            ClearDockOffer();
            int idx = Mode == WaveMode.HatchBank
                ? HatchBank.CarryWaveIndex(_hatchCarry)
                : CurrentWaveIndex >= WaveDirector.MaxWaves
                    ? WaveDirector.MaxWaves - 1
                    : CurrentWaveIndex;
            WaveSpec spec = _director.NextWave(idx, Siphon.Level, PlayerCount);
            if (Mode == WaveMode.HatchBank)
                spec = HatchBank.ApplyHatchComposition(spec, _hatchCarry);
            spec = ConsumeFaucetAxisIfPending(spec);
            // The elite is the last CONTROL-ROOM wave, so this no longer keys on Hatch-bank
            // mode — the phase alone says which wave it is.
            if (_masterPhase == MasterControlPhase.Elite)
                spec = MasterControl.ApplyFinaleElite(spec);
            if (Mode != WaveMode.HatchBank)
            {
                CurrentWaveIndex++;
                WavesSinceBank++;
                // Seeing a wave is not beating one: this is the "got this far" record, raised
                // on the draw, and it is deliberately NOT DeepestWaveReached — that one is a
                // permission (deep start reads it) and must stay clear-only.
                int entered = CurrentWaveIndex > WaveDirector.MaxWaves
                    ? WaveDirector.MaxWaves : CurrentWaveIndex;
                if (_meta != null && entered > _meta.DeepestWaveEntered)
                    _meta.DeepestWaveEntered = entered;
            }
            var handler = WaveStarted;
            if (handler != null) handler(spec);
        }

        /// <summary>Fire <see cref="FieldWaveCleared"/> for a 1-based field Wave. The single
        /// place the "not a Hatch-bank window" rule lives, so a future raise site cannot
        /// forget it. Clamped the same way DeepestWaveReached is, so a draw past the field
        /// cannot claim a Wave the director does not author. Allocation-free: Tick calls it.</summary>
        void RaiseFieldWaveCleared(int wave)
        {
            if (Mode == WaveMode.HatchBank || wave < 1) return;
            if (wave > WaveDirector.MaxWaves) wave = WaveDirector.MaxWaves;
            var handler = FieldWaveCleared;
            if (handler != null) handler(wave);
        }

        void SnapshotHatchCarry()
        {
            _hatchCarry = HatchBank.ClampCarry(WavesSinceBank);
            _hatchConvertMul = HatchBank.CarryConvert(_hatchCarry);
            WavesSinceBank = 0;
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
            // ENTERED, NOT REACHED (Zo, 2026-08-27: "Replay didnt let me start at the latest").
            // This read DeepestWaveReached — cleared-only — on the argument that a death must
            // never buy a deeper start. That argument does not survive what a deep start
            // actually does: TriggerDraw chains waves 1..N into the FIRST assault, so choosing
            // wave 5 is fighting one through five back to back with no Prep between them. It
            // buys more fighting, not less, so there is nothing to exploit by dying — the
            // "exploit" costs the exploiter a harder run.
            //
            // The live save is the case: DeepestWaveEntered 5, DeepestWaveReached 2. The player
            // had met wave 5 twice and was offered wave 2, which reads as the game forgetting
            // the run rather than as a rule.
            int deepest = _meta.DeepestWaveEntered;
            if (deepest < _meta.DeepestWaveReached) deepest = _meta.DeepestWaveReached;
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
        /// BOOKKEEPING ONLY — leftover Unbanked is wiped, not converted, with one stated
        /// exception: a Returned end first keeps HatchBank.ReturnedKeepShare of each slot's
        /// pile (KeepReturnedShare). Already-converted ship bank is kept on every cause.
        /// </summary>
        private void EndRun(string cause)
        {
            bool docked = cause == DockedCause;
            FieldTrickleLive = false;   // no drip outlives the Run it was holding open
            ClearChainState();
            CancelSequentialDraws();
            CancelMasterSequence();
            if (docked) KeepReturnedShare();
            WipeUnbanked();
            ClearDockOffer();
            RunReport report = BuildRunReport(cause);
            if (!docked) _meta.PodEjections++;
            // Zone 2's bank falling is the authored loss that puts a Primary on the rack
            // (Docs/Demo - Single/5-repeat-to-control-room.md). Nothing above this line resets Mode or
            // Assault, so they still hold the values the window ended with — a live bank is
            // HatchBank+Assault, and a Lost during the zone's FIELD Waves is Terminal and does
            // not qualify. CurrentWaveIndex is the 1-based number of the last field Wave
            // launched (a Hatch-bank does not advance it), which is what ZoneOfWave takes.
            // PodEjections is untouched: both loss kinds still move it.
            // WaveIsIn, not ZoneOfWave: the latter FLOORS a Wave past the last authored zone
            // onto that zone, and both the Master Control terminal half and the terminals
            // past the table's end draw past it — so a floored read credited the finale's
            // lost window to zone 2's bank.
            // The LOST half of "zone 2 resolved"; the survived half is
            // OfferDockIfBankSurvived's twin write (one flag, either outcome).
            if (!docked && _meta != null && _meta.DemoActive
                && Mode == WaveMode.HatchBank && Assault == AssaultState.Assault
                && DemoZones.WaveIsIn(Zone2, CurrentWaveIndex))
                _meta.DemoZone2BankLost = true;
            LastRunReport = report;
            SetPhase(docked ? RunPhase.Returned : RunPhase.Lost);
            long net = report.grossEarned - report.repairSpent;
            Log.Info(Tag, "Run ended (" + cause + "). Gross " + report.grossEarned +
                          ", repair spent " + report.repairSpent + ", net " + net +
                          "; nothing refunded.");
        }

        /// <summary>
        /// The run-SUCCESS path: Master Control Hatch-bank half survived. Bookkeeping only
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
            Log.Info(Tag, "Run CLEARED: Master Control shut off at its terminal. Gross " +
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
