using System;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: Bastion-scene bootstrap that owns the RunContext, ticks its RunSim
    /// every frame, and exposes the command methods all sim mutation flows through. Consumers
    /// hold it by serialized scene reference or receive it via Bind — never through a static.</summary>
    /// <remarks>
    /// Scene flow: the ship hub is now a real scene (SceneFlow.Ship), so entering the Bastion
    /// starts fighting immediately — Start() calls StartRun + EngageTether and Docked is a
    /// transient boot phase. A run now ends TWO ways: death (player death via PlayerDied →
    /// Sim.NotifyPlayerDown, or hatch destruction) lands in Lost, and clearing all
    /// WaveDirector.MaxWaves waves lands in Escaped. Both are terminal and both leave by exactly
    /// one door: ReturnToShip (save + load the Ship scene) — the old in-place ResetRun/Respawn
    /// path is gone. MetaState comes from GameSession (with a direct-play fallback); no
    /// Time.timeScale pausing — the sim ignores Tick outside Tethered.
    ///
    /// Money: GatekeeperKilled → Sim.AwardKill() banks the kill into MetaState.BankedSalvage
    /// immediately. RepairHatch spends from that bank as healing is applied. Ending a run —
    /// well or badly — settles nothing and refunds nothing; the purse is also spent at the
    /// ship's stations between runs.
    ///
    /// Terminal-phase contract (the anti-soft-lock rules — see GameController.EndScreen.cs, which
    /// applies all of them to Lost AND Escaped alike, because a winning player trapped behind a
    /// captured cursor is the same bug as a losing one):
    ///   * the cursor is freed AND made visible every frame in a terminal phase, not just on the
    ///     transition, so a station panel or a stray click can never re-capture it;
    ///   * Enter / Space / Escape are keyboard fallbacks for the RETURN TO SHIP button;
    ///   * if 30s pass on the end screen with no input, or the scene load fails, we Warn.
    /// </remarks>
    public sealed partial class GameController : MonoBehaviour
    {
        const string Tag = "GameController";

        [SerializeField, Tooltip("0 = randomize seed per run.")]
        int seed;

        [SerializeField, Tooltip("Bastion wave spawner. Authored in the scene.")]
        WaveSpawner _spawner;

        [SerializeField, Tooltip("Hatch repair point. Authored in the scene.")]
        RepairInteractable _repair;

        [SerializeField, OptionalBinding, Tooltip("Bastion net session. Null in solo / tests.")]
        NetSession _net;

        /// <summary>Everything this Run owns (meta, sim, decoy state, spawn ids). Constructed
        /// in Awake; null only on a failed boot (no MetaState).</summary>
        public RunContext Run { get; private set; }

        /// <summary>The authoritative game simulation. MonoBehaviours are thin views over this.</summary>
        public RunSim Sim => Run != null ? Run.Sim : null;

        /// <summary>The player rig's decoy bay — it rides the player rig, so it cannot be wired
        /// as a scene reference here. ApplyMetaEffects binds the bay, which registers itself;
        /// null until then, and again after the bay disables.</summary>
        public DecoyBay Decoys { get; private set; }

        /// <summary>Command from DecoyBay (Bind / re-enable): publish the bay through the controller.</summary>
        public void RegisterDecoyBay(DecoyBay bay) => Decoys = bay;

        /// <summary>Command from DecoyBay.OnDisable: drop the handle — only if it is still this
        /// bay's, so a replacement rig that registered first is not knocked out.</summary>
        public void UnregisterDecoyBay(DecoyBay bay)
        {
            if (Decoys == bay) Decoys = null;
        }


        /// <summary>Persistent cross-run progression state, owned by GameSession.</summary>
        public MetaState Meta => GameSession.Meta;

        /// <summary>Gatekeepers spawned and still alive right now (spawner readout). Used for the
        /// death/hatch-loss context lines; 0 when no spawner is present.</summary>
        public int EnemiesAlive => _spawner != null ? _spawner.AliveCount : 0;

        /// <summary>The scene's wave spawner — the one owner of the live-gatekeeper ledger.
        /// Abilities reach live actors through here rather than a static list. Null on a
        /// spawnerless scene (tests); callers null-check.</summary>
        public WaveSpawner Spawner => _spawner;

        /// <summary>Bastion net session. Null when unwired (solo, Ship, tests).</summary>
        public NetSession Net => _net;

        /// <summary>True once the run has ended (terminal phase) — enemy AI and the spawner
        /// use this as their single "stop everything" test.</summary>
        public bool RunIsOver => Sim == null || IsTerminal(Sim.Phase);

        FirstPersonController _fpc;

        // Hosted fight components, resolved from the rig at run start (ApplyMetaEffects).
        // This controller owns their per-frame advance; see the written order in Update.
        PlayerGun _gun;
        PlayerAbility _ability;
        Interactor _interactor;
        DecoyPlacement _decoyPlacement;

        /// <summary>Field terminal id waiting on the live Wave clear. Null when none pending.</summary>
        string _pendingTerminalId;

        // One GameController per scene, authored; there is no process-global instance left to protect.
        void Awake()
        {
            SceneFlow.EnsureEventSystem();

            // Direct-play/test fallback: entering the Bastion without passing menu/ship.
            GameSession.EnsureLoaded();
            if (Meta == null)
            {
                // Guard trip: RunContext's ctor throws on a null MetaState — fail loudly, not with a stack trace.
                Log.Error(Tag, "No MetaState after GameSession.EnsureLoaded(); the run cannot start.");
                return;
            }

            int runSeed = seed != 0 ? seed : Environment.TickCount;
            Run = new RunContext(GameSession.Meta, runSeed);
            Subscribe();
        }

        void Start()
        {
            if (Sim == null) return; // guard: failed boot must not touch a null Sim
            // The ship-menu phase is a real scene now: boarding is immediate, Docked transient.
            // PendingStartWave is set by MapStation; consumed once so direct-play falls back to 1.
            int startWave = GameSession.PendingStartWave;
            GameSession.PendingStartWave = 1;
            Sim.StartRun(startWave);
            Sim.EngageTether(); // → Tethered (Prep)
            // The spawner's per-frame delivery advance belongs to this controller's
            // written order from here on; its own Update yields.
            if (_spawner != null) _spawner.SetHosted(true);

            ApplyMetaEffects();

            // Stable fields after the prose. The sentence stays readable;
            // wave-report reads startWave=/primary=/… without re-parsing the prose.
            Log.Event("Run", $"run begins at {SceneFlow.BastionName} starting at wave {Sim.StartWave} — " +
                $"{LoadoutSummary(Meta)}, " +
                $"{Meta.BankedSalvage} banked, hatch {Sim.Ship.Hatch.MaxHp:0}hp " +
                $"{RunBeginsFields(Meta, Sim.StartWave)}; seed {Run.Sim.Seed}");
        }

        /// <summary>Primary + Secondary with Tiers for the run-begins line — criterion 12 cannot
        /// be read from a log that does not say which weapon it came from.</summary>
        static string LoadoutSummary(MetaState meta)
        {
            if (meta == null)
                return "no weapon (improvised cutter); no repair rig";
            string weapons = !meta.OwnsAnyWeapon
                ? "no weapon (improvised cutter)"
                : $"{SlotSummary("Primary", meta.EquippedPrimary, meta)}; " +
                  $"{SlotSummary("Secondary", meta.EquippedSecondary, meta)}";
            string rig = meta.HasRepairRigEquipped ? "repair rig equipped" : "no repair rig";
            return $"{weapons}; {rig}";
        }

        static string SlotSummary(string label, WeaponId id, MetaState meta)
        {
            if (id == WeaponId.None) return $"{label}=—";
            return $"{label}={WeaponCatalog.Def(id).displayName} T{meta.TierOf(id)}";
        }

        /// <summary>Machine-readable tail for the run-begins line.</summary>
        static string RunBeginsFields(MetaState meta, int startWave)
        {
            string primary = "none", primaryTier = "0", secondary = "none", secondaryTier = "0";
            string rig = "none";
            long banked = 0;
            if (meta != null)
            {
                banked = meta.BankedSalvage;
                if (meta.EquippedPrimary != WeaponId.None)
                {
                    primary = WeaponCatalog.Def(meta.EquippedPrimary).displayName;
                    primaryTier = meta.TierOf(meta.EquippedPrimary).ToString();
                }
                if (meta.EquippedSecondary != WeaponId.None)
                {
                    secondary = WeaponCatalog.Def(meta.EquippedSecondary).displayName;
                    secondaryTier = meta.TierOf(meta.EquippedSecondary).ToString();
                }
                if (meta.HasRepairRigEquipped) rig = "repair";
            }
            return $"startWave={startWave} primary={primary} primaryTier={primaryTier} " +
                   $"secondary={secondary} secondaryTier={secondaryTier} rig={rig} banked={banked}";
        }

        /// <summary>
        /// THE BASTION FIGHT ADVANCES IN THIS ORDER, FROM THIS ONE CALLER. Hosted components
        /// (SetHosted) yield their own Update; Unity's script order no longer decides anything
        /// the fight depends on.
        ///   1. input sample (Controls.Sample — the sole producer's per-frame read)
        ///   2. RunSim.Tick(dt)
        ///   3. prep commands (siphon draw / dial)
        ///   4. player locomotion
        ///   5. combat actions (gun / ability / decoy placement)
        ///   6. interact / repair
        ///   7. enemy AI — engine-driven views (EnemyGatekeeper.Update); they read no ambient
        ///      input and no process-global state, and their order among themselves is free
        ///   8. spawn delivery (tick-driven group emission)
        ///   9. assault-clear probe — fires inside the spawner on delivery-complete and death
        ///  10. presentation last (HUD / VFX / audio keep their own Update/LateUpdate and Time)
        /// Components tick every frame regardless of phase, as their own Updates always did;
        /// each guards internally. Prep commands are Tethered-only, end-screen tick terminal-only.
        /// </summary>
        void Update()
        {
            if (Sim == null) return; // guard: a failed boot leaves no Sim to Tick

            float dt = Time.deltaTime;

            Controls.Sample();                                        // 1

            // Client-only: the host owns the run. Keep local move + fire so RPCs can fire;
            // do not tick the local sim or the spawner (that would fork a second live sim).
            if (_net != null && _net.IsClientOnly)
            {
                if (_fpc != null) _fpc.Tick(dt);
                if (_gun != null) _gun.Tick(dt);
                return;
            }

            Sim.Tick(dt);                                             // 2

            if (Sim.Phase == RunPhase.Tethered)
                TickFightInput();                                     // 3

            if (_fpc != null) _fpc.Tick(dt);                          // 4
            if (_gun != null) _gun.Tick(dt);                          // 5
            if (_ability != null) _ability.Tick(dt);
            if (_decoyPlacement != null) _decoyPlacement.Tick(dt);
            if (_interactor != null) _interactor.Tick(dt);            // 6
                                                                      // 7 — engine-driven
            if (_spawner != null) _spawner.TickDelivery(dt);          // 8
                                                                      // 9 — inside the spawner
            if (Sim.Phase == RunPhase.Tethered)
                return;

            // BOTH terminal phases run the end-screen tick: Escaped is a win, but it is exactly
            // as much of a dead end as Lost — same cursor, same button, same soft-lock risk.
            if (IsTerminal(Sim.Phase))
                TickLoseScreen();
        }

        /// <summary>Prep-only inputs (siphon draw + dial stepper). Dial is [ ] — the number
        /// row belongs to weapon slots (see Controls), so Prep no longer double-fires on 1/2/3.</summary>
        void TickFightInput()
        {
            // The Bastion tick samples the input frame first each frame; every other consumer's
            // defensive Sample() call is then a free frame-guarded read.
            Controls.Sample();
            if (OptionsScreen.IsOpen || HudController.IsPauseOpen) return;
            if (Sim.Assault != AssaultState.Prep) return;

            if (Controls.Down(ControlId.PullDraw))
            {
                Log.Event("Run", "draw refused — Waves start at a terminal");
                return;
            }

            int delta = 0;
            if (Controls.Down(ControlId.DialUp)) delta = 1;
            else if (Controls.Down(ControlId.DialDown)) delta = -1;
            if (delta == 0) return;

            int before = Sim.Siphon.Level;
            Sim.SetSiphonLevel(before + delta);
            // Only a real change is worth a line; clamp at the ends and phase refusal are not.
            if (Sim.Siphon.Level == before) return;
            Log.Event("Run",
                $"set siphon dial to L{Sim.Siphon.Level} — reward x{Sim.Siphon.RewardMultiplier:0.##} " +
                $"(each kill +{Sim.CurrentKillAward} salvage)");
        }

        /// <summary>Command from WaveSpawner: one gatekeeper is down. This is the ONLY income path
        /// in the game — the sim computes the kill's value from the siphon dial and banks it into
        /// MetaState.BankedSalvage on the spot, so the player owns it before the body lands.
        /// Convention R1: the spawner never calls the sim itself.</summary>
        public void GatekeeperKilled()
        {
            if (Sim == null)
            {
                Log.Warn(Tag, "GatekeeperKilled ignored: no Sim (scene teardown or failed boot).");
                return;
            }
            if (RunIsOver)
            {
                // A gatekeeper dying on the same frame the run ends must not pay out.
                Log.Warn(Tag, $"GatekeeperKilled ignored in terminal phase {Sim.Phase}.");
                return;
            }
            Sim.AwardKill();
        }

        /// <summary>Command from PlayerHealth: the player is dead — end the run exactly like a
        /// hatch loss. Nothing is forfeited; the salvage is already banked. Only meaningful
        /// while Tethered.</summary>
        public void PlayerDied()
        {
            if (Sim == null)
            {
                // Guard trip: no sim means no lose screen — free the cursor so the player isn't
                // trapped in a dead body with a captured mouse.
                Log.Error(Tag, "PlayerDied with no Sim; freeing the cursor as a last resort.");
                EnsureCursorFree();
                return;
            }
            if (Sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"PlayerDied ignored in phase {Sim.Phase} (run already over).");
                return;
            }
            Sim.NotifyPlayerDown();
        }

        /// <summary>Command from HatchStation: damage the authoritative sim Hatch.
        /// Field-terminal Waves leave the Hatch invulnerable (M52).</summary>
        public void DamageHatch(float amount)
        {
            if (amount <= 0f || Sim == null || RunIsOver) return;
            if (Sim.Mode == WaveMode.Terminal) return;
            Sim.Ship.Hatch.Damage(amount);
        }

        /// <summary>Field terminal Interact: start the next Wave; door opens only when that
        /// Wave clears (<see cref="OnAssaultEnded"/>).</summary>
        public bool StartTerminalWave(BastionTerminal terminal)
        {
            if (terminal == null)
            {
                Log.Warn(Tag, "StartTerminalWave refused — no terminal");
                return false;
            }

            if (terminal.Kind != BastionTerminalKind.Field)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — kind=" + terminal.Kind);
                return false;
            }

            if (Sim == null || RunIsOver)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — run not live");
                return false;
            }

            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — not in Prep");
                return false;
            }

            if (Sim.AllWavesCleared || Sim.CurrentWaveIndex >= WaveDirector.MaxWaves)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — all waves cleared");
                return false;
            }

            BastionLayout layout = terminal.Layout;
            if (layout == null)
                layout = FindFirstObjectByType<BastionLayout>();
            if (layout != null && layout.IsTerminalDone(terminal.TerminalId))
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — already complete");
                return false;
            }

            if (!string.IsNullOrEmpty(_pendingTerminalId))
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — wave already pending "
                    + _pendingTerminalId);
                return false;
            }

            // Field Wave Spawns register only while active; enable before TriggerDraw.
            terminal.SetUnlockedSpawnsActive(true);

            _pendingTerminalId = terminal.TerminalId;
            Sim.TriggerDraw();
            Log.Event("Terminal", terminal.TerminalId + " started wave " + Sim.CurrentWaveIndex
                + " mode=terminal");
            return true;
        }

        /// <summary>Command from RepairInteractable: heal the hatch by up to
        /// <paramref name="requestedHp"/>, charging salvage continuously via
        /// <see cref="RunSim.RepairHatch"/>. Returns HP actually applied. Refuses when the
        /// repair rig is not equipped — Dev RestoreHatchAndPlayer bypasses this via
        /// <c>Subsystem.Repair</c> directly.</summary>
        public float RepairHatch(float requestedHp)
        {
            if (Sim == null || RunIsOver) return 0f;
            if (Meta == null || !Meta.HasRepairRigEquipped) return 0f;
            var hatch = Sim.Ship.Hatch;
            bool wasFull = hatch.Hp >= hatch.MaxHp - 0.01f;
            float applied = Sim.RepairHatch(requestedHp);
            if (applied > 0f && !wasFull && hatch.Hp >= hatch.MaxHp - 0.01f)
                Log.Event("Hatch", $"hatch fully repaired — {hatch.Hp:0}/{hatch.MaxHp:0}");
            return applied;
        }

        /// <summary>
        /// Push every shop value into the scene at run start: ARMORY weapons, FABRICATION
        /// repair rate (and the rigless state), and the CREW QUARTERS player attributes
        /// (Vitality → PlayerHealth max HP, Stamina → FirstPersonController sprint pool).
        /// Called once from Start.
        /// </summary>
        /// <remarks>
        /// Two tracks are deliberately NOT applied here:
        ///   * Hatch Armor flows through the RunSim constructor (it is sim state, not scene state).
        ///   * SCAVENGING is applied by RunSim.CurrentKillValue and NOWHERE ELSE. Do not add a
        ///     salvage-per-kill push here: the award path already reads
        ///     MetaUpgrades.SalvagePerKill(ScavengingLevel), and a second application would pay
        ///     the multiplier twice for every body.
        /// Luck is read per-shot by PlayerGun, so it needs no push either.
        /// Repair ALWAYS pushes both rate and equipped flag — leaving Configure uncalled would
        /// leave the serialized 8 HP/s standing on a rigless embark.
        /// </remarks>
        public void ApplyMetaEffects()
        {
            if (Meta == null) return;

            var local = PlayerRegistry.Local;
            var gun = local != null ? local.GetComponentInChildren<PlayerGun>() : null;
            if (gun != null)
            {
                // The rig is runtime-resolved, so the controller pushes itself alongside the
                // config, and adopts the component's per-frame advance (written order).
                gun.Bind(this);
                gun.SetHosted(true);
                _gun = gun;
                var ability = gun.GetComponent<PlayerAbility>();
                if (ability != null)
                {
                    ability.Bind(this);
                    ability.SetHosted(true);
                }
                _ability = ability;
                var placement = gun.GetComponent<DecoyPlacement>();
                if (placement == null && local != null)
                    placement = local.GetComponentInChildren<DecoyPlacement>();
                if (placement != null) placement.SetHosted(true);
                _decoyPlacement = placement;
                // Values are pushed at run start, never serialized onto a prefab (gotcha 1).
                gun.ClearLoadout();
                PushSlot(gun, Meta, WeaponSlot.Primary, Meta.EquippedPrimary);
                PushSlot(gun, Meta, WeaponSlot.Secondary, Meta.EquippedSecondary);
                // Start on Primary when carried; otherwise Secondary (sidearm-only runs).
                if (gun.SlotConfigured(WeaponSlot.Primary))
                    gun.SelectSlot(WeaponSlot.Primary);
                else if (gun.SlotConfigured(WeaponSlot.Secondary))
                    gun.SelectSlot(WeaponSlot.Secondary);
            }
            else
            {
                Log.Warn(Tag, "ApplyMetaEffects: no PlayerGun found under the local player.");
            }

            // VITALITY. Heals to full by design — see PlayerHealth.ConfigureMaxHp.
            if (local != null)
            {
                local.Bind(this);
                local.ConfigureMaxHp(MetaUpgrades.PlayerMaxHp(Meta.VitalityLevel));
            }
            else Log.Warn(Tag, "ApplyMetaEffects: no local PlayerHealth to size from Vitality.");

            // STAMINA — the sprint pool. Without this push the controller keeps its serialized
            // 100 and the whole Stamina track is invisible to the player.
            var fpc = local != null ? local.GetComponent<FirstPersonController>() : null;
            if (fpc != null)
            {
                fpc.ConfigureStamina(MetaUpgrades.StaminaMax(Meta.StaminaLevel));
                fpc.SetHosted(true);
                _fpc = fpc;
            }
            else Log.Warn(Tag, "ApplyMetaEffects: no FirstPersonController to size the stamina pool on.");

            var interactor = local != null ? local.GetComponentInChildren<Interactor>() : null;
            if (interactor == null && fpc != null) interactor = fpc.GetComponentInChildren<Interactor>();
            if (interactor != null) interactor.SetHosted(true);
            _interactor = interactor;

            // Repair: flat rate from the equipped rig, or explicitly zero when unequipped.
            var repair = _repair;
            if (repair != null)
            {
                bool equipped = Meta.HasRepairRigEquipped;
                float rate = equipped
                    ? EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond
                    : 0f;
                repair.Configure(rate, equipped);
            }
            else Log.Warn(Tag, "ApplyMetaEffects: no RepairInteractable in the scene.");

            // Drone bay: lock mid-Run equip, reset charges for this Run's equipped payload.
            if (Meta != null) Meta.PayloadLoadoutLocked = true;
            var bay = local != null ? local.GetComponentInChildren<DecoyBay>() : null;
            if (bay == null && gun != null) bay = gun.GetComponent<DecoyBay>();
            if (bay != null)
            {
                // Bind registers the bay with this controller and subscribes it to Sim events.
                bay.Bind(this);
                bay.ResetForRun(Meta != null ? Meta.EquippedDrone : PayloadId.None);
            }
            else
                Log.Warn(Tag, "ApplyMetaEffects: no DecoyBay on the player rig.");
        }

        static void PushSlot(PlayerGun gun, MetaState meta, WeaponSlot slot, WeaponId id)
        {
            if (id == WeaponId.None || meta == null) return;
            // Disabled catalog rows (e.g. VK-7) stay in saves but never reach the rig.
            if (!WeaponCatalog.IsAvailable(id)) return;
            gun.Configure(slot, WeaponCatalog.Def(id), meta.PointsOf(id), id);
        }

        /// <summary>Phase transitions themselves are NOT logged: the death / hatch-loss line that
        /// caused them is already in the transcript, and a "Phase -> Lost" line only restates it.
        /// This handler does cursor/lock work and, on either terminal phase, emits the
        /// end-of-run summary — RUN OVER for Lost, BASTION STRIPPED for Escaped.</summary>
        void OnPhaseChanged(RunPhase phase)
        {
            bool fighting = phase == RunPhase.Tethered;

            // Re-resolve every transition: a cached _fpc from a previous player rig is fake-null,
            // and a null _fpc used to mean the cursor was NEVER freed on the lose screen.
            if (_fpc == null)
            {
                var local = PlayerRegistry.Local;
                _fpc = local != null ? local.GetComponent<FirstPersonController>() : null;
            }
            if (_fpc != null)
            {
                _fpc.MovementLocked = !fighting;
                _fpc.SetCursorLock(fighting);
            }
            else if (!fighting)
            {
                // Guard trip: no controller to release the cursor for us — do it directly, or the
                // RETURN TO SHIP button can never be clicked.
                Log.Warn(Tag, "No FirstPersonController found; freeing the cursor directly.");
                EnsureCursorFree();
            }

            if (IsTerminal(phase))
            {
                _pendingTerminalId = null;
                EnterTerminalPhase(phase); // GameController.EndScreen.cs
            }
        }

        void OnWaveStarted(WaveSpec wave)
        {
            Log.Event("Run", $"wave {Sim.CurrentWaveIndex}/{WaveDirector.MaxWaves} — {wave.enemyCount} gatekeepers, " +
                $"siphon L{Sim.Siphon.Level} (each kill +{Sim.CurrentKillAward})");
        }

        void OnAssaultEnded()
        {
            Log.Event("Run", $"wave {Sim.CurrentWaveIndex}/{WaveDirector.MaxWaves} cleared — " +
                $"salvage {Sim.Salvage.RunSalvage:0}, " +
                $"hatch {Sim.Ship.Hatch.Hp:0}/{Sim.Ship.Hatch.MaxHp:0}");

            if (string.IsNullOrEmpty(_pendingTerminalId))
                return;

            string id = _pendingTerminalId;
            _pendingTerminalId = null;

            BastionLayout layout = FindFirstObjectByType<BastionLayout>();
            if (layout == null)
            {
                Log.Warn(Tag, "AssaultEnded: pending terminal " + id + " but no BastionLayout");
                return;
            }

            if (!layout.TryCompleteTerminal(id, out string refuseReason))
            {
                Log.Event("Terminal", id + " complete refused — " + refuseReason);
                return;
            }

            Log.Event("Terminal", id + " complete — wave cleared");
        }

        void OnApplicationQuit()
        {
            GameSession.Save();
        }

        /// <summary>Pause QUIT TO MENU confirm: persist then load MainMenu.
        /// WP-2 owns the Options/Settings row authoring; this is the save-then-load call site.</summary>
        public void QuitToMenu()
        {
            if (_leavingScene) return;
            _leavingScene = true;
            Log.Event("Ship", $"quit to menu — {(Meta != null ? Meta.BankedSalvage : 0)} banked");
            GameSession.Save();
            if (SceneFlow.LoadMainMenu()) return;
            _leavingScene = false;
            Log.Error(Tag, "QUIT TO MENU failed to load MainMenu; staying in Bastion.");
        }

        void OnEnable()
        {
            Subscribe(); // idempotent: covers a disable/enable cycle without double-firing
        }

        void OnDisable()
        {
            // Unsubscribe here too: OnDestroy alone leaves the sim holding this handler if the
            // component is merely disabled during scene teardown.
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>Idempotent subscribe (-= before += so re-entry can never double-fire).</summary>
        void Subscribe()
        {
            if (Sim == null) return;
            Unsubscribe();
            Sim.PhaseChanged += OnPhaseChanged;
            Sim.WaveStarted += OnWaveStarted;
            Sim.AssaultEnded += OnAssaultEnded;
        }

        void Unsubscribe()
        {
            if (Sim == null) return;
            Sim.PhaseChanged -= OnPhaseChanged;
            Sim.WaveStarted -= OnWaveStarted;
            Sim.AssaultEnded -= OnAssaultEnded;
        }
    }
}
