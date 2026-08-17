using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The complete combat profile of one <see cref="GatekeeperKind"/>, produced by
    /// <see cref="WaveDirector.StatsFor"/> and pushed onto each spawned actor by the Game layer's
    /// WaveSpawner. Nothing else describes a gatekeeper: if a behaviour is not expressible here it
    /// belongs in the Game layer's state machine, not in a new field.
    ///
    /// THE TWO FLAGS ARE THE DESIGN, THE NUMBERS ARE ONLY TUNING. A kind is what it refuses to
    /// do; <see cref="ignoresHatch"/> and <see cref="ignoresPlayer"/> are those refusals, and the
    /// zeroed damage fields beside them are the same rule stated twice so it cannot be half-obeyed
    /// (a Hunter that somehow reached the hatch still cannot hurt it).
    /// </summary>
    [Serializable]
    public struct GatekeeperStats
    {
        /// <summary>Hit points BEFORE <see cref="WaveSpec.enemyHealthMultiplier"/> is applied.
        /// The Game layer spawns each actor at maxHp * that multiplier.</summary>
        public float maxHp;

        /// <summary>Damage dealt to the player per swing. 0 = this kind NEVER damages the player.</summary>
        public float playerDamage;

        /// <summary>Damage dealt to the hatch per swing. 0 = this kind NEVER damages the hatch.</summary>
        public float hatchDamage;

        /// <summary>Seconds between swings. All three kinds share one swing clock: kinds differ by
        /// what they hit and how hard, never by cadence.</summary>
        public float attackInterval;

        /// <summary>Move speed while walking its default errand (Default/Ram to the hatch).</summary>
        public float patrolSpeed;

        /// <summary>Move speed while provoked or hunting. For Hunter and Ram this equals
        /// <see cref="patrolSpeed"/> — neither has a second gear, because neither changes its mind.</summary>
        public float chaseSpeed;

        /// <summary>Hunter: never targets the hatch, at any range, including inside the 8 m commit
        /// radius that pulls a Default gatekeeper back to it.</summary>
        public bool ignoresHatch;

        /// <summary>Ram: never targets the player AND is never provoked by being shot — the one
        /// enemy in the game that ignores the provoke-on-damage rule.</summary>
        public bool ignoresPlayer;
    }
}
