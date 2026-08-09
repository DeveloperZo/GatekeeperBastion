using GatekeeperBastion.Sim;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: the gatekeeper that NEVER targets the player and is NEVER provoked. It
    /// walks to the hatch and hits it hard; shooting it changes nothing — not its target, not its
    /// speed, and it does not shout for help. You have to kill it, and it is slow and tanky
    /// (WaveDirector.StatsFor pushes its hp and speeds), so every second spent on a Ram is a
    /// second the Hunters escorting it spend on you. That trade is the point of the kind.</summary>
    /// <remarks>
    /// Two refusals, both hardened here rather than trusted from the pushed flags:
    /// <see cref="MayTargetPlayer"/> is false, so aggro radius, line-of-sight and the whole
    /// `_chasingPlayer` branch of Retarget() are closed; <see cref="CanBeProvoked"/> is false, so
    /// Provoke() — the single gate that OnDamaged and a neighbour's shout both go through —
    /// returns immediately without touching speed, target or the shout counter.
    ///
    /// Crowd priority is INVERTED relative to the chaff: a Ram outranks every Default and Hunter
    /// (lower avoidancePriority wins in Unity), so a wall of bodies at the hatch parts for it
    /// instead of stalling it in the doorway it exists to reach.
    /// </remarks>
    public sealed class RamGatekeeper : EnemyGatekeeper
    {
        public override GatekeeperKind Kind => GatekeeperKind.Ram;

        /// <summary>Never. The player is scenery to a Ram.</summary>
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
        /// so two Rams at the same door do not deadlock each other symmetrically.</summary>
        protected override int AvoidancePriority => 4 + (GetInstanceID() & 0x0F);
    }
}
