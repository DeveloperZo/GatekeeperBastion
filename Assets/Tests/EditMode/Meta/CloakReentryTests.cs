using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>VEIL's re-entry (Zo, 2026-09-04). Cloak leaves the acquisition lists and firing
    /// collapses it, which left the sheet able to buy distance and never pressure. Kills now
    /// take seconds off Cloak's cooldown, so the way back into stealth is killing rather than
    /// waiting. Nothing here grants damage: raising Luck was the alternative and Luck is the
    /// game's only crit source, frozen at 0.</summary>
    public class CloakReentryTests
    {
        static BoarderAbilityState CoolingCloak()
        {
            var s = new BoarderAbilityState();
            float window = BoarderProtocol.Window(BoarderAbilityId.Cloak);
            Assert.IsTrue(s.TryActivate(window, BoarderProtocol.Cooldown(BoarderAbilityId.Cloak)));
            s.Tick(window);   // ride the window out; the cooldown is now running
            Assert.AreEqual(BoarderProtocol.Cooldown(BoarderAbilityId.Cloak),
                s.CooldownRemaining, 1e-4f);
            return s;
        }

        [Test]
        public void OnlyCloak_Refunds()
        {
            Assert.AreEqual(BoarderProtocol.CloakKillRefundSeconds,
                BoarderProtocol.KillRefundSeconds(BoarderAbilityId.Cloak));
            foreach (BoarderAbilityId id in System.Enum.GetValues(typeof(BoarderAbilityId)))
            {
                if (id == BoarderAbilityId.Cloak) continue;
                Assert.AreEqual(0f, BoarderProtocol.KillRefundSeconds(id),
                    id + " must not refund: nowhere else is re-entry the point of the ability");
            }
        }

        [Test]
        public void EachKill_TakesItsSecondsOffTheWait()
        {
            float refund = BoarderProtocol.CloakKillRefundSeconds;
            float cooldown = BoarderProtocol.Cooldown(BoarderAbilityId.Cloak);

            var s = CoolingCloak();
            Assert.IsTrue(s.RefundCooldown(refund));
            Assert.AreEqual(cooldown - refund, s.CooldownRemaining, 1e-4f);
            Assert.IsTrue(s.RefundCooldown(refund));
            Assert.AreEqual(cooldown - refund * 2f, s.CooldownRemaining, 1e-4f);
        }

        /// <summary>The sizing argument, kept as a test so the number cannot drift away from
        /// the claim: clearing a wave-1 roster is worth about half the wait.</summary>
        [Test]
        public void AWaveOneRoster_BuysAboutHalfTheWait()
        {
            float cooldown = BoarderProtocol.Cooldown(BoarderAbilityId.Cloak);
            float roster = WaveDirector.BaseEnemyCount * BoarderProtocol.CloakKillRefundSeconds;
            Assert.Less(roster, cooldown,
                "a single wave-1 roster must not hand back the whole cooldown");
            Assert.Greater(roster, cooldown * 0.4f,
                "and it must be worth enough to change how the wearer plays");
        }

        [Test]
        public void KillingEnough_ClearsTheWaitEntirely_ButNeverGoesNegative()
        {
            var s = CoolingCloak();
            for (int i = 0; i < 100; i++) s.RefundCooldown(BoarderProtocol.CloakKillRefundSeconds);
            Assert.AreEqual(0f, s.CooldownRemaining,
                "a cooldown below zero would read as ready carrying a debt");
            Assert.IsTrue(s.IsReady, "enough kills is a re-entry, which is the point");
        }

        [Test]
        public void ARefundOffCooldown_DoesNothing()
        {
            float refund = BoarderProtocol.CloakKillRefundSeconds;

            // Ready: there is no wait to discount.
            var ready = new BoarderAbilityState();
            Assert.IsTrue(ready.IsReady);
            Assert.IsFalse(ready.RefundCooldown(refund));
            Assert.AreEqual(0f, ready.CooldownRemaining);

            // Window open: the wait has not started, and banking credit against it would be a
            // stored resource rather than a discount.
            var open = new BoarderAbilityState();
            Assert.IsTrue(open.TryActivate(
                BoarderProtocol.Window(BoarderAbilityId.Cloak),
                BoarderProtocol.Cooldown(BoarderAbilityId.Cloak)));
            Assert.IsFalse(open.RefundCooldown(refund));
            open.Tick(BoarderProtocol.Window(BoarderAbilityId.Cloak));
            Assert.AreEqual(BoarderProtocol.Cooldown(BoarderAbilityId.Cloak),
                open.CooldownRemaining, 1e-4f,
                "the window's kills did not shorten the wait that followed it");
        }

        [Test]
        public void AWearerWhoKillsNothing_WaitsExactlyAsLongAsBefore()
        {
            var s = CoolingCloak();
            float cooldown = BoarderProtocol.Cooldown(BoarderAbilityId.Cloak);
            s.Tick(cooldown - 0.5f);
            Assert.AreEqual(0.5f, s.CooldownRemaining, 1e-4f);
            Assert.IsFalse(s.IsReady);
            s.Tick(0.5f);
            Assert.IsTrue(s.IsReady);
        }

        [Test]
        public void ANonPositiveRefund_IsRefused()
        {
            var s = CoolingCloak();
            float before = s.CooldownRemaining;
            Assert.IsFalse(s.RefundCooldown(0f));
            Assert.IsFalse(s.RefundCooldown(-5f), "a negative refund must not extend the wait");
            Assert.AreEqual(before, s.CooldownRemaining, 1e-4f);
        }
    }
}
