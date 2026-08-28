using GatekeepersProtocol.Sim;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: the gatekeeper that walks to the Hatch, on every Wave mode.
    /// Shooting does not provoke it.</summary>
    /// <remarks>
    /// Two refusals, both hardened here rather than trusted from the pushed flags:
    /// <see cref="MayTargetPlayer"/> is false on Hatch-bank Waves, so aggro radius, line-of-sight
    /// and the whole `_chasingPlayer` branch of Retarget() are closed; <see cref="CanBeProvoked"/>
    /// is always false, so Provoke() returns immediately without touching speed, target or the
    /// shout counter.
    ///
    /// Crowd priority is INVERTED relative to the chaff: a Thief outranks every Default and Hunter
    /// (lower avoidancePriority wins in Unity), so a wall of bodies at the hatch parts for it
    /// instead of stalling it in the doorway it exists to reach.
    /// </remarks>
    public sealed class ThiefGatekeeper : EnemyGatekeeper
    {
        public override GatekeeperKind Kind => GatekeeperKind.Thief;

        /// <summary>Never. THE 2026-08-13 BREAK IS REVERTED (Zo, 2026-08-27): this returned true
        /// on field-terminal Waves so a Thief had something to do while the Hatch was out of the
        /// fight — but a Thief's playerDamage is 0, so "hunting" the player meant walking at them
        /// harmlessly. It could not hurt the player and could not hurt the Hatch, which made it
        /// pure ballast: 12 of 13 survived a logged run, each soaking a full magazine at Wave 5
        /// for nothing. The Hatch is damageable on both modes now, so the original rule stands and
        /// the Thief has its job back.</summary>
        protected override bool MayTargetPlayer => false;

        /// <summary>Never — stated separately from <see cref="MayTargetPlayer"/> so that "shooting
        /// it does nothing" is a written rule and not a side effect of one.</summary>
        protected override bool CanBeProvoked => false;

        /// <summary>The one kind that keeps the expensive solver: its whole job is pushing through
        /// a crowd of its own escorts to reach the hatch, and a cheap solver is what makes an agent
        /// give up and orbit instead.</summary>
        protected override ObstacleAvoidanceType AvoidanceQuality =>
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        /// <summary>Outranks the chaff band (30..61) outright, still staggered inside its own band
        /// so two Thieves at the same door do not deadlock each other symmetrically.</summary>
        protected override int AvoidancePriority => 4 + (GetInstanceID() & 0x0F);
    }
}
