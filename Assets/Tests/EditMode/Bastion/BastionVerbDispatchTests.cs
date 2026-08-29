using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Which verbs the Bastion will dispatch in which assault state.
    ///
    /// <para>This fixture exists because of a bug that nothing could have caught: on
    /// 2026-08-28, T at a zone terminal did nothing during a live wave. The RULE was fine —
    /// <see cref="FieldShutoff.MayRun"/> takes no AssaultState on purpose, because the shutoff
    /// is legal in both. What was broken was a tick gate one layer up, <c>if (Assault != Prep)
    /// return;</c>, sitting above the handler that carries the verb. The verb existed, was
    /// correct, and was unreachable, and the suite was green throughout.</para>
    ///
    /// <para>The dispatch table is a function now (<see cref="BastionVerbs.LiveIn"/>), so the
    /// reachability question has an answer a test can read.</para></summary>
    public class BastionVerbDispatchTests
    {
        [Test]
        public void TheShutoff_IsLiveDuringAWave_BecauseADrippingZoneNeverLeavesAssault()
        {
            // THE REGRESSION, stated directly. A field wave holds itself open with the drip
            // until its terminal is shut off, so it never returns to Prep on its own: if the
            // shutoff is not dispatchable in Assault, that wave cannot be ended at all. The
            // 07:17 run authored 39 bodies in wave 5, killed 93, and had to be abandoned.
            Assert.IsTrue(BastionVerbs.IsLive(BastionVerb.Shutoff, AssaultState.Assault),
                "the terminal shutoff is not dispatchable mid-wave. A dripping zone never "
                + "returns to Prep, so this is the only state its off switch is reachable in.");

            // And in Prep too: a player who killed the field empty and walked up arrives there,
            // and the walk is still the objective.
            Assert.IsTrue(BastionVerbs.IsLive(BastionVerb.Shutoff, AssaultState.Prep),
                "the shutoff is refused in Prep, so killing the field empty and walking up "
                + "leaves the zone permanently unfinished");
        }

        [Test]
        public void TheStartLine_AndTheBankVerbs_AreBetweenWavesOnly()
        {
            // The other half of the same table: mid-wave the Hatch is not a start line and not
            // a bank. A second draw on top of a live wave, or a Convert while bodies are on the
            // floor, are the failures this half prevents.
            Assert.IsFalse(BastionVerbs.IsLive(BastionVerb.WaveStart, AssaultState.Assault),
                "a wave can be started while one is already running");
            Assert.IsFalse(BastionVerbs.IsLive(BastionVerb.Convert, AssaultState.Assault),
                "a Convert can be staked mid-wave");
            Assert.IsFalse(BastionVerbs.IsLive(BastionVerb.Withdraw, AssaultState.Assault),
                "salvage can be withdrawn mid-wave");
            Assert.IsFalse(BastionVerbs.IsLive(BastionVerb.Retrigger, AssaultState.Assault),
                "a zone can be retriggered mid-wave");

            Assert.IsTrue(BastionVerbs.IsLive(BastionVerb.WaveStart, AssaultState.Prep));
            Assert.IsTrue(BastionVerbs.IsLive(BastionVerb.Convert, AssaultState.Prep));
            Assert.IsTrue(BastionVerbs.IsLive(BastionVerb.Withdraw, AssaultState.Prep));
        }

        [Test]
        public void NoVerbIsLive_OutsideAssaultAndPrep()
        {
            // Every other assault state is a state the player is not playing in. Enumerated
            // rather than spot-checked, so a state added later defaults to refusing rather
            // than to silently inheriting a verb set nobody chose for it.
            foreach (AssaultState state in System.Enum.GetValues(typeof(AssaultState)))
            {
                if (state == AssaultState.Assault || state == AssaultState.Prep) continue;
                Assert.AreEqual(BastionVerb.None, BastionVerbs.LiveIn(state),
                    "verbs are dispatchable in " + state + ", which is not a state the player "
                    + "is acting in. A new assault state must refuse by default.");
            }
        }

        [Test]
        public void IsLive_AnswersFalseForTheEmptySet()
        {
            // Guard on the helper itself: a flags test written as `(set & verb) == verb` says
            // true for None against anything, which would make every assertion above vacuous.
            Assert.IsFalse(BastionVerbs.IsLive(BastionVerb.None, AssaultState.Prep),
                "IsLive(None) is true, so every other assertion in this fixture proves nothing");
        }
    }
}
