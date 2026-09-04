using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the equipped protocol's actives on G/H. Ids pushed at Bind — never
    /// serialized (gotcha 1). This type owns the two windows, the input and the log lines;
        /// the effects that hold state of their own live in their own types
        /// (<see cref="TurretObject"/> and <see cref="OutriderObject"/> for FORGE, and
        /// <see cref="WaveMachineRecord"/> for what those two did across a Wave). Cooldowns run
        /// here; Dump auras go server-side via NetVerbs when a session is listening.</summary>
    public sealed class PlayerProtocolAbilities : MonoBehaviour
    {
        const string Tag = "Protocol";

        GameController _game;
        FirstPersonController _fpc;
        PlayerHealth _health;
        PlayerGun _gun;
        PlayerTurretControl _turretControl;
        PlayerOutriderControl _outriderControl;
        BoarderProtocolId _protocol = BoarderProtocolId.None;
        BoarderAbilityId _passive = BoarderAbilityId.None;
        BoarderAbilityState _crew;
        BoarderAbilityState _personal;
        BarrageField _planted;
        bool _hosted;
        bool _loggedCrewExpiry;
        bool _loggedPersonalExpiry;
        readonly WaveMachineRecord _machines = new WaveMachineRecord();

        public bool HasProtocol => _protocol != BoarderProtocolId.None;
        public string ProtocolName => BoarderProtocol.ProtocolName(_protocol);
        public BoarderAbilityId CrewAbility => BoarderProtocol.CrewAbilityOf(_protocol);
        public BoarderAbilityId PersonalAbility => BoarderProtocol.PersonalAbilityOf(_protocol);

        /// <summary>Dump's zone belongs to Depot alone. Gated on the ability id, not on
        /// the window: Cloak and Overcharge open the same slots and must not hand anyone
        /// Dump's multiplier.</summary>
        public bool BarrageWindowOpen =>
            CrewAbility == BoarderAbilityId.Barrage && _crew.WindowOpen;

        /// <summary>Feed window open (hold ammo regen).</summary>
        public bool RushOpen =>
            PersonalAbility == BoarderAbilityId.Rush && _personal.WindowOpen;

        /// <summary>VEIL's render window. The through-walls presentation reads this.</summary>
        public bool PingWindowOpen =>
            CrewAbility == BoarderAbilityId.Ping && _crew.WindowOpen;

        public bool CloakOpen =>
            PersonalAbility == BoarderAbilityId.Cloak && _personal.WindowOpen;

        public bool OverchargeOpen =>
            _turretControl != null && _turretControl.IsOvercharged;

        /// <summary>This Boarder's own fire-rate multiplier. Exclusive by construction — one
        /// protocol means one personal ability — so the gun asks for a number, not a set of
        /// flags it has to combine correctly.</summary>
        public float SelfFireMul
        {
            get
            {
                // Feed (Rush id) regenerates clip — it does not multiply fire rate.
                if (OverchargeOpen) return BoarderProtocol.OverchargeSelfFireMul;
                return 1f;
            }
        }

        /// <summary>FORGE's standing machine, or null. The gun never reads this — the aura is
        /// positional, not owner-scoped.</summary>
        public TurretObject Turret => _turretControl != null ? _turretControl.Live : null;

        /// <summary>Whichever ability currently has a window open, or None. Personal wins a
        /// tie: a Cloak inside a running Ping is the louder thing to be told about, and two
        /// screen treatments at once average into a colour belonging to neither.</summary>
        public BoarderAbilityId ActiveWindowAbility
        {
            get
            {
                if (_personal.WindowOpen) return PersonalAbility;
                if (_crew.WindowOpen) return CrewAbility;
                return BoarderAbilityId.None;
            }
        }

        public string CrewName => BoarderProtocol.DisplayName(CrewAbility);
        public string PersonalName => BoarderProtocol.DisplayName(PersonalAbility);
        public string PassiveName => BoarderProtocol.DisplayName(_passive);

        /// <summary>The standing rule this Boarder wears. Target selection reads it.</summary>
        public BoarderAbilityId Passive => _passive;
        public float CrewWindow => _crew.WindowRemaining;
        public float CrewCooldown => _crew.CooldownRemaining;
        public float PersonalWindow => _personal.WindowRemaining;
        public float PersonalCooldown => _personal.CooldownRemaining;
        public bool CrewReady => _crew.IsReady;
        public bool PersonalReady => _personal.IsReady;
        public bool HasTurret => _turretControl != null && _turretControl.HasTurret;
        public bool HasOutrider => _outriderControl != null && _outriderControl.HasOutrider;

        public bool MachinesPaired
        {
            get
            {
                var verbs = LocalVerbs();
                if (verbs != null) return verbs.ServerMachinesPaired;
                return TurretObject.PairingActive(transform);
            }
        }

        /// <summary>Charge fraction for the crew square. Not routed through
        /// <see cref="PlayerAbility"/> — that type divides by the active weapon's cooldown.</summary>
        public float TurretChargeFraction
        {
            get
            {
                if (_turretControl == null) return 0f;
                float max = _turretControl.ChargeMax;
                return max > 0f ? _turretControl.Charge / max : 0f;
            }
        }

        /// <summary>Charge readout for the crew square. The HUD asks for this from LateUpdate,
        /// every frame the strip is up, so the string comes out of the shared meter table —
        /// concatenating one here made a garbage string per frame per machine.</summary>
        public string TurretChargeLabel =>
            _turretControl == null ? "" : MachineChargeLabels.OfTurret(_turretControl.Charge);

        /// <summary>True when the crew square should show turret charge rather than cooldown.
        /// </summary>
        public bool TryCrewChargeSquare(out float fill, out string label)
        {
            fill = 0f;
            label = "";
            if (CrewAbility != BoarderAbilityId.FieldDeploy || !HasTurret) return false;
            fill = TurretChargeFraction;
            label = TurretChargeLabel;
            return true;
        }

        public float OutriderChargeFraction
        {
            get
            {
                if (_outriderControl == null) return 0f;
                float max = _outriderControl.ChargeMax;
                return max > 0f ? _outriderControl.Charge / max : 0f;
            }
        }

        /// <summary>Charge readout for the personal square, on the same per-frame path and off
        /// the same table as <see cref="TurretChargeLabel"/>.</summary>
        public string OutriderChargeLabel =>
            _outriderControl == null ? "" : MachineChargeLabels.OfOutrider(_outriderControl.Charge);

        public bool TryPersonalChargeSquare(out float fill, out string label)
        {
            fill = 0f;
            label = "";
            if (PersonalAbility != BoarderAbilityId.Emp || !HasOutrider) return false;
            fill = OutriderChargeFraction;
            label = OutriderChargeLabel;
            return true;
        }

        /// <summary>Wave boundary. The rig is the only thing that knows what is still standing,
        /// so it hands that in; the record owns the rule that a survivor counts as this Wave's
        /// first.</summary>
        public void BeginWaveMachines() { _machines.BeginWave(HasTurret, HasOutrider); }

        public void NoteMachineDeploy(string machine) { _machines.NoteDeploy(machine); }

        public void NoteTurretSpecial(float charge) { _machines.NoteTurretSpecial(charge); }

        public void NoteOutriderSpecial(float charge) { _machines.NoteOutriderSpecial(charge); }

        /// <summary>Criterion 10: which machine deployed first this Wave, and each machine's
        /// charge when its special ran. Null when neither machine was deployed.</summary>
        public string WaveMachineLine() { return _machines.Line(); }

        public void Bind(GameController game, BoarderProtocolId protocol, BoarderAbilityId passive)
        {
            _game = game;
            _protocol = protocol;
            _passive = BoarderProtocol.OwnsPassive(_protocol, passive) ? passive : BoarderAbilityId.None;
            if (_fpc == null) _fpc = GetComponent<FirstPersonController>();
            if (_health == null) _health = GetComponent<PlayerHealth>();
            if (_gun == null) _gun = GetComponent<PlayerGun>();
            if (_turretControl == null) _turretControl = GetComponent<PlayerTurretControl>();
            if (_turretControl == null) _turretControl = gameObject.AddComponent<PlayerTurretControl>();
            _turretControl.Bind(game, _fpc);
            if (_outriderControl == null) _outriderControl = GetComponent<PlayerOutriderControl>();
            if (_outriderControl == null) _outriderControl = gameObject.AddComponent<PlayerOutriderControl>();
            _outriderControl.Bind(game);
            if (GetComponent<ProtocolLayerView>() == null)
                gameObject.AddComponent<ProtocolLayerView>();
            ApplyPassive();
        }

        /// <summary>Standing rules. Ghost biases what notices you. Every path writes the
        /// acquisition bias so a protocol swap cannot leave the previous protocol's rule on
        /// the rig.</summary>
        void ApplyPassive()
        {
            if (_health == null) return;
            _health.ConfigureAcquisition(_passive == BoarderAbilityId.Ghost
                ? BoarderProtocol.Magnitude(BoarderAbilityId.Ghost)
                : 1f);
        }

        public void SetHosted(bool hosted) { _hosted = hosted; }

        /// <summary>Drop both protocol cooldowns. The range's bypass, cleared from outside for
        /// the same reason <see cref="PlayerAbility.ClearCooldown"/> is. An open window is left
        /// alone: a window is the ability doing its job, not the wait afterwards.</summary>
        public void ClearCooldowns()
        {
            _crew.CooldownRemaining = 0f;
            _personal.CooldownRemaining = 0f;
        }

        void Update()
        {
            if (_hosted) return;
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            bool crewWasOpen = _crew.WindowOpen;
            bool personalWasOpen = _personal.WindowOpen;
            _crew.Tick(dt);
            _personal.Tick(dt);

            if (crewWasOpen && !_crew.WindowOpen) CloseCrewEffect(true);
            if (personalWasOpen && !_personal.WindowOpen) ClosePersonalEffect(true);

            bool paused = OptionsScreen.IsOpen || HudController.IsPauseOpen;
            if (CrewAbility == BoarderAbilityId.FieldDeploy && _turretControl != null)
                _turretControl.TickCrewBinding(dt, paused, ref _crew);
            else if (!paused && Controls.Down(ControlId.CrewAbility))
                OnCrew();
            if (PersonalAbility == BoarderAbilityId.Emp && _outriderControl != null)
                _outriderControl.TickPersonalBinding(dt, paused, ref _personal);
            else if (!paused && Controls.Down(ControlId.PersonalAbility))
                OnPersonal();

            if (!paused && RushOpen)
            {
                if (_gun == null) _gun = GetComponent<PlayerGun>();
                if (_gun != null) _gun.TickFeedRegen(dt);
            }
        }

        public void NotifyDeath()
        {
            if (_crew.WindowOpen)
            {
                _crew.EndWindowEarly();
                CloseCrewEffect(true);
            }
            else if (CrewAbility == BoarderAbilityId.Barrage)
            {
                // Dump leaves an aura on the caster; clear it if death skipped the window path.
                EndDump();
            }
            if (_personal.WindowOpen)
            {
                _personal.EndWindowEarly();
                ClosePersonalEffect(true);
            }
            if (_turretControl != null) _turretControl.CancelCrewHold();
            if (_outriderControl != null) _outriderControl.CancelHold();
        }

        /// <summary>The gun fired. Cloak is the only window firing collapses — it is the whole
        /// price of the layer.</summary>
        public void NotifyFired()
        {
            if (!CloakOpen) return;
            _personal.EndWindowEarly();
            ClosePersonalEffect(false);
            Log.Event(Tag, "self Cloak dropped — fired");
        }

        /// <summary>A gatekeeper died to this player. VEIL's re-entry (2026-09-04): each kill
        /// takes <see cref="BoarderProtocol.CloakKillRefundSeconds"/> off Cloak's cooldown, so
        /// the way back into stealth is killing rather than waiting. Every other personal
        /// ability refunds nothing and this is a cheap early return for them.
        ///
        /// Routed from GameController's one kill seam, which already drops trickle bodies
        /// before it calls anything: a body that pays no Salvage and mends no health must not
        /// buy stealth either.</summary>
        public void NotifyKill()
        {
            float refund = BoarderProtocol.KillRefundSeconds(PersonalAbility);
            if (refund <= 0f) return;
            if (!_personal.RefundCooldown(refund)) return;
            Log.Event(Tag, "self " + BoarderProtocol.DisplayName(PersonalAbility)
                + " refunded " + refund.ToString("0.#") + "s on a kill — "
                + _personal.CooldownRemaining.ToString("0.#") + "s to re-enter");
        }

        public void NotifyBulkheadConsumed()
        {
            _loggedPersonalExpiry = false;
            _personal.EndWindowEarly();
        }

        // ---------- crew ----------

        void OnCrew()
        {
            if (!HasProtocol)
            {
                Log.Event(Tag, "crew refused — no protocol");
                return;
            }
            var id = CrewAbility;
            if (id == BoarderAbilityId.None)
            {
                Log.Event(Tag, "crew refused — this protocol has no crew ability");
                return;
            }

            if (_crew.WindowOpen)
            {
                // Dump: pressing again stands the aura down. Ping is a pulse already spent.
                // Field Deploy never opens a window at all.
                if (id != BoarderAbilityId.Barrage)
                {
                    Log.Event(Tag, "crew refused — " + BoarderProtocol.DisplayName(id)
                        + " window already running");
                    return;
                }
                float sat = _crew.SecondsSatReady;
                _crew.EndWindowEarly();
                CloseCrewEffect(false);
                Log.Event(Tag, "crew " + BoarderProtocol.DisplayName(id)
                    + " stood down sat-ready=" + sat.ToString("0.##"));
                return;
            }
            if (!_crew.IsReady)
            {
                Log.Event(Tag, "crew refused — " + _crew.CooldownRemaining.ToString("0.#") + "s left");
                return;
            }

            float ready = _crew.SecondsSatReady;

            if (!_crew.TryActivate(BoarderProtocol.Window(id), BoarderProtocol.Cooldown(id)))
                return;
            _loggedCrewExpiry = true;

            switch (id)
            {
                case BoarderAbilityId.Barrage:
                    BeginDump();
                    Log.Event(Tag, "crew Dump sat-ready=" + ready.ToString("0.##"));
                    break;
                case BoarderAbilityId.Ping:
                    PulsePing(ready);
                    break;
            }
        }

        void CloseCrewEffect(bool expired)
        {
            var id = CrewAbility;
            switch (id)
            {
                case BoarderAbilityId.Barrage:
                    EndDump();
                    break;
            }
            if (expired && _loggedCrewExpiry)
                Log.Event(Tag, "crew " + BoarderProtocol.DisplayName(id) + " window ended");
            _loggedCrewExpiry = false;
        }

        /// <summary>Cloak both sides of the wire. The local rig keeps the flag for its own
        /// registry entry; the server needs it on the avatar, which is the body every host-side
        /// gatekeeper actually targets. Setting only the local one leaves a guest uncloaked in
        /// the eyes of every enemy in the game.</summary>
        void SetCloaked(bool cloaked)
        {
            if (_health != null) _health.SetCloaked(cloaked);
            var verbs = LocalVerbs();
            if (verbs != null) verbs.RequestCloak(cloaked);
        }

        /// <summary>VEIL's PING: the crew gets the map, the caster gets the room. The pull is
        /// the existing provoke rule, so every kind refusal (a Thief walks on and never
        /// retargets) is honoured without this knowing which kinds those are.</summary>
        void PulsePing(float ready)
        {
            var verbs = LocalVerbs();
            if (verbs != null)
            {
                // Gatekeepers are host-only; a guest provoking its own clones moves nothing.
                verbs.RequestPing();
                Log.Event(Tag, "crew Ping — requested, "
                    + BoarderProtocol.Window(BoarderAbilityId.Ping).ToString("0")
                    + "s render sat-ready=" + ready.ToString("0.##"));
                return;
            }
            int turned = 0;
            var live = LiveGatekeepers();
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                if (gk.Provoke(transform, shout: false)) turned++;
            }
            PingMarker.ShowAll(live, BoarderProtocol.Window(BoarderAbilityId.Ping));
            Log.Event(Tag, "crew Ping — " + turned + " gatekeeper(s) turned on the caster, "
                + BoarderProtocol.Window(BoarderAbilityId.Ping).ToString("0")
                + "s render sat-ready=" + ready.ToString("0.##"));
        }

        // ---------- personal ----------

        void OnPersonal()
        {
            if (!HasProtocol)
            {
                Log.Event(Tag, "self refused — no protocol");
                return;
            }
            var id = PersonalAbility;
            if (id == BoarderAbilityId.None)
            {
                Log.Event(Tag, "self refused — this protocol has no personal ability");
                return;
            }

            if (_personal.WindowOpen)
            {
                // Feed is a toggle: press again to stand down early. Cloak refuses a second press.
                if (id != BoarderAbilityId.Rush)
                {
                    Log.Event(Tag, "self refused — window open");
                    return;
                }
                float sat = _personal.SecondsSatReady;
                _personal.EndWindowEarly();
                ClosePersonalEffect(false);
                Log.Event(Tag, "self Feed stood down sat-ready=" + sat.ToString("0.##"));
                return;
            }
            if (!_personal.IsReady)
            {
                Log.Event(Tag, "self refused — " + _personal.CooldownRemaining.ToString("0.#") + "s left");
                return;
            }

            float ready = _personal.SecondsSatReady;
            if (!_personal.TryActivate(BoarderProtocol.Window(id), BoarderProtocol.Cooldown(id)))
                return;
            _loggedPersonalExpiry = true;

            switch (id)
            {
                case BoarderAbilityId.Rush:
                    Log.Event(Tag, "self Feed sat-ready=" + ready.ToString("0.##"));
                    break;
                case BoarderAbilityId.Cloak:
                    SetCloaked(true);
                    Log.Event(Tag, "self Cloak sat-ready=" + ready.ToString("0.##")
                        + " — off every targeting list");
                    break;
            }
        }

        void ClosePersonalEffect(bool expired)
        {
            var id = PersonalAbility;
            switch (id)
            {
                case BoarderAbilityId.Cloak:
                    SetCloaked(false);
                    break;
            }
            if (expired && _loggedPersonalExpiry)
                Log.Event(Tag, "self " + BoarderProtocol.DisplayName(id) + " window ended");
            _loggedPersonalExpiry = false;
        }

        // ---------- Dump aura ----------

        void BeginDump()
        {
            EndDump();
            if (_fpc != null) _fpc.MoveSpeedMul = BoarderProtocol.DumpMoveMul;
            var verbs = LocalVerbs();
            if (verbs != null)
                verbs.RequestAnchor(transform.position, true);
            else
                _planted = BarrageField.Plant(transform);
        }

        void EndDump()
        {
            if (_fpc != null) _fpc.MoveSpeedMul = 1f;
            var verbs = LocalVerbs();
            if (verbs != null)
            {
                verbs.RequestAnchor(transform.position, false);
                return;
            }
            if (_planted == null) return;
            BarrageField.Uproot(_planted);
            _planted = null;
        }

        /// <summary>Live gatekeepers via the spawner's ledger — the one list, reached through
        /// the bound controller so there is no process-global actor list.</summary>
        System.Collections.Generic.IReadOnlyList<EnemyGatekeeper> LiveGatekeepers()
        {
            var gc = _game;
            if (gc == null || gc.Spawner == null)
                return System.Array.Empty<EnemyGatekeeper>();
            return gc.Spawner.Live;
        }

        NetVerbs LocalVerbs()
        {
            var net = _game != null ? _game.Net : null;
            return net != null ? net.LocalVerbs : null;
        }
    }
}
