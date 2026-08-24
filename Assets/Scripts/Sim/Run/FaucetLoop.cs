using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The modifier axis a faucet shutoff drafts. Order is the draft's reveal order:
    /// one option at the mark, one more per Wave endured past it (Density first, then
    /// Toughness, then Tempo). The axes are the core axes from Docs/Brainstorms/economy.md.</summary>
    public enum FaucetAxis : byte
    {
        Density = 0,
        Toughness = 1,
        Tempo = 2,
    }

    /// <summary>
    /// The faucet-loop rules (Docs/Brainstorms/coop-bastion-complexity.md, the faucet loop):
    /// a field terminal pours Waves that hunt the Hatch until the Boarder walks back and
    /// turns it off. Enduring Waves fills unbanked; holding past the mark grows the modifier
    /// draft; the chosen axis hardens the following Hatch-bank window and raises its convert
    /// multiplier from the same choice (one-source stake). Pure rules in the HatchBank mold —
    /// RunSim owns the state, this class owns the numbers and the spec transform.
    /// </summary>
    public static class FaucetLoop
    {
        /// <summary>Waves endured before the origin terminal is marked and the off verb is
        /// legal. The mark is also the commitment floor — there is no shutoff before it.</summary>
        public const int MarkAfterWaves = 2;

        /// <summary>Most options a draft ever offers, however long the hold.</summary>
        public const int DraftCap = 3;

        /// <summary>Unbanked paid per faucet Wave endured, split across connected slots the
        /// same way a kill award is. Income rides Waves endured, not kills.</summary>
        public const long WaveEndureBonus = 100L;

        /// <summary>Added to the Hatch-bank convert multiplier when a drafted axis is applied
        /// to that window — the reward half of the axis choice (one-source stake).</summary>
        public const float ConvertBonus = 0.25f;

        /// <summary>Density: multiplier on the bank Wave's Default count.</summary>
        public const float DensityDefaultMul = 1.5f;

        /// <summary>Toughness: multiplier on the bank Wave's HP multiplier.</summary>
        public const float ToughnessHpMul = 1.5f;

        /// <summary>Tempo: multiplier on the bank Wave's group gap (smaller = faster).</summary>
        public const float TempoIntervalMul = 0.6f;

        /// <summary>Options the draft offers after enduring <paramref name="wavesEndured"/>
        /// Waves: 0 before the mark, then one plus one per Wave past it, capped.</summary>
        public static int DraftSize(int wavesEndured)
        {
            int n = wavesEndured - MarkAfterWaves + 1;
            if (n < 0) n = 0;
            if (n > DraftCap) n = DraftCap;
            return n;
        }

        /// <summary>Apply a drafted axis to a Hatch-bank spec. Keeps the WaveSpec invariant
        /// (enemyCount == defaultCount + hunterCount + thiefCount) the way
        /// ApplyHatchComposition does, and floors Tempo at the director's minimum gap.</summary>
        public static WaveSpec ApplyAxis(WaveSpec spec, FaucetAxis axis)
        {
            switch (axis)
            {
                case FaucetAxis.Density:
                    int extra = (int)Math.Round(spec.defaultCount * (DensityDefaultMul - 1f));
                    if (extra < 1) extra = 1;
                    spec.defaultCount += extra;
                    spec.enemyCount += extra;
                    break;
                case FaucetAxis.Toughness:
                    spec.enemyHealthMultiplier *= ToughnessHpMul;
                    break;
                case FaucetAxis.Tempo:
                    spec.spawnInterval *= TempoIntervalMul;
                    if (spec.spawnInterval < WaveDirector.MinSpawnInterval)
                        spec.spawnInterval = WaveDirector.MinSpawnInterval;
                    break;
            }
            return spec;
        }

        /// <summary>Display name for an axis. Static strings — allocation-free for HUD use.</summary>
        public static string AxisName(FaucetAxis axis)
        {
            switch (axis)
            {
                case FaucetAxis.Density: return "DENSITY";
                case FaucetAxis.Toughness: return "TOUGHNESS";
                case FaucetAxis.Tempo: return "TEMPO";
                default: return "?";
            }
        }
    }
}
