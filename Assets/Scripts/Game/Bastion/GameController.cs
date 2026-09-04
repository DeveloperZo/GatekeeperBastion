using System;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST + CLIENT presentation: Bastion-scene bootstrap that owns the RunContext,
    /// ticks its RunSim every frame on host, and exposes the command methods all sim mutation
    /// flows through. Consumers hold it by serialized scene reference or receive it via Bind —
    /// never through a static. Client StartRun+EngageTether for local phase/UI; Update skips
    /// Sim.Tick so the host remains the only live sim.</summary>
    /// <remarks>
    /// Scene flow: the ship hub is now a real scene (SceneFlow.ShipCoop), so entering the Bastion
    /// starts fighting immediately — Start() calls StartRun + EngageTether and Docked is a
    /// transient boot phase. A run now ends TWO ways: death (player death via PlayerDied →
    /// Sim.NotifyPlayerDown, or hatch destruction) lands in Lost, and Master Control's
    /// Hatch-bank half survived lands in Escaped. Hatch-bank after the field is stripped
    /// stays Tethered. Wave 5 terminal does not Escaped. Both terminal phases leave by exactly
    /// one door: ReturnToShip (save + load the Ship scene) — the old in-place ResetRun/Respawn
    /// path is gone. MetaState comes from GameSession (with a direct-play fallback); no
    /// Time.timeScale pausing — the sim ignores Tick outside Tethered.
    /// Money: GatekeeperKilled → Sim.AwardKill() credits Unbanked. Hatch-bank drip/dump
    /// converts into MetaState.BankedSalvage. RepairHatch spends from that bank.
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

        [SerializeField, Tooltip("Bastion layout on Environment. Authored in the scene.")]
        BastionLayout _layout;

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

        /// <summary>The hosted weapon ability, resolved from the rig at run start. Exposed the
        /// same way <see cref="Spawner"/> is so the practice range can reach it without a scene
        /// lookup; null before ApplyMetaEffects has run.</summary>
        public PlayerAbility Ability => _ability;

        /// <summary>The hosted protocol abilities, crew and personal.</summary>
        public PlayerProtocolAbilities Protocol => _protocol;

        /// <summary>The hosted weapon. Exposed for the practice range's readout, which counts
        /// shots off its Fired event and has to re-resolve when the rig is rebuilt.</summary>
        public PlayerGun Gun => _gun;

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
        PlayerProtocolAbilities _protocol;
        Interactor _interactor;
        DecoyPlacement _decoyPlacement;

        // Party Bastion load can spawn the local avatar after Start — retry bind like ShipController.
        const float MetaBindWindowSeconds = 5f;
        float _metaBindWindow;

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

            RebindNetSession();

            int runSeed = seed != 0 ? seed : Environment.TickCount;
            Run = new RunContext(GameSession.Meta, runSeed);
            Subscribe();
        }

        /// <summary>Adopt the DDOL friends session when Bastion loads via NetworkSceneManager.
        /// Scene-authored _net is disabled on Bastion — prefer a live listening session only.</summary>
        void RebindNetSession()
        {
            var live = NetSession.FindLive();
            if (live != null && live.Active)
            {
                _net = live;
                live.BindGame(this);
                return;
            }

            // CLI / Splash (-gkb-host/-gkb-join): intent set, Bastion scene Net disabled.
            if (NetJoinIntent.HasPending)
            {
                var created = NetSession.CreateBootstrap();
                if (created != null)
                {
                    _net = created;
                    created.BindGame(this);
                    created.ApplyPendingIntent();
                    return;
                }
            }

            // Bastion scene Net is disabled; friends sessions use DDOL Resources/Net/NetSession.
            // Never bind a disabled / non-listening scene Net.
            if (_net != null && _net.isActiveAndEnabled && _net.Active)
            {
                _net.BindGame(this);
                return;
            }
            _net = null;
        }

        void Start()
        {
            if (Sim == null) return; // guard: failed boot must not touch a null Sim

            LevelMusic.CaptureOrJoin(null);

            // PendingStartWave is set by MapStation / party lobby; consumed once so direct-play falls back to 1.
            int startWave = GameSession.PendingStartWave;
            if (_net != null && _net.Lobby != null && _net.Lobby.PendingStartWave.Value > 0)
                startWave = _net.Lobby.PendingStartWave.Value;
            GameSession.PendingStartWave = 1;

            // The demo's rule: the ship terminal's start grants the MK-1 before any embark
            // (MapStation refuses an unarmed EMBARK). A run that still begins unarmed with
            // the demo active — a direct scene play, a dev jump — honours the same grant
            // here: the improvised cutter must never appear inside the demo.
            if (Meta != null && Meta.DemoActive && !Meta.OwnsAnyWeapon)
            {
                Meta.SetOwned(WeaponId.Mk1Sidearm, true);
                Meta.SetEquipped(WeaponId.Mk1Sidearm);
                Meta.DemoWeaponGranted = true;
                Log.Event("Demo", "unarmed demo run — MK-1 granted (the ship terminal's grant, applied late)");
            }

            // The in-Bastion teaching lines (beats 1/2/5) ride their own poller — authored
            // long ago, wired 2026-08-25 (Docs/demo-readiness.md E1). Demo runs only.
            if (Meta != null && Meta.DemoActive)
                gameObject.AddComponent<DemoTeach>().Bind(this);

            // Bind FPC/gun before Docked so OnPhaseChanged has a controller. Client still
            // StartRun+EngageTether for local phase/UI; Update skips Sim.Tick (no second live sim).
            ApplyMetaEffects();
            if (_fpc == null)
                _metaBindWindow = MetaBindWindowSeconds;

            // A guest's sim is a local shadow of the host's run: it MUST still StartRun +
            // EngageTether, because Tethered/Prep is what its own phase, HUD and Prep RPC path
            // key on (GameController.Client.cs, TickClientPrepCommands). What it must not do is
            // tick TotalRuns — that counter belongs to the realm being played, which is the
            // host's, not the one in this machine's save.
            bool clientOnly = _net != null && _net.IsClientOnly;
            if (clientOnly)
                Log.Event("Run", "guest run — this machine's realm run counter left alone");
            Sim.StartRun(startWave, !clientOnly);
            Sim.EngageTether(); // → Tethered (Prep)
            if (Sim.StartWave > 1 && _layout != null && Meta != null)
                _layout.RestoreCompletedTerminals(Meta.CompletedTerminalIds);
            // Door state restores on EVERY embark, not only a deep one: a Docked return
            // re-embarks at Wave 1 and must still find the door it paid for standing open.
            if (_layout != null && Meta != null)
            {
                string[] doors = Meta.OpenDoorIds;
                _layout.RestoreOpenDoors(doors);
                if (doors.Length > 0)
                    Log.Event("Demo", "snapshot restored doors=" + string.Join(",", doors));
            }
            // The spawner's per-frame delivery advance belongs to this controller's
            // written order from here on; its own Update yields.
            if (_spawner != null) _spawner.SetHosted(true);

            if (clientOnly)
                return;

            RefreshPlayerCount();

            // Stable fields after the prose. The sentence stays readable;
            // wave-report reads startWave=/primary=/… without re-parsing the prose.
            Log.Event("Run", $"run begins at {SceneFlow.CombatDisplayName} starting at wave {Sim.StartWave} — " +
                $"{LoadoutSummary(Meta)}, " +
                $"{Meta.BankedSalvage} banked, hatch {Sim.Ship.Hatch.MaxHp:0}hp " +
                $"{RunBeginsFields(Meta, Sim.StartWave)}; seed {Run.Sim.Seed}");
            // NAV offered no wave choice: refight-avoidance is the terminal's retrigger.
            if (Meta.DemoActive)
                Log.Event("Demo", "embark plain");

            ApplyPendingDemoFight();
            HatchRecallPad.BindAll();
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

            if (_fpc == null && _metaBindWindow > 0f)
            {
                _metaBindWindow -= dt;
                if (PlayerRegistry.Local != null)
                {
                    ApplyMetaEffects();
                    if (_fpc != null) _metaBindWindow = 0f;
                }
            }

            // Client-only: the host owns the run. Local move/fire/verbs so RPCs can fire;
            // do not tick the local sim or the spawner (that would fork a second live sim).
            if (_net != null && _net.IsClientOnly)
            {
                TickClientLocal(dt);
                return;
            }

            TickSeatRoster();
            RefreshPlayerCount();

            Sim.Tick(dt);                                             // 2

            if (Sim.Phase == RunPhase.Tethered)
            {
                TickFightInput();                                     // 3
                TickLocalDown();
            }

            if (_fpc != null) _fpc.Tick(dt);                          // 4
            if (_gun != null) _gun.Tick(dt);                          // 5
            if (_ability != null) _ability.Tick(dt);
            if (_protocol != null) _protocol.Tick(dt);
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

        /// <summary>Prep-only inputs (start T and juice +/-). Dial is +/- — the number
        /// row belongs to weapon slots (see Controls), so Prep no longer double-fires on 1/2/3.</summary>
        void TickFightInput()
        {
            // The Bastion tick samples the input frame first each frame; every other consumer's
            // defensive Sample() call is then a free frame-guarded read.
            Controls.Sample();
            if (OptionsScreen.IsOpen || HudController.IsPauseOpen) return;
            // The range's verbs live on its own interactables; the Bastion's would only
            // refuse them here and say so in the log.
            if (Sim.PracticeMode) return;
            TickFaucetHousekeeping();
            // WHICH VERBS ARE LIVE IS SIM'S CALL (BastionVerbs.LiveIn), not this method's.
            // It used to be the shape of these branches, and that is how the terminal shutoff
            // came to be unreachable during a live wave: the Prep gate below sat above the
            // handler that carries it, and for a dripping zone Assault is the only state that
            // handler is ever reached in. Asserting the dispatch table is now one test.
            if (BastionVerbs.IsLive(BastionVerb.Shutoff, Sim.Assault)
                && Sim.Assault == AssaultState.Assault)
            {
                TickFaucetAssaultCommands();
                // AND THE ZONE SHUTOFF. TickPrepCommands carries the T-at-a-terminal branch and
                // its own comment says it works "in Assault OR in Prep" — but this method
                // returned before ever calling it, so during a live wave T at the terminal did
                // NOTHING, and did it silently. Zone 1 hid the bug: killing the field empty
                // drops the sim to Prep, so the player always pressed T from Prep there. Zone 2
                // keeps dripping by design, never leaves Assault, and the 07:17 run had no way
                // to end wave 5 at all — the only exit was abandoning the run.
                TickAssaultShutoffCommand();
                return;
            }
            if (Sim.Assault != AssaultState.Prep) return;
            TickPrepCommands();
        }

        /// <summary>Command from WaveSpawner: one gatekeeper is down. Credits Unbanked; the
        /// ship bank does not move until a Hatch-bank converts. Also speeds the local player's
        /// passive health regen — see <see cref="Sim.PlayerRegen"/> for why kills drive it.</summary>
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

            // Regen boost rides the same seam as the payout so "a body died" has exactly one
            // place in the Game layer. Not per-frame, so the registry read is free.
            //
            // A TRICKLE BODY HEALS NOTHING (2026-08-26). FieldTrickle's whole rule is that the
            // drip is "a clock, not an income", and CurrentKillAward already returns 0 while it
            // runs — but zeroing the PAYOUT was only half of it. This call was unconditional,
            // so the drip fed the regen boost and the walk up the lane became a healing
            // station: modelled at +21 HP net across zone 1's 66 m, i.e. the player arrived at
            // the terminal healthier than they left the Hatch. A body that pays nothing must
            // also mend nothing, or "free kills" is just a slower way of saying "free health".
            if (Sim.FieldTrickleLive) return;
            var local = PlayerRegistry.Local;
            if (local != null) local.NotifyKill();
            // VEIL's re-entry rides the same seam for the same reason (2026-09-04): a trickle
            // body buys no stealth, exactly as it buys no Salvage and no health.
            if (Protocol != null) Protocol.NotifyKill();
        }

        /// <summary>Command from HatchStation: damage the authoritative sim Hatch.
        /// Field-terminal Waves leave the Hatch invulnerable.</summary>
        public void DamageHatch(float amount)
        {
            if (amount <= 0f || Sim == null || RunIsOver) return;
            Sim.DamageHatch(amount);
        }

        /// <summary>Command from RepairInteractable: heal the hatch by up to
        /// <paramref name="requestedHp"/>, charging salvage continuously via
        /// <see cref="RunSim.RepairHatch"/>. A full Hatch in Prep withdraws instead
        /// (<see cref="RunSim.Withdraw"/>). Repair refuses when the
        /// repair rig is not equipped — Dev RestoreHatchAndPlayer bypasses this via
        /// <c>Subsystem.Repair</c> directly.</summary>
        public float RepairHatch(float requestedHp)
        {
            if (Sim == null || RunIsOver) return 0f;
            var hatch = Sim.Ship.Hatch;
            if (hatch.Hp >= hatch.MaxHp - 0.01f)
            {
                TryWithdrawFromHpRequest(requestedHp);
                return 0f;
            }
            if (Meta == null || !Meta.HasRepairRigEquipped) return 0f;
            float applied = Sim.RepairHatch(requestedHp);
            if (applied > 0f && hatch.Hp >= hatch.MaxHp - 0.01f)
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
                BindProtocol(local);
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
                local.ConfigureMaxHp(MetaUpgrades.PlayerMaxHp());
            }
            else Log.Warn(Tag, "ApplyMetaEffects: no local PlayerHealth to size from Vitality.");

            // STAMINA — the sprint pool. Without this push the controller keeps its serialized
            // 100 and the whole Stamina track is invisible to the player.
            var fpc = local != null ? local.GetComponent<FirstPersonController>() : null;
            if (fpc != null)
            {
                fpc.ConfigureStamina(MetaUpgrades.StaminaMax());
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
            if (bay == null && gun != null)
                bay = gun.gameObject.AddComponent<DecoyBay>();
            if (bay != null)
            {
                // Bind registers the bay with this controller and subscribes it to Sim events.
                bay.Bind(this);
                bay.ResetForRun(Meta != null ? Meta.EquippedDrone : PayloadId.None);
                if (gun != null) gun.ConfigureDrone(bay.HasPayload);
                var placement = bay.Placement;
                if (placement != null)
                {
                    placement.SetHosted(true);
                    _decoyPlacement = placement;
                }
            }
            else
                Log.Warn(Tag, "ApplyMetaEffects: no DecoyBay on the player rig.");
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
                ClearPendingTerminal();
                ClearPendingRetrigger();
                EnterTerminalPhase(phase); // GameController.EndScreen.cs
            }
        }

        void OnWaveStarted(WaveSpec wave)
        {
            Log.Event("Run", $"wave {Sim.CurrentWaveIndex}/{WaveDirector.MaxWaves} — {wave.enemyCount} gatekeepers, " +
                $"intensity={Sim.Siphon.Level} juiced={Sim.JuiceSpentThisPull} (each kill +{Sim.CurrentKillAward})");
            if (_protocol != null) _protocol.BeginWaveMachines();
            if (!string.IsNullOrEmpty(_pendingTerminalId))
                Log.Event("Terminal", _pendingTerminalId + " started wave " + Sim.CurrentWaveIndex
                    + " mode=terminal");
        }

        void OnAssaultEnded()
        {
            Log.Event("Run", $"wave {Sim.CurrentWaveIndex}/{WaveDirector.MaxWaves} cleared — " +
                $"salvage {Sim.Salvage.RunSalvage:0}, " +
                $"hatch {Sim.Ship.Hatch.Hp:0}/{Sim.Ship.Hatch.MaxHp:0}");
            if (_protocol != null)
            {
                string machines = _protocol.WaveMachineLine();
                if (machines != null)
                    Log.Event("Run", "wave " + Sim.CurrentWaveIndex + "/" + WaveDirector.MaxWaves
                        + " " + machines);
            }
            if (Sim.Mode == WaveMode.HatchBank && _spawner != null)
                _spawner.DespawnAllLive();
            RecordPendingTerminalClear();
            RecordCompositeClear();
            if (Sim.MasterPhase == MasterControlPhase.Elite && Sim.Phase == RunPhase.Tethered)
            {
                if (_hatchBankLayout != null)
                    _hatchBankLayout.ActivateHatchBankSpawns();
            }
            else
                RestoreHatchBankSpawns();
            if (Sim.Mode == WaveMode.HatchBank)
                SnapshotHatchBankGraph();
            PushGuestProgress();
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
            if (_net != null && _net.Active)
                _net.ShutdownSession();
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
            // A teardown while the run is still Tethered is an abandonment — quit to menu,
            // a mid-run checkpoint reload, or the player closing. Terminal phases already
            // wrote their summary; this writes the one no terminal phase ever will.
            LogAbandonMetricIfLiveRun();
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
            Sim.MasterHatchHalfStarted += OnMasterHatchHalfStarted;
            Sim.BankWindowSurvived += OnBankWindowSurvived;
            Sim.FieldWaveCleared += OnFieldWaveCleared;
        }

        void Unsubscribe()
        {
            if (Sim == null) return;
            Sim.PhaseChanged -= OnPhaseChanged;
            Sim.WaveStarted -= OnWaveStarted;
            Sim.AssaultEnded -= OnAssaultEnded;
            Sim.MasterHatchHalfStarted -= OnMasterHatchHalfStarted;
            Sim.BankWindowSurvived -= OnBankWindowSurvived;
            Sim.FieldWaveCleared -= OnFieldWaveCleared;
        }
    }
}
