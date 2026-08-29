using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Who a gatekeeper goes after, and how far it will divert to hit hardware. Both
    /// rules were changed on 2026-08-28 on Zo's report and neither had anything executing it —
    /// the surrounding class is a NavMesh MonoBehaviour, so the decisions were pulled out as
    /// statics (the idiom <see cref="BastionDoor.CountsAsOpener"/> already uses) rather than
    /// left where only a playthrough could reach them.</summary>
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

        // ================================================================
        //  How far it diverts to hit hardware
        // ================================================================

        [Test]
        public void MagnetHands_WidensTheTurretPull_ItDoesNotGrantIt()
        {
            // THE BUG: the passive used to be the ONLY way a turret could be targeted at all,
            // so without it the emplacement was literally invulnerable — 150 HP it could never
            // lose, standing all run. The 07:17 log had four turrets and the only two deaths
            // were the player's own Overcharge burning them out.
            const float melee = 3.5f;
            float without = EnemyGatekeeper.TurretPullRadius(false, melee);
            float with = EnemyGatekeeper.TurretPullRadius(true, melee);

            Assert.Greater(without, 0f,
                "a turret cannot be reached without the passive, which is invulnerability by "
                + "another name");
            Assert.AreEqual(melee, without, 1e-4f,
                "without the passive a machine should only be struck when it is already in "
                + "reach — in the way, so it gets hit");
            Assert.AreEqual(BoarderProtocol.TurretAuraRadius, with, 1e-4f,
                "the passive should pull bodies from anywhere in the turret's aura");
            Assert.Greater(with, without,
                "the passive buys no extra reach, so there is no reason to wear it");
        }
    }
}
