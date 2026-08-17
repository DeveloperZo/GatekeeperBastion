using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// A damageable ship subsystem with clamped HP. Fires Destroyed once each time HP crosses to 0
    /// (repairing above 0 re-arms it).
    /// </summary>
    public sealed class Subsystem
    {
        /// <summary>Display/debug name, e.g. "Reactor".</summary>
        public string Name { get; private set; }

        /// <summary>Maximum hit points.</summary>
        public float MaxHp { get; private set; }

        /// <summary>Current hit points, always in [0, MaxHp].</summary>
        public float Hp { get; private set; }

        /// <summary>True while Hp is 0.</summary>
        public bool IsDestroyed
        {
            get { return Hp <= 0f; }
        }

        /// <summary>Raised when Hp reaches 0 (once per destruction).</summary>
        public event Action<Subsystem> Destroyed;

        public Subsystem(string name, float maxHp)
        {
            if (maxHp <= 0f) throw new ArgumentOutOfRangeException("maxHp");
            Name = name;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        /// <summary>Apply damage; HP clamps at 0. Non-positive amounts and hits on a destroyed system are ignored.</summary>
        public void Damage(float amount)
        {
            if (amount <= 0f || IsDestroyed) return;
            Hp = Math.Max(0f, Hp - amount);
            if (Hp <= 0f)
            {
                var handler = Destroyed;
                if (handler != null) handler(this);
            }
        }

        /// <summary>Restore HP; clamps at MaxHp. Repairing a destroyed system brings it back online.</summary>
        public void Repair(float amount)
        {
            if (amount <= 0f) return;
            Hp = Math.Min(MaxHp, Hp + amount);
        }
    }
}
