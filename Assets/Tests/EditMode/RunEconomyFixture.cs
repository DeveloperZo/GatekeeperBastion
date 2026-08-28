using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// SHARED SEAM FOR THE POST-2026-08-25 ECONOMY. Kills bank as they land (Zo), so the
    /// carried pile no longer fills on its own — a fixture that wants Unbanked has to
    /// Withdraw it back out of the bank at the Hatch, exactly as the player now does.
    ///
    /// Every fixture that used to write "kill eight things, then start a Convert window"
    /// goes through <see cref="Stake"/> instead. It is deliberately the REAL verb
    /// (<see cref="RunSim.Withdraw(float)"/>) rather than a back door onto the wallets:
    /// a test that reaches past the withdraw gate would keep passing after that gate broke,
    /// and the gate is now the only way a pile exists at all.
    /// </summary>
    public static class RunEconomyFixture
    {
        /// <summary>Seconds of hold to simulate per step. Small enough that the accrual
        /// remainder never overshoots the ask by a meaningful amount.</summary>
        const float WithdrawStepSeconds = 0.25f;

        /// <summary>Safety valve: a withdraw that cannot reach the ask (empty bank, damaged
        /// Hatch, wrong phase) must fail the test loudly rather than spin.</summary>
        const int MaxWithdrawSteps = 4000;

        /// <summary>Move <paramref name="want"/> salvage out of the bank and into the
        /// operating seat's carried pile, by holding the real Withdraw verb. Returns what
        /// actually moved. Asserts rather than looping forever when the verb refuses.</summary>
        public static long Stake(RunSim sim, long want)
        {
            Assert.IsNotNull(sim, "Stake needs a Run.");
            if (want <= 0L) return 0L;

            long start = sim.TotalUnbanked;
            long target = start + want;
            int steps = 0;
            while (sim.TotalUnbanked < target)
            {
                if (sim.Withdraw(WithdrawStepSeconds) <= 0L)
                {
                    Assert.Fail("Withdraw refused with " + (target - sim.TotalUnbanked)
                        + " of " + want + " still to move — bank=" + sim.BankedSalvage
                        + ", phase=" + sim.Phase + ", assault=" + sim.Assault
                        + ", hatch=" + sim.Ship.Hatch.Hp + "/" + sim.Ship.Hatch.MaxHp
                        + ". The Withdraw gate is Prep + a full Hatch + a funded bank.");
                }
                if (++steps > MaxWithdrawSteps)
                {
                    Assert.Fail("Withdraw never reached " + want
                        + " — moved " + (sim.TotalUnbanked - start) + " in "
                        + MaxWithdrawSteps + " steps.");
                }
            }
            return sim.TotalUnbanked - start;
        }

        /// <summary>Kill <paramref name="kills"/> bodies and immediately stake everything they
        /// paid. The one-line replacement for the old "kills leave a pile behind" assumption:
        /// the Run ends in the same state it used to, by the route the player now takes.</summary>
        public static long KillAndStake(RunSim sim, int kills)
        {
            long before = sim.BankedSalvage;
            for (int i = 0; i < kills; i++) sim.AwardKill();
            long earned = sim.BankedSalvage - before;
            Assert.Greater(earned, 0L,
                kills + " kills banked nothing — the fixture has no stake to move.");
            return earned;
        }
    }
}
