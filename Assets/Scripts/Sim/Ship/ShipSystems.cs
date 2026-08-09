using System;

namespace GatekeeperBastion.Sim
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
        /// <summary>Default hatch HP pool — a simple starting number for the solo dev to tune.</summary>
        public const float HatchMaxHp = 300f;

        /// <summary>Salvage charged per hit point healed on the hatch. Lives here (not Game) so
        /// the end-of-run report and EditMode tests share one constant with the heal path.</summary>
        public const float RepairSalvagePerHp = 2f;

        /// <summary>The umbilical junction back to the ship. Destruction ends the run (Lost).</summary>
        public Subsystem Hatch { get; private set; }

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
        }

        private void OnHatchDestroyed(Subsystem s)
        {
            var handler = HatchDestroyed;
            if (handler != null) handler();
        }
    }
}
