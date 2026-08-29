using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The Hatch offers five verbs and they are not equal. This fixture is the ORDER.
    ///
    /// <para>Docs/Design/0-Gameplay.md has said "the Hatch's start prompt outranks Convert and
    /// Withdraw" since 2026-08-26, and nothing could check it: the ordering was whatever the
    /// <c>if</c> statements in RepairInteractable.Prompt happened to be in. On 2026-08-28 a Dock
    /// offer was added above the start branch and the Hatch stopped mentioning the start line
    /// the moment zone 1 closed — the loop's start line, invisible, with a green suite. The rule
    /// moved into <see cref="HatchVerbs"/> so that it is a function, and a function can be
    /// asserted.</para></summary>
    public class HatchVerbOrderTests
    {
        /// <summary>A healthy Hatch mid-run with nothing carried and nothing banked — the
        /// baseline every case below varies exactly one thing from.</summary>
        static HatchVerb Choose(bool dockOffered = false, bool dockHold = false,
            bool offersWaveStart = false, bool prep = true,
            long unbanked = 0L, long bank = 0L,
            bool runIsOver = false, bool destroyed = false, bool hpFull = true)
        {
            return HatchVerbs.Choose(runIsOver, destroyed, hpFull, dockOffered, dockHold,
                offersWaveStart, prep, unbanked, bank);
        }

        [Test]
        public void TheStartLine_OutranksConvert_Withdraw_AndTheDock()
        {
            // THE REGRESSION, as a test. Each of these has every competing verb switched on at
            // once, so nothing passes by the competitor merely being absent.
            Assert.AreEqual(HatchVerb.StartWave,
                Choose(offersWaveStart: true, unbanked: 500L, bank: 9000L, dockOffered: true),
                "something outranked the start line. A wave BEGINS at the Hatch; if the Hatch "
                + "will not say so, the loop has no visible entry point.");

            // And each competitor individually, so a future reorder cannot pass by accident.
            Assert.AreEqual(HatchVerb.StartWave, Choose(offersWaveStart: true, dockOffered: true),
                "the Dock offer swallowed the start line - this is the 2026-08-28 bug exactly");
            Assert.AreEqual(HatchVerb.StartWave, Choose(offersWaveStart: true, unbanked: 500L),
                "Convert outranked the start line");
            Assert.AreEqual(HatchVerb.StartWave, Choose(offersWaveStart: true, bank: 9000L),
                "Withdraw outranked the start line");
        }

        [Test]
        public void TheDock_IsOffered_OnlyWhenThereIsNothingToPressOnWith()
        {
            // The way home must be reachable — its absence was the other half of the same
            // session's bugs — but it is the headline only when no zone is left to start.
            Assert.AreEqual(HatchVerb.Dock, Choose(dockOffered: true),
                "closing a zone left the player with no way home");
            Assert.AreEqual(HatchVerb.StartWave, Choose(dockOffered: true, offersWaveStart: true),
                "the dock took the headline while a zone was still waiting to be started");
            Assert.AreEqual(HatchVerb.None, Choose(dockOffered: false),
                "the Hatch offered the dock when the run had nowhere to go");
        }

        [Test]
        public void BothVerbsAreNamed_WhenBothAreLive()
        {
            // Naming only one of them is what hid the dock before it was fixed, and then hid
            // the start line after. Between zones the player has two real choices and the
            // prompt has to say both.
            Assert.IsTrue(HatchVerbs.ShowsDockAlongside(HatchVerb.StartWave, true),
                "the way home goes unmentioned while a zone is startable - the player cannot "
                + "learn they may leave");
            Assert.IsFalse(HatchVerbs.ShowsDockAlongside(HatchVerb.StartWave, false),
                "a dock that is not offered was advertised anyway");
            Assert.IsFalse(HatchVerbs.ShowsDockAlongside(HatchVerb.Convert, true),
                "the dock was appended to a verb that is not the start line");
        }

        [Test]
        public void AHoldInProgress_OutranksEverything_IncludingTheStartLine()
        {
            // The one exception, and only because it is already happening: interrupting a hold
            // to advertise a different verb is how a hold gets abandoned by accident.
            Assert.AreEqual(HatchVerb.Docking,
                Choose(dockOffered: true, dockHold: true, offersWaveStart: true),
                "a dock already being held was interrupted by another prompt");
            Assert.AreEqual(HatchVerb.StartWave,
                Choose(dockOffered: false, dockHold: true, offersWaveStart: true),
                "a stale hold flag hijacked the prompt with no dock on offer");
        }

        [Test]
        public void DamageAndDeath_OutrankEveryOffer_ButNotAHoldAlreadyRunning()
        {
            Assert.AreEqual(HatchVerb.Destroyed,
                Choose(destroyed: true, offersWaveStart: true, dockOffered: true),
                "a destroyed Hatch still advertised verbs it cannot perform");
            Assert.AreEqual(HatchVerb.Repair,
                Choose(hpFull: false, offersWaveStart: true, unbanked: 500L),
                "a damaged Hatch hid the repair behind another verb");
            Assert.AreEqual(HatchVerb.None,
                Choose(runIsOver: true, offersWaveStart: true, dockOffered: true),
                "the Hatch kept offering verbs after the run ended");
        }

        [Test]
        public void ConvertOutranksWithdraw_AndBothNeedPrep()
        {
            Assert.AreEqual(HatchVerb.Convert, Choose(unbanked: 500L, bank: 9000L),
                "Withdraw outranked Convert - a staked pile must be resolvable first");
            Assert.AreEqual(HatchVerb.Withdraw, Choose(unbanked: 0L, bank: 9000L));

            // Mid-wave the Hatch is not a bank.
            Assert.AreEqual(HatchVerb.None, Choose(prep: false, unbanked: 500L, bank: 9000L),
                "the Hatch offered a bank verb while a wave was running");
        }
    }
}
