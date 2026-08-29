using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The player-facing verbs the Bastion dispatches, as a set.</summary>
    [Flags]
    public enum BastionVerb
    {
        None = 0,

        /// <summary>T at a zone terminal: ENDS the wave and completes the zone.</summary>
        Shutoff = 1 << 0,

        /// <summary>T at the Hatch: the loop's start line.</summary>
        WaveStart = 1 << 1,

        /// <summary>Staking a carried pile at the Hatch.</summary>
        Convert = 1 << 2,

        /// <summary>Pulling banked salvage back out into the carry.</summary>
        Withdraw = 1 << 3,

        /// <summary>Re-running a completed zone from its terminal.</summary>
        Retrigger = 1 << 4,

        /// <summary>The dev-gated faucet graybox, which has verbs of its own in both states.
        /// </summary>
        Faucet = 1 << 5,
    }

    /// <summary>WHICH VERBS ARE LIVE IN WHICH ASSAULT STATE — one function, so "the shutoff
    /// works during a wave" is a fact something can assert rather than a property of the order
    /// of <c>if</c> statements in a MonoBehaviour.
    ///
    /// <para>WHY THIS EXISTS. On 2026-08-28, T at a zone terminal did nothing during a live
    /// wave. Nothing was wrong with the rule — <see cref="FieldShutoff.MayRun"/> deliberately
    /// takes no AssaultState and its remarks say the shutoff is legal in Assault AND Prep.
    /// What was wrong was a tick gate one layer up: <c>if (Assault != Prep) return;</c> sat
    /// ABOVE the handler carrying the shutoff, whose own comment claimed both states. The verb
    /// existed, was correct, and was unreachable — and zone 1 hid it, because killing the field
    /// empty drops the sim to Prep, so the shutoff was always pressed from Prep there. Zone 2
    /// drips by design, never leaves Assault, and wave 5 became inescapable.</para>
    ///
    /// <para>A dispatcher that consults this cannot make that mistake again without the
    /// mistake being visible here, where it is one assertion.</para></summary>
    public static class BastionVerbs
    {
        /// <summary>The verbs a tick may dispatch in <paramref name="assault"/>.</summary>
        public static BastionVerb LiveIn(AssaultState assault)
        {
            switch (assault)
            {
                // MID-WAVE. The shutoff is here because a wave that keeps a body on the field
                // never returns to Prep — for a dripping zone this is the ONLY state its off
                // switch is ever reachable in, so removing it removes the ability to end the
                // wave at all.
                case AssaultState.Assault:
                    return BastionVerb.Shutoff | BastionVerb.Faucet;

                // BETWEEN WAVES. Everything, including the shutoff: a player who killed the
                // field empty and then walked up arrives in Prep, and the walk is still the
                // objective.
                case AssaultState.Prep:
                    return BastionVerb.Shutoff | BastionVerb.WaveStart | BastionVerb.Convert
                         | BastionVerb.Withdraw | BastionVerb.Retrigger | BastionVerb.Faucet;

                default:
                    return BastionVerb.None;
            }
        }

        /// <summary>Is <paramref name="verb"/> dispatchable in <paramref name="assault"/>?</summary>
        public static bool IsLive(BastionVerb verb, AssaultState assault)
        {
            return (LiveIn(assault) & verb) == verb && verb != BastionVerb.None;
        }
    }
}
