using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The defended objective. v0 has exactly one: the HATCH — the tunnel/umbilical junction
    /// back to your ship at one end of the bastion. Enemies push toward it; if it falls, the
    /// run is lost (Lost). The four ship subsystems (Reactor, TetherCore, Engines,
    /// LifeSupport) return when the bastion outfitting layer lands; this class stays the
    /// aggregate so RunSim's wiring keeps its shape when they do.
    /// </summary>
    public sealed class ShipSystems
    {
        /// <summary>Default hatch HP pool — a simple starting number for the solo dev to tune.
        /// <para>300 -> 600 (Zo, 2026-08-28). 300 was sized for Hatch-bank waves, where the
        /// player STANDS ON IT. Field waves put the Hatch in the fight while the player is
        /// 66-156 m up the spine, and the 09:24 run lost it: 50 bodies reached it and took the
        /// last quarter in 2.7 s. Doubling the pool doubles every survival figure derived from
        /// it — the reference small group of one Thief and three Defaults now gives ~88 s
        /// instead of ~44, which is a round trip to the near terminal plus a real fight rather
        /// than a sprint against a clock.</para>
        /// <para>Read with HatchBank.FieldWaveDamageScale: that is the other half of the same
        /// sum, and moving either one alone re-times the whole decision to walk away.</para>
        /// </summary>
        public const float HatchMaxHp = 600f;

        /// <summary>Salvage charged per hit point healed on the hatch. Lives here (not Game) so
        /// the end-of-run report and EditMode tests share one constant with the heal path.</summary>
        public const float RepairSalvagePerHp = 2f;

        /// <summary>The umbilical junction back to the ship. Destruction ends the run (Lost).</summary>
        public Subsystem Hatch { get; private set; }

        /// <summary>The three hatch doors and their open flags. The single
        /// <see cref="Hatch"/> subsystem stays the one damage target — per-hatch HP and
        /// the split pool are the co-op demo's work (Docs/Demo - Coop/README.md), not
        /// this aggregate's.</summary>
        public ShipHatches Hatches { get; private set; }

        /// <summary>Raised when the Hatch's HP first reaches 0.</summary>
        public event Action HatchDestroyed;

        /// <summary>True once the Hatch is at 0 HP (run lost).</summary>
        public bool HatchIsDestroyed
        {
            get { return Hatch.IsDestroyed; }
        }

        /// <summary>Default hatch pool (300) — the unupgraded/test path.</summary>
        public ShipSystems() : this(HatchMaxHp)
        {
        }

        /// <summary>Hatch pool from the ship-shop: RunSim passes MetaUpgrades.HatchHp(level).</summary>
        public ShipSystems(float hatchMaxHp)
        {
            Hatch = new Subsystem("Hatch", hatchMaxHp);
            Hatch.Destroyed += OnHatchDestroyed;
            Hatches = new ShipHatches();
        }

        private void OnHatchDestroyed(Subsystem s)
        {
            var handler = HatchDestroyed;
            if (handler != null) handler();
        }
    }
}
