using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Who a gatekeeper goes after. Pulled out as statics because the surrounding
    /// class is a NavMesh MonoBehaviour, and these decisions have to run in EditMode.</summary>
    public class GatekeeperTargetingTests
    {
        // ================================================================
        //  Who it hunts
        // ================================================================

        [Test]
        public void RemovingTheHatch_ForcesEveryBodyOntoThePlayer_WhichIsTheTrap()
        {
            // THE BUG, stated as the arithmetic that caused it. hatchAvailable is a TERM in the
            // chase expression, not merely one fewer option — so a field wave that took the
            // Hatch out of targeting turned `!hatchAvailable` into an unconditional true and
            // sent all 66 wave-5 bodies at one player, for life, from anywhere on the spine.
            // Six deaths in the 09:24 run; zero in the run after the fix.
            Assert.IsTrue(
                EnemyGatekeeper.ChasesPlayer(mayTargetPlayer: true, committedToHatch: false,
                    provoked: false, playerNear: false, hatchAvailable: false),
                "with no Hatch to march on, a body must fall back to hunting the player — if "
                + "this is false the enemy has nothing to do at all");

            // ...and the fix: with the Hatch in the fight, an unprovoked body at distance has
            // somewhere else to be.
            Assert.IsFalse(
                EnemyGatekeeper.ChasesPlayer(mayTargetPlayer: true, committedToHatch: false,
                    provoked: false, playerNear: false, hatchAvailable: true),
                "a body with a live Hatch to march on still hunts the player unprovoked and at "
                + "range. That is the 2026-08-28 bug: every wave becomes 66 bodies on one "
                + "player with no other job in the world.");
        }

        [Test]
        public void BeingShot_OrStandingTooClose_StillPullsABodyOffTheHatch()
        {
            // The Hatch having a job must not make the player safe. Both provocations survive.
            Assert.IsTrue(
                EnemyGatekeeper.ChasesPlayer(true, false, provoked: true, playerNear: false,
                    hatchAvailable: true),
                "shooting a gatekeeper no longer makes it yours — the player could dismantle a "
                + "marching column with no reply");
            Assert.IsTrue(
                EnemyGatekeeper.ChasesPlayer(true, false, provoked: false, playerNear: true,
                    hatchAvailable: true),
                "standing inside a gatekeeper's aggro radius no longer provokes it");
        }

        [Test]
        public void AKindThatRefusesPlayers_AndABodyCommittedToTheHatch_NeverHunt()
        {
            // The Thief's refusal, and the hatch-commit latch. Asserted with every provocation
            // switched on at once, because these are refusals and a refusal that only holds in
            // the quiet case is not a refusal.
            Assert.IsFalse(
                EnemyGatekeeper.ChasesPlayer(mayTargetPlayer: false, committedToHatch: false,
                    provoked: true, playerNear: true, hatchAvailable: false),
                "a kind that may never target players was provoked into it anyway - shooting a "
                + "Thief must change nothing about it");
            Assert.IsFalse(
                EnemyGatekeeper.ChasesPlayer(mayTargetPlayer: true, committedToHatch: true,
                    provoked: true, playerNear: true, hatchAvailable: true),
                "a body already wrecking the Hatch was pulled off it. HOME COMES FIRST: the "
                + "commit latch exists so a crowd at the door cannot be peeled away one shot "
                + "at a time.");
        }

    }
}
