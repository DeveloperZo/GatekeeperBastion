using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: activated weapon ability on the player camera. Reads the active
    /// slot's <see cref="WeaponDef"/>, runs the cooldown, and dispatches by
    /// <see cref="AbilityId"/>. Gatekeeper motion goes through <see cref="EnemyGatekeeper.Shove"/> /
    /// <see cref="EnemyGatekeeper.Pin"/> — never by writing NavMeshAgent from here.</summary>
    /// <remarks>
    /// Dial points come from the active weapon's ability spoke on the upgrade wheel.
    /// </remarks>
    public sealed class PlayerAbility : MonoBehaviour
    {
        const string Tag = "PlayerAbility";

        PlayerGun _gun;
        // Pushed by GameController.ApplyMetaEffects (Bind); the wave-tally events come off its Sim.
        GameController _game;
        float _cooldown;
        float _surgeRemaining;
        float _waveGatekeeperSeconds;
        float _waveRegenRounds;
        PinBuildState _pinBuild;
        EnemyGatekeeper _pinTarget;
        bool _subscribed;
        bool _waveTallyOpen;

        /// <summary>Seconds left on the ability cooldown for the active weapon. 0 = ready.</summary>
        public float CooldownRemaining => Mathf.Max(0f, _cooldown);

        /// <summary>1 when ready, 0 when the cooldown just started.</summary>
        public float CooldownProgress
        {
            get
            {
                var def = ActiveDef;
                if (WeaponAbility.IsEmpty(def.abilityId) || def.abilityCooldown <= 0.01f)
                    return 1f;
                return 1f - Mathf.Clamp01(_cooldown / def.abilityCooldown);
            }
        }

        public string AbilityName
        {
            get
            {
                var def = ActiveDef;
                return string.IsNullOrEmpty(def.abilityName) ? "" : def.abilityName;
            }
        }

        public bool HasAbility => !WeaponAbility.IsEmpty(ActiveDef.abilityId);

        /// <summary>Fixed MK-1 surge rate while live; 1 otherwise. Dial stretches duration.</summary>
        public float RegenSurgeMultiplier =>
            _surgeRemaining > 0f ? WeaponAbility.Mk1SurgeMultiplier : 1f;

        public bool RegenSurgeActive => _surgeRemaining > 0f;

        /// <summary>Pin stacks on the current build target (MR-35). 0 when none.</summary>
        public int PinBuildHits => _pinBuild.Hits;

        /// <summary>Ability dial from the active weapon's wheel points.</summary>
        public int DialPoints => _gun != null ? _gun.ActiveAbilityPoints : 0;

        WeaponDef ActiveDef => _gun != null ? _gun.ActiveDef : default;

        void Awake()
        {
            _gun = GetComponent<PlayerGun>();
            if (_gun == null) _gun = GetComponentInParent<PlayerGun>();
        }

        void OnDisable() => Unsubscribe();

        /// <summary>Adopt the scene's GameController (called from ApplyMetaEffects at run
        /// start) and subscribe to its Sim's wave events right away.</summary>
        public void Bind(GameController game)
        {
            _game = game;
            EnsureSubscribed();
        }

        void EnsureSubscribed()
        {
            if (_subscribed) return;
            var gc = _game;
            if (gc == null || gc.Sim == null) return;
            gc.Sim.WaveStarted += OnWaveStarted;
            gc.Sim.AssaultEnded += OnAssaultEnded;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            var gc = _game;
            if (gc != null && gc.Sim != null)
            {
                gc.Sim.WaveStarted -= OnWaveStarted;
                gc.Sim.AssaultEnded -= OnAssaultEnded;
            }
            _subscribed = false;
        }

        /// <summary>True once the Bastion's tick owner drives this component; its own
        /// Update then yields so the written system order is the only order.</summary>
        bool _hosted;

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return; // the Bastion tick owner calls Tick in the written order
            Tick(Time.deltaTime);
        }

        /// <summary>Ability cooldown, surge window, pin decay and activation input.</summary>
        public void Tick(float dt)
        {
            EnsureSubscribed();

            if (_gun == null)
            {
                _gun = GetComponent<PlayerGun>();
                if (_gun == null) return;
            }

            if (_cooldown > 0f) _cooldown -= dt;
            if (_surgeRemaining > 0f) _surgeRemaining -= dt;

            TickPinDecay(dt);
            if (_gun != null) _gun.ClearExpiredRailshot();

            if (!Controls.Down(ControlId.WeaponAbility)) return;
            TryActivate();
        }

        void TickPinDecay(float dt)
        {
            // Charge is weapon-scoped — a dead build target must not wipe Hits, or trash kills
            // that arm a Ram pin can never land. Only the decay window clears the build.
            if (_pinBuild.Hits <= 0) return;
            _pinBuild.Tick(dt);
        }

        /// <summary>MR-35 hits accumulate here — count of hits, never damage. Auto-pins when
        /// the threshold is reached and the ability cooldown is ready. Charge is weapon-scoped
        /// (survives target changes); a killing blow that only tips the meter does not spend it.</summary>
        public void RegisterPinHit(EnemyGatekeeper gatekeeper)
        {
            if (gatekeeper == null) return;
            if (_gun == null || _gun.ActiveWeaponId != WeaponId.Mr35Rifle) return;
            if (_cooldown > 0f) return;

            _pinTarget = gatekeeper;
            _pinBuild.RegisterHit(gatekeeper.GetInstanceID(), WeaponAbility.PinBuildDecaySeconds);
            if (!_pinBuild.Ready) return;

            // T10 MR-35 one-shots Defaults; spending the pin on that corpse wastes the charge.
            // Leave Ready armed until a living body takes the tipping (or follow-up) hit.
            var body = gatekeeper.GetComponent<Damageable>();
            if (body != null && body.IsDead)
                return;

            float seconds = WeaponAbility.Magnitude(AbilityId.Mr35Pin, DialPoints);
            if (!ApplyPin(gatekeeper, seconds)) return;
            var def = ActiveDef;
            BeginCooldown(def);
            Log.Event("Ability",
                $"{def.displayName} — {def.abilityName} (dial {DialPoints}, mag {seconds:0.##})");
        }

        void TryActivate()
        {
            var def = ActiveDef;
            if (WeaponAbility.IsEmpty(def.abilityId))
            {
                Log.Info(Tag, "ability refused — active weapon has no ability");
                return;
            }
            if (_cooldown > 0f) return;

            int dial = DialPoints;
            float magnitude = WeaponAbility.Magnitude(def.abilityId, dial);
            bool fired;
            switch (def.abilityId)
            {
                case AbilityId.Br12Concussive:
                    fired = ActivateConcussive(def, magnitude);
                    break;
                case AbilityId.Mr35Pin:
                    // Pin is hit-driven; key only fires if build is already ready.
                    fired = TryManualPin(magnitude);
                    break;
                case AbilityId.Mk1RegenSurge:
                    fired = ActivateRegenSurge(magnitude);
                    break;
                case AbilityId.PriCRailshot:
                    fired = ActivateRailshot(magnitude);
                    break;
                default:
                    fired = false;
                    break;
            }

            if (!fired) return;

            BeginCooldown(def);
            Log.Event("Ability",
                $"{def.displayName} — {def.abilityName} (dial {dial}, mag {magnitude:0.##})");
        }

        void BeginCooldown(WeaponDef def)
        {
            _cooldown = def.abilityCooldown > 0f ? def.abilityCooldown : WeaponAbility.Cooldown(def.abilityId);
        }

        bool ActivateConcussive(WeaponDef def, float metres)
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            float range = def.range > 0.01f ? def.range : 14f;
            if (ClientOnly())
            {
                var verbs = LocalVerbs();
                if (verbs == null) return false;
                verbs.RequestConcussive(origin, forward, metres, range);
                return true;
            }

            float cosMin = Mathf.Cos(WeaponAbility.Br12ConeHalfAngleDegrees * Mathf.Deg2Rad);
            var hatch = HatchStation.Instance;
            Vector3 awayFromHatch = hatch != null
                ? (origin - hatch.transform.position)
                : -forward;
            awayFromHatch.y = 0f;
            if (awayFromHatch.sqrMagnitude < 0.01f) awayFromHatch = -forward;
            awayFromHatch.Normalize();

            int hit = 0;
            float credited = 0f;
            var live = LiveGatekeepers();
            for (int i = 0; i < live.Count; i++)
            {
                var b = live[i];
                if (b == null) continue;
                Vector3 to = b.transform.position - origin;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > range || dist < 0.05f) continue;
                if (Vector3.Dot(forward, to / dist) < cosMin) continue;

                credited += b.Shove(awayFromHatch * metres);
                hit++;
            }

            _waveGatekeeperSeconds += credited;
            Log.Event("Ability",
                $"concussive shove — {hit} gatekeeper(s), {credited:0.##} gatekeeper-seconds");
            return true;
        }

        /// <summary>Live gatekeepers via the spawner's ledger — the one list, reached through
        /// the bound controller so there is no process-global actor list. Empty when unbound
        /// or spawnerless (ability then finds no targets, same as an empty room).</summary>
        System.Collections.Generic.IReadOnlyList<EnemyGatekeeper> LiveGatekeepers()
        {
            var gc = _game;
            if (gc == null || gc.Spawner == null)
                return System.Array.Empty<EnemyGatekeeper>();
            return gc.Spawner.Live;
        }

        bool TryManualPin(float seconds)
        {
            if (!_pinBuild.Ready || _pinBuild.TargetKey == 0) return false;
            if (ClientOnly())
            {
                if (_pinTarget == null) return false;
                if (_pinTarget.GetInstanceID() != _pinBuild.TargetKey) return false;
                return ApplyPin(_pinTarget, seconds);
            }

            var live = LiveGatekeepers();
            for (int i = 0; i < live.Count; i++)
            {
                var b = live[i];
                if (b != null && b.GetInstanceID() == _pinBuild.TargetKey)
                    return ApplyPin(b, seconds);
            }
            return false;
        }

        bool ApplyPin(EnemyGatekeeper target, float seconds)
        {
            if (target == null || seconds <= 0f) return false;
            if (ClientOnly())
            {
                var netObj = target.GetComponent<NetworkObject>();
                if (netObj == null || !netObj.IsSpawned) return false;
                var verbs = LocalVerbs();
                if (verbs == null) return false;
                verbs.RequestPin(netObj.NetworkObjectId, seconds);
                ClearPinBuild();
                return true;
            }

            target.Pin(seconds);
            _waveGatekeeperSeconds += seconds;
            ClearPinBuild();
            Log.Event("Ability",
                $"pin held {seconds:0.##}s on {target.Kind} — {seconds:0.##} gatekeeper-seconds");
            return true;
        }

        bool ActivateRegenSurge(float durationSeconds)
        {
            if (_gun.ActiveSlot != WeaponSlot.Secondary)
            {
                Log.Info(Tag, "regen surge refused — Secondary not active");
                return false;
            }
            _surgeRemaining = durationSeconds;
            Log.Event("Ability",
                $"regen surge — {WeaponAbility.Mk1SurgeMultiplier:0.##}x for {durationSeconds:0.#}s");
            return true;
        }

        bool ActivateRailshot(float pierceBodies)
        {
            if (_gun == null || _gun.ActiveWeaponId != WeaponId.PriC)
            {
                Log.Info(Tag, "railshot refused — PRI-C not active");
                return false;
            }
            if (_gun.RailshotArmed)
            {
                Log.Info(Tag, "railshot refused — already armed");
                return false;
            }
            int bodies = Mathf.Max(1, Mathf.RoundToInt(pierceBodies));
            _gun.ArmRailshot(bodies, WeaponAbility.PriCRailshotArmedSeconds);
            Log.Event("Ability",
                $"railshot armed — pierce {bodies} at full damage for {WeaponAbility.PriCRailshotArmedSeconds:0.#}s");
            return true;
        }

        /// <summary>PlayerGun reports bodies damaged by a consumed Railshot.</summary>
        public void NotifyRailshotFired(int bodiesDamaged, int pierceArmed)
        {
            float credited = bodiesDamaged * WeaponAbility.PriCRailshotSecondsPerBody;
            _waveGatekeeperSeconds += credited;
            Log.Event("Ability",
                $"railshot fired — {bodiesDamaged} body(s) of {pierceArmed} armed" +
                (credited > 0f ? $", {credited:0.##} gatekeeper-seconds" : ""));
        }

        /// <summary>PlayerGun reports rounds the surge granted beyond baseline.</summary>
        public void CreditRegenRound(float rounds)
        {
            if (rounds <= 0f) return;
            _waveRegenRounds += rounds;
            _waveGatekeeperSeconds += rounds * WeaponAbility.Mk1GatekeeperSecondsPerSurgeRound;
        }

        void OnWaveStarted(WaveSpec _)
        {
            // Deep-start chains fire WaveStarted between waves without AssaultEnded — flush the
            // previous wave's tally first or criterion 6 loses every wave but the last.
            if (_waveTallyOpen)
            {
                int completed = _game != null && _game.Sim != null
                    ? _game.Sim.CurrentWaveIndex - 1
                    : 0;
                LogWaveGatekeeperSeconds(completed);
            }

            _waveTallyOpen = true;
            _waveGatekeeperSeconds = 0f;
            _waveRegenRounds = 0f;
            ClearPinBuild();
            if (_gun != null) _gun.ClearRailshot();
        }

        void OnAssaultEnded()
        {
            if (!_waveTallyOpen) return;
            int wave = _game != null && _game.Sim != null
                ? _game.Sim.CurrentWaveIndex
                : 0;
            LogWaveGatekeeperSeconds(wave);
            _waveTallyOpen = false;
            _waveGatekeeperSeconds = 0f;
            _waveRegenRounds = 0f;
        }

        void LogWaveGatekeeperSeconds(int wave)
        {
            Log.Event("Ability",
                $"wave {wave} gatekeeper-seconds removed: {_waveGatekeeperSeconds:0.##}" +
                (_waveRegenRounds > 0f ? $" (regen rounds {_waveRegenRounds:0.##})" : ""));
        }

        void ClearPinBuild()
        {
            _pinBuild.Clear();
            _pinTarget = null;
        }

        bool ClientOnly()
        {
            var gc = _game;
            var net = gc != null ? gc.Net : null;
            return net != null && net.IsClientOnly;
        }

        NetVerbs LocalVerbs()
        {
            var gc = _game;
            if (gc == null || gc.Net == null) return null;
            return gc.Net.LocalVerbs;
        }
    }
}
