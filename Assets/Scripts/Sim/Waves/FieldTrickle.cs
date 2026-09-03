namespace GatekeepersProtocol.Sim
{
    /// <summary>What the spent-roster check should do this tick: nothing yet, hand the wave
    /// to the drip, or declare it cleared. One value so the spawner cannot act on half a
    /// decision.</summary>
    public enum FieldWaveResolution
    {
        /// <summary>The wave is still in flight — bodies owed, bodies alive, or no Assault.</summary>
        Hold,
        /// <summary>Roster spent, field empty, shutoff still owed: raise the trickle flag and
        /// keep the Assault open.</summary>
        StartTrickle,
        /// <summary>Roster spent, field empty, nothing owed: the wave is cleared.</summary>
        Cleared,
        /// <summary>Roster spent, field empty, shutoff owed and the zone's LAST wave was the
        /// one that just died (M115): arm the terminal and keep the Assault open — no drip,
        /// the walk is done and the switch is what is left.</summary>
        ArmShutoff,
    }

    /// <summary>The pressure that keeps a field Wave honest after its authored bodies are
    /// spent: a slow drip of gatekeepers that continues until the player reaches the zone's
    /// terminal and shuts it off (Zo, 2026-08-26).</summary>
    /// <remarks>
    /// WHY IT EXISTS. A wave BEGINS at the Hatch and ENDS at a terminal
    /// (Docs/Design/0-Gameplay.md), and the 227 m spine is built so that the walk between them
    /// IS the fight. Without a trickle the walk has a hole in it: a player who holds the Hatch
    /// and kills all 48 bodies of zone 1's composite then strolls an empty corridor to press a
    /// button. The objective was positional, and the pressure was not — so the corridor stopped
    /// mattering the moment the last authored body fell.
    ///
    /// TRICKLE BODIES PAY NOTHING. Not salvage, not experience when experience exists. They are
    /// a clock, not an income: paying for them would turn "walk up the lane" into "farm the
    /// lane", which is the exact stall the trickle is there to prevent — and it would let a
    /// player who never advances out-earn one who does. <see cref="RunSim.CurrentKillAward"/>
    /// returns 0 while <see cref="RunSim.FieldTrickleLive"/> is set, so every payout path is
    /// covered at its source rather than at each caller.
    ///
    /// ARITHMETIC. The player walks 5 m/s, and the spine's terminals sit 66 m (zone 1's T2) and
    /// 156 m (zone 2's T5) from the Hatch, so an unhurried walk up is about 13 s and 31 s. At
    /// one body every <see cref="SecondsBetweenBodies"/> that is roughly 4 and 10 bodies met on
    /// the way — enough that the corridor has to be fought through, far short of a second wave.
    /// A player who sprints (7.5 m/s) meets fewer, which is the right incentive: the trickle
    /// rewards moving, and only punishes standing still.
    ///
    /// <see cref="MaxLive"/> is the safety rail. The drip cannot become the encounter: if the
    /// player stops to fight rather than pushing, the lane fills to this count and holds there,
    /// so the cost of stalling is time and ammunition rather than an unwinnable pile. It is
    /// deliberately well under WaveDirector.MaxEnemyCount — the trickle is never the thing that
    /// hits the global cap.
    /// </remarks>
    public static class FieldTrickle
    {
        /// <summary>Seconds between trickle bodies while a zone waits on its shutoff.</summary>
        public const float SecondsBetweenBodies = 3f;

        /// <summary>Most trickle bodies alive at once. A stalled player faces a standing cost,
        /// never a growing one.</summary>
        public const int MaxLive = 6;

        /// <summary>Resolve what a spent-roster check does, in one place. The 2026-08-28
        /// deadlock was two halves of this decision living in two classes and disagreeing:
        /// the drip started once the roster was spent while the pay-zero flag waited on an
        /// empty field, so the drip's own bodies kept the flag down forever — every dripped
        /// body billed the full award and the wave could never clear (wave 5, 07:17 run:
        /// 39 authored, 93 paid kills, abandoned). Whoever spawns or clears must act on this
        /// verdict and nothing else.
        /// <para>Order is load-bearing: <paramref name="aliveCount"/> is checked before
        /// <paramref name="awaitsShutoff"/> because the flag zeroes
        /// <see cref="RunSim.CurrentKillAward"/> — raising it with authored bodies still on
        /// the field would quietly stop paying for the wave the player is fighting.</para></summary>
        /// <para><paramref name="finalWaveDrawn"/> (M115, 2026-09-02): once the zone's last
        /// wave — the one the terminal press draws — is the wave that just died, the verdict is
        /// <see cref="FieldWaveResolution.ArmShutoff"/> rather than the drip. The drip holds
        /// the lane between the opening span and the terminal; after the last wave there is
        /// no lane left to hold, only a switch to throw.</para>
        public static FieldWaveResolution Resolve(RunPhase phase, AssaultState assault,
            bool chainPending, bool rosterDelivering, int aliveCount, bool awaitsShutoff,
            bool finalWaveDrawn)
        {
            if (phase != RunPhase.Tethered) return FieldWaveResolution.Hold;
            if (assault != AssaultState.Assault) return FieldWaveResolution.Hold;
            if (chainPending) return FieldWaveResolution.Hold;
            if (rosterDelivering) return FieldWaveResolution.Hold;
            if (aliveCount > 0) return FieldWaveResolution.Hold;
            if (!awaitsShutoff) return FieldWaveResolution.Cleared;
            return finalWaveDrawn ? FieldWaveResolution.ArmShutoff : FieldWaveResolution.StartTrickle;
        }

        /// <summary>Advance the drip clock by <paramref name="dt"/> and report whether a body is
        /// due. Consumes exactly one interval per true, so a long frame never dumps a burst —
        /// the drip is a cadence, not a budget.
        /// <para><paramref name="liveNow"/> holds the clock at zero once the lane is full, so
        /// standing still cannot bank up a wall of bodies to be released in one tick when the
        /// player finally kills one.</para></summary>
        public static bool DueIn(float dt, int liveNow, ref float accrued)
        {
            if (dt <= 0f) return false;
            if (liveNow >= MaxLive)
            {
                accrued = 0f;
                return false;
            }
            accrued += dt;
            if (accrued < SecondsBetweenBodies) return false;
            accrued -= SecondsBetweenBodies;
            return true;
        }
    }
}
