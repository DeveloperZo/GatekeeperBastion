using UnityEngine;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Passive player health regeneration: the player always climbs back to FULL,
    /// slowly, and killing gatekeepers makes the climb faster (Zo, 2026-08-25 — "full health
    /// amount regen over time slowly that increases with enemies killed").</summary>
    /// <remarks>
    /// WHY THIS EXISTS. Session log Logs/GatekeepersProtocol_2026-08-25_22-35-26_p17676.log:
    /// the player cleared Wave 1 of a two-wave composite at 22/100 HP. A composite has no Prep
    /// gap between its waves, so Wave 2 drew 0.0 s later and they were dead 4.1 s in. That death
    /// was STRUCTURAL, not a difficulty result — at 22 HP there was no route back to fighting
    /// strength at any skill level, because nothing in the game restored health during a run.
    /// This rule is that route, and it is kill-driven so the route runs THROUGH the fight
    /// rather than around it: hiding heals, but strictly slower than fighting.
    ///
    /// SHAPE: a decaying "recent kills" boost, not a flat per-run kill count. A flat run total
    /// would make the rate a function of how far into the run you are — wave 1 unhealable and
    /// wave 6 self-sustaining — and would keep paying a player who has stopped fighting, which
    /// is exactly the behaviour Zo's answer is aimed against. A decaying window pays the player
    /// who is killing NOW, is bounded without a special case, and costs one float of state.
    ///
    /// COMPOSITION WITH Mk1RegenSurge: none, deliberately. Despite the shared word, the MK-1
    /// surge regenerates SECONDARY CLIP ROUNDS (see WeaponAbility.SecondaryRegenPerSecond and
    /// PlayerGun's regen path); it has never touched hit points. Multiplying health regen by
    /// PlayerAbility.RegenSurgeMultiplier would silently turn an ammo ability into a heal, and
    /// would double-charge one cooldown for two resources. These two regens are separate
    /// resources on separate clocks and must stay that way.
    ///
    /// ARITHMETIC (player 100 HP; a Default gatekeeper swings 6 damage / 0.7 s = 8.57 dps).
    /// Boost decays with a 6 s half-life, so lambda = ln2/6 = 0.1155 /s. Killing at a steady
    /// k kills/second gives a TIME-AVERAGE boost of RegenPerKill * k / lambda
    ///   = 0.5 * k * 6 / ln2 = 4.33 * k HP/s (before the cap).
    /// Recovering the log's 22 HP player, i.e. 78 HP:
    ///   hiding (no kills)      0.80 HP/s          -> 97.5 s
    ///   a kill every 3.0 s     0.80 + 1.44 = 2.24 -> 34.8 s   (2.8x faster than hiding)
    ///   a kill every 1.5 s     0.80 + 2.89 = 3.69 -> 21.1 s   (4.6x faster)
    ///   a kill every 1.0 s     0.80 + 4.00 = 4.80 -> 16.3 s   (6.0x, boost capped)
    /// Wave 2 of that composite is 30 bodies, so even the middle cadence buys a full recovery
    /// inside about a third of the wave. Hiding still reaches full — Zo asked for full — but it
    /// costs a minute and a half, during which the Hatch is taking hits.
    ///
    /// CAP: 4.0 HP/s of boost, so the ceiling rate is 4.8 HP/s = 56% of ONE Default's dps.
    /// That is the point of the cap: no amount of killing turns regen into a tank build. Even
    /// at maximum boost, standing in melee with a single gatekeeper still loses health.
    ///
    /// DAMAGE DELAY: 1 s, matching the sprint-stamina idiom in FirstPersonController
    /// (StaminaRegenDelaySeconds). Gatekeepers swing every 0.7 s, so a body actually on you
    /// holds regen at zero for as long as it stays on you — regen is the route back from a
    /// fight, never a way to win one standing still. The boost keeps decaying on wall clock
    /// during the delay, so a boost cannot be banked by tanking.
    /// </remarks>
    public struct PlayerRegen
    {
        /// <summary>Floor rate with no recent kills, HP/second. 100 HP from empty = 125 s.</summary>
        public const float BaseRegenPerSecond = 0.8f;

        /// <summary>HP/second added to the boost by one gatekeeper kill.</summary>
        public const float RegenPerKill = 0.5f;

        /// <summary>Ceiling on the kill boost, HP/second. Total rate tops out at
        /// BaseRegenPerSecond + this = 4.8 HP/s, which is 56% of one Default's 8.57 dps.</summary>
        public const float MaxKillBoostPerSecond = 4f;

        /// <summary>Seconds for a standing boost to fall to half. Sets the whole shape:
        /// average boost = RegenPerKill * kills-per-second * HalfLife / ln2.</summary>
        public const float KillBoostHalfLifeSeconds = 6f;

        /// <summary>Seconds after taking damage before regen resumes. Matches the sprint pool's
        /// FirstPersonController.StaminaRegenDelaySeconds.</summary>
        public const float DamageDelaySeconds = 1f;

        float _boost;  // HP/s of kill boost currently standing
        float _delay;  // seconds left before regen resumes

        /// <summary>Kill boost currently standing, HP/second. 0 when nothing died recently.</summary>
        public float KillBoostPerSecond => _boost;

        /// <summary>Seconds left on the post-damage hold. 0 when regen is free to run.</summary>
        public float DelayRemaining => _delay;

        /// <summary>Rate regen would apply at right now if it were not held or capped out,
        /// HP/second. HUD-facing and test-facing; Tick does not read it.</summary>
        public float RatePerSecond => BaseRegenPerSecond + _boost;

        /// <summary>One gatekeeper died. Adds to the boost, capped.</summary>
        public void NotifyKill()
        {
            _boost = Mathf.Min(MaxKillBoostPerSecond, _boost + RegenPerKill);
        }

        /// <summary>The player took a hit: hold regen for DamageDelaySeconds. Does not touch
        /// the boost — see the tanking note above.</summary>
        public void NotifyDamaged()
        {
            _delay = DamageDelaySeconds;
        }

        /// <summary>Back to the cold state (run start, respawn, dev heal).</summary>
        public void Reset()
        {
            _boost = 0f;
            _delay = 0f;
        }

        /// <summary>Advance one frame and return the new HP. Caller injects dt — this type
        /// never reads a clock. Returns <paramref name="hp"/> unchanged when dead, when dt is
        /// not positive, or when already at max.</summary>
        /// <remarks>Death FREEZES the rule rather than decaying through it: death is a run
        /// ender here, and freezing means no arrangement of ticks can regenerate a corpse.</remarks>
        public float Tick(float dt, float hp, float maxHp, bool isDead)
        {
            if (isDead || dt <= 0f || maxHp <= 0f) return hp;

            // Boost decays on wall clock whatever else is true: exact half-life, one Pow, no
            // per-kill bookkeeping and therefore no allocation on the frame path.
            if (_boost > 0f)
            {
                _boost *= Mathf.Pow(0.5f, dt / KillBoostHalfLifeSeconds);
                if (_boost < 0.001f) _boost = 0f; // settle to exactly cold
            }

            // Serve out the delay first, then refill — the same two-branch shape as
            // FirstPersonController.TickSprint, so the frame the hold expires is the last held
            // frame rather than a partly-credited one.
            if (_delay > 0f)
            {
                _delay -= dt;
                if (_delay < 0f) _delay = 0f;
                return hp;
            }

            if (hp >= maxHp) return maxHp; // full: clamp, never overshoot
            return Mathf.Min(maxHp, hp + (BaseRegenPerSecond + _boost) * dt);
        }
    }
}
