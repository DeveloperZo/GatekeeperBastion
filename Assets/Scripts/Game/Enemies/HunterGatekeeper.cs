using GatekeeperBastion.Sim;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: the gatekeeper that NEVER targets the hatch. It beelines the nearest live
    /// player from the breach and keeps doing so for life — there is no hatch in its world, so no
    /// distance to home can ever pull it off you. Fast and fragile (WaveDirector.StatsFor pushes
    /// its hp and speeds); the answer to a Hunter is to shoot it, not to stand on the hatch.</summary>
    /// <remarks>
    /// Everything here is a refusal or an identity. The AI is EnemyGatekeeper's, unchanged:
    /// <see cref="MayTargetHatch"/> returning false closes every hatch branch in Retarget(), which
    /// is what makes `!hatchAvailable` hold from the first tick and the hatch-commit latch
    /// unreachable — including while a Hunter is standing on top of the hatch, which is exactly
    /// the case the flag alone had to be trusted for and now cannot be mis-pushed.
    ///
    /// A Hunter is NOT self-provoked at spawn on purpose: its patrol speed already equals its
    /// chase speed, so it arrives at full tilt anyway, and leaving `_provoked` clear means being
    /// shot still lets it SHOUT and pull its group. A pre-provoked Hunter would silently swallow
    /// that, and the group shout is the reason groups spawn together.
    /// </remarks>
    public sealed class HunterGatekeeper : EnemyGatekeeper
    {
        public override GatekeeperKind Kind => GatekeeperKind.Hunter;

        /// <summary>Never, at any distance, for any reason.</summary>
        protected override bool MayTargetHatch => false;

        /// <summary>Chaff: the cheapest solver Unity has. A Hunter is running a straight line at
        /// one moving target and does not need to negotiate a scrum to do its job.</summary>
        protected override ObstacleAvoidanceType AvoidanceQuality =>
            ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    }
}
