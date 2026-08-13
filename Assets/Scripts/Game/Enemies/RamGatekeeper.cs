using GatekeeperBastion.Sim;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: the gatekeeper that walks to the Hatch — except on field-terminal
    /// Waves, where it hunts the Boarder (dated break, 2026-08-13). Shooting still does not provoke it.</summary>
    /// <remarks>
    /// Two refusals, both hardened here rather than trusted from the pushed flags:
    /// <see cref="MayTargetPlayer"/> is false on Hatch-bank Waves, so aggro radius, line-of-sight
    /// and the whole `_chasingPlayer` branch of Retarget() are closed; <see cref="CanBeProvoked"/>
    /// is always false, so Provoke() returns immediately without touching speed, target or the
    /// shout counter.
    ///
    /// Crowd priority is INVERTED relative to the chaff: a Ram outranks every Default and Hunter
    /// (lower avoidancePriority wins in Unity), so a wall of bodies at the hatch parts for it
    /// instead of stalling it in the doorway it exists to reach.
    /// </remarks>
    public sealed class RamGatekeeper : EnemyGatekeeper
    {
        public override GatekeeperKind Kind => GatekeeperKind.Ram;

        /// <summary>True on field-terminal Waves only (2026-08-13). Hatch-bank keeps the old rule.</summary>
        protected override bool MayTargetPlayer =>
            _game != null && _game.Sim != null && _game.Sim.Mode == WaveMode.Terminal;

        /// <summary>Never — stated separately from <see cref="MayTargetPlayer"/> so that "shooting
        /// it does nothing" is a written rule and not a side effect of one.</summary>
        protected override bool CanBeProvoked => false;

        /// <summary>The one kind that keeps the expensive solver: its whole job is pushing through
        /// a crowd of its own escorts to reach the hatch, and a cheap solver is what makes an agent
        /// give up and orbit instead.</summary>
        protected override ObstacleAvoidanceType AvoidanceQuality =>
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        /// <summary>Outranks the chaff band (30..61) outright, still staggered inside its own band
        /// so two Rams at the same door do not deadlock each other symmetrically.</summary>
        protected override int AvoidancePriority => 4 + (GetInstanceID() & 0x0F);
    }
}
