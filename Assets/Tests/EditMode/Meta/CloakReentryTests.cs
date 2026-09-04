using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>VEIL's re-entry (Zo, 2026-09-04). Cloak is the only window firing collapses,
    /// which left the sheet able to buy distance and nothing else. The window a shot does not
    /// spend now comes off the cooldown, so breaking early is a way back into stealth rather
    /// than only a shorter hide. No damage is granted anywhere: this moves one timer.</summary>
    public class CloakReentryTests
    {
        static BoarderAbilityState OpenCloak()
        {
            var s = new BoarderAbilityState();
            Assert.IsTrue(s.TryActivate(
                BoarderProtocol.Window(BoarderAbilityId.Cloak),
                BoarderProtocol.Cooldown(BoarderAbilityId.Cloak)));
            return s;
        }

        [Test]
        public void OnlyCloak_Refunds()
        {
            Assert.AreEqual(BoarderProtocol.CloakBreakRefundPerSecond,
                BoarderProtocol.BreakRefundPerSecond(BoarderAbilityId.Cloak));
            foreach (BoarderAbilityId id in System.Enum.GetValues(typeof(BoarderAbilityId)))
            {
                if (id == BoarderAbilityId.Cloak) continue;
                Assert.AreEqual(0f, BoarderProtocol.BreakRefundPerSecond(id),
                    id + " must not refund: unspent window is only a choice where firing "
                    + "collapses the window");
            }
        }

        [Test]
        public void BreakingInstantly_PaysTheMostBack_AndHidingFullPaysNothing()
        {
            float window = BoarderProtocol.Window(BoarderAbilityId.Cloak);
            float cooldown = BoarderProtocol.Cooldown(BoarderAbilityId.Cloak);
            float refund = BoarderProtocol.CloakBreakRefundPerSecond;

            var instant = OpenCloak();
            instant.EndWindowEarly(refund);
            Assert.AreEqual(cooldown - window * refund, instant.CooldownRemaining, 1e-4f,
                "an instant break refunds the whole window");

            var half = OpenCloak();
            half.Tick(window * 0.5f);
            half.EndWindowEarly(refund);
            Assert.AreEqual(cooldown - window * 0.5f * refund, half.CooldownRemaining, 1e-4f);

            // Riding the window to its end is not an early break at all, so nothing is owed.
            var full = OpenCloak();
            full.Tick(window);
            Assert.AreEqual(cooldown, full.CooldownRemaining, 1e-4f,
                "a window that expired on its own pays the full cooldown");
            full.EndWindowEarly(refund);
            Assert.AreEqual(cooldown, full.CooldownRemaining, 1e-4f,
                "EndWindowEarly on a shut window is a no-op, refund or not");
        }

        [Test]
        public void TheStrikeCycle_IsShorterThanTheHideCycle_ButBuysLessStealth()
        {
            float window = BoarderProtocol.Window(BoarderAbilityId.Cloak);
            float cooldown = BoarderProtocol.Cooldown(BoarderAbilityId.Cloak);
            float refund = BoarderProtocol.CloakBreakRefundPerSecond;

            float hideCycle = window + cooldown;
            var strike = OpenCloak();
            strike.EndWindowEarly(refund);
            float strikeCycle = strike.CooldownRemaining;

            Assert.Less(strikeCycle, hideCycle,
                "breaking early must actually re-enter sooner or the refund is decoration");
            Assert.Greater(strikeCycle, 0f,
                "it must still be a cooldown: a free re-entry is a toggle, not a window");
            Assert.Less(window * refund, cooldown,
                "the refund must not be able to erase the whole cooldown");
        }

        [Test]
        public void ARefundOfZero_IsTheOldBehaviourExactly()
        {
            var a = OpenCloak();
            a.Tick(1f);
            a.EndWindowEarly(0f);
            var b = OpenCloak();
            b.Tick(1f);
            b.EndWindowEarly();
            Assert.AreEqual(b.CooldownRemaining, a.CooldownRemaining, 1e-4f);
            Assert.AreEqual(BoarderProtocol.Cooldown(BoarderAbilityId.Cloak),
                a.CooldownRemaining, 1e-4f);
        }

        [Test]
        public void TheCooldownFloorsAtZero_NeverNegative()
        {
            var s = new BoarderAbilityState();
            Assert.IsTrue(s.TryActivate(10f, 5f));   // a refund larger than the cooldown
            s.EndWindowEarly(2f);
            Assert.AreEqual(0f, s.CooldownRemaining,
                "a cooldown below zero would read as ready carrying a debt");
            Assert.IsTrue(s.IsReady);
        }
    }
}
