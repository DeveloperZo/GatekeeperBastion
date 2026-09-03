using System;
using System.Collections.Generic;

namespace GatekeepersProtocol.Sim
{
    /// <summary>What a wave is made of, as one word the player reads before it lands (M116).
    /// Player-facing names are Mass, Pack, Convoy — <see cref="WaveShapes.Name"/>.</summary>
    public enum WaveShape
    {
        /// <summary>All Default.</summary>
        Mass = 0,
        /// <summary>Hunters, and a few Cloakers once they are legal. Never a Thief.</summary>
        Pack = 1,
        /// <summary>Thieves and Cloakers. Never a Hunter.</summary>
        Convoy = 2,
    }

    /// <summary>Wave shapes: which shape a wave draws, and what a shape does to a spec's four
    /// counts. Pure functions of the wave number, so host and clients agree and the terminal
    /// prompt can name the last wave before it is called.
    /// <para>A shape is SHARES of the unchanged <c>11 + 7i</c> roster (Docs/Design/4-Data.md,
    /// "Waves and intensity"): each non-Default count is <c>round(share × roster)</c>, capped by
    /// <see cref="WaveDirector.KindCap"/> (the debut gates: Hunter from wave 2, Thief from 3,
    /// Cloaker from 4), and Default is the remainder. The roster is the dial-0 solo count, so
    /// the dial's and the party's surplus stay all Default — the same rule as before the
    /// shapes. The four counts always sum to <see cref="WaveSpec.enemyCount"/>; the spawner
    /// relies on that without checking.</para>
    /// <para>Which wave draws which shape: a zone's opening span alternates Mass, Pack, Mass…
    /// from the zone's first wave, and its terminal-called last wave is Convoy where the Thief
    /// is legal, else Pack. On the demo table that is 1 Mass, 2 Pack, 3 Mass, 4 Pack,
    /// 5 Convoy. Waves off the table (probes, a campaign save) alternate Mass/Pack from
    /// wave 6; the finale lever pins its own.</para></summary>
    public static class WaveShapes
    {
        public const float PackHunterShare = 0.35f;
        public const float PackCloakerShare = 0.05f;
        public const float ConvoyThiefShare = 0.15f;
        public const float ConvoyCloakerShare = 0.15f;

        /// <summary>The shape a 1-based field wave draws.</summary>
        public static WaveShape For(int wave)
        {
            if (wave < 1) wave = 1;
            int zone = DemoZones.ZoneOfWave(wave);
            if (zone > 0 && DemoZones.WaveIsIn(zone, wave))
            {
                int first = DemoZones.FirstWave(zone);
                if (wave == DemoZones.EndWave(zone) && wave > first)
                {
                    // The last wave belongs to the terminal (M115) and is the zone's heaviest
                    // draw; zone 1 ends before the Thief is legal, so its last wave is a Pack.
                    return WaveDirector.KindCap(GatekeeperKind.Thief, wave - 1) > 0
                        ? WaveShape.Convoy
                        : WaveShape.Pack;
                }
                return (wave - first) % 2 == 0 ? WaveShape.Mass : WaveShape.Pack;
            }
            return (wave - WaveDirector.MaxWaves - 1) % 2 == 0 ? WaveShape.Mass : WaveShape.Pack;
        }

        /// <summary>Player-facing name. Literals, not <c>ToString</c>: the prompt reads this
        /// every frame.</summary>
        public static string Name(WaveShape shape)
        {
            switch (shape)
            {
                case WaveShape.Pack: return "Pack";
                case WaveShape.Convoy: return "Convoy";
                default: return "Mass";
            }
        }

        /// <summary>Write the shape's four counts onto a spec whose <see cref="WaveSpec.enemyCount"/>
        /// is already set. Shares are taken of the dial-0 solo roster for
        /// <paramref name="waveIndex"/> (0-based), never of the dialled count.</summary>
        public static WaveSpec Apply(WaveSpec spec, WaveShape shape, int waveIndex)
        {
            if (waveIndex < 0) waveIndex = 0;
            int roster = Math.Min(WaveDirector.MaxEnemyCount,
                                  WaveDirector.BaseEnemyCount + WaveDirector.CountPerWave * waveIndex);
            float hunter = 0f, thief = 0f, cloaker = 0f;
            switch (shape)
            {
                case WaveShape.Pack:
                    hunter = PackHunterShare;
                    cloaker = PackCloakerShare;
                    break;
                case WaveShape.Convoy:
                    thief = ConvoyThiefShare;
                    cloaker = ConvoyCloakerShare;
                    break;
                default:
                    break;
            }
            int left = spec.enemyCount;
            spec.hunterCount = Take(Share(hunter, roster), GatekeeperKind.Hunter, waveIndex, ref left);
            spec.thiefCount = Take(Share(thief, roster), GatekeeperKind.Thief, waveIndex, ref left);
            spec.cloakerCount = Take(Share(cloaker, roster), GatekeeperKind.Cloaker, waveIndex, ref left);
            spec.defaultCount = left;
            return spec;
        }

        /// <summary>round(share × bodies), midpoints up, snapped to 4 decimals first for the
        /// same reason <see cref="WaveDirector.NextWave"/> snaps its dial product.</summary>
        static int Share(float share, int bodies)
        {
            if (share <= 0f || bodies <= 0) return 0;
            double scaled = Math.Round((double)share * bodies, 4, MidpointRounding.AwayFromZero);
            return (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
        }

        static int Take(int want, GatekeeperKind kind, int waveIndex, ref int left)
        {
            int n = Math.Min(want, WaveDirector.KindCap(kind, waveIndex));
            if (n > left) n = left;
            if (n < 0) n = 0;
            left -= n;
            return n;
        }

        /// <summary>The wave's roster in emission order (index 0 lands first), GROUPED BY KIND:
        /// every run of <see cref="WaveSpec.groupSize"/> is one kind where the counts allow —
        /// Hunters together, Thieves together, Cloakers together, Defaults filling a kind's
        /// short last group and making up the rest — so a Hunter group is three Hunters, not
        /// two Defaults and a Hunter. The ORDER of the full groups is shuffled on the sim's
        /// seeded stream so the mix still arrives across the wave rather than kind by kind; a
        /// short tail group stays last because the spawner cuts the queue in fixed widths.
        /// Once per wave, so the list growth is not a per-frame allocation.
        /// <para>A FINALE SPEC (<see cref="WaveSpec.finaleElite"/>) is the exception to the
        /// mix: its Thief block goes LAST, after the escort, so with the group width
        /// <see cref="MasterControl.ApplyFinaleElite"/> sets the elite is the short tail group
        /// and the shuffle cannot move it — the escort lands first, the elite alone
        /// after.</para></summary>
        public static void GroupByKind(WaveSpec spec, Random rng, List<GatekeeperKind> queue)
        {
            queue.Clear();
            int width = Math.Max(1, spec.groupSize);
            int defaults = Math.Max(0, spec.defaultCount);
            if (spec.finaleElite)
            {
                AppendBlock(queue, GatekeeperKind.Hunter, spec.hunterCount, width, ref defaults);
                AppendBlock(queue, GatekeeperKind.Cloaker, spec.cloakerCount, width, ref defaults);
                AppendBlock(queue, GatekeeperKind.Thief, spec.thiefCount, width, ref defaults);
            }
            else
            {
                AppendBlock(queue, GatekeeperKind.Hunter, spec.hunterCount, width, ref defaults);
                AppendBlock(queue, GatekeeperKind.Thief, spec.thiefCount, width, ref defaults);
                AppendBlock(queue, GatekeeperKind.Cloaker, spec.cloakerCount, width, ref defaults);
            }
            for (int i = 0; i < defaults; i++) queue.Add(GatekeeperKind.Default);
            if (rng != null) ShuffleFullGroups(queue, width, rng);
        }

        static void AppendBlock(List<GatekeeperKind> queue, GatekeeperKind kind, int count, int width,
                                ref int defaults)
        {
            for (int i = 0; i < count; i++) queue.Add(kind);
            int partial = queue.Count % width;
            if (partial == 0) return;
            int fill = Math.Min(width - partial, defaults);
            for (int i = 0; i < fill; i++) queue.Add(GatekeeperKind.Default);
            defaults -= fill;
        }

        static void ShuffleFullGroups(List<GatekeeperKind> queue, int width, Random rng)
        {
            int groups = queue.Count / width;
            for (int i = groups - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                if (j == i) continue;
                for (int k = 0; k < width; k++)
                {
                    GatekeeperKind swap = queue[i * width + k];
                    queue[i * width + k] = queue[j * width + k];
                    queue[j * width + k] = swap;
                }
            }
        }
    }
}
