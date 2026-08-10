using System;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: Bastion-scene bootstrap that owns the RunSim, ticks it every frame,
    /// and exposes the command methods all sim mutation flows through (grandfathered singleton).</summary>
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

        /// <summary>Scene-wide access point. Set in Awake, cleared in OnDestroy.</summary>
        public static GameController Instance { get; private set; }

        /// <summary>The authoritative game simulation. MonoBehaviours are thin views over this.</summary>
        public RunSim Sim { get; private set; }


        /// <summary>Persistent cross-run progression state, owned by GameSession.</summary>
        public MetaState Meta => GameSession.Meta;

        /// <summary>Gatekeepers spawned and still alive right now (spawner readout). Used for the
        /// death/hatch-loss context lines; 0 when no spawner is present.</summary>
        public int EnemiesAlive
        {
            get
            {
                if (_spawner == null) _spawner = FindFirstObjectByType<WaveSpawner>();
                return _spawner != null ? _spawner.AliveCount : 0;
            }
        }

        /// <summary>True once the run has ended (terminal phase) — enemy AI and the spawner
        /// use this as their single "stop everything" test.</summary>
        public bool RunIsOver => Sim == null || IsTerminal(Sim.Phase);

        FirstPersonController _fpc;
        WaveSpawner _spawner;
        bool _discarded;      // this instance lost the duplicate race and is being destroyed

        /// <summary>Clears the static handle at play-mode start (domain-reload-disabled safety).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Guard: Destroy is deferred to end of frame, so Start/Update still run on this
                // instance — _discarded makes them no-ops instead of NRE-ing on a null Sim.
                _discarded = true;
                Log.Warn(Tag, "Duplicate GameController destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SceneFlow.EnsureEventSystem();

            // Direct-play/test fallback: entering the Bastion without passing menu/ship.
            GameSession.EnsureLoaded();
            if (Meta == null)
            {
                // Guard trip: RunSim's ctor throws on a null MetaState — fail loudly, not with a stack trace.
                Log.Error(Tag, "No MetaState after GameSession.EnsureLoaded(); the run cannot start.");
                _discarded = true;
                return;
            }

            int runSeed = seed != 0 ? seed : Environment.TickCount;
            Sim = new RunSim(Meta, runSeed);
            Subscribe();
        }

        void Start()
        {
            if (_discarded || Sim == null) return; // guard: duplicate/failed boot must not touch a null Sim
            // The ship-menu phase is a real scene now: boarding is immediate, Docked transient.
            // PendingStartWave is set by MapStation; consumed once so direct-play falls back to 1.
            int startWave = GameSession.PendingStartWave;
            GameSession.PendingStartWave = 1;
            Sim.StartRun(startWave);
            Sim.EngageTether(); // → Tethered (Prep)
            ApplyMetaEffects();

            // Stable fields after the prose — M29 criterion 14. The sentence stays readable;
            // wave-report reads startWave=/primary=/… without re-parsing the prose.
            Log.Event("Run", $"run begins at {SceneFlow.BastionName} starting at wave {Sim.StartWave} — " +
                $"{LoadoutSummary(Meta)}, " +
                $"{Meta.BankedSalvage} banked, hatch {Sim.Ship.Hatch.MaxHp:0}hp " +
                $"{RunBeginsFields(Meta, Sim.StartWave)}");
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

        /// <summary>Machine-readable tail for the run-begins line (M29 criterion 14).</summary>
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

        void Update()
        {
            if (_discarded || Sim == null) return; // guard: NRE at Sim.Tick on the duplicate-controller path

            Sim.Tick(Time.deltaTime);

            if (Sim.Phase == RunPhase.Tethered)
            {
                TickFightInput();
                return;
            }

            // BOTH terminal phases run the end-screen tick: Escaped is a win, but it is exactly
            // as much of a dead end as Lost — same cursor, same button, same soft-lock risk.
            if (IsTerminal(Sim.Phase))
                TickLoseScreen();
        }

        /// <summary>Prep-only inputs (siphon draw + dial stepper). Dial is [ ] — the number
        /// row belongs to weapon slots (see Controls), so Prep no longer double-fires on 1/2/3.</summary>
        void TickFightInput()
        {
            if (OptionsScreen.IsOpen) return;
            if (Sim.Assault != AssaultState.Prep) return;

            if (Controls.Down(ControlId.PullDraw))
            {
                // Both halves matter: AllWavesCleared is the sim's own "run completed" flag,
                // but it only flips as the run goes Escaped, so the draw-budget test is what
                // actually catches a refused T while still Tethered.
                if (Sim.AllWavesCleared || Sim.CurrentWaveIndex >= WaveDirector.MaxWaves)
                {
                    Log.Event("Run", $"draw refused — the bastion is stripped, " +
                                     $"all {WaveDirector.MaxWaves} waves cleared");
                }
                else
                {
                    Log.Event("Run", $"pulled the siphon draw at L{Sim.Siphon.Level} — " +
                                     $"wave {Sim.CurrentWaveIndex + 1}/{WaveDirector.MaxWaves} incoming");
                    Sim.TriggerDraw();
                }
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
                $"(each kill +{Sim.CurrentKillValue:0} salvage)");
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
                // Values are pushed at run start, never serialized onto a prefab (gotcha 1).
                gun.ClearLoadout();
                PushSlot(gun, Meta, WeaponSlot.Primary, Meta.EquippedPrimary);
                PushSlot(gun, Meta, WeaponSlot.Secondary, Meta.EquippedSecondary);
                // Start on Primary when carried; otherwise Secondary (sidearm-only runs).
                if (gun.SlotConfigured(WeaponSlot.Primary))
                    gun.TrySelectSlotPublic(WeaponSlot.Primary);
                else if (gun.SlotConfigured(WeaponSlot.Secondary))
                    gun.TrySelectSlotPublic(WeaponSlot.Secondary);
            }
            else
            {
                Log.Warn(Tag, "ApplyMetaEffects: no PlayerGun found under the local player.");
            }

            // VITALITY. Heals to full by design — see PlayerHealth.ConfigureMaxHp.
            if (local != null) local.ConfigureMaxHp(MetaUpgrades.PlayerMaxHp(Meta.VitalityLevel));
            else Log.Warn(Tag, "ApplyMetaEffects: no local PlayerHealth to size from Vitality.");

            // STAMINA — the sprint pool. Without this push the controller keeps its serialized
            // 100 and the whole Stamina track is invisible to the player.
            var fpc = local != null ? local.GetComponent<FirstPersonController>() : null;
            if (fpc != null) fpc.ConfigureStamina(MetaUpgrades.StaminaMax(Meta.StaminaLevel));
            else Log.Warn(Tag, "ApplyMetaEffects: no FirstPersonController to size the stamina pool on.");

            // Repair: flat rate from the equipped rig, or explicitly zero when unequipped.
            var repair = FindFirstObjectByType<RepairInteractable>();
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
                bay.ResetForRun(Meta != null ? Meta.EquippedDrone : PayloadId.None);
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

            if (IsTerminal(phase)) EnterTerminalPhase(phase); // GameController.EndScreen.cs
        }

        void OnWaveStarted(WaveSpec wave)
        {
            Log.Event("Run", $"wave {Sim.CurrentWaveIndex}/{WaveDirector.MaxWaves} — {wave.enemyCount} gatekeepers, " +
                $"siphon L{Sim.Siphon.Level} (each kill +{Sim.CurrentKillValue:0})");
        }

        void OnAssaultEnded()
        {
            Log.Event("Run", $"wave {Sim.CurrentWaveIndex}/{WaveDirector.MaxWaves} cleared — " +
                $"salvage {Sim.Salvage.RunSalvage:0}, " +
                $"hatch {Sim.Ship.Hatch.Hp:0}/{Sim.Ship.Hatch.MaxHp:0}");
        }

        void OnApplicationQuit()
        {
            GameSession.Save();
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
            if (Instance == this) Instance = null;
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
