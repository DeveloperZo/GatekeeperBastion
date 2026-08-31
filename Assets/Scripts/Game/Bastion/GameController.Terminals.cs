using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): field-terminal Wave start and clear.
    /// Split when the command surface landed and GameController.cs passed the 400-line cap —
    /// same class, same state, one concern per file (EndScreen, DevCommands, Retrigger).</summary>
    public sealed partial class GameController
    {
        /// <summary>Field terminal id waiting on the live Wave clear. Null when none pending.</summary>
        string _pendingTerminalId;
        int _pendingWaveIndexAtPull;
        BastionLayout _pendingLayout;
        BastionLayout _hatchBankLayout;
        BastionTerminal _masterTerminal;
        bool _masterLookPlayed;
        string _armedTerminalId;
        bool _armedHatch;

        /// <summary>True when this trigger has been E-selected this Prep (juice locked; T starts).</summary>
        public bool IsWaveStartArmed(string terminalId) =>
            !string.IsNullOrEmpty(terminalId) && terminalId == _armedTerminalId;

        /// <summary>True when Hatch-bank has been E-selected this Prep (juice locked; T starts).</summary>
        public bool IsHatchBankArmed => _armedHatch;

        static readonly WaveDirector s_preview = new WaveDirector();

        int PreviewBodies()
        {
            var sim = Sim;
            int carry = sim.Mode == WaveMode.HatchBank && sim.Assault == AssaultState.Assault
                ? sim.HatchCarry
                : sim.WavesSinceBank;
            WaveSpec raw = s_preview.NextWave(HatchBank.CarryWaveIndex(carry), sim.Siphon.Level, sim.PlayerCount);
            WaveSpec spec = HatchBank.ApplyHatchComposition(raw, carry);
            if (sim.MasterPhase == MasterControlPhase.Elite)
                spec = MasterControl.ApplyFinaleElite(spec);
            return spec.enemyCount;
        }

        int FieldPreviewBodies()
        {
            int idx = Sim.CurrentWaveIndex;
            if (idx >= WaveDirector.MaxWaves) idx = WaveDirector.MaxWaves - 1;
            if (idx < 0) idx = 0;
            return s_preview.NextWave(idx, Sim.Siphon.Level, Sim.PlayerCount).enemyCount;
        }

        /// <summary>T at a zone terminal while the wave is LIVE — the off switch. Prep has the
        /// same verb inside <see cref="TickPrepCommands"/>; this is the Assault half, which was
        /// unreachable until 2026-08-28 because TickFightInput returned on Assault before ever
        /// calling it.
        /// <para>Deliberately narrow: it is the shutoff and nothing else. Every other Prep verb
        /// — wave start, Convert, juice, retrigger — stays Prep-only, so widening the Assault
        /// path cannot smuggle them in. The conditions are the same four ShutOffTerminal
        /// re-checks, so the two cannot disagree about when the verb is offered, and
        /// ShutOffTerminal still owns every refusal line.</para></summary>
        void TickAssaultShutoffCommand()
        {
            if (Sim == null || _layout == null || RunIsOver) return;
            if (!Controls.Down(ControlId.PullDraw)) return;
            if (Sim.Mode == WaveMode.HatchBank) return;   // a Convert window has no off switch

            var lookingTerminal = (_interactor != null ? _interactor.Current : null)
                                  as BastionTerminal;
            if (lookingTerminal == null) return;
            string id = lookingTerminal.TerminalId;
            if (string.IsNullOrEmpty(id)) return;
            if (lookingTerminal.Kind == BastionTerminalKind.Master)
            {
                if (Sim.MasterAwaitsShutoff) ShutOffTerminal(lookingTerminal);
                return;
            }
            if (_layout.IsTerminalDone(id)) return;
            if (DemoZones.ZoneOf(id) <= 0) return;
            if (!ZoneWavesHaveRun(id)) return;

            ShutOffTerminal(lookingTerminal);
        }

        void TickPrepCommands()
        {
            // The open faucet draft owns E and T until it is confirmed (faucet graybox).
            if (Sim.FaucetAwaitingDraft)
            {
                TickFaucetDraft();
                return;
            }
            var looking = _interactor != null ? _interactor.Current : null;
            var lookingTerminal = looking as BastionTerminal;
            var lookingHatch = looking as RepairInteractable;
            if (lookingTerminal == null || lookingTerminal.TerminalId != _armedTerminalId)
            {
                _armedTerminalId = null;
                _armedRetrigger = false;
                _retriggerPartial = false;
            }
            if (lookingHatch == null)
                _armedHatch = false;
            if (lookingTerminal != null && lookingTerminal.IsMaster)
                NoticeMasterLook();

            bool hatchStartable = lookingHatch != null && lookingHatch.OffersHatchBank;
            var retriggerOffer = RetriggerOfferFor(lookingTerminal);
            bool terminalStartable = !retriggerOffer.Available
                && lookingTerminal != null && CanSelectTerminal(lookingTerminal);

            if (Controls.Down(ControlId.PullDraw))
            {
                // Prep-gap faucet shutoff: T at the origin between chained Waves.
                if (TryFaucetPrepShutoff(lookingTerminal))
                    return;

                // THE LOOP (Zo, 2026-08-25). A wave BEGINS at the Hatch and ENDS at a
                // terminal, so T means opposite things at the two ends of the lane:
                //   * at a terminal while the wave runs — shut it off, the zone is done;
                //   * at the Hatch while the zone is unfinished — start that zone's wave.
                // Both are checked before the older Convert/retrigger branches, which own T
                // once the zone IS finished.
                // T at a zone terminal is the shutoff, in Assault OR in Prep: the field may
                // already be empty when the player gets here, and the walk is still the
                // objective (Zo, 2026-08-26). ShutOffTerminal owns the refusals.
                // THE MASTER SWITCH IS THROWN IN PREP, and this branch could not reach it.
                // Killing the elite clears the assault, so the player is in PREP when they walk
                // up — but the zone test below (`ZoneOf > 0`) is false for the Master terminal,
                // which the zone table does not author, so T there did nothing. The Assault-side
                // handler covers the other state; without this the finale was armed and
                // unthrowable, which is the same "verb exists, cannot be reached" shape as the
                // zone shutoff before it.
                if (lookingTerminal != null && lookingTerminal.Kind == BastionTerminalKind.Master
                    && Sim != null && Sim.MasterAwaitsShutoff)
                {
                    ShutOffTerminal(lookingTerminal);
                    return;
                }
                if (lookingTerminal != null && Sim != null
                    && Sim.Mode != WaveMode.HatchBank
                    && !_layout.IsTerminalDone(lookingTerminal.TerminalId)
                    && DemoZones.ZoneOf(lookingTerminal.TerminalId) > 0
                    && ZoneWavesHaveRun(lookingTerminal.TerminalId))
                {
                    ShutOffTerminal(lookingTerminal);
                    return;
                }
                if (lookingHatch != null && Sim != null
                    && Sim.Assault == AssaultState.Prep && HatchOffersWaveStart)
                {
                    StartZoneWaveFromHatch();
                    return;
                }
                if (hatchStartable)
                {
                    if (Ante.Enabled && !_armedHatch)
                    {
                        Log.Event("Hatch", "hatch-bank refused — select intensity first");
                        return;
                    }
                    StartHatchBankWave();
                    return;
                }
                if (retriggerOffer.Available)
                {
                    if (HatchOffersWaveStart)
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — the wave starts at the Hatch");
                        return;
                    }
                    if (Ante.Enabled
                        && (!_armedRetrigger || _armedTerminalId != lookingTerminal.TerminalId))
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — select the retrigger first");
                        return;
                    }
                    if (!Ante.Enabled && !_armedRetrigger)
                    {
                        _armedTerminalId = lookingTerminal.TerminalId;
                        _armedRetrigger = true;
                        _retriggerPartial = false;
                    }
                    StartZoneRetrigger(lookingTerminal);
                    return;
                }
                if (terminalStartable)
                {
                    if (Ante.Enabled && _armedTerminalId != lookingTerminal.TerminalId)
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — select the Ante first");
                        return;
                    }
                    // THE MASTER TERMINAL IS NOT A START LINE. It was, and that made two of
                    // them — the shape 0-Gameplay.md forbids. The Hatch starts the finale now
                    // (StartZoneWaveFromHatch) and this terminal is where it ENDS.
                    if (lookingTerminal.Kind == BastionTerminalKind.Master)
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — Master Control starts at the Hatch");
                        return;
                    }
                    if (!Ante.Enabled)
                        _armedTerminalId = lookingTerminal.TerminalId;
                    StartTerminalWave(lookingTerminal);
                    return;
                }
                if (Sim.TotalUnbanked > 0L)
                    Log.Event("Hatch", "hatch-bank refused — look at the Hatch");
                else
                    Log.Event("Run", "draw refused — Waves start at the Hatch");
                return;
            }

            if (Controls.Down(ControlId.Interact))
            {
                // Ante select is parked with the dial — E at a trigger does not arm.
                if (!Ante.Enabled)
                {
                    // Retrigger still uses E to open the composite/partial election.
                    if (retriggerOffer.Available)
                    {
                        ToggleRetriggerSelect(lookingTerminal, retriggerOffer);
                        return;
                    }
                }
                else
                {
                    if (retriggerOffer.Available)
                    {
                        ToggleRetriggerSelect(lookingTerminal, retriggerOffer);
                        return;
                    }
                    if (terminalStartable)
                    {
                        ToggleTerminalSelect(lookingTerminal);
                        return;
                    }
                    if (hatchStartable)
                    {
                        ToggleHatchSelect();
                        return;
                    }
                }
            }

            int delta = 0;
            if (Controls.Down(ControlId.DialUp)) delta = 1;
            else if (Controls.Down(ControlId.DialDown)) delta = -1;
            if (delta == 0) return;

            if (_armedRetrigger && retriggerOffer.Available
                && lookingTerminal != null && lookingTerminal.TerminalId == _armedTerminalId)
            {
                // While the retrigger is armed, +/- elects the start — juice has no seat
                // in this Prep (intensity is selected with the arm, level as it stands).
                ToggleRetriggerElection(lookingTerminal, retriggerOffer);
                return;
            }

            if (!Ante.Enabled) return;

            if (_armedHatch || !string.IsNullOrEmpty(_armedTerminalId))
            {
                Log.Event("Run", "juice refused — intensity selected");
                return;
            }

            bool atTrigger = lookingHatch != null
                || (lookingTerminal != null
                    && (lookingTerminal.Kind == BastionTerminalKind.Field
                        || lookingTerminal.Kind == BastionTerminalKind.Master));
            Sim.TryJuice(delta, atTrigger);
        }

        bool CanSelectTerminal(BastionTerminal terminal)
        {
            if (terminal == null || Sim == null || Sim.MasterSequenceLive) return false;
            if (terminal.Kind == BastionTerminalKind.Master) return true;
            if (terminal.Kind != BastionTerminalKind.Field) return false;
            BastionLayout layout = terminal.Layout;
            if (layout != null && layout.IsTerminalDone(terminal.TerminalId))
                return false;
            // THE LOOP (Zo, 2026-08-25): the wave BEGINS at the Hatch. While the Hatch is the
            // start line a field terminal is an OBJECTIVE, not a trigger — the 2026-08-25
            // playthrough started at the terminal because the terminal let it, and the walk
            // back down the lane never happened. StartZoneWaveFromHatch reaches
            // StartTerminalWave directly and so is deliberately not gated by this.
            if (HatchOffersWaveStart) return false;
            return FieldStartBlockReason(terminal.TerminalId) == null;
        }

        /// <summary>Null when this Field id may start. Else the refuse suffix after
        /// <c>start refused — </c>.</summary>
        public string FieldStartBlockReason(string terminalId)
        {
            if (string.IsNullOrEmpty(terminalId) || _layout == null)
                return null;
            MetaState meta = Meta;
            bool t1Exists = _layout.FindTerminal(FirstTerminal.Id) != null;
            bool t1Done = _layout.IsTerminalDone(FirstTerminal.Id);
            if (!FirstTerminal.OtherFieldBlocked(
                    terminalId, meta != null && meta.DemoActive, t1Exists, t1Done))
                return null;
            // Only one reason survives the zone re-spec: the old "Hatch-bank first" case
            // needed the bank-started clause, and zone 1's bank comes after both terminals.
            return "finish this terminal";
        }

        void ToggleTerminalSelect(BastionTerminal terminal)
        {
            bool on = _armedTerminalId != terminal.TerminalId;
            _armedTerminalId = on ? terminal.TerminalId : null;
            if (on && terminal.TerminalId == FirstTerminal.Id)
                Log.Event("Demo", "first field id=" + FirstTerminal.Id + " waves=" + FirstTerminal.Waves);
            Log.Event("Terminal", terminal.TerminalId + (on
                ? " prep intensity=" + Sim.Siphon.Level + " juice=" + Sim.JuiceSpentThisPull
                    + " bodies=" + FieldPreviewBodies() + " — selected"
                : " intensity unlocked"));
        }

        void ToggleHatchSelect()
        {
            bool on = !_armedHatch;
            _armedHatch = on;
            Log.Event("Hatch", on
                ? "hatch-bank prep intensity=" + Sim.Siphon.Level + " juice=" + Sim.JuiceSpentThisPull
                    + " bodies=" + PreviewBodies() + " — selected"
                : "intensity unlocked");
        }

        /// <summary>Open any door the just-completed terminals have earned, snapshot the ids
        /// onto Meta so a Docked return finds them open, and speak the door line. Shared by
        /// the terminal shutoff and the Convert window, so the two cannot drift.</summary>
        void OpenDoorsFor(string cause)
        {
            BastionLayout layout = _layout;
            // The Hatch is a scene root, so the parent walk that fills _layout misses it on the
            // finale's Master Control path; the terminal's own reference is the fallback.
            // Unity fake-null: ==, not is.
            if (layout == null && _masterTerminal != null) layout = _masterTerminal.Layout;
            if (layout == null) return;
            if (!layout.OpenEligibleDoors(out string[] opened)) return;
            for (int i = 0; i < opened.Length; i++)
                Log.Event("Demo", "door open " + opened[i] + " cause=" + cause);
            if (Meta != null)
                Meta.SetOpenDoorIds(layout.CopyOpenDoorIds());
            if (opened.Length > 0 && Meta != null && Meta.DemoActive)
                DemoVoice.PlayLocalAndBroadcast(
                    DoorLineFor(opened[0]), DemoVoiceMode.Subtitle);
        }

        void ClearPendingTerminal()
        {
            _pendingTerminalId = null;
            _pendingWaveIndexAtPull = 0;
            _pendingLayout = null;
            if (Sim != null)
                Sim.CancelSequentialDraws();
        }

        /// <summary>Field terminal T after E-select: start the zone's trigger. A zone authored
        /// at one Wave per trigger draws one Wave and completes the terminal on its Nth clear;
        /// a zone authored above that launches its whole Wave span consecutively, with no Prep
        /// between them (Docs/Demo - Single/5-repeat-to-control-room.md). Prep after the clear is
        /// untimed either way.</summary>
        public bool StartTerminalWave(BastionTerminal terminal)
        {
            if (terminal == null)
            {
                Log.Warn(Tag, "StartTerminalWave refused — no terminal");
                return false;
            }

            if (_net != null && _net.IsClientOnly)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — client cannot start a Wave");
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

            if (Sim.AllWavesCleared)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — run already ended");
                return false;
            }

            if (Sim.MasterSequenceLive)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — Master Control sequence live");
                return false;
            }

            BastionLayout layout = terminal.Layout;
            if (layout != null && layout.IsTerminalDone(terminal.TerminalId))
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — already complete");
                return false;
            }

            if (!string.IsNullOrEmpty(_pendingTerminalId) || Sim.SequentialPending)
            {
                string pending = _pendingTerminalId;
                if (string.IsNullOrEmpty(pending))
                    pending = "sequence";
                Log.Event("Terminal", terminal.TerminalId + " refused — wave already pending "
                    + pending);
                return false;
            }

            if (Ante.Enabled && _armedTerminalId != terminal.TerminalId)
            {
                Log.Event("Terminal", terminal.TerminalId + " refused — select the Ante first");
                return false;
            }
            if (!Ante.Enabled)
                _armedTerminalId = terminal.TerminalId;

            string block = FieldStartBlockReason(terminal.TerminalId);
            if (block != null)
            {
                Log.Event("Demo", "start refused — " + block);
                return false;
            }

            _armedTerminalId = null;

            // A multi-Wave zone's trigger is one composite, never N separate draws with Prep
            // between them. Off the demo the table has no say — a campaign save keeps the
            // single draw at whatever Wave the Run has reached.
            MetaState demoMeta = Meta;
            int zone = demoMeta != null && demoMeta.DemoActive
                ? DemoZones.ZoneOf(terminal.TerminalId)
                : 0;
            int wavesPerTrigger = DemoZones.WavesPerTrigger(zone);
            if (wavesPerTrigger > 1)
                return StartZoneComposite(terminal, zone, wavesPerTrigger);

            int intensity = Sim.Siphon.Level;
            long juiced = Sim.JuiceSpentThisPull;

            // Field Wave Spawns register only while active; enable before TriggerDraw.
            terminal.SetUnlockedSpawnsActive(true);

            _pendingTerminalId = terminal.TerminalId;
            _pendingWaveIndexAtPull = Sim.CurrentWaveIndex;
            _pendingLayout = layout;
            Sim.TriggerDraw();
            if (Sim.Assault != AssaultState.Assault)
            {
                ClearPendingTerminal();
                Sim.CancelSequentialDraws();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

            Log.Event("Terminal", terminal.TerminalId + " started wave " + Sim.CurrentWaveIndex
                + " intensity=" + intensity + " juiced=" + juiced);
            if (Sim.FaucetCycleLive)
                _faucetTerminal = terminal; // this slice's off switch is the origin terminal
            return true;
        }

        /// <summary>The multi-Wave arm of <see cref="StartTerminalWave"/>: the zone's whole
        /// Wave span on one trigger, consecutively. Completion is the shared composite
        /// bookkeeping in GameController.Retrigger.cs — a first trigger and a post-loss
        /// retrigger end the same way, so there is one clear path, not two.</summary>
        bool StartZoneComposite(BastionTerminal terminal, int zone, int wavesPerTrigger)
        {
            int first = DemoZones.FirstWave(zone);
            int end = DemoZones.EndWave(zone);
            // The chain crosses every room the zone owns, so the zone's spawns go live, not
            // just this terminal's. Spawns register only while active; enable before the draw.
            ActivateZoneSpawns(zone);
            if (!Sim.TriggerCompositeDraw(first, end))
            {
                if (_layout != null) _layout.RestoreFieldSpawns();
                Log.Event("Terminal", terminal.TerminalId + " refused — draw did not start");
                return false;
            }

            _pendingCompositeZone = zone;
            _pendingCompositeEnd = end;
            Log.Event("Demo", "zone start zone=" + zone + " waves=" + wavesPerTrigger
                + " mode=composite");
            return true;
        }

        /// <summary>Is the Hatch the start line right now? Exactly while the current zone
        /// still has a terminal to run — which is also precisely when the zone's terminal
        /// must NOT offer a start of its own (see the retrigger dispatcher). One verb, one
        /// place. A save with every terminal done has every door open, and an open door
        /// retires the retrigger too, so there is deliberately nothing to fall through
        /// to: that save's next verb is Master Control, not another field wave.</summary>
        public bool HatchOffersWaveStart
        {
            get
            {
                if (Sim == null || _layout == null || Sim.Assault != AssaultState.Prep)
                    return false;
                if (HatchBankStartBlocked) return true;

                // AND THE FINALE, which is a zone the zone table does not author. Moving Master
                // Control's start to the Hatch on 2026-08-28 was only half a change: this getter
                // is what TickPrepCommands consults before it will call StartZoneWaveFromHatch
                // at all, and it answers off CurrentDemoZone — which walks PAST DemoZones.
                // ZoneCount once both doors are open, so HatchBankBlocked refuses an unauthored
                // zone and this returned false at the control room. With the Master terminal
                // now refusing to start (correctly — one start line), the finale would have
                // been unstartable and the demo uncompletable.
                //
                // ControlRoomReachable is the other half: without it, a zone whose terminals
                // are done but whose bank has not opened the control gate (F12-B/D, or a
                // shutoff that has not yet been banked) offered Master at the Hatch while D3
                // was still locked — "terminals say complete / wrong door locked".
                return MasterControlAwaitsStart && _layout.ControlRoomReachable;
            }
        }

        /// <summary>Is Master Control sitting unstarted at the Hatch? The finale is the last
        /// zone and begins where every other one does; it is simply not in the zone table, so
        /// the ordinary zone check cannot see it.</summary>
        public bool MasterControlAwaitsStart
        {
            get
            {
                if (Sim == null || _layout == null || RunIsOver) return false;
                if (Sim.Phase != RunPhase.Tethered) return false;
                if (Sim.MasterSequenceLive) return false;
                BastionTerminal master = _layout.FindMaster();
                if (master == null) return false;
                return !_layout.IsTerminalDone(master.TerminalId);
            }
        }

        /// <summary>The loop's start line: T at the Hatch runs the current zone's whole
        /// trigger. The wave is still owned by that zone's terminal — the same pending
        /// bookkeeping a terminal start used — so the far spawns, the door and the
        /// completion all behave exactly as before; only the place the player stands to
        /// begin has moved to the Hatch (Zo, 2026-08-25).</summary>
        public bool StartZoneWaveFromHatch()
        {
            if (Sim == null || _layout == null) return false;
            if (Sim.Assault != AssaultState.Prep) return false;

            if (_armedRetrigger && !string.IsNullOrEmpty(_armedTerminalId))
            {
                BastionTerminal elected = FindTerminalById(_armedTerminalId);
                if (elected != null && RetriggerOfferFor(elected).Available)
                {
                    Log.Event("Hatch", "wave started from the Hatch — the retrigger elected "
                        + "at " + elected.TerminalId);
                    return StartZoneRetrigger(elected);
                }
            }

            for (int zone = 1; zone <= DemoZones.ZoneCount; zone++)
            {
                if (ZoneTerminalsAllDone(_layout, zone)) continue;
                for (int i = 0; i < DemoZones.TerminalCount(zone); i++)
                {
                    string id = DemoZones.TerminalIdAt(zone, i);
                    if (string.IsNullOrEmpty(id) || _layout.IsTerminalDone(id)) continue;
                    BastionTerminal terminal = FindTerminalById(id);
                    if (terminal == null) continue;
                    // StartTerminalWave still demands its terminal be armed — that check is
                    // the Ante's, and the Ante is elected before the start wherever the start
                    // is pressed. Arming here is what makes the Hatch a real start line
                    // rather than one that logs a start and then refuses to draw.
                    _armedTerminalId = id;
                    Log.Event("Hatch", "wave started from the Hatch — zone " + zone
                        + " answers at " + id);
                    if (StartTerminalWave(terminal)) return true;
                    _armedTerminalId = null;
                    return false;
                }
            }
            // THE FINALE STARTS AT THE HATCH TOO (Zo, 2026-08-28: "Master terminal should
            // still be the same flow. Start at hatch and end at terminal"). Every field
            // terminal being done used to mean the Hatch had nothing left to offer, and the
            // only way into Master Control was pressing T at the Master terminal itself — a
            // SECOND START LINE, which is the exact shape 0-Gameplay.md forbids and which sent
            // the 2026-08-25 run to a terminal to begin a wave. The finale is a zone like the
            // others: it begins here — but only once the control gate is open. Offering it
            // while D3 is still shut (zone terminals done, bank not survived) started a
            // finale the player could not walk to.
            BastionTerminal master = _layout != null ? _layout.FindMaster() : null;
            if (master != null && Sim.Assault == AssaultState.Prep && !Sim.MasterSequenceLive
                && _layout.ControlRoomReachable
                && !_layout.IsTerminalDone(master.TerminalId))
            {
                _armedTerminalId = master.TerminalId;
                Log.Event("Hatch", "wave started from the Hatch — Master Control answers at "
                    + master.TerminalId);
                if (StartMasterControl(master)) return true;
                _armedTerminalId = null;
                return false;
            }

            Log.Event("Hatch", "no zone left to start — every field terminal is done");
            return false;
        }

        /// <summary>THE LOOP (Zo, 2026-08-25, enforced 2026-08-26): the wave began at the
        /// Hatch; reaching this terminal is what ENDS it, and this is the ONLY thing that
        /// completes a zone. Completing the terminal outright — rather than crediting only
        /// the waves that happened to run — is the point: the objective is positional, so a
        /// player who fights all the way up the lane finishes the zone whether or not the
        /// last body fell. The pending-terminal bookkeeping is cleared first so the
        /// AssaultEnded handler does not record the same terminal twice.
        ///
        /// <para>PREP IS LEGAL HERE. A player who holds the Hatch and kills every body
        /// arrives to an empty lane, and the zone is still unfinished — the walk is the
        /// objective, not a consolation for failing to kill. Refusing outside Assault is what
        /// left the 2026-08-26 playthrough pressing T at a dead prop three times.</para></summary>
        /// <summary>Has this terminal's zone actually been run at? The shutoff is the END of a
        /// wave that began at the Hatch, so it may not be walked up to and pressed on a zone
        /// nobody has started — that would let a player skip the lane entirely. Read from the
        /// Run rather than the layout: CurrentWaveIndex is the last field Wave launched, and
        /// the zone's span is what the Hatch would have drawn.</summary>
        bool ZoneWavesHaveRun(string terminalId)
        {
            return Sim != null && FieldShutoff.WavesHaveRun(terminalId, Sim.CurrentWaveIndex);
        }

        /// <summary>Is a field wave waiting on a shutoff that has not happened yet? True once
        /// the current zone's terminal can be shut off — which is exactly when the lane should
        /// keep dripping (<see cref="FieldTrickle"/>), because the walk up to that terminal is
        /// the rest of the wave. Read every frame by the spawner, so it allocates nothing.</summary>
        public bool FieldWaveAwaitsShutoff
        {
            get
            {
                if (Sim == null || _layout == null || RunIsOver) return false;
                if (Sim.Phase != RunPhase.Tethered) return false;
                if (Sim.Mode == WaveMode.HatchBank) return false;
                if (Sim.MasterSequenceLive) return false;
                int zone = CurrentDemoZone;
                int count = DemoZones.TerminalCount(zone);
                for (int i = 0; i < count; i++)
                {
                    string id = DemoZones.TerminalIdAt(zone, i);
                    if (!string.IsNullOrEmpty(id) && CanShutOffTerminal(id)) return true;
                }
                return false;
            }
        }

        /// <summary>Is the shutoff on offer at this terminal right now? The prompt's question,
        /// answered by the same pure rule (<see cref="FieldShutoff.MayRun"/>) ShutOffTerminal
        /// enforces, so the two cannot disagree. This shell only supplies the scene-owned
        /// fact (terminal done) as a value.</summary>
        public bool CanShutOffTerminal(string terminalId)
        {
            if (Sim == null || _layout == null) return false;
            // MASTER CONTROL ENDS AT ITS TERMINAL TOO (Zo, 2026-08-28), and the elite is the
            // gate: killing it arms the switch and the switch ends the Boarding. The finale is
            // not in the zone table FieldShutoff reads, so it is answered before that.
            var master = _layout.FindMaster();
            if (master != null && master.TerminalId == terminalId)
                return Sim.MasterAwaitsShutoff;
            return FieldShutoff.MayRun(Sim.Phase, RunIsOver, Sim.Mode, terminalId,
                _layout.IsTerminalDone(terminalId), Sim.CurrentWaveIndex);
        }

        public bool ShutOffTerminal(BastionTerminal terminal)
        {
            if (terminal == null || Sim == null || _layout == null) return false;
            string id = terminal.TerminalId;
            if (string.IsNullOrEmpty(id)) return false;
            if (Sim.Phase != RunPhase.Tethered || RunIsOver)
            {
                Log.Event("Terminal", id + " shutoff refused — no boarding live");
                return false;
            }
            if (Sim.Assault == AssaultState.Prep && !ZoneWavesHaveRun(id))
            {
                Log.Event("Terminal", id + " shutoff refused — start the wave at the Hatch first");
                return false;
            }
            if (Sim.Assault != AssaultState.Assault && Sim.Assault != AssaultState.Prep)
            {
                Log.Event("Terminal", id + " shutoff refused — no wave is running");
                return false;
            }
            if (Sim.Mode == WaveMode.HatchBank)
            {
                Log.Event("Terminal", id + " shutoff refused — a Convert window has no off switch");
                return false;
            }

            if (terminal.Kind == BastionTerminalKind.Master)
            {
                if (!Sim.MasterAwaitsShutoff)
                {
                    Log.Event("Terminal", id + " shutoff refused — the elite is still standing");
                    return false;
                }
                Log.Event("Terminal", id + " shut down — Master Control ends here");
                return Sim.ShutOffMasterControl();
            }

            BastionLayout layout = _layout;
            int guard = 0;
            while (!layout.IsTerminalDone(id) && guard++ < 16)
            {
                if (layout.RecordWaveCleared(id, 1, out int _, out int _, out string why))
                    continue;
                Log.Event("Terminal", id + " shutoff refused — " + why);
                return false;
            }

            OpenDoorsFor("shutoff");
            ClearPendingTerminal();
            Sim.SetFieldTrickle(false);   // the lane stops dripping here — that is the point
            Sim.CancelSequentialDraws();
            // Ending the wave is only half of this verb, and only when there IS one. A player
            // who killed the field empty and then walked up arrives in Prep with nothing to
            // end — the zone still closes here, because the walk is the objective. Refusing on
            // ShutOffFieldWave's Assault-only rule left the terminal completed and the call
            // reporting failure, which is the worst of both.
            if (Sim.Assault == AssaultState.Assault && !Sim.ShutOffFieldWave())
            {
                Log.Warn(Tag, id + " shut off but the sim would not leave Assault");
                return false;
            }
            Log.Event("Terminal", id + " shut down — "
                + (Sim.Assault == AssaultState.Prep ? "the zone closes here" : "the wave ends here"));
            int zone = DemoZones.ZoneOf(id);
            if (zone > 0 && ZoneTerminalsAllDone(layout, zone))
                Log.Event("Demo", "zone terminals complete zone=" + zone);
            return true;
        }

        void RecordPendingTerminalClear()
        {
            if (string.IsNullOrEmpty(_pendingTerminalId))
                return;

            string id = _pendingTerminalId;
            int indexAtPull = _pendingWaveIndexAtPull;
            BastionLayout layout = _pendingLayout;

            if (layout == null)
            {
                Log.Warn(Tag, "AssaultEnded: pending terminal " + id + " but no BastionLayout");
                ClearPendingTerminal();
                return;
            }

            int waves = Sim.CurrentWaveIndex - indexAtPull;
            if (!layout.RecordWaveCleared(id, waves, out int k, out int n, out string refuseReason))
            {
                Log.Event("Terminal", id + " complete refused — " + refuseReason);
                ClearPendingTerminal();
                Sim.CancelSequentialDraws();
                return;
            }

            Log.Event("Terminal", id + " wave " + k + "/" + n + " cleared");
            // DemoZoneDeepestWave is NOT written here any more: RunSim.FieldWaveCleared
            // carries every field-Wave clear, including the ones inside a composite this
            // method never sees, and OnFieldWaveCleared below writes them all.
            MetaState meta = Meta;
            if (layout.IsTerminalDone(id))
            {
                Log.Event("Terminal", id + " complete — wave cleared");
                int zone = DemoZones.ZoneOf(id);
                if (meta != null && meta.DemoActive && zone > 0
                    && ZoneTerminalsAllDone(layout, zone))
                    Log.Event("Demo", "zone terminals complete zone=" + zone);
            }
            ClearPendingTerminal();
            // Faucet chain: the next chained Wave is this same terminal's — keep recording
            // clears until the terminal completes or the cycle ends (faucet graybox).
            if (Sim.FaucetCycleLive && !layout.IsTerminalDone(id))
            {
                _pendingTerminalId = id;
                _pendingWaveIndexAtPull = Sim.CurrentWaveIndex;
                _pendingLayout = layout;
            }
        }

        /// <summary>True when every terminal the zone table authors is complete in this
        /// layout. Absent ids answer false — a zone is not complete because a room is
        /// missing.</summary>
        static bool ZoneTerminalsAllDone(BastionLayout layout, int zone)
        {
            int count = DemoZones.TerminalCount(zone);
            if (count <= 0) return false;
            for (int i = 0; i < count; i++)
            {
                string id = DemoZones.TerminalIdAt(zone, i);
                if (string.IsNullOrEmpty(id) || !layout.IsTerminalDone(id))
                    return false;
            }
            return true;
        }

        /// <summary>A field Wave cleared (RunSim.FieldWaveCleared; 1-based, never a bank
        /// Wave). Per-zone deepest writes on field-Wave CLEAR, never on bank start (ruled at
        /// cut, Zo 2026-08-17), and it writes per Wave rather than per trigger so a composite
        /// that dies mid-chain still records the Waves it did clear — that is what the partial
        /// election and the guest BankCap read (Docs/Demo - Single/5-repeat-to-control-room.md).
        /// SetZoneDeepest is raise-only, so re-clearing an earlier Wave cannot lower it.</summary>
        void OnFieldWaveCleared(int wave)
        {
            // Depth rises here, mid-run, and the plate on the Ship shows it. A guest waiting
            // out a down would otherwise read the pre-run figure until the run ended.
            RealmRecordPublisher.Publish("wave cleared");
            MetaState meta = Meta;
            if (meta == null || !meta.DemoActive)
                return;
            meta.SetZoneDeepest(DemoZones.ZoneOfWave(wave), wave);
        }
    }
}
